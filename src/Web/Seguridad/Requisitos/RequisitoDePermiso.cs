using Microsoft.AspNetCore.Authorization;
using TherapEase.Domain.Compartido.Entidades.Enums;

namespace TherapEase.Web.Seguridad.Requisitos;

public sealed class RequisitoDePermiso : IAuthorizationRequirement
{
    public RequisitoDePermiso(Permiso permiso)
    {
        Permiso = permiso;
    }

    public Permiso Permiso { get; }
}
