using TherapEase.Application.Identidad.Servicios;

namespace TherapEase.UnitTests.Application.Identidad;

public class GeneradorDeContrasenaTemporalTests
{
    [Fact]
    public void Generar_ProduceLaLongitudEsperadaConSoloCaracteresDelAlfabeto()
    {
        var contrasena = GeneradorDeContrasenaTemporal.Generar();

        Assert.Equal(GeneradorDeContrasenaTemporal.Longitud, contrasena.Length);
        Assert.All(contrasena, c => Assert.Contains(c, GeneradorDeContrasenaTemporal.Alfabeto));
    }

    [Fact]
    public void Generar_CumpleLaLongitudMinimaDeLaPoliticaDeContrasenas()
    {
        Assert.True(GeneradorDeContrasenaTemporal.Longitud >= 12);
    }

    [Fact]
    public void Alfabeto_NoIncluyeCaracteresAmbiguos()
    {
        Assert.All("0O1lI", c => Assert.DoesNotContain(c, GeneradorDeContrasenaTemporal.Alfabeto));
    }

    [Fact]
    public void Generar_NoRepiteContrasenasEnMuchasLlamadas()
    {
        var generadas = Enumerable.Range(0, 2000).Select(_ => GeneradorDeContrasenaTemporal.Generar()).ToHashSet();

        Assert.Equal(2000, generadas.Count);
    }
}
