using TherapEase.Domain.Citas.Constantes;
using TherapEase.Domain.Citas.Entidades;
using TherapEase.Domain.Citas.Entidades.Enums;
using TherapEase.Domain.Citas.Reglas;
using TherapEase.Domain.Compartido.Entidades.Enums;
using TherapEase.Domain.Compartido.Excepciones;
using TherapEase.Domain.Pacientes.Entidades.Enums;

namespace TherapEase.UnitTests.Domain.Citas;

public class ReglasDeCambioDePagoTests
{
    private static readonly Guid Agendo = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Corrige = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly DateTimeOffset Alta = new(2026, 10, 5, 15, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Cambio = new(2026, 10, 6, 18, 30, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Inicio = new(2026, 11, 2, 17, 0, 0, TimeSpan.Zero);

    private static Cita NuevaCita() =>
        ReglasDeCita.Agendar(Guid.NewGuid(), Guid.NewGuid(), AmbitoAtencion.Independiente, Inicio, Inicio.AddHours(1), Agendo, Alta);

    private static Cita CitaPagada()
    {
        var cita = NuevaCita();
        Assert.True(ReglasDeCita.CambiarPago(cita, EstadoPago.Pagado, cita.Version, Agendo, Alta));
        return cita;
    }

    // ----- Cambio efectivo -----

    [Fact]
    public void CambiarPago_DePendienteAPagado_GuardaElPagoSubeLaVersionYRegistraActorYHora()
    {
        var cita = NuevaCita();

        var huboCambio = ReglasDeCita.CambiarPago(cita, EstadoPago.Pagado, 1, Corrige, Cambio);

        Assert.True(huboCambio);
        Assert.Equal(EstadoPago.Pagado, cita.EstadoPago);
        Assert.Equal(2, cita.Version);
        Assert.Equal(Cambio, cita.FechaActualizacion);
        Assert.Equal(Corrige, cita.IdUsuarioActualizacion);
    }

    [Fact]
    public void CambiarPago_DePagadoAPendiente_TambienSeCorrige()
    {
        var cita = CitaPagada();
        var versionPagada = cita.Version;

        var huboCambio = ReglasDeCita.CambiarPago(cita, EstadoPago.Pendiente, versionPagada, Corrige, Cambio);

        Assert.True(huboCambio);
        Assert.Equal(EstadoPago.Pendiente, cita.EstadoPago);
        Assert.Equal(versionPagada + 1, cita.Version);
        Assert.Equal(Corrige, cita.IdUsuarioActualizacion);
    }

    [Fact]
    public void CambiarPago_ConservaHorarioPacienteAmbitoEstadoCondicionYDatosDeAlta()
    {
        var cita = NuevaCita();
        var (paciente, ambito, inicio, fin, estado, condicion) = (cita.IdPaciente, cita.Ambito, cita.Inicio, cita.Fin, cita.Estado, cita.Condicion);

        ReglasDeCita.CambiarPago(cita, EstadoPago.Pagado, 1, Corrige, Cambio);

        Assert.Equal(paciente, cita.IdPaciente);
        Assert.Equal(ambito, cita.Ambito);
        Assert.Equal(inicio, cita.Inicio);
        Assert.Equal(fin, cita.Fin);
        Assert.Equal(estado, cita.Estado);
        Assert.Equal(condicion, cita.Condicion);
        Assert.True(cita.EsActiva);
        Assert.Equal(Alta, cita.FechaAlta);
        Assert.Equal(Agendo, cita.IdUsuarioAlta);
        Assert.Null(cita.FechaBaja);
        Assert.Null(cita.IdUsuarioBaja);
    }

    [Fact]
    public void CambiarPago_UnaCitaCanceladaAdmiteLaCorreccionYSigueCancelada()
    {
        var cita = NuevaCita();
        ReglasDeCita.Cancelar(cita, cita.Version, Agendo, Alta);

        var huboCambio = ReglasDeCita.CambiarPago(cita, EstadoPago.Pagado, cita.Version, Corrige, Cambio);

        Assert.True(huboCambio);
        Assert.Equal(EstadoPago.Pagado, cita.EstadoPago);
        Assert.Equal(EstadoCita.Cancelada, cita.Estado);
        Assert.Equal(CondicionRegistro.Vigente, cita.Condicion);
        Assert.False(cita.EsActiva);
    }

    // ----- Sin cambios -----

    [Theory]
    [InlineData(EstadoPago.Pendiente)]
    [InlineData(EstadoPago.Pagado)]
    public void CambiarPago_ConLaVersionVigenteYElMismoPago_NoTocaNada(EstadoPago actual)
    {
        var cita = NuevaCita();
        if (actual == EstadoPago.Pagado)
        {
            ReglasDeCita.CambiarPago(cita, EstadoPago.Pagado, 1, Agendo, Alta);
        }

        var version = cita.Version;
        var actualizacion = cita.FechaActualizacion;
        var actor = cita.IdUsuarioActualizacion;

        var huboCambio = ReglasDeCita.CambiarPago(cita, actual, version, Corrige, Cambio);

        Assert.False(huboCambio);
        Assert.Equal(actual, cita.EstadoPago);
        Assert.Equal(version, cita.Version);
        Assert.Equal(actualizacion, cita.FechaActualizacion);
        Assert.Equal(actor, cita.IdUsuarioActualizacion);
    }

    // ----- Versión -----

    [Theory]
    [InlineData(EstadoPago.Pendiente)]
    [InlineData(EstadoPago.Pagado)]
    public void CambiarPago_ConUnaVersionAnterior_DaConflictoAunqueElPagoCoincida(EstadoPago destino)
    {
        var cita = NuevaCita();
        ReglasDeCita.CambiarPago(cita, EstadoPago.Pagado, 1, Agendo, Alta);
        var version = cita.Version;
        var pago = cita.EstadoPago;

        Assert.Throws<ConflictoDeVersionException>(() => ReglasDeCita.CambiarPago(cita, destino, 1, Corrige, Cambio));

        Assert.Equal(pago, cita.EstadoPago);
        Assert.Equal(version, cita.Version);
        Assert.Equal(Alta, cita.FechaActualizacion);
        Assert.Equal(Agendo, cita.IdUsuarioActualizacion);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(int.MaxValue)]
    public void CambiarPago_ConUnaVersionQueNoEsLaVigente_DaConflicto(int versionEsperada)
    {
        var cita = NuevaCita();

        Assert.Throws<ConflictoDeVersionException>(() => ReglasDeCita.CambiarPago(cita, EstadoPago.Pagado, versionEsperada, Corrige, Cambio));

        Assert.Equal(EstadoPago.Pendiente, cita.EstadoPago);
        Assert.Equal(1, cita.Version);
    }

    // ----- Cita de baja -----

    [Theory]
    [InlineData(EstadoPago.Pendiente)]
    [InlineData(EstadoPago.Pagado)]
    public void CambiarPago_EnUnaCitaDeBaja_SeRechazaAunqueElValorSeaElMismo(EstadoPago destino)
    {
        var cita = NuevaCita();
        ReglasDeCita.DarDeBaja(cita, 1, Agendo, Alta);
        var version = cita.Version;

        var error = Assert.Throws<ReglaDeNegocioException>(() => ReglasDeCita.CambiarPago(cita, destino, version, Corrige, Cambio));

        Assert.Equal(MensajesDePagoDeCita.CitaDeBajaNoSeActualiza, error.Message);
        Assert.IsNotType<ConflictoDeVersionException>(error);
        Assert.Equal(EstadoPago.Pendiente, cita.EstadoPago);
        Assert.Equal(version, cita.Version);
    }

    // La cita de baja se rechaza antes que la versión: una versión antigua en una baja no se presenta como conflicto.
    [Fact]
    public void CambiarPago_EnUnaCitaDeBajaConVersionAnterior_DiceQueEstaDeBajaYNoConflicto()
    {
        var cita = NuevaCita();
        ReglasDeCita.DarDeBaja(cita, 1, Agendo, Alta);

        var error = Assert.Throws<ReglaDeNegocioException>(() => ReglasDeCita.CambiarPago(cita, EstadoPago.Pagado, 1, Corrige, Cambio));

        Assert.Equal(MensajesDePagoDeCita.CitaDeBajaNoSeActualiza, error.Message);
        Assert.IsNotType<ConflictoDeVersionException>(error);
    }

    [Fact]
    public void CambiarPago_EnUnaCitaCanceladaYDeBaja_SeRechaza()
    {
        var cita = NuevaCita();
        ReglasDeCita.Cancelar(cita, 1, Agendo, Alta);
        ReglasDeCita.DarDeBaja(cita, cita.Version, Agendo, Alta);

        var error = Assert.Throws<ReglaDeNegocioException>(() => ReglasDeCita.CambiarPago(cita, EstadoPago.Pagado, cita.Version, Corrige, Cambio));

        Assert.Equal(MensajesDePagoDeCita.CitaDeBajaNoSeActualiza, error.Message);
    }

    // ----- Entradas inválidas -----

    [Theory]
    [InlineData(2)]
    [InlineData(-1)]
    [InlineData(99)]
    public void CambiarPago_ConUnValorQueNoEsPendienteNiPagado_SeRechazaSinCambiarNada(int numero)
    {
        var cita = NuevaCita();

        var error = Assert.Throws<ReglaDeNegocioException>(() => ReglasDeCita.CambiarPago(cita, (EstadoPago)numero, 1, Corrige, Cambio));

        Assert.Equal(MensajesDePagoDeCita.PagoNoValido, error.Message);
        Assert.Equal(EstadoPago.Pendiente, cita.EstadoPago);
        Assert.Equal(1, cita.Version);
    }

    [Theory]
    [InlineData(EstadoPago.Pendiente)]
    [InlineData(EstadoPago.Pagado)]
    public void CambiarPago_SinActor_SeRechazaSinCambiarNada(EstadoPago destino)
    {
        var cita = NuevaCita();

        Assert.Throws<ReglaDeNegocioException>(() => ReglasDeCita.CambiarPago(cita, destino, 1, Guid.Empty, Cambio));

        Assert.Equal(EstadoPago.Pendiente, cita.EstadoPago);
        Assert.Equal(1, cita.Version);
        Assert.Null(cita.FechaActualizacion);
        Assert.Null(cita.IdUsuarioActualizacion);
    }

    [Fact]
    public void CambiarPago_SinCita_Lanza()
    {
        Assert.Throws<ArgumentNullException>(() => ReglasDeCita.CambiarPago(null!, EstadoPago.Pagado, 1, Corrige, Cambio));
    }

    // ----- Cancelar y dar de baja conservan el pago guardado -----

    [Theory]
    [InlineData(EstadoPago.Pendiente)]
    [InlineData(EstadoPago.Pagado)]
    public void CancelarYDarDeBajaYRecuperar_ConservanElPagoGuardado(EstadoPago pago)
    {
        var cita = NuevaCita();
        if (pago == EstadoPago.Pagado)
        {
            ReglasDeCita.CambiarPago(cita, EstadoPago.Pagado, 1, Agendo, Alta);
        }

        ReglasDeCita.Cancelar(cita, cita.Version, Corrige, Cambio);
        Assert.Equal(pago, cita.EstadoPago);

        ReglasDeCita.DarDeBaja(cita, cita.Version, Corrige, Cambio);
        Assert.Equal(pago, cita.EstadoPago);

        ReglasDeCita.Recuperar(cita, cita.Version, Corrige, Cambio);
        Assert.Equal(pago, cita.EstadoPago);
    }

    // ----- Evento -----

    [Fact]
    public void CamposDePago_SoloLlevaElNombreDelCampoDelPago()
    {
        Assert.Equal(["EstadoPago"], ReglasDeCita.CamposDePago());
    }
}
