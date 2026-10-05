using TherapEase.Application.Identidad.Interfaces.Repositorios;
using TherapEase.Application.Identidad.Interfaces.Servicios;
using TherapEase.Application.Identidad.Modelos;
using TherapEase.Domain.Identidad.Reglas;

namespace TherapEase.Application.Identidad.Servicios;

public class ServicioIdentidad : IServicioIdentidad
{
    private readonly IGestorIdentidad _gestor;

    public ServicioIdentidad(IGestorIdentidad gestor)
    {
        _gestor = gestor;
    }

    public async Task<IdentidadYPermisos?> ObtenerIdentidadYPermisosAsync(Guid idUsuario, CancellationToken cancelacion = default)
    {
        var usuario = await _gestor.ObtenerAsync(idUsuario, cancelacion);
        if (usuario is null)
        {
            return null;
        }

        var permisos = usuario.Activo && !usuario.DebeCambiarContrasena
            ? MatrizDePermisos.PermisosDelRol(usuario.Rol)
            : [];
        return new IdentidadYPermisos(usuario, permisos);
    }
}
