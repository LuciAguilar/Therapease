using TherapEase.Domain.Compartido.Reglas;
using TherapEase.Domain.Identidad.Reglas;
using TherapEase.Domain.Pacientes.Constantes;
using TherapEase.Domain.Pacientes.Entidades;
using TherapEase.Domain.Pacientes.Reglas;
using TherapEase.Domain.Compartido.Excepciones;

namespace TherapEase.UnitTests.Domain.Compartido;

// La regla compartida recibe los límites como parámetros: cuentas conserva 2-150 y pacientes 2-200 sin que lo común conozca a ninguno.
public class ReglasDeNombreYCorreoTests
{
    private static readonly DateOnly Nacimiento = new(1990, 1, 15);
    private static readonly DateTimeOffset Ahora = new(2026, 10, 5, 15, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Nombre_RespetaLosLimitesQueSeLePasan()
    {
        var de160 = new string('a', 160);

        Assert.False(ReglasDeNombreYCorreo.EsNombreValido(de160, 2, 150, out _));
        Assert.True(ReglasDeNombreYCorreo.EsNombreValido(de160, 2, 200, out var nombre));
        Assert.Equal(de160, nombre);
    }

    [Fact]
    public void Nombre_ConUnCaracterSoloSeRechazaConMinimoDeDos()
    {
        Assert.False(ReglasDeNombreYCorreo.EsNombreValido("A", 2, 150, out _));
        Assert.True(ReglasDeNombreYCorreo.EsNombreValido("A", 1, 150, out _));
    }

    [Fact]
    public void Cuentas_ConservaSuLimiteDe150YPacientesElDe200()
    {
        var de151 = new string('a', 151);
        var de200 = new string('a', 200);

        Assert.False(ReglasDeDatosDeCuenta.EsNombreCompletoValido(de151, out _));
        Assert.True(ReglasDeDatosDeCuenta.EsNombreCompletoValido(new string('a', 150), out _));

        var paciente = ReglasDePaciente.Registrar(Guid.NewGuid(), new DatosDePaciente(de200, Nacimiento), [TherapEase.Domain.Pacientes.Entidades.Enums.AmbitoAtencion.Escolar], Guid.NewGuid(), Ahora);
        Assert.Equal(de200, paciente.Nombre);
    }

    [Theory]
    [InlineData("Persona@Ficticio.Test", "persona@ficticio.test")]
    [InlineData("  persona@ficticio.test  ", "persona@ficticio.test")]
    public void Correo_ValidoSeGuardaRecortadoYEnMinusculasConLaRegla(string entrada, string esperado)
    {
        Assert.True(ReglasDeNombreYCorreo.EsCorreoValido(entrada, LimitesDePaciente.LongitudMaximaCorreo, out var correo));
        Assert.Equal(esperado, correo);
    }

    [Theory]
    [InlineData("persona")]
    [InlineData("@ficticio.test")]
    [InlineData("persona@ficticio")]
    [InlineData("persona@.ficticio.test")]
    [InlineData("persona@ficticio.test.")]
    [InlineData("persona@ficticio..test")]
    [InlineData("persona@@ficticio.test")]
    [InlineData("per sona@ficticio.test")]
    public void Correo_InvalidoSeRechazaConLaMismaReglaQueCuentas(string entrada)
    {
        Assert.False(ReglasDeNombreYCorreo.EsCorreoValido(entrada, LimitesDePaciente.LongitudMaximaCorreo, out var correo));
        Assert.Null(correo);
        Assert.False(ReglasDeDatosDeCuenta.EsCorreoValido(entrada, out _));
    }

    [Fact]
    public void Pacientes_RechazaCorreosConCaracteresInvisiblesComoCuentas()
    {
        var conCeroAncho = "persona\u200B@ficticio.test";

        Assert.False(ReglasDeNombreYCorreo.EsCorreoValido(conCeroAncho, LimitesDePaciente.LongitudMaximaCorreo, out _));
        Assert.Throws<ReglaDeNegocioException>(() => ReglasDePaciente.Registrar(
            Guid.NewGuid(), new DatosDePaciente("Paciente Ficticio", Nacimiento, Correo: conCeroAncho),
            [TherapEase.Domain.Pacientes.Entidades.Enums.AmbitoAtencion.Escolar], Guid.NewGuid(), Ahora));
    }
}
