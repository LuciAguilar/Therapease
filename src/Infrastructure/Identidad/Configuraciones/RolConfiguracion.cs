using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TherapEase.Domain.Identidad.Constantes;
using TherapEase.Infrastructure.Identidad.Constantes;
using TherapEase.Infrastructure.Identidad.Entidades;

namespace TherapEase.Infrastructure.Identidad.Configuraciones;

public class RolConfiguracion : IEntityTypeConfiguration<Rol>
{
    public void Configure(EntityTypeBuilder<Rol> builder)
    {
        builder.HasData(
            new Rol
            {
                Id = IdentidadConstantes.IdRolUsuaria,
                Name = NombresDeRol.Usuaria,
                NormalizedName = NombresDeRol.Usuaria.ToUpperInvariant(),
                ConcurrencyStamp = "a1b2c3d4-0001-4000-8000-0000000000a1"
            },
            new Rol
            {
                Id = IdentidadConstantes.IdRolSuperusuario,
                Name = NombresDeRol.Superusuario,
                NormalizedName = NombresDeRol.Superusuario.ToUpperInvariant(),
                ConcurrencyStamp = "a1b2c3d4-0001-4000-8000-0000000000a2"
            });
    }
}
