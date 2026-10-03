using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TherapEase.Infrastructure.Identidad.Entidades;

namespace TherapEase.Infrastructure.Identidad.Configuraciones;

public class UsuarioConfiguracion : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.Property(u => u.Activo).HasDefaultValue(true);
        builder.Property(u => u.DebeCambiarContrasena).HasDefaultValue(false);
    }
}
