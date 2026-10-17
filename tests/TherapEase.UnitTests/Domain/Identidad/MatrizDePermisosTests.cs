using System.Reflection;
using TherapEase.Domain.Compartido.Entidades.Enums;
using TherapEase.Domain.Identidad.Constantes;
using TherapEase.Domain.Identidad.Reglas;

namespace TherapEase.UnitTests.Domain.Identidad;

public class MatrizDePermisosTests
{
    private static IEnumerable<string> RolesDefinidos() =>
        typeof(NombresDeRol).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(campo => campo.IsLiteral && campo.FieldType == typeof(string))
            .Select(campo => (string)campo.GetRawConstantValue()!);

    public static TheoryData<string> Roles()
    {
        var roles = new TheoryData<string>();
        foreach (var rol in RolesDefinidos())
        {
            roles.Add(rol);
        }

        return roles;
    }

    [Fact]
    public void RolesDefinidos_IncluyeSuperusuarioYUsuaria_ParaQueLaPruebaDeCompletitudNoQuedeVacia()
    {
        var roles = RolesDefinidos().ToList();

        Assert.Equal(2, roles.Count);
        Assert.Contains(NombresDeRol.Superusuario, roles);
        Assert.Contains(NombresDeRol.Usuaria, roles);
    }

    // Quien agregue un permiso o un rol sin decidirlo por escrito en la matriz ve fallar esta prueba.
    [Theory]
    [MemberData(nameof(Roles))]
    public void CadaRol_TieneUnaDecisionEscritaParaCadaPermisoDefinido(string rol)
    {
        var decisiones = MatrizDePermisos.DecisionesDelRol(rol);

        foreach (var permiso in Enum.GetValues<Permiso>())
        {
            Assert.True(decisiones.ContainsKey(permiso), $"El rol {rol} no tiene decisión escrita para {permiso}.");
        }

        Assert.Equal(Enum.GetValues<Permiso>().Length, decisiones.Count);
    }

    [Theory]
    [InlineData(NombresDeRol.Superusuario, Permiso.AdministrarUsuarios, true)]
    [InlineData(NombresDeRol.Superusuario, Permiso.ConsultarAuditoria, true)]
    [InlineData(NombresDeRol.Superusuario, Permiso.DarDeBajaPacientes, true)]
    [InlineData(NombresDeRol.Usuaria, Permiso.AdministrarUsuarios, false)]
    [InlineData(NombresDeRol.Usuaria, Permiso.ConsultarAuditoria, false)]
    [InlineData(NombresDeRol.Usuaria, Permiso.DarDeBajaPacientes, true)]
    public void RolTienePermiso_CoincideConLaTablaAprobada(string rol, Permiso permiso, bool esperado)
    {
        Assert.Equal(esperado, MatrizDePermisos.RolTienePermiso(rol, permiso));
        Assert.Equal(esperado, MatrizDePermisos.DecisionesDelRol(rol)[permiso]);
    }

    [Fact]
    public void PermisosDelSuperusuario_SonLosTresDecididosPorEscrito()
    {
        Assert.Equal(
            [Permiso.AdministrarUsuarios, Permiso.ConsultarAuditoria, Permiso.DarDeBajaPacientes],
            MatrizDePermisos.PermisosDelRol(NombresDeRol.Superusuario));
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
    public void RolDesconocidoOMalEscrito_NoTienePermisosNiDecisiones(string? rol)
    {
        Assert.Empty(MatrizDePermisos.DecisionesDelRol(rol));
        Assert.Empty(MatrizDePermisos.PermisosDelRol(rol));
        foreach (var permiso in Enum.GetValues<Permiso>())
        {
            Assert.False(MatrizDePermisos.RolTienePermiso(rol, permiso));
        }
    }

    [Theory]
    [InlineData(NombresDeRol.Superusuario)]
    [InlineData(NombresDeRol.Usuaria)]
    public void PermisoInexistente_NuncaSeConcede(string rol)
    {
        Assert.False(MatrizDePermisos.RolTienePermiso(rol, (Permiso)99));
    }

    // Una decisión que nadie escribió se niega: nunca se concede por omisión, ni siquiera al superusuario.
    [Theory]
    [InlineData(NombresDeRol.Superusuario)]
    [InlineData(NombresDeRol.Usuaria)]
    public void PermisoSinDecisionEscrita_SeNiega(string rol)
    {
        var sinDecision = (Permiso)99;

        Assert.False(MatrizDePermisos.DecisionesDelRol(rol).ContainsKey(sinDecision));
        Assert.False(MatrizDePermisos.RolTienePermiso(rol, sinDecision));
        Assert.DoesNotContain(sinDecision, MatrizDePermisos.PermisosDelRol(rol));
    }

    [Theory]
    [MemberData(nameof(Roles))]
    public void DecisionesDelRol_NoSePuedenModificarDesdeFuera(string rol)
    {
        var decisiones = MatrizDePermisos.DecisionesDelRol(rol);

        Assert.True(((IDictionary<Permiso, bool>)decisiones).IsReadOnly);
        Assert.Throws<NotSupportedException>(() => ((IDictionary<Permiso, bool>)decisiones)[Permiso.AdministrarUsuarios] = true);
    }
}
