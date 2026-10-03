using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using TherapEase.Domain.Compartido.Entidades.Enums;
using TherapEase.Domain.Identidad.Constantes;
using TherapEase.Web.Seguridad.Manejadores;
using TherapEase.Web.Seguridad.Requisitos;

namespace TherapEase.UnitTests.Web;

public class ManejadorDeRequisitoDePermisoTests
{
    private static async Task<bool> Evaluar(string? rol, string? debeCambiarContrasena, Permiso permiso)
    {
        var reclamaciones = new List<Claim> { new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()) };
        if (rol is not null)
        {
            reclamaciones.Add(new Claim(ClaimTypes.Role, rol));
        }

        if (debeCambiarContrasena is not null)
        {
            reclamaciones.Add(new Claim(Reclamaciones.DebeCambiarContrasena, debeCambiarContrasena));
        }

        var requisito = new RequisitoDePermiso(permiso);
        var contexto = new AuthorizationHandlerContext([requisito], new ClaimsPrincipal(new ClaimsIdentity(reclamaciones, "prueba")), null);
        await new ManejadorDeRequisitoDePermiso().HandleAsync(contexto);
        return contexto.HasSucceeded;
    }

    [Theory]
    [InlineData(Permiso.AdministrarUsuarios)]
    [InlineData(Permiso.ConsultarAuditoria)]
    public async Task Superusuario_SinContrasenaTemporalPendiente_Autoriza(Permiso permiso)
    {
        Assert.True(await Evaluar(NombresDeRol.Superusuario, "false", permiso));
    }

    [Fact]
    public async Task Superusuario_ConContrasenaTemporalPendiente_NoAutoriza()
    {
        Assert.False(await Evaluar(NombresDeRol.Superusuario, "true", Permiso.AdministrarUsuarios));
    }

    [Fact]
    public async Task Superusuario_SinLaReclamacionDeContrasenaTemporal_NoAutoriza()
    {
        Assert.False(await Evaluar(NombresDeRol.Superusuario, null, Permiso.AdministrarUsuarios));
    }

    [Theory]
    [InlineData(Permiso.AdministrarUsuarios)]
    [InlineData(Permiso.ConsultarAuditoria)]
    public async Task Usuaria_NoAutoriza(Permiso permiso)
    {
        Assert.False(await Evaluar(NombresDeRol.Usuaria, "false", permiso));
    }

    [Fact]
    public async Task SinRolOConRolDesconocido_NoAutoriza()
    {
        Assert.False(await Evaluar(null, "false", Permiso.AdministrarUsuarios));
        Assert.False(await Evaluar("Capturista", "false", Permiso.AdministrarUsuarios));
    }
}
