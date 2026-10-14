using System.Net;
using System.Text.Json;

namespace TherapEase.IntegrationTests;

/// <summary>
/// Comprueba cabeceras y errores seguros del servidor HTTPS real.
/// Usa sesiones ficticias y PostgreSQL desechable, sin agregar rutas a la aplicación.
/// </summary>
public sealed class CabecerasSeguridadTests(HttpsLocalRevision entorno) : IClassFixture<HttpsLocalRevision>
{


    // ──── CABECERAS Y CACHÉ ───────────────────────────────────────────────────────────────


    /// Las respuestas llevan las cabeceras acordadas; el contenido privado no se guarda en caché.
    [Theory]
    [InlineData("/salud", false, 200, true)]
    [InlineData("/css/site.css", false, 200, false)]
    [InlineData("auditoria", false, 401, true)]
    [InlineData("auditoria", true, 200, true)]
    [InlineData("/", true, 200, true)]
    [InlineData("/Error", false, 200, true)]
    public async Task Respuestas_Conservan_Cabeceras_Y_Cache_Acordada(
        string ruta, bool conSesion, int estado, bool noStore)
    {
        // Arrange
        using var solicitud = CrearSolicitud(ruta, conSesion);

        // Act
        using var respuesta = await entorno.Cliente.SendAsync(solicitud);

        // Assert
        Assert.Equal(estado, (int)respuesta.StatusCode);
        ComprobarCabeceras(respuesta);
        Assert.Equal(noStore, respuesta.Headers.CacheControl?.NoStore == true);
    }


    // ──── FALLOS REALES DE IDENTITY ───────────────────────────────────────────────────────


    /// Si Identity no puede leer la base, API y página devuelven un 500 fijo y piden revisar antes de repetir.
    [Theory]
    [InlineData("auditoria", "application/json")]
    [InlineData("/", "text/html")]
    public async Task Fallo_De_Identity_Conserva_Cabeceras_Y_Mensaje_Seguro(string ruta, string formato)
    {
        // Arrange
        const string esperado = "Ocurrió un error. Si estabas guardando algo, revisa si se guardó antes de repetirlo.";
        await PostgreSqlRevision.Sql(entorno.Escenario.Migrador,
            "REVOKE SELECT ON \"AspNetUsers\" FROM therapease_app");
        try
        {
            using var solicitud = CrearSolicitud(ruta, true);

            // Act
            using var respuesta = await entorno.Cliente.SendAsync(solicitud);
            var cuerpo = await respuesta.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.InternalServerError, respuesta.StatusCode);
            Assert.Equal(formato, respuesta.Content.Headers.ContentType?.MediaType);
            ComprobarCabeceras(respuesta);
            Assert.True(respuesta.Headers.CacheControl?.NoStore);
            if (formato == "application/json")
            {
                using var documento = JsonDocument.Parse(cuerpo);
                Assert.Equal(esperado, documento.RootElement.GetProperty("error").GetString());
                Assert.Single(documento.RootElement.EnumerateObject());
            }
            else
            {
                Assert.Equal("<!DOCTYPE html><html lang=\"es\"><head><meta charset=\"utf-8\" /><title>Error - TherapEase</title></head>"
                    + "<body><h1>Ocurrió un error</h1><p>" + esperado + "</p></body></html>", cuerpo);
            }
            Assert.DoesNotContain("Intenta de nuevo", cuerpo, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(entorno.Cookie, cuerpo);
            Assert.DoesNotContain(entorno.Escenario.App, cuerpo);
        }
        finally
        {
            await PostgreSqlRevision.Sql(entorno.Escenario.Migrador,
                "GRANT SELECT ON \"AspNetUsers\" TO therapease_app");
        }

        // Act: devolver el permiso debe recuperar la consulta sin reiniciar el servidor.
        using var recuperada = await entorno.ConsultarAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, recuperada.StatusCode);
    }

    /// La página de error accesible sin sesión también pide revisar el resultado antes de repetir.
    [Fact]
    public async Task Pagina_De_Error_No_Invita_A_Repetir_A_Ciegas()
    {
        // Arrange
        using var solicitud = CrearSolicitud("/Error", false);

        // Act
        using var respuesta = await entorno.Cliente.SendAsync(solicitud);
        var cuerpo = WebUtility.HtmlDecode(await respuesta.Content.ReadAsStringAsync());

        // Assert
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains("revisa si se guardó antes de repetirlo", cuerpo);
        Assert.DoesNotContain("Intenta de nuevo", cuerpo, StringComparison.OrdinalIgnoreCase);
    }


    // ──── SOLICITUDES Y COMPROBACIONES ─────────────────────────────────────────────────────


    /// Envía la cookie solo cuando el caso requiere una sesión, sin seguir redirecciones.
    private HttpRequestMessage CrearSolicitud(string ruta, bool conSesion)
    {
        var solicitud = new HttpRequestMessage(HttpMethod.Get,
            new Uri(entorno.Https, ruta == "auditoria" ? entorno.Consulta : ruta));
        if (conSesion) solicitud.Headers.Add("Cookie", entorno.Cookie);
        return solicitud;
    }

    /// Comprueba valores acordados sin depender de las constantes del código que se revisa.
    private static void ComprobarCabeceras(HttpResponseMessage respuesta)
    {
        Assert.Equal("nosniff", Assert.Single(respuesta.Headers.GetValues("X-Content-Type-Options")));
        Assert.Equal("frame-ancestors 'none'", Assert.Single(respuesta.Headers.GetValues("Content-Security-Policy")));
        Assert.Equal("DENY", Assert.Single(respuesta.Headers.GetValues("X-Frame-Options")));
        Assert.Equal("no-referrer", Assert.Single(respuesta.Headers.GetValues("Referrer-Policy")));
    }
}
