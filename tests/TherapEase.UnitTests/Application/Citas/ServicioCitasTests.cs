using TherapEase.Application.Auditoria.Servicios;
using TherapEase.Application.Citas.Constantes;
using TherapEase.Application.Citas.Modelos;
using TherapEase.Application.Citas.Servicios;
using TherapEase.Domain.Auditoria.Entidades.Enums;
using TherapEase.Domain.Citas.Constantes;
using TherapEase.Domain.Citas.Entidades;
using TherapEase.Domain.Citas.Entidades.Enums;
using TherapEase.Domain.Citas.Excepciones;
using TherapEase.Domain.Citas.Reglas;
using TherapEase.Domain.Compartido.Entidades.Enums;
using TherapEase.Domain.Compartido.Excepciones;
using TherapEase.Domain.Pacientes.Entidades.Enums;
using TherapEase.UnitTests.Dobles;

namespace TherapEase.UnitTests.Application.Citas;

public class ServicioCitasTests
{
    // 5-oct-2026, 08:00 en Hermosillo.
    private static readonly DateTimeOffset Ahora = new(2026, 10, 5, 15, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Hoy = new(2026, 10, 5);
    private static readonly DateOnly DiaFuturo = new(2026, 11, 2);
    private static readonly Guid OtroUsuario = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private readonly RepositorioCitasFalso _citas = new();
    private readonly ConsultaDePacientesParaCitasFalsa _pacientes = new();
    private readonly UnidadDeTrabajoFalsa _unidad = new();
    private readonly RepositorioAuditoriaFalso _repositorioAuditoria = new();
    private readonly AutorizacionFalsa _autorizacion = new();
    private readonly UsuarioActualFalso _actual = new();
    private readonly Guid _idActor = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _idPaciente;
    private readonly ServicioCitas _servicio;

    public ServicioCitasTests()
    {
        _actual.IdUsuario = _idActor;
        _autorizacion.Permisos.UnionWith([Permiso.ConsultarAgenda, Permiso.AgendarCitas, Permiso.ConsultarPacientes]);
        _idPaciente = _pacientes.Agregar("Paciente Ficticio", CondicionRegistro.Vigente, AmbitoAtencion.Escolar, AmbitoAtencion.Independiente);
        _servicio = Crear(Ahora);
    }

    private ServicioCitas Crear(DateTimeOffset ahora)
    {
        var reloj = new RelojFijo(ahora);
        var auditoria = new ServicioAuditoria(_repositorioAuditoria, _autorizacion, reloj);
        return new ServicioCitas(_citas, _pacientes, _unidad, auditoria, _autorizacion, _actual, reloj);
    }

    private DatosDeCitaEntrada Datos(
        int horaInicio = 10, int minutoInicio = 0, int horaFin = 11, int minutoFin = 0, AmbitoAtencion ambito = AmbitoAtencion.Escolar,
        Guid? paciente = null, DateOnly? fecha = null) =>
        new(paciente ?? _idPaciente, ambito, fecha ?? DiaFuturo, new TimeOnly(horaInicio, minutoInicio), new TimeOnly(horaFin, minutoFin));

    // Guarda directo en el doble, sin pasar por el servicio ni por sus eventos. Por omisión es la misma reserva que Datos().
    private Cita Gemela(
        Guid idCita, Guid? actor = null, int horaInicio = 10, int horaFin = 11, Guid? paciente = null,
        AmbitoAtencion ambito = AmbitoAtencion.Escolar, DateOnly? fecha = null, DateTimeOffset? alta = null)
    {
        var horario = ReglasDeHorarioDeCita.Calcular(fecha ?? DiaFuturo, new TimeOnly(horaInicio, 0), new TimeOnly(horaFin, 0));
        var cita = ReglasDeCita.Agendar(idCita, paciente ?? _idPaciente, ambito, horario.Inicio, horario.Fin, actor ?? _idActor, alta ?? Ahora);
        _citas.Citas[idCita] = cita;
        return cita;
    }

    private Cita AgregarCita(
        DateTimeOffset inicio, DateTimeOffset fin, Guid? paciente = null, AmbitoAtencion ambito = AmbitoAtencion.Escolar, DateTimeOffset? alta = null)
    {
        var cita = ReglasDeCita.Agendar(Guid.NewGuid(), paciente ?? _idPaciente, ambito, inicio, fin, _idActor, alta ?? Ahora);
        _citas.Citas[cita.IdCita] = cita;
        return cita;
    }

    private static DateTimeOffset Utc(int anio, int mes, int dia, int hora, int minuto = 0) => new(anio, mes, dia, hora, minuto, 0, TimeSpan.Zero);

    private void AssertSinEfectos()
    {
        Assert.Equal(0, _citas.Lecturas);
        Assert.Equal(0, _pacientes.ConsultasDeCondicion);
        Assert.Equal(0, _pacientes.ConsultasDeNombres);
        Assert.Equal(0, _unidad.Confirmaciones);
        Assert.Empty(_citas.Citas);
        Assert.Empty(_repositorioAuditoria.Eventos);
    }

    // ----- Permisos -----

    [Theory]
    [InlineData(Permiso.AgendarCitas)]
    [InlineData(Permiso.ConsultarPacientes)]
    public async Task Agendar_SinAlgunoDeLosDosPermisos_RechazaAntesDeLeerNada(Permiso faltante)
    {
        _autorizacion.Permisos.Remove(faltante);

        var respuesta = await _servicio.AgendarAsync(Guid.NewGuid(), Datos());

        Assert.False(respuesta.Exito);
        Assert.Equal(MensajesDeCitas.SinPermiso, respuesta.Mensaje);
        Assert.Null(respuesta.Datos);
        AssertSinEfectos();
    }

    [Fact]
    public async Task Agendar_SinSesion_RechazaAntesDeLeerNada()
    {
        _actual.IdUsuario = null;

        var respuesta = await _servicio.AgendarAsync(Guid.NewGuid(), Datos());

        Assert.Equal(MensajesDeCitas.SinPermiso, respuesta.Mensaje);
        AssertSinEfectos();
    }

    [Theory]
    [InlineData(Permiso.ConsultarAgenda)]
    [InlineData(Permiso.ConsultarPacientes)]
    public async Task ConsultarAgenda_SinAlgunoDeLosDosPermisos_RechazaAntesDeLeerNada(Permiso faltante)
    {
        _autorizacion.Permisos.Remove(faltante);
        AgregarCita(Utc(2026, 10, 6, 17), Utc(2026, 10, 6, 18));

        var respuesta = await _servicio.ConsultarAgendaAsync(null, null);

        Assert.Equal(MensajesDeCitas.SinPermiso, respuesta.Mensaje);
        Assert.Null(respuesta.Datos);
        Assert.Equal(0, _citas.Lecturas);
        Assert.Equal(0, _pacientes.ConsultasDeNombres);
    }

    [Fact]
    public async Task ConsultarAgenda_SinSesion_Rechaza()
    {
        _actual.IdUsuario = null;

        var respuesta = await _servicio.ConsultarAgendaAsync(null, null);

        Assert.Equal(MensajesDeCitas.SinPermiso, respuesta.Mensaje);
        Assert.Equal(0, _citas.Lecturas);
    }

    [Theory]
    [InlineData(Permiso.ConsultarAgenda)]
    [InlineData(Permiso.ConsultarPacientes)]
    public async Task ObtenerCita_SinAlgunoDeLosDosPermisos_RechazaAntesDeLeerNada(Permiso faltante)
    {
        var cita = Gemela(Guid.NewGuid());
        _autorizacion.Permisos.Remove(faltante);

        var respuesta = await _servicio.ObtenerCitaAsync(cita.IdCita);

        Assert.Equal(MensajesDeCitas.SinPermiso, respuesta.Mensaje);
        Assert.Equal(0, _citas.Lecturas);
        Assert.Equal(0, _pacientes.ConsultasDeNombres);
    }

    // ----- Entrada de agendar -----

    [Fact]
    public async Task Agendar_ConIdentificadorVacio_EsSolicitudInvalidaSinLeerNada()
    {
        var respuesta = await _servicio.AgendarAsync(Guid.Empty, Datos());

        Assert.Equal(MensajesDeCitas.SolicitudInvalida, respuesta.Mensaje);
        AssertSinEfectos();
    }

    [Fact]
    public async Task Agendar_SinDatos_EsSolicitudInvalidaSinLeerNada()
    {
        var respuesta = await _servicio.AgendarAsync(Guid.NewGuid(), null!);

        Assert.Equal(MensajesDeCitas.SolicitudInvalida, respuesta.Mensaje);
        AssertSinEfectos();
    }

    public static TheoryData<DatosDeCitaEntrada> EntradasIncompletasOInvalidas() => new()
    {
        new DatosDeCitaEntrada(null, AmbitoAtencion.Escolar, new DateOnly(2026, 11, 2), new TimeOnly(10, 0), new TimeOnly(11, 0)),
        new DatosDeCitaEntrada(Guid.Empty, AmbitoAtencion.Escolar, new DateOnly(2026, 11, 2), new TimeOnly(10, 0), new TimeOnly(11, 0)),
        new DatosDeCitaEntrada(Guid.NewGuid(), null, new DateOnly(2026, 11, 2), new TimeOnly(10, 0), new TimeOnly(11, 0)),
        new DatosDeCitaEntrada(Guid.NewGuid(), (AmbitoAtencion)99, new DateOnly(2026, 11, 2), new TimeOnly(10, 0), new TimeOnly(11, 0)),
        new DatosDeCitaEntrada(Guid.NewGuid(), AmbitoAtencion.Escolar, null, new TimeOnly(10, 0), new TimeOnly(11, 0)),
        new DatosDeCitaEntrada(Guid.NewGuid(), AmbitoAtencion.Escolar, new DateOnly(2026, 11, 2), null, new TimeOnly(11, 0)),
        new DatosDeCitaEntrada(Guid.NewGuid(), AmbitoAtencion.Escolar, new DateOnly(2026, 11, 2), new TimeOnly(10, 0), null)
    };

    // Un campo que no llegó no se confunde con 0001-01-01, 00:00 ni el primer ámbito: se rechaza como solicitud inválida antes de leer nada.
    [Theory]
    [MemberData(nameof(EntradasIncompletasOInvalidas))]
    public async Task Agendar_ConUnCampoFaltanteOInvalido_EsSolicitudInvalidaSinLeerNada(DatosDeCitaEntrada datos)
    {
        var respuesta = await _servicio.AgendarAsync(Guid.NewGuid(), datos);

        Assert.False(respuesta.Exito);
        Assert.Equal(MensajesDeCitas.SolicitudInvalida, respuesta.Mensaje);
        AssertSinEfectos();
    }

    [Theory]
    [InlineData(10, 0, 10, 30, MensajesDeHorarioDeCita.DuracionAntesDeLas15)]
    [InlineData(8, 0, 9, 0, MensajesDeHorarioDeCita.FueraDelHorarioDeAtencion)]
    [InlineData(18, 30, 19, 30, MensajesDeHorarioDeCita.FueraDelHorarioDeAtencion)]
    [InlineData(11, 0, 10, 0, MensajesDeHorarioDeCita.FinNoPosteriorAlInicio)]
    [InlineData(15, 0, 15, 45, MensajesDeHorarioDeCita.DuracionDesdeLas15)]
    public async Task Agendar_ConHorarioInvalido_RechazaConElMensajeFijoDeDomainSinLeerNada(
        int horaInicio, int minutoInicio, int horaFin, int minutoFin, string esperado)
    {
        var respuesta = await _servicio.AgendarAsync(Guid.NewGuid(), Datos(horaInicio, minutoInicio, horaFin, minutoFin));

        Assert.False(respuesta.Exito);
        Assert.Equal(esperado, respuesta.Mensaje);
        AssertSinEfectos();
    }

    [Fact]
    public async Task Agendar_ConSegundosEnLaHora_RechazaSinLeerNada()
    {
        var datos = Datos() with { HoraInicio = new TimeOnly(10, 0, 30) };

        var respuesta = await _servicio.AgendarAsync(Guid.NewGuid(), datos);

        Assert.Equal(MensajesDeHorarioDeCita.HorasConSegundos, respuesta.Mensaje);
        AssertSinEfectos();
    }

    // El último día representable: el instante UTC del fin pasaría de 9999. Debe rechazarse de forma controlada, sin dejar escapar ArgumentOutOfRangeException.
    [Fact]
    public async Task Agendar_ConElUltimoDiaRepresentableYUnFinQueNoCabe_RechazaDeFormaControlada()
    {
        var datos = Datos(17, 0, 18, 0, fecha: DateOnly.MaxValue);

        var respuesta = await _servicio.AgendarAsync(Guid.NewGuid(), datos);

        Assert.False(respuesta.Exito);
        Assert.Equal(MensajesDeHorarioDeCita.FechaNoRepresentable, respuesta.Mensaje);
        AssertSinEfectos();
    }

    // ----- Agendar: paciente y ámbito -----

    [Fact]
    public async Task Agendar_ConPacienteInexistente_RechazaSinGuardarNada()
    {
        var respuesta = await _servicio.AgendarAsync(Guid.NewGuid(), Datos(paciente: Guid.NewGuid()));

        Assert.False(respuesta.Exito);
        Assert.Equal(MensajesDeCitas.PacienteNoEncontrado, respuesta.Mensaje);
        Assert.Empty(_citas.Citas);
        Assert.Empty(_repositorioAuditoria.Eventos);
        Assert.Equal(0, _unidad.Confirmaciones);
    }

    [Fact]
    public async Task Agendar_ConPacienteDeBaja_RechazaConElTextoDeBajaSinGuardarNada()
    {
        var deBaja = _pacientes.Agregar("Paciente Baja", CondicionRegistro.Baja);

        var respuesta = await _servicio.AgendarAsync(Guid.NewGuid(), Datos(paciente: deBaja));

        Assert.False(respuesta.Exito);
        Assert.Equal(MensajesDeCitas.PacienteDeBaja, respuesta.Mensaje);
        Assert.Equal("Está dada de baja.", respuesta.Mensaje);
        Assert.Empty(_citas.Citas);
        Assert.Empty(_repositorioAuditoria.Eventos);
    }

    [Fact]
    public async Task Agendar_ConUnAmbitoQueElPacienteNoTiene_RechazaSinGuardarNada()
    {
        var soloEscolar = _pacientes.Agregar("Solo Escolar", CondicionRegistro.Vigente, AmbitoAtencion.Escolar);

        var respuesta = await _servicio.AgendarAsync(Guid.NewGuid(), Datos(ambito: AmbitoAtencion.Independiente, paciente: soloEscolar));

        Assert.False(respuesta.Exito);
        Assert.Equal(MensajesDeCitas.AmbitoNoPermitido, respuesta.Mensaje);
        Assert.Empty(_citas.Citas);
        Assert.Empty(_repositorioAuditoria.Eventos);
    }

    [Theory]
    [InlineData(AmbitoAtencion.Escolar)]
    [InlineData(AmbitoAtencion.Independiente)]
    public async Task Agendar_ConCadaAmbitoDelPaciente_Guarda(AmbitoAtencion ambito)
    {
        var respuesta = await _servicio.AgendarAsync(Guid.NewGuid(), Datos(ambito: ambito));

        Assert.True(respuesta.Exito);
        Assert.Equal(ambito, respuesta.Datos!.Cita.Ambito);
    }

    // D11: tres mensajes fijos distintos, porque quien agenda ya tiene permiso para consultar pacientes.
    [Fact]
    public void LosTresMensajesDeRechazoDelPaciente_SonDistintos()
    {
        var mensajes = new[] { MensajesDeCitas.PacienteNoEncontrado, MensajesDeCitas.PacienteDeBaja, MensajesDeCitas.AmbitoNoPermitido };

        Assert.Equal(3, mensajes.Distinct().Count());
    }

    // ----- Agendar: guardado, evento y resultado -----

    [Fact]
    public async Task Agendar_ConDatosValidos_GuardaLaCitaYSuEventoJuntosYDevuelveElDetalle()
    {
        var id = Guid.NewGuid();

        var respuesta = await _servicio.AgendarAsync(id, Datos());

        Assert.True(respuesta.Exito);
        Assert.Equal(MensajesDeCitas.CitaAgendada, respuesta.Mensaje);
        Assert.Equal(EstadoDeAgendar.Agendada, respuesta.Datos!.Estado);
        var detalle = respuesta.Datos.Cita;
        Assert.Equal(id, detalle.IdCita);
        Assert.Equal(new DateTime(2026, 11, 2, 10, 0, 0), detalle.InicioLocal);
        Assert.Equal(new DateTime(2026, 11, 2, 11, 0, 0), detalle.FinLocal);
        Assert.Equal(AmbitoAtencion.Escolar, detalle.Ambito);
        Assert.Equal(EstadoCita.Agendada, detalle.Estado);
        Assert.Equal(_idPaciente, detalle.IdPaciente);
        Assert.Equal("Paciente Ficticio", detalle.NombreDelPaciente);
        Assert.False(detalle.PacienteDeBaja);
        Assert.False(detalle.RegistradaDespuesDeSuHorario);
        Assert.Equal(EstadoPago.Pendiente, detalle.EstadoPago);
        Assert.Equal(1, detalle.Version);

        var guardada = Assert.Single(_citas.Citas).Value;
        Assert.Equal(Utc(2026, 11, 2, 17), guardada.Inicio);
        Assert.Equal(Utc(2026, 11, 2, 18), guardada.Fin);
        Assert.Equal(_idActor, guardada.IdUsuarioAlta);
        Assert.Equal(EstadoPago.Pendiente, guardada.EstadoPago);
        Assert.Equal(1, _unidad.Confirmaciones);

        var evento = Assert.Single(_repositorioAuditoria.Eventos);
        Assert.Equal(TipoRegistroAuditoria.Cita, evento.TipoRegistro);
        Assert.Equal(AccionAuditoria.Alta, evento.Accion);
        Assert.Equal(id, evento.IdRegistro);
        Assert.Equal(_idActor, evento.IdUsuarioActor);
        Assert.Equal("Se agendó una cita.", evento.Hecho);
    }

    [Fact]
    public async Task Agendar_ElEventoLlevaSoloNombresDeCamposYNingunValorPrivado()
    {
        var respuesta = await _servicio.AgendarAsync(Guid.NewGuid(), Datos());
        Assert.True(respuesta.Exito);

        var evento = Assert.Single(_repositorioAuditoria.Eventos);
        Assert.Equal(["IdPaciente", "Ambito", "Inicio", "Fin", "Estado", "EstadoPago"], evento.CamposAfectados);
        var texto = evento.Hecho + string.Join(' ', evento.CamposAfectados);
        Assert.DoesNotContain("Paciente Ficticio", texto);
        Assert.DoesNotContain(_idPaciente.ToString(), texto);
        Assert.DoesNotContain("2026", texto);
        Assert.DoesNotContain("10:00", texto);
    }

    // Decisión de Lucía del 01-nov-2026: una fecha u hora pasada se permite; el evento lo indica con un texto fijo y la cita con una leyenda calculada.
    [Fact]
    public async Task Agendar_ConFechaPasada_SePermiteConLeyendaYEventoDeRegistroTardio()
    {
        var respuesta = await _servicio.AgendarAsync(Guid.NewGuid(), Datos(fecha: new DateOnly(2026, 10, 1)));

        Assert.True(respuesta.Exito);
        Assert.True(respuesta.Datos!.Cita.RegistradaDespuesDeSuHorario);
        Assert.Equal("Se agendó una cita registrada después de su horario.", Assert.Single(_repositorioAuditoria.Eventos).Hecho);
    }

    // La hora de la leyenda es la del servidor, nunca la de la página: la entrada no tiene ningún campo de hora de registro.
    [Fact]
    public async Task Agendar_ElMismoDiaPeroAntesDeLaHora_NoLlevaLaLeyenda()
    {
        // Ahora son las 08:00 de Hermosillo; una cita a las 9:00 de hoy todavía no empezó.
        var respuesta = await _servicio.AgendarAsync(Guid.NewGuid(), Datos(9, 0, 10, 0, fecha: Hoy));

        Assert.True(respuesta.Exito);
        Assert.False(respuesta.Datos!.Cita.RegistradaDespuesDeSuHorario);
    }

    [Fact]
    public async Task Agendar_ElMismoDiaYaIniciada_LlevaLaLeyendaConLaHoraDelServidor()
    {
        var servicioTarde = Crear(Utc(2026, 10, 5, 19)); // 12:00 de Hermosillo.

        var respuesta = await servicioTarde.AgendarAsync(Guid.NewGuid(), Datos(9, 0, 10, 0, fecha: Hoy));

        Assert.True(respuesta.Exito);
        Assert.True(respuesta.Datos!.Cita.RegistradaDespuesDeSuHorario);
    }

    // ----- Reenvío: reconocimiento en la primera lectura -----

    [Fact]
    public async Task Agendar_ReenvioDeLaMismaReservaYaConfirmada_ReconoceYaAgendadaSinOtraCitaNiEvento()
    {
        var id = Guid.NewGuid();
        var primera = await _servicio.AgendarAsync(id, Datos());

        var segunda = await _servicio.AgendarAsync(id, Datos());

        Assert.True(segunda.Exito);
        Assert.Equal(EstadoDeAgendar.YaAgendada, segunda.Datos!.Estado);
        Assert.Equal(MensajesDeCitas.CitaYaAgendada, segunda.Mensaje);
        Assert.Equal(primera.Datos!.Cita, segunda.Datos.Cita);
        Assert.Single(_citas.Citas);
        Assert.Single(_repositorioAuditoria.Eventos);
        Assert.Equal(1, _unidad.Confirmaciones);

        // Reconocerla no vuelve a comprobar al paciente ni al ámbito.
        Assert.Equal(1, _pacientes.ConsultasDeCondicion);
    }

    public enum Variante
    {
        OtroActor,
        OtrosDatos,
        OtroPaciente,
        OtroAmbito,
        Cancelada,
        DeBaja
    }

    // Una cita con el mismo número que no es la misma reserva activa del mismo actor: ya existe, pero no se puede reconocer ni se muestra.
    private Cita ExistenteNoReconocible(Guid id, Variante variante)
    {
        var cita = variante switch
        {
            Variante.OtroActor => Gemela(id, actor: OtroUsuario),
            Variante.OtrosDatos => Gemela(id, horaInicio: 12, horaFin: 13),
            Variante.OtroPaciente => Gemela(id, paciente: _pacientes.Agregar("Otra Persona")),
            Variante.OtroAmbito => Gemela(id, ambito: AmbitoAtencion.Independiente),
            _ => Gemela(id)
        };

        if (variante == Variante.Cancelada)
        {
            ReglasDeCita.Cancelar(cita, cita.Version, _idActor, Ahora);
        }

        if (variante == Variante.DeBaja)
        {
            ReglasDeCita.DarDeBaja(cita, cita.Version, _idActor, Ahora);
        }

        return cita;
    }

    [Theory]
    [InlineData(Variante.OtroActor)]
    [InlineData(Variante.OtrosDatos)]
    [InlineData(Variante.OtroPaciente)]
    [InlineData(Variante.OtroAmbito)]
    [InlineData(Variante.Cancelada)]
    [InlineData(Variante.DeBaja)]
    public async Task Agendar_ConElMismoNumeroQueNoEsLaMismaReservaActiva_EsSolicitudYaUsadaSinMostrarLaOtraCita(Variante variante)
    {
        var id = Guid.NewGuid();
        ExistenteNoReconocible(id, variante);

        var respuesta = await _servicio.AgendarAsync(id, Datos());

        Assert.False(respuesta.Exito);
        Assert.Equal(MensajesDeCitas.SolicitudYaUsada, respuesta.Mensaje);
        Assert.Null(respuesta.Datos);
        Assert.Empty(_repositorioAuditoria.Eventos);
        Assert.Equal(0, _unidad.Confirmaciones);
        Assert.Single(_citas.Citas);
    }

    // Paciente o ámbito pudieron cambiar después de la reserva original: el reenvío de una cita existente no repite esas comprobaciones.
    [Fact]
    public async Task Agendar_ReenvioConElPacienteYaDadoDeBaja_ReconoceLaReservaSinRepetirLaComprobacion()
    {
        var id = Guid.NewGuid();
        Gemela(id);
        _pacientes.Pacientes[_idPaciente] = (CondicionRegistro.Baja, "Paciente Ficticio", []);

        var respuesta = await _servicio.AgendarAsync(id, Datos());

        Assert.True(respuesta.Exito);
        Assert.Equal(EstadoDeAgendar.YaAgendada, respuesta.Datos!.Estado);
        Assert.Equal(0, _pacientes.ConsultasDeCondicion);
        Assert.Empty(_repositorioAuditoria.Eventos);
    }

    [Fact]
    public async Task Agendar_ReenvioConElAmbitoYaRetiradoDelPaciente_ReconoceLaReserva()
    {
        var id = Guid.NewGuid();
        Gemela(id, ambito: AmbitoAtencion.Independiente);
        _pacientes.Pacientes[_idPaciente] = (CondicionRegistro.Vigente, "Paciente Ficticio", [AmbitoAtencion.Escolar]);

        var respuesta = await _servicio.AgendarAsync(id, Datos(ambito: AmbitoAtencion.Independiente));

        Assert.Equal(EstadoDeAgendar.YaAgendada, respuesta.Datos!.Estado);
        Assert.Equal(AmbitoAtencion.Independiente, respuesta.Datos.Cita.Ambito);
        Assert.Equal(0, _pacientes.ConsultasDeCondicion);
    }

    // ----- Reenvío: la gemela termina entre la primera lectura y el rechazo del paciente o ámbito -----

    // La gemela confirma su cita y una tercera solicitud retira el ámbito del paciente antes de que A compruebe al paciente. A debe reconocer su propia reserva
    // activa con los mismos datos, sin restaurar el ámbito ni crear otro evento.
    [Fact]
    public async Task Agendar_LaGemelaConfirmaYSeRetiraElAmbitoDespuesDeLaPrimeraLectura_ReconoceLaReservaPropia()
    {
        var id = Guid.NewGuid();
        _pacientes.AntesDeConsultarCondicion = () =>
        {
            Gemela(id);
            _pacientes.Pacientes[_idPaciente] = (CondicionRegistro.Vigente, "Paciente Ficticio", [AmbitoAtencion.Independiente]);
        };

        var respuesta = await _servicio.AgendarAsync(id, Datos(ambito: AmbitoAtencion.Escolar));

        Assert.True(respuesta.Exito);
        Assert.Equal(EstadoDeAgendar.YaAgendada, respuesta.Datos!.Estado);
        Assert.Equal(MensajesDeCitas.CitaYaAgendada, respuesta.Mensaje);
        Assert.Single(_citas.Citas);
        Assert.Empty(_repositorioAuditoria.Eventos);
        Assert.Equal(0, _unidad.Confirmaciones);
        Assert.Equal([AmbitoAtencion.Independiente], _pacientes.Pacientes[_idPaciente].Ambitos);
        Assert.Equal(2, _citas.LecturasParaComparacion);
    }

    // Sin cita confirmada, el ámbito retirado sí rechaza el alta.
    [Fact]
    public async Task Agendar_SeRetiraElAmbitoDespuesDeLaPrimeraLecturaYNadieConfirmoLaCita_RechazaElAmbito()
    {
        _pacientes.AntesDeConsultarCondicion = () =>
            _pacientes.Pacientes[_idPaciente] = (CondicionRegistro.Vigente, "Paciente Ficticio", [AmbitoAtencion.Independiente]);

        var respuesta = await _servicio.AgendarAsync(Guid.NewGuid(), Datos(ambito: AmbitoAtencion.Escolar));

        Assert.False(respuesta.Exito);
        Assert.Equal(MensajesDeCitas.AmbitoNoPermitido, respuesta.Mensaje);
        Assert.Empty(_citas.Citas);
        Assert.Equal(2, _citas.LecturasParaComparacion);
    }

    [Fact]
    public async Task Agendar_LaGemelaConfirmaYElPacienteDejaDeExistirParaLaComprobacion_ReconoceLaReservaPropia()
    {
        var id = Guid.NewGuid();
        _pacientes.AntesDeConsultarCondicion = () =>
        {
            Gemela(id);
            _pacientes.Pacientes.Remove(_idPaciente);
        };

        var respuesta = await _servicio.AgendarAsync(id, Datos());

        Assert.Equal(EstadoDeAgendar.YaAgendada, respuesta.Datos!.Estado);
    }

    [Fact]
    public async Task Agendar_LaGemelaConfirmaYElPacienteSeDaDeBajaDespuesDeLaPrimeraLectura_ConOtroActorNoSeReconoce()
    {
        var id = Guid.NewGuid();
        _pacientes.AntesDeConsultarCondicion = () =>
        {
            Gemela(id, actor: OtroUsuario);
            _pacientes.Pacientes[_idPaciente] = (CondicionRegistro.Baja, "Paciente Ficticio", []);
        };

        var respuesta = await _servicio.AgendarAsync(id, Datos());

        Assert.False(respuesta.Exito);
        Assert.Equal(MensajesDeCitas.SolicitudYaUsada, respuesta.Mensaje);
    }

    // Retirar el permiso mientras tanto responde SinPermiso, también en la relectura, y no revela si la reserva existía.
    [Fact]
    public async Task Agendar_SeRetiraElPermisoAntesDeLaRelectura_RespondeSinPermiso()
    {
        var id = Guid.NewGuid();
        _pacientes.AntesDeConsultarCondicion = () =>
        {
            Gemela(id);
            _pacientes.Pacientes[_idPaciente] = (CondicionRegistro.Vigente, "Paciente Ficticio", [AmbitoAtencion.Independiente]);
            _autorizacion.Permisos.Remove(Permiso.AgendarCitas);
        };

        var respuesta = await _servicio.AgendarAsync(id, Datos(ambito: AmbitoAtencion.Escolar));

        Assert.False(respuesta.Exito);
        Assert.Equal(MensajesDeCitas.SinPermiso, respuesta.Mensaje);
        Assert.Null(respuesta.Datos);
        Assert.Equal(1, _citas.LecturasParaComparacion);
    }

    // ----- Reenvío: la gemela termina antes del guardado -----

    public static TheoryData<int> ExcepcionesQueRechazanElGuardado() => new() { 0, 1, 2 };

    private static Exception ExcepcionDeLaBase(int cual) => cual switch
    {
        0 => new IdentificadorDeCitaRepetidoException(),
        1 => new CruceDeHorarioException(),
        _ => new CitaActivaConPacienteDeBajaException()
    };

    [Theory]
    [MemberData(nameof(ExcepcionesQueRechazanElGuardado))]
    public async Task Agendar_LaGemelaConfirmaAntesDelGuardado_ReconoceLaReservaSinOtraCitaNiEvento(int cual)
    {
        var id = Guid.NewGuid();
        _citas.AlAgregar = _ =>
        {
            Gemela(id);
            throw ExcepcionDeLaBase(cual);
        };

        var respuesta = await _servicio.AgendarAsync(id, Datos());

        Assert.True(respuesta.Exito);
        Assert.Equal(EstadoDeAgendar.YaAgendada, respuesta.Datos!.Estado);
        Assert.Single(_citas.Citas);
        Assert.Empty(_repositorioAuditoria.Eventos);
        Assert.Equal(1, _unidad.Reversiones);
        Assert.Equal(0, _unidad.Confirmaciones);
        Assert.Equal(2, _citas.LecturasParaComparacion);
    }

    [Theory]
    [InlineData(0, Variante.OtroActor)]
    [InlineData(0, Variante.OtrosDatos)]
    [InlineData(0, Variante.Cancelada)]
    [InlineData(0, Variante.DeBaja)]
    [InlineData(1, Variante.OtroActor)]
    [InlineData(1, Variante.OtrosDatos)]
    [InlineData(2, Variante.OtroAmbito)]
    [InlineData(2, Variante.OtroPaciente)]
    public async Task Agendar_ElMismoNumeroYaExistePeroNoSePuedeReconocer_EsSolicitudYaUsada(int cual, Variante variante)
    {
        var id = Guid.NewGuid();
        _citas.AlAgregar = _ =>
        {
            ExistenteNoReconocible(id, variante);
            throw ExcepcionDeLaBase(cual);
        };

        var respuesta = await _servicio.AgendarAsync(id, Datos());

        Assert.False(respuesta.Exito);
        Assert.Equal(MensajesDeCitas.SolicitudYaUsada, respuesta.Mensaje);
        Assert.Empty(_repositorioAuditoria.Eventos);
        Assert.Equal(0, _unidad.Confirmaciones);
    }

    // Otra reserva con otro número por el mismo horario: la relectura por número no encuentra nada y el rechazo original se conserva.
    [Fact]
    public async Task Agendar_ElCruceSinCitaConEseNumero_RespondeHorarioOcupado()
    {
        _citas.AlAgregar = _ => throw new CruceDeHorarioException();

        var respuesta = await _servicio.AgendarAsync(Guid.NewGuid(), Datos());

        Assert.False(respuesta.Exito);
        Assert.Equal(MensajesDeCitas.HorarioOcupado, respuesta.Mensaje);
        Assert.Empty(_repositorioAuditoria.Eventos);
        Assert.Equal(0, _unidad.Confirmaciones);
        Assert.Equal(2, _citas.LecturasParaComparacion);
    }

    // Dos altas conflictivas simultáneas pueden bloquearse entre sí: la base aborta una por interbloqueo y el alta lo trata como el conflicto de horario.
    [Fact]
    public async Task Agendar_ElInterbloqueoSinCitaConEseNumero_RespondeHorarioOcupado()
    {
        _citas.AlAgregar = _ => throw new InterbloqueoDeBaseException();

        var respuesta = await _servicio.AgendarAsync(Guid.NewGuid(), Datos());

        Assert.False(respuesta.Exito);
        Assert.Equal(MensajesDeCitas.HorarioOcupado, respuesta.Mensaje);
        Assert.Empty(_repositorioAuditoria.Eventos);
        Assert.Equal(0, _unidad.Confirmaciones);
        Assert.Equal(2, _citas.LecturasParaComparacion);
    }

    [Fact]
    public async Task Agendar_ElInterbloqueoCuandoLaGemelaYaConfirmo_ReconoceLaReserva()
    {
        var id = Guid.NewGuid();
        _citas.AlAgregar = _ =>
        {
            Gemela(id);
            throw new InterbloqueoDeBaseException();
        };

        var respuesta = await _servicio.AgendarAsync(id, Datos());

        Assert.True(respuesta.Exito);
        Assert.Equal(EstadoDeAgendar.YaAgendada, respuesta.Datos!.Estado);
        Assert.Single(_citas.Citas);
        Assert.Empty(_repositorioAuditoria.Eventos);
    }

    [Fact]
    public async Task Agendar_ElInterbloqueoCuandoSeRetiraElPermiso_RespondeSinPermiso()
    {
        _citas.AlAgregar = _ =>
        {
            _autorizacion.Permisos.Remove(Permiso.AgendarCitas);
            throw new InterbloqueoDeBaseException();
        };

        var respuesta = await _servicio.AgendarAsync(Guid.NewGuid(), Datos());

        Assert.Equal(MensajesDeCitas.SinPermiso, respuesta.Mensaje);
    }

    [Fact]
    public async Task Agendar_LaBajaSimultaneaSinCitaConEseNumero_RespondeConElTextoDeBaja()
    {
        _citas.AlAgregar = _ => throw new CitaActivaConPacienteDeBajaException();

        var respuesta = await _servicio.AgendarAsync(Guid.NewGuid(), Datos());

        Assert.False(respuesta.Exito);
        Assert.Equal(MensajesDeCitas.PacienteDeBaja, respuesta.Mensaje);
        Assert.Empty(_repositorioAuditoria.Eventos);
    }

    [Fact]
    public async Task Agendar_ClaveRepetidaSinNingunaCita_EsUnErrorInesperadoYNoUnRechazo()
    {
        _citas.AlAgregar = _ => throw new IdentificadorDeCitaRepetidoException();

        await Assert.ThrowsAsync<InvalidOperationException>(() => _servicio.AgendarAsync(Guid.NewGuid(), Datos()));

        Assert.Empty(_repositorioAuditoria.Eventos);
        Assert.Equal(0, _unidad.Confirmaciones);
    }

    [Fact]
    public async Task Agendar_SeRetiraElPermisoAntesDeLaRelecturaDelGuardado_RespondeSinPermiso()
    {
        var id = Guid.NewGuid();
        _citas.AlAgregar = _ =>
        {
            Gemela(id);
            _autorizacion.Permisos.Remove(Permiso.ConsultarPacientes);
            throw new CruceDeHorarioException();
        };

        var respuesta = await _servicio.AgendarAsync(id, Datos());

        Assert.False(respuesta.Exito);
        Assert.Equal(MensajesDeCitas.SinPermiso, respuesta.Mensaje);
        Assert.Null(respuesta.Datos);
    }

    // ----- Fallos inesperados -----

    // Un fallo de auditoría no es un rechazo: sube como excepción, no confirma nada, no anuncia éxito y no relee.
    [Fact]
    public async Task Agendar_ConFalloDeAuditoria_SubeLaExcepcionSinConfirmarNiRelerNiAnunciarExito()
    {
        _repositorioAuditoria.ExcepcionAlAgregar = new InvalidOperationException("fallo de auditoría simulado");

        await Assert.ThrowsAsync<InvalidOperationException>(() => _servicio.AgendarAsync(Guid.NewGuid(), Datos()));

        Assert.Equal(0, _unidad.Confirmaciones);
        Assert.Equal(1, _unidad.Reversiones);
        Assert.Empty(_repositorioAuditoria.Eventos);
        Assert.Equal(1, _citas.LecturasParaComparacion);
    }

    [Fact]
    public async Task Agendar_ConUnErrorInesperadoDeLaBase_SubeLaExcepcionSinRelerNiAnunciarExito()
    {
        _citas.AlAgregar = _ => throw new TimeoutException();

        await Assert.ThrowsAsync<TimeoutException>(() => _servicio.AgendarAsync(Guid.NewGuid(), Datos()));

        Assert.Equal(0, _unidad.Confirmaciones);
        Assert.Equal(1, _citas.LecturasParaComparacion);
        Assert.Empty(_repositorioAuditoria.Eventos);
    }

    [Fact]
    public async Task Agendar_SiPacientesNiegaElPermisoAlComprobar_RespondeSinPermisoSinGuardar()
    {
        _pacientes.SinPermiso = true;

        var respuesta = await _servicio.AgendarAsync(Guid.NewGuid(), Datos());

        Assert.Equal(MensajesDeCitas.SinPermiso, respuesta.Mensaje);
        Assert.Empty(_citas.Citas);
        Assert.Equal(0, _unidad.Confirmaciones);
    }

    // ----- Agenda -----

    public static TheoryData<DateOnly?, DateOnly?> PeriodosInvalidos() => new()
    {
        { null, new DateOnly(2026, 10, 10) },
        { new DateOnly(2026, 10, 10), null },
        { new DateOnly(2026, 10, 10), new DateOnly(2026, 10, 9) },
        { new DateOnly(2026, 10, 1), new DateOnly(2026, 11, 5) },
        { new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31) },
        { DateOnly.MaxValue, DateOnly.MaxValue },
        { DateOnly.MaxValue.AddDays(-1), DateOnly.MaxValue }
    };

