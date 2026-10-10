using System.Net;
using System.Security.Authentication;
using System.Text.Json;

namespace TherapEase.IntegrationTests;

/// <summary>
/// Comprueba HTTPS, certificados, redirecciones y cookies del servidor real con datos ficticios.
/// Registra las cabeceras actuales; su política final y las páginas requieren validación posterior.
/// </summary>
public sealed class HttpsLocalTests(HttpsLocalRevision entorno) : IClassFixture<HttpsLocalRevision>
{


    // ──── CONEXIÓN CIFRADA Y CERTIFICADO ────────────────────────────────────────────────────


    /// La conexión usa TLS 1.2 o 1.3 y devuelve el evento esperado desde la base de datos desechable.
    [Fact]
    public async Task Https_Valida_Certificado_Y_Consulta_Real()
    {
        // Arrange
        var esperado = entorno.Evento;

        // Act
        var protocolo = await entorno.ProtocoloAsync();
        using var respuesta = await entorno.ConsultarAsync();
        using var cuerpo = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());

        // Assert
        Assert.Contains(protocolo, new[] { SslProtocols.Tls12, SslProtocols.Tls13 });
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal(esperado, Assert.Single(cuerpo.RootElement.EnumerateArray()).GetProperty("idEvento").GetGuid());
        await entorno.GuardarCabecerasAsync(respuesta, "api-https");
    }

    /// El cliente rechaza el certificado cuando no confía en la autoridad ficticia.
    [Fact]
    public async Task Https_Rechaza_Autoridad_No_Confiable()
    {
        // Arrange
        using var cliente = entorno.CrearCliente(confiar: false);

        // Act + Assert
        var error = await Assert.ThrowsAsync<HttpRequestException>(() => cliente.GetAsync(new Uri(entorno.Https, "/salud")));
        Assert.True(EsErrorDeCertificado(error), "El rechazo debe proceder de la validación TLS.");
    }

    /// Aunque la autoridad sea aceptada, un nombre distinto del certificado impide la conexión.
    [Fact]
    public async Task Https_Rechaza_Nombre_Distinto_Del_Certificado()
    {
        // Arrange
        using var cliente = entorno.CrearCliente();
        var direccion = new UriBuilder(entorno.Https) { Host = "otro.local.test", Path = "/salud" }.Uri;

        // Act + Assert
        var error = await Assert.ThrowsAsync<HttpRequestException>(() => cliente.GetAsync(direccion));
        Assert.True(EsErrorDeCertificado(error), "El rechazo debe proceder de la validación TLS.");
    }

    /// Identifica el rechazo de TLS sin mostrar contraseñas, conexiones o cookies en el informe.
    private static bool EsErrorDeCertificado(Exception error) =>
        error is AuthenticationException || error.InnerException is not null && EsErrorDeCertificado(error.InnerException);


    // ──── REDIRECCIÓN Y AUTORIZACIÓN ────────────────────────────────────────────────────────


    /// HTTP redirige a HTTPS conservando ruta y consulta, antes de entregar contenido o emitir una sesión.
    [Theory]
    [InlineData("/salud")]
    [InlineData("/api/auditoria/eventos?desde=2026-10-10T00%3A00%3A00Z&hasta=2026-10-11T00%3A00%3A00Z&limite=1")]
    public async Task Http_Redirige_A_Https_Sin_Emitir_Cookie(string ruta)
    {
        // Arrange
        var direccion = new Uri(entorno.Http, ruta);
        var esperado = new Uri(entorno.Https, ruta);

        // Act
        using var respuesta = await entorno.Cliente.GetAsync(direccion);

        // Assert
        Assert.Equal(HttpStatusCode.TemporaryRedirect, respuesta.StatusCode);
        Assert.Equal(esperado, respuesta.Headers.Location);
        Assert.False(respuesta.Headers.Contains("Set-Cookie"));
        Assert.Empty(await respuesta.Content.ReadAsStringAsync());
    }

    /// HTTPS cifra la conexión, pero la consulta de auditoría sigue exigiendo una sesión.
    [Fact]
    public async Task Https_Sin_Sesion_No_Entrega_Auditoria()
    {
        // Arrange
        var direccion = new Uri(entorno.Https, entorno.Consulta);

        // Act
        using var respuesta = await entorno.Cliente.GetAsync(direccion);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        Assert.Null(respuesta.Headers.Location);
        Assert.DoesNotContain(entorno.Evento.ToString(), await respuesta.Content.ReadAsStringAsync());
    }


    // ──── COOKIE REAL DEL SERVIDOR ──────────────────────────────────────────────────────────


    /// Al renovar una sesión por HTTPS, la cookie real exige conexión segura y evita lectura desde JavaScript.
    [Fact]
    public async Task Cookie_Renovada_Por_Https_Es_Secure_HttpOnly_Y_Lax()
    {
        // Arrange
        var cookies = new CookieContainer();

        // Act
        using var respuesta = await entorno.ConsultarAsync();
        var cabecera = CookieDeSesion(respuesta);
        cookies.SetCookies(entorno.Https, cabecera);
        var cookie = cookies.GetCookies(entorno.Https)["TherapEase.Sesion"];

        // Assert
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.NotNull(cookie);
        Assert.True(cookie.Secure);
        Assert.True(cookie.HttpOnly);
        Assert.Equal("/", cookie.Path);
        Assert.True(cabecera.Contains("samesite=lax", StringComparison.OrdinalIgnoreCase), "La cookie debe indicar SameSite=Lax.");
        Assert.False(string.Equals(entorno.Cookie, cabecera.Split(';', 2)[0], StringComparison.Ordinal), "La respuesta debe renovar la sesión anterior.");
    }

    /// El cliente HTTP de .NET envía la cookie Secure por HTTPS y la excluye de una solicitud HTTP.
    [Fact]
    public async Task Cookie_Secure_No_Se_Selecciona_Para_Http()
    {
        // Arrange
        var cookies = new CookieContainer();
        using var renovacion = await entorno.ConsultarAsync();
        cookies.SetCookies(entorno.Https, CookieDeSesion(renovacion));
        using var cliente = entorno.CrearCliente(cookies: cookies);

        // Act
        using var segura = await cliente.GetAsync(new Uri(entorno.Https, entorno.Consulta));
        using var sinCifrar = await cliente.GetAsync(new Uri(entorno.Http, entorno.Consulta));

        // Assert
        Assert.NotEmpty(cookies.GetCookieHeader(entorno.Https));
        Assert.True(string.IsNullOrEmpty(cookies.GetCookieHeader(entorno.Http)), "El cliente no debe seleccionar una cookie Secure para HTTP.");
        Assert.Equal(HttpStatusCode.OK, segura.StatusCode);
        Assert.Equal(HttpStatusCode.TemporaryRedirect, sinCifrar.StatusCode);
    }

    /// Obtiene la renovación de sesión sin escribir su valor privado en archivos de evidencia.
    private static string CookieDeSesion(HttpResponseMessage respuesta)
    {
        Assert.True(respuesta.Headers.TryGetValues("Set-Cookie", out var cabeceras), "El servidor debe emitir la renovación de sesión.");
        var sesiones = cabeceras!.Where(c => c.StartsWith("TherapEase.Sesion=", StringComparison.Ordinal)).ToArray();
        Assert.True(sesiones.Length == 1, "Debe existir exactamente una renovación de la cookie de sesión.");
        return sesiones[0];
    }


    // ──── HSTS Y REGISTROS ──────────────────────────────────────────────────────────────────


    /// En modo Production local, HTTPS indica que el navegador debe preferir conexiones seguras durante 30 días.
    [Fact]
    public async Task Hsts_Se_Emite_En_Https_Para_Nombre_De_Prueba()
    {
        // Arrange
        var direccion = new Uri(entorno.Https, "/salud");

        // Act
        using var respuesta = await entorno.Cliente.GetAsync(direccion);

        // Assert
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal("max-age=2592000", Assert.Single(respuesta.Headers.GetValues("Strict-Transport-Security")));
        await entorno.GuardarCabecerasAsync(respuesta, "salud-https");
    }

    /// HSTS no se emite en HTTP ni para localhost, que ASP.NET Core excluye por defecto.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Hsts_No_Se_Emite_En_Http_O_Localhost(bool localhost)
    {
        // Arrange
        var direccion = localhost
            ? new UriBuilder(entorno.Https) { Host = "localhost", Path = "/salud" }.Uri
            : new Uri(entorno.Http, "/salud");

        // Act
        using var respuesta = await entorno.Cliente.GetAsync(direccion);

        // Assert
        Assert.Equal(localhost ? HttpStatusCode.OK : HttpStatusCode.TemporaryRedirect, respuesta.StatusCode);
        Assert.False(respuesta.Headers.Contains("Strict-Transport-Security"));
    }

    /// Los mensajes capturados durante este arranque y consulta no revelan los secretos ficticios del entorno HTTPS.
    [Fact]
    public async Task Registros_De_Https_No_Muestran_Secretos_Del_Entorno()
    {
        // Arrange
        using var respuesta = await entorno.ConsultarAsync();

        // Act
        await Task.Delay(100);
        var sinSecretos = entorno.RegistrosSinSecretos();

        // Assert
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.True(sinSecretos, "Los mensajes deben existir y omitir los secretos ficticios del entorno HTTPS.");
    }
}
