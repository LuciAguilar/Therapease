using TherapEase.Application.Auditoria.Interfaces.Servicios;
using TherapEase.Application.Citas.Constantes;
using TherapEase.Application.Citas.Interfaces.Repositorios;
using TherapEase.Application.Citas.Interfaces.Servicios;
using TherapEase.Application.Citas.Modelos;
using TherapEase.Application.Compartido.Interfaces.Repositorios;
using TherapEase.Application.Compartido.Interfaces.Servicios;
using TherapEase.Application.Compartido.Modelos;
using TherapEase.Application.Pacientes.Interfaces.Servicios;
using TherapEase.Domain.Auditoria.Entidades.Enums;
using TherapEase.Domain.Citas.Entidades;
using TherapEase.Domain.Citas.Entidades.Enums;
using TherapEase.Domain.Citas.Excepciones;
using TherapEase.Domain.Citas.Reglas;
using TherapEase.Domain.Compartido.Entidades.Enums;
using TherapEase.Domain.Compartido.Excepciones;
using TherapEase.Domain.Compartido.Reglas;
using TherapEase.Domain.Pacientes.Entidades.Enums;

namespace TherapEase.Application.Citas.Servicios;

public class ServicioCitas : IServicioCitas
{
    private const string HechoDeAlta = "Se agendó una cita.";
    private const string HechoDeAltaDespuesDeSuHorario = "Se agendó una cita registrada después de su horario.";

    // Un solo hecho para ambos sentidos: el evento no revela el valor nuevo ni el anterior (decisión D7 de Lucía, 17-nov-2026).
    private const string HechoDeCambioDePago = "Se cambió el estado de pago de una cita.";

    private readonly IRepositorioCitas _citas;
    private readonly IConsultaDePacientesParaCitas _pacientes;
    private readonly IUnidadDeTrabajo _unidad;
    private readonly IServicioAuditoria _auditoria;
    private readonly IAutorizacion _autorizacion;
    private readonly IUsuarioActual _usuarioActual;
    private readonly TimeProvider _tiempo;

    public ServicioCitas(
        IRepositorioCitas citas,
        IConsultaDePacientesParaCitas pacientes,
        IUnidadDeTrabajo unidad,
        IServicioAuditoria auditoria,
        IAutorizacion autorizacion,
        IUsuarioActual usuarioActual,
        TimeProvider tiempo)
    {
        _citas = citas;
        _pacientes = pacientes;
        _unidad = unidad;
        _auditoria = auditoria;
        _autorizacion = autorizacion;
        _usuarioActual = usuarioActual;
        _tiempo = tiempo;
    }

    public async Task<RespuestaServicio<ResultadoDeAgenda>> ConsultarAgendaAsync(
        DateOnly? desde, DateOnly? hasta, VistaDeAgenda vista = VistaDeAgenda.Activas, CancellationToken cancelacion = default)
    {
        if (await ObtenerActorConPermisosAsync(cancelacion, Permiso.ConsultarAgenda, Permiso.ConsultarPacientes) is null)
        {
            return RespuestaServicio<ResultadoDeAgenda>.Fallida(MensajesDeCitas.SinPermiso);
        }

        // Todo se valida antes de consultar datos: un periodo, una vista o una fecha inválidos no llegan a la base.
        var hoy = DateOnly.FromDateTime(HoraHermosillo.ALocal(_tiempo.GetUtcNow()));
        if (!Enum.IsDefined(vista) || PeriodoValido(desde, hasta, hoy) is not { } periodo)
        {
            return RespuestaServicio<ResultadoDeAgenda>.Fallida(MensajesDeCitas.PeriodoInvalido);
        }

        var filas = await _citas.ConsultarAgendaAsync(periodo.InicioUtc, periodo.FinUtc, vista, LimitesDeAgenda.MaximoDeFilas + 1, cancelacion);
        var hayMas = filas.Count > LimitesDeAgenda.MaximoDeFilas;
        var visibles = filas.Take(LimitesDeAgenda.MaximoDeFilas).ToList();

        // Dos consultas, sin unir módulos: Pacientes decide qué nombre se puede mostrar. Quien falta en la respuesta está de baja.
        IReadOnlyDictionary<Guid, string> nombres = new Dictionary<Guid, string>();
        if (visibles.Count > 0)
        {
            var respuesta = await _pacientes.ConsultarNombresParaAgendaAsync(visibles.Select(f => f.IdPaciente).Distinct().ToArray(), cancelacion);
            if (!respuesta.Exito || respuesta.Datos is null)
            {
                return RespuestaServicio<ResultadoDeAgenda>.Fallida(MensajesDeCitas.SinPermiso);
            }

            nombres = respuesta.Datos;
        }

        IReadOnlyList<CitaDeAgenda> citas = visibles.Select(f => ACitaDeAgenda(f, nombres)).ToList();
        return RespuestaServicio<ResultadoDeAgenda>.Correcta(
            new ResultadoDeAgenda(citas, periodo.Desde, periodo.Hasta, hoy, vista, hayMas));
    }

