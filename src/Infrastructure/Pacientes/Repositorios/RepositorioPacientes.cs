using Microsoft.EntityFrameworkCore;
using TherapEase.Application.Pacientes.Interfaces.Repositorios;
using TherapEase.Application.Pacientes.Modelos;
using TherapEase.Domain.Compartido.Entidades.Enums;
using TherapEase.Domain.Pacientes.Entidades;
using TherapEase.Domain.Pacientes.Entidades.Enums;
using TherapEase.Infrastructure.Data;

namespace TherapEase.Infrastructure.Pacientes.Repositorios;

// Solo registra cambios en el contexto; la confirmación la hace IUnidadDeTrabajo. Las lecturas para mostrar no rastrean entidades.
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

    public Task<Paciente?> ObtenerParaComparacionAsync(Guid idPaciente, CancellationToken cancelacion = default)
    {
        return _contexto.Pacientes
            .AsNoTracking()
            .Include(p => p.Ambitos)
            .FirstOrDefaultAsync(p => p.IdPaciente == idPaciente, cancelacion);
    }

    // Una sola instrucción SQL: el contenido se proyecta con CASE solo para una ficha vigente, de modo que de una baja la base no entrega nada más que su condición.
    public Task<LecturaDePaciente?> ObtenerParaConsultaAsync(Guid idPaciente, CancellationToken cancelacion = default)
    {
        return _contexto.Pacientes
            .AsNoTracking()
            .Where(p => p.IdPaciente == idPaciente)
            .Select(p => new LecturaDePaciente(
                p.IdPaciente,
                p.Condicion,
                p.Version,
                p.Condicion == CondicionRegistro.Vigente ? p.Nombre : null,
                p.Condicion == CondicionRegistro.Vigente ? p.FechaNacimiento : null,
                p.Condicion == CondicionRegistro.Vigente ? p.Telefono : null,
                p.Condicion == CondicionRegistro.Vigente ? p.Correo : null,
                p.Condicion == CondicionRegistro.Vigente ? p.Escuela : null,
                p.Condicion == CondicionRegistro.Vigente ? p.Grupo : null,
                p.Condicion == CondicionRegistro.Vigente ? p.TutorNombre : null,
                p.Condicion == CondicionRegistro.Vigente ? p.TutorParentesco : null,
                p.Condicion == CondicionRegistro.Vigente ? p.TutorTelefono : null,
                p.Condicion == CondicionRegistro.Vigente ? p.TutorCorreo : null,
                p.Condicion == CondicionRegistro.Vigente && p.Ambitos.Any(a => a.Ambito == AmbitoAtencion.Escolar),
                p.Condicion == CondicionRegistro.Vigente && p.Ambitos.Any(a => a.Ambito == AmbitoAtencion.Independiente)))
            .FirstOrDefaultAsync(cancelacion);
    }

    public Task<CondicionRegistro?> ObtenerCondicionAsync(Guid idPaciente, CancellationToken cancelacion = default)
    {
        return _contexto.Pacientes
            .AsNoTracking()
            .Where(p => p.IdPaciente == idPaciente)
            .Select(p => (CondicionRegistro?)p.Condicion)
            .FirstOrDefaultAsync(cancelacion);
    }

    public async Task<IReadOnlyList<FilaDeBusquedaDePaciente>> BuscarVigentesAsync(
        IReadOnlyList<string> palabras, DateOnly? nacimiento, int limite, CancellationToken cancelacion = default)
    {
        var consulta = _contexto.Pacientes.AsNoTracking().Where(p => p.Condicion == CondicionRegistro.Vigente);

        // Contains con parámetro se traduce a strpos: las palabras buscadas no son patrones y %, _ o \ no tienen significado especial.
        foreach (var palabra in palabras)
        {
            var buscada = palabra;
            consulta = consulta.Where(p => p.NombreNormalizado.Contains(buscada));
        }

        if (nacimiento is { } fecha)
        {
            consulta = consulta.Where(p => p.FechaNacimiento == fecha);
        }

        return await consulta
            .OrderBy(p => p.NombreNormalizado)
            .ThenBy(p => p.IdPaciente)
            .Take(limite)
            .Select(p => new FilaDeBusquedaDePaciente(
                p.IdPaciente,
                p.Nombre,
                p.FechaNacimiento,
                _contexto.Pacientes.Any(o =>
                    o.IdPaciente != p.IdPaciente
                    && o.Condicion == CondicionRegistro.Vigente
                    && o.NombreNormalizado == p.NombreNormalizado)))
            .ToListAsync(cancelacion);
    }

    public async Task<IReadOnlyList<ParecidoDePaciente>> BuscarParecidosAsync(
        string nombreNormalizado, int limite, CancellationToken cancelacion = default)
    {
        return await _contexto.Pacientes
            .AsNoTracking()
            .Where(p => p.Condicion == CondicionRegistro.Vigente && p.NombreNormalizado == nombreNormalizado)
            .OrderBy(p => p.NombreNormalizado)
            .ThenBy(p => p.IdPaciente)
            .Take(limite)
            .Select(p => new ParecidoDePaciente(p.IdPaciente, p.Nombre, p.FechaNacimiento))
            .ToListAsync(cancelacion);
    }

    // Se marcan para borrar sin detección intermedia de cambios: la clave de PacienteAmbito incluye a Paciente y, si EF viera primero el ámbito fuera de la
    // colección, lo leería como una relación obligatoria rota y fallaría con DeleteBehavior.Restrict.
    public void QuitarAmbitos(IEnumerable<PacienteAmbito> ambitos)
    {
        var seguimiento = _contexto.ChangeTracker;
        var detectabaCambios = seguimiento.AutoDetectChangesEnabled;
        seguimiento.AutoDetectChangesEnabled = false;
        try
        {
            foreach (var ambito in ambitos)
            {
                _contexto.Set<PacienteAmbito>().Remove(ambito);
            }
        }
        finally
        {
            seguimiento.AutoDetectChangesEnabled = detectabaCambios;
        }
    }
}
