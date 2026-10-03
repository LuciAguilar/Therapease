using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using TherapEase.Application.Compartido.Interfaces.Servicios;

namespace TherapEase.Infrastructure.Identidad.Servicios;

public class UsuarioActualHttp : IUsuarioActual
{
    private readonly IHttpContextAccessor _contexto;

    public UsuarioActualHttp(IHttpContextAccessor contexto)
    {
        _contexto = contexto;
    }

    public Guid? IdUsuario
    {
        get
        {
            var usuario = _contexto.HttpContext?.User;
            return usuario?.Identity?.IsAuthenticated == true && Guid.TryParse(usuario.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
                ? id
                : null;
        }
    }
}
