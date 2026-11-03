using TherapEase.Domain.Compartido.Entidades.Enums;
using TherapEase.Domain.Compartido.Excepciones;
using TherapEase.Domain.Compartido.Reglas;
using TherapEase.Domain.Pacientes.Constantes;
using TherapEase.Domain.Pacientes.Entidades;
using TherapEase.Domain.Pacientes.Entidades.Enums;

namespace TherapEase.Domain.Pacientes.Reglas;

public static class ReglasDePaciente
{
    // Valida todo antes de crear nada: un rechazo no deja una ficha a medias. El nacimiento es obligatorio y, si la persona es menor de 18 años, también un tutor completo.
    public static Paciente Registrar(
        Guid idPaciente,
        DatosDePaciente datos,
        IEnumerable<AmbitoAtencion> ambitos,
        Guid idActor,
        DateTimeOffset ahora)
    {
        ArgumentNullException.ThrowIfNull(ambitos);

        if (idPaciente == Guid.Empty)
        {
            throw new ReglaDeNegocioException(MensajesDeDatosDePaciente.IdentificadorNecesario);
        }

        var ambitosSolicitados = ValidarAmbitos(ambitos);
        var hoy = ReglasDeEdad.HoyEnHermosillo(ahora);
        var normalizados = ReglasDeDatosDePaciente.Normalizar(datos, hoy);
        ReglasDeDatosDePaciente.ExigirCompletos(normalizados, hoy);

        var paciente = new Paciente
        {
            IdPaciente = idPaciente,
            Condicion = CondicionRegistro.Vigente,
            Version = 1
        };
        Asignar(paciente, normalizados);
        foreach (var ambito in ambitosSolicitados)
        {
            paciente.AmbitosEditables.Add(new PacienteAmbito { IdPaciente = idPaciente, Ambito = ambito });
        }

        MetadatosDeAuditoria.RegistrarAlta(paciente, idActor, ahora);
        return paciente;
    }

