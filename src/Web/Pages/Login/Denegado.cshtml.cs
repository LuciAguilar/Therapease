using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TherapEase.Domain.Identidad.Constantes;

namespace TherapEase.Web.Pages.Login;

// Evita que la página de denegación vuelva a activar la política que llevó hasta aquí.
[AllowAnonymous]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class DenegadoModel : PageModel
{
    public IActionResult OnGet()
    {
        // Sin un indicador explícito de contraseña vigente, se exige cambiarla.
        if (User.Identity?.IsAuthenticated == true &&
            User.FindFirst(Reclamaciones.DebeCambiarContrasena)?.Value != "false")
        {
            return RedirectToPage("/Cuenta/Contrasena");
        }

        Response.StatusCode = StatusCodes.Status403Forbidden;
        return Page();
    }
}
