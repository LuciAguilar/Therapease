namespace TherapEase.Application.Identidad.Modelos;

public sealed record DatosUsuario(
    Guid IdUsuario, 
    string NombreUsuario, 
    string Rol, 
    bool Activo, 
    bool DebeCambiarContrasena
    );
