using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TherapEase.Domain.Auditoria.Entidades;
using TherapEase.Domain.Citas.Entidades;
using TherapEase.Domain.Pacientes.Entidades;
using TherapEase.Infrastructure.Identidad.Entidades;

namespace TherapEase.Infrastructure.Data;

// Las tablas de Identity conservan los nombres del framework (AspNetUsers, etc.), igual que sus columnas.
public class ContextoDeDatos : IdentityDbContext<Usuario, Rol, Guid>, IDataProtectionKeyContext
{
    public ContextoDeDatos(DbContextOptions<ContextoDeDatos> opciones)
        : base(opciones)
    {
    }

    public DbSet<Paciente> Pacientes => Set<Paciente>();

    public DbSet<Cita> Citas => Set<Cita>();

    public DbSet<EventoAuditoria> EventosAuditoria => Set<EventoAuditoria>();

    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ContextoDeDatos).Assembly);
    }
}
