using TherapEase.Application.Compartido.Modelos;
using TherapEase.Application.Identidad.Modelos;

namespace TherapEase.Application.Identidad.Interfaces.Servicios;

public interface IServicioAcceso
{
    // Si DebeCambiarContrasena es true, el acceso queda restringido al cambio de contraseña (CU11).
    Task<RespuestaServicio<DatosUsuario>> IniciarSesionAsync(string nombreUsuario, string contrasena, CancellationToken cancelacion = default);

    // Renueva el sello: todas las sesiones del usuario dejan de valer en su siguiente solicitud. Es idempotente.
    Task CerrarSesionAsync(CancellationToken cancelacion = default);
}
