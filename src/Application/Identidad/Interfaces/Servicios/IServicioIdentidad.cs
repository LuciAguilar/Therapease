using TherapEase.Application.Identidad.Modelos;

namespace TherapEase.Application.Identidad.Interfaces.Servicios;

// Contrato público del módulo Identidad (ObtenerIdentidadYPermisos, 08C §4).
public interface IServicioIdentidad
{
    Task<IdentidadYPermisos?> ObtenerIdentidadYPermisosAsync(
        Guid idUsuario, 
        CancellationToken cancelacion = default
        );
}