    // Orden: ficha de baja, versión (una versión anterior da conflicto siempre, aunque los datos coincidan), formato de los datos y, solo si algo cambia,
    // los datos obligatorios. Sin cambios con la versión vigente no se escribe nada, no se sube la versión y no se exige completar datos de una ficha anterior.
    public static CambiosDePaciente Actualizar(
        Paciente paciente,
        DatosDePaciente datos,
        IEnumerable<AmbitoAtencion> ambitos,
        int versionEsperada,
        Guid idActor,
        DateTimeOffset ahora)
    {
        ArgumentNullException.ThrowIfNull(paciente);
        ArgumentNullException.ThrowIfNull(ambitos);

        if (paciente.Condicion == CondicionRegistro.Baja)
        {
            throw new ReglaDeNegocioException(MensajesDeDatosDePaciente.FichaDeBajaNoSeActualiza);
        }

        ExigirVersion(paciente, versionEsperada);
        MetadatosDeAuditoria.ExigirActor(idActor);

        var ambitosSolicitados = ValidarAmbitos(ambitos);
        var hoy = ReglasDeEdad.HoyEnHermosillo(ahora);
        var normalizados = ReglasDeDatosDePaciente.Normalizar(datos, hoy);

        var campos = CamposQueCambian(paciente, normalizados, ambitosSolicitados);
        if (campos.Count == 0)
        {
            return CambiosDePaciente.Ninguno;
        }

        // Con el nacimiento resultante de esta misma edición: pasar de adulto a menor exige tutor completo antes de guardar.
        ReglasDeDatosDePaciente.ExigirCompletos(normalizados, hoy);

        Asignar(paciente, normalizados);
        var quitados = ActualizarAmbitos(paciente, ambitosSolicitados);
        MetadatosDeAuditoria.RegistrarActualizacion(paciente, idActor, ahora);
        paciente.Version++;
        return new CambiosDePaciente(campos, quitados);
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

    // Nombres de los campos que lleva un alta, para su evento: los obligatorios y, de los opcionales, solo los que se escribieron.
    public static IReadOnlyList<string> CamposDeAlta(Paciente paciente)
    {
        var campos = new List<string> { CamposDePaciente.Nombre, CamposDePaciente.FechaNacimiento };
        AgregarSiHay(campos, CamposDePaciente.Telefono, paciente.Telefono);
        AgregarSiHay(campos, CamposDePaciente.Correo, paciente.Correo);
        AgregarSiHay(campos, CamposDePaciente.Escuela, paciente.Escuela);
        AgregarSiHay(campos, CamposDePaciente.Grupo, paciente.Grupo);
        AgregarSiHay(campos, CamposDePaciente.TutorNombre, paciente.TutorNombre);
        AgregarSiHay(campos, CamposDePaciente.TutorParentesco, paciente.TutorParentesco);
        AgregarSiHay(campos, CamposDePaciente.TutorTelefono, paciente.TutorTelefono);
        AgregarSiHay(campos, CamposDePaciente.TutorCorreo, paciente.TutorCorreo);
        campos.Add(CamposDePaciente.Ambitos);
        return campos;
    }

    // Dos fichas con los mismos datos de contenido y los mismos ámbitos. Sirve para reconocer el reenvío de un alta ya guardada.
    public static bool TienenLosMismosDatos(Paciente uno, Paciente otro) =>
        uno.Nombre == otro.Nombre
        && uno.FechaNacimiento == otro.FechaNacimiento
        && uno.Telefono == otro.Telefono
        && uno.Correo == otro.Correo
        && uno.Escuela == otro.Escuela
        && uno.Grupo == otro.Grupo
        && uno.TutorNombre == otro.TutorNombre
        && uno.TutorParentesco == otro.TutorParentesco
        && uno.TutorTelefono == otro.TutorTelefono
        && uno.TutorCorreo == otro.TutorCorreo
        && MismosAmbitos(uno, otro);

    private static void ExigirVersion(Paciente paciente, int versionEsperada)
    {
        if (versionEsperada != paciente.Version)
        {
            throw new ConflictoDeVersionException();
        }
    }

    private static List<AmbitoAtencion> ValidarAmbitos(IEnumerable<AmbitoAtencion> ambitos)
    {
        var solicitados = ambitos.ToList();
        if (solicitados.Count is < 1 or > 2 || solicitados.Any(a => !Enum.IsDefined(a)) || solicitados.Distinct().Count() != solicitados.Count)
        {
            throw new ReglaDeNegocioException(MensajesDeDatosDePaciente.AmbitosInvalidos);
        }

        return solicitados;
    }

    private static void Asignar(Paciente paciente, DatosDePaciente datos)
    {
        paciente.Nombre = datos.Nombre!;
        paciente.NombreNormalizado = NormalizacionDeNombres.Normalizar(datos.Nombre);
        paciente.FechaNacimiento = datos.FechaNacimiento;
        paciente.Telefono = datos.Telefono;
        paciente.Correo = datos.Correo;
        paciente.Escuela = datos.Escuela;
        paciente.Grupo = datos.Grupo;
        paciente.TutorNombre = datos.Tutor?.Nombre;
        paciente.TutorParentesco = datos.Tutor?.Parentesco;
        paciente.TutorTelefono = datos.Tutor?.Telefono;
        paciente.TutorCorreo = datos.Tutor?.Correo;
    }

    private static List<string> CamposQueCambian(Paciente paciente, DatosDePaciente datos, List<AmbitoAtencion> ambitos)
    {
        var campos = new List<string>();
        AgregarSiCambia(campos, CamposDePaciente.Nombre, paciente.Nombre != datos.Nombre);
        AgregarSiCambia(campos, CamposDePaciente.FechaNacimiento, paciente.FechaNacimiento != datos.FechaNacimiento);
        AgregarSiCambia(campos, CamposDePaciente.Telefono, paciente.Telefono != datos.Telefono);
        AgregarSiCambia(campos, CamposDePaciente.Correo, paciente.Correo != datos.Correo);
        AgregarSiCambia(campos, CamposDePaciente.Escuela, paciente.Escuela != datos.Escuela);
        AgregarSiCambia(campos, CamposDePaciente.Grupo, paciente.Grupo != datos.Grupo);
        AgregarSiCambia(campos, CamposDePaciente.TutorNombre, paciente.TutorNombre != datos.Tutor?.Nombre);
        AgregarSiCambia(campos, CamposDePaciente.TutorParentesco, paciente.TutorParentesco != datos.Tutor?.Parentesco);
        AgregarSiCambia(campos, CamposDePaciente.TutorTelefono, paciente.TutorTelefono != datos.Tutor?.Telefono);
        AgregarSiCambia(campos, CamposDePaciente.TutorCorreo, paciente.TutorCorreo != datos.Tutor?.Correo);
        AgregarSiCambia(
            campos, CamposDePaciente.Ambitos,
            !paciente.Ambitos.Select(a => a.Ambito).Order().SequenceEqual(ambitos.Order()));
        return campos;
    }

    private static List<PacienteAmbito> ActualizarAmbitos(Paciente paciente, List<AmbitoAtencion> solicitados)
    {
        var quitados = paciente.AmbitosEditables.Where(a => !solicitados.Contains(a.Ambito)).ToList();
        foreach (var ambito in quitados)
        {
            paciente.AmbitosEditables.Remove(ambito);
        }

        foreach (var ambito in solicitados.Where(s => paciente.AmbitosEditables.All(a => a.Ambito != s)))
        {
            paciente.AmbitosEditables.Add(new PacienteAmbito { IdPaciente = paciente.IdPaciente, Ambito = ambito });
        }

        return quitados;
    }

    private static bool MismosAmbitos(Paciente uno, Paciente otro) =>
        uno.Ambitos.Select(a => a.Ambito).Order().SequenceEqual(otro.Ambitos.Select(a => a.Ambito).Order());

    private static void AgregarSiCambia(List<string> campos, string nombre, bool cambia)
    {
        if (cambia)
        {
            campos.Add(nombre);
        }
    }

    private static void AgregarSiHay(List<string> campos, string nombre, string? valor)
    {
        if (valor is not null)
        {
            campos.Add(nombre);
        }
    }
}
