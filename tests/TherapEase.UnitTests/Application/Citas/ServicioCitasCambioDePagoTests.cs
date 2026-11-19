using TherapEase.Application.Auditoria.Servicios;
using TherapEase.Application.Citas.Constantes;
using TherapEase.Application.Citas.Modelos;
using TherapEase.Application.Citas.Servicios;
using TherapEase.Application.Compartido.Interfaces.Repositorios;
using TherapEase.Application.Pacientes.Constantes;
using TherapEase.Domain.Auditoria.Entidades.Enums;
using TherapEase.Domain.Citas.Constantes;
using TherapEase.Domain.Citas.Entidades;
using TherapEase.Domain.Citas.Entidades.Enums;
using TherapEase.Domain.Citas.Reglas;
using TherapEase.Domain.Compartido.Entidades.Enums;
using TherapEase.Domain.Compartido.Excepciones;
using TherapEase.Domain.Pacientes.Entidades.Enums;
using TherapEase.UnitTests.Dobles;

namespace TherapEase.UnitTests.Application.Citas;

public class ServicioCitasCambioDePagoTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 10, 5, 15, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Inicio = new(2026, 11, 2, 17, 0, 0, TimeSpan.Zero);
    private static readonly Guid QuienAgendo = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private const string HechoEsperado = "Se cambió el estado de pago de una cita.";

    private readonly RepositorioCitasFalso _citas = new();
    private readonly ConsultaDePacientesParaCitasFalsa _pacientes = new();
    private readonly UnidadDeTrabajoFalsa _unidad = new();
    private readonly RepositorioAuditoriaFalso _repositorioAuditoria = new();
    private readonly AutorizacionFalsa _autorizacion = new();
    private readonly UsuarioActualFalso _actual = new();
    private readonly Guid _idActor = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly ServicioCitas _servicio;

    public ServicioCitasCambioDePagoTests()
    {
        _actual.IdUsuario = _idActor;
        _autorizacion.Permisos.UnionWith([Permiso.CambiarPagoDeCitas, Permiso.ConsultarAgenda, Permiso.ConsultarPacientes]);
        _servicio = Crear(_unidad);
    }

    private ServicioCitas Crear(IUnidadDeTrabajo unidad)
    {
        var reloj = new RelojFijo(Ahora);
        var auditoria = new ServicioAuditoria(_repositorioAuditoria, _autorizacion, reloj);
        return new ServicioCitas(_citas, _pacientes, unidad, auditoria, _autorizacion, _actual, reloj);
    }

    // Guarda directo en el doble, sin pasar por el servicio ni por sus eventos.
    private Cita Guardar(EstadoPago pago = EstadoPago.Pendiente, bool cancelada = false, bool deBaja = false)
    {
        var cita = ReglasDeCita.Agendar(Guid.NewGuid(), Guid.NewGuid(), AmbitoAtencion.Escolar, Inicio, Inicio.AddHours(1), QuienAgendo, Ahora);
        if (pago == EstadoPago.Pagado)
        {
            ReglasDeCita.CambiarPago(cita, EstadoPago.Pagado, cita.Version, QuienAgendo, Ahora);
        }

        if (cancelada)
        {
            ReglasDeCita.Cancelar(cita, cita.Version, QuienAgendo, Ahora);
        }

        if (deBaja)
        {
            ReglasDeCita.DarDeBaja(cita, cita.Version, QuienAgendo, Ahora);
        }

        _citas.Citas[cita.IdCita] = cita;
        return cita;
    }

    private void AssertSinEfectos(Cita? cita = null)
    {
        Assert.Equal(0, _unidad.Confirmaciones);
        Assert.Empty(_repositorioAuditoria.Eventos);
        Assert.Equal(0, _pacientes.ConsultasDeCondicion);
        Assert.Equal(0, _pacientes.ConsultasDeNombres);
        if (cita is not null)
        {
            Assert.Null(cita.IdUsuarioActualizacion);
        }
    }

    // ----- Permisos -----

    [Theory]
    [InlineData(Permiso.CambiarPagoDeCitas)]
    [InlineData(Permiso.ConsultarAgenda)]
    [InlineData(Permiso.ConsultarPacientes)]
    public async Task CambiarPago_SinAlgunoDeLosTresPermisos_RechazaAntesDeLeerNada(Permiso faltante)
    {
        var cita = Guardar();
        _autorizacion.Permisos.Remove(faltante);

        var respuesta = await _servicio.CambiarPagoAsync(cita.IdCita, 1, EstadoPago.Pagado);

        Assert.False(respuesta.Exito);
        Assert.Equal(MensajesDeCitas.SinPermiso, respuesta.Mensaje);
        Assert.Null(respuesta.Datos);
        Assert.Equal(0, _citas.Lecturas);
        Assert.Equal(EstadoPago.Pendiente, cita.EstadoPago);
        Assert.Equal(1, cita.Version);
        AssertSinEfectos(cita);
    }

    [Fact]
    public async Task CambiarPago_SinSesion_RechazaAntesDeLeerNada()
    {
        var cita = Guardar();
        _actual.IdUsuario = null;

        var respuesta = await _servicio.CambiarPagoAsync(cita.IdCita, 1, EstadoPago.Pagado);

        Assert.Equal(MensajesDeCitas.SinPermiso, respuesta.Mensaje);
        Assert.Equal(0, _citas.Lecturas);
        AssertSinEfectos(cita);
    }

    // Quien no tiene permiso no averigua si la cita existe ni si su entrada era válida: la respuesta es la misma en todos los casos.
    [Theory]
    [InlineData(false, 1, EstadoPago.Pagado)]
    [InlineData(true, 0, EstadoPago.Pagado)]
    [InlineData(true, 1, null)]
    [InlineData(true, 1, (EstadoPago)7)]
    public async Task CambiarPago_SinPermiso_RespondeIgualConEntradaInvalidaOCitaInexistente(bool existe, int version, EstadoPago? pago)
    {
        var idCita = existe ? Guardar().IdCita : Guid.NewGuid();
        _autorizacion.Permisos.Remove(Permiso.CambiarPagoDeCitas);

        var respuesta = await _servicio.CambiarPagoAsync(idCita, version, pago);

        Assert.Equal(MensajesDeCitas.SinPermiso, respuesta.Mensaje);
        Assert.Equal(0, _citas.Lecturas);
    }

    [Fact]
    public async Task CambiarPago_SinPermisoYConIdentificadorVacio_RespondeSinPermisoYNoQueNoSeEncontro()
    {
        _autorizacion.Permisos.Remove(Permiso.CambiarPagoDeCitas);

        var respuesta = await _servicio.CambiarPagoAsync(Guid.Empty, 1, EstadoPago.Pagado);

        Assert.Equal(MensajesDeCitas.SinPermiso, respuesta.Mensaje);
    }

    // ----- Validación sin tocar la base -----

    [Fact]
    public async Task CambiarPago_ConIdentificadorVacio_DiceQueNoSeEncontroSinLeerNada()
    {
        var respuesta = await _servicio.CambiarPagoAsync(Guid.Empty, 1, EstadoPago.Pagado);

        Assert.False(respuesta.Exito);
        Assert.Equal(MensajesDeCitas.CitaNoEncontrada, respuesta.Mensaje);
        Assert.Equal(0, _citas.Lecturas);
        AssertSinEfectos();
    }

    [Fact]
    public async Task CambiarPago_SinPago_NoSeToma_PorPendiente()
    {
        var cita = Guardar(EstadoPago.Pagado);

        var respuesta = await _servicio.CambiarPagoAsync(cita.IdCita, cita.Version, null);

        Assert.Equal(MensajesDeCitas.SolicitudInvalida, respuesta.Mensaje);
        Assert.Equal(EstadoPago.Pagado, cita.EstadoPago);
        Assert.Equal(0, _citas.Lecturas);
        Assert.Empty(_repositorioAuditoria.Eventos);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(-1)]
    [InlineData(99)]
    public async Task CambiarPago_ConUnPagoQueNoEsPendienteNiPagado_SeRechazaSinLeerNada(int numero)
    {
        var cita = Guardar();

        var respuesta = await _servicio.CambiarPagoAsync(cita.IdCita, 1, (EstadoPago)numero);

        Assert.Equal(MensajesDeCitas.SolicitudInvalida, respuesta.Mensaje);
        Assert.Equal(0, _citas.Lecturas);
        AssertSinEfectos(cita);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public async Task CambiarPago_ConVersionAusenteOInvalida_EsSolicitudInvalidaYNoConflicto(int version)
    {
        var cita = Guardar();

        var respuesta = await _servicio.CambiarPagoAsync(cita.IdCita, version, EstadoPago.Pagado);

        Assert.Equal(MensajesDeCitas.SolicitudInvalida, respuesta.Mensaje);
        Assert.Equal(0, _citas.Lecturas);
        AssertSinEfectos(cita);
    }

    [Fact]
    public async Task CambiarPago_ConUnaCitaInexistente_DiceQueNoSeEncontro()
    {
        var respuesta = await _servicio.CambiarPagoAsync(Guid.NewGuid(), 1, EstadoPago.Pagado);

        Assert.False(respuesta.Exito);
        Assert.Equal(MensajesDeCitas.CitaNoEncontrada, respuesta.Mensaje);
        Assert.Null(respuesta.Datos);
        Assert.Equal(1, _citas.LecturasParaEscribir);
        AssertSinEfectos();
    }

    // ----- Cambio confirmado -----

    [Fact]
    public async Task CambiarPago_DePendienteAPagado_GuardaElCambioYUnSoloEventoSinValores()
    {
        var cita = Guardar();

        var respuesta = await _servicio.CambiarPagoAsync(cita.IdCita, 1, EstadoPago.Pagado);

        Assert.True(respuesta.Exito);
        Assert.Equal(MensajesDeCitas.PagoActualizado, respuesta.Mensaje);
        var resultado = Assert.IsType<ResultadoDeCambioDePago>(respuesta.Datos);
        Assert.Equal(EstadoDeCambioDePago.Actualizado, resultado.Estado);
        Assert.Equal(new EstadoDePagoDeCita(cita.IdCita, EstadoPago.Pagado, EstadoCita.Agendada, 2), resultado.Pago);

        Assert.Equal(EstadoPago.Pagado, cita.EstadoPago);
        Assert.Equal(2, cita.Version);
        Assert.Equal(_idActor, cita.IdUsuarioActualizacion);
        Assert.Equal(Ahora, cita.FechaActualizacion);
        Assert.Equal(1, _unidad.Confirmaciones);
        Assert.Equal(0, _unidad.Reversiones);

        var evento = Assert.Single(_repositorioAuditoria.Eventos);
        Assert.Equal(TipoRegistroAuditoria.Cita, evento.TipoRegistro);
        Assert.Equal(AccionAuditoria.Actualizacion, evento.Accion);
        Assert.Equal(cita.IdCita, evento.IdRegistro);
        Assert.Equal(_idActor, evento.IdUsuarioActor);
        Assert.Equal(Ahora, evento.FechaEvento);
        Assert.Equal(["EstadoPago"], evento.CamposAfectados);
        Assert.Equal(HechoEsperado, evento.Hecho);
    }

    [Fact]
    public async Task CambiarPago_DePagadoAPendiente_UsaElMismoHechoQueElOtroSentido()
    {
        var cita = Guardar(EstadoPago.Pagado);
        var versionPagada = cita.Version;

        var respuesta = await _servicio.CambiarPagoAsync(cita.IdCita, versionPagada, EstadoPago.Pendiente);

        Assert.Equal(MensajesDeCitas.PagoActualizado, respuesta.Mensaje);
        Assert.Equal(new EstadoDePagoDeCita(cita.IdCita, EstadoPago.Pendiente, EstadoCita.Agendada, versionPagada + 1), respuesta.Datos!.Pago);
        var evento = Assert.Single(_repositorioAuditoria.Eventos);
        Assert.Equal(HechoEsperado, evento.Hecho);
        Assert.DoesNotContain("Pagado", evento.Hecho);
        Assert.DoesNotContain("Pendiente", evento.Hecho);
        Assert.Equal(["EstadoPago"], evento.CamposAfectados);
    }

    [Fact]
    public async Task CambiarPago_EnUnaCitaCancelada_SeCorrigeYSigueCancelada()
    {
        var cita = Guardar(cancelada: true);

        var respuesta = await _servicio.CambiarPagoAsync(cita.IdCita, cita.Version, EstadoPago.Pagado);

        Assert.True(respuesta.Exito);
        Assert.Equal(EstadoCita.Cancelada, respuesta.Datos!.Pago.Estado);
        Assert.Equal(EstadoPago.Pagado, respuesta.Datos.Pago.EstadoPago);
        Assert.Equal(EstadoCita.Cancelada, cita.Estado);
        Assert.Single(_repositorioAuditoria.Eventos);
    }

    // El actor del cambio y del evento es quien tiene la sesión, no quien agendó la cita.
    [Fact]
    public async Task CambiarPago_ElActorSaleDeLaSesion_NoDeQuienAgendo()
    {
        var cita = Guardar();
        Assert.NotEqual(_idActor, cita.IdUsuarioAlta);

        await _servicio.CambiarPagoAsync(cita.IdCita, 1, EstadoPago.Pagado);

        Assert.Equal(_idActor, cita.IdUsuarioActualizacion);
        Assert.Equal(QuienAgendo, cita.IdUsuarioAlta);
        Assert.Equal(_idActor, Assert.Single(_repositorioAuditoria.Eventos).IdUsuarioActor);
    }

    [Fact]
    public async Task CambiarPago_LeeLaCitaUnaVezYNoConsultaPacientesNiLeeDeNuevoDespuesDeConfirmar()
    {
        var cita = Guardar();

        await _servicio.CambiarPagoAsync(cita.IdCita, 1, EstadoPago.Pagado);

        Assert.Equal(1, _citas.LecturasParaEscribir);
        Assert.Equal(0, _citas.LecturasParaComparacion);
        Assert.Equal(0, _citas.ConsultasDeAgenda);
        Assert.Equal(0, _pacientes.ConsultasDeCondicion);
        Assert.Equal(0, _pacientes.ConsultasDeNombres);
    }

    [Fact]
    public async Task CambiarPago_ConservaHorarioPacienteAmbitoEstadoYCondicion()
    {
        var cita = Guardar();
        var (paciente, ambito, inicio, fin) = (cita.IdPaciente, cita.Ambito, cita.Inicio, cita.Fin);

        await _servicio.CambiarPagoAsync(cita.IdCita, 1, EstadoPago.Pagado);

        Assert.Equal(paciente, cita.IdPaciente);
        Assert.Equal(ambito, cita.Ambito);
        Assert.Equal(inicio, cita.Inicio);
        Assert.Equal(fin, cita.Fin);
        Assert.Equal(EstadoCita.Agendada, cita.Estado);
        Assert.Equal(CondicionRegistro.Vigente, cita.Condicion);
    }

    // ----- Sin cambios -----

    [Theory]
    [InlineData(EstadoPago.Pendiente)]
    [InlineData(EstadoPago.Pagado)]
    public async Task CambiarPago_ConLaVersionVigenteYElMismoPago_RespondeSinCambiosSinEscribirNiRegistrarEvento(EstadoPago pago)
    {
        var cita = Guardar(pago);
        var version = cita.Version;
        var actualizacion = cita.FechaActualizacion;
        var actor = cita.IdUsuarioActualizacion;

        var respuesta = await _servicio.CambiarPagoAsync(cita.IdCita, version, pago);

        Assert.True(respuesta.Exito);
        Assert.Equal(MensajesDeCitas.SinCambios, respuesta.Mensaje);
        Assert.Equal(EstadoDeCambioDePago.SinCambios, respuesta.Datos!.Estado);
        Assert.Equal(new EstadoDePagoDeCita(cita.IdCita, pago, EstadoCita.Agendada, version), respuesta.Datos.Pago);
        Assert.Equal(version, cita.Version);
        Assert.Equal(actualizacion, cita.FechaActualizacion);
        Assert.Equal(actor, cita.IdUsuarioActualizacion);
        Assert.Equal(0, _unidad.Confirmaciones);
        Assert.Equal(0, _unidad.Reversiones);
        Assert.Empty(_repositorioAuditoria.Eventos);
    }

    // Una versión anterior da conflicto aunque el pago ya sea el pedido: no se presenta como éxito ni como «sin cambios».
    [Theory]
    [InlineData(EstadoPago.Pendiente)]
    [InlineData(EstadoPago.Pagado)]
    public async Task CambiarPago_ConUnaVersionAnterior_DaConflictoAunqueElPagoCoincida(EstadoPago pedido)
    {
        var cita = Guardar(EstadoPago.Pagado);
        var pago = cita.EstadoPago;
        var version = cita.Version;

        var respuesta = await _servicio.CambiarPagoAsync(cita.IdCita, 1, pedido);

        Assert.False(respuesta.Exito);
        Assert.Equal(MensajesDeCitas.ConflictoDeVersion, respuesta.Mensaje);
        Assert.Null(respuesta.Datos);
        Assert.Equal(pago, cita.EstadoPago);
        Assert.Equal(version, cita.Version);
        Assert.Equal(0, _unidad.Confirmaciones);
        Assert.Empty(_repositorioAuditoria.Eventos);
    }

    // Reenvío del mismo formulario tras un éxito: un solo cambio, un solo evento, y el segundo envío no se atribuye el éxito del primero.
    [Fact]
    public async Task CambiarPago_ReenviarElMismoFormulario_DaConflictoYDejaUnSoloCambioYUnEvento()
    {
        var cita = Guardar();

        var primero = await _servicio.CambiarPagoAsync(cita.IdCita, 1, EstadoPago.Pagado);
        var segundo = await _servicio.CambiarPagoAsync(cita.IdCita, 1, EstadoPago.Pagado);

        Assert.True(primero.Exito);
        Assert.False(segundo.Exito);
        Assert.Equal(MensajesDeCitas.ConflictoDeVersion, segundo.Mensaje);
        Assert.Equal(2, cita.Version);
        Assert.Equal(1, _unidad.Confirmaciones);
        Assert.Single(_repositorioAuditoria.Eventos);
    }

    // Tras recargar, elegir el mismo valor es una acción nueva: con la versión vigente responde «No hubo cambios».
    [Fact]
    public async Task CambiarPago_TrasRecargarYElegirElMismoValor_RespondeSinCambios()
    {
        var cita = Guardar();
        await _servicio.CambiarPagoAsync(cita.IdCita, 1, EstadoPago.Pagado);

        var recargado = await _servicio.CambiarPagoAsync(cita.IdCita, cita.Version, EstadoPago.Pagado);

        Assert.True(recargado.Exito);
        Assert.Equal(EstadoDeCambioDePago.SinCambios, recargado.Datos!.Estado);
        Assert.Single(_repositorioAuditoria.Eventos);
        Assert.Equal(2, cita.Version);
    }

    [Fact]
    public async Task CambiarPago_DosEnviosQueYaCoincidenConLoGuardado_RespondenAmbosSinCambios()
    {
        var cita = Guardar(EstadoPago.Pagado);

        var primero = await _servicio.CambiarPagoAsync(cita.IdCita, cita.Version, EstadoPago.Pagado);
        var segundo = await _servicio.CambiarPagoAsync(cita.IdCita, cita.Version, EstadoPago.Pagado);

        Assert.Equal(EstadoDeCambioDePago.SinCambios, primero.Datos!.Estado);
        Assert.Equal(EstadoDeCambioDePago.SinCambios, segundo.Datos!.Estado);
        Assert.Empty(_repositorioAuditoria.Eventos);
    }

    // ----- Cita de baja -----

    [Theory]
    [InlineData(EstadoPago.Pendiente)]
    [InlineData(EstadoPago.Pagado)]
    public async Task CambiarPago_EnUnaCitaDeBaja_SeRechazaAunqueElValorSeaElMismoYNoDevuelveSuContenido(EstadoPago pedido)
    {
        var cita = Guardar(deBaja: true);
        var version = cita.Version;

        var respuesta = await _servicio.CambiarPagoAsync(cita.IdCita, version, pedido);

        Assert.False(respuesta.Exito);
        Assert.Equal(MensajesDeCitas.PagoDeCitaDeBajaNoSeActualiza, respuesta.Mensaje);
        Assert.Null(respuesta.Datos);
        Assert.Equal(EstadoPago.Pendiente, cita.EstadoPago);
        Assert.Equal(version, cita.Version);
        Assert.Equal(0, _unidad.Confirmaciones);
        Assert.Empty(_repositorioAuditoria.Eventos);
    }

    [Fact]
    public async Task CambiarPago_EnUnaCitaDeBajaConVersionAnterior_DiceQueEstaDeBajaYNoConflicto()
    {
        var cita = Guardar(deBaja: true);

        var respuesta = await _servicio.CambiarPagoAsync(cita.IdCita, 1, EstadoPago.Pagado);

        Assert.Equal(MensajesDeCitas.PagoDeCitaDeBajaNoSeActualiza, respuesta.Mensaje);
    }

    // ----- Paciente de baja y ámbito retirado: el pago es de la cita y no consulta Pacientes -----

    [Fact]
    public async Task CambiarPago_EnUnaCitaCanceladaDeUnPacienteDeBaja_SeCorrigeSinConsultarAPacientes()
    {
        var cita = Guardar(cancelada: true);
        _pacientes.Agregar("Paciente Ficticio", CondicionRegistro.Baja, AmbitoAtencion.Escolar);

        var respuesta = await _servicio.CambiarPagoAsync(cita.IdCita, cita.Version, EstadoPago.Pagado);

        Assert.True(respuesta.Exito);
        Assert.Equal(EstadoDeCambioDePago.Actualizado, respuesta.Datos!.Estado);
        Assert.Equal(0, _pacientes.ConsultasDeCondicion);
        Assert.Equal(0, _pacientes.ConsultasDeNombres);
    }

    // ----- Mensajes -----

    // Las páginas comparan con las constantes de Application; las reglas de Domain llevan el mismo texto, y el de conflicto es el de Pacientes.
    [Fact]
    public void Mensajes_LosDeApplicationCoincidenConLosDeDomainYConLosDePacientes()
    {
        Assert.Equal(MensajesDePagoDeCita.CitaDeBajaNoSeActualiza, MensajesDeCitas.PagoDeCitaDeBajaNoSeActualiza);
        Assert.Equal(MensajesDeCitas.ConflictoDeVersion, new ConflictoDeVersionException().Message);
        Assert.Equal(MensajesDeCitas.ConflictoDeVersion, MensajesDePacientes.ConflictoDeVersion);
        Assert.Equal(MensajesDeCitas.SinCambios, MensajesDePacientes.SinCambios);
    }

    // ----- Guardado -----

    // La base rechaza el UPDATE porque otra operación cambió la fila entre la lectura y el guardado: la unidad de trabajo lo traduce a conflicto.
    [Fact]
    public async Task CambiarPago_SiLaFilaCambioAntesDeGuardar_DaConflictoSinExito()
    {
        var cita = Guardar();
        var servicio = Crear(new UnidadQueDetectaConflicto());

        var respuesta = await servicio.CambiarPagoAsync(cita.IdCita, 1, EstadoPago.Pagado);

        Assert.False(respuesta.Exito);
        Assert.Equal(MensajesDeCitas.ConflictoDeVersion, respuesta.Mensaje);
        Assert.Null(respuesta.Datos);
    }

    // Un fallo de auditoría u otro error de la base no se traduce a un rechazo de negocio: sube como error general y nada se confirma.
    [Fact]
    public async Task CambiarPago_SiLaAuditoriaFalla_SubeComoExcepcionYNoConfirma()
    {
        var cita = Guardar();
        _repositorioAuditoria.ExcepcionAlAgregar = new InvalidOperationException("Fallo ficticio de auditoría.");

        await Assert.ThrowsAsync<InvalidOperationException>(() => _servicio.CambiarPagoAsync(cita.IdCita, 1, EstadoPago.Pagado));

        Assert.Equal(0, _unidad.Confirmaciones);
        Assert.Equal(1, _unidad.Reversiones);
        Assert.Empty(_repositorioAuditoria.Eventos);
    }

    [Fact]
    public async Task CambiarPago_SiLaUnidadDeTrabajoFalla_SubeComoExcepcion()
    {
        var cita = Guardar();
        var servicio = Crear(new UnidadQueFalla());

        await Assert.ThrowsAsync<TimeoutException>(() => servicio.CambiarPagoAsync(cita.IdCita, 1, EstadoPago.Pagado));
    }

    // ----- Modelos -----

    [Fact]
    public void LosModelosDelPago_NoMuestranDatosAlConvertirseATexto()
    {
        var pago = new EstadoDePagoDeCita(Guid.NewGuid(), EstadoPago.Pagado, EstadoCita.Agendada, 3);

        Assert.Equal(nameof(EstadoDePagoDeCita), pago.ToString());
        Assert.Equal(nameof(ResultadoDeCambioDePago), new ResultadoDeCambioDePago(EstadoDeCambioDePago.Actualizado, pago).ToString());
    }

    private sealed class UnidadQueDetectaConflicto : IUnidadDeTrabajo
    {
        public async Task EjecutarAsync(Func<CancellationToken, Task> operacion, CancellationToken cancelacion = default)
        {
            await operacion(cancelacion);
            throw new ConflictoDeVersionException();
        }
    }

    private sealed class UnidadQueFalla : IUnidadDeTrabajo
    {
        public async Task EjecutarAsync(Func<CancellationToken, Task> operacion, CancellationToken cancelacion = default)
        {
            await operacion(cancelacion);
            throw new TimeoutException("Fallo ficticio de la base.");
        }
    }
}
