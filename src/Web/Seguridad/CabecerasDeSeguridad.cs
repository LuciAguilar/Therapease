using Microsoft.AspNetCore.Authorization;

namespace TherapEase.Web.Seguridad;

// Cabeceras aprobadas por Lucía el 11-oct-2026 (AGENTS §2.5). La CSP de recursos se completa con las páginas y la duración final de HSTS se decide al desplegar.
public static class CabecerasDeSeguridad
{
    public const string PoliticaDeContenido = "frame-ancestors 'none'";

    public static IApplicationBuilder UsarCabecerasDeSeguridad(this IApplicationBuilder aplicacion)
    {
        return aplicacion.Use((contexto, siguiente) =>
        {
            // Al empezar la respuesta, para que también alcancen a las respuestas de error y a las que otro middleware reescribe.
            contexto.Response.OnStarting(estado =>
            {
                Aplicar((HttpContext)estado);
                return Task.CompletedTask;
            }, contexto);

            return siguiente(contexto);
        });
    }

    public static void Aplicar(HttpContext contexto)
    {
        var cabeceras = contexto.Response.Headers;
        cabeceras.XContentTypeOptions = "nosniff";
        cabeceras.ContentSecurityPolicy = PoliticaDeContenido;
        cabeceras.XFrameOptions = "DENY";
        cabeceras["Referrer-Policy"] = "no-referrer";

        if (EsContenidoPrivado(contexto))
        {
            cabeceras.CacheControl = "no-store";
        }
    }

    // Privado es lo que exige sesión; los archivos estáticos y /salud son anónimos y no reciben no-store.
    private static bool EsContenidoPrivado(HttpContext contexto) =>
        contexto.GetEndpoint() is { } punto && punto.Metadata.GetMetadata<IAllowAnonymous>() is null;
}