    [Theory]
    [MemberData(nameof(PeriodosInvalidos))]
    public async Task ConsultarAgenda_ConUnPeriodoInvalido_RechazaSinConsultarDatos(DateOnly? desde, DateOnly? hasta)
    {
        AgregarCita(Utc(2026, 10, 6, 17), Utc(2026, 10, 6, 18));

        var respuesta = await _servicio.ConsultarAgendaAsync(desde, hasta);

        Assert.False(respuesta.Exito);
        Assert.Equal(MensajesDeCitas.PeriodoInvalido, respuesta.Mensaje);
        Assert.Equal(0, _citas.Lecturas);
        Assert.Equal(0, _pacientes.ConsultasDeNombres);
    }

    [Fact]
    public async Task ConsultarAgenda_ConUnaVistaDesconocida_RechazaSinConsultarDatos()
    {
        var respuesta = await _servicio.ConsultarAgendaAsync(Hoy, Hoy, (VistaDeAgenda)99);

        Assert.False(respuesta.Exito);
        Assert.Equal(MensajesDeCitas.PeriodoInvalido, respuesta.Mensaje);
        Assert.Equal(0, _citas.Lecturas);
    }

    [Fact]
    public async Task ConsultarAgenda_SinFechas_ConsultaHoyYLosSeisDiasSiguientes()
    {
        var respuesta = await _servicio.ConsultarAgendaAsync(null, null);

        Assert.True(respuesta.Exito);
        Assert.Equal(Hoy, respuesta.Datos!.Desde);
        Assert.Equal(new DateOnly(2026, 10, 11), respuesta.Datos.Hasta);
        Assert.Equal(Hoy, respuesta.Datos.HoyEnHermosillo);
        Assert.Equal(VistaDeAgenda.Activas, respuesta.Datos.Vista);

        // Medianoche de Hermosillo (07:00 UTC) del primer día a la medianoche del día siguiente al último.
        var consulta = _citas.UltimaConsultaDeAgenda!.Value;
        Assert.Equal(Utc(2026, 10, 5, 7), consulta.Desde);
        Assert.Equal(Utc(2026, 10, 12, 7), consulta.Hasta);
        Assert.Equal(LimitesDeAgenda.MaximoDeFilas + 1, consulta.Limite);
    }

