using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TherapEase.Application.Identidad.Interfaces.Servicios;
using TherapEase.Domain.Auditoria.Entidades.Enums;
using static TherapEase.IntegrationTests.ServidorIdentidadRevision;

namespace TherapEase.IntegrationTests;

/// <summary>
/// Comprueba lo que queda en la auditoría al crear usuarios, cambiar roles,
/// desactivar y cambiar contraseñas: quién lo hizo, a quién, cuándo y qué datos cambió.
/// También comprueba que no se guarden contraseñas.
/// </summary>
public class AuditoriaServidorTests(PostgreSqlRevision postgres) : IClassFixture<PostgreSqlRevision>
{


    // ──── AUDITORÍA DE CAMBIOS DE USUARIOS ─────────────────────────────────────────────────


    /// Cada cambio guardado deja un solo registro en la auditoría, con quién lo hizo y a quién afectó.
    /// Ni la auditoría ni los mensajes del sistema muestran contraseñas, cookies o datos de conexión.
    [Theory]
    [InlineData("crear", "Alta", "NombreUsuario,Rol,DebeCambiarContrasena,NombreCompleto")]
    [InlineData("cambiar-rol", "Actualizacion", "Rol")]
    [InlineData("desactivar", "Desactivacion", "Activo")]
    [InlineData("restablecer", "ReinicioContrasena", "Contrasena")]
    [InlineData("cambiar-propia", "CambioContrasena", "Contrasena")]
    [InlineData("cambiar-temporal", "CambioContrasena", "Contrasena")]
    public async Task Cambio_Confirmado_Guarda_Evento_Completo_Sin_Secretos(
        string operacion, string accion, string campos)
    {
        // Arrange
        var escenario = await postgres.Crear();
        using var registro = new RegistroRevision();
        using var proveedor = escenario.Servicios(servicios =>
            servicios.AddLogging(opciones => opciones.AddProvider(registro)));
        var actor = await CrearAsync(proveedor, "operador-ficticio", "Superusuario");
        var objetivo = operacion == "crear" ? Guid.Empty
            : await CrearAsync(proveedor, "objetivo-ficticio", temporal: operacion == "cambiar-temporal");
        var propia = operacion is "cambiar-propia" or "cambiar-temporal";
        var cookie = await EntrarAsync(proveedor, propia ? "objetivo-ficticio" : "operador-ficticio");

        // Valores que no deben aparecer en la auditoría ni en los mensajes del sistema.

        var secretos = new List<string> { Contrasena, NuevaContrasena, cookie.Split('=', 2)[1], escenario.App };
        if (objetivo != Guid.Empty)
            secretos.Add(await PostgreSqlRevision.Valor<string>(escenario.App,
                $"""SELECT "PasswordHash" FROM "AspNetUsers" WHERE "Id"='{objetivo}'"""));
        using var solicitud = new SolicitudIdentidad(proveedor, cookie);
        Assert.True(await solicitud.AutenticarAsync());
        var antes = DateTimeOffset.UtcNow;

        // Act + Assert: cada cambio debe responder que se guardó.
        var servicio = solicitud.Obtener<IServicioUsuarios>();
        switch (operacion)
        {
            case "crear":
                var alta = await servicio.CrearUsuarioAsync("objetivo-ficticio", "Usuaria", "Usuario Ficticio");
                Assert.True(alta.Exito);
                Assert.NotNull(alta.Datos);
                objetivo = alta.Datos.Usuario.IdUsuario;
                secretos.Add(alta.Datos.ContrasenaTemporal);
                break;
            case "cambiar-rol":
                Assert.True((await servicio.CambiarRolAsync(objetivo, "Superusuario")).Exito);
                break;
            case "desactivar":
                Assert.True((await servicio.DesactivarUsuarioAsync(objetivo)).Exito);
                break;
            case "restablecer":
                var cambio = await servicio.RestablecerContrasenaAsync(objetivo);
                Assert.True(cambio.Exito);
                Assert.NotNull(cambio.Datos);
                secretos.Add(cambio.Datos.ContrasenaTemporal);
                break;
            default:
                Assert.True((await solicitud.Obtener<IServicioContrasena>()
                    .CambiarContrasenaPropiaAsync(propia && operacion == "cambiar-propia" ? Contrasena : null,
                        NuevaContrasena)).Exito);
                break;
        }
        var despues = DateTimeOffset.UtcNow;

        // Assert
        await using var lectura = escenario.Contexto();
        var evento = Assert.Single(await lectura.EventosAuditoria.AsNoTracking().SoloCambios().ToListAsync());
        Assert.NotEqual(Guid.Empty, evento.IdEvento);
        Assert.Equal(propia ? objetivo : actor, evento.IdUsuarioActor);
        Assert.Equal(objetivo, evento.IdRegistro);
        Assert.Equal(TipoRegistroAuditoria.Usuario, evento.TipoRegistro);
        Assert.Equal(accion, evento.Accion.ToString());
        Assert.Equal(campos.Split(','), evento.CamposAfectados);
        Assert.InRange(evento.FechaEvento, antes, despues);
        Assert.False(string.IsNullOrWhiteSpace(evento.Hecho));
        var auditoria = await PostgreSqlRevision.Valor<string>(escenario.App,
            """SELECT json_agg(e)::text FROM "EventoAuditoria" e""");
        secretos.Add(await PostgreSqlRevision.Valor<string>(escenario.App,
            $"""SELECT "PasswordHash" FROM "AspNetUsers" WHERE "Id"='{objetivo}'"""));
        foreach (var secreto in secretos)
        {
            Assert.False(auditoria.Contains(secreto, StringComparison.Ordinal),
                "El evento contiene un valor privado de la prueba.");
            Assert.False(registro.Texto.Contains(secreto, StringComparison.Ordinal),
                "Los registros contienen un valor privado de la prueba.");
        }
    }
}
