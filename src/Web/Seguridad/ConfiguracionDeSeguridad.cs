using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using TherapEase.Domain.Compartido.Entidades.Enums;
using TherapEase.Domain.Identidad.Constantes;
using TherapEase.Web.Seguridad.Manejadores;
using TherapEase.Web.Seguridad.Requisitos;

namespace TherapEase.Web.Seguridad;

public static class ConfiguracionDeSeguridad
{
    public const string PrefijoDeRutasTecnicas = "/api";

    // Duración por inactividad: propuesta de Miguel, pendiente de decisión de Lucía (07B no fija un valor en el repositorio).
    public static readonly TimeSpan DuracionDeSesion = TimeSpan.FromMinutes(30);

    // Requiere AgregarIdentidad: configura la cookie de sesión que ese registro crea.
    public static IServiceCollection AgregarSeguridadWeb(this IServiceCollection servicios)
    {
        servicios.ConfigureApplicationCookie(opciones =>
        {
            opciones.Cookie.Name = "TherapEase.Sesion";
            opciones.Cookie.HttpOnly = true;
            opciones.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            opciones.Cookie.SameSite = SameSiteMode.Lax;
            opciones.ExpireTimeSpan = DuracionDeSesion;
            opciones.SlidingExpiration = true;
            opciones.LoginPath = "/Acceso";
            opciones.AccessDeniedPath = "/Acceso/Denegado";

            // Se modifican los eventos existentes: reemplazarlos quitaría la validación del sello de seguridad.
            opciones.Events.OnRedirectToLogin = contexto => Responder(contexto, StatusCodes.Status401Unauthorized);
            opciones.Events.OnRedirectToAccessDenied = contexto => Responder(contexto, StatusCodes.Status403Forbidden);
        });

        servicios.AddAntiforgery(opciones =>
        {
            opciones.Cookie.Name = "TherapEase.Antifalsificacion";
            opciones.Cookie.HttpOnly = true;
            opciones.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            opciones.Cookie.SameSite = SameSiteMode.Strict;
        });

        servicios.AddSingleton<IAuthorizationHandler, ManejadorDeRequisitoDePermiso>();
        servicios.AddAuthorizationBuilder()
            .SetFallbackPolicy(PoliticaDeAccesoOperativo())
            .AddPolicy(PoliticasDeAutorizacion.AccesoOperativo, PoliticaDeAccesoOperativo())
            .AddPolicy(PoliticasDeAutorizacion.CambiarContrasenaPropia, politica => politica.RequireAuthenticatedUser())
            .AddPolicy(PoliticasDeAutorizacion.AdministrarUsuarios, politica =>
                politica.RequireAuthenticatedUser().AddRequirements(new RequisitoDePermiso(Permiso.AdministrarUsuarios)))
            .AddPolicy(PoliticasDeAutorizacion.ConsultarAuditoria, politica =>
                politica.RequireAuthenticatedUser().AddRequirements(new RequisitoDePermiso(Permiso.ConsultarAuditoria)));

        return servicios;
    }

    private static AuthorizationPolicy PoliticaDeAccesoOperativo() =>
        new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .RequireAssertion(contexto => contexto.User.FindFirst(Reclamaciones.DebeCambiarContrasena)?.Value == "false")
            .Build();

    // Las rutas técnicas responden con código de estado; las páginas redirigen al acceso.
    private static Task Responder(RedirectContext<CookieAuthenticationOptions> contexto, int codigoDeEstado)
    {
        if (contexto.Request.Path.StartsWithSegments(PrefijoDeRutasTecnicas))
        {
            contexto.Response.StatusCode = codigoDeEstado;
        }
        else
        {
            contexto.Response.Redirect(contexto.RedirectUri);
        }

        return Task.CompletedTask;
    }
}
