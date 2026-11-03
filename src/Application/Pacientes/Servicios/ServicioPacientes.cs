using TherapEase.Application.Auditoria.Interfaces.Servicios;
using TherapEase.Application.Compartido.Interfaces.Repositorios;
using TherapEase.Application.Compartido.Interfaces.Servicios;
using TherapEase.Application.Compartido.Modelos;
using TherapEase.Application.Pacientes.Constantes;
using TherapEase.Application.Pacientes.Interfaces.Repositorios;
using TherapEase.Application.Pacientes.Interfaces.Servicios;
using TherapEase.Application.Pacientes.Modelos;
using TherapEase.Domain.Auditoria.Entidades.Enums;
using TherapEase.Domain.Compartido.Entidades.Enums;
using TherapEase.Domain.Compartido.Excepciones;
using TherapEase.Domain.Compartido.Reglas;
using TherapEase.Domain.Pacientes.Entidades;
using TherapEase.Domain.Pacientes.Entidades.Enums;
using TherapEase.Domain.Pacientes.Excepciones;
using TherapEase.Domain.Pacientes.Reglas;

namespace TherapEase.Application.Pacientes.Servicios;

public class ServicioPacientes : IServicioPacientes
{
    private const string HechoDeAlta = "Se registró un paciente.";
    private const string HechoDeActualizacion = "Se actualizaron los datos de un paciente.";

    private readonly IRepositorioPacientes _pacientes;
    private readonly IUnidadDeTrabajo _unidad;
    private readonly IServicioAuditoria _auditoria;
    private readonly IAutorizacion _autorizacion;
    private readonly IUsuarioActual _usuarioActual;
    private readonly TimeProvider _tiempo;

    public ServicioPacientes(
        IRepositorioPacientes pacientes,
        IUnidadDeTrabajo unidad,
        IServicioAuditoria auditoria,
        IAutorizacion autorizacion,
        IUsuarioActual usuarioActual,
        TimeProvider tiempo)
    {
        _pacientes = pacientes;
        _unidad = unidad;
        _auditoria = auditoria;
        _autorizacion = autorizacion;
        _usuarioActual = usuarioActual;
        _tiempo = tiempo;
    }