    [Fact]
    public async Task ConsultarAgenda_HoyEnHermosilloNoEsElDiaUtc()
    {
        // 03:00 UTC del 6-oct son las 20:00 del 5-oct en Hermosillo.
        var servicio = Crear(Utc(2026, 10, 6, 3));

        var respuesta = await servicio.ConsultarAgendaAsync(null, null);

        Assert.Equal(new DateOnly(2026, 10, 5), respuesta.Datos!.HoyEnHermosillo);
        Assert.Equal(new DateOnly(2026, 10, 5), respuesta.Datos.Desde);
    }

    [Fact]
    public async Task ConsultarAgenda_DeUnSoloDiaYDeTreintaYCincoDias_SeAceptan()
    {
        var unDia = await _servicio.ConsultarAgendaAsync(Hoy, Hoy);

        Assert.True(unDia.Exito);
        Assert.Equal((Utc(2026, 10, 5, 7), Utc(2026, 10, 6, 7)), (_citas.UltimaConsultaDeAgenda!.Value.Desde, _citas.UltimaConsultaDeAgenda.Value.Hasta));

        // Del 1 de octubre al 4 de noviembre son 35 días con ambos extremos incluidos.
        var maximo = await _servicio.ConsultarAgendaAsync(new DateOnly(2026, 10, 1), new DateOnly(2026, 11, 4));

        Assert.True(maximo.Exito);
        Assert.Equal((Utc(2026, 10, 1, 7), Utc(2026, 11, 5, 7)), (_citas.UltimaConsultaDeAgenda!.Value.Desde, _citas.UltimaConsultaDeAgenda.Value.Hasta));
    }

