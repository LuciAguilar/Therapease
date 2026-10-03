using TherapEase.Application.Compartido.Interfaces.Servicios;
using TherapEase.Application.Identidad.Interfaces.Repositorios;
using TherapEase.Domain.Compartido.Entidades.Enums;
using TherapEase.Domain.Identidad.Reglas;

namespace TherapEase.Application.Identidad.Servicios;

public class Autorizacion : IAutorizacion
{
    private readonly IUsuarioActual _usuarioActual;
    private readonly IGestorIdentidad _gestor;

    public Autorizacion(IUsuarioActual usuarioActual, IGestorIdentidad gestor)
    {
        _usuarioActual = usuarioActual;
        _gestor = gestor;
    }

    public async Task<bool> TienePermisoAsync(Permiso permiso, CancellationToken cancelacion = default)
    {
        if (_usuarioActual.IdUsuario is not { } idUsuario)
        {
            return false;
        }

        var usuario = await _gestor.ObtenerAsync(idUsuario, cancelacion);
        return usuario is { Activo: true, DebeCambiarContrasena: false }
            && MatrizDePermisos.RolTienePermiso(usuario.Rol, permiso);
    }
}
