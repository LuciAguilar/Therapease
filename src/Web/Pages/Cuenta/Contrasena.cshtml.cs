using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TherapEase.Application.Identidad.Interfaces.Servicios;
using TherapEase.Web.Seguridad;

namespace TherapEase.Web.Pages.Cuenta;

[Authorize(Policy = PoliticasDeAutorizacion.CambiarContrasenaPropia)]
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public class ContrasenaModel : PageModel
{
    private readonly IServicioContrasena _contrasena;
    private readonly IServicioCuentaPropia _cuentaPropia;

    public ContrasenaModel(IServicioContrasena contrasena, IServicioCuentaPropia cuentaPropia)
    {
        _contrasena = contrasena;
        _cuentaPropia = cuentaPropia;
    }

    [BindProperty]
    public EntradaContrasena Entrada { get; set; } = new();

    public bool EsTemporal { get; private set; }

    public string? MensajeError { get; private set; }

    [TempData]
    public string? MensajeExito { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        return await CargarModoAsync() ? Page() : RedirectToPage("/Login/Index");
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // El modo lo decide el servidor en cada envío.
        if (!await CargarModoAsync())
        {
            return RedirectToPage("/Login/Index");
        }

        if (string.IsNullOrEmpty(Entrada.ContrasenaNueva)
            || (!EsTemporal && string.IsNullOrEmpty(Entrada.ContrasenaActual)))
        {
            return ConError("Completa todos los campos.");
        }

        if (Entrada.ContrasenaNueva != Entrada.Confirmacion)
        {
            return ConError("La confirmación no coincide con la contraseña nueva.");
        }

        var respuesta = await _contrasena.CambiarContrasenaPropiaAsync(
            EsTemporal ? null : Entrada.ContrasenaActual,
            Entrada.ContrasenaNueva,
            HttpContext.RequestAborted);

        if (!respuesta.Exito)
        {
            return ConError(respuesta.Mensaje);
        }

        MensajeExito = respuesta.Mensaje;
        return EsTemporal ? RedirectToPage("/Index") : RedirectToPage();
    }

    private async Task<bool> CargarModoAsync()
    {
        var ficha = await _cuentaPropia.ObtenerMisDatosAsync(HttpContext.RequestAborted);
        if (!ficha.Exito || ficha.Datos is null)
        {
            return false;
        }

        EsTemporal = ficha.Datos.Datos.DebeCambiarContrasena;
        return true;
    }

    private PageResult ConError(string mensaje)
    {
        Entrada = new EntradaContrasena();
        MensajeError = mensaje;
        return Page();
    }

    public class EntradaContrasena
    {
        public string? ContrasenaActual { get; set; }

        public string? ContrasenaNueva { get; set; }

        public string? Confirmacion { get; set; }
    }
}