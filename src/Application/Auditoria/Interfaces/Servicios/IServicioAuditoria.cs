using TherapEase.Application.Auditoria.Modelos;
using TherapEase.Application.Compartido.Modelos;
using TherapEase.Domain.Auditoria.Entidades.Enums;

namespace TherapEase.Application.Auditoria.Interfaces.Servicios;

public interface IServicioAuditoria
{
    // Añade el evento al guardado en curso: debe llamarse dentro de IUnidadDeTrabajo para confirmarse junto al cambio.
    Task RegistrarHechoDeCambioAsync(
        Guid idActor,
        TipoRegistroAuditoria tipoRegistro,
        Guid idRegistro,
        AccionAuditoria accion,
        IEnumerable<string> camposAfectados,
        string hecho,
        CancellationToken cancelacion = default);

    // Consulta técnica de solo lectura aprobada para B00: exige permiso vigente de superusuario activo.
    Task<RespuestaServicio<IReadOnlyList<EventoConsultado>>> ConsultarEventosAutorizadosAsync(
        FiltroDeEventos filtro,
        CancellationToken cancelacion = default);
}
