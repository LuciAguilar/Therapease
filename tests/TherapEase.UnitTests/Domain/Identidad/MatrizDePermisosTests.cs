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

    [Theory]
    [InlineData(Permiso.AdministrarUsuarios)]
    [InlineData(Permiso.ConsultarAuditoria)]
    [InlineData(Permiso.DarDeBajaPacientes)]
    public void Usuaria_NoTieneLosPermisosActuales(Permiso permiso)
    {
        Assert.False(MatrizDePermisos.RolTienePermiso(NombresDeRol.Usuaria, permiso));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Capturista")]
    [InlineData("superusuario")]
    public void RolDesconocidoOMalEscrito_NoTienePermisos(string? rol)
    {
        Assert.Empty(MatrizDePermisos.PermisosDelRol(rol));
        Assert.False(MatrizDePermisos.RolTienePermiso(rol, Permiso.AdministrarUsuarios));
    }

    [Fact]
    public void PermisoInexistente_NuncaSeConcede()
    {
        Assert.False(MatrizDePermisos.RolTienePermiso(NombresDeRol.Superusuario, (Permiso)99));
    }

    [Fact]
    public void PermisosDelSuperusuario_SonTodosLosDefinidos()
    {
        Assert.Equal(Enum.GetValues<Permiso>().Length, MatrizDePermisos.PermisosDelRol(NombresDeRol.Superusuario).Count);
    }

}
