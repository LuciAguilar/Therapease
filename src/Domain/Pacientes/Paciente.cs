using TherapEase.Domain.Compartido;

namespace TherapEase.Domain.Pacientes;

public class Paciente : EntidadAuditable
{
    public const int LongitudMaximaNombre = 200;
    public const int LongitudMaximaContacto = 200;

    private readonly List<PacienteAmbito> _ambitos = [];

    private Paciente()
    {
    }

    public Guid IdPaciente { get; private set; }

    public string Nombre { get; private set; } = string.Empty;

    public string? Contacto { get; private set; }

    public CondicionRegistro Condicion { get; private set; }

    public int Version { get; private set; }

    public IReadOnlyCollection<PacienteAmbito> Ambitos => _ambitos;

    public static Paciente Registrar(
        Guid idPaciente,
        string nombre,
        string? contacto,
        IEnumerable<AmbitoAtencion> ambitos,
        Guid idActor,
        DateTimeOffset ahora)
    {
        ArgumentNullException.ThrowIfNull(ambitos);

        if (idPaciente == Guid.Empty)
        {
            throw new ReglaDeNegocioException("El paciente necesita un identificador.");
        }

        var ambitosSolicitados = ambitos.ToList();
        var hayAmbitoInvalido = ambitosSolicitados.Any(a => !Enum.IsDefined(a));
        if (ambitosSolicitados.Count is < 1 or > 2 || hayAmbitoInvalido || ambitosSolicitados.Distinct().Count() != ambitosSolicitados.Count)
        {
            throw new ReglaDeNegocioException("El paciente debe tener uno o dos ámbitos distintos.");
        }

        var paciente = new Paciente
        {
            IdPaciente = idPaciente,
            Condicion = CondicionRegistro.Vigente,
            Version = 1
        };
        paciente.AsignarDatos(nombre, contacto);
        paciente._ambitos.AddRange(ambitosSolicitados.Select(a => new PacienteAmbito(idPaciente, a)));
        paciente.RegistrarAlta(idActor, ahora);
        return paciente;
    }

    public void Actualizar(string nombre, string? contacto, int versionEsperada, Guid idActor, DateTimeOffset ahora)
    {
        if (Condicion == CondicionRegistro.Baja)
        {
            throw new ReglaDeNegocioException("La ficha está dada de baja y no puede actualizarse.");
        }

        ExigirVersion(versionEsperada);
        AsignarDatos(nombre, contacto);
        RegistrarActualizacion(idActor, ahora);
        Version++;
    }

    public void DarDeBaja(int versionEsperada, Guid idActor, DateTimeOffset ahora)
    {
        if (Condicion == CondicionRegistro.Baja)
        {
            throw new ReglaDeNegocioException("La ficha ya está dada de baja.");
        }

        ExigirVersion(versionEsperada);
        RegistrarBaja(idActor, ahora);
        Condicion = CondicionRegistro.Baja;
        Version++;
    }

    public void Recuperar(int versionEsperada, Guid idActor, DateTimeOffset ahora)
    {
        if (Condicion == CondicionRegistro.Vigente)
        {
            throw new ReglaDeNegocioException("La ficha ya está vigente.");
        }

        ExigirActor(idActor);
        ExigirVersion(versionEsperada);
        LimpiarBaja();
        Condicion = CondicionRegistro.Vigente;
        Version++;
    }

    private void ExigirVersion(int versionEsperada)
    {
        if (versionEsperada != Version)
        {
            throw new ConflictoDeVersionException();
        }
    }

    private void AsignarDatos(string nombre, string? contacto)
    {
        var nombreLimpio = nombre?.Trim();
        if (string.IsNullOrEmpty(nombreLimpio) || nombreLimpio.Length > LongitudMaximaNombre)
        {
            throw new ReglaDeNegocioException($"El nombre es obligatorio y admite hasta {LongitudMaximaNombre} caracteres.");
        }

        var contactoLimpio = string.IsNullOrWhiteSpace(contacto) ? null : contacto.Trim();
        if (contactoLimpio is { Length: > LongitudMaximaContacto })
        {
            throw new ReglaDeNegocioException($"El contacto admite hasta {LongitudMaximaContacto} caracteres.");
        }

        Nombre = nombreLimpio;
        Contacto = contactoLimpio;
    }
}
