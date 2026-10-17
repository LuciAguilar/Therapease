using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TherapEase.Domain.Identidad.Constantes;
using TherapEase.Infrastructure.Identidad.Entidades;

namespace TherapEase.Infrastructure.Identidad.Configuraciones;

public class UsuarioConfiguracion : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.Property(u => u.Activo).HasDefaultValue(true);
        builder.Property(u => u.DebeCambiarContrasena).HasDefaultValue(false);
        builder.Property(u => u.NombreCompleto).HasMaxLength(LimitesDeCuenta.LongitudMaximaNombreCompleto).IsRequired().HasDefaultValue(string.Empty);
    }
}
