using Microsoft.AspNetCore.Identity;

namespace TherapEase.Infrastructure.Identidad.Entidades;

// Identity administra hash, bloqueo (AccessFailedCount/LockoutEnd) y sello; aquí solo lo propio de TherapEase.
public class Usuario : IdentityUser<Guid>
{
    public bool Activo { get; set; } = true;

    public bool DebeCambiarContrasena { get; set; }

    // Vacío hasta que el superusuario lo capture: las cuentas anteriores y la del comando de arranque no lo tienen.
    public string NombreCompleto { get; set; } = string.Empty;
}
