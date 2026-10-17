using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TherapEase.Application.Identidad.Constantes;
using TherapEase.Application.Identidad.Interfaces.Servicios;
using TherapEase.Application.Identidad.Modelos;
using TherapEase.Infrastructure.Identidad.Entidades;
using Microsoft.AspNetCore.Identity;
using static TherapEase.IntegrationTests.ServidorIdentidadB01Revision;

namespace TherapEase.IntegrationTests;

/// <summary>
/// Comprueba altas, datos inválidos y operaciones repetidas de usuarios con PostgreSQL real.
/// Los rechazos deben conservar usuarios, roles y auditoría sin cambios parciales.
/// </summary>
public class UsuariosServidorB01Tests(PostgreSqlRevision postgres) : IClassFixture<PostgreSqlRevision>
{


    // ──── ALTAS Y DATOS INVÁLIDOS ──────────────────────────────────────────────────────────


    /// Un nombre vacío, demasiado largo o no admitido, o un rol desconocido, no crea nada.
    [Theory]
    [InlineData(null, "Usuaria")]
    [InlineData("", "Usuaria")]
    [InlineData("   ", "Usuaria")]
    [InlineData("nombre con espacios", "Usuaria")]
    [InlineData("<script>ficticio</script>", "Usuaria")]
    [InlineData("ficticio-' OR 1=1;--", "Usuaria")]
    [InlineData("usuario-ficticio", "Capturista")]
    [InlineData("usuario-ficticio", "superusuario")]
    [InlineData("usuario-ficticio", "")]
    [InlineData("usuario-ficticio", null)]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", "Usuaria")]
    public async Task Alta_Invalida_No_Guarda_Usuario_Rol_Ni_Evento(string? nombre, string? rol)
    {
        // Arrange
        var escenario = await postgres.Crear();
        using var proveedor = escenario.Servicios();
        await CrearAsync(proveedor, "operador-b01", "Superusuario");
        var cookie = await EntrarAsync(proveedor, "operador-b01");
        using var solicitud = new SolicitudIdentidad(proveedor, cookie);
        Assert.True(await solicitud.AutenticarAsync());
        var anterior = await EstadoAsync(escenario);

        // Act
        var respuesta = await solicitud.Obtener<IServicioUsuarios>().CrearUsuarioAsync(nombre!, rol!, "Nombre Ficticio");

        // Assert
        Assert.False(respuesta.Exito);
        Assert.Null(respuesta.Datos);
        Assert.Equal(MensajesDeIdentidad.DatosDeUsuarioInvalidos, respuesta.Mensaje);
        await ComprobarSinCambiosAsync(escenario, anterior);
    }

    /// El alta válida admite ambos roles y un nombre de 50 caracteres; quita espacios exteriores.
    /// Lo guardado contiene una temporal válida y la obligación de cambiarla.
    [Theory]
    [InlineData("Usuaria")]
    [InlineData("Superusuario")]
    public async Task Alta_Valida_Conserva_Nombre_Rol_Y_Temporal(string rol)
    {
        // Arrange
        var escenario = await postgres.Crear();
        using var proveedor = escenario.Servicios();
        await CrearAsync(proveedor, "operador-b01", "Superusuario");
        var cookie = await EntrarAsync(proveedor, "operador-b01");
        using var solicitud = new SolicitudIdentidad(proveedor, cookie);
        Assert.True(await solicitud.AutenticarAsync());
        var nombre = new string('a', 50);

        // Act
        var respuesta = await solicitud.Obtener<IServicioUsuarios>().CrearUsuarioAsync($"  {nombre}  ", rol, "Nombre Ficticio");

        // Assert
        Assert.True(respuesta.Exito);
        var datos = Assert.IsType<ContrasenaTemporalEntregada>(respuesta.Datos);
        Assert.Equal(nombre, datos.Usuario.NombreUsuario);
        Assert.Equal(rol, datos.Usuario.Rol);
        Assert.True(datos.Usuario.Activo);
        Assert.True(datos.Usuario.DebeCambiarContrasena);
        Assert.True(datos.ContrasenaTemporal.Length >= 12);
        using var lectura = proveedor.CreateScope();
        var usuarios = lectura.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
        var guardado = await usuarios.FindByIdAsync(datos.Usuario.IdUsuario.ToString());
        Assert.NotNull(guardado);
        Assert.Equal(nombre, guardado.UserName);
        Assert.True(guardado.DebeCambiarContrasena);
        Assert.True(await usuarios.CheckPasswordAsync(guardado, datos.ContrasenaTemporal));
        Assert.Equal(rol, Assert.Single(await usuarios.GetRolesAsync(guardado)));
        Assert.Equal(1L, await PostgreSqlRevision.Valor<long>(escenario.App,
            """SELECT count(*) FROM "EventoAuditoria" e"""));
    }

