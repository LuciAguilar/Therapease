using Microsoft.AspNetCore.Identity;
using TherapEase.Application.Identidad.Interfaces.Servicios;
using TherapEase.Infrastructure.Identidad.Entidades;

namespace TherapEase.Infrastructure.Identidad.Servicios;

public class EmisorDeSesion : IEmisorDeSesion
{
    private readonly SignInManager<Usuario> _inicioDeSesion;
    private readonly UserManager<Usuario> _usuarios;

    public EmisorDeSesion(SignInManager<Usuario> inicioDeSesion, UserManager<Usuario> usuarios)
    {
        _inicioDeSesion = inicioDeSesion;
        _usuarios = usuarios;
    }

    public async Task AbrirAsync(Guid idUsuario, CancellationToken cancelacion = default)
    {
        var usuario = await ObtenerActivoAsync(idUsuario);
        await _inicioDeSesion.SignInAsync(usuario, isPersistent: false);
    }

    public async Task RefrescarAsync(Guid idUsuario, CancellationToken cancelacion = default)
    {
        var usuario = await ObtenerActivoAsync(idUsuario);
        await _inicioDeSesion.RefreshSignInAsync(usuario);
    }

    public Task CerrarAsync(CancellationToken cancelacion = default) => _inicioDeSesion.SignOutAsync();

    private async Task<Usuario> ObtenerActivoAsync(Guid idUsuario) =>
        await _usuarios.FindByIdAsync(idUsuario.ToString()) is { Activo: true } usuario
            ? usuario
            : throw new InvalidOperationException("No se puede abrir sesión para este usuario.");
}
