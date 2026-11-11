using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TherapEase.Application.Identidad.Interfaces.Servicios;
using TherapEase.Web.Seguridad;

namespace TherapEase.Web.Pages.Cuenta;

[Authorize(Policy = PoliticasDeAutorizacion.CambiarContrasenaPropia)]
public class SalirModel(IServicioAcceso acceso) : PageModel
{
    public IActionResult OnGet() => RedirectToPage("/Index");

    public async Task<IActionResult> OnPostAsync()
    {
        await acceso.CerrarSesionAsync(HttpContext.RequestAborted);
        return RedirectToPage("/Login/Index");
    }
}
