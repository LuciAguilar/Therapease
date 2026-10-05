using Microsoft.EntityFrameworkCore;
using TherapEase.Application.Pacientes.Interfaces.Repositorios;
using TherapEase.Domain.Pacientes.Entidades;
using TherapEase.Infrastructure.Data;

namespace TherapEase.Infrastructure.Pacientes.Repositorios;

// Solo registra cambios en el contexto; la confirmación la hace IUnidadDeTrabajo.
public class RepositorioPacientes : IRepositorioPacientes
{
    private readonly ContextoDeDatos _contexto;

    public RepositorioPacientes(ContextoDeDatos contexto)
    {
        _contexto = contexto;
    }

    public async Task AgregarAsync(Paciente paciente, CancellationToken cancelacion = default)
    {
        await _contexto.Pacientes.AddAsync(paciente, cancelacion);
    }

    public Task<Paciente?> ObtenerAsync(Guid idPaciente, CancellationToken cancelacion = default)
    {
        return _contexto.Pacientes
            .Include(p => p.Ambitos)
            .FirstOrDefaultAsync(p => p.IdPaciente == idPaciente, cancelacion);
    }
}
