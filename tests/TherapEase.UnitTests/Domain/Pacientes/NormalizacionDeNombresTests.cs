using System.Text;
using TherapEase.Domain.Pacientes.Reglas;

namespace TherapEase.UnitTests.Domain.Pacientes;

public class NormalizacionDeNombresTests
{
    [Theory]
    [InlineData("María", "maria")]
    [InlineData("MARÍA", "maria")]
    [InlineData("maria", "maria")]
    [InlineData("Muñoz", "munoz")]
    [InlineData("MUÑOZ", "munoz")]
    [InlineData("Güemes Pérez", "guemes perez")]
    [InlineData("  José   Luis \t Ramírez\n", "jose luis ramirez")]
    [InlineData("A\u000BB\u000CC\rD", "a b c d")]
    [InlineData("Ana-María O'Brien", "ana-maria o'brien")]
    public void Normalizar_QuitaAcentosPasaAMinusculaYColapsaEspacios(string entrada, string esperada)
    {
        Assert.Equal(esperada, NormalizacionDeNombres.Normalizar(entrada));
    }

    [Fact]
    public void Normalizar_DaLaMismaClaveConLaFormaComPuestaYLaDescompuesta()
    {
        var compuesta = "María Muñoz";
        var descompuesta = "Mari\u0301a Mun\u0303oz";

        Assert.NotEqual(compuesta, descompuesta);
        Assert.Equal(NormalizacionDeNombres.Normalizar(compuesta), NormalizacionDeNombres.Normalizar(descompuesta));
        Assert.Equal("maria munoz", NormalizacionDeNombres.Normalizar(descompuesta));
    }

    [Fact]
    public void Normalizar_SoloPasaAMinusculaLasLetrasAZ_LasDemasSeComparanTalCual()
    {
        // Independiente de la cultura del equipo: la I sin punto turca y la Ñ mayúscula no dependen de la configuración regional.
        var anterior = Thread.CurrentThread.CurrentCulture;
        try
        {
            Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("tr-TR");

            Assert.Equal("isik", NormalizacionDeNombres.Normalizar("ISIK"));
            Assert.Equal("ß", NormalizacionDeNombres.Normalizar("ß"));
            Assert.Equal("Ж", NormalizacionDeNombres.Normalizar("Ж"));
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = anterior;
        }
    }

    [Fact]
    public void Normalizar_QuitaSoloLasMarcasCombinantesU0300AU036F()
    {
        var conMarcas = new StringBuilder("e");
        conMarcas.Append((char)0x0300).Append((char)0x036F).Append((char)0x0370);

        // U+0370 (GREEK CAPITAL LETTER HETA) queda fuera del rango de marcas y se conserva.
        Assert.Equal("e" + (char)0x0370, NormalizacionDeNombres.Normalizar(conMarcas.ToString()));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\u0301\u0302")]
    public void Normalizar_SinContenidoUtilDaVacio(string? entrada)
    {
        Assert.Equal(string.Empty, NormalizacionDeNombres.Normalizar(entrada));
    }

    [Fact]
    public void Normalizar_ConSustitutosUnicodeMalFormados_TryNormalizarInformaEnLugarDeFallar()
    {
        var malFormado = "Ana\uD800Perez";

        var resultado = NormalizacionDeNombres.TryNormalizar(malFormado, out var normalizado);

        // Según la plataforma, Normalize acepta o rechaza el sustituto aislado: en cualquier caso no debe propagar una excepción.
        if (!resultado)
        {
            Assert.Equal(string.Empty, normalizado);
        }
    }

    [Fact]
    public void Normalizar_ElNombreMasLargoPermitidoNoSuperaElLimiteDeLaClave()
    {
        var nombre = new string('Ñ', 200);

        var clave = NormalizacionDeNombres.Normalizar(nombre);

        Assert.Equal(200, clave.Length);
        Assert.True(clave.Length <= TherapEase.Domain.Pacientes.Constantes.LimitesDePaciente.LongitudMaximaNombreNormalizado);
    }
}
