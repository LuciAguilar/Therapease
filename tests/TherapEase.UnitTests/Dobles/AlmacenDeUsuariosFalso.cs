using Microsoft.AspNetCore.Identity;
using TherapEase.Infrastructure.Identidad.Entidades;

namespace TherapEase.UnitTests.Dobles;

// UserManager exige un almacén al construirse; UserManagerFalso sustituye todas las operaciones que se usan, así que este no se invoca.
internal sealed class AlmacenDeUsuariosFalso : IUserStore<Usuario>
{
    public void Dispose()
    {
    }

    public Task<string> GetUserIdAsync(Usuario user, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<string?> GetUserNameAsync(Usuario user, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task SetUserNameAsync(Usuario user, string? userName, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<string?> GetNormalizedUserNameAsync(Usuario user, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task SetNormalizedUserNameAsync(Usuario user, string? normalizedName, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<IdentityResult> CreateAsync(Usuario user, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<IdentityResult> UpdateAsync(Usuario user, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<IdentityResult> DeleteAsync(Usuario user, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<Usuario?> FindByIdAsync(string userId, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<Usuario?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken) => throw new NotSupportedException();
}