    /// Un nombre ya usado sigue reservado aunque la cuenta esté inactiva o cambien mayúsculas.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Alta_Duplicada_Normalizada_No_Modifica_Cuenta_Existente(bool inactiva)
    {
        // Arrange
        var escenario = await postgres.Crear();
        using var proveedor = escenario.Servicios();
        await CrearAsync(proveedor, "operador-b01", "Superusuario");
        var objetivo = await CrearAsync(proveedor, "objetivo-b01");
        if (inactiva)
            await PostgreSqlRevision.Sql(escenario.App,
                $"""UPDATE "AspNetUsers" SET "Activo"=false WHERE "Id"='{objetivo}'""");
        var cookie = await EntrarAsync(proveedor, "operador-b01");
        using var solicitud = new SolicitudIdentidad(proveedor, cookie);
        Assert.True(await solicitud.AutenticarAsync());
        var anterior = await EstadoAsync(escenario);

        // Act
        var respuesta = await solicitud.Obtener<IServicioUsuarios>()
            .CrearUsuarioAsync("  OBJETIVO-B01  ", "Superusuario", "Nombre Ficticio");

        // Assert
        Assert.False(respuesta.Exito);
        Assert.Null(respuesta.Datos);
        Assert.Equal(MensajesDeIdentidad.NombreDeUsuarioDuplicado, respuesta.Mensaje);
        await ComprobarSinCambiosAsync(escenario, anterior);
    }


    // ──── OPERACIONES RECHAZADAS ──────────────────────────────────────────────────────────


    /// Un usuario que no existe no puede desactivarse, cambiar de rol ni recibir una temporal.
    [Theory]
    [InlineData("desactivar")]
    [InlineData("cambiar-rol")]
    [InlineData("restablecer")]
    public async Task Objetivo_Inexistente_No_Guarda_Cambios(string operacion)
    {
        // Arrange
        var escenario = await postgres.Crear();
        using var proveedor = escenario.Servicios();
        await CrearAsync(proveedor, "operador-b01", "Superusuario");
        var cookie = await EntrarAsync(proveedor, "operador-b01");
        using var solicitud = new SolicitudIdentidad(proveedor, cookie);
        Assert.True(await solicitud.AutenticarAsync());
        var servicio = solicitud.Obtener<IServicioUsuarios>();
        var objetivo = Guid.NewGuid();
        var anterior = await EstadoAsync(escenario);

        // Act
        var exito = operacion switch
        {
            "desactivar" => (await servicio.DesactivarUsuarioAsync(objetivo)).Exito,
            "cambiar-rol" => (await servicio.CambiarRolAsync(objetivo, "Superusuario")).Exito,
            "restablecer" => (await servicio.RestablecerContrasenaAsync(objetivo)).Exito,
            _ => throw new ArgumentOutOfRangeException(nameof(operacion))
        };

        // Assert
        Assert.False(exito);
        await ComprobarSinCambiosAsync(escenario, anterior);
    }

