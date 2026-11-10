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

public class DetalleModel(IServicioPacientes pacientes) : PageModel
{
    public FichaDePaciente? Ficha { get; private set; }
    public string? Error { get; private set; }

    [TempData]
    public string? Confirmacion { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var respuesta = await pacientes.ObtenerAsync(id, HttpContext.RequestAborted);
        if (respuesta.Mensaje == MensajesDePacientes.SinPermiso) return Forbid();
        if (respuesta.Exito) Ficha = respuesta.Datos;
        else
        {
            Error = respuesta.Mensaje;
            if (respuesta.Mensaje == MensajesDePacientes.PacienteNoEncontrado)
                Response.StatusCode = StatusCodes.Status404NotFound;
        }
        return Page();
    }
}