    [Fact]
    public async Task ConsultarAgenda_SinCitas_EsCorrectaConLaListaVaciaYNoPideNombres()
    {
        var respuesta = await _servicio.ConsultarAgendaAsync(null, null);

        Assert.True(respuesta.Exito);
        Assert.Empty(respuesta.Datos!.Citas);
        Assert.False(respuesta.Datos.HayMas);
        Assert.Equal(0, _pacientes.ConsultasDeNombres);
    }

    [Fact]
    public async Task ConsultarAgenda_ConvierteLasHorasAHermosilloEnElServidor()
    {
        var cita = AgregarCita(Utc(2026, 10, 6, 21, 30), Utc(2026, 10, 6, 22, 30));

        var respuesta = await _servicio.ConsultarAgendaAsync(null, null);

        var fila = Assert.Single(respuesta.Datos!.Citas);
        Assert.Equal(cita.IdCita, fila.IdCita);
        Assert.Equal(new DateTime(2026, 10, 6, 14, 30, 0), fila.InicioLocal);
        Assert.Equal(new DateTime(2026, 10, 6, 15, 30, 0), fila.FinLocal);
        Assert.Equal("Paciente Ficticio", fila.NombreDelPaciente);
        Assert.Equal(_idPaciente, fila.IdPaciente);
        Assert.False(fila.PacienteDeBaja);
        Assert.Equal(EstadoCita.Agendada, fila.Estado);
    }

