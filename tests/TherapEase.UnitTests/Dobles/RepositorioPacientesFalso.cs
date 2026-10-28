using TherapEase.Application.Pacientes.Interfaces.Repositorios;
using TherapEase.Application.Pacientes.Modelos;
using TherapEase.Domain.Compartido.Entidades.Enums;
using TherapEase.Domain.Pacientes.Entidades;
using TherapEase.Domain.Pacientes.Entidades.Enums;

namespace TherapEase.UnitTests.Dobles;

internal sealed class RepositorioPacientesFalso : IRepositorioPacientes
{
    public Dictionary<Guid, Paciente> Pacientes { get; } = [];

    // Para simular una carrera: se ejecuta justo antes de guardar el alta y puede insertar a la competidora o lanzar la excepción de la base.
    public Action<Paciente>? AlAgregar { get; set; }

    public int LecturasParaEscribir { get; private set; }

    public int LecturasParaComparacion { get; private set; }

    public int LecturasParaConsulta { get; private set; }

    public int LecturasDeCondicion { get; private set; }

    public int BusquedasDeVigentes { get; private set; }

    public int BusquedasDeParecidos { get; private set; }

    public List<PacienteAmbito> AmbitosQuitados { get; } = [];

    public (IReadOnlyList<string> Palabras, DateOnly? Nacimiento, int Limite)? UltimaBusqueda { get; private set; }

    public int Lecturas => LecturasParaEscribir + LecturasParaComparacion + LecturasParaConsulta + LecturasDeCondicion + BusquedasDeVigentes + BusquedasDeParecidos;

    public Task AgregarAsync(Paciente paciente, CancellationToken cancelacion = default)
    {
        AlAgregar?.Invoke(paciente);
        Pacientes[paciente.IdPaciente] = paciente;
        return Task.CompletedTask;
    }

    public Task<Paciente?> ObtenerAsync(Guid idPaciente, CancellationToken cancelacion = default)
    {
        LecturasParaEscribir++;
        return Task.FromResult(Pacientes.GetValueOrDefault(idPaciente));
    }

    public Task<Paciente?> ObtenerParaComparacionAsync(Guid idPaciente, CancellationToken cancelacion = default)
    {
        LecturasParaComparacion++;
        return Task.FromResult(Pacientes.GetValueOrDefault(idPaciente));
    }

    public Task<LecturaDePaciente?> ObtenerParaConsultaAsync(Guid idPaciente, CancellationToken cancelacion = default)
    {
        LecturasParaConsulta++;
        if (!Pacientes.TryGetValue(idPaciente, out var p))
        {
            return Task.FromResult<LecturaDePaciente?>(null);
        }

        // Igual que la proyección real: de una baja solo se entrega la condición.
        var vigente = p.Condicion == CondicionRegistro.Vigente;
        LecturaDePaciente lectura = new(
            p.IdPaciente, p.Condicion, p.Version,
            vigente ? p.Nombre : null, vigente ? p.FechaNacimiento : null, vigente ? p.Telefono : null, vigente ? p.Correo : null,
            vigente ? p.Escuela : null, vigente ? p.Grupo : null,
            vigente ? p.TutorNombre : null, vigente ? p.TutorParentesco : null, vigente ? p.TutorTelefono : null, vigente ? p.TutorCorreo : null,
            vigente && p.Ambitos.Any(a => a.Ambito == AmbitoAtencion.Escolar),
            vigente && p.Ambitos.Any(a => a.Ambito == AmbitoAtencion.Independiente));
        return Task.FromResult<LecturaDePaciente?>(lectura);
    }

    public Task<CondicionRegistro?> ObtenerCondicionAsync(Guid idPaciente, CancellationToken cancelacion = default)
    {
        LecturasDeCondicion++;
        return Task.FromResult<CondicionRegistro?>(Pacientes.TryGetValue(idPaciente, out var p) ? p.Condicion : null);
    }

    public Task<IReadOnlyList<FilaDeBusquedaDePaciente>> BuscarVigentesAsync(
        IReadOnlyList<string> palabras, DateOnly? nacimiento, int limite, CancellationToken cancelacion = default)
    {
        BusquedasDeVigentes++;
        UltimaBusqueda = (palabras, nacimiento, limite);
        var vigentes = Pacientes.Values.Where(p => p.Condicion == CondicionRegistro.Vigente).ToList();
        IReadOnlyList<FilaDeBusquedaDePaciente> filas = vigentes
            .Where(p => palabras.All(w => p.NombreNormalizado.Contains(w, StringComparison.Ordinal)))
            .Where(p => nacimiento is null || p.FechaNacimiento == nacimiento)
            .OrderBy(p => p.NombreNormalizado, StringComparer.Ordinal)
            .ThenBy(p => p.IdPaciente)
            .Take(limite)
            .Select(p => new FilaDeBusquedaDePaciente(
                p.IdPaciente, p.Nombre, p.FechaNacimiento,
                vigentes.Any(o => o.IdPaciente != p.IdPaciente && o.NombreNormalizado == p.NombreNormalizado)))
            .ToList();
        return Task.FromResult(filas);
    }

    public Task<IReadOnlyList<ParecidoDePaciente>> BuscarParecidosAsync(
        string nombreNormalizado, int limite, CancellationToken cancelacion = default)
    {
        BusquedasDeParecidos++;
        IReadOnlyList<ParecidoDePaciente> parecidos = Pacientes.Values
            .Where(p => p.Condicion == CondicionRegistro.Vigente && p.NombreNormalizado == nombreNormalizado)
            .OrderBy(p => p.NombreNormalizado, StringComparer.Ordinal)
            .ThenBy(p => p.IdPaciente)
            .Take(limite)
            .Select(p => new ParecidoDePaciente(p.IdPaciente, p.Nombre, p.FechaNacimiento))
            .ToList();
        return Task.FromResult(parecidos);
    }

    public void QuitarAmbitos(IEnumerable<PacienteAmbito> ambitos) => AmbitosQuitados.AddRange(ambitos);
}
