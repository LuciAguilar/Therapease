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

    public List<(Guid Id, string Nombre, string Rol, string Contrasena, bool DebeCambiar, string? NombreCompleto, string? Correo)> Creaciones { get; } = [];

    public List<(Guid Id, string NombreCompleto, string? Correo)> ActualizacionesDeDatos { get; } = [];

    public Exception? ExcepcionAlActualizarDatos { get; set; }

    public List<(Guid Id, string Contrasena, bool DebeCambiar)> ContrasenasEstablecidas { get; } = [];

    public List<Guid> SesionesRevocadas { get; } = [];

    public Exception? ExcepcionAlCambiarRol { get; set; }

    public DatosUsuario Agregar(
        string nombre, string rol, string contrasena = "contrasena-valida-123", bool activo = true, bool debeCambiar = false, string nombreCompleto = "")
    {
        var usuario = new DatosUsuario(Guid.NewGuid(), nombre, rol, activo, debeCambiar, nombreCompleto);
        Usuarios[usuario.IdUsuario] = usuario;
        Contrasenas[usuario.IdUsuario] = contrasena;
        return usuario;
    }

    public Task<DatosUsuario?> ObtenerAsync(Guid idUsuario, CancellationToken cancelacion = default) =>
        Task.FromResult(Usuarios.GetValueOrDefault(idUsuario));

    public Dictionary<Guid, string> Correos { get; } = [];

    public List<(string? Busqueda, int Maximo)> Listados { get; } = [];

    public Task<IReadOnlyList<DatosUsuario>> ListarAsync(string? busqueda, int maximo, CancellationToken cancelacion = default)
    {
        Listados.Add((busqueda, maximo));
        IReadOnlyList<DatosUsuario> resultado = Usuarios.Values
            .Where(u => busqueda is null
                || u.NombreUsuario.Contains(busqueda, StringComparison.OrdinalIgnoreCase)
                || u.NombreCompleto.Contains(busqueda, StringComparison.OrdinalIgnoreCase))
            .OrderBy(u => u.NombreUsuario, StringComparer.OrdinalIgnoreCase)
            .Take(maximo)
            .ToList();
        return Task.FromResult(resultado);
    }

    public Task<FichaDeUsuario?> ObtenerFichaAsync(Guid idUsuario, CancellationToken cancelacion = default) =>
        Task.FromResult(Usuarios.TryGetValue(idUsuario, out var datos)
            ? new FichaDeUsuario(datos, Correos.GetValueOrDefault(idUsuario))
            : null);

    public Task<DatosUsuario?> ObtenerPorNombreAsync(string nombreUsuario, CancellationToken cancelacion = default) =>
        Task.FromResult(Usuarios.Values.FirstOrDefault(u => string.Equals(u.NombreUsuario, nombreUsuario, StringComparison.OrdinalIgnoreCase)));

    // Como el gestor real, simula el límite de Identity: al llegar a él la cuenta queda bloqueada y el contador vuelve a cero. Sin límite, solo cuenta.
    public int? LimiteDeFallos { get; set; }

    // Un intento a la vez, como el bloqueo de fila de la base (S-03): permite probar intentos simultáneos.
    private readonly object _candadoDeIntentos = new();

    public virtual Task<ResultadoDeVerificacion> VerificarCredencialesAsync(string nombreUsuario, string contrasena, CancellationToken cancelacion = default)
    {
        lock (_candadoDeIntentos)
        {
            var usuario = Usuarios.Values.FirstOrDefault(u => string.Equals(u.NombreUsuario, nombreUsuario, StringComparison.OrdinalIgnoreCase));
            if (usuario is null || !usuario.Activo || Bloqueados.Contains(usuario.IdUsuario))
            {
                return Task.FromResult(new ResultadoDeVerificacion(null));
            }

            return Task.FromResult(Contrasenas[usuario.IdUsuario] == contrasena
                ? new ResultadoDeVerificacion(usuario)
                : new ResultadoDeVerificacion(null, RegistrarFallo(usuario.IdUsuario) ? usuario.IdUsuario : null));
        }
    }

    public Task<ResultadoDeContrasenaActual> VerificarContrasenaActualAsync(Guid idUsuario, string contrasena, CancellationToken cancelacion = default)
    {
        lock (_candadoDeIntentos)
        {
            if (Bloqueados.Contains(idUsuario))
            {
                return Task.FromResult(new ResultadoDeContrasenaActual(Correcta: false));
            }

            return Task.FromResult(Contrasenas.GetValueOrDefault(idUsuario) == contrasena
                ? new ResultadoDeContrasenaActual(Correcta: true)
                : new ResultadoDeContrasenaActual(Correcta: false, BloqueoNuevo: RegistrarFallo(idUsuario)));
        }
    }

    private bool RegistrarFallo(Guid idUsuario)
    {
        var intentos = IntentosFallidos.GetValueOrDefault(idUsuario) + 1;
        if (LimiteDeFallos is { } limite && intentos >= limite)
        {
            Bloqueados.Add(idUsuario);
            IntentosFallidos[idUsuario] = 0;
            return true;
        }

        IntentosFallidos[idUsuario] = intentos;
        return false;
    }

    public Task<ResultadoCreacionDeUsuario> CrearAsync(
        Guid idUsuario, string nombreUsuario, string rol, string contrasena, bool debeCambiarContrasena,
        string? nombreCompleto = null, string? correo = null, CancellationToken cancelacion = default)
    {
        Creaciones.Add((idUsuario, nombreUsuario, rol, contrasena, debeCambiarContrasena, nombreCompleto, correo));
        if (Usuarios.Values.Any(u => string.Equals(u.NombreUsuario, nombreUsuario, StringComparison.OrdinalIgnoreCase)))
        {
            return Task.FromResult(ResultadoCreacionDeUsuario.NombreDuplicado);
        }

        Usuarios[idUsuario] = new DatosUsuario(idUsuario, nombreUsuario, rol, true, debeCambiarContrasena, nombreCompleto ?? string.Empty);
        if (correo is not null)
        {
            Correos[idUsuario] = correo;
        }

        Contrasenas[idUsuario] = contrasena;
        return Task.FromResult(ResultadoCreacionDeUsuario.Creado);
    }

    // Igual que el gestor real: compara con lo guardado y solo informa de lo que cambió; no toca sesiones.
    public Task<CamposActualizados> ActualizarDatosAsync(Guid idUsuario, string nombreCompleto, string? correo, CancellationToken cancelacion = default)
    {
        if (ExcepcionAlActualizarDatos is not null)
        {
            throw ExcepcionAlActualizarDatos;
        }

        var actual = Usuarios[idUsuario];
        var correoActual = Correos.GetValueOrDefault(idUsuario);
        var cambios = new CamposActualizados(
            NombreCompleto: actual.NombreCompleto != nombreCompleto,
            Correo: correoActual != correo);
        if (cambios.Hubo)
        {
            Usuarios[idUsuario] = actual with { NombreCompleto = nombreCompleto };
            if (correo is null)
            {
                Correos.Remove(idUsuario);
            }
            else
            {
                Correos[idUsuario] = correo;
            }
        }

        ActualizacionesDeDatos.Add((idUsuario, nombreCompleto, correo));
        return Task.FromResult(cambios);
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
