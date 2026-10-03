using TherapEase.Application.Auditoria.Interfaces.Repositorios;
using TherapEase.Domain.Auditoria.Entidades;
using TherapEase.Domain.Auditoria.Entidades.Enums;

namespace TherapEase.UnitTests.Dobles;

internal sealed class RepositorioAuditoriaFalso : IRepositorioAuditoria
{
    public List<EventoAuditoria> Eventos { get; } = [];

    public Exception? ExcepcionAlAgregar { get; set; }

    public int Consultas { get; private set; }

    public (DateTimeOffset Desde, DateTimeOffset Hasta, int Limite)? UltimaConsulta { get; private set; }

    public Task AgregarAsync(EventoAuditoria evento, CancellationToken cancelacion = default)
    {
        if (ExcepcionAlAgregar is not null)
        {
            throw ExcepcionAlAgregar;
        }

        Eventos.Add(evento);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<EventoAuditoria>> ConsultarAsync(
        DateTimeOffset desde, DateTimeOffset hasta, TipoRegistroAuditoria? tipoRegistro, Guid? idRegistro, int limite, CancellationToken cancelacion = default)
    {
        Consultas++;
        UltimaConsulta = (desde, hasta, limite);
        IReadOnlyList<EventoAuditoria> resultado = Eventos
            .Where(e => e.FechaEvento >= desde && e.FechaEvento < hasta)
            .Where(e => tipoRegistro is null || e.TipoRegistro == tipoRegistro)
            .Where(e => idRegistro is null || e.IdRegistro == idRegistro)
            .Take(limite)
            .ToList();
        return Task.FromResult(resultado);
    }
}
