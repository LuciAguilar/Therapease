using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TherapEase.Application.Identidad.Interfaces.Servicios;
using TherapEase.Infrastructure.Data;
using static TherapEase.IntegrationTests.ServidorIdentidadB01Revision;

namespace TherapEase.IntegrationTests;

/// <summary>
/// Ejecuta solicitudes simultáneas a los servicios reales de usuarios y acceso.
/// Comprueba el último Superusuario, nombres únicos y el bloqueo de una cuenta ficticia.
/// </summary>
public class ConcurrenciaServidorB01Tests(PostgreSqlRevision postgres) : IClassFixture<PostgreSqlRevision>
{


    // ──── ÚLTIMO SUPERUSUARIO DESDE LOS SERVICIOS ──────────────────────────────────────────


    /// Los dos últimos Superusuarios intentan desactivar al otro o quitarle el rol al mismo tiempo.
    /// El cambio rechazado tampoco debe dejar su evento, rol o cuenta a medias.
    [Theory]
    [InlineData("desactivar", "desactivar")]
    [InlineData("cambiar-rol", "cambiar-rol")]
    [InlineData("desactivar", "cambiar-rol")]
    public async Task Dos_Ultimos_Superusuarios_Cambian_Al_Otro_Y_Queda_Uno(
        string primeraOperacion, string segundaOperacion)
    {
        // Arrange
        var escenario = await postgres.Crear();
        using var preparacion = escenario.Servicios();
        var primero = await CrearAsync(preparacion, "super-a-b01", "Superusuario");
        var segundo = await CrearAsync(preparacion, "super-b-b01", "Superusuario");
        var cookiePrimero = await EntrarAsync(preparacion, "super-a-b01");
        var cookieSegundo = await EntrarAsync(preparacion, "super-b-b01");
        var espera = new EsperaSuperusuariosB01Revision();
        using var proveedor = escenario.Servicios(servicios => servicios.AddDbContext<ContextoDeDatos>(
            opciones => opciones.AddInterceptors(espera)));
        var anteriores = new Dictionary<Guid, string>();
        foreach (var id in new[] { primero, segundo })
            anteriores[id] = await PostgreSqlRevision.Valor<string>(escenario.App,
                $"""SELECT row_to_json(u)::text FROM "AspNetUsers" u WHERE "Id"='{id}'""");

        async Task<bool> CambiarAlOtroAsync(Guid id, string cookie, string operacion)
        {
            using var solicitud = new SolicitudIdentidad(proveedor, cookie);
            Assert.True(await solicitud.AutenticarAsync());
            var servicio = solicitud.Obtener<IServicioUsuarios>();
            return operacion == "desactivar"
                ? (await servicio.DesactivarUsuarioAsync(id)).Exito
                : (await servicio.CambiarRolAsync(id, "Usuaria")).Exito;
        }

        // Act
        var resultados = await Task.WhenAll(
            CambiarAlOtroAsync(segundo, cookiePrimero, primeraOperacion),
            CambiarAlOtroAsync(primero, cookieSegundo, segundaOperacion)).WaitAsync(TimeSpan.FromSeconds(30));

        // Assert
        Assert.Equal(2, espera.Lecturas);
        Assert.Single(resultados, exito => exito);
        Assert.Single(resultados, exito => !exito);
        Assert.Equal(1L, await PostgreSqlRevision.Valor<long>(escenario.App, """
            SELECT count(*) FROM "AspNetUsers" u
            JOIN "AspNetUserRoles" ur ON ur."UserId"=u."Id"
            JOIN "AspNetRoles" r ON r."Id"=ur."RoleId"
            WHERE u."Activo" AND r."Name"='Superusuario'
            """));
        Assert.Equal(1L, await PostgreSqlRevision.Valor<long>(escenario.App,
            """SELECT count(*) FROM "EventoAuditoria" e"""));
        var rechazado = resultados[0] ? primero : segundo;
        var guardado = await PostgreSqlRevision.Valor<string>(escenario.App,
            $"""SELECT row_to_json(u)::text FROM "AspNetUsers" u WHERE "Id"='{rechazado}'""");
        Assert.True(anteriores[rechazado] == guardado, "La cuenta rechazada no quedó como antes.");
        Assert.Equal("Superusuario", await PostgreSqlRevision.Valor<string>(escenario.App, $"""
            SELECT r."Name" FROM "AspNetRoles" r JOIN "AspNetUserRoles" ur ON ur."RoleId"=r."Id"
            WHERE ur."UserId"='{rechazado}'
            """));
        await using var lectura = escenario.Contexto();
        var evento = await lectura.EventosAuditoria.AsNoTracking().SingleAsync();
        Assert.Equal(resultados[0] ? primero : segundo, evento.IdUsuarioActor);
        Assert.Equal(resultados[0] ? segundo : primero, evento.IdRegistro);
    }


