using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;

namespace TherapEase.IntegrationTests;

/// <summary>
/// Interrumpe la auditoría de formularios reales para comprobar la respuesta y el guardado.
/// Usa HTTPS y una base desechable; conserva únicamente estados y conteos seguros.
/// </summary>
public sealed class FallosDeFormularioTests(HttpsLocalRevision entorno) : IClassFixture<HttpsLocalRevision>
{


    // ──── FALLOS CONTROLADOS ────────────────────────────────────────────────────────────────


    /// Completa los envíos de rol, desactivación, restablecimiento y acceso con token ausente o ajeno.
    [Fact]
    public async Task LosDemasEnviosRechazanProteccionAusenteODeOtroContexto()
    {
        // Arrange
        using var cliente = entorno.CrearCliente();
        using var otro = entorno.CrearCliente();
        cliente.DefaultRequestHeaders.Add("Cookie", entorno.Cookie);
        otro.DefaultRequestHeaders.Add("Cookie", entorno.Cookie);
        var resultados = new List<object>();
        foreach (var manejador in new[] { "Rol", "Desactivar", "Restablecer", "Acceso" })
        {
            var pagina = manejador == "Acceso" ? "/Acceso" : "/Usuarios/Detalle/" + entorno.Registro;
            _ = await ProteccionAsync(cliente, pagina);
            var ajeno = await ProteccionAsync(otro, pagina);
            var destino = manejador == "Acceso" ? pagina : pagina + "?handler=" + manejador;

            // Act
            using var sinToken = await cliente.PostAsync(new Uri(entorno.Https, destino), new FormUrlEncodedContent(new Dictionary<string, string>()));
            using var conOtro = await cliente.PostAsync(new Uri(entorno.Https, destino), new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ajeno
            }));