    public async Task<RespuestaServicio<DetalleDeCita>> ObtenerCitaAsync(Guid idCita, CancellationToken cancelacion = default)
    {
        if (await ObtenerActorConPermisosAsync(cancelacion, Permiso.ConsultarAgenda, Permiso.ConsultarPacientes) is null)
        {
            return RespuestaServicio<DetalleDeCita>.Fallida(MensajesDeCitas.SinPermiso);
        }

        // Una cita de baja se trata igual que una inexistente: no se distingue ni se muestra.
        var cita = idCita == Guid.Empty ? null : await _citas.ObtenerParaComparacionAsync(idCita, cancelacion);
        if (cita is null || cita.Condicion != CondicionRegistro.Vigente)
        {
            return RespuestaServicio<DetalleDeCita>.Fallida(MensajesDeCitas.CitaNoEncontrada);
        }

        var nombres = await LeerNombresAsync(cita.IdPaciente, cancelacion);
        if (nombres is null)
        {
            return RespuestaServicio<DetalleDeCita>.Fallida(MensajesDeCitas.SinPermiso);
        }

        return RespuestaServicio<DetalleDeCita>.Correcta(ADetalle(cita, nombres));
    }

    public async Task<RespuestaServicio<ResultadoDeAgendar>> AgendarAsync(
        Guid idSolicitud, DatosDeCitaEntrada datos, CancellationToken cancelacion = default)
    {
        // Permisos antes de leer nada: sin ConsultarPacientes no hay mensajes que digan si un paciente existe o está de baja.
        if (await ObtenerActorConPermisosAsync(cancelacion, Permiso.AgendarCitas, Permiso.ConsultarPacientes) is not { } idActor)
        {
            return RespuestaServicio<ResultadoDeAgendar>.Fallida(MensajesDeCitas.SinPermiso);
        }

        // Sin datos de la base: identificador, campos presentes, ámbito definido y horario válido y representable.
        if (idSolicitud == Guid.Empty
            || datos is not { IdPaciente: { } idPaciente, Ambito: { } ambito, Fecha: { } fecha, HoraInicio: { } horaInicio, HoraFin: { } horaFin }
            || idPaciente == Guid.Empty
            || !Enum.IsDefined(ambito))
        {
            return RespuestaServicio<ResultadoDeAgendar>.Fallida(MensajesDeCitas.SolicitudInvalida);
        }

        Cita candidata;
        try
        {
            var horario = ReglasDeHorarioDeCita.Calcular(fecha, horaInicio, horaFin);
            candidata = ReglasDeCita.Agendar(idSolicitud, idPaciente, ambito, horario.Inicio, horario.Fin, idActor, _tiempo.GetUtcNow());
        }
        catch (ReglaDeNegocioException excepcion)
        {
            return RespuestaServicio<ResultadoDeAgendar>.Fallida(excepcion.Message);
        }

        // Primera lectura por número, sin rastrear. Un reenvío de una reserva ya confirmada se reconoce aquí y no repite la comprobación del paciente ni del ámbito.
        var existente = await _citas.ObtenerParaComparacionAsync(idSolicitud, cancelacion);
        if (existente is not null)
        {
            return await ResponderAlReenvioAsync(existente, candidata, cancelacion);
        }

        // Una lectura sin bloqueo de condición y ámbitos (D10): si el retiro de un ámbito se confirma después de esta lectura, la cita conserva el ámbito elegido.
        var paciente = await _pacientes.ConsultarCondicionYAmbitosAsync(idPaciente, cancelacion);
        if (!paciente.Exito || paciente.Datos is null)
        {
            return RespuestaServicio<ResultadoDeAgendar>.Fallida(MensajesDeCitas.SinPermiso);
        }

        var rechazo = paciente.Datos switch
        {
            { Condicion: null } => MensajesDeCitas.PacienteNoEncontrado,
            { Condicion: CondicionRegistro.Baja } => MensajesDeCitas.PacienteDeBaja,
            var vigente when !vigente.Ambitos.Contains(ambito) => MensajesDeCitas.AmbitoNoPermitido,
            _ => null
        };
        if (rechazo is not null)
        {
            // La misma reserva pudo confirmarse entre la primera lectura y esta comprobación: antes de rechazar se resuelve con una relectura.
            return await ResolverRechazoAsync(candidata, rechazo, cancelacion);
        }

        try
        {
            await _unidad.EjecutarAsync(async ct =>
            {
                await _citas.AgregarAsync(candidata, ct);
                await _auditoria.RegistrarHechoDeCambioAsync(
                    idActor, TipoRegistroAuditoria.Cita, idSolicitud, AccionAuditoria.Alta,
                    ReglasDeCita.CamposDeAlta(),
                    ReglasDeCita.FueRegistradaDespuesDeSuHorario(candidata) ? HechoDeAltaDespuesDeSuHorario : HechoDeAlta, ct);
            }, cancelacion);
        }
        catch (IdentificadorDeCitaRepetidoException)
        {
            // Dos envíos simultáneos con el mismo identificador: la base dejó pasar a uno. Solo una cita confirmada puede explicarlo; sin ella no hay respuesta honesta.
            return await RelecerAsync(candidata, cancelacion)
                ?? throw new InvalidOperationException("La base informó un identificador repetido, pero no se encontró la cita.");
        }
        catch (CruceDeHorarioException)
        {
            return await ResolverRechazoAsync(candidata, MensajesDeCitas.HorarioOcupado, cancelacion);
        }
        catch (CitaActivaConPacienteDeBajaException)
        {
            return await ResolverRechazoAsync(candidata, MensajesDeCitas.PacienteDeBaja, cancelacion);
        }

        // Ya confirmada: el nombre es solo para mostrar. Si el permiso se retira justo ahora, la cita ya existe y se responde sin nombre.
        var nombresAlConfirmar = await LeerNombresAsync(idPaciente, cancelacion);
        return RespuestaServicio<ResultadoDeAgendar>.Correcta(
            new ResultadoDeAgendar(EstadoDeAgendar.Agendada, ADetalle(candidata, nombresAlConfirmar)), MensajesDeCitas.CitaAgendada);
    }

