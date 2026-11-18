using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TherapEase.Application.Citas.Constantes;
using TherapEase.Application.Citas.Interfaces.Servicios;
using TherapEase.Application.Citas.Modelos;
using TherapEase.Application.Pacientes.Constantes;
using TherapEase.Application.Pacientes.Interfaces.Servicios;
using TherapEase.Application.Pacientes.Modelos;
using TherapEase.Domain.Pacientes.Entidades.Enums;
using TherapEase.Web.Seguridad;

namespace TherapEase.Web.Pages.Agenda;

[Authorize(Policy = PoliticasDeAutorizacion.AgendarCitas)]
[Authorize(Policy = PoliticasDeAutorizacion.ConsultarPacientes)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class CrearModel(IServicioCitas citas, IServicioPacientes pacientes) : PageModel
{
    [BindProperty, BindRequired]
    public Guid IdSolicitud { get; set; }

    [BindProperty]
    public DatosDelFormularioDeCita Entrada { get; set; } = new();

    [BindProperty]
    public string? Texto { get; set; }

    public ResultadoDeBusqueda? Busqueda { get; private set; }
    public string? NombreDelPaciente { get; private set; }
    public IReadOnlyList<AmbitoAtencion> Ambitos { get; private set; } = [];
    public ResultadoDeAgendar? Resultado { get; private set; }
    [TempData]
    public string? Confirmacion { get; set; }
    public string? Error { get; private set; }

    public Task<IActionResult> OnGetAsync(Guid? paciente)
    {
        IdSolicitud = Guid.NewGuid();
        Entrada.IdPaciente = paciente;
        return MostrarFormularioAsync();
    }

    public async Task<IActionResult> OnPostBuscarAsync()
    {
        var respuesta = await pacientes.BuscarAsync(Texto, cancelacion: HttpContext.RequestAborted);
        if (respuesta.Mensaje == MensajesDePacientes.SinPermiso) return Forbid();

        if (respuesta.Exito) Busqueda = respuesta.Datos;
        else Error = respuesta.Mensaje;

        return await MostrarFormularioAsync();
    }

    public Task<IActionResult> OnPostElegirAsync(Guid paciente)
    {
        if (paciente == Guid.Empty)
        {
            Error = MensajesDeCitas.SolicitudInvalida;
            return MostrarFormularioAsync();
        }

        if (Entrada.IdPaciente != paciente)
        {
            Entrada.IdPaciente = paciente;
            Entrada.Ambito = null;
            ModelState.Remove(nameof(Entrada) + "." + nameof(Entrada.IdPaciente));
            ModelState.Remove(nameof(Entrada) + "." + nameof(Entrada.Ambito));
        }

        return MostrarFormularioAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid || IdSolicitud == Guid.Empty)
        {
            Error = MensajesDeCitas.SolicitudInvalida;
            return await MostrarFormularioAsync();
        }

        // El servicio resuelve los reenvíos antes de comprobar si cambiaron el paciente o su ámbito.
        var respuesta = await citas.AgendarAsync(IdSolicitud, Entrada.ParaServicio(), HttpContext.RequestAborted);
        if (respuesta.Mensaje == MensajesDeCitas.SinPermiso) return Forbid();

        if (respuesta.Exito && respuesta.Datos is { } resultado &&
            resultado.Estado is EstadoDeAgendar.Agendada or EstadoDeAgendar.YaAgendada)
        {
            Resultado = resultado;
            Confirmacion = resultado.Estado == EstadoDeAgendar.YaAgendada
                ? MensajesDeCitas.CitaYaAgendada
                : MensajesDeCitas.CitaAgendada;
            return Page();
        }

        Error = respuesta.Mensaje;
        return await MostrarFormularioAsync();
    }

    private async Task<IActionResult> MostrarFormularioAsync()
    {
        if (Entrada.IdPaciente is not { } id) return Page();

        var respuesta = await pacientes.ObtenerAsync(id, HttpContext.RequestAborted);
        if (respuesta.Mensaje == MensajesDePacientes.SinPermiso) return Forbid();

        if (respuesta.Exito && respuesta.Datos is { } ficha)
        {
            NombreDelPaciente = ficha.Nombre;
            Ambitos = ficha.Ambitos;
        }
        else
        {
            Error ??= respuesta.Mensaje;
        }

        return Page();
    }
}
