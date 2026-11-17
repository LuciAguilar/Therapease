using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TherapEase.Application.Compartido.Modelos;
using TherapEase.Application.Identidad.Interfaces.Servicios;
using TherapEase.Application.Identidad.Modelos;
using TherapEase.Web.Seguridad;

namespace TherapEase.Web.Pages.Usuarios;

[Authorize(Policy = PoliticasDeAutorizacion.AdministrarUsuarios)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class DetalleModel(IServicioUsuarios usuarios) : PageModel
{
    public FichaDeUsuario? Ficha { get; private set; }
    public ContrasenaTemporalEntregada? Entrega { get; private set; }
    public string? Error { get; private set; }

    [TempData]
    public string? Confirmacion { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var respuesta = await usuarios.ObtenerUsuarioAsync(id, HttpContext.RequestAborted);
        Ficha = respuesta.Datos;
        if (!respuesta.Exito || Ficha is null)
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            Error ??= respuesta.Mensaje;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostDatosAsync([FromRoute] Guid id, string? nombreCompleto, string? correo)
    {
        var respuesta = await usuarios.ActualizarDatosDeCuentaAsync(
            id, nombreCompleto ?? "", correo, HttpContext.RequestAborted);
        return await MostrarResultadoAsync(id, respuesta);
    }

    public async Task<IActionResult> OnPostRolAsync([FromRoute] Guid id, string? rol, bool confirmar)
    {
        if (!confirmar)
        {
            Error = "Confirma el cambio de rol.";
            return await OnGetAsync(id);
        }

        return await MostrarResultadoAsync(id,
            await usuarios.CambiarRolAsync(id, rol ?? "", HttpContext.RequestAborted));
    }

    public async Task<IActionResult> OnPostDesactivarAsync([FromRoute] Guid id, bool confirmar)
    {
        if (!confirmar)
        {
            Error = "Confirma la desactivación de la cuenta.";
            return await OnGetAsync(id);
        }

        return await MostrarResultadoAsync(id,
            await usuarios.DesactivarUsuarioAsync(id, HttpContext.RequestAborted));
    }

    public async Task<IActionResult> OnPostRestablecerAsync([FromRoute] Guid id, bool confirmar)
    {
        if (!confirmar)
        {
            Error = "Confirma que verificaste la identidad antes de restablecer.";
            return await OnGetAsync(id);
        }

        var respuesta = await usuarios.RestablecerContrasenaAsync(id, HttpContext.RequestAborted);
        if (!respuesta.Exito || respuesta.Datos is null)
        {
            Error = respuesta.Mensaje;
            return await OnGetAsync(id);
        }

        Entrega = respuesta.Datos;
        return Page();
    }

    private async Task<IActionResult> MostrarResultadoAsync<T>(Guid id, RespuestaServicio<T> respuesta)
    {
        if (respuesta.Exito)
        {
            Confirmacion = respuesta.Mensaje;
            return RedirectToPage(new { id });
        }

        Error = respuesta.Mensaje;
        return await OnGetAsync(id);
    }
}
