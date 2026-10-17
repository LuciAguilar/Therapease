using TherapEase.Application.Compartido.Modelos;
using TherapEase.Application.Identidad.Modelos;

namespace TherapEase.Application.Identidad.Interfaces.Servicios;

// CU02: solo un superusuario activo; el actor es la sesión en curso. Cada cambio y su evento se confirman juntos.
public interface IServicioUsuarios
{
    // Consultas de solo lectura, sin evento de auditoría. Exigen el mismo permiso y no devuelven hash, sello ni contadores.
    // La búsqueda es opcional, parcial y sin distinguir mayúsculas; el resultado va ordenado por nombre de usuario, con un tope fijo e incluye cuentas desactivadas.
    Task<RespuestaServicio<IReadOnlyList<DatosUsuario>>> ListarUsuariosAsync(
        string? busqueda = null,
        CancellationToken cancelacion = default
        );

    Task<RespuestaServicio<FichaDeUsuario>> ObtenerUsuarioAsync(
        Guid idUsuario,
        CancellationToken cancelacion = default
        );

    // El nombre completo es obligatorio (2 a 150 caracteres); el correo es opcional, no tiene que ser único y no sirve para entrar.
    Task<RespuestaServicio<ContrasenaTemporalEntregada>> CrearUsuarioAsync(
        string nombreUsuario, 
        string rol, 
        string nombreCompleto,
        string? correo = null,
        CancellationToken cancelacion = default
        );

    // Reemplaza nombre completo y correo (sin correo = quitarlo). No cierra sesiones. Con evento solo si algo cambió, y sin valores.
    Task<RespuestaServicio<FichaDeUsuario>> ActualizarDatosDeCuentaAsync(
        Guid idUsuario,
        string nombreCompleto,
        string? correo,
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

    // No levanta un bloqueo vigente (máximo 15 minutos): decisión de Lucía del 04-oct-2026 (AGENTS §2.2, CU02 A8).
    Task<RespuestaServicio<ContrasenaTemporalEntregada>> RestablecerContrasenaAsync(
        Guid idUsuario, 
        CancellationToken cancelacion = default
        );
}
