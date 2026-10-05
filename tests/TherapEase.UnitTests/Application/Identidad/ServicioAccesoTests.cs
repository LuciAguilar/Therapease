using TherapEase.Application.Identidad.Constantes;
using TherapEase.Application.Identidad.Modelos;
using TherapEase.Application.Identidad.Servicios;
using TherapEase.Domain.Identidad.Constantes;
using TherapEase.UnitTests.Dobles;

namespace TherapEase.UnitTests.Application.Identidad;

public class ServicioAccesoTests
{
    private const string Contrasena = "contrasena-valida-123";

    private readonly GestorIdentidadFalso _gestor = new();
    private readonly EmisorDeSesionFalso _emisor = new();
    private readonly UnidadDeTrabajoFalsa _unidad = new();
    private readonly UsuarioActualFalso _actual = new();
    private readonly ServicioAcceso _servicio;

    public ServicioAccesoTests()
    {
        _servicio = new ServicioAcceso(_gestor, _emisor, _unidad, _actual);
    }

    [Fact]
    public async Task IniciarSesion_ConCredencialesValidas_AbreLaSesionConSuRol()
    {
        var usuaria = _gestor.Agregar("usuaria1", NombresDeRol.Usuaria, Contrasena);

        var respuesta = await _servicio.IniciarSesionAsync("usuaria1", Contrasena);

        Assert.True(respuesta.Exito);
        Assert.Equal(NombresDeRol.Usuaria, respuesta.Datos!.Rol);
        Assert.Equal([usuaria.IdUsuario], _emisor.Abiertas);
    }

    [Fact]
    public async Task IniciarSesion_ConContrasenaTemporal_AbreSesionMarcandoQueDebeCambiarla()
    {
        _gestor.Agregar("usuaria1", NombresDeRol.Usuaria, Contrasena, debeCambiar: true);

        var respuesta = await _servicio.IniciarSesionAsync("usuaria1", Contrasena);

        Assert.True(respuesta.Exito);
        Assert.True(respuesta.Datos!.DebeCambiarContrasena);
    }

    [Fact]
    public async Task IniciarSesion_UsuarioInexistenteContrasenaIncorrectaBloqueadoYDesactivado_Reciben_ElMismoMensaje()
    {
        _gestor.Agregar("usuaria1", NombresDeRol.Usuaria, Contrasena);
        var bloqueada = _gestor.Agregar("bloqueada", NombresDeRol.Usuaria, Contrasena);
        _gestor.Bloqueados.Add(bloqueada.IdUsuario);
        _gestor.Agregar("inactiva", NombresDeRol.Usuaria, Contrasena, activo: false);

        var mensajes = new[]
        {
            (await _servicio.IniciarSesionAsync("no-existe", Contrasena)).Mensaje,
            (await _servicio.IniciarSesionAsync("usuaria1", "incorrecta-123456")).Mensaje,
            (await _servicio.IniciarSesionAsync("bloqueada", Contrasena)).Mensaje,
            (await _servicio.IniciarSesionAsync("inactiva", Contrasena)).Mensaje,
            (await _servicio.IniciarSesionAsync("", Contrasena)).Mensaje,
            (await _servicio.IniciarSesionAsync("usuaria1", "")).Mensaje
        };

        Assert.All(mensajes, m => Assert.Equal(MensajesDeIdentidad.CredencialesInvalidas, m));
        Assert.Empty(_emisor.Abiertas);
    }

    [Fact]
    public async Task IniciarSesion_AunqueSeNiegue_ConfirmaElConteoDeFallos()
    {
        var usuaria = _gestor.Agregar("usuaria1", NombresDeRol.Usuaria, Contrasena);

        await _servicio.IniciarSesionAsync("usuaria1", "incorrecta-123456");

        Assert.Equal(1, _unidad.Confirmaciones);
        Assert.Equal(1, _gestor.IntentosFallidos[usuaria.IdUsuario]);
    }

    [Fact]
    public async Task IniciarSesion_SiFallaLaVerificacion_NoAbreSesion()
    {
        var servicio = new ServicioAcceso(new GestorQueFalla(), _emisor, _unidad, _actual);

        await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.IniciarSesionAsync("usuaria1", Contrasena));

        Assert.Empty(_emisor.Abiertas);
    }

    [Fact]
    public async Task CerrarSesion_RenuevaElSelloYRetiraLaCookie()
    {
        var usuaria = _gestor.Agregar("usuaria1", NombresDeRol.Usuaria, Contrasena);
        _actual.IdUsuario = usuaria.IdUsuario;

        await _servicio.CerrarSesionAsync();

        Assert.Equal([usuaria.IdUsuario], _gestor.SesionesRevocadas);
        Assert.Equal(1, _emisor.Cierres);
    }

    [Fact]
    public async Task CerrarSesion_SinSesionOYaVencida_TerminaSinError()
    {
        _actual.IdUsuario = null;

        await _servicio.CerrarSesionAsync();
        await _servicio.CerrarSesionAsync();

        Assert.Empty(_gestor.SesionesRevocadas);
        Assert.Equal(2, _emisor.Cierres);
    }

    private sealed class GestorQueFalla : GestorIdentidadFalso
    {
        public override Task<DatosUsuario?> VerificarCredencialesAsync(string nombreUsuario, string contrasena, CancellationToken cancelacion = default) =>
            throw new InvalidOperationException("fallo simulado");
    }
}
