using TherapEase.Application.Compartido.Modelos;
using TherapEase.Application.Identidad.Modelos;

namespace TherapEase.Application.Identidad.Interfaces.Servicios;

// CU11: solo la contraseña del usuario de la sesión en curso; el de otro usuario es exclusivo de CU02.
public interface IServicioContrasena
{
    // contrasenaActual es obligatoria salvo en la primera entrada con contraseña temporal ya verificada por CU01.
    Task<RespuestaServicio<DatosUsuario>> CambiarContrasenaPropiaAsync(
        string? contrasenaActual, 
        string contrasenaNueva, 
        CancellationToken cancelacion = default
        );
}
