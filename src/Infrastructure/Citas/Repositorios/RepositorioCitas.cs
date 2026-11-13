using Microsoft.EntityFrameworkCore;
using TherapEase.Application.Citas.Interfaces.Repositorios;
using TherapEase.Application.Citas.Modelos;
using TherapEase.Domain.Citas.Entidades;
using TherapEase.Domain.Citas.Entidades.Enums;
using TherapEase.Domain.Compartido.Entidades.Enums;
using TherapEase.Infrastructure.Data;

namespace TherapEase.Infrastructure.Citas.Repositorios;

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

    public Task<Cita?> ObtenerParaComparacionAsync(Guid idCita, CancellationToken cancelacion = default)
    {
        return _contexto.Citas
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.IdCita == idCita, cancelacion);
    }

    // Una sola consulta sin datos del paciente. La leyenda de registro tardío se calcula en la base con la misma comparación que ReglasDeCita.
    public async Task<IReadOnlyList<FilaDeCitaDeAgenda>> ConsultarAgendaAsync(
        DateTimeOffset desde, DateTimeOffset hasta, VistaDeAgenda vista, int limite, CancellationToken cancelacion = default)
    {
        // Las citas de baja no aparecen nunca; las que se solapan con el periodo cuentan aunque empiecen antes o terminen después.
        var consulta = _contexto.Citas
            .AsNoTracking()
            .Where(c => c.Condicion == CondicionRegistro.Vigente && c.Fin > desde && c.Inicio < hasta);

        consulta = vista == VistaDeAgenda.Activas
            ? consulta.Where(c => c.Estado == EstadoCita.Agendada)
            : consulta.Where(c => c.Estado == EstadoCita.Cancelada);

        return await consulta
            .OrderBy(c => c.Inicio)
            .ThenBy(c => c.IdCita)
            .Take(limite)
            .Select(c => new FilaDeCitaDeAgenda(c.IdCita, c.IdPaciente, c.Ambito, c.Estado, c.Inicio, c.Fin, c.FechaAlta > c.Inicio))
            .ToListAsync(cancelacion);
    }

    public Task<bool> ExistenActivasDePacienteAsync(Guid idPaciente, CancellationToken cancelacion = default)
    {
        return _contexto.Citas.AnyAsync(
            c => c.IdPaciente == idPaciente && c.Estado == EstadoCita.Agendada && c.Condicion == CondicionRegistro.Vigente,
            cancelacion);
    }
}
