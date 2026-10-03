using Microsoft.EntityFrameworkCore;
using TherapEase.Application.Citas;
using TherapEase.Domain.Citas;

namespace TherapEase.Infrastructure.Persistencia.Citas;

// Solo registra cambios en el contexto; la confirmación la hace IUnidadDeTrabajo.
public class RepositorioCitas : IRepositorioCitas
{
    private readonly ContextoDeDatos _contexto;

    public RepositorioCitas(ContextoDeDatos contexto)
    {
        _contexto = contexto;
    }

    public async Task AgregarAsync(Cita cita, CancellationToken cancelacion = default)
    {
        await _contexto.Citas.AddAsync(cita, cancelacion);
    }

    public Task<Cita?> ObtenerAsync(Guid idCita, CancellationToken cancelacion = default)
    {
        return _contexto.Citas.FirstOrDefaultAsync(c => c.IdCita == idCita, cancelacion);
    }
}
