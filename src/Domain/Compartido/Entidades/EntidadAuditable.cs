namespace TherapEase.Domain.Compartido.Entidades;

// Solo datos: quién y cuándo dio de alta, actualizó o dio de baja. Las operaciones están en MetadatosDeAuditoria.
public abstract class EntidadAuditable
{
    public DateTimeOffset FechaAlta { get; internal set; }

    public Guid IdUsuarioAlta { get; internal set; }

    public DateTimeOffset? FechaActualizacion { get; internal set; }

    public Guid? IdUsuarioActualizacion { get; internal set; }

    public DateTimeOffset? FechaBaja { get; internal set; }

    public Guid? IdUsuarioBaja { get; internal set; }
}
