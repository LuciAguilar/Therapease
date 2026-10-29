using TherapEase.Application.Identidad.Constantes;
using TherapEase.Application.Identidad.Interfaces.Servicios;
using TherapEase.Application.Identidad.Servicios;
using TherapEase.Domain.Identidad.Constantes;
using TherapEase.UnitTests.Dobles;

namespace TherapEase.UnitTests.Application.Identidad;

public class ServicioCuentaPropiaTests
{
    private readonly GestorIdentidadFalso _gestor = new();
    private readonly UsuarioActualFalso _actual = new();
    private readonly ServicioCuentaPropia _servicio;

    public ServicioCuentaPropiaTests()
    {
        _servicio = new ServicioCuentaPropia(_gestor, _actual);
    }

    [Fact]
    public async Task ObtenerMisDatos_DevuelveElNombreYElCorreoDeLaCuentaDeLaSesion_NoLosDeOtra()
    {
        var propia = _gestor.Agregar("usuaria1", NombresDeRol.Usuaria, nombreCompleto: "Ana Ficticia");
        _gestor.Correos[propia.IdUsuario] = "ana@ficticio.mx";
        var ajena = _gestor.Agregar("usuaria2", NombresDeRol.Usuaria, nombreCompleto: "Luis Ficticio");
        _gestor.Correos[ajena.IdUsuario] = "luis@ficticio.mx";
        _actual.IdUsuario = propia.IdUsuario;

        var respuesta = await _servicio.ObtenerMisDatosAsync();

        Assert.True(respuesta.Exito);
        Assert.Equal(propia.IdUsuario, respuesta.Datos!.Datos.IdUsuario);
        Assert.Equal("Ana Ficticia", respuesta.Datos.Datos.NombreCompleto);
        Assert.Equal("ana@ficticio.mx", respuesta.Datos.Correo);
    }

    [Fact]
    public async Task ObtenerMisDatos_NoNecesitaPermisoDeAdministracion_UnaUsuariaVeLosSuyos()
    {
        var usuaria = _gestor.Agregar("usuaria1", NombresDeRol.Usuaria, nombreCompleto: "Ana Ficticia");
        _actual.IdUsuario = usuaria.IdUsuario;

        var respuesta = await _servicio.ObtenerMisDatosAsync();

        Assert.True(respuesta.Exito);
        Assert.Null(respuesta.Datos!.Correo);
    }

    // CU11: quien tiene la temporal pendiente solo puede cambiarla, y esa pantalla muestra el nombre.
    [Fact]
    public async Task ObtenerMisDatos_ConLaContrasenaTemporalPendiente_Funciona()
    {
        var cuenta = _gestor.Agregar("usuaria1", NombresDeRol.Usuaria, debeCambiar: true, nombreCompleto: "Ana Ficticia");
        _actual.IdUsuario = cuenta.IdUsuario;

        var respuesta = await _servicio.ObtenerMisDatosAsync();

        Assert.True(respuesta.Exito);
        Assert.True(respuesta.Datos!.Datos.DebeCambiarContrasena);
    }

    [Fact]
    public async Task ObtenerMisDatos_SinSesion_RechazaSinConsultar()
    {
        _gestor.Agregar("usuaria1", NombresDeRol.Usuaria);
        _actual.IdUsuario = null;

        var respuesta = await _servicio.ObtenerMisDatosAsync();

        Assert.False(respuesta.Exito);
        Assert.Equal(MensajesDeIdentidad.SinPermiso, respuesta.Mensaje);
        Assert.Null(respuesta.Datos);
    }

    [Fact]
    public async Task ObtenerMisDatos_DeUnaCuentaDesactivadaOInexistente_RespondeLoMismoSinDatos()
    {
        var desactivada = _gestor.Agregar("usuaria1", NombresDeRol.Usuaria, activo: false);
        _actual.IdUsuario = desactivada.IdUsuario;
        var deDesactivada = await _servicio.ObtenerMisDatosAsync();

        _actual.IdUsuario = Guid.NewGuid();
        var deInexistente = await _servicio.ObtenerMisDatosAsync();

        Assert.False(deDesactivada.Exito);
        Assert.False(deInexistente.Exito);
        Assert.Equal(MensajesDeIdentidad.SinPermiso, deDesactivada.Mensaje);
        Assert.Equal(deDesactivada.Mensaje, deInexistente.Mensaje);
        Assert.Null(deDesactivada.Datos);
        Assert.Null(deInexistente.Datos);
    }

    [Fact]
    public void ObtenerMisDatos_NoRecibeIdentificador_SoloElTokenDeCancelacion()
    {
        var metodo = typeof(IServicioCuentaPropia).GetMethod(nameof(IServicioCuentaPropia.ObtenerMisDatosAsync))!;

        Assert.Equal([typeof(CancellationToken)], metodo.GetParameters().Select(p => p.ParameterType));
    }
}