    public async Task<RespuestaServicio<ResultadoDeAlta>> RegistrarAsync(
        Guid idSolicitud, DatosDePacienteEntrada datos, bool confirmaQueEsOtraPersona = false, CancellationToken cancelacion = default)
    {
        // Registrar y consultar se comprueban antes de leer nada: sin consultar no hay lista de parecidos, ni siquiera indicios de que exista una ficha.
        if (await ObtenerActorConPermisosAsync(cancelacion, Permiso.RegistrarPacientes, Permiso.ConsultarPacientes) is not { } idActor)
        {
            return RespuestaServicio<ResultadoDeAlta>.Fallida(MensajesDePacientes.SinPermiso);
        }

        if (datos is null || idSolicitud == Guid.Empty)
        {
            return RespuestaServicio<ResultadoDeAlta>.Fallida(MensajesDePacientes.SolicitudInvalida);
        }

        Paciente candidato;
        try
        {
            candidato = ReglasDePaciente.Registrar(idSolicitud, ADominio(datos), datos.Ambitos ?? [], idActor, _tiempo.GetUtcNow());
        }
        catch (ReglaDeNegocioException excepcion)
        {
            return RespuestaServicio<ResultadoDeAlta>.Fallida(excepcion.Message);
        }

        // Un reenvío del mismo alta se reconoce antes de buscar parecidos: de otro modo la ficha ya guardada aparecería como parecida de sí misma.
        var existente = await _pacientes.ObtenerParaComparacionAsync(idSolicitud, cancelacion);
        if (existente is not null)
        {
            return ResponderAlReenvio(existente, candidato, idActor);
        }

        if (!confirmaQueEsOtraPersona)
        {
            var parecidos = await _pacientes.BuscarParecidosAsync(
                candidato.NombreNormalizado, LimitesDeBusquedaDePacientes.MaximoDeCoincidencias + 1, cancelacion);
            if (parecidos.Count > 0)
            {
                // Entre la primera lectura y esta búsqueda, el mismo envío (doble clic o reintento) pudo guardarse y entonces su propia ficha aparece como parecida.
                // Antes de devolver parecidos se vuelve a buscar por el número de la ficha: si ya existe, es un reenvío y se responde como tal.
                var guardada = await _pacientes.ObtenerParaComparacionAsync(idSolicitud, cancelacion);
                return guardada is not null
                    ? ResponderAlReenvio(guardada, candidato, idActor)
                    : CoincidenciasPorConfirmar(parecidos);
            }
        }

        try
        {
            await _unidad.EjecutarAsync(async ct =>
            {
                await _pacientes.AgregarAsync(candidato, ct);
                await _auditoria.RegistrarHechoDeCambioAsync(
                    idActor, TipoRegistroAuditoria.Paciente, idSolicitud, AccionAuditoria.Alta,
                    ReglasDePaciente.CamposDeAlta(candidato), HechoDeAlta, ct);
            }, cancelacion);
        }
        catch (IdentificadorDePacienteRepetidoException)
        {
            return await ResolverColisionDelIdentificadorAsync(idSolicitud, candidato, idActor, cancelacion);
        }
        catch (ReglaDeNegocioException excepcion)
        {
            return RespuestaServicio<ResultadoDeAlta>.Fallida(excepcion.Message);
        }

        return RespuestaServicio<ResultadoDeAlta>.Correcta(
            new ResultadoDeAlta(EstadoDeAlta.Registrado, idSolicitud), MensajesDePacientes.PacienteRegistrado);
    }

    public async Task<RespuestaServicio<ResultadoDeBusqueda>> BuscarAsync(
        string? texto, DateOnly? nacimientoExacto = null, CancellationToken cancelacion = default)
    {
        if (await ObtenerActorConPermisosAsync(cancelacion, Permiso.ConsultarPacientes) is null)
        {
            return RespuestaServicio<ResultadoDeBusqueda>.Fallida(MensajesDePacientes.SinPermiso);
        }

        var palabras = PalabrasDeBusqueda(texto);
        if (palabras is null)
        {
            return RespuestaServicio<ResultadoDeBusqueda>.Fallida(MensajesDePacientes.BusquedaInvalida);
        }

        var filas = await _pacientes.BuscarVigentesAsync(palabras, nacimientoExacto, LimitesDeBusquedaDePacientes.MaximoDeResultados + 1, cancelacion);
        var hayMas = filas.Count > LimitesDeBusquedaDePacientes.MaximoDeResultados;
        IReadOnlyList<ResumenDePaciente> resultado = filas
            .Take(LimitesDeBusquedaDePacientes.MaximoDeResultados)
            .Select(f => f.NombreRepetido
                ? new ResumenDePaciente(f.IdPaciente, f.Nombre, f.FechaNacimiento, NacimientoDesconocido: f.FechaNacimiento is null)
                : new ResumenDePaciente(f.IdPaciente, f.Nombre))
            .ToList();
        return RespuestaServicio<ResultadoDeBusqueda>.Correcta(new ResultadoDeBusqueda(resultado, hayMas));
    }

