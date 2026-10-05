using TherapEase.Application.Auditoria.Servicios;
using TherapEase.Application.Identidad.Constantes;
using TherapEase.Application.Identidad.Servicios;
using TherapEase.Domain.Auditoria.Entidades.Enums;
using TherapEase.Domain.Identidad.Constantes;
using TherapEase.UnitTests.Dobles;

namespace TherapEase.UnitTests.Application.Identidad;

public class ServicioContrasenaTests
{
    private const string ContrasenaActual = "contrasena-actual-123";
    private const string ContrasenaNueva = "contrasena-nueva-segura-456";

    private readonly GestorIdentidadFalso _gestor = new();
    private readonly EmisorDeSesionFalso _emisor = new();
    private readonly UnidadDeTrabajoFalsa _unidad = new();
    private readonly RepositorioAuditoriaFalso _repositorioAuditoria = new();
    private readonly UsuarioActualFalso _actual = new();
    private readonly ServicioContrasena _servicio;

    public ServicioContrasenaTests()
    {
        var auditoria = new ServicioAuditoria(_repositorioAuditoria, new AutorizacionFalsa(), new RelojFijo(new DateTimeOffset(2026, 10, 5, 15, 0, 0, TimeSpan.Zero)));
        _servicio = new ServicioContrasena(_gestor, _emisor, _unidad, auditoria, _actual);
    }

    [Fact]
    public async Task CambioOrdinario_ConLaActualCorrecta_CambiaAuditaYReemiteLaSesion()
    {
        var usuario = _gestor.Agregar("usuaria1", NombresDeRol.Usuaria, ContrasenaActual);
        _actual.IdUsuario = usuario.IdUsuario;

        var respuesta = await _servicio.CambiarContrasenaPropiaAsync(ContrasenaActual, ContrasenaNueva);

        Assert.True(respuesta.Exito);
        Assert.Equal(ContrasenaNueva, _gestor.Contrasenas[usuario.IdUsuario]);
        Assert.False(_gestor.ContrasenasEstablecidas.Single().DebeCambiar);
        Assert.Equal([usuario.IdUsuario], _emisor.Refrescadas);
        var evento = Assert.Single(_repositorioAuditoria.Eventos);
        Assert.Equal(AccionAuditoria.CambioContrasena, evento.Accion);
        Assert.Equal(usuario.IdUsuario, evento.IdUsuarioActor);
        Assert.Equal(usuario.IdUsuario, evento.IdRegistro);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("contrasena-equivocada-999")]
    public async Task CambioOrdinario_SinLaActualOConUnaIncorrecta_NoCambiaNada(string? actual)
    {
        var usuario = _gestor.Agregar("usuaria1", NombresDeRol.Usuaria, ContrasenaActual);
        _actual.IdUsuario = usuario.IdUsuario;

        var respuesta = await _servicio.CambiarContrasenaPropiaAsync(actual, ContrasenaNueva);

        Assert.False(respuesta.Exito);
        Assert.Equal(MensajesDeIdentidad.CambioDeContrasenaNoRealizado, respuesta.Mensaje);
        Assert.Empty(_gestor.ContrasenasEstablecidas);
        Assert.Empty(_repositorioAuditoria.Eventos);
        Assert.Empty(_emisor.Refrescadas);
    }

    [Fact]
    public async Task CambioOrdinario_ConLaActualIncorrecta_CuentaComoIntentoFallido()
    {
        var usuario = _gestor.Agregar("usuaria1", NombresDeRol.Usuaria, ContrasenaActual);
        _actual.IdUsuario = usuario.IdUsuario;

        await _servicio.CambiarContrasenaPropiaAsync("contrasena-equivocada-999", ContrasenaNueva);

        Assert.Equal(1, _gestor.IntentosFallidos[usuario.IdUsuario]);
        Assert.Equal(1, _unidad.Confirmaciones);
    }