    [Fact]
    public async Task ConsultarAgenda_OrdenaPorInicioYMuestraLasQueSeSolapanConElPeriodo()
    {
        var tarde = AgregarCita(Utc(2026, 10, 7, 20), Utc(2026, 10, 7, 21));
        var temprano = AgregarCita(Utc(2026, 10, 6, 17), Utc(2026, 10, 6, 18));

        // Empieza antes del periodo y termina dentro: se solapa.
        var empiezaAntes = AgregarCita(Utc(2026, 10, 5, 6, 30), Utc(2026, 10, 5, 7, 30));

        // Termina justo cuando empieza el periodo: queda fuera. Empieza justo cuando termina: queda fuera.
        AgregarCita(Utc(2026, 10, 5, 6), Utc(2026, 10, 5, 7));
        AgregarCita(Utc(2026, 10, 12, 7), Utc(2026, 10, 12, 8));

        var respuesta = await _servicio.ConsultarAgendaAsync(null, null);

        Assert.Equal([empiezaAntes.IdCita, temprano.IdCita, tarde.IdCita], respuesta.Datos!.Citas.Select(c => c.IdCita));
    }

    [Fact]
    public async Task ConsultarAgenda_SeparaLasVistasYNuncaMuestraLasDeBaja()
    {
        var activa = AgregarCita(Utc(2026, 10, 6, 17), Utc(2026, 10, 6, 18));
        var cancelada = AgregarCita(Utc(2026, 10, 6, 19), Utc(2026, 10, 6, 20));
        ReglasDeCita.Cancelar(cancelada, cancelada.Version, _idActor, Ahora);
        var deBaja = AgregarCita(Utc(2026, 10, 6, 21), Utc(2026, 10, 6, 22));
        ReglasDeCita.DarDeBaja(deBaja, deBaja.Version, _idActor, Ahora);

        var activas = await _servicio.ConsultarAgendaAsync(null, null, VistaDeAgenda.Activas);
        var canceladas = await _servicio.ConsultarAgendaAsync(null, null, VistaDeAgenda.Canceladas);

        Assert.Equal([activa.IdCita], activas.Datos!.Citas.Select(c => c.IdCita));
        Assert.Equal([cancelada.IdCita], canceladas.Datos!.Citas.Select(c => c.IdCita));
        Assert.Equal(VistaDeAgenda.Canceladas, canceladas.Datos.Vista);
    }

