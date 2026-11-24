using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TherapEase.Application.Citas.Constantes;
using TherapEase.Application.Citas.Interfaces.Servicios;
using TherapEase.Application.Citas.Modelos;
using TherapEase.Domain.Citas.Entidades.Enums;
using TherapEase.Web.Seguridad;
namespace TherapEase.Web.Pages.Agenda;

[Authorize(Policy = PoliticasDeAutorizacion.CambiarPagoDeCitas)]
[Authorize(Policy = PoliticasDeAutorizacion.ConsultarAgenda)]
[Authorize(Policy = PoliticasDeAutorizacion.ConsultarPacientes)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class PagoModel(IServicioCitas citas) : PageModel
{
    [BindProperty] public int? VersionEsperada { get; set; }
    [BindProperty] public EstadoPago? NuevoPago { get; set; }
    [TempData] public string? Confirmacion { get; set; }
    public Guid IdCita { get; private set; }
    public string? Error { get; private set; }
    public bool MostrarFormulario { get; private set; }
    public bool HayConflicto { get; private set; }
    public IActionResult OnGet([FromRoute] Guid id) => RedirectToPage("Detalle", new { id });
    public async Task<IActionResult> OnPostAsync([FromRoute] Guid id)
    {
        IdCita = id;
        MostrarFormulario = true;
        if (id == Guid.Empty)
        {
            MostrarFormulario = false;
            Response.StatusCode = 404;
            Error = MensajesDeCitas.CitaNoEncontrada;
            return Page();
        }
        if (!ModelState.IsValid || VersionEsperada is null or < 1 || NuevoPago is null || !Enum.IsDefined(NuevoPago.Value))
        {
            Error = MensajesDeCitas.SolicitudInvalida;
            return Page();
        }
        var respuesta = await citas.CambiarPagoAsync(id, VersionEsperada.Value, NuevoPago, HttpContext.RequestAborted);
        if (respuesta.Mensaje == MensajesDeCitas.SinPermiso) return Forbid();
        if (respuesta.Exito && respuesta.Datos is { } resultado)
        {
            Confirmacion = resultado.Estado == EstadoDeCambioDePago.SinCambios ? MensajesDeCitas.SinCambios : MensajesDeCitas.PagoActualizado;
            return RedirectToPage("Detalle", new { id });
        }
        Error = respuesta.Mensaje;
        HayConflicto = Error == MensajesDeCitas.ConflictoDeVersion;
        MostrarFormulario = Error is not (MensajesDeCitas.CitaNoEncontrada or MensajesDeCitas.PagoDeCitaDeBajaNoSeActualiza);
        if (Error == MensajesDeCitas.CitaNoEncontrada) Response.StatusCode = 404;
        return Page();
    }
}
