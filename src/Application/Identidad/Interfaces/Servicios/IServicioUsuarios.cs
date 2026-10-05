using TherapEase.Application.Compartido.Modelos;
using TherapEase.Application.Identidad.Modelos;

namespace TherapEase.Application.Identidad.Interfaces.Servicios;

// CU02: solo un superusuario activo; el actor es la sesión en curso. Cada cambio y su evento se confirman juntos.
public interface IServicioUsuarios
{
    Task<RespuestaServicio<ContrasenaTemporalEntregada>> CrearUsuarioAsync(
        string nombreUsuario, 
        string rol, 
        CancellationToken cancelacion = default
        );

    Task<RespuestaServicio<DatosUsuario>> CambiarRolAsync(
        Guid idUsuario, 
        string nuevoRol, 
        CancellationToken cancelacion = default
        );

    Task<RespuestaServicio<DatosUsuario>> DesactivarUsuarioAsync(
        Guid idUsuario, 
        CancellationToken cancelacion = default
        );

    // No modifica el bloqueo por intentos: levantarlo con un restablecimiento está pendiente de decisión (CU02 A8).
    Task<RespuestaServicio<ContrasenaTemporalEntregada>> RestablecerContrasenaAsync(
        Guid idUsuario, 
        CancellationToken cancelacion = default
        );
}