            // Assert
            resultados.Add(new { manejador, SinToken = (int)sinToken.StatusCode, TokenAjeno = (int)conOtro.StatusCode });
            Assert.Equal(HttpStatusCode.BadRequest, sinToken.StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, conOtro.StatusCode);
        }
        await GuardarAsync("antifalsificacion-complementaria", resultados);
    }


    /// Si falla registrar la entrada, el formulario responde con el error general y no abre sesión.
    [Fact]
    public async Task FallarElEventoDeEntradaNoAbreSesionNiLoPresentaComoClaveIncorrecta()
    {
        // Arrange
        var cookies = new CookieContainer();
        using var cliente = entorno.CrearCliente(cookies: cookies);
        var proteccion = await ProteccionAsync(cliente, "/Acceso");
        await PostgreSqlRevision.Sql(entorno.Escenario.Migrador,
            """ALTER TABLE "EventoAuditoria" ADD CONSTRAINT "QA_RechazarEntrada" CHECK ("Accion" <> 'InicioSesion') NOT VALID""");
        try
        {
            // Act
            using var respuesta = await cliente.PostAsync(new Uri(entorno.Https, "/Acceso"), new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = proteccion,
                ["Entrada.NombreUsuario"] = "https-ficticio",
                ["Entrada.Contrasena"] = "contrasenaficticiahttpslocal"
            }));
            var texto = await respuesta.Content.ReadAsStringAsync();
            using var privada = await cliente.GetAsync(new Uri(entorno.Https, "/"));

            // Assert
            var sinSesion = cookies.GetCookies(entorno.Https)["TherapEase.Sesion"] is null;
            var generico = !texto.Contains("Usuario o contraseña incorrectos.") && !texto.Contains("QA_RechazarEntrada");
            await GuardarAsync("fallo-evento-entrada", new { Estado = (int)respuesta.StatusCode, SinSesion = sinSesion, ErrorGenerico = generico, Privada = (int)privada.StatusCode });
            Assert.Equal(HttpStatusCode.InternalServerError, respuesta.StatusCode);
            Assert.True(sinSesion && generico);
            Assert.Equal(HttpStatusCode.Redirect, privada.StatusCode);
            Assert.True(entorno.RegistrosContienen("QA_RechazarEntrada"), "No se alcanzó el fallo controlado de auditoría.");
        }
        finally
        {
            await PostgreSqlRevision.Sql(entorno.Escenario.Migrador, """ALTER TABLE "EventoAuditoria" DROP CONSTRAINT "QA_RechazarEntrada";""");
        }
    }

    /// Si falla el evento del alta, no queda cuenta parcial ni un mensaje que anuncie éxito.
    [Fact]
    public async Task FallarLaAuditoriaDelAltaRevierteLaCuentaSinAnunciarExito()
    {
        // Arrange
        using var cliente = entorno.CrearCliente();
        cliente.DefaultRequestHeaders.Add("Cookie", entorno.Cookie);
        var proteccion = await ProteccionAsync(cliente, "/Usuarios/Crear");
        await PostgreSqlRevision.Sql(entorno.Escenario.Migrador,
            """ALTER TABLE "EventoAuditoria" ADD CONSTRAINT "QA_RechazarAlta" CHECK ("Accion" <> 'Alta') NOT VALID""");
        try
        {
            // Act
            using var solicitud = new HttpRequestMessage(HttpMethod.Post, new Uri(entorno.Https, "/Usuarios/Crear"));
            solicitud.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = proteccion,
                ["Entrada.NombreUsuario"] = "qa-alta-revertida",
                ["Entrada.NombreCompleto"] = "Cuenta Ficticia Revertida",
                ["Entrada.Rol"] = "Usuaria"
            });
            using var respuesta = await cliente.SendAsync(solicitud);
            var texto = await respuesta.Content.ReadAsStringAsync();
            await using var contexto = entorno.Escenario.Contexto();
            var cuentas = await contexto.Users.CountAsync(v => v.UserName == "qa-alta-revertida");

            // Assert
            await GuardarAsync("fallo-evento-alta", new { Estado = (int)respuesta.StatusCode, Cuentas = cuentas, SinExito = !texto.Contains("La operación se confirmó.") });
            Assert.Equal(HttpStatusCode.InternalServerError, respuesta.StatusCode);
            Assert.Equal(0, cuentas);
            Assert.DoesNotContain("La operación se confirmó.", texto);
            Assert.True(entorno.RegistrosContienen("QA_RechazarAlta"), "No se alcanzó el fallo controlado de auditoría.");
        }
        finally
        {
            await PostgreSqlRevision.Sql(entorno.Escenario.Migrador, """ALTER TABLE "EventoAuditoria" DROP CONSTRAINT "QA_RechazarAlta";""");
        }
    }

    /// Lee la protección emitida por la página; no la escribe en la evidencia.
    private async Task<string> ProteccionAsync(HttpClient cliente, string ruta)
    {
        using var respuesta = await cliente.GetAsync(new Uri(entorno.Https, ruta));
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var html = await respuesta.Content.ReadAsStringAsync();
        var valor = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
        Assert.True(valor.Length > 0, "La página no emitió el token esperado.");
        var antifalsificacion = respuesta.Headers.TryGetValues("Set-Cookie", out var valores)
            ? valores.FirstOrDefault(v => v.StartsWith("TherapEase.Antifalsificacion=")) : null;
        if (antifalsificacion is not null)
        {
            // El cliente sin almacén automático conserva la cookie emitida además de su sesión.
            var anteriores = cliente.DefaultRequestHeaders.TryGetValues("Cookie", out var cabeceras)
                ? string.Join("; ", cabeceras) : "";
            var conservadas = anteriores.Split(';').Select(v => v.Trim())
                .Where(v => v.Length > 0 && !v.StartsWith("TherapEase.Antifalsificacion="));
            cliente.DefaultRequestHeaders.Remove("Cookie");
            cliente.DefaultRequestHeaders.Add("Cookie",
                string.Join("; ", conservadas.Append(antifalsificacion.Split(';', 2)[0])));
        }
        return WebUtility.HtmlDecode(valor);
    }

    /// Conserva solo el resultado comprobable, sin formularios ni valores de cookies.
    private static Task GuardarAsync(string nombre, object datos)
    {
        var carpeta = Environment.GetEnvironmentVariable("THERAPEASE_EVIDENCIA_QA")
            ?? Path.Combine(ServidorRevision.RaizRepositorio(), "TestResults", "formularios");
        Directory.CreateDirectory(carpeta);
        return File.WriteAllTextAsync(Path.Combine(carpeta, nombre + ".json"), JsonSerializer.Serialize(datos));
    }
}