    [Fact]
    public async Task ConsultarAgenda_UnaCanceladaDeUnPacienteDeBajaNoRevelaNombreNiIdentificador()
    {
        var deBaja = _pacientes.Agregar("Nombre Privado", CondicionRegistro.Baja);
        var cancelada = AgregarCita(Utc(2026, 10, 6, 17), Utc(2026, 10, 6, 18), paciente: deBaja);
        ReglasDeCita.Cancelar(cancelada, cancelada.Version, _idActor, Ahora);

        var respuesta = await _servicio.ConsultarAgendaAsync(null, null, VistaDeAgenda.Canceladas);

        var fila = Assert.Single(respuesta.Datos!.Citas);
        Assert.True(fila.PacienteDeBaja);
        Assert.Null(fila.NombreDelPaciente);
        Assert.Null(fila.IdPaciente);
        Assert.Equal(new DateTime(2026, 10, 6, 10, 0, 0), fila.InicioLocal);
        Assert.Equal(AmbitoAtencion.Escolar, fila.Ambito);
        Assert.Equal(EstadoCita.Cancelada, fila.Estado);
        Assert.DoesNotContain("Nombre Privado", fila.ToString());
    }

    [Fact]
    public async Task ConsultarAgenda_PideLosNombresUnaSolaVezSinRepetirPacientes()
    {
        AgregarCita(Utc(2026, 10, 6, 17), Utc(2026, 10, 6, 18));
        AgregarCita(Utc(2026, 10, 6, 19), Utc(2026, 10, 6, 20));
        var otro = _pacientes.Agregar("Otra Persona");
        AgregarCita(Utc(2026, 10, 7, 17), Utc(2026, 10, 7, 18), paciente: otro);

        var respuesta = await _servicio.ConsultarAgendaAsync(null, null);

        Assert.Equal(3, respuesta.Datos!.Citas.Count);
        Assert.Equal(1, _pacientes.ConsultasDeNombres);
        Assert.Equal(2, _pacientes.UltimosIdsDeNombres!.Length);
    }

