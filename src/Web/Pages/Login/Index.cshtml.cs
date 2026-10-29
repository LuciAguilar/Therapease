using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TherapEase.Application.Identidad.Constantes;
using TherapEase.Application.Identidad.Interfaces.Servicios;

namespace TherapEase.Web.Pages.Login;

[AllowAnonymous]
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public class IndexModel : PageModel
{
    private readonly IServicioAcceso _acceso;

    public IndexModel(IServicioAcceso acceso)
    {
        _acceso = acceso;
    }

    [BindProperty]
    public EntradaAcceso Entrada { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public string? MensajeError { get; private set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // Sin try/catch: un fallo inesperado llega al error 500 general,
        // nunca se presenta como credenciales incorrectas.
        var respuesta = await _acceso.IniciarSesionAsync(
            Entrada.NombreUsuario ?? string.Empty,
            Entrada.Contrasena ?? string.Empty,
            HttpContext.RequestAborted);

        if (!respuesta.Exito || respuesta.Datos is null)
        {
            // Mismo mensaje para todos los motivos; la contraseña no se devuelve.
            Entrada.Contrasena = null;
            MensajeError = MensajesDeIdentidad.CredencialesInvalidas;
            return Page();
        }

        // Con temporal pendiente solo se permite cambiarla.
        if (respuesta.Datos.DebeCambiarContrasena)
        {
            return RedirectToPage("/Cuenta/Contrasena");
        }

        // Solo destinos dentro de TherapEase.
        if (!string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
        {
            return LocalRedirect(ReturnUrl);
        }

        return LocalRedirect("/");
    }

    public class EntradaAcceso
    {
        public string? NombreUsuario { get; set; }

        public string? Contrasena { get; set; }
    }
}