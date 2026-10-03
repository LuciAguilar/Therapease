using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TherapEase.Domain.Pacientes;

namespace TherapEase.Infrastructure.Persistencia.Pacientes;

public class PacienteConfiguracion : IEntityTypeConfiguration<Paciente>
{
    public void Configure(EntityTypeBuilder<Paciente> builder)
    {
        builder.ToTable("Paciente", tabla =>
        {
            tabla.HasCheckConstraint("CK_Paciente_NombreNoVacio", "length(btrim(\"Nombre\")) > 0");
            tabla.HasCheckConstraint(
                "CK_Paciente_BajaCoherente",
                "(\"Condicion\" = 'Vigente' AND \"FechaBaja\" IS NULL AND \"IdUsuarioBaja\" IS NULL) " +
                "OR (\"Condicion\" = 'Baja' AND \"FechaBaja\" IS NOT NULL AND \"IdUsuarioBaja\" IS NOT NULL)");
        });

        builder.HasKey(p => p.IdPaciente);
        builder.Property(p => p.IdPaciente).ValueGeneratedNever();
        builder.Property(p => p.Nombre).IsRequired().HasMaxLength(Paciente.LongitudMaximaNombre);
        builder.Property(p => p.Contacto).HasMaxLength(Paciente.LongitudMaximaContacto);
        builder.Property(p => p.Condicion).HasConversion<string>().HasMaxLength(10);
        builder.Property(p => p.Version).IsConcurrencyToken();

        builder.HasMany(p => p.Ambitos)
            .WithOne()
            .HasForeignKey(a => a.IdPaciente)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(p => p.Ambitos).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
