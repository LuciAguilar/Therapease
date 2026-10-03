using TherapEase.Application.Auditoria;
using TherapEase.Domain.Auditoria;

namespace TherapEase.Infrastructure.Persistencia.Auditoria;

// Solo registra el evento en el contexto; se confirma junto con el cambio que lo origina.
public class RepositorioAuditoria : IRepositorioAuditoria
{
    private readonly ContextoDeDatos _contexto;

    public RepositorioAuditoria(ContextoDeDatos contexto)
    {
        _contexto = contexto;
    }

    public async Task AgregarAsync(EventoAuditoria evento, CancellationToken cancelacion = default)
    {
        await _contexto.EventosAuditoria.AddAsync(evento, cancelacion);
    }
}
