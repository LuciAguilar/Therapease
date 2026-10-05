using Microsoft.EntityFrameworkCore;
using TherapEase.Application.Auditoria.Interfaces.Repositorios;
using TherapEase.Domain.Auditoria.Entidades;
using TherapEase.Domain.Auditoria.Entidades.Enums;
using TherapEase.Infrastructure.Data;

namespace TherapEase.Infrastructure.Auditoria.Repositorios;

// Agregar solo registra el evento en el contexto; se confirma junto con el cambio que lo origina.
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

    public async Task<IReadOnlyList<EventoAuditoria>> ConsultarAsync(
        DateTimeOffset desde,
        DateTimeOffset hasta,
        TipoRegistroAuditoria? tipoRegistro,
        Guid? idRegistro,
        int limite,
        CancellationToken cancelacion = default)
    {
        var consulta = _contexto.EventosAuditoria
            .AsNoTracking()
            .Where(e => e.FechaEvento >= desde && e.FechaEvento < hasta);

        if (tipoRegistro is { } tipo)
        {
            consulta = consulta.Where(e => e.TipoRegistro == tipo);
        }

        if (idRegistro is { } id)
        {
            consulta = consulta.Where(e => e.IdRegistro == id);
        }

        return await consulta.OrderByDescending(e => e.FechaEvento).Take(limite).ToListAsync(cancelacion);
    }
}
