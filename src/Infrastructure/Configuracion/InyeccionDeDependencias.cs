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
using TherapEase.Infrastructure.Compartido.Servicios;
using TherapEase.Infrastructure.Data;
using TherapEase.Infrastructure.Identidad.Entidades;
using TherapEase.Infrastructure.Identidad.Repositorios;
using TherapEase.Infrastructure.Identidad.Servicios;
using TherapEase.Infrastructure.Pacientes.Repositorios;

namespace TherapEase.Infrastructure.Configuracion;

public static class InyeccionDeDependencias
{
    // Mensaje fijo de arranque cuando falta la conexión. Nombra la clave de configuración y la variable de entorno, nunca una cadena de conexión ni su valor.
    public const string MensajeSinCadenaDeConexion =
        "Falta la cadena de conexión «ConnectionStrings:TherapEase» (variable de entorno ConnectionStrings__TherapEase). La aplicación no se inicia sin ella.";

    // La conexión es la del usuario de la aplicación, sin permisos de esquema ni de modificar la auditoría.
    // Sin cadena (nula, vacía o solo espacios) se detiene antes de registrar nada: es el único lugar donde se decide, para la aplicación y para los comandos del operador.
    public static IServiceCollection AgregarPersistencia(this IServiceCollection servicios, string? cadenaConexion)
    {
        if (string.IsNullOrWhiteSpace(cadenaConexion))
        {
            throw new InvalidOperationException(MensajeSinCadenaDeConexion);
        }

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
                // Mínimo de 12 caracteres, sin reglas de composición: decisión de Lucía del 04-oct-2026 (AGENTS §2.2, L-CU11-02).
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
        servicios.AddSingleton<IRegistroTecnico, RegistroTecnico>();
        return servicios;
    }
}
