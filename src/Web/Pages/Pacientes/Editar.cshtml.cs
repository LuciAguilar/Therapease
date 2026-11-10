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

    public FichaDePaciente? Ficha { get; private set; }
    public string? Error { get; private set; }
    public string? Confirmacion { get; private set; }
    public bool HayConflicto { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
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
            // Los Tag Helpers priorizan ModelState: retirar la versión anterior y los valores sin normalizar.
            ModelState.Clear();
            CargarFicha(resultado.Ficha);
            Confirmacion = resultado.Estado == EstadoDeEdicion.SinCambios
                ? MensajesDePacientes.SinCambios : MensajesDePacientes.DatosActualizados;
        }
        else
        {
            Error = respuesta.Mensaje;
            HayConflicto = respuesta.Mensaje == MensajesDePacientes.ConflictoDeVersion;
            // No consultar ni reenviar automáticamente: conservar la captura y su versión para decidir.
        }
        return Page();
    }

    private void CargarFicha(FichaDePaciente ficha)
    {
        Ficha = ficha;
        Entrada = DatosDelFormularioDePaciente.DesdeFicha(ficha);
        VersionEsperada = ficha.Version;
    }
}
