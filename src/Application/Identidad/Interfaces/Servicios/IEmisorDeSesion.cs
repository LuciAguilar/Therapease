namespace TherapEase.Application.Identidad.Interfaces.Servicios;

// Emite o retira la cookie de sesión; es la única parte de Identidad que toca HTTP.
public interface IEmisorDeSesion
{
    Task AbrirAsync(Guid idUsuario, CancellationToken cancelacion = default);

    // Reemite la cookie con el sello vigente tras un cambio de contraseña propio.
    Task RefrescarAsync(Guid idUsuario, CancellationToken cancelacion = default);

    Task CerrarAsync(CancellationToken cancelacion = default);
}
