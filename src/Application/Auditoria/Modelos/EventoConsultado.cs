using TherapEase.Domain.Auditoria.Entidades.Enums;

namespace TherapEase.Application.Auditoria.Modelos;

// Solo metadatos del evento: actor, momento, registro y campos afectados, acción y hecho; nunca valores privados.
public sealed record EventoConsultado(
    Guid IdEvento,
    DateTimeOffset FechaEvento,
    Guid IdUsuarioActor,
    TipoRegistroAuditoria TipoRegistro,
    Guid IdRegistro,
    AccionAuditoria Accion,
    IReadOnlyList<string> CamposAfectados,
    string Hecho);
