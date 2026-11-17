using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TherapEase.Application.Pacientes.Constantes;
using TherapEase.Application.Pacientes.Interfaces.Servicios;
using TherapEase.Application.Pacientes.Modelos;
using TherapEase.Web.Seguridad;

namespace TherapEase.Web.Pages.Pacientes;

[Authorize(Policy = PoliticasDeAutorizacion.ConsultarPacientes)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class IndexModel(IServicioPacientes pacientes) : PageModel
{
    [BindProperty]
    public string? Texto { get; set; }

    [BindProperty]
    public DateOnly? NacimientoExacto { get; set; }

    public PaginaDePacientes? Pagina { get; private set; }
    public string? Error { get; private set; }
    public Guid? FichaInicial { get; private set; }

    public bool HayFiltro => !string.IsNullOrWhiteSpace(Texto) || NacimientoExacto is not null;

    // Al abrir se muestra la primera página de todos los pacientes vigentes, sin filtro.
    public Task<IActionResult> OnGetAsync(Guid? ficha = null)
    {
        FichaInicial = ficha;
        return CargarAsync(1);
    }

    public Task<IActionResult> OnPostAsync(int numeroPagina = 1) => CargarAsync(numeroPagina);

    private async Task<IActionResult> CargarAsync(int numeroPagina)
    {
        if (!ModelState.IsValid || numeroPagina < 1)
        {
            Error = MensajesDePacientes.BusquedaInvalida;
            return Page();
        }

        var respuesta = await pacientes.ListarAsync(Texto, NacimientoExacto, numeroPagina, HttpContext.RequestAborted);
        if (respuesta.Mensaje == MensajesDePacientes.SinPermiso) return Forbid();
        if (respuesta.Exito && respuesta.Datos is { } pagina) Pagina = pagina;
        else Error = respuesta.Mensaje;
        return Page();
    }

    public static string Iniciales(string nombre) => string.Concat(
        nombre.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2)
            .Select(palabra => System.Globalization.StringInfo.GetNextTextElement(palabra))).ToUpperInvariant();
}
