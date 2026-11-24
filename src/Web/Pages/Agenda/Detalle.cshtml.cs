using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TherapEase.Application.Citas.Constantes;
using TherapEase.Application.Citas.Interfaces.Servicios;
using TherapEase.Application.Citas.Modelos;
using TherapEase.Web.Seguridad;

namespace TherapEase.Web.Pages.Agenda;

[Authorize(Policy = PoliticasDeAutorizacion.ConsultarAgenda)]
[Authorize(Policy = PoliticasDeAutorizacion.ConsultarPacientes)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class DetalleModel(IServicioCitas citas) : PageModel
{
    public DetalleDeCita? Cita { get; private set; }
    [TempData] public string? Confirmacion { get; set; }
    public string? Error { get; private set; }

    public async Task<IActionResult> OnGetAsync([FromRoute] Guid id)
    {
        var respuesta = await citas.ObtenerCitaAsync(id, HttpContext.RequestAborted);
        if (respuesta.Mensaje == MensajesDeCitas.SinPermiso) return Forbid();

        if (respuesta.Exito) Cita = respuesta.Datos;
        else
        {
            Error = respuesta.Mensaje;
            if (Error == MensajesDeCitas.CitaNoEncontrada)
                Response.StatusCode = StatusCodes.Status404NotFound;
        }

        return Page();
    }
}
