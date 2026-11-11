using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TherapEase.Application.Identidad.Interfaces.Servicios;
using TherapEase.Application.Identidad.Modelos;
using TherapEase.Application.Identidad.Constantes;
using TherapEase.Web.Seguridad;

namespace TherapEase.Web.Pages.Usuarios;

[Authorize(Policy = PoliticasDeAutorizacion.AdministrarUsuarios)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class IndexModel(IServicioUsuarios usuarios) : PageModel
{
    public string? Error { get; private set; }

    public string? Busqueda { get; private set; }

    public IReadOnlyList<DatosUsuario> Cuentas { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(string? busqueda)
    {
        Busqueda = busqueda;
        var respuesta = await usuarios.ListarUsuariosAsync(Busqueda, HttpContext.RequestAborted);
        if (!respuesta.Exito || respuesta.Datos is null)
        {
            if (respuesta.Mensaje == MensajesDeIdentidad.SinPermiso)
            {
                return Forbid();
            }

            Error = respuesta.Mensaje;
            return Page();
        }

        Cuentas = respuesta.Datos;
        return Page();
    }
}
