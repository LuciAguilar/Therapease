using Microsoft.EntityFrameworkCore;
using TherapEase.Domain.Auditoria;
using TherapEase.Domain.Citas;
using TherapEase.Domain.Pacientes;

namespace TherapEase.Infrastructure.Persistencia;

public class ContextoDeDatos : DbContext
{
    public ContextoDeDatos(DbContextOptions<ContextoDeDatos> opciones)
        : base(opciones)
    {
    }

    public DbSet<Paciente> Pacientes => Set<Paciente>();

    public DbSet<Cita> Citas => Set<Cita>();

    public DbSet<EventoAuditoria> EventosAuditoria => Set<EventoAuditoria>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ContextoDeDatos).Assembly);
    }
}
