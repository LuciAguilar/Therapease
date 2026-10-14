using Microsoft.AspNetCore.Identity;
using TherapEase.Infrastructure.Identidad.Entidades;

namespace TherapEase.UnitTests.Dobles;

// Sustituye las operaciones de Identity que usa GestorIdentidad. Cada una devuelve éxito salvo que la prueba fije otro resultado por nombre.
internal sealed class UserManagerFalso : UserManager<Usuario>
{
    public UserManagerFalso()
        : base(new AlmacenDeUsuariosFalso(), null!, null!, [], [], null!, null!, null!, null!)
    {
    }

    public Dictionary<string, Usuario> Usuarios { get; } = [];

    public Dictionary<string, IdentityResult> Resultados { get; } = [];

    public List<string> Llamadas { get; } = [];

    public IList<string> RolesActuales { get; set; } = ["Usuaria"];

    public bool Bloqueado { get; set; }

    public bool ContrasenaCorrecta { get; set; } = true;

    public static IdentityResult Falla(string codigo = "FalloSimulado") =>
        IdentityResult.Failed(new IdentityError { Code = codigo, Description = "Descripción con datos privados que no debe salir en el mensaje." });

    private Task<IdentityResult> Registrar(string operacion)
    {
        Llamadas.Add(operacion);
        return Task.FromResult(Resultados.GetValueOrDefault(operacion, IdentityResult.Success));
    }

    public override string? NormalizeName(string? name) => name?.ToUpperInvariant();

    public override Task<Usuario?> FindByIdAsync(string userId) => Task.FromResult(Usuarios.GetValueOrDefault(userId));

    public override Task<IdentityResult> CreateAsync(Usuario user, string password) => Registrar(nameof(CreateAsync));

    public override Task<IdentityResult> UpdateAsync(Usuario user) => Registrar(nameof(UpdateAsync));

    public override Task<IdentityResult> AddToRoleAsync(Usuario user, string role) => Registrar(nameof(AddToRoleAsync));

    public override Task<IdentityResult> RemoveFromRolesAsync(Usuario user, IEnumerable<string> roles) => Registrar(nameof(RemoveFromRolesAsync));

    public override Task<IdentityResult> UpdateSecurityStampAsync(Usuario user) => Registrar(nameof(UpdateSecurityStampAsync));

    public override Task<IdentityResult> RemovePasswordAsync(Usuario user) => Registrar(nameof(RemovePasswordAsync));

    public override Task<IdentityResult> AddPasswordAsync(Usuario user, string password) => Registrar(nameof(AddPasswordAsync));

    public override Task<IdentityResult> AccessFailedAsync(Usuario user) => Registrar(nameof(AccessFailedAsync));

    public override Task<IdentityResult> ResetAccessFailedCountAsync(Usuario user) => Registrar(nameof(ResetAccessFailedCountAsync));

    public override Task<IList<string>> GetRolesAsync(Usuario user) => Task.FromResult(RolesActuales);

    public override Task<bool> IsLockedOutAsync(Usuario user) => Task.FromResult(Bloqueado);

    public override Task<bool> CheckPasswordAsync(Usuario user, string password) => Task.FromResult(ContrasenaCorrecta);
}
