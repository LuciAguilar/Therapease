using TherapEase.Domain.Pacientes.Entidades;

namespace TherapEase.Application.Pacientes.Interfaces.Repositorios;

public interface IRepositorioPacientes
{
    Task AgregarAsync(Paciente paciente, CancellationToken cancelacion = default);

    Task<Paciente?> ObtenerAsync(Guid idPaciente, CancellationToken cancelacion = default);
}
