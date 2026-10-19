using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TherapEase.Application.Identidad.Interfaces.Servicios;
using TherapEase.Infrastructure.Data;
using static TherapEase.IntegrationTests.ServidorIdentidadRevision;

namespace TherapEase.IntegrationTests;

/// <summary>
/// Prueba cambios al mismo tiempo en usuarios y accesos.
/// Comprueba que siempre quede un superusuario
/// y que los intentos fallidos bloqueen solo al usuario correcto.
/// </summary>
public class ConcurrenciaServidorTests(PostgreSqlRevision postgres) : IClassFixture<PostgreSqlRevision>
{


    // ──── ÚLTIMO SUPERUSUARIO ──────────────────────────────────────────────────────────────


    /// Los dos últimos superusuarios intentan, al mismo tiempo, desactivar al otro o quitarle el rol.
    /// Solo uno lo logra; el otro usuario queda igual que antes y sin registro en la auditoría.
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
        var primero = await CrearAsync(preparacion, "super-a-ficticio", "Superusuario");
        var segundo = await CrearAsync(preparacion, "super-b-ficticio", "Superusuario");
        var cookiePrimero = await EntrarAsync(preparacion, "super-a-ficticio");
        var cookieSegundo = await EntrarAsync(preparacion, "super-b-ficticio");

        // Estas solicitudes se detienen hasta que ambas cuentan los superusuarios.

        var espera = new EsperaSuperusuariosRevision();
        using var proveedor = escenario.Servicios(servicios => servicios.AddDbContext<ContextoDeDatos>(
            opciones => opciones.AddInterceptors(espera)));
        var anteriores = new Dictionary<Guid, string>();
        foreach (var id in new[] { primero, segundo })
            anteriores[id] = await PostgreSqlRevision.Valor<string>(escenario.App,
                $"""SELECT row_to_json(u)::text FROM "AspNetUsers" u WHERE "Id"='{id}'""");

        // Un superusuario intenta desactivar al otro o quitarle el rol.

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


    /// Veinte intentos con contraseña incorrecta, al mismo tiempo, bloquean solo a ese usuario.
    /// No abren sesiones y otro usuario sigue entrando. No prueba cantidades mayores.
    [Fact]
    public async Task Veinte_Fallos_Simultaneos_Bloquean_Solo_El_Usuario_Afectado()
    {
        // Arrange
        var escenario = await postgres.Crear();
        using var proveedor = escenario.Servicios();
        var objetivo = await CrearAsync(proveedor, "objetivo-ficticio");
        var otra = await CrearAsync(proveedor, "otra-ficticio");

        // Los veinte intentos esperan esta señal para salir al mismo tiempo.

        var continuar = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task FallarAsync()
        {
            await continuar.Task;
            using var solicitud = new SolicitudIdentidad(proveedor);
            var respuesta = await solicitud.Obtener<IServicioAcceso>()
                .IniciarSesionAsync("objetivo-ficticio", "contrasena-incorrecta-ficticia");
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
                .IniciarSesionAsync("objetivo-ficticio", Contrasena)).Exito);
            Assert.Equal(0, solicitud.Contexto.Response.Headers.SetCookie.Count);
        }
        Assert.True(await PostgreSqlRevision.Valor<bool>(escenario.App,
            $"""SELECT "LockoutEnd" IS NULL AND "AccessFailedCount"=0 FROM "AspNetUsers" WHERE "Id"='{otra}'"""));
        Assert.False(string.IsNullOrEmpty(await EntrarAsync(proveedor, "otra-ficticio")));
    }

    /// Entrar bien borra los cuatro fallos anteriores.
    /// Después hacen falta cinco fallos nuevos para bloquear al usuario.
    [Fact]
    public async Task Acceso_Correcto_Reinicia_Contador_Antes_De_Cinco_Nuevos_Fallos()
    {
        // Arrange
        var escenario = await postgres.Crear();
        using var proveedor = escenario.Servicios();
        var objetivo = await CrearAsync(proveedor, "objetivo-ficticio");
        async Task FallarAsync()
        {
            using var solicitud = new SolicitudIdentidad(proveedor);
            Assert.False((await solicitud.Obtener<IServicioAcceso>()
                .IniciarSesionAsync("objetivo-ficticio", "contrasena-incorrecta-ficticia")).Exito);
        }
        for (var intento = 0; intento < 4; intento++) await FallarAsync();
        Assert.Equal(4, await PostgreSqlRevision.Valor<int>(escenario.App,
            $"""SELECT "AccessFailedCount" FROM "AspNetUsers" WHERE "Id"='{objetivo}'"""));

        // Act
        await EntrarAsync(proveedor, "  OBJETIVO-FICTICIO  ");

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
