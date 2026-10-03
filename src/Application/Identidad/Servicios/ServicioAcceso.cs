using TherapEase.Application.Compartido.Interfaces.Repositorios;
using TherapEase.Application.Compartido.Interfaces.Servicios;
using TherapEase.Application.Compartido.Modelos;
using TherapEase.Application.Identidad.Constantes;
using TherapEase.Application.Identidad.Interfaces.Repositorios;
using TherapEase.Application.Identidad.Interfaces.Servicios;
using TherapEase.Application.Identidad.Modelos;

namespace TherapEase.Application.Identidad.Servicios;

public class ServicioAcceso : IServicioAcceso
{
    private readonly IGestorIdentidad _gestor;
    private readonly IEmisorDeSesion _emisor;
    private readonly IUnidadDeTrabajo _unidad;
    private readonly IUsuarioActual _usuarioActual;

    public ServicioAcceso(IGestorIdentidad gestor, IEmisorDeSesion emisor, IUnidadDeTrabajo unidad, IUsuarioActual usuarioActual)
    {
        _gestor = gestor;
        _emisor = emisor;
        _unidad = unidad;
        _usuarioActual = usuarioActual;
    }

    public async Task<RespuestaServicio<DatosUsuario>> IniciarSesionAsync(
        string nombreUsuario, string contrasena, CancellationToken cancelacion = default)
    {
        var nombre = nombreUsuario?.Trim();
        if (string.IsNullOrEmpty(nombre) || string.IsNullOrEmpty(contrasena))
        {
            return RespuestaServicio<DatosUsuario>.Fallida(MensajesDeIdentidad.CredencialesInvalidas);
        }

        // La verificación y el conteo de fallos se confirman siempre, incluso cuando el acceso se niega.
        DatosUsuario? usuario = null;
        await _unidad.EjecutarAsync(async ct => usuario = await _gestor.VerificarCredencialesAsync(nombre, contrasena, ct), cancelacion);

        if (usuario is null)
        {
            return RespuestaServicio<DatosUsuario>.Fallida(MensajesDeIdentidad.CredencialesInvalidas);
        }

        // La sesión solo se abre después del commit (HU01 CA5).
        await _emisor.AbrirAsync(usuario.IdUsuario, cancelacion);
        return RespuestaServicio<DatosUsuario>.Correcta(usuario);
    }

    public async Task CerrarSesionAsync(CancellationToken cancelacion = default)
    {
        if (_usuarioActual.IdUsuario is { } idUsuario)
        {
            await _gestor.RevocarSesionesAsync(idUsuario, cancelacion);
        }

        await _emisor.CerrarAsync(cancelacion);
    }
}