    public async Task<RespuestaServicio<FichaDePaciente>> ObtenerAsync(Guid idPaciente, CancellationToken cancelacion = default)
    {
        if (await ObtenerActorConPermisosAsync(cancelacion, Permiso.ConsultarPacientes) is null)
        {
            return RespuestaServicio<FichaDePaciente>.Fallida(MensajesDePacientes.SinPermiso);
        }

        if (idPaciente == Guid.Empty)
        {
            return RespuestaServicio<FichaDePaciente>.Fallida(MensajesDePacientes.PacienteNoEncontrado);
        }

        // Una sola consulta: de una baja la base no entrega el contenido, y no se lee condición para lanzar luego otra consulta.
        var lectura = await _pacientes.ObtenerParaConsultaAsync(idPaciente, cancelacion);
        if (lectura is null)
        {
            return RespuestaServicio<FichaDePaciente>.Fallida(MensajesDePacientes.PacienteNoEncontrado);
        }

        if (lectura.Condicion == CondicionRegistro.Baja)
        {
            return RespuestaServicio<FichaDePaciente>.Fallida(MensajesDePacientes.FichaDeBaja);
        }

        return RespuestaServicio<FichaDePaciente>.Correcta(AFicha(lectura));
    }

    public async Task<RespuestaServicio<ResultadoDeEdicion>> ActualizarAsync(
        Guid idPaciente, int versionEsperada, DatosDePacienteEntrada datos, CancellationToken cancelacion = default)
    {
        if (await ObtenerActorConPermisosAsync(cancelacion, Permiso.ActualizarPacientes) is not { } idActor)
        {
            return RespuestaServicio<ResultadoDeEdicion>.Fallida(MensajesDePacientes.SinPermiso);
        }

        if (datos is null || idPaciente == Guid.Empty)
        {
            return RespuestaServicio<ResultadoDeEdicion>.Fallida(MensajesDePacientes.SolicitudInvalida);
        }

        // La condición se lee antes que la ficha: el contenido de una baja nunca se trae para rechazar la edición.
        var condicion = await _pacientes.ObtenerCondicionAsync(idPaciente, cancelacion);
        if (condicion is null)
        {
            return RespuestaServicio<ResultadoDeEdicion>.Fallida(MensajesDePacientes.PacienteNoEncontrado);
        }

        if (condicion == CondicionRegistro.Baja)
        {
            return RespuestaServicio<ResultadoDeEdicion>.Fallida(MensajesDePacientes.FichaDeBajaNoSeActualiza);
        }

        var paciente = await _pacientes.ObtenerAsync(idPaciente, cancelacion);
        if (paciente is null)
        {
            return RespuestaServicio<ResultadoDeEdicion>.Fallida(MensajesDePacientes.PacienteNoEncontrado);
        }

        CambiosDePaciente cambios;
        try
        {
            cambios = ReglasDePaciente.Actualizar(paciente, ADominio(datos), datos.Ambitos ?? [], versionEsperada, idActor, _tiempo.GetUtcNow());
        }
        catch (ConflictoDeVersionException)
        {
            return RespuestaServicio<ResultadoDeEdicion>.Fallida(MensajesDePacientes.ConflictoDeVersion);
        }
        catch (ReglaDeNegocioException excepcion)
        {
            return RespuestaServicio<ResultadoDeEdicion>.Fallida(excepcion.Message);
        }

        if (!cambios.Hubo)
        {
            return RespuestaServicio<ResultadoDeEdicion>.Correcta(
                new ResultadoDeEdicion(EstadoDeEdicion.SinCambios, AFicha(paciente)), MensajesDePacientes.SinCambios);
        }

        try
        {
            await _unidad.EjecutarAsync(async ct =>
            {
                _pacientes.QuitarAmbitos(cambios.AmbitosQuitados);
                await _auditoria.RegistrarHechoDeCambioAsync(
                    idActor, TipoRegistroAuditoria.Paciente, idPaciente, AccionAuditoria.Actualizacion,
                    cambios.Campos, HechoDeActualizacion, ct);
            }, cancelacion);
        }
        catch (ConflictoDeVersionException)
        {
            return RespuestaServicio<ResultadoDeEdicion>.Fallida(MensajesDePacientes.ConflictoDeVersion);
        }
        catch (ReglaDeNegocioException excepcion)
        {
            return RespuestaServicio<ResultadoDeEdicion>.Fallida(excepcion.Message);
        }

        return RespuestaServicio<ResultadoDeEdicion>.Correcta(
            new ResultadoDeEdicion(EstadoDeEdicion.Actualizado, AFicha(paciente)), MensajesDePacientes.DatosActualizados);
    }

