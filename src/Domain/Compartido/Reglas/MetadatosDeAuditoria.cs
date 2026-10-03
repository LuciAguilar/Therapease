using TherapEase.Domain.Compartido.Entidades;
using TherapEase.Domain.Compartido.Excepciones;

namespace TherapEase.Domain.Compartido.Reglas;

internal static class MetadatosDeAuditoria
{
    public static void RegistrarAlta(EntidadAuditable entidad, Guid idActor, DateTimeOffset ahora)
    {
        ExigirActor(idActor);
        entidad.FechaAlta = ahora.ToUniversalTime();
        entidad.IdUsuarioAlta = idActor;
    }

    public static void RegistrarActualizacion(EntidadAuditable entidad, Guid idActor, DateTimeOffset ahora)
    {
        ExigirActor(idActor);
        entidad.FechaActualizacion = ahora.ToUniversalTime();
        entidad.IdUsuarioActualizacion = idActor;
    }

    public static void RegistrarBaja(EntidadAuditable entidad, Guid idActor, DateTimeOffset ahora)
    {
        ExigirActor(idActor);
        entidad.FechaBaja = ahora.ToUniversalTime();
        entidad.IdUsuarioBaja = idActor;
    }

    public static void LimpiarBaja(EntidadAuditable entidad)
    {
        entidad.FechaBaja = null;
        entidad.IdUsuarioBaja = null;
    }

    public static void ExigirActor(Guid idActor)
    {
        if (idActor == Guid.Empty)
        {
            throw new ReglaDeNegocioException("No se identificó a la persona que realiza el cambio.");
        }
    }
}
