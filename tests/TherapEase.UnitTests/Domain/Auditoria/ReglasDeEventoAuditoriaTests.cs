using TherapEase.Domain.Auditoria.Entidades;
using TherapEase.Domain.Auditoria.Entidades.Enums;
using TherapEase.Domain.Auditoria.Reglas;
using TherapEase.Domain.Compartido.Excepciones;

namespace TherapEase.UnitTests.Domain.Auditoria;

public class ReglasDeEventoAuditoriaTests
{
    private static readonly Guid Actor = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly DateTimeOffset Ahora = new(2026, 10, 5, 15, 0, 0, TimeSpan.Zero);

    private static EventoAuditoria Evento(IEnumerable<string>? campos = null, string hecho = "Se actualizó la ficha del paciente") =>
        ReglasDeEventoAuditoria.Registrar(
            Guid.NewGuid(), Ahora, Actor, TipoRegistroAuditoria.Paciente, Guid.NewGuid(),
            AccionAuditoria.Actualizacion, campos ?? ["Nombre", "Contacto"], hecho);

    [Fact]
    public void Registrar_LlevaActorMomentoTipoRegistroAccionYCampos()
    {
        var idRegistro = Guid.NewGuid();

        var evento = ReglasDeEventoAuditoria.Registrar(
            Guid.NewGuid(), Ahora, Actor, TipoRegistroAuditoria.Cita, idRegistro,
            AccionAuditoria.Cancelacion, ["Estado"], "Se canceló la cita");

        Assert.Equal(Actor, evento.IdUsuarioActor);
        Assert.Equal(Ahora, evento.FechaEvento);
        Assert.Equal(TimeSpan.Zero, evento.FechaEvento.Offset);
        Assert.Equal(TipoRegistroAuditoria.Cita, evento.TipoRegistro);
        Assert.Equal(idRegistro, evento.IdRegistro);
        Assert.Equal(AccionAuditoria.Cancelacion, evento.Accion);
        Assert.Equal(["Estado"], evento.CamposAfectados);
    }

    // La acción se guarda como texto en una columna de 20 caracteres sin restricción de valores: una acción nueva no requiere migración mientras quepa.
    [Fact]
    public void Acciones_CabenEnLaColumnaDeLaBase()
    {
        Assert.All(Enum.GetNames<AccionAuditoria>(), nombre => Assert.InRange(nombre.Length, 1, 20));
    }

    [Theory]
    [InlineData(AccionAuditoria.InicioSesion, "Inició sesión.")]
    [InlineData(AccionAuditoria.CierreSesion, "Cerró sesión.")]
    [InlineData(AccionAuditoria.BloqueoAcceso, "Se bloqueó el acceso por cinco intentos fallidos.")]
    public void Registrar_ConUnaAccionDeAcceso_AceptaCamposVaciosYLaCuentaComoActorYRegistro(AccionAuditoria accion, string hecho)
    {
        var evento = ReglasDeEventoAuditoria.Registrar(
            Guid.NewGuid(), Ahora, Actor, TipoRegistroAuditoria.Usuario, Actor, accion, [], hecho);

        Assert.Equal(accion, evento.Accion);
        Assert.Equal(Actor, evento.IdUsuarioActor);
        Assert.Equal(Actor, evento.IdRegistro);
        Assert.Empty(evento.CamposAfectados);
        Assert.Equal(hecho, evento.Hecho);
    }

    [Theory]
    [InlineData("canario@ficticio.test")]
    [InlineData("Canario#Prueba1")]
    [InlineData("662 123 4567")]
    [InlineData("")]
    [InlineData("1Campo")]
    public void Registrar_ConUnValorEnLugarDeUnNombreDeCampo_Rechaza(string valor)
    {
        Assert.Throws<ReglaDeNegocioException>(() => Evento(campos: [valor]));
    }

    [Fact]
    public void Registrar_SinHechoOConHechoDemasiadoLargo_Rechaza()
    {
        Assert.Throws<ReglaDeNegocioException>(() => Evento(hecho: "   "));
        Assert.Throws<ReglaDeNegocioException>(() => Evento(hecho: new string('a', EventoAuditoria.LongitudMaximaHecho + 1)));
    }

    [Fact]
    public void Registrar_SinActorOSinRegistro_Rechaza()
    {
        Assert.Throws<ReglaDeNegocioException>(() => ReglasDeEventoAuditoria.Registrar(
            Guid.NewGuid(), Ahora, Guid.Empty, TipoRegistroAuditoria.Paciente, Guid.NewGuid(), AccionAuditoria.Alta, ["Nombre"], "Alta"));
        Assert.Throws<ReglaDeNegocioException>(() => ReglasDeEventoAuditoria.Registrar(
            Guid.NewGuid(), Ahora, Actor, TipoRegistroAuditoria.Paciente, Guid.Empty, AccionAuditoria.Alta, ["Nombre"], "Alta"));
    }

    [Fact]
    public void Registrar_ConTipoOAccionInexistente_Rechaza()
    {
        Assert.Throws<ReglaDeNegocioException>(() => ReglasDeEventoAuditoria.Registrar(
            Guid.NewGuid(), Ahora, Actor, (TipoRegistroAuditoria)99, Guid.NewGuid(), AccionAuditoria.Alta, ["Nombre"], "Alta"));
        Assert.Throws<ReglaDeNegocioException>(() => ReglasDeEventoAuditoria.Registrar(
            Guid.NewGuid(), Ahora, Actor, TipoRegistroAuditoria.Paciente, Guid.NewGuid(), (AccionAuditoria)99, ["Nombre"], "Alta"));
    }
}
