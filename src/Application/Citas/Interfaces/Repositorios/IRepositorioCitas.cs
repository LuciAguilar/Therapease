using TherapEase.Domain.Citas.Entidades;

namespace TherapEase.Application.Citas.Interfaces.Repositorios;

public interface IRepositorioCitas
{
    Task AgregarAsync(Cita cita, CancellationToken cancelacion = default);

    Task<Cita?> ObtenerAsync(Guid idCita, CancellationToken cancelacion = default);
}