    [Fact]
    public async Task ConsultarAgenda_ConMasDeQuinientasFilas_DevuelveQuinientasYAvisaQueHayMas()
    {
        var comienzo = Utc(2026, 10, 6, 15);
        for (var i = 0; i < LimitesDeAgenda.MaximoDeFilas + 1; i++)
        {
            AgregarCita(comienzo.AddMinutes(i), comienzo.AddMinutes(i + 1));
        }

        var respuesta = await _servicio.ConsultarAgendaAsync(null, null);

        Assert.Equal(LimitesDeAgenda.MaximoDeFilas, respuesta.Datos!.Citas.Count);
        Assert.True(respuesta.Datos.HayMas);
    }

    [Fact]
    public async Task ConsultarAgenda_ConExactamenteQuinientasFilas_NoAvisaQueHayMas()
    {
        var comienzo = Utc(2026, 10, 6, 15);
        for (var i = 0; i < LimitesDeAgenda.MaximoDeFilas; i++)
        {
            AgregarCita(comienzo.AddMinutes(i), comienzo.AddMinutes(i + 1));
        }

        var respuesta = await _servicio.ConsultarAgendaAsync(null, null);

        Assert.Equal(LimitesDeAgenda.MaximoDeFilas, respuesta.Datos!.Citas.Count);
        Assert.False(respuesta.Datos.HayMas);
    }

