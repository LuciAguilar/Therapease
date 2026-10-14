using Microsoft.AspNetCore.Identity;
using TherapEase.Infrastructure.Identidad.Entidades;
using TherapEase.Infrastructure.Identidad.Repositorios;

namespace TherapEase.UnitTests.Dobles;

// El GestorIdentidad real, sin el SELECT ... FOR UPDATE de PostgreSQL: el usuario "bloqueado" lo fija la prueba.
internal sealed class GestorIdentidadSinBloqueoDeFila(UserManagerFalso usuarios)
    : GestorIdentidad(null!, usuarios, new PasswordHasher<Usuario>())
{
    public Usuario? UsuarioBloqueado { get; set; }

    protected override Task<Usuario?> BloquearUsuarioAsync(string nombreNormalizado, CancellationToken cancelacion) =>
        Task.FromResult(UsuarioBloqueado);

    protected override Task<Usuario?> BloquearUsuarioAsync(Guid idUsuario, CancellationToken cancelacion) =>
        Task.FromResult(UsuarioBloqueado);
}