    /// Un rol desconocido o el mismo rol actual se rechazan sin renovar sesiones ni crear eventos.
    [Theory]
    [InlineData("Capturista")]
    [InlineData("superusuario")]
    [InlineData("Usuaria")]
    public async Task Cambio_Rol_Invalido_O_Repetido_No_Escribe(string rol)
    {
        // Arrange
        var escenario = await postgres.Crear();
        using var proveedor = escenario.Servicios();
        await CrearAsync(proveedor, "operador-b01", "Superusuario");
        var objetivo = await CrearAsync(proveedor, "objetivo-b01");
        var cookie = await EntrarAsync(proveedor, "operador-b01");
        using var solicitud = new SolicitudIdentidad(proveedor, cookie);
        Assert.True(await solicitud.AutenticarAsync());
        var anterior = await EstadoAsync(escenario);

        // Act
        var respuesta = await solicitud.Obtener<IServicioUsuarios>().CambiarRolAsync(objetivo, rol);

        // Assert
        Assert.False(respuesta.Exito);
        Assert.Null(respuesta.Datos);
        await ComprobarSinCambiosAsync(escenario, anterior);
    }

    /// Desactivar otra vez o restablecer una cuenta ya inactiva no cambia sus datos ni eventos.
    [Theory]
    [InlineData("desactivar")]
    [InlineData("restablecer")]
    public async Task Objetivo_Inactivo_No_Se_Modifica(string operacion)
    {
        // Arrange
        var escenario = await postgres.Crear();
        using var proveedor = escenario.Servicios();
        await CrearAsync(proveedor, "operador-b01", "Superusuario");
        var objetivo = await CrearAsync(proveedor, "objetivo-b01");
        var cookie = await EntrarAsync(proveedor, "operador-b01");
        using (var primera = new SolicitudIdentidad(proveedor, cookie))
        {
            Assert.True(await primera.AutenticarAsync());
            Assert.True((await primera.Obtener<IServicioUsuarios>().DesactivarUsuarioAsync(objetivo)).Exito);
        }
        using var solicitud = new SolicitudIdentidad(proveedor, cookie);
        Assert.True(await solicitud.AutenticarAsync());
        var anterior = await EstadoAsync(escenario);

        // Act
        var servicio = solicitud.Obtener<IServicioUsuarios>();
        var exito = operacion == "desactivar"
            ? (await servicio.DesactivarUsuarioAsync(objetivo)).Exito
            : (await servicio.RestablecerContrasenaAsync(objetivo)).Exito;

        // Assert
        Assert.False(exito);
        await ComprobarSinCambiosAsync(escenario, anterior);
    }


    // ──── FALLO DEL REGISTRO DE AUDITORÍA ──────────────────────────────────────────────────


    /// Si falla el evento de un alta, tampoco se conservan el usuario ni su rol.
    [Fact]
    public async Task Alta_Con_Fallo_De_Auditoria_Revierte_Usuario_Y_Rol()
    {
        // Arrange
        var escenario = await postgres.Crear();
        using var proveedor = escenario.Servicios();
        await CrearAsync(proveedor, "operador-b01", "Superusuario");
        var cookie = await EntrarAsync(proveedor, "operador-b01");
        using var solicitud = new SolicitudIdentidad(proveedor, cookie);
        Assert.True(await solicitud.AutenticarAsync());
        var anterior = await EstadoAsync(escenario);
        await PostgreSqlRevision.Sql(escenario.Migrador, """
            CREATE FUNCTION fallo_alta_b01() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN RAISE EXCEPTION 'Fallo ficticio de registro B01'; END $$;
            CREATE TRIGGER fallo_alta_b01 BEFORE INSERT ON "EventoAuditoria"
                FOR EACH ROW EXECUTE FUNCTION fallo_alta_b01();
            """);

        // Act + Assert
        await Assert.ThrowsAsync<DbUpdateException>(() => solicitud.Obtener<IServicioUsuarios>()
            .CrearUsuarioAsync("alta-ficticia-b01", "Usuaria", "Nombre Ficticio"));
        await ComprobarSinCambiosAsync(escenario, anterior);
    }
}
