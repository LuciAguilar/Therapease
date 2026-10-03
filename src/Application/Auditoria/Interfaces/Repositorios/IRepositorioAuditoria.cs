using TherapEase.Domain.Auditoria.Entidades;
using TherapEase.Domain.Auditoria.Entidades.Enums;

namespace TherapEase.Application.Auditoria.Interfaces.Repositorios;

public interface IRepositorioAuditoria
{
    Task AgregarAsync(EventoAuditoria evento, CancellationToken cancelacion = default);

    // Solo lectura, más reciente primero; el periodo es [desde, hasta).
    Task<IReadOnlyList<EventoAuditoria>> ConsultarAsync(
        DateTimeOffset desde,
        DateTimeOffset hasta,
        TipoRegistroAuditoria? tipoRegistro,
        Guid? idRegistro,
        int limite,
        CancellationToken cancelacion = default);
}
