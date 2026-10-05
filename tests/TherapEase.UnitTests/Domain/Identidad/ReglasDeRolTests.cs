using TherapEase.Domain.Identidad.Reglas;

namespace TherapEase.UnitTests.Domain.Identidad;

public class ReglasDeRolTests
{
    [Theory]
    [InlineData("Usuaria", true)]
    [InlineData("Superusuario", true)]
    [InlineData("Capturista", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void EsRolConocido_SoloAceptaLosRolesFijosDelPrimerAvance(string? rol, bool esperado)
    {
        Assert.Equal(esperado, ReglasDeRol.EsRolConocido(rol));
    }
}
