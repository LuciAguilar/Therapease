using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TherapEase.Application.Citas.Constantes;
using TherapEase.Application.Citas.Interfaces.Servicios;
using TherapEase.Application.Citas.Modelos;
using TherapEase.Application.Compartido.Modelos;
using TherapEase.Application.Coordinadores.Interfaces.Servicios;
using TherapEase.Application.Pacientes.Interfaces.Servicios;
using TherapEase.Domain.Auditoria.Entidades.Enums;
using TherapEase.Domain.Citas.Entidades;
using TherapEase.Domain.Citas.Entidades.Enums;
using TherapEase.Domain.Pacientes.Entidades.Enums;

namespace TherapEase.IntegrationTests;

/// <summary>
/// Comprueba el pago con solicitudes y conexiones independientes, en momentos controlados.
/// Distingue lo confirmado en PostgreSQL de la respuesta que recibe la persona.
/// </summary>
public sealed partial class PagoDeCitasRevisionTests(PostgreSqlRevision postgres) : IClassFixture<PostgreSqlRevision>
{


    // ──── PREPARACIÓN Y LECTURA INDEPENDIENTE ──────────────────────────────────────────────


    private static DatosDeCitaEntrada Datos(Guid paciente) =>
        new(paciente, AmbitoAtencion.Escolar, new DateOnly(2026, 12, 2), new TimeOnly(10, 0), new TimeOnly(11, 0));

    /// Agenda con el servicio real; las consultas posteriores usan conexiones distintas.
    private static async Task<(Guid Paciente, Guid Cita)> PrepararAsync(PacientesRevision e)
    {
        var paciente = await e.RegistrarAsync();
        var cita = Guid.NewGuid();
        var alta = await e.EnSolicitudAsync(s => s.Obtener<IServicioCitas>().AgendarAsync(cita, Datos(paciente)));
        Assert.True(alta.Exito, "Debe prepararse la cita ficticia.");
        return (paciente, cita);
    }

    private static Task<RespuestaServicio<ResultadoDeCambioDePago>> CambiarAsync(
        PacientesRevision e, Guid cita, int version, EstadoPago pago) =>
        e.EnSolicitudAsync(s => s.Obtener<IServicioCitas>().CambiarPagoAsync(cita, version, pago));

    private static async Task<Cita> LeerAsync(PacientesRevision e, Guid cita)
    {
        await using var db = e.Escenario.Contexto();
        return await db.Citas.AsNoTracking().SingleAsync(c => c.IdCita == cita);
    }

    /// Cuenta solo cambios de pago, separados de las altas y de los eventos de sesión.
    private static async Task<int> EventosAsync(PacientesRevision e, Guid cita)
    {
        await using var db = e.Escenario.Contexto();
        var eventos = await db.EventosAuditoria.AsNoTracking().Where(v => v.IdRegistro == cita
            && v.TipoRegistro == TipoRegistroAuditoria.Cita && v.Accion == AccionAuditoria.Actualizacion).ToListAsync();
        Assert.All(eventos, v =>
        {
            Assert.Equal(e.Actor, v.IdUsuarioActor);
            Assert.Equal(["EstadoPago"], v.CamposAfectados);
            Assert.Equal("Se cambió el estado de pago de una cita.", v.Hecho);
        });
        return eventos.Count;
    }

    /// La respuesta del cambio no expone el paciente, su nombre, su ámbito ni su horario.
    private static void ExigirRespuestaMinima(RespuestaServicio<ResultadoDeCambioDePago> respuesta, Guid paciente)
    {
        Assert.NotNull(respuesta.Datos);
        var texto = JsonSerializer.Serialize(respuesta.Datos);
        Assert.False(texto.Contains(paciente.ToString(), StringComparison.OrdinalIgnoreCase), "La respuesta no debe incluir al paciente.");
        foreach (var campo in new[] { "IdPaciente", "Nombre", "Inicio", "Fin", "Ambito", "Cookie", "Contrasena" })
            Assert.False(texto.Contains(campo, StringComparison.OrdinalIgnoreCase), "La respuesta contiene un campo no previsto.");
    }


    // ──── ROLES Y CONTEXTO DEL PACIENTE ────────────────────────────────────────────────────


    /// Ambos roles ficticios cambian pago con su actor y el reloj del servidor, sin revelar al paciente.
    [Theory]
    [InlineData("Usuaria")]
    [InlineData("Superusuario")]
    public async Task AmbosRolesConfirmanConSuActorYHoraDelServidor(string rol)
    {
        // Arrange
        using var e = await PacientesRevision.CrearAsync(postgres, rol);
        var (paciente, cita) = await PrepararAsync(e);
        e.Reloj.Avanzar(TimeSpan.FromMinutes(1));
        var ahora = e.Reloj.GetUtcNow();

        // Act
        var respuesta = await CambiarAsync(e, cita, 1, EstadoPago.Pagado);

        // Assert
        Assert.True(respuesta.Exito);
        ExigirRespuestaMinima(respuesta, paciente);
        var guardada = await LeerAsync(e, cita);
        Assert.Equal(ahora, guardada.FechaActualizacion);
        Assert.Equal(e.Actor, guardada.IdUsuarioActualizacion);
        Assert.Equal(1, await EventosAsync(e, cita));
        await using var db = e.Escenario.Contexto();
        Assert.Equal(ahora, (await db.EventosAuditoria.SingleAsync(v => v.IdRegistro == cita
            && v.Accion == AccionAuditoria.Actualizacion)).FechaEvento);
    }

    /// Retirar el ámbito o dar de baja al paciente no inventa una prohibición nueva sobre el pago de una cita vigente.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PacienteDeBajaOAmbitoRetiradoNoImpidenElPagoPermitido(bool pacienteDeBaja)
    {
        // Arrange
        using var e = await PacientesRevision.CrearAsync(postgres);
        var (paciente, cita) = await PrepararAsync(e);
        var version = 1;
        if (pacienteDeBaja)
        {
            await e.SqlAsync("UPDATE \"Cita\" SET \"Estado\"='Cancelada', \"Version\"=2 WHERE \"IdCita\"=@cita", ("cita", cita));
            var baja = await e.EnSolicitudAsync(s => s.Obtener<ICoordinadorDeBajaDePaciente>().DarDeBajaAsync(paciente, 1));
            Assert.True(baja.Exito);
            version = 2;
        }
        else
        {
            var retiro = await e.ServicioAsync(s => s.ActualizarAsync(paciente, 1,
                PacientesRevision.Adulto() with { Ambitos = [AmbitoAtencion.Independiente] }));
            Assert.True(retiro.Exito);
        }

        // Act
        var respuesta = await CambiarAsync(e, cita, version, EstadoPago.Pagado);

        // Assert
        Assert.True(respuesta.Exito);
        ExigirRespuestaMinima(respuesta, paciente);
        var guardada = await LeerAsync(e, cita);
        Assert.Equal((EstadoPago.Pagado, version + 1, AmbitoAtencion.Escolar),
            (guardada.EstadoPago, guardada.Version, guardada.Ambito));
        Assert.Equal(1, await EventosAsync(e, cita));
    }
}
