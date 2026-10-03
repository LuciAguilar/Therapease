using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TherapEase.Domain.Citas;
using TherapEase.Domain.Pacientes;

namespace TherapEase.Infrastructure.Persistencia.Citas;

// El cruce de citas activas y la coherencia paciente-cita se imponen en la migración con SQL propio.
public class CitaConfiguracion : IEntityTypeConfiguration<Cita>
{
    public void Configure(EntityTypeBuilder<Cita> builder)
    {
        builder.ToTable("Cita", tabla =>
        {
            tabla.HasCheckConstraint("CK_Cita_InicioAntesDeFin", "\"Inicio\" < \"Fin\"");
            tabla.HasCheckConstraint("CK_Cita_Ambito", "\"Ambito\" IN ('Escolar', 'Independiente')");
            tabla.HasCheckConstraint("CK_Cita_Estado", "\"Estado\" IN ('Agendada', 'Cancelada')");
            tabla.HasCheckConstraint("CK_Cita_EstadoPago", "\"EstadoPago\" IN ('Pendiente', 'Pagado')");
            tabla.HasCheckConstraint(
                "CK_Cita_BajaCoherente",
                "(\"Condicion\" = 'Vigente' AND \"FechaBaja\" IS NULL AND \"IdUsuarioBaja\" IS NULL) " +
                "OR (\"Condicion\" = 'Baja' AND \"FechaBaja\" IS NOT NULL AND \"IdUsuarioBaja\" IS NOT NULL)");
        });

        builder.HasKey(c => c.IdCita);
        builder.Property(c => c.IdCita).ValueGeneratedNever();
        builder.Property(c => c.Ambito).HasConversion<string>().HasMaxLength(20);
        builder.Property(c => c.Estado).HasConversion<string>().HasMaxLength(10);
        builder.Property(c => c.EstadoPago).HasConversion<string>().HasMaxLength(10);
        builder.Property(c => c.Condicion).HasConversion<string>().HasMaxLength(10);
        builder.Property(c => c.Version).IsConcurrencyToken();

        builder.HasOne<Paciente>()
            .WithMany()
            .HasForeignKey(c => c.IdPaciente)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(c => c.EsActiva);
    }
}
