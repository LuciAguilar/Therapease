using TherapEase.Domain.Pacientes.Reglas;

namespace TherapEase.UnitTests.Domain.Pacientes;

public class ReglasDeTelefonoTests
{
    [Theory]
    [InlineData("6441234567", "6441234567")]
    [InlineData("  644 123 4567 ", "6441234567")]
    [InlineData("(644) 123-4567", "6441234567")]
    [InlineData("644.123.4567", "6441234567")]
    [InlineData("+52 644 123 4567", "+526441234567")]
    [InlineData("+526441234567", "+526441234567")]
    [InlineData("1234567", "1234567")]
    [InlineData("123456789012345", "123456789012345")]
    public void Telefono_ConFormatoValido_SeGuardaSoloConDigitosYElMasInicial(string entrada, string esperado)
    {
        var valido = ReglasDeTelefono.EsTelefonoValido(entrada, out var telefono);

        Assert.True(valido);
        Assert.Equal(esperado, telefono);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void Telefono_Opcional_SinTextoEsValidoYNoSeGuardaNinguno(string? entrada)
    {
        var valido = ReglasDeTelefono.EsTelefonoValido(entrada, out var telefono);

        Assert.True(valido);
        Assert.Null(telefono);
    }

    [Theory]
    [InlineData("123456")]
    [InlineData("1234567890123456")]
    [InlineData("+123456")]
    [InlineData("+")]
    [InlineData("llamar al 6441234567")]
    [InlineData("6441234567 ext 5")]
    [InlineData("644-123-4567x")]
    [InlineData("644+1234567")]
    [InlineData("++526441234567")]
    [InlineData("6441234567@ficticio.test")]
    [InlineData("644\t1234567")]
    [InlineData("٤٤١٢٣٤٥")]
    public void Telefono_ConLetrasSignosOCantidadDeDigitosFueraDeRango_SeRechazaYNoDevuelveValor(string entrada)
    {
        var valido = ReglasDeTelefono.EsTelefonoValido(entrada, out var telefono);

        Assert.False(valido);
        Assert.Null(telefono);
    }

    [Fact]
    public void Telefono_GuardadoCabeEnLaColumna()
    {
        ReglasDeTelefono.EsTelefonoValido("+123456789012345", out var telefono);

        Assert.True(telefono!.Length <= TherapEase.Domain.Pacientes.Constantes.LimitesDePaciente.LongitudMaximaTelefono);
    }
}
