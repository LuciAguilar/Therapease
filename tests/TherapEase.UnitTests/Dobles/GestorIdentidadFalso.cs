using TherapEase.Application.Identidad.Interfaces.Repositorios;
using TherapEase.Application.Identidad.Modelos;
using TherapEase.Domain.Identidad.Constantes;

namespace TherapEase.UnitTests.Dobles;

internal class GestorIdentidadFalso : IGestorIdentidad
{
    public Dictionary<Guid, DatosUsuario> Usuarios { get; } = [];

    public Dictionary<Guid, string> Contrasenas { get; } = [];

    public Dictionary<Guid, int> IntentosFallidos { get; } = [];

    public HashSet<Guid> Bloqueados { get; } = [];

    public List<(Guid Id, string Nombre, string Rol, string Contrasena, bool DebeCambiar)> Creaciones { get; } = [];

    public List<(Guid Id, string Contrasena, bool DebeCambiar)> ContrasenasEstablecidas { get; } = [];

    public List<Guid> SesionesRevocadas { get; } = [];

    public Exception? ExcepcionAlCambiarRol { get; set; }

    public DatosUsuario Agregar(string nombre, string rol, string contrasena = "contrasena-valida-123", bool activo = true, bool debeCambiar = false)
    {
        var usuario = new DatosUsuario(Guid.NewGuid(), nombre, rol, activo, debeCambiar);
        Usuarios[usuario.IdUsuario] = usuario;
        Contrasenas[usuario.IdUsuario] = contrasena;
        return usuario;
    }

    public Task<DatosUsuario?> ObtenerAsync(Guid idUsuario, CancellationToken cancelacion = default) =>
        Task.FromResult(Usuarios.GetValueOrDefault(idUsuario));

    public Task<DatosUsuario?> ObtenerPorNombreAsync(string nombreUsuario, CancellationToken cancelacion = default) =>
        Task.FromResult(Usuarios.Values.FirstOrDefault(u => string.Equals(u.NombreUsuario, nombreUsuario, StringComparison.OrdinalIgnoreCase)));

    public virtual async Task<DatosUsuario?> VerificarCredencialesAsync(string nombreUsuario, string contrasena, CancellationToken cancelacion = default)
    {
        var usuario = await ObtenerPorNombreAsync(nombreUsuario, cancelacion);
        if (usuario is null || !usuario.Activo || Bloqueados.Contains(usuario.IdUsuario))
        {
            return null;
        }

        if (Contrasenas[usuario.IdUsuario] != contrasena)
        {
            IntentosFallidos[usuario.IdUsuario] = IntentosFallidos.GetValueOrDefault(usuario.IdUsuario) + 1;
            return null;
        }

        return usuario;
    }

    public Task<bool> VerificarContrasenaActualAsync(Guid idUsuario, string contrasena, CancellationToken cancelacion = default)
    {
        if (Contrasenas.GetValueOrDefault(idUsuario) == contrasena)
        {
            return Task.FromResult(true);
        }

        IntentosFallidos[idUsuario] = IntentosFallidos.GetValueOrDefault(idUsuario) + 1;
        return Task.FromResult(false);
    }

    public Task<ResultadoCreacionDeUsuario> CrearAsync(
        Guid idUsuario, string nombreUsuario, string rol, string contrasena, bool debeCambiarContrasena, CancellationToken cancelacion = default)
    {
        Creaciones.Add((idUsuario, nombreUsuario, rol, contrasena, debeCambiarContrasena));
        if (Usuarios.Values.Any(u => string.Equals(u.NombreUsuario, nombreUsuario, StringComparison.OrdinalIgnoreCase)))
        {
            return Task.FromResult(ResultadoCreacionDeUsuario.NombreDuplicado);
        }

        Usuarios[idUsuario] = new DatosUsuario(idUsuario, nombreUsuario, rol, true, debeCambiarContrasena);
        Contrasenas[idUsuario] = contrasena;
        return Task.FromResult(ResultadoCreacionDeUsuario.Creado);
    }

    public Task CambiarRolAsync(Guid idUsuario, string nuevoRol, CancellationToken cancelacion = default)
    {
        if (ExcepcionAlCambiarRol is not null)
        {
            throw ExcepcionAlCambiarRol;
        }

        Usuarios[idUsuario] = Usuarios[idUsuario] with { Rol = nuevoRol };
        return Task.CompletedTask;
    }

    public Task DesactivarAsync(Guid idUsuario, CancellationToken cancelacion = default)
    {
        Usuarios[idUsuario] = Usuarios[idUsuario] with { Activo = false };
        return Task.CompletedTask;
    }

    public Task<ResultadoDeContrasena> EstablecerContrasenaAsync(
        Guid idUsuario, string contrasena, bool debeCambiarContrasena, CancellationToken cancelacion = default)
    {
        if (contrasena.Length < 12)
        {
            return Task.FromResult(ResultadoDeContrasena.NoCumpleReglas);
        }

        ContrasenasEstablecidas.Add((idUsuario, contrasena, debeCambiarContrasena));
        Contrasenas[idUsuario] = contrasena;
        Usuarios[idUsuario] = Usuarios[idUsuario] with { DebeCambiarContrasena = debeCambiarContrasena };
        return Task.FromResult(ResultadoDeContrasena.Establecida);
    }

    public Task<int> ContarSuperusuariosActivosAsync(CancellationToken cancelacion = default) =>
        Task.FromResult(Usuarios.Values.Count(u => u.Rol == NombresDeRol.Superusuario && u.Activo));

    public Task RevocarSesionesAsync(Guid idUsuario, CancellationToken cancelacion = default)
    {
        SesionesRevocadas.Add(idUsuario);
        return Task.CompletedTask;
    }
}
