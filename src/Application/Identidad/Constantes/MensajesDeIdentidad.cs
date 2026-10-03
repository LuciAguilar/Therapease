namespace TherapEase.Application.Identidad.Constantes;

public static class MensajesDeIdentidad
{
    // El mismo texto para usuario inexistente, contraseña incorrecta, bloqueo y desactivación (CU01 A1, S-02).
    public const string CredencialesInvalidas = "Usuario o contraseña incorrectos.";

    public const string SinPermiso = "No tienes permiso para realizar esta operación.";
    public const string UsuarioNoEncontrado = "No se encontró el usuario.";
    public const string DatosDeUsuarioInvalidos = "Los datos del usuario no son válidos.";
    public const string NombreDeUsuarioDuplicado = "El nombre de usuario ya existe.";
    public const string UltimoSuperusuario = "Debe quedar al menos un superusuario activo.";
    public const string CambioDeContrasenaNoRealizado = "No fue posible cambiar la contraseña. Verifica los datos e intenta de nuevo.";
    public const string ContrasenaNuevaNoCumpleReglas = "La contraseña nueva no cumple las reglas de seguridad.";
}
