using TherapEase.Domain.Auditoria;

namespace TherapEase.Application.Auditoria;

public interface IRepositorioAuditoria
{
    Task AgregarAsync(EventoAuditoria evento, CancellationToken cancelacion = default);
}