    [Fact]
    public async Task PrimeraEntradaConTemporal_SoloRequiereLaNueva()
    {
        var usuario = _gestor.Agregar("usuaria1", NombresDeRol.Usuaria, "temporal-asignada-789", debeCambiar: true);
        _actual.IdUsuario = usuario.IdUsuario;

        var respuesta = await _servicio.CambiarContrasenaPropiaAsync(null, ContrasenaNueva);

        Assert.True(respuesta.Exito);
        Assert.False(_gestor.Usuarios[usuario.IdUsuario].DebeCambiarContrasena);
        Assert.False(respuesta.Datos!.DebeCambiarContrasena);
    }

    [Fact]
    public async Task ContrasenaNuevaQueNoCumpleLasReglas_NoCambiaNiAuditaNiReemiteLaSesion()
    {
        var usuario = _gestor.Agregar("usuaria1", NombresDeRol.Usuaria, ContrasenaActual);
        _actual.IdUsuario = usuario.IdUsuario;

        var respuesta = await _servicio.CambiarContrasenaPropiaAsync(ContrasenaActual, "corta");

        Assert.False(respuesta.Exito);
        Assert.Equal(MensajesDeIdentidad.ContrasenaNuevaNoCumpleReglas, respuesta.Mensaje);
        Assert.Equal(ContrasenaActual, _gestor.Contrasenas[usuario.IdUsuario]);
        Assert.Empty(_repositorioAuditoria.Eventos);
        Assert.Empty(_emisor.Refrescadas);
    }

    [Fact]
    public async Task SoloCambiaLaContrasenaDelUsuarioDeLaSesion_NuncaLaDeOtro()
    {
        var yo = _gestor.Agregar("usuaria1", NombresDeRol.Usuaria, ContrasenaActual);
        var otro = _gestor.Agregar("usuaria2", NombresDeRol.Usuaria, "otra-contrasena-123");
        _actual.IdUsuario = yo.IdUsuario;

        await _servicio.CambiarContrasenaPropiaAsync(ContrasenaActual, ContrasenaNueva);

        Assert.Equal("otra-contrasena-123", _gestor.Contrasenas[otro.IdUsuario]);
        Assert.Equal(yo.IdUsuario, _gestor.ContrasenasEstablecidas.Single().Id);
    }

    [Fact]
    public async Task SinSesionOConUsuarioDesactivado_Rechaza()
    {
        var inactivo = _gestor.Agregar("usuaria1", NombresDeRol.Usuaria, ContrasenaActual, activo: false);

        _actual.IdUsuario = null;
        Assert.False((await _servicio.CambiarContrasenaPropiaAsync(ContrasenaActual, ContrasenaNueva)).Exito);

        _actual.IdUsuario = inactivo.IdUsuario;
        Assert.False((await _servicio.CambiarContrasenaPropiaAsync(ContrasenaActual, ContrasenaNueva)).Exito);
        Assert.Empty(_gestor.ContrasenasEstablecidas);
    }

    [Fact]
    public async Task SiLaAuditoriaFalla_NoConfirmaElCambioNiReemiteLaSesion()
    {
        var usuario = _gestor.Agregar("usuaria1", NombresDeRol.Usuaria, ContrasenaActual);
        _actual.IdUsuario = usuario.IdUsuario;
        _repositorioAuditoria.ExcepcionAlAgregar = new InvalidOperationException("fallo simulado");

        await Assert.ThrowsAsync<InvalidOperationException>(() => _servicio.CambiarContrasenaPropiaAsync(ContrasenaActual, ContrasenaNueva));

        Assert.Equal(1, _unidad.Reversiones);
        Assert.Empty(_emisor.Refrescadas);
    }

    [Fact]
    public async Task EventoDelCambio_NoContieneNingunaContrasena()
    {
        var usuario = _gestor.Agregar("usuaria_canario", NombresDeRol.Usuaria, ContrasenaActual);
        _actual.IdUsuario = usuario.IdUsuario;

        await _servicio.CambiarContrasenaPropiaAsync(ContrasenaActual, ContrasenaNueva);

        var evento = Assert.Single(_repositorioAuditoria.Eventos);
        var texto = $"{evento.Hecho}|{string.Join(',', evento.CamposAfectados)}";
        Assert.DoesNotContain(ContrasenaActual, texto);
        Assert.DoesNotContain(ContrasenaNueva, texto);
        Assert.DoesNotContain("usuaria_canario", texto);
    }
}
