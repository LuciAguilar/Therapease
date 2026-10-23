using TherapEase.Application.Compartido.Modelos;
using TherapEase.Application.Identidad.Modelos;

namespace TherapEase.Application.Identidad.Interfaces.Servicios;

public interface IServicioAcceso
{
    // Si DebeCambiarContrasena es true, el acceso queda restringido al cambio de contraseña (CU11).
    // Una entrada correcta guarda su evento InicioSesion junto con la verificación: si falla, no se abre sesión y la excepción llega al error general.
    // El quinto fallo inicia el bloqueo y registra BloqueoAcceso aparte; la respuesta es la misma que ante cualquier otro fallo.
    Task<RespuestaServicio<DatosUsuario>> IniciarSesionAsync(string nombreUsuario, string contrasena, CancellationToken cancelacion = default);

    // Renueva el sello: todas las sesiones del usuario dejan de valer en su siguiente solicitud. Es idempotente.
    // Registra CierreSesion después de cerrar, en otra transacción: si el evento falla, la sesión queda cerrada igual. Sin sesión no hay evento.
    Task CerrarSesionAsync(CancellationToken cancelacion = default);
}
