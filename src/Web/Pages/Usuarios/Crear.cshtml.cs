using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TherapEase.Application.Identidad.Interfaces.Servicios;
using TherapEase.Application.Identidad.Modelos;
using TherapEase.Application.Identidad.Constantes;
using TherapEase.Domain.Identidad.Constantes;
using TherapEase.Web.Seguridad;

namespace TherapEase.Web.Pages.Usuarios;

[Authorize(Policy = PoliticasDeAutorizacion.AdministrarUsuarios)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class CrearModel(IServicioUsuarios usuarios) : PageModel
{
    [BindProperty]
    public DatosDeAlta Entrada { get; set; } = new();

    public string? Error { get; private set; }
    public ContrasenaTemporalEntregada? Entrega { get; private set; }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var respuesta = await usuarios.CrearUsuarioAsync(
            Entrada.NombreUsuario, Entrada.Rol, Entrada.NombreCompleto, Entrada.Correo, HttpContext.RequestAborted);
        if (respuesta.Exito)
        {
            Entrega = respuesta.Datos;
        }
        else
        {
            Error = respuesta.Mensaje;
        }

        return Page();
    }

    public class DatosDeAlta
    {
        [Required(ErrorMessage = "Escribe el nombre de usuario.")]
        [StringLength(LimitesDeIdentidad.LongitudMaximaNombreUsuario, ErrorMessage = "Usa hasta 50 caracteres.")]
        public string NombreUsuario { get; set; } = "";

        [Required(ErrorMessage = "Escribe el nombre completo.")]
        [StringLength(LimitesDeCuenta.LongitudMaximaNombreCompleto, MinimumLength = LimitesDeCuenta.LongitudMinimaNombreCompleto, ErrorMessage = "Usa de 2 a 150 caracteres.")]
        public string NombreCompleto { get; set; } = "";

        [StringLength(LimitesDeCuenta.LongitudMaximaCorreo, ErrorMessage = "Usa hasta 254 caracteres.")]
        public string? Correo { get; set; }

        public string Rol { get; set; } = NombresDeRol.Usuaria;
    }
}
