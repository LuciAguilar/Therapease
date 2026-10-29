using Microsoft.AspNetCore.Authorization;
using TherapEase.Application.Compartido.Interfaces.Servicios;
using TherapEase.Web.Seguridad.Requisitos;

namespace TherapEase.Web.Seguridad.Manejadores;

// Se registra por solicitud porque IAutorizacion depende del usuario de esa solicitud y no debe compartirse entre ellas.
public sealed class ManejadorDeRequisitoDePermiso : AuthorizationHandler<RequisitoDePermiso>
{
    private readonly IAutorizacion _autorizacion;

    public ManejadorDeRequisitoDePermiso(IAutorizacion autorizacion)
    {
        _autorizacion = autorizacion;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext contexto, RequisitoDePermiso requisito)
    {
        if (await _autorizacion.TienePermisoAsync(requisito.Permiso))
        {
            contexto.Succeed(requisito);
        }
    }
}
