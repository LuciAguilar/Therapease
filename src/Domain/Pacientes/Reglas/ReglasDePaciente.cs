using TherapEase.Domain.Compartido.Entidades.Enums;
using TherapEase.Domain.Compartido.Excepciones;
using TherapEase.Domain.Compartido.Reglas;
using TherapEase.Domain.Pacientes.Entidades;
using TherapEase.Domain.Pacientes.Entidades.Enums;

namespace TherapEase.Domain.Pacientes.Reglas;

public static class ReglasDePaciente
{
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
        AsignarDatos(paciente, nombre, contacto);
        foreach (var ambito in ambitosSolicitados)
        {
            paciente.Ambitos.Add(new PacienteAmbito { IdPaciente = idPaciente, Ambito = ambito });
        }

        MetadatosDeAuditoria.RegistrarAlta(paciente, idActor, ahora);
        return paciente;
    }

    public static void Actualizar(Paciente paciente, string nombre, string? contacto, int versionEsperada, Guid idActor, DateTimeOffset ahora)
    {
        if (paciente.Condicion == CondicionRegistro.Baja)
        {
            throw new ReglaDeNegocioException("La ficha está dada de baja y no puede actualizarse.");
        }

        ExigirVersion(paciente, versionEsperada);
        AsignarDatos(paciente, nombre, contacto);
        MetadatosDeAuditoria.RegistrarActualizacion(paciente, idActor, ahora);
        paciente.Version++;
    }

    public static void DarDeBaja(Paciente paciente, int versionEsperada, Guid idActor, DateTimeOffset ahora)
    {
        if (paciente.Condicion == CondicionRegistro.Baja)
        {
            throw new ReglaDeNegocioException("La ficha ya está dada de baja.");
        }

        ExigirVersion(paciente, versionEsperada);
        MetadatosDeAuditoria.RegistrarBaja(paciente, idActor, ahora);
        paciente.Condicion = CondicionRegistro.Baja;
        paciente.Version++;
    }

    public static void Recuperar(Paciente paciente, int versionEsperada, Guid idActor, DateTimeOffset ahora)
    {
        if (paciente.Condicion == CondicionRegistro.Vigente)
        {
            throw new ReglaDeNegocioException("La ficha ya está vigente.");
        }

        MetadatosDeAuditoria.ExigirActor(idActor);
        ExigirVersion(paciente, versionEsperada);
        MetadatosDeAuditoria.LimpiarBaja(paciente);
        paciente.Condicion = CondicionRegistro.Vigente;
        paciente.Version++;
    }

    private static void ExigirVersion(Paciente paciente, int versionEsperada)
    {
        if (versionEsperada != paciente.Version)
        {
            throw new ConflictoDeVersionException();
        }
    }

    private static void AsignarDatos(Paciente paciente, string nombre, string? contacto)
    {
        var nombreLimpio = nombre?.Trim();
        if (string.IsNullOrEmpty(nombreLimpio) || nombreLimpio.Length > Paciente.LongitudMaximaNombre)
        {
            throw new ReglaDeNegocioException($"El nombre es obligatorio y admite hasta {Paciente.LongitudMaximaNombre} caracteres.");
        }

        var contactoLimpio = string.IsNullOrWhiteSpace(contacto) ? null : contacto.Trim();
        if (contactoLimpio is { Length: > Paciente.LongitudMaximaContacto })
        {
            throw new ReglaDeNegocioException($"El contacto admite hasta {Paciente.LongitudMaximaContacto} caracteres.");
        }

        paciente.Nombre = nombreLimpio;
        paciente.Contacto = contactoLimpio;
    }
}
