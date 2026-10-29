using TherapEase.Application.Pacientes.Interfaces.Repositorios;
using TherapEase.Domain.Pacientes.Entidades;

namespace TherapEase.UnitTests.Dobles;

internal sealed class RepositorioPacientesFalso : IRepositorioPacientes
{
    public Dictionary<Guid, Paciente> Pacientes { get; } = [];

    public Task AgregarAsync(Paciente paciente, CancellationToken cancelacion = default)
    {
        Pacientes[paciente.IdPaciente] = paciente;
        return Task.CompletedTask;
    }

    public Task<Paciente?> ObtenerAsync(Guid idPaciente, CancellationToken cancelacion = default) =>
        Task.FromResult(Pacientes.GetValueOrDefault(idPaciente));
}
