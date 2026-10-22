using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TherapEase.Application.Identidad.Constantes;
using TherapEase.Application.Identidad.Interfaces.Servicios;
using TherapEase.Application.Identidad.Modelos;
using TherapEase.Infrastructure.Identidad.Entidades;
using Microsoft.AspNetCore.Identity;
using static TherapEase.IntegrationTests.ServidorIdentidadRevision;

namespace TherapEase.IntegrationTests;

/// <summary>
/// Comprueba altas de usuarios, datos inválidos y cambios rechazados en una base de datos de prueba.
/// Cuando algo se rechaza, usuarios, roles y auditoría deben quedar sin ningún cambio.
/// </summary>
public class UsuariosServidorTests(PostgreSqlRevision postgres) : IClassFixture<PostgreSqlRevision>
{


    // ──── ALTAS Y DATOS INVÁLIDOS ──────────────────────────────────────────────────────────


    /// Un nombre vacío, muy largo o con caracteres no permitidos, o un rol desconocido, no crea nada.
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
        var cookie = await PrepararSuperusuarioAsync(proveedor);
        using var solicitud = new SolicitudIdentidad(proveedor, cookie);
        Assert.True(await solicitud.AutenticarAsync());
        var anterior = await EstadoAsync(escenario);

        // Act
        var respuesta = await solicitud.Obtener<IServicioUsuarios>().CrearUsuarioAsync(nombre!, rol!, "Usuario Ficticio");

        // Assert
        Assert.False(respuesta.Exito);
        Assert.Null(respuesta.Datos);
        Assert.Equal(MensajesDeIdentidad.DatosDeUsuarioInvalidos, respuesta.Mensaje);
        await ComprobarSinCambiosAsync(escenario, anterior);
    }

    /// Se puede crear un usuario con cualquiera de los dos roles y un nombre de 50 caracteres.
    /// Se quitan los espacios al inicio y al final, y se guarda una contraseña temporal que debe cambiarse.
    [Theory]
    [InlineData("Usuaria")]
    [InlineData("Superusuario")]
    public async Task Alta_Valida_Conserva_Nombre_Rol_Y_Temporal(string rol)
    {
        // Arrange
        var escenario = await postgres.Crear();
        using var proveedor = escenario.Servicios();
        var cookie = await PrepararSuperusuarioAsync(proveedor);
        using var solicitud = new SolicitudIdentidad(proveedor, cookie);
        Assert.True(await solicitud.AutenticarAsync());
        var nombre = new string('a', 50);

        // Act
        var respuesta = await solicitud.Obtener<IServicioUsuarios>().CrearUsuarioAsync($"  {nombre}  ", rol, "Usuario Ficticio");

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
        Assert.True(await ContrasenaFuncionaAsync(proveedor, datos.Usuario.IdUsuario, datos.ContrasenaTemporal));
        Assert.Equal(rol, Assert.Single(await usuarios.GetRolesAsync(guardado)));
        Assert.Equal(1L, await PostgreSqlRevision.Valor<long>(escenario.App,
            """SELECT count(*) FROM "EventoAuditoria" e"""));
    }

    /// Un nombre ya usado no se puede repetir, aunque ese usuario esté desactivado
    /// o se escriba con otras mayúsculas. El usuario existente no cambia.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Alta_Duplicada_Normalizada_No_Modifica_Usuario_Existente(bool inactiva)
    {
        // Arrange
        var escenario = await postgres.Crear();
        using var proveedor = escenario.Servicios();
        var objetivo = await CrearAsync(proveedor, "objetivo-ficticio");

        // Desactiva al usuario directo en la base de datos, solo para preparar la prueba.

        if (inactiva)
            await PostgreSqlRevision.Sql(escenario.App,
                $"""UPDATE "AspNetUsers" SET "Activo"=false WHERE "Id"='{objetivo}'""");
        var cookie = await PrepararSuperusuarioAsync(proveedor);
        using var solicitud = new SolicitudIdentidad(proveedor, cookie);
        Assert.True(await solicitud.AutenticarAsync());
        var anterior = await EstadoAsync(escenario);

        // Act
        var respuesta = await solicitud.Obtener<IServicioUsuarios>()
            .CrearUsuarioAsync("  OBJETIVO-FICTICIO  ", "Superusuario", "Usuario Ficticio");

        // Assert
        Assert.False(respuesta.Exito);
        Assert.Null(respuesta.Datos);
        Assert.Equal(MensajesDeIdentidad.NombreDeUsuarioDuplicado, respuesta.Mensaje);
        await ComprobarSinCambiosAsync(escenario, anterior);
    }


    // ──── OPERACIONES RECHAZADAS ───────────────────────────────────────────────────────────


    /// Un usuario que no existe no se puede desactivar, cambiar de rol ni recibir una contraseña temporal.
    [Theory]
    [InlineData("desactivar")]
    [InlineData("cambiar-rol")]
    [InlineData("restablecer")]
    public async Task Objetivo_Inexistente_No_Guarda_Cambios(string operacion)
    {
        // Arrange
        var escenario = await postgres.Crear();
        using var proveedor = escenario.Servicios();
        var cookie = await PrepararSuperusuarioAsync(proveedor);
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

    /// Un rol desconocido, o el mismo rol que ya tiene, se rechaza.
    /// No se cierran sus sesiones ni queda registro en la auditoría.
    [Theory]
    [InlineData("Capturista")]
    [InlineData("superusuario")]
    [InlineData("Usuaria")]
    public async Task Cambio_Rol_Invalido_O_Repetido_No_Escribe(string rol)
    {
        // Arrange
        var escenario = await postgres.Crear();
        using var proveedor = escenario.Servicios();
        var objetivo = await CrearAsync(proveedor, "objetivo-ficticio");
        var cookie = await PrepararSuperusuarioAsync(proveedor);
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

    /// Desactivar otra vez, o restablecer la contraseña de un usuario ya desactivado, no cambia nada.
    [Theory]
    [InlineData("desactivar")]
    [InlineData("restablecer")]
    public async Task Objetivo_Inactivo_No_Se_Modifica(string operacion)
    {
        // Arrange
        var escenario = await postgres.Crear();
        using var proveedor = escenario.Servicios();
        await CrearAsync(proveedor, "operador-ficticio", "Superusuario");
        var objetivo = await CrearAsync(proveedor, "objetivo-ficticio");
        var cookie = await EntrarAsync(proveedor, "operador-ficticio");
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


    /// Si la auditoría no se puede guardar al crear un usuario,
    /// tampoco se guardan el usuario ni su rol.
    [Fact]
    public async Task Alta_Con_Fallo_De_Auditoria_Revierte_Usuario_Y_Rol()
    {
        // Arrange
        var escenario = await postgres.Crear();
        using var proveedor = escenario.Servicios();
        var cookie = await PrepararSuperusuarioAsync(proveedor);
        using var solicitud = new SolicitudIdentidad(proveedor, cookie);
        Assert.True(await solicitud.AutenticarAsync());
        var anterior = await EstadoAsync(escenario);

        // Simula que la base de datos no puede guardar la auditoría.

        await SimularFalloDeAuditoriaAsync(escenario);

        // Act + Assert
        await Assert.ThrowsAsync<DbUpdateException>(() => solicitud.Obtener<IServicioUsuarios>()
            .CrearUsuarioAsync("usuario-nuevo-ficticio", "Usuaria", "Usuario Ficticio"));
        await ComprobarSinCambiosAsync(escenario, anterior);
    }
}
