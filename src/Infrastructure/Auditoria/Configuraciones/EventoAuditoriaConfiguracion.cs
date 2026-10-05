using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TherapEase.Domain.Auditoria.Entidades;

namespace TherapEase.Infrastructure.Auditoria.Configuraciones;

public class EventoAuditoriaConfiguracion : IEntityTypeConfiguration<EventoAuditoria>
{
    public void Configure(EntityTypeBuilder<EventoAuditoria> builder)
    {
        builder.ToTable("EventoAuditoria");

        builder.HasKey(e => e.IdEvento);
        builder.Property(e => e.IdEvento).ValueGeneratedNever();
        builder.Property(e => e.TipoRegistro).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.Accion).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.CamposAfectados).IsRequired();
        builder.Property(e => e.Hecho).IsRequired().HasMaxLength(EventoAuditoria.LongitudMaximaHecho);

        builder.HasIndex(e => new { e.TipoRegistro, e.IdRegistro, e.FechaEvento });
        builder.HasIndex(e => e.FechaEvento);
    }
}
