using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TherapEase.Application.Pacientes.Constantes;
using TherapEase.Application.Pacientes.Interfaces.Servicios;
using TherapEase.Application.Pacientes.Modelos;
using TherapEase.Web.Seguridad;

namespace TherapEase.Web.Pages.Pacientes;

[Authorize(Policy = PoliticasDeAutorizacion.RegistrarPacientes)]
[Authorize(Policy = PoliticasDeAutorizacion.ConsultarPacientes)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class CrearModel(IServicioPacientes pacientes) : PageModel
{
    [BindProperty, BindRequired]
    public Guid IdSolicitud { get; set; }

    [BindProperty]
    public DatosDelFormularioDePaciente Entrada { get; set; } = new();

    [BindProperty]
    public bool ConfirmaQueEsOtraPersona { get; set; }

    public ResultadoDeAlta? Resultado { get; private set; }
    public string? Error { get; private set; }

    [TempData]
    public string? Confirmacion { get; set; }

    public void OnGet() => IdSolicitud = Guid.NewGuid();

    public async Task<IActionResult> OnPostAsync()
    {
        // Nunca generar un identificador en POST: perdería la protección del reenvío.
        if (!ModelState.IsValid || IdSolicitud == Guid.Empty)
        {
            Error = MensajesDePacientes.SolicitudInvalida;
            return Page();
        }

        var respuesta = await pacientes.RegistrarAsync(
            IdSolicitud, Entrada.ParaServicio(), ConfirmaQueEsOtraPersona, HttpContext.RequestAborted);
        if (respuesta.Mensaje == MensajesDePacientes.SinPermiso) return Forbid();
        Resultado = respuesta.Datos;
        if (respuesta.Exito && Resultado?.IdPaciente is { } id
            && Resultado.Estado is EstadoDeAlta.Registrado or EstadoDeAlta.YaRegistrado)
        {
            Confirmacion = Resultado.Estado == EstadoDeAlta.YaRegistrado
                ? MensajesDePacientes.PacienteYaRegistrado : MensajesDePacientes.PacienteRegistrado;
            return RedirectToPage("Index", new { ficha = id });
        }

        // Las coincidencias llegan con Exito falso y no significan que se guardó la ficha.
        Error = respuesta.Mensaje;
        return Page();
    }
}
