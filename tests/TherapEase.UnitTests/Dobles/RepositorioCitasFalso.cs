using TherapEase.Application.Citas.Interfaces.Repositorios;
using TherapEase.Domain.Citas.Entidades;

namespace TherapEase.UnitTests.Dobles;

internal sealed class RepositorioCitasFalso : IRepositorioCitas
{
    public Dictionary<Guid, Cita> Citas { get; } = [];

    public Task AgregarAsync(Cita cita, CancellationToken cancelacion = default)
    {
        Citas[cita.IdCita] = cita;
        return Task.CompletedTask;
    }

    public Task<Cita?> ObtenerAsync(Guid idCita, CancellationToken cancelacion = default) =>
        Task.FromResult(Citas.GetValueOrDefault(idCita));

    public Task<bool> ExistenActivasDePacienteAsync(Guid idPaciente, CancellationToken cancelacion = default) =>
        Task.FromResult(Citas.Values.Any(c => c.IdPaciente == idPaciente && c.EsActiva));
}