    // ──── VARIOS INTENTOS DE ACCESO ────────────────────────────────────────────────────────


    /// Veinte accesos incorrectos simultáneos bloquean una sola cuenta sin abrir sesiones.
    /// Otro usuario conserva su acceso; el resultado no demuestra cualquier volumen posible.
    [Fact]
    public async Task Veinte_Fallos_Simultaneos_Bloquean_Solo_La_Cuenta_Afectada()
    {
        // Arrange
        var escenario = await postgres.Crear();
        using var proveedor = escenario.Servicios();
        var objetivo = await CrearAsync(proveedor, "objetivo-b01");
        var otra = await CrearAsync(proveedor, "otra-b01");
        var continuar = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task FallarAsync()
        {
            await continuar.Task;
            using var solicitud = new SolicitudIdentidad(proveedor);
            var respuesta = await solicitud.Obtener<IServicioAcceso>()
                .IniciarSesionAsync("objetivo-b01", "incorrecta-ficticia-b01");
            Assert.False(respuesta.Exito);
            Assert.Null(respuesta.Datos);
            Assert.Equal(0, solicitud.Contexto.Response.Headers.SetCookie.Count);
        }
        var intentos = Enumerable.Range(0, 20).Select(_ => FallarAsync()).ToArray();
        var antes = DateTimeOffset.UtcNow;

        // Act
        continuar.SetResult();
        await Task.WhenAll(intentos).WaitAsync(TimeSpan.FromSeconds(45));

        // Assert
        Assert.True(await PostgreSqlRevision.Valor<bool>(escenario.App,
            $"""SELECT "LockoutEnd">CURRENT_TIMESTAMP AND "AccessFailedCount"=0 FROM "AspNetUsers" WHERE "Id"='{objetivo}'"""));
        var fin = await PostgreSqlRevision.Valor<DateTime>(escenario.App,
            $"""SELECT "LockoutEnd" FROM "AspNetUsers" WHERE "Id"='{objetivo}'""");
        Assert.InRange(fin, antes.UtcDateTime.AddMinutes(15), DateTime.UtcNow.AddMinutes(15));
        using (var solicitud = new SolicitudIdentidad(proveedor))
        {
            Assert.False((await solicitud.Obtener<IServicioAcceso>()
                .IniciarSesionAsync("objetivo-b01", Contrasena)).Exito);
            Assert.Equal(0, solicitud.Contexto.Response.Headers.SetCookie.Count);
        }
        Assert.True(await PostgreSqlRevision.Valor<bool>(escenario.App,
            $"""SELECT "LockoutEnd" IS NULL AND "AccessFailedCount"=0 FROM "AspNetUsers" WHERE "Id"='{otra}'"""));
        Assert.False(string.IsNullOrEmpty(await EntrarAsync(proveedor, "otra-b01")));
    }

    /// Un acceso correcto reinicia los cuatro fallos anteriores; el bloqueo necesita cinco nuevos.
    [Fact]
    public async Task Acceso_Correcto_Reinicia_Contador_Antes_De_Cinco_Nuevos_Fallos()
    {
        // Arrange
        var escenario = await postgres.Crear();
        using var proveedor = escenario.Servicios();
        var objetivo = await CrearAsync(proveedor, "objetivo-b01");
        async Task FallarAsync()
        {
            using var solicitud = new SolicitudIdentidad(proveedor);
            Assert.False((await solicitud.Obtener<IServicioAcceso>()
                .IniciarSesionAsync("objetivo-b01", "incorrecta-ficticia-b01")).Exito);
        }
        for (var intento = 0; intento < 4; intento++) await FallarAsync();
        Assert.Equal(4, await PostgreSqlRevision.Valor<int>(escenario.App,
            $"""SELECT "AccessFailedCount" FROM "AspNetUsers" WHERE "Id"='{objetivo}'"""));

        // Act
        await EntrarAsync(proveedor, "  OBJETIVO-B01  ");

        // Assert
        Assert.Equal(0, await PostgreSqlRevision.Valor<int>(escenario.App,
            $"""SELECT "AccessFailedCount" FROM "AspNetUsers" WHERE "Id"='{objetivo}'"""));

        // Act
        for (var intento = 0; intento < 4; intento++) await FallarAsync();

        // Assert
        Assert.True(await PostgreSqlRevision.Valor<bool>(escenario.App,
            $"""SELECT "LockoutEnd" IS NULL AND "AccessFailedCount"=4 FROM "AspNetUsers" WHERE "Id"='{objetivo}'"""));

        // Act
        await FallarAsync();

        // Assert
        Assert.True(await PostgreSqlRevision.Valor<bool>(escenario.App,
            $"""SELECT "LockoutEnd">CURRENT_TIMESTAMP AND "AccessFailedCount"=0 FROM "AspNetUsers" WHERE "Id"='{objetivo}'"""));
    }
}
