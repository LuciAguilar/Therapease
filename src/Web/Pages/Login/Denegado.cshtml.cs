using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TherapEase.Domain.Identidad.Constantes;

namespace TherapEase.Web.Pages.Login;

[AllowAnonymous]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class DenegadoModel : PageModel
{
    public IActionResult OnGet()
    {
        if (User.Identity?.IsAuthenticated == true &&
            User.FindFirst(Reclamaciones.DebeCambiarContrasena)?.Value != "false")
        {
            return RedirectToPage("/Cuenta/Contrasena");
        }

        Response.StatusCode = StatusCodes.Status403Forbidden;
        return Page();
    }
}
