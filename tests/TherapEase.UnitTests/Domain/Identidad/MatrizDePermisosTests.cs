using TherapEase.Domain.Compartido.Entidades.Enums;
using TherapEase.Domain.Identidad.Constantes;
using TherapEase.Domain.Identidad.Reglas;

namespace TherapEase.UnitTests.Domain.Identidad;

public class MatrizDePermisosTests
{
    [Theory]
    [InlineData(Permiso.AdministrarUsuarios)]
    [InlineData(Permiso.ConsultarAuditoria)]
    [InlineData(Permiso.DarDeBajaPacientes)]
    public void Superusuario_TieneLosPermisosActuales(Permiso permiso)
    {
        Assert.True(MatrizDePermisos.RolTienePermiso(NombresDeRol.Superusuario, permiso));
    }

    [Fact]
    public void Usuaria_PuedeDarDeBajaPacientes()
    {
        Assert.True(MatrizDePermisos.RolTienePermiso(NombresDeRol.Usuaria, Permiso.DarDeBajaPacientes));
    }

    [Theory]
    [InlineData(Permiso.AdministrarUsuarios)]
    [InlineData(Permiso.ConsultarAuditoria)]
    public void Usuaria_NoAdministraUsuariosNiConsultaLaAuditoriaTecnica(Permiso permiso)
    {
        Assert.False(MatrizDePermisos.RolTienePermiso(NombresDeRol.Usuaria, permiso));
    }

    [Fact]
    public void PermisosDeLaUsuaria_SonSoloLaBajaDePacientes()
    {
        Assert.Equal([Permiso.DarDeBajaPacientes], MatrizDePermisos.PermisosDelRol(NombresDeRol.Usuaria));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Capturista")]
    [InlineData("superusuario")]
    [InlineData("usuaria")]
    public void RolDesconocidoOMalEscrito_NoTienePermisos(string? rol)
    {
        Assert.Empty(MatrizDePermisos.PermisosDelRol(rol));
        Assert.False(MatrizDePermisos.RolTienePermiso(rol, Permiso.AdministrarUsuarios));
        Assert.False(MatrizDePermisos.RolTienePermiso(rol, Permiso.DarDeBajaPacientes));
    }

    [Theory]
    [InlineData(NombresDeRol.Superusuario)]
    [InlineData(NombresDeRol.Usuaria)]
    public void PermisoInexistente_NuncaSeConcede(string rol)
    {
        Assert.False(MatrizDePermisos.RolTienePermiso(rol, (Permiso)99));
    }

    [Fact]
    public void PermisosDelSuperusuario_SonTodosLosDefinidos()
    {
        Assert.Equal(Enum.GetValues<Permiso>().Length, MatrizDePermisos.PermisosDelRol(NombresDeRol.Superusuario).Count);
    }
}
