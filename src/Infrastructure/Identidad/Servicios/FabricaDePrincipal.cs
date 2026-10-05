using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using TherapEase.Domain.Identidad.Constantes;
using TherapEase.Infrastructure.Identidad.Entidades;

namespace TherapEase.Infrastructure.Identidad.Servicios;

// Agrega a la cookie si el usuario debe cambiar su contraseña temporal; cualquier cambio de ese estado renueva el sello.
public class FabricaDePrincipal : UserClaimsPrincipalFactory<Usuario, Rol>
{
    public FabricaDePrincipal(UserManager<Usuario> administradorDeUsuarios, RoleManager<Rol> administradorDeRoles, IOptions<IdentityOptions> opciones)
        : base(administradorDeUsuarios, administradorDeRoles, opciones)
    {
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(Usuario usuario)
    {
        var identidad = await base.GenerateClaimsAsync(usuario);
        identidad.AddClaim(new Claim(Reclamaciones.DebeCambiarContrasena, usuario.DebeCambiarContrasena ? "true" : "false"));
        return identidad;
    }
}