    public async Task<RespuestaServicio<ResultadoDeCambioDePago>> CambiarPagoAsync(
        Guid idCita, int versionEsperada, EstadoPago? nuevoPago, CancellationToken cancelacion = default)
    {
        // Permisos antes de leer o validar nada: sin ellos no se averigua si la cita existe ni si la entrada era válida.
        if (await ObtenerActorConPermisosAsync(cancelacion, Permiso.CambiarPagoDeCitas, Permiso.ConsultarAgenda, Permiso.ConsultarPacientes) is not { } idActor)
        {
            return RespuestaServicio<ResultadoDeCambioDePago>.Fallida(MensajesDeCitas.SinPermiso);
        }

        // Sin datos de la base. Un identificador vacío no puede ser una cita; un pago ausente no se toma por Pendiente.
        if (idCita == Guid.Empty)
        {
            return RespuestaServicio<ResultadoDeCambioDePago>.Fallida(MensajesDeCitas.CitaNoEncontrada);
        }

        if (nuevoPago is not { } pago || !Enum.IsDefined(pago) || versionEsperada < 1)
        {
            return RespuestaServicio<ResultadoDeCambioDePago>.Fallida(MensajesDeCitas.SolicitudInvalida);
        }

        // Una sola lectura rastreada. De una cita de baja no se muestra nada: la regla la rechaza sin devolver su contenido.
        var cita = await _citas.ObtenerAsync(idCita, cancelacion);
        if (cita is null)
        {
            return RespuestaServicio<ResultadoDeCambioDePago>.Fallida(MensajesDeCitas.CitaNoEncontrada);
        }

        bool huboCambio;
        try
        {
            huboCambio = ReglasDeCita.CambiarPago(cita, pago, versionEsperada, idActor, _tiempo.GetUtcNow());
        }
        catch (ConflictoDeVersionException)
        {
            return RespuestaServicio<ResultadoDeCambioDePago>.Fallida(MensajesDeCitas.ConflictoDeVersion);
        }
        catch (ReglaDeNegocioException excepcion)
        {
            return RespuestaServicio<ResultadoDeCambioDePago>.Fallida(excepcion.Message);
        }

        // «Sin cambios» describe la cita como estaba al leerla: no se abre transacción ni se bloquea nada para una respuesta que no escribe.
        if (!huboCambio)
        {
            return RespuestaServicio<ResultadoDeCambioDePago>.Correcta(
                new ResultadoDeCambioDePago(EstadoDeCambioDePago.SinCambios, APago(cita)), MensajesDeCitas.SinCambios);
        }

        // El cambio y su evento se guardan o se revierten juntos. La versión protege el guardado: si otra operación cambió la fila, el UPDATE no
        // afecta ninguna fila y la unidad de trabajo lo traduce a conflicto, sin dejar ni el cambio ni el evento. Cualquier otro fallo sube como error general.
        try
        {
            await _unidad.EjecutarAsync(async ct =>
            {
                await _auditoria.RegistrarHechoDeCambioAsync(
                    idActor, TipoRegistroAuditoria.Cita, idCita, AccionAuditoria.Actualizacion,
                    ReglasDeCita.CamposDePago(), HechoDeCambioDePago, ct);
            }, cancelacion);
        }
        catch (ConflictoDeVersionException)
        {
            return RespuestaServicio<ResultadoDeCambioDePago>.Fallida(MensajesDeCitas.ConflictoDeVersion);
        }

        // Ya confirmada: la respuesta sale de la cita en memoria, sin otra lectura ni datos del paciente.
        return RespuestaServicio<ResultadoDeCambioDePago>.Correcta(
            new ResultadoDeCambioDePago(EstadoDeCambioDePago.Actualizado, APago(cita)), MensajesDeCitas.PagoActualizado);
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

    // Única resolución de un rechazo posterior a la primera lectura: se vuelven a validar sesión y permisos y se relee por número, sin rastrear.
    // Con la misma reserva ya confirmada: YaAgendada. Con una cita que no se puede reconocer: SolicitudYaUsada. Sin cita: el rechazo original.
    private async Task<RespuestaServicio<ResultadoDeAgendar>> ResolverRechazoAsync(
        Cita candidata, string rechazoOriginal, CancellationToken cancelacion) =>
        await RelecerAsync(candidata, cancelacion) ?? RespuestaServicio<ResultadoDeAgendar>.Fallida(rechazoOriginal);

    // Nulo cuando no hay ninguna cita con ese número: quien llama decide si es un rechazo de negocio o un error inesperado.
    private async Task<RespuestaServicio<ResultadoDeAgendar>?> RelecerAsync(Cita candidata, CancellationToken cancelacion)
    {
        if (await ObtenerActorConPermisosAsync(cancelacion, Permiso.AgendarCitas, Permiso.ConsultarPacientes) is null)
        {
            return RespuestaServicio<ResultadoDeAgendar>.Fallida(MensajesDeCitas.SinPermiso);
        }

        var confirmada = await _citas.ObtenerParaComparacionAsync(candidata.IdCita, cancelacion);
        return confirmada is null ? null : await ResponderAlReenvioAsync(confirmada, candidata, cancelacion);
    }

    // Solo la misma persona, con los mismos datos y la cita aún agendada y vigente, reconoce el reenvío; cualquier otro caso se rechaza sin revelar contenido.
    // Paciente y ámbito no se vuelven a comprobar: pudieron cambiar después de la reserva original y basta con actor, datos y permisos vigentes.
    private async Task<RespuestaServicio<ResultadoDeAgendar>> ResponderAlReenvioAsync(Cita existente, Cita candidata, CancellationToken cancelacion)
    {
        if (!ReglasDeCita.EsLaMismaReserva(existente, candidata))
        {
            return RespuestaServicio<ResultadoDeAgendar>.Fallida(MensajesDeCitas.SolicitudYaUsada);
        }

        var nombres = await LeerNombresAsync(existente.IdPaciente, cancelacion);
        return RespuestaServicio<ResultadoDeAgendar>.Correcta(
            new ResultadoDeAgendar(EstadoDeAgendar.YaAgendada, ADetalle(existente, nombres)), MensajesDeCitas.CitaYaAgendada);
    }

    // Nulo si Pacientes negó el permiso; si no, quien no está en el diccionario es un paciente de baja.
    private async Task<IReadOnlyDictionary<Guid, string>?> LeerNombresAsync(Guid idPaciente, CancellationToken cancelacion)
    {
        var respuesta = await _pacientes.ConsultarNombresParaAgendaAsync([idPaciente], cancelacion);
        return respuesta.Exito ? respuesta.Datos : null;
    }

    // Periodo con ambas fechas incluidas: sin fechas, hoy y los seis días siguientes. Devuelve también el intervalo UTC semiabierto [inicio, fin),
    // o nulo si el periodo no sirve. Se rechaza antes de sumar un día o convertir cualquier extremo que no sea representable.
    private static (DateOnly Desde, DateOnly Hasta, DateTimeOffset InicioUtc, DateTimeOffset FinUtc)? PeriodoValido(
        DateOnly? desde, DateOnly? hasta, DateOnly hoy)
    {
        if (desde is null != hasta is null)
        {
            return null;
        }

        var primero = desde ?? hoy;
        var ultimo = hasta ?? hoy.AddDays(LimitesDeAgenda.DiasPorOmision - 1);
        var dias = ultimo.DayNumber - primero.DayNumber + 1;
        if (dias < LimitesDeAgenda.DiasMinimos || dias > LimitesDeAgenda.DiasMaximos || ultimo == DateOnly.MaxValue)
        {
            return null;
        }

        var siguiente = ultimo.AddDays(1);
        if (!HoraHermosillo.EsRepresentable(primero, TimeOnly.MinValue) || !HoraHermosillo.EsRepresentable(siguiente, TimeOnly.MinValue))
        {
            return null;
        }

        return (primero, ultimo, HoraHermosillo.AInstante(primero, TimeOnly.MinValue), HoraHermosillo.AInstante(siguiente, TimeOnly.MinValue));
    }

    private static CitaDeAgenda ACitaDeAgenda(FilaDeCitaDeAgenda fila, IReadOnlyDictionary<Guid, string> nombres)
    {
        var tieneNombre = nombres.TryGetValue(fila.IdPaciente, out var nombre);
        return new CitaDeAgenda(
            fila.IdCita,
            HoraHermosillo.ALocal(fila.Inicio),
            HoraHermosillo.ALocal(fila.Fin),
            fila.Ambito,
            fila.Estado,
            tieneNombre ? fila.IdPaciente : null,
            tieneNombre ? nombre : null,
            PacienteDeBaja: !tieneNombre,
            fila.RegistradaDespuesDeSuHorario);
    }

    private static EstadoDePagoDeCita APago(Cita cita) =>
        new(cita.IdCita, cita.EstadoPago, cita.Estado, cita.Version);

    // Con nombres nulos (permiso retirado tras confirmar) no se afirma que el paciente esté de baja: solo falta el nombre.
    private static DetalleDeCita ADetalle(Cita cita, IReadOnlyDictionary<Guid, string>? nombres)
    {
        string? nombre = null;
        var deBaja = nombres is not null && !nombres.TryGetValue(cita.IdPaciente, out nombre);
        return new DetalleDeCita(
            cita.IdCita,
            HoraHermosillo.ALocal(cita.Inicio),
            HoraHermosillo.ALocal(cita.Fin),
            cita.Ambito,
            cita.Estado,
            deBaja ? null : cita.IdPaciente,
            nombre,
            deBaja,
            ReglasDeCita.FueRegistradaDespuesDeSuHorario(cita),
            cita.EstadoPago,
            cita.Version);
    }
}
