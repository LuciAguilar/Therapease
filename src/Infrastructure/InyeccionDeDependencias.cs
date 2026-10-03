using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TherapEase.Application.Auditoria;
using TherapEase.Application.Citas;
using TherapEase.Application.Compartido;
using TherapEase.Application.Pacientes;
using TherapEase.Infrastructure.Persistencia;
using TherapEase.Infrastructure.Persistencia.Auditoria;
using TherapEase.Infrastructure.Persistencia.Citas;
using TherapEase.Infrastructure.Persistencia.Pacientes;

namespace TherapEase.Infrastructure;

public static class InyeccionDeDependencias
{
    // La conexión es la del usuario de la aplicación, sin permisos de esquema ni de modificar la auditoría.
    public static IServiceCollection AgregarPersistencia(this IServiceCollection servicios, string? cadenaConexion)
    {
        servicios.AddDbContext<ContextoDeDatos>(opciones => opciones.UseNpgsql(cadenaConexion));
        servicios.AddScoped<IUnidadDeTrabajo, UnidadDeTrabajo>();
        servicios.AddScoped<IRepositorioPacientes, RepositorioPacientes>();
        servicios.AddScoped<IRepositorioCitas, RepositorioCitas>();
        servicios.AddScoped<IRepositorioAuditoria, RepositorioAuditoria>();
        return servicios;
    }
}
