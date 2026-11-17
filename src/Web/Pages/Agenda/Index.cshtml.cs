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
public class IndexModel(IServicioCitas citas) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public DateOnly? Desde { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateOnly? Hasta { get; set; }

    [BindProperty(SupportsGet = true)]
    public VistaDeAgenda Vista { get; set; }

    public ResultadoDeAgenda? Agenda { get; private set; }
    public string? Error { get; private set; }

    [TempData]
    public string? Confirmacion { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!ModelState.IsValid)
        {
            Error = MensajesDeCitas.PeriodoInvalido;
            return Page();
        }

        var respuesta = await citas.ConsultarAgendaAsync(Desde, Hasta, Vista, HttpContext.RequestAborted);
        if (respuesta.Mensaje == MensajesDeCitas.SinPermiso) return Forbid();

        if (respuesta.Exito && respuesta.Datos is { } agenda)
        {
            Agenda = agenda;
            Desde = agenda.Desde;
            Hasta = agenda.Hasta;
        }
        else
        {
            Error = respuesta.Mensaje;
        }

        return Page();
    }
}
