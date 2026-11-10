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

    public ResultadoDeBusqueda? Resultado { get; private set; }
    public string? Error { get; private set; }

    // Abrir la página no consulta ni lista el directorio (B07).
    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            Error = MensajesDePacientes.BusquedaInvalida;
            return Page();
        }

        var respuesta = await pacientes.BuscarAsync(Texto, NacimientoExacto, HttpContext.RequestAborted);
        if (respuesta.Mensaje == MensajesDePacientes.SinPermiso) return Forbid();
        if (respuesta.Exito) Resultado = respuesta.Datos;
        else Error = respuesta.Mensaje;
        return Page();
    }
}
