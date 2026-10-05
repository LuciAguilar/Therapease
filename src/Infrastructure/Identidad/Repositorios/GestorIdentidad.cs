using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TherapEase.Application.Identidad.Interfaces.Repositorios;
using TherapEase.Application.Identidad.Modelos;
using TherapEase.Infrastructure.Data;
using TherapEase.Infrastructure.Identidad.Constantes;
using TherapEase.Infrastructure.Identidad.Entidades;

namespace TherapEase.Infrastructure.Identidad.Repositorios;

public class GestorIdentidad : IGestorIdentidad
{
    // Hash de una contraseña inexistente: iguala el tiempo de respuesta con un usuario desconocido (CU01 A1).
    private static readonly string HashFicticio = new PasswordHasher<Usuario>().HashPassword(new Usuario(), "contrasena-ficticia-sin-uso");

    private readonly ContextoDeDatos _contexto;
    private readonly UserManager<Usuario> _usuarios;
    private readonly IPasswordHasher<Usuario> _hasher;

    public GestorIdentidad(ContextoDeDatos contexto, UserManager<Usuario> usuarios, IPasswordHasher<Usuario> hasher)
    {
        _contexto = contexto;
        _usuarios = usuarios;
        _hasher = hasher;
    }

    public async Task<DatosUsuario?> ObtenerAsync(Guid idUsuario, CancellationToken cancelacion = default)
    {
        var usuario = await _usuarios.FindByIdAsync(idUsuario.ToString());
        return usuario is null ? null : await ADatosAsync(usuario);
    }

    public async Task<DatosUsuario?> ObtenerPorNombreAsync(string nombreUsuario, CancellationToken cancelacion = default)
    {
        var usuario = await _usuarios.FindByNameAsync(nombreUsuario);
        return usuario is null ? null : await ADatosAsync(usuario);
    }

    public async Task<DatosUsuario?> VerificarCredencialesAsync(string nombreUsuario, string contrasena, CancellationToken cancelacion = default)
    {
        var normalizado = _usuarios.NormalizeName(nombreUsuario);
        var usuario = await BloquearUsuarioAsync(normalizado, cancelacion);

        if (usuario is null)
        {
            _hasher.VerifyHashedPassword(new Usuario(), HashFicticio, contrasena);
            return null;
        }

        // Con el bloqueo vigente no se evalúa la contraseña: una correcta tampoco abre sesión (CU01 A2).
        if (!usuario.Activo || await _usuarios.IsLockedOutAsync(usuario))
        {
            _hasher.VerifyHashedPassword(new Usuario(), HashFicticio, contrasena);
            return null;
        }

        if (!await _usuarios.CheckPasswordAsync(usuario, contrasena))
        {
            await _usuarios.AccessFailedAsync(usuario);
            return null;
        }

        await _usuarios.ResetAccessFailedCountAsync(usuario);
        return await ADatosAsync(usuario);
    }

    public async Task<bool> VerificarContrasenaActualAsync(Guid idUsuario, string contrasena, CancellationToken cancelacion = default)
    {
        var usuario = await BloquearUsuarioAsync(idUsuario, cancelacion);
        if (usuario is not { Activo: true } || await _usuarios.IsLockedOutAsync(usuario))
        {
            return false;
        }

        if (!await _usuarios.CheckPasswordAsync(usuario, contrasena))
        {
            await _usuarios.AccessFailedAsync(usuario);
            return false;
        }

        await _usuarios.ResetAccessFailedCountAsync(usuario);
        return true;
    }

    public async Task<ResultadoCreacionDeUsuario> CrearAsync(
        Guid idUsuario, string nombreUsuario, string rol, string contrasena, bool debeCambiarContrasena, CancellationToken cancelacion = default)
    {
        var usuario = new Usuario
        {
            Id = idUsuario,
            UserName = nombreUsuario,
            Activo = true,
            DebeCambiarContrasena = debeCambiarContrasena,
            LockoutEnabled = true
        };

        var resultado = await _usuarios.CreateAsync(usuario, contrasena);
        if (!resultado.Succeeded)
        {
            return resultado.Errors.Any(e => e.Code is nameof(IdentityErrorDescriber.DuplicateUserName))
                ? ResultadoCreacionDeUsuario.NombreDuplicado
                : ResultadoCreacionDeUsuario.DatosInvalidos;
        }

        await _usuarios.AddToRoleAsync(usuario, rol);
        return ResultadoCreacionDeUsuario.Creado;
    }