    [Fact]
    public async Task ConsultarAgenda_LaLeyendaDeRegistroTardioSaleDeLaFechaDeAltaDelServidor()
    {
        var puntual = AgregarCita(Utc(2026, 10, 6, 17), Utc(2026, 10, 6, 18), alta: Utc(2026, 10, 5, 15));
        var tarde = AgregarCita(Utc(2026, 10, 6, 19), Utc(2026, 10, 6, 20), alta: Utc(2026, 10, 6, 19, 5));

        var respuesta = await _servicio.ConsultarAgendaAsync(null, null);

        var filas = respuesta.Datos!.Citas.ToDictionary(c => c.IdCita);
        Assert.False(filas[puntual.IdCita].RegistradaDespuesDeSuHorario);
        Assert.True(filas[tarde.IdCita].RegistradaDespuesDeSuHorario);
    }

    [Fact]
    public async Task ConsultarAgenda_SiPacientesNiegaElPermisoAlPedirNombres_RespondeSinPermisoYSinFilas()
    {
        AgregarCita(Utc(2026, 10, 6, 17), Utc(2026, 10, 6, 18));
        _pacientes.SinPermiso = true;

        var respuesta = await _servicio.ConsultarAgendaAsync(null, null);

        Assert.False(respuesta.Exito);
        Assert.Equal(MensajesDeCitas.SinPermiso, respuesta.Mensaje);
        Assert.Null(respuesta.Datos);
    }

    // ----- Detalle -----

    [Fact]
    public async Task ObtenerCita_DeUnaCitaVigente_DevuelveElDetalleConPagoYVersion()
    {
        var cita = Gemela(Guid.NewGuid());

        var respuesta = await _servicio.ObtenerCitaAsync(cita.IdCita);

        Assert.True(respuesta.Exito);
        var detalle = respuesta.Datos!;
        Assert.Equal(cita.IdCita, detalle.IdCita);
        Assert.Equal(new DateTime(2026, 11, 2, 10, 0, 0), detalle.InicioLocal);
        Assert.Equal(EstadoPago.Pendiente, detalle.EstadoPago);
        Assert.Equal(1, detalle.Version);
        Assert.Equal("Paciente Ficticio", detalle.NombreDelPaciente);
    }

    [Fact]
    public async Task ObtenerCita_UnaCanceladaVigenteSeConsulta_ConSuEstado()
    {
        var cita = Gemela(Guid.NewGuid());
        ReglasDeCita.Cancelar(cita, cita.Version, _idActor, Ahora);

        var respuesta = await _servicio.ObtenerCitaAsync(cita.IdCita);

        Assert.Equal(EstadoCita.Cancelada, respuesta.Datos!.Estado);
        Assert.Equal(2, respuesta.Datos.Version);
    }

    [Fact]
    public async Task ObtenerCita_DeUnaCitaDeBajaOInexistente_RespondeLoMismo()
    {
        var deBaja = Gemela(Guid.NewGuid());
        ReglasDeCita.DarDeBaja(deBaja, deBaja.Version, _idActor, Ahora);

        var deBajaRespuesta = await _servicio.ObtenerCitaAsync(deBaja.IdCita);
        var inexistente = await _servicio.ObtenerCitaAsync(Guid.NewGuid());
        var vacia = await _servicio.ObtenerCitaAsync(Guid.Empty);

        foreach (var respuesta in new[] { deBajaRespuesta, inexistente, vacia })
        {
            Assert.False(respuesta.Exito);
            Assert.Equal(MensajesDeCitas.CitaNoEncontrada, respuesta.Mensaje);
            Assert.Null(respuesta.Datos);
        }
    }

    [Fact]
    public async Task ObtenerCita_DeUnPacienteDeBaja_NoRevelaNombreNiIdentificador()
    {
        var deBaja = _pacientes.Agregar("Nombre Privado", CondicionRegistro.Baja);
        var cita = Gemela(Guid.NewGuid(), paciente: deBaja);
        ReglasDeCita.Cancelar(cita, cita.Version, _idActor, Ahora);

        var respuesta = await _servicio.ObtenerCitaAsync(cita.IdCita);

        Assert.True(respuesta.Datos!.PacienteDeBaja);
        Assert.Null(respuesta.Datos.NombreDelPaciente);
        Assert.Null(respuesta.Datos.IdPaciente);
    }

    // ----- Los textos de los modelos no imprimen valores -----

    [Fact]
    public void ToString_DeLosModelos_NoImprimeValoresPersonales()
    {
        var detalle = new DetalleDeCita(
            Guid.NewGuid(), Ahora.DateTime, Ahora.DateTime, AmbitoAtencion.Escolar, EstadoCita.Agendada, Guid.NewGuid(), "Nombre Privado", false, false,
            EstadoPago.Pendiente, 1);
        var entrada = Datos();

        Assert.Equal(nameof(DetalleDeCita), detalle.ToString());
        Assert.Equal(nameof(DatosDeCitaEntrada), entrada.ToString());
        Assert.DoesNotContain("Nombre Privado", new ResultadoDeAgendar(EstadoDeAgendar.Agendada, detalle).ToString());
    }
}
