using TherapEase.Domain.Citas.Entidades;

namespace TherapEase.Application.Citas.Interfaces.Repositorios;

public interface IRepositorioCitas
{
    Task AgregarAsync(Cita cita, CancellationToken cancelacion = default);

    Task<Cita?> ObtenerAsync(Guid idCita, CancellationToken cancelacion = default);

    // Activa = agendada y vigente; solo esas citas impiden la baja del paciente.
    Task<bool> ExistenActivasDePacienteAsync(Guid idPaciente, CancellationToken cancelacion = default);
}
