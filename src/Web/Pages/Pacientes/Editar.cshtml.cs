using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TherapEase.Application.Pacientes.Constantes;
using TherapEase.Application.Pacientes.Interfaces.Servicios;
using TherapEase.Application.Pacientes.Modelos;
using TherapEase.Web.Seguridad;

namespace TherapEase.Web.Pages.Pacientes;

[Authorize(Policy = PoliticasDeAutorizacion.ActualizarPacientes)]
[Authorize(Policy = PoliticasDeAutorizacion.ConsultarPacientes)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]

public class EditarModel(IServicioPacientes pacientes) : PageModel
{
    [BindProperty]
    public DatosDelFormularioDePaciente Entrada { get; set; } = new();

    [BindProperty, BindRequired]
    public int VersionEsperada { get; set; }

    public Guid IdPaciente { get; private set; }
    public bool MostrarFormulario { get; private set; }
    public string? Error { get; private set; }
    [TempData]
    public string? Confirmacion { get; set; }
    public bool HayConflicto { get; private set; }

    public async Task<IActionResult> OnGetAsync([FromRoute] Guid id)
    {
        IdPaciente = id;
        var respuesta = await pacientes.ObtenerAsync(id, HttpContext.RequestAborted);
        if (respuesta.Mensaje == MensajesDePacientes.SinPermiso) return Forbid();
        if (respuesta.Exito && respuesta.Datos is { } ficha) CargarFicha(ficha);
        else
        {
            Error = respuesta.Mensaje;
            if (respuesta.Mensaje == MensajesDePacientes.PacienteNoEncontrado)
                Response.StatusCode = StatusCodes.Status404NotFound;
        }
        return Page();
    }

    public async Task<IActionResult> OnPostAsync([FromRoute] Guid id)
    {
        IdPaciente = id;
        MostrarFormulario = id != Guid.Empty;
        if (!ModelState.IsValid || id == Guid.Empty || VersionEsperada < 1)
        {
            Error = MensajesDePacientes.SolicitudInvalida;
            return Page();
        }

        var respuesta = await pacientes.ActualizarAsync(
            id, VersionEsperada, Entrada.ParaServicio(), HttpContext.RequestAborted);
        if (respuesta.Mensaje == MensajesDePacientes.SinPermiso) return Forbid();
        if (respuesta.Exito && respuesta.Datos is { } resultado)
        {
            Confirmacion = resultado.Estado == EstadoDeEdicion.SinCambios
                ? MensajesDePacientes.SinCambios : MensajesDePacientes.DatosActualizados;
            return RedirectToPage("Index", new { ficha = resultado.Ficha.IdPaciente });
        }
        else
        {
            Error = respuesta.Mensaje;
            HayConflicto = respuesta.Mensaje == MensajesDePacientes.ConflictoDeVersion;
            if (respuesta.Mensaje == MensajesDePacientes.PacienteNoEncontrado)
                Response.StatusCode = StatusCodes.Status404NotFound;
            MostrarFormulario = respuesta.Mensaje is not (MensajesDePacientes.PacienteNoEncontrado
                or MensajesDePacientes.FichaDeBajaNoSeActualiza);
            // No consultar ni reenviar automáticamente: conservar la captura y su versión para decidir.
        }
        return Page();
    }

    private void CargarFicha(FichaDePaciente ficha)
    {
        MostrarFormulario = true;
        Entrada = DatosDelFormularioDePaciente.DesdeFicha(ficha);
        VersionEsperada = ficha.Version;
    }
}
