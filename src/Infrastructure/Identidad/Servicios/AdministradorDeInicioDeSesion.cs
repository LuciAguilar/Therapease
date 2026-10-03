using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TherapEase.Infrastructure.Identidad.Entidades;

namespace TherapEase.Infrastructure.Identidad.Servicios;

// Además del sello, cada solicitud exige que el usuario siga activo.
public class AdministradorDeInicioDeSesion : SignInManager<Usuario>
{
    public AdministradorDeInicioDeSesion(
        UserManager<Usuario> administradorDeUsuarios,
        IHttpContextAccessor contexto,
        IUserClaimsPrincipalFactory<Usuario> fabricaDePrincipal,
        IOptions<IdentityOptions> opciones,
        ILogger<SignInManager<Usuario>> registro,
        IAuthenticationSchemeProvider esquemas,
        IUserConfirmation<Usuario> confirmacion)
        : base(administradorDeUsuarios, contexto, fabricaDePrincipal, opciones, registro, esquemas, confirmacion)
    {
    }

    public override async Task<Usuario?> ValidateSecurityStampAsync(ClaimsPrincipal? principal)
    {
        var usuario = await base.ValidateSecurityStampAsync(principal);
        return usuario is { Activo: true } ? usuario : null;
    }
}
