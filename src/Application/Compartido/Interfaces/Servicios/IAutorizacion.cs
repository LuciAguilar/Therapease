using TherapEase.Domain.Compartido.Entidades.Enums;

namespace TherapEase.Application.Compartido.Interfaces.Servicios;

public interface IAutorizacion
{
    // Comprueba el permiso vigente de la sesión actual contra el estado actual del usuario, no contra lo guardado en la cookie.
    Task<bool> TienePermisoAsync(Permiso permiso, CancellationToken cancelacion = default);
}
