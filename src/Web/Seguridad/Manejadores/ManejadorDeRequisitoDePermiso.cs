using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using TherapEase.Domain.Identidad.Constantes;
using TherapEase.Domain.Identidad.Reglas;
using TherapEase.Web.Seguridad.Requisitos;

namespace TherapEase.Web.Seguridad.Manejadores;

public sealed class ManejadorDeRequisitoDePermiso : AuthorizationHandler<RequisitoDePermiso>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext contexto, RequisitoDePermiso requisito)
    {
        var sinContrasenaTemporal = contexto.User.FindFirst(Reclamaciones.DebeCambiarContrasena)?.Value == "false";
        var tienePermiso = contexto.User.FindAll(ClaimTypes.Role).Any(r => MatrizDePermisos.RolTienePermiso(r.Value, requisito.Permiso));

        if (sinContrasenaTemporal && tienePermiso)
        {
            contexto.Succeed(requisito);
        }

        return Task.CompletedTask;
    }
}
