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

    [Theory]
    [InlineData("\u00D8scar", "\u00F8scar")]
    [InlineData("\u00D8SCAR", "\u00F8scar")]
    [InlineData("\u0152uvre", "\u0153uvre")]
    [InlineData("\u0416\u0443\u043A", "\u0436\u0443\u043A")]
    [InlineData("\u0416\u0423\u041A", "\u0436\u0443\u043A")]
    [InlineData("\u0130stanbul", "istanbul")]
    [InlineData("\u0130STANBUL", "istanbul")]
    [InlineData("I\u0307stanbul", "istanbul")]
    [InlineData("\u03A3\u039F\u03A6\u038A\u0391", "\u03C3\u03BF\u03C6\u03B9\u03B1")]
    [InlineData("\u039F\u0394\u03A5\u03A3\u03A3\u0395\u03A5\u03A3", "\u03BF\u03B4\u03C5\u03C3\u03C3\u03B5\u03C5\u03C3")]
    [InlineData("\u03BF\u03B4\u03C5\u03C3\u03C3\u03B5\u03C5\u03C2", "\u03BF\u03B4\u03C5\u03C3\u03C3\u03B5\u03C5\u03C3")]
    public void Normalizar_PasaAMinusculaLasLetrasDeCualquierEscrituraYNoSoloLasDeAZ(string entrada, string esperada)
    {
        // Ø, Œ, Ж, la İ turca y la sigma de Σ: la clave de «Øscar» es la misma que la de «øscar», y lo mismo con el resto.
        Assert.Equal(esperada, NormalizacionDeNombres.Normalizar(entrada));
    }

    [Fact]
    public void Normalizar_NoDependeDeLaCulturaDelEquipo()
    {
        var anterior = Thread.CurrentThread.CurrentCulture;
        try
        {
            Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("tr-TR");

            // Con la cultura turca, ToLower daría «ısık»; la clave usa la minúscula invariante.
            Assert.Equal("isik", NormalizacionDeNombres.Normalizar("ISIK"));
            Assert.Equal("istanbul", NormalizacionDeNombres.Normalizar("\u0130stanbul"));
            Assert.Equal("\u00DF", NormalizacionDeNombres.Normalizar("\u00DF"));
            Assert.Equal("\u0436", NormalizacionDeNombres.Normalizar("\u0416"));
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

        // U+0370 (GREEK CAPITAL LETTER HETA) queda fuera del rango de marcas: se conserva y solo pasa a su minúscula, U+0371.
        Assert.Equal("e" + (char)0x0371, NormalizacionDeNombres.Normalizar(conMarcas.ToString()));
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
