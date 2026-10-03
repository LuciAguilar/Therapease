using TherapEase.Application.Compartido;

namespace TherapEase.UnitTests.Application;

public class RespuestaServicioTests
{
    [Fact]
    public void Correcta_ConDatos_MarcaExitoYConservaDatosYMensaje()
    {
        var respuesta = RespuestaServicio<string>.Correcta("dato", "guardado");

        Assert.True(respuesta.Exito);
        Assert.Equal("dato", respuesta.Datos);
        Assert.Equal("guardado", respuesta.Mensaje);
    }

    [Fact]
    public void Fallida_ConMensaje_MarcaFalloSinDatos()
    {
        var respuesta = RespuestaServicio<string>.Fallida("no se pudo");

        Assert.False(respuesta.Exito);
        Assert.Null(respuesta.Datos);
        Assert.Equal("no se pudo", respuesta.Mensaje);
    }
}
