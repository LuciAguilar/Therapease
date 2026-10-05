using TherapEase.Application.Auditoria.Constantes;
using TherapEase.Application.Auditoria.Modelos;
using TherapEase.Application.Auditoria.Servicios;
using TherapEase.Domain.Auditoria.Entidades.Enums;
using TherapEase.Domain.Compartido.Entidades.Enums;
using TherapEase.Domain.Compartido.Excepciones;
using TherapEase.UnitTests.Dobles;

namespace TherapEase.UnitTests.Application.Auditoria;

public class ServicioAuditoriaTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 10, 5, 15, 0, 0, TimeSpan.Zero);
    private static readonly Guid Actor = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private readonly RepositorioAuditoriaFalso _repositorio = new();
    private readonly AutorizacionFalsa _autorizacion = new();
    private readonly ServicioAuditoria _servicio;

    public ServicioAuditoriaTests()
    {
        _servicio = new ServicioAuditoria(_repositorio, _autorizacion, new RelojFijo(Ahora));
    }

    private static FiltroDeEventos Filtro(int limite = 100, TipoRegistroAuditoria? tipo = null, Guid? idRegistro = null) =>
        new(Ahora.AddDays(-1), Ahora.AddDays(1), tipo, idRegistro, limite);

    [Fact]
    public async Task RegistrarHechoDeCambio_AgregaElEventoConActorMomentoYRegistro()
    {
        var idRegistro = Guid.NewGuid();

        await _servicio.RegistrarHechoDeCambioAsync(Actor, TipoRegistroAuditoria.Paciente, idRegistro, AccionAuditoria.Alta, ["Nombre", "Contacto"], "Alta de paciente");

        var evento = Assert.Single(_repositorio.Eventos);
        Assert.Equal(Actor, evento.IdUsuarioActor);
        Assert.Equal(Ahora, evento.FechaEvento);
        Assert.Equal(idRegistro, evento.IdRegistro);
        Assert.Equal(["Nombre", "Contacto"], evento.CamposAfectados);
    }

    [Fact]
    public async Task RegistrarHechoDeCambio_ConUnValorEnLugarDeUnCampo_Rechaza()
    {
        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => _servicio.RegistrarHechoDeCambioAsync(
            Actor, TipoRegistroAuditoria.Paciente, Guid.NewGuid(), AccionAuditoria.Actualizacion, ["canario@ficticio.test"], "Actualización"));

        Assert.Empty(_repositorio.Eventos);
    }

    [Fact]
    public async Task Consultar_SinPermisoDeAuditoria_NoConsultaLaBase()
    {
        var respuesta = await _servicio.ConsultarEventosAutorizadosAsync(Filtro());

        Assert.False(respuesta.Exito);
        Assert.Equal(MensajesDeAuditoria.SinPermiso, respuesta.Mensaje);
        Assert.Equal(0, _repositorio.Consultas);
    }

    [Fact]
    public async Task Consultar_ConPermiso_DevuelveSoloMetadatosDelEvento()
    {
        _autorizacion.Permisos.Add(Permiso.ConsultarAuditoria);
        await _servicio.RegistrarHechoDeCambioAsync(Actor, TipoRegistroAuditoria.Cita, Guid.NewGuid(), AccionAuditoria.Cancelacion, ["Estado"], "Se canceló la cita");

        var respuesta = await _servicio.ConsultarEventosAutorizadosAsync(Filtro());

        Assert.True(respuesta.Exito);
        var evento = Assert.Single(respuesta.Datos!);
        Assert.Equal(Actor, evento.IdUsuarioActor);
        Assert.Equal(Ahora, evento.FechaEvento);
        Assert.Equal(AccionAuditoria.Cancelacion, evento.Accion);
        Assert.Equal(["Estado"], evento.CamposAfectados);
        Assert.Equal("Se canceló la cita", evento.Hecho);
    }

    [Fact]
    public async Task Consultar_FiltraPorTipoYPorRegistro()
    {
        _autorizacion.Permisos.Add(Permiso.ConsultarAuditoria);
        var objetivo = Guid.NewGuid();
        await _servicio.RegistrarHechoDeCambioAsync(Actor, TipoRegistroAuditoria.Paciente, objetivo, AccionAuditoria.Alta, ["Nombre"], "Alta");
        await _servicio.RegistrarHechoDeCambioAsync(Actor, TipoRegistroAuditoria.Cita, Guid.NewGuid(), AccionAuditoria.Alta, ["Inicio"], "Alta");

        var porTipo = await _servicio.ConsultarEventosAutorizadosAsync(Filtro(tipo: TipoRegistroAuditoria.Paciente));
        var porRegistro = await _servicio.ConsultarEventosAutorizadosAsync(Filtro(idRegistro: objetivo));

        Assert.Single(porTipo.Datos!);
        Assert.Equal(objetivo, Assert.Single(porRegistro.Datos!).IdRegistro);
    }

    [Fact]
    public async Task Consultar_PasaLosInstantesEnUtcYElLimiteAlRepositorio()
    {
        _autorizacion.Permisos.Add(Permiso.ConsultarAuditoria);
        var desde = new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.FromHours(-7));
        var hasta = new DateTimeOffset(2026, 10, 2, 0, 0, 0, TimeSpan.FromHours(-7));

        await _servicio.ConsultarEventosAutorizadosAsync(new FiltroDeEventos(desde, hasta, Limite: 50));

        var consulta = _repositorio.UltimaConsulta!.Value;
        Assert.Equal(TimeSpan.Zero, consulta.Desde.Offset);
        Assert.Equal(desde, consulta.Desde);
        Assert.Equal(50, consulta.Limite);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(501)]
    public async Task Consultar_ConLimiteFueraDeRango_Rechaza(int limite)
    {
        _autorizacion.Permisos.Add(Permiso.ConsultarAuditoria);

        var respuesta = await _servicio.ConsultarEventosAutorizadosAsync(Filtro(limite));

        Assert.False(respuesta.Exito);
        Assert.Equal(MensajesDeAuditoria.FiltroInvalido, respuesta.Mensaje);
        Assert.Equal(0, _repositorio.Consultas);
    }

    [Fact]
    public async Task Consultar_ConPeriodoInvertidoOTipoInexistente_Rechaza()
    {
        _autorizacion.Permisos.Add(Permiso.ConsultarAuditoria);

        var invertido = await _servicio.ConsultarEventosAutorizadosAsync(new FiltroDeEventos(Ahora, Ahora.AddDays(-1)));
        var vacio = await _servicio.ConsultarEventosAutorizadosAsync(new FiltroDeEventos(Ahora, Ahora));
        var tipoMalo = await _servicio.ConsultarEventosAutorizadosAsync(Filtro(tipo: (TipoRegistroAuditoria)99));

        Assert.False(invertido.Exito);
        Assert.False(vacio.Exito);
        Assert.False(tipoMalo.Exito);
        Assert.Equal(0, _repositorio.Consultas);
    }
}
