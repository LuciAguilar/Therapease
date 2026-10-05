using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TherapEase.Domain.Pacientes.Entidades;

namespace TherapEase.Infrastructure.Pacientes.Configuraciones;

public class PacienteAmbitoConfiguracion : IEntityTypeConfiguration<PacienteAmbito>
{
    public void Configure(EntityTypeBuilder<PacienteAmbito> builder)
    {
        builder.ToTable("PacienteAmbito", tabla =>
            tabla.HasCheckConstraint("CK_PacienteAmbito_Ambito", "\"Ambito\" IN ('Escolar', 'Independiente')"));

        builder.HasKey(a => new { a.IdPaciente, a.Ambito });
        builder.Property(a => a.Ambito).HasConversion<string>().HasMaxLength(20);
    }
}