    // El actor sale de la sesión; todos los permisos se comprueban en el servidor, en orden, antes de cualquier lectura.
    private async Task<Guid?> ObtenerActorConPermisosAsync(CancellationToken cancelacion, params Permiso[] permisos)
    {
        if (_usuarioActual.IdUsuario is not { } idUsuario)
        {
            return null;
        }

        foreach (var permiso in permisos)
        {
            if (!await _autorizacion.TienePermisoAsync(permiso, cancelacion))
            {
                return null;
            }
        }

        return idUsuario;
    }

    // Solo la misma persona que creó la ficha y con los mismos datos normalizados reconoce el reenvío; cualquier otro caso se rechaza sin revelar contenido.
    private static RespuestaServicio<ResultadoDeAlta> ResponderAlReenvio(Paciente existente, Paciente candidato, Guid idActor) =>
        existente.Condicion == CondicionRegistro.Vigente
        && existente.IdUsuarioAlta == idActor
        && ReglasDePaciente.TienenLosMismosDatos(existente, candidato)
            ? RespuestaServicio<ResultadoDeAlta>.Correcta(
                new ResultadoDeAlta(EstadoDeAlta.YaRegistrado, existente.IdPaciente), MensajesDePacientes.PacienteYaRegistrado)
            : RespuestaServicio<ResultadoDeAlta>.Fallida(MensajesDePacientes.SolicitudYaUsada);

    // Dos envíos simultáneos con el mismo identificador: la base dejó pasar a uno y rechazó al otro por la clave primaria de Paciente. La unidad de trabajo ya revirtió,
    // pero la ficha candidata puede seguir en el contexto: por eso la lectura no rastrea y viene de la base, con permisos vigentes. Solo ella puede producir YaRegistrado;
    // un fallo de auditoría u otra restricción no llega aquí.
    private async Task<RespuestaServicio<ResultadoDeAlta>> ResolverColisionDelIdentificadorAsync(
        Guid idSolicitud, Paciente candidato, Guid idActor, CancellationToken cancelacion)
    {
        if (await ObtenerActorConPermisosAsync(cancelacion, Permiso.RegistrarPacientes, Permiso.ConsultarPacientes) is null)
        {
            return RespuestaServicio<ResultadoDeAlta>.Fallida(MensajesDePacientes.SinPermiso);
        }

        var confirmado = await _pacientes.ObtenerParaComparacionAsync(idSolicitud, cancelacion)
            ?? throw new InvalidOperationException("La base informó un identificador repetido, pero no se encontró la ficha.");
        return ResponderAlReenvio(confirmado, candidato, idActor);
    }

    private static RespuestaServicio<ResultadoDeAlta> CoincidenciasPorConfirmar(IReadOnlyList<ParecidoDePaciente> parecidos)
    {
        var hayMas = parecidos.Count > LimitesDeBusquedaDePacientes.MaximoDeCoincidencias;
        IReadOnlyList<ResumenDePaciente> lista = parecidos
            .Take(LimitesDeBusquedaDePacientes.MaximoDeCoincidencias)
            .Select(p => new ResumenDePaciente(p.IdPaciente, p.Nombre, p.FechaNacimiento, NacimientoDesconocido: p.FechaNacimiento is null))
            .ToList();

        // Fallida(mensaje) no lleva datos: la combinación Exito falso con la lista se construye solo aquí.
        return new RespuestaServicio<ResultadoDeAlta>
        {
            Exito = false,
            Mensaje = MensajesDePacientes.CoincidenciasPorConfirmar,
            Datos = new ResultadoDeAlta(EstadoDeAlta.CoincidenciasPorConfirmar, Coincidencias: lista, HayMasCoincidencias: hayMas)
        };
    }

