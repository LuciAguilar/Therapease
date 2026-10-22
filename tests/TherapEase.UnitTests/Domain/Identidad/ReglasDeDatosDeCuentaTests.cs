using TherapEase.Domain.Identidad.Constantes;
using TherapEase.Domain.Identidad.Reglas;

namespace TherapEase.UnitTests.Domain.Identidad;

public class ReglasDeDatosDeCuentaTests
{
    // ──── Nombre completo ────

    [Theory]
    [InlineData("Erika Ficticia Pérez", "Erika Ficticia Pérez")]
    [InlineData("  Erika Ficticia  ", "Erika Ficticia")]
    [InlineData("Li", "Li")]
    [InlineData("María-José O'Brien Núñez", "María-José O'Brien Núñez")]
    public void NombreCompleto_ConTextoValido_SeAceptaYSeRecortanLosEspaciosExteriores(string entrada, string esperado)
    {
        Assert.True(ReglasDeDatosDeCuenta.EsNombreCompletoValido(entrada, out var nombre));
        Assert.Equal(esperado, nombre);
    }

    [Fact]
    public void NombreCompleto_ConLongitudMinimaYMaxima_SeAceptaYUnCaracterFueraDelLimiteSeRechaza()
    {
        Assert.True(ReglasDeDatosDeCuenta.EsNombreCompletoValido(new string('a', LimitesDeCuenta.LongitudMinimaNombreCompleto), out _));
        Assert.False(ReglasDeDatosDeCuenta.EsNombreCompletoValido(new string('a', LimitesDeCuenta.LongitudMinimaNombreCompleto - 1), out _));
        Assert.True(ReglasDeDatosDeCuenta.EsNombreCompletoValido(new string('a', LimitesDeCuenta.LongitudMaximaNombreCompleto), out _));
        Assert.False(ReglasDeDatosDeCuenta.EsNombreCompletoValido(new string('a', LimitesDeCuenta.LongitudMaximaNombreCompleto + 1), out _));
    }

    [Fact]
    public void NombreCompleto_LosLimitesSeCuentanDespuesDeRecortar()
    {
        Assert.True(ReglasDeDatosDeCuenta.EsNombreCompletoValido("  " + new string('a', LimitesDeCuenta.LongitudMaximaNombreCompleto) + "  ", out _));
        Assert.False(ReglasDeDatosDeCuenta.EsNombreCompletoValido("  a  ", out _));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Erika\nFicticia")]
    [InlineData("Erika\tFicticia")]
    [InlineData("Erika\0Ficticia")]
    public void NombreCompleto_VacioOConCaracteresDeControl_SeRechaza(string? entrada)
    {
        Assert.False(ReglasDeDatosDeCuenta.EsNombreCompletoValido(entrada, out _));
    }

    // Se indican por código para que el archivo no contenga caracteres invisibles: inversión de sentido, espacio de ancho cero y separador de línea.
    [Theory]
    [InlineData(0x202E)]
    [InlineData(0x200B)]
    [InlineData(0x2028)]
    public void NombreCompleto_ConCaracteresDeFormatoOSeparadoresInvisibles_SeRechaza(int codigo)
    {
        var entrada = "Erika" + char.ConvertFromUtf32(codigo) + "Ficticia";

        Assert.False(ReglasDeDatosDeCuenta.EsNombreCompletoValido(entrada, out _));
    }

    // ──── Correo ────

    [Theory]
    [InlineData("erika@ficticio.mx", "erika@ficticio.mx")]
    [InlineData("  Erika.Ficticia@Ficticio.MX  ", "erika.ficticia@ficticio.mx")]
    [InlineData("erika+aviso@mail.ficticio.com.mx", "erika+aviso@mail.ficticio.com.mx")]
    public void Correo_ConFormatoValido_SeGuardaRecortadoYEnMinusculas(string entrada, string esperado)
    {
        Assert.True(ReglasDeDatosDeCuenta.EsCorreoValido(entrada, out var correo));
        Assert.Equal(esperado, correo);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Correo_Opcional_SinTextoEsValidoYNoSeGuardaNinguno(string? entrada)
    {
        Assert.True(ReglasDeDatosDeCuenta.EsCorreoValido(entrada, out var correo));
        Assert.Null(correo);
    }

    [Theory]
    [InlineData("erika@ficticio")]
    [InlineData("erika@.com")]
    [InlineData("erika@ficticio.")]
    [InlineData("erika@ficticio..mx")]
    [InlineData("@ficticio.mx")]
    [InlineData("erika@")]
    [InlineData("erika")]
    [InlineData("erika@@ficticio.mx")]
    [InlineData("erika@fic@ticio.mx")]
    [InlineData("eri ka@ficticio.mx")]
    [InlineData("erika@fic ticio.mx")]
    [InlineData("erika\t@ficticio.mx")]
    [InlineData("erika@ficticio.mx\notro@ficticio.mx")]
    public void Correo_ConFormatoInvalido_SeRechazaYNoDevuelveValor(string entrada)
    {
        Assert.False(ReglasDeDatosDeCuenta.EsCorreoValido(entrada, out var correo));
        Assert.Null(correo);
    }

    // Inversión de sentido (0x202E) y espacio sin separación (0x00A0), por código para no dejar caracteres invisibles en el archivo.
    [Theory]
    [InlineData(0x202E)]
    [InlineData(0x00A0)]
    public void Correo_ConCaracteresInvisiblesOEspaciosEspeciales_SeRechaza(int codigo)
    {
        var entrada = "erika" + char.ConvertFromUtf32(codigo) + "@ficticio.mx";

        Assert.False(ReglasDeDatosDeCuenta.EsCorreoValido(entrada, out var correo));
        Assert.Null(correo);
    }

    [Fact]
    public void Correo_ConSaltoDeLineaOTabuladorExteriores_SeRecorta()
    {
        Assert.True(ReglasDeDatosDeCuenta.EsCorreoValido("\terika@ficticio.mx\n", out var correo));
        Assert.Equal("erika@ficticio.mx", correo);
    }

    [Fact]
    public void Correo_ConLongitudMaxima_SeAceptaYUnCaracterMasSeRechaza()
    {
        const string dominio = "@ficticio.mx";
        var correoMaximo = new string('a', LimitesDeCuenta.LongitudMaximaCorreo - dominio.Length) + dominio;

        Assert.Equal(LimitesDeCuenta.LongitudMaximaCorreo, correoMaximo.Length);
        Assert.True(ReglasDeDatosDeCuenta.EsCorreoValido(correoMaximo, out _));
        Assert.False(ReglasDeDatosDeCuenta.EsCorreoValido("a" + correoMaximo, out _));
    }
}
