using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TherapEase.Application.Identidad.Interfaces.Servicios;
using TherapEase.Web.Seguridad;

namespace TherapEase.Web.Pages;

[Authorize(Policy = PoliticasDeAutorizacion.AccesoOperativo)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class IndexModel(IServicioCuentaPropia cuenta) : PageModel
{
    [TempData]
    public string? MensajeExito { get; set; }

    public string Nombre { get; private set; } = "";

    public async Task<IActionResult> OnGetAsync()
    {
        var respuesta = await cuenta.ObtenerMisDatosAsync(HttpContext.RequestAborted);
        if (!respuesta.Exito || respuesta.Datos is null)
        {
            return Challenge();
        }

        Nombre = respuesta.Datos.Datos.NombreCompleto;
        return Page();
    }
}
