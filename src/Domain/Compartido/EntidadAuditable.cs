namespace TherapEase.Domain.Compartido;

public abstract class EntidadAuditable
{
    public DateTimeOffset FechaAlta { get; private set; }

    public Guid IdUsuarioAlta { get; private set; }

    public DateTimeOffset? FechaActualizacion { get; private set; }

    public Guid? IdUsuarioActualizacion { get; private set; }

    public DateTimeOffset? FechaBaja { get; private set; }

    public Guid? IdUsuarioBaja { get; private set; }

    protected void RegistrarAlta(Guid idActor, DateTimeOffset ahora)
    {
        ExigirActor(idActor);
        FechaAlta = ahora.ToUniversalTime();
        IdUsuarioAlta = idActor;
    }

    protected void RegistrarActualizacion(Guid idActor, DateTimeOffset ahora)
    {
        ExigirActor(idActor);
        FechaActualizacion = ahora.ToUniversalTime();
        IdUsuarioActualizacion = idActor;
    }

    protected void RegistrarBaja(Guid idActor, DateTimeOffset ahora)
    {
        ExigirActor(idActor);
        FechaBaja = ahora.ToUniversalTime();
        IdUsuarioBaja = idActor;
    }

    protected void LimpiarBaja()
    {
        FechaBaja = null;
        IdUsuarioBaja = null;
    }

    protected static void ExigirActor(Guid idActor)
    {
        if (idActor == Guid.Empty)
        {
            throw new ReglaDeNegocioException("No se identificó a la persona que realiza el cambio.");
        }
    }
}
