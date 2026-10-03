using TherapEase.Domain.Citas;

namespace TherapEase.Application.Citas;

public interface IRepositorioCitas
{
    Task AgregarAsync(Cita cita, CancellationToken cancelacion = default);

    Task<Cita?> ObtenerAsync(Guid idCita, CancellationToken cancelacion = default);
}
