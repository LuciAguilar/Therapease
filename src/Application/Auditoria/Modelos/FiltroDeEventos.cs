using TherapEase.Domain.Auditoria.Entidades.Enums;

namespace TherapEase.Application.Auditoria.Modelos;

public sealed record FiltroDeEventos(
    DateTimeOffset Desde,
    DateTimeOffset Hasta,
    TipoRegistroAuditoria? TipoRegistro = null,
    Guid? IdRegistro = null,
    int Limite = FiltroDeEventos.LimitePorDefecto)
{
    public const int LimitePorDefecto = 100;
    public const int LimiteMaximo = 500;
}
