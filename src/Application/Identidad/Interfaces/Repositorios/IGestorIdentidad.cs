using TherapEase.Application.Identidad.Modelos;

namespace TherapEase.Application.Identidad.Interfaces.Repositorios;

// Puerto de persistencia del módulo Identidad; lo implementa ASP.NET Core Identity en Infrastructure.
public interface IGestorIdentidad
{
    Task<DatosUsuario?> ObtenerAsync(Guid idUsuario, CancellationToken cancelacion = default);

    // Lectura sin bloqueos. Coincidencia parcial con el nombre de usuario o el nombre completo sin distinguir mayúsculas (nulo = todos), orden por nombre de usuario, incluye cuentas desactivadas.
    Task<IReadOnlyList<DatosUsuario>> ListarAsync(string? busqueda, int maximo, CancellationToken cancelacion = default);

    Task<FichaDeUsuario?> ObtenerFichaAsync(Guid idUsuario, CancellationToken cancelacion = default);

    // Solo para el procedimiento de operador; el acceso usa VerificarCredencialesAsync para no revelar si el usuario existe.
    Task<DatosUsuario?> ObtenerPorNombreAsync(string nombreUsuario, CancellationToken cancelacion = default);

    // Usuario nulo ante cualquier fallo sin distinguirlo (S-02); cuenta los fallos para el bloqueo y serializa los intentos de un usuario (S-03).
    // Indica si este intento fue el que inició el bloqueo, para registrarlo una sola vez: con el bloqueo vigente no cuenta ni vuelve a informar.
    Task<ResultadoDeVerificacion> VerificarCredencialesAsync(string nombreUsuario, string contrasena, CancellationToken cancelacion = default);

    // Un intento fallido también cuenta para el bloqueo y puede iniciarlo. Como la anterior, debe ejecutarse dentro de IUnidadDeTrabajo.
    Task<ResultadoDeContrasenaActual> VerificarContrasenaActualAsync(Guid idUsuario, string contrasena, CancellationToken cancelacion = default);

    // Nombre completo y correo son opcionales aquí (el comando de arranque crea al primer superusuario sin ellos); CU02 los exige antes de llamar. Llegan ya validados.
    Task<ResultadoCreacionDeUsuario> CrearAsync(
        Guid idUsuario, string nombreUsuario, string rol, string contrasena, bool debeCambiarContrasena,
        string? nombreCompleto = null, string? correo = null, CancellationToken cancelacion = default);

    // Reemplaza nombre completo y correo (nulo = sin correo) y escribe solo lo que cambió. No renueva el sello de seguridad: las sesiones abiertas siguen válidas.
    // Debe ejecutarse dentro de IUnidadDeTrabajo, junto con su evento.
    Task<CamposActualizados> ActualizarDatosAsync(
        Guid idUsuario, string nombreCompleto, string? correo, CancellationToken cancelacion = default);

    // Cambiar rol, desactivar y establecer contraseña renuevan el sello de seguridad: las sesiones abiertas dejan de valer.
    Task CambiarRolAsync(Guid idUsuario, string nuevoRol, CancellationToken cancelacion = default);

    Task DesactivarAsync(Guid idUsuario, CancellationToken cancelacion = default);

    Task<ResultadoDeContrasena> EstablecerContrasenaAsync(
        Guid idUsuario, string contrasena, bool debeCambiarContrasena, CancellationToken cancelacion = default);

    Task<int> ContarSuperusuariosActivosAsync(CancellationToken cancelacion = default);

    Task RevocarSesionesAsync(Guid idUsuario, CancellationToken cancelacion = default);
}
