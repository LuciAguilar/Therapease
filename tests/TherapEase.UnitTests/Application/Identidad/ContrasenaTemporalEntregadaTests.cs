using TherapEase.Application.Identidad.Modelos;

namespace TherapEase.UnitTests.Application.Identidad;

public class ContrasenaTemporalEntregadaTests
{
    [Fact]
    public void ToString_NoImprimeLaContrasena()
    {
        var entregada = new ContrasenaTemporalEntregada(
            new DatosUsuario(Guid.NewGuid(), "usuaria1", "Usuaria", true, true), "Canario#Prueba1");

        Assert.DoesNotContain("Canario#Prueba1", entregada.ToString());
        Assert.DoesNotContain("usuaria1", entregada.ToString());
        Assert.Equal("Canario#Prueba1", entregada.ContrasenaTemporal);
    }
}
