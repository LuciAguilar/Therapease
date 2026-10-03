using TherapEase.Application.Identidad.Modelos;

namespace TherapEase.Application.Identidad.Interfaces.Repositorios;

// Puerto de persistencia del módulo Identidad; lo implementa ASP.NET Core Identity en Infrastructure.
public interface IGestorIdentidad
{
    Task<DatosUsuario?> ObtenerAsync(Guid idUsuario, CancellationToken cancelacion = default);

    // Solo para el procedimiento de operador; el acceso usa VerificarCredencialesAsync para no revelar si el usuario existe.
    Task<DatosUsuario?> ObtenerPorNombreAsync(string nombreUsuario, CancellationToken cancelacion = default);

    // Null ante cualquier fallo sin distinguirlo (S-02); cuenta los fallos para el bloqueo y serializa los intentos de un usuario (S-03).
    Task<DatosUsuario?> VerificarCredencialesAsync(string nombreUsuario, string contrasena, CancellationToken cancelacion = default);

    // Un intento fallido también cuenta para el bloqueo. Como la anterior, debe ejecutarse dentro de IUnidadDeTrabajo.
    Task<bool> VerificarContrasenaActualAsync(Guid idUsuario, string contrasena, CancellationToken cancelacion = default);

    Task<ResultadoCreacionDeUsuario> CrearAsync(
        Guid idUsuario, string nombreUsuario, string rol, string contrasena, bool debeCambiarContrasena, CancellationToken cancelacion = default);

    // Cambiar rol, desactivar y establecer contraseña renuevan el sello de seguridad: las sesiones abiertas dejan de valer.
    Task CambiarRolAsync(Guid idUsuario, string nuevoRol, CancellationToken cancelacion = default);

    Task DesactivarAsync(Guid idUsuario, CancellationToken cancelacion = default);

    Task<ResultadoDeContrasena> EstablecerContrasenaAsync(
        Guid idUsuario, string contrasena, bool debeCambiarContrasena, CancellationToken cancelacion = default);

    Task<int> ContarSuperusuariosActivosAsync(CancellationToken cancelacion = default);

    Task RevocarSesionesAsync(Guid idUsuario, CancellationToken cancelacion = default);
}
