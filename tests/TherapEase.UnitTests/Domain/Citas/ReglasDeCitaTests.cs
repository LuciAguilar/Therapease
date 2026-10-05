using TherapEase.Domain.Citas.Entidades;
using TherapEase.Domain.Citas.Entidades.Enums;
using TherapEase.Domain.Citas.Reglas;
using TherapEase.Domain.Compartido.Entidades.Enums;
using TherapEase.Domain.Compartido.Excepciones;
using TherapEase.Domain.Pacientes.Entidades.Enums;

namespace TherapEase.UnitTests.Domain.Citas;

public class ReglasDeCitaTests
{
    private static readonly Guid Actor = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly DateTimeOffset Ahora = new(2026, 10, 5, 15, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Inicio = new(2026, 11, 2, 17, 0, 0, TimeSpan.Zero);

    private static Cita NuevaCita() =>
        ReglasDeCita.Agendar(Guid.NewGuid(), Guid.NewGuid(), AmbitoAtencion.Escolar, Inicio, Inicio.AddHours(1), Actor, Ahora);

    [Fact]
    public void Agendar_QuedaAgendadaVigenteConPagoPendienteYActiva()
    {
        var cita = NuevaCita();

        Assert.Equal(EstadoCita.Agendada, cita.Estado);
        Assert.Equal(CondicionRegistro.Vigente, cita.Condicion);
        Assert.Equal(EstadoPago.Pendiente, cita.EstadoPago);
        Assert.Equal(1, cita.Version);
        Assert.True(cita.EsActiva);
        Assert.Equal(Actor, cita.IdUsuarioAlta);
    }

    [Theory]
    [InlineData(EstadoCita.Agendada, CondicionRegistro.Vigente, true)]
    [InlineData(EstadoCita.Agendada, CondicionRegistro.Baja, false)]
    [InlineData(EstadoCita.Cancelada, CondicionRegistro.Vigente, false)]
    [InlineData(EstadoCita.Cancelada, CondicionRegistro.Baja, false)]
    public void EsActiva_SoloSiEstaAgendadaYVigente(EstadoCita estado, CondicionRegistro condicion, bool esperado)
    {
        var cita = NuevaCita();
        if (estado == EstadoCita.Cancelada)
        {
            ReglasDeCita.Cancelar(cita, 1, Actor, Ahora);
        }

        if (condicion == CondicionRegistro.Baja)
        {
            ReglasDeCita.DarDeBaja(cita, cita.Version, Actor, Ahora);
        }

        Assert.Equal(esperado, cita.EsActiva);
    }

    [Fact]
    public void Agendar_ConInicioIgualOPosteriorAlFin_Rechaza()
    {
        Assert.Throws<ReglaDeNegocioException>(() =>
            ReglasDeCita.Agendar(Guid.NewGuid(), Guid.NewGuid(), AmbitoAtencion.Escolar, Inicio, Inicio, Actor, Ahora));
        Assert.Throws<ReglaDeNegocioException>(() =>
            ReglasDeCita.Agendar(Guid.NewGuid(), Guid.NewGuid(), AmbitoAtencion.Escolar, Inicio.AddHours(1), Inicio, Actor, Ahora));
    }

    [Fact]
    public void Agendar_ConPacienteOCitaSinIdentificador_Rechaza()
    {
        Assert.Throws<ReglaDeNegocioException>(() =>
            ReglasDeCita.Agendar(Guid.NewGuid(), Guid.Empty, AmbitoAtencion.Escolar, Inicio, Inicio.AddHours(1), Actor, Ahora));
        Assert.Throws<ReglaDeNegocioException>(() =>
            ReglasDeCita.Agendar(Guid.Empty, Guid.NewGuid(), AmbitoAtencion.Escolar, Inicio, Inicio.AddHours(1), Actor, Ahora));
    }

    [Fact]
    public void Agendar_ConAmbitoInexistente_Rechaza()
    {
        Assert.Throws<ReglaDeNegocioException>(() =>
            ReglasDeCita.Agendar(Guid.NewGuid(), Guid.NewGuid(), (AmbitoAtencion)99, Inicio, Inicio.AddHours(1), Actor, Ahora));
    }

    [Fact]
    public void Agendar_NormalizaInstantesAUtc()
    {
        var inicioLocal = new DateTimeOffset(2026, 11, 2, 10, 0, 0, TimeSpan.FromHours(-7));

        var cita = ReglasDeCita.Agendar(Guid.NewGuid(), Guid.NewGuid(), AmbitoAtencion.Escolar, inicioLocal, inicioLocal.AddHours(1), Actor, Ahora);

        Assert.Equal(TimeSpan.Zero, cita.Inicio.Offset);
        Assert.Equal(TimeSpan.Zero, cita.Fin.Offset);
        Assert.Equal(inicioLocal, cita.Inicio);
    }

    [Fact]
    public void Cancelar_LiberaHorarioConservaPagoYNoLaDaDeBaja()
    {
        var cita = NuevaCita();

        ReglasDeCita.Cancelar(cita, 1, Actor, Ahora);

        Assert.Equal(EstadoCita.Cancelada, cita.Estado);
        Assert.False(cita.EsActiva);
        Assert.Equal(EstadoPago.Pendiente, cita.EstadoPago);
        Assert.Equal(CondicionRegistro.Vigente, cita.Condicion);
        Assert.Null(cita.FechaBaja);
        Assert.Equal(2, cita.Version);
    }

    [Fact]
    public void Cancelar_UnaCitaYaCanceladaODeBaja_Rechaza()
    {
        var cancelada = NuevaCita();
        ReglasDeCita.Cancelar(cancelada, 1, Actor, Ahora);
        var deBaja = NuevaCita();
        ReglasDeCita.DarDeBaja(deBaja, 1, Actor, Ahora);

        Assert.Throws<ReglaDeNegocioException>(() => ReglasDeCita.Cancelar(cancelada, 2, Actor, Ahora));
        Assert.Throws<ReglaDeNegocioException>(() => ReglasDeCita.Cancelar(deBaja, 2, Actor, Ahora));
    }

    [Fact]
    public void Cancelar_ConVersionAnterior_LanzaConflicto()
    {
        var cita = NuevaCita();

        Assert.Throws<ConflictoDeVersionException>(() => ReglasDeCita.Cancelar(cita, 5, Actor, Ahora));
        Assert.Equal(EstadoCita.Agendada, cita.Estado);
    }

    [Fact]
    public void DarDeBaja_ConservaEstadoYPagoYRegistraFechaYUsuario()
    {
        var cita = NuevaCita();
        ReglasDeCita.Cancelar(cita, 1, Actor, Ahora);

        ReglasDeCita.DarDeBaja(cita, 2, Actor, Ahora);

        Assert.Equal(CondicionRegistro.Baja, cita.Condicion);
        Assert.Equal(EstadoCita.Cancelada, cita.Estado);
        Assert.Equal(EstadoPago.Pendiente, cita.EstadoPago);
        Assert.Equal(Ahora, cita.FechaBaja);
        Assert.Equal(Actor, cita.IdUsuarioBaja);
    }

    [Fact]
    public void DarDeBaja_UnaCitaYaDeBaja_InformaSinDuplicar()
    {
        var cita = NuevaCita();
        ReglasDeCita.DarDeBaja(cita, 1, Actor, Ahora);

        Assert.Throws<ReglaDeNegocioException>(() => ReglasDeCita.DarDeBaja(cita, 2, Actor, Ahora));
        Assert.Equal(2, cita.Version);
    }

    [Fact]
    public void Recuperar_UnaCanceladaVuelveVigentePeroSigueCancelada()
    {
        var cita = NuevaCita();
        ReglasDeCita.Cancelar(cita, 1, Actor, Ahora);
        ReglasDeCita.DarDeBaja(cita, 2, Actor, Ahora);

        ReglasDeCita.Recuperar(cita, 3, Actor, Ahora);

        Assert.Equal(CondicionRegistro.Vigente, cita.Condicion);
        Assert.Equal(EstadoCita.Cancelada, cita.Estado);
        Assert.False(cita.EsActiva);
        Assert.Null(cita.FechaBaja);
        Assert.Null(cita.IdUsuarioBaja);
    }

    [Fact]
    public void Recuperar_UnaAgendadaVuelveActiva()
    {
        var cita = NuevaCita();
        ReglasDeCita.DarDeBaja(cita, 1, Actor, Ahora);

        ReglasDeCita.Recuperar(cita, 2, Actor, Ahora);

        Assert.True(cita.EsActiva);
        Assert.Equal(3, cita.Version);
    }

    [Fact]
    public void Recuperar_UnaCitaYaVigente_Informa()
    {
        var cita = NuevaCita();

        Assert.Throws<ReglaDeNegocioException>(() => ReglasDeCita.Recuperar(cita, 1, Actor, Ahora));
    }
}
