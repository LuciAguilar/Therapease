using TherapEase.Domain.Pacientes;

namespace TherapEase.Application.Pacientes;

public interface IRepositorioPacientes
{
    Task AgregarAsync(Paciente paciente, CancellationToken cancelacion = default);

    Task<Paciente?> ObtenerAsync(Guid idPaciente, CancellationToken cancelacion = default);
}
