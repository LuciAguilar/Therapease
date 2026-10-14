using System.Text;
using TherapEase.Web.Seguridad;

namespace TherapEase.Web.Configuracion;

// Respuesta fija ante una excepción no controlada: no pasa por Identity ni la base de datos y no incluye datos de la excepción, de la solicitud ni del usuario.
public static class ManejadorDeErroresInesperados
{
    public const string MensajeGenerico = "Ocurrió un error. Si estabas guardando algo, revisa si se guardó antes de repetirlo.";

    public const string CuerpoJson = "{\"error\":\"" + MensajeGenerico + "\"}";

    public const string CuerpoHtml =
        "<!DOCTYPE html><html lang=\"es\"><head><meta charset=\"utf-8\" /><title>Error - TherapEase</title></head>" +
        "<body><h1>Ocurrió un error</h1><p>" + MensajeGenerico + "</p></body></html>";

    public static IApplicationBuilder UsarRespuestaDeErrorGenerica(this IApplicationBuilder aplicacion) =>
        aplicacion.UseExceptionHandler(rama => rama.Run(ResponderAsync));

    public static async Task ResponderAsync(HttpContext contexto)
    {
        var respuesta = contexto.Response;
        respuesta.StatusCode = StatusCodes.Status500InternalServerError;
        CabecerasDeSeguridad.Aplicar(contexto);
        respuesta.Headers.CacheControl = "no-store";

        var esRutaTecnica = contexto.Request.Path.StartsWithSegments(ConfiguracionDeSeguridad.PrefijoDeRutasTecnicas);
        respuesta.ContentType = esRutaTecnica ? "application/json; charset=utf-8" : "text/html; charset=utf-8";
        await respuesta.WriteAsync(esRutaTecnica ? CuerpoJson : CuerpoHtml, Encoding.UTF8);
    }
}
