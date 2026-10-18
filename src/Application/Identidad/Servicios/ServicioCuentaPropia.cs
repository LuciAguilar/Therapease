using TherapEase.Application.Compartido.Interfaces.Servicios;
using TherapEase.Application.Compartido.Modelos;
using TherapEase.Application.Identidad.Constantes;
using TherapEase.Application.Identidad.Interfaces.Repositorios;
using TherapEase.Application.Identidad.Interfaces.Servicios;
using TherapEase.Application.Identidad.Modelos;

namespace TherapEase.Application.Identidad.Servicios;

public class ServicioCuentaPropia : IServicioCuentaPropia
{
    private readonly IGestorIdentidad _gestor;
    private readonly IUsuarioActual _usuarioActual;

    public ServicioCuentaPropia(IGestorIdentidad gestor, IUsuarioActual usuarioActual)
    {
        _gestor = gestor;
        _usuarioActual = usuarioActual;
    }

    public async Task<RespuestaServicio<FichaDeUsuario>> ObtenerMisDatosAsync(CancellationToken cancelacion = default)
    {
        if (_usuarioActual.IdUsuario is not { } idUsuario)
        {
            return RespuestaServicio<FichaDeUsuario>.Fallida(MensajesDeIdentidad.SinPermiso);
        }

        // Una cuenta desactivada ya no tiene sesión válida; si llegara aquí se trata como sin acceso.
        var ficha = await _gestor.ObtenerFichaAsync(idUsuario, cancelacion);
        return ficha is { Datos.Activo: true }
            ? RespuestaServicio<FichaDeUsuario>.Correcta(ficha)
            : RespuestaServicio<FichaDeUsuario>.Fallida(MensajesDeIdentidad.SinPermiso);
    }
}