    public async Task CambiarRolAsync(Guid idUsuario, string nuevoRol, CancellationToken cancelacion = default)
    {
        var usuario = await ObtenerEntidadAsync(idUsuario);
        var actuales = await _usuarios.GetRolesAsync(usuario);
        await _usuarios.RemoveFromRolesAsync(usuario, actuales);
        await _usuarios.AddToRoleAsync(usuario, nuevoRol);
        await _usuarios.UpdateSecurityStampAsync(usuario);
    }

    public async Task DesactivarAsync(Guid idUsuario, CancellationToken cancelacion = default)
    {
        var usuario = await ObtenerEntidadAsync(idUsuario);
        usuario.Activo = false;
        await _usuarios.UpdateAsync(usuario);
        await _usuarios.UpdateSecurityStampAsync(usuario);
    }

    public async Task<ResultadoDeContrasena> EstablecerContrasenaAsync(
        Guid idUsuario, string contrasena, bool debeCambiarContrasena, CancellationToken cancelacion = default)
    {
        var usuario = await ObtenerEntidadAsync(idUsuario);

        // Se valida antes de retirar la actual, para no dejar al usuario sin contraseña si la nueva no cumple.
        foreach (var validador in _usuarios.PasswordValidators)
        {
            if (!(await validador.ValidateAsync(_usuarios, usuario, contrasena)).Succeeded)
            {
                return ResultadoDeContrasena.NoCumpleReglas;
            }
        }

        // Quitar y agregar la contraseña renueva el sello de seguridad: las demás sesiones dejan de valer.
        await _usuarios.RemovePasswordAsync(usuario);
        var resultado = await _usuarios.AddPasswordAsync(usuario, contrasena);
        if (!resultado.Succeeded)
        {
            throw new InvalidOperationException("No se pudo establecer la contraseña.");
        }

        usuario.DebeCambiarContrasena = debeCambiarContrasena;
        await _usuarios.UpdateAsync(usuario);
        return ResultadoDeContrasena.Establecida;
    }

    public async Task<int> ContarSuperusuariosActivosAsync(CancellationToken cancelacion = default)
    {
        return await _contexto.UserRoles
            .Where(ur => ur.RoleId == IdentidadConstantes.IdRolSuperusuario)
            .Join(_contexto.Users.Where(u => u.Activo), ur => ur.UserId, u => u.Id, (ur, u) => u.Id)
            .CountAsync(cancelacion);
    }

    public async Task RevocarSesionesAsync(Guid idUsuario, CancellationToken cancelacion = default)
    {
        var usuario = await _usuarios.FindByIdAsync(idUsuario.ToString());
        if (usuario is not null)
        {
            await _usuarios.UpdateSecurityStampAsync(usuario);
        }
    }

    private async Task<Usuario> ObtenerEntidadAsync(Guid idUsuario) =>
        await _usuarios.FindByIdAsync(idUsuario.ToString())
        ?? throw new InvalidOperationException("El usuario no existe.");

    // Un intento a la vez por usuario (S-03): el bloqueo de fila dura hasta el commit de IUnidadDeTrabajo; sin LINQ encima, para que FOR UPDATE no quede en una subconsulta.
    private async Task<Usuario?> BloquearUsuarioAsync(string nombreNormalizado, CancellationToken cancelacion) =>
        (await _contexto.Users
            .FromSqlInterpolated($"""SELECT * FROM "AspNetUsers" WHERE "NormalizedUserName" = {nombreNormalizado} FOR UPDATE""")
            .ToListAsync(cancelacion)).SingleOrDefault();

    private async Task<Usuario?> BloquearUsuarioAsync(Guid idUsuario, CancellationToken cancelacion) =>
        (await _contexto.Users
            .FromSqlInterpolated($"""SELECT * FROM "AspNetUsers" WHERE "Id" = {idUsuario} FOR UPDATE""")
            .ToListAsync(cancelacion)).SingleOrDefault();

    private async Task<DatosUsuario> ADatosAsync(Usuario usuario)
    {
        var roles = await _usuarios.GetRolesAsync(usuario);
        return new DatosUsuario(usuario.Id, usuario.UserName!, roles.FirstOrDefault() ?? string.Empty, usuario.Activo, usuario.DebeCambiarContrasena);
    }
}