    // Texto de 2 a 100 caracteres y hasta 6 palabras, sin caracteres de control. Devuelve las palabras normalizadas, o nulo si no sirve.
    private static IReadOnlyList<string>? PalabrasDeBusqueda(string? texto)
    {
        var recortado = texto?.Trim();
        if (string.IsNullOrEmpty(recortado)
            || recortado.Length < LimitesDeBusquedaDePacientes.LongitudMinimaDelTexto
            || recortado.Length > LimitesDeBusquedaDePacientes.LongitudMaximaDelTexto
            || ReglasDeNombreYCorreo.TieneCaracteresInvisibles(recortado)
            || !NormalizacionDeNombres.TryNormalizar(recortado, out var normalizado))
        {
            return null;
        }

        var palabras = normalizado.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return palabras.Length is >= 1 and <= LimitesDeBusquedaDePacientes.MaximoDePalabras ? palabras : null;
    }

    private static DatosDePaciente ADominio(DatosDePacienteEntrada entrada) =>
        new(entrada.Nombre, entrada.FechaNacimiento, entrada.Telefono, entrada.Correo, entrada.Escuela, entrada.Grupo,
            entrada.Tutor is null ? null : new DatosDeTutor(entrada.Tutor.Nombre, entrada.Tutor.Parentesco, entrada.Tutor.Telefono, entrada.Tutor.Correo));

    private FichaDePaciente AFicha(LecturaDePaciente lectura) =>
        new(
            lectura.IdPaciente,
            lectura.Version,
            lectura.Nombre ?? string.Empty,
            lectura.FechaNacimiento,
            EdadDe(lectura.FechaNacimiento),
            lectura.Telefono,
            lectura.Correo,
            lectura.Escuela,
            lectura.Grupo,
            AmbitosDe(lectura.TieneAmbitoEscolar, lectura.TieneAmbitoIndependiente),
            TutorDe(lectura.TutorNombre, lectura.TutorParentesco, lectura.TutorTelefono, lectura.TutorCorreo));

    private FichaDePaciente AFicha(Paciente paciente) =>
        new(
            paciente.IdPaciente,
            paciente.Version,
            paciente.Nombre,
            paciente.FechaNacimiento,
            EdadDe(paciente.FechaNacimiento),
            paciente.Telefono,
            paciente.Correo,
            paciente.Escuela,
            paciente.Grupo,
            AmbitosDe(
                paciente.Ambitos.Any(a => a.Ambito == AmbitoAtencion.Escolar),
                paciente.Ambitos.Any(a => a.Ambito == AmbitoAtencion.Independiente)),
            TutorDe(paciente.TutorNombre, paciente.TutorParentesco, paciente.TutorTelefono, paciente.TutorCorreo));

    // Sin nacimiento la edad es desconocida (nula): nunca cero ni una fecha inventada.
    private int? EdadDe(DateOnly? nacimiento) =>
        nacimiento is { } fecha ? ReglasDeEdad.Calcular(fecha, ReglasDeEdad.HoyEnHermosillo(_tiempo.GetUtcNow())) : null;

    private static IReadOnlyList<AmbitoAtencion> AmbitosDe(bool escolar, bool independiente)
    {
        var ambitos = new List<AmbitoAtencion>();
        if (escolar)
        {
            ambitos.Add(AmbitoAtencion.Escolar);
        }

        if (independiente)
        {
            ambitos.Add(AmbitoAtencion.Independiente);
        }

        return ambitos;
    }

    private static DatosDeTutorEntrada? TutorDe(string? nombre, string? parentesco, string? telefono, string? correo) =>
        nombre is null && parentesco is null && telefono is null && correo is null
            ? null
            : new DatosDeTutorEntrada(nombre, parentesco, telefono, correo);
}
