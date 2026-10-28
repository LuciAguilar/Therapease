using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TherapEase.Domain.Pacientes.Constantes;
using TherapEase.Domain.Pacientes.Entidades;

namespace TherapEase.Infrastructure.Pacientes.Configuraciones;

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

            // La clave normalizada la calcula Domain (NormalizacionDeNombres). La base no la recalcula: .NET y PostgreSQL pueden diferir en caracteres
            // exóticos según su versión de Unicode y un nombre válido no debe fallar por eso. Sí exige que tenga la forma de una clave ya normalizada.
            tabla.HasCheckConstraint(
                "CK_Paciente_NombreNormalizadoFormato",
                "length(\"NombreNormalizado\") > 0 " +
                "AND \"NombreNormalizado\" !~ '[A-Z\\t\\n\\r\\v\\f\\u0300-\\u036f]' " +
                "AND \"NombreNormalizado\" !~ '^ | $|  '");
            tabla.HasCheckConstraint(
                "CK_Paciente_FechaNacimientoMinima",
                $"\"FechaNacimiento\" IS NULL OR \"FechaNacimiento\" >= DATE '{LimitesDePaciente.AnioMinimoDeNacimiento}-01-01'");

            // Un tutor es todo o nada: nombre, parentesco y, al menos, teléfono o correo.
            tabla.HasCheckConstraint(
                "CK_Paciente_TutorCompleto",
                "(\"TutorNombre\" IS NULL AND \"TutorParentesco\" IS NULL AND \"TutorTelefono\" IS NULL AND \"TutorCorreo\" IS NULL) " +
                "OR (\"TutorNombre\" IS NOT NULL AND \"TutorParentesco\" IS NOT NULL AND (\"TutorTelefono\" IS NOT NULL OR \"TutorCorreo\" IS NOT NULL))");
        });

        builder.HasKey(p => p.IdPaciente);
        builder.Property(p => p.IdPaciente).ValueGeneratedNever();
        builder.Property(p => p.Nombre).IsRequired().HasMaxLength(LimitesDePaciente.LongitudMaximaNombre);
        builder.Property(p => p.NombreNormalizado).IsRequired().HasMaxLength(LimitesDePaciente.LongitudMaximaNombreNormalizado);
        builder.Property(p => p.FechaNacimiento).HasColumnType("date");
        builder.Property(p => p.Telefono).HasMaxLength(LimitesDePaciente.LongitudMaximaTelefono);
        builder.Property(p => p.Correo).HasMaxLength(LimitesDePaciente.LongitudMaximaCorreo);
        builder.Property(p => p.Escuela).HasMaxLength(LimitesDePaciente.LongitudMaximaEscuela);
        builder.Property(p => p.Grupo).HasMaxLength(LimitesDePaciente.LongitudMaximaGrupo);
        builder.Property(p => p.TutorNombre).HasMaxLength(LimitesDePaciente.LongitudMaximaNombreDeTutor);
        builder.Property(p => p.TutorParentesco).HasMaxLength(LimitesDePaciente.LongitudMaximaParentesco);
        builder.Property(p => p.TutorTelefono).HasMaxLength(LimitesDePaciente.LongitudMaximaTelefono);
        builder.Property(p => p.TutorCorreo).HasMaxLength(LimitesDePaciente.LongitudMaximaCorreo);
        builder.Property(p => p.Condicion).HasConversion<string>().HasMaxLength(10);
        builder.Property(p => p.Version).IsConcurrencyToken();

        // Búsqueda de parecidos al registrar y orden estable de los resultados.
        builder.HasIndex(p => p.NombreNormalizado).HasDatabaseName("IX_Paciente_NombreNormalizado");

        builder.HasMany(p => p.Ambitos)
            .WithOne()
            .HasForeignKey(a => a.IdPaciente)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
