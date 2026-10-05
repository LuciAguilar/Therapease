using TherapEase.Domain.Auditoria.Entidades.Enums;

namespace TherapEase.Domain.Auditoria.Entidades;

// Solo datos del evento (nombres de campos, nunca valores). Se crea con ReglasDeEventoAuditoria.
public sealed class EventoAuditoria
{
    public const int LongitudMaximaHecho = 300;

    internal EventoAuditoria()
    {
    }

    public Guid IdEvento { get; internal set; }

    public DateTimeOffset FechaEvento { get; internal set; }

    public Guid IdUsuarioActor { get; internal set; }

    public TipoRegistroAuditoria TipoRegistro { get; internal set; }

    public Guid IdRegistro { get; internal set; }

    public AccionAuditoria Accion { get; internal set; }

    public IReadOnlyList<string> CamposAfectados { get; internal set; } = [];

    public string Hecho { get; internal set; } = string.Empty;
}
