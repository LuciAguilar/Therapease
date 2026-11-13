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
    public IReadOnlyList<ResumenDePaciente> Tarjetas { get; private set; } = [];
    public bool HaySiguiente { get; private set; }
    public int NumeroPagina { get; private set; } = 1;
    public string? Error { get; private set; }
    public Guid? FichaInicial { get; private set; }

    public void OnGet(Guid? ficha = null) => FichaInicial = ficha;

    public Task<IActionResult> OnPostAsync(int numeroPagina = 1) => CargarAsync(numeroPagina);

    private async Task<IActionResult> CargarAsync(int numeroPagina)
    {
        if (!ModelState.IsValid || numeroPagina < 1)
        {
            Error = MensajesDePacientes.BusquedaInvalida;
            return Page();
        }

        var respuesta = await pacientes.BuscarAsync(Texto, NacimientoExacto, HttpContext.RequestAborted);
        if (respuesta.Mensaje == MensajesDePacientes.SinPermiso) return Forbid();
        if (respuesta.Exito && respuesta.Datos is { } resultado)
        {
            const int tamano = 6;
            Resultado = resultado;
            NumeroPagina = Math.Min(numeroPagina, Math.Max(1, (resultado.Pacientes.Count + tamano - 1) / tamano));
            Tarjetas = resultado.Pacientes.Skip((NumeroPagina - 1) * tamano).Take(tamano).ToList();
            HaySiguiente = NumeroPagina * tamano < resultado.Pacientes.Count;
        }
        else Error = respuesta.Mensaje;
        return Page();
    }

    public static string Iniciales(string nombre) => string.Concat(
        nombre.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2)
            .Select(palabra => System.Globalization.StringInfo.GetNextTextElement(palabra))).ToUpperInvariant();
}
