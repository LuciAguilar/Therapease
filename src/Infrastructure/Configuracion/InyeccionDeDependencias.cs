using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TherapEase.Application.Auditoria.Interfaces.Repositorios;
using TherapEase.Application.Citas.Interfaces.Repositorios;
using TherapEase.Application.Compartido.Interfaces.Repositorios;
using TherapEase.Application.Compartido.Interfaces.Servicios;
using TherapEase.Application.Identidad.Interfaces.Repositorios;
using TherapEase.Application.Identidad.Interfaces.Servicios;
using TherapEase.Application.Pacientes.Interfaces.Repositorios;
using TherapEase.Infrastructure.Auditoria.Repositorios;
using TherapEase.Infrastructure.Citas.Repositorios;
using TherapEase.Infrastructure.Compartido.Repositorios;
using TherapEase.Infrastructure.Data;
using TherapEase.Infrastructure.Identidad.Entidades;
using TherapEase.Infrastructure.Identidad.Repositorios;
using TherapEase.Infrastructure.Identidad.Servicios;
using TherapEase.Infrastructure.Pacientes.Repositorios;

namespace TherapEase.Infrastructure.Configuracion;

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

    // Requiere AgregarPersistencia. La configuración de la cookie y las políticas de autorización viven en Web.
    public static IServiceCollection AgregarIdentidad(this IServiceCollection servicios)
    {
        servicios.AddHttpContextAccessor();

        servicios.AddIdentity<Usuario, Rol>(opciones =>
            {
                // Propuesta de Miguel, pendiente de decisión de Lucía (L-CU11-02): solo longitud, sin reglas de composición.
                opciones.Password.RequiredLength = 12;
                opciones.Password.RequireDigit = false;
                opciones.Password.RequireLowercase = false;
                opciones.Password.RequireUppercase = false;
                opciones.Password.RequireNonAlphanumeric = false;
                opciones.Password.RequiredUniqueChars = 1;

                // 07B: cinco fallos bloquean 15 minutos.
                opciones.Lockout.AllowedForNewUsers = true;
                opciones.Lockout.MaxFailedAccessAttempts = 5;
                opciones.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);

                opciones.User.RequireUniqueEmail = false;
            })
            .AddEntityFrameworkStores<ContextoDeDatos>()
            .AddSignInManager<AdministradorDeInicioDeSesion>()
            .AddClaimsPrincipalFactory<FabricaDePrincipal>();

        // Intervalo cero: cada solicitud revalida sello y estado, así salir, desactivar, restablecer o cambiar el rol surten efecto de inmediato.
        servicios.Configure<SecurityStampValidatorOptions>(opciones => opciones.ValidationInterval = TimeSpan.Zero);

        // Las claves que protegen la cookie viven en PostgreSQL, no en disco (AGENTS.md, regla 5).
        servicios.AddDataProtection()
            .SetApplicationName("TherapEase")
            .PersistKeysToDbContext<ContextoDeDatos>();

        servicios.AddScoped<IGestorIdentidad, GestorIdentidad>();
        servicios.AddScoped<IEmisorDeSesion, EmisorDeSesion>();
        servicios.AddScoped<IUsuarioActual, UsuarioActualHttp>();
        return servicios;
    }
}
