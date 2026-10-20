using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using TherapEase.Application.Identidad.Interfaces.Servicios;
using static TherapEase.IntegrationTests.ServidorIdentidadRevision;

namespace TherapEase.IntegrationTests;

/// <summary>
/// Comprueba renovación y revocación de cookies enviadas al servidor HTTPS real.
/// Las cookies y contraseñas ficticias quedan en memoria; no usa páginas ni navegador.
/// </summary>
public class SesionesHttpsTests(SesionesHttpsRevision revision) : IClassFixture<SesionesHttpsRevision>
{
    /// Servidor real con su propia carpeta de evidencia, separado de otras pruebas HTTPS.
    private HttpsLocalRevision Entorno => revision.Servidor;


    // ──── RENOVACIÓN ───────────────────────────────────────────────────────────────────────


    /// El uso por HTTPS renueva treinta minutos, conserva la identidad y permite seguir tras vencer la cookie original.
    [Fact]
    public async Task Renovacion_Https_Extiende_Plazo_Y_Conserva_Identidad()
    {
        // Arrange
        using var proveedor = Entorno.Escenario.Servicios();
        using var lectura = new SolicitudIdentidad(proveedor);
        var formato = lectura.Obtener<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(IdentityConstants.ApplicationScheme).TicketDataFormat;
        var original = formato.Unprotect(Entorno.Cookie.Split('=', 2)[1]);
        Assert.NotNull(original);

        // Act
        using var respuesta = await ConsultarAsync(Entorno.Cookie);
        var renovada = LeerCookie(respuesta);
        var nueva = formato.Unprotect(renovada.Split('=', 2)[1]);

        // Assert
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.NotNull(nueva);
        Assert.Equal(original.Principal.FindFirstValue(ClaimTypes.NameIdentifier),
            nueva.Principal.FindFirstValue(ClaimTypes.NameIdentifier));
        Assert.True(nueva.Properties.IssuedUtc > original.Properties.IssuedUtc);
        Assert.True(nueva.Properties.ExpiresUtc > original.Properties.ExpiresUtc);
        Assert.Equal(TimeSpan.FromMinutes(30), nueva.Properties.ExpiresUtc - nueva.Properties.IssuedUtc);
        Assert.False(nueva.Properties.IsPersistent);
        var cabecera = CabeceraDeSesion(respuesta);
        Assert.False(cabecera.Contains("expires=", StringComparison.OrdinalIgnoreCase),
            "La cookie de sesión incluye una fecha de conservación.");
        Assert.False(cabecera.Contains("max-age=", StringComparison.OrdinalIgnoreCase),
            "La cookie de sesión incluye un plazo de conservación.");

        // Arrange: se comprueban las dos cookies después del vencimiento original, sin esperar esos minutos.
        var reloj = new RelojRevision();
        reloj.Avanzar(original.Properties.ExpiresUtc!.Value.AddSeconds(1) - reloj.GetUtcNow());
        using var posterior = Entorno.Escenario.Servicios(reloj.Configurar);

        // Act + Assert
        using (var solicitud = new SolicitudIdentidad(posterior, Entorno.Cookie))
            Assert.False(await solicitud.AutenticarAsync());
        using (var solicitud = new SolicitudIdentidad(posterior, renovada))
            Assert.True(await solicitud.AutenticarAsync());
    }


    // ──── REVOCACIÓN TRAS RENOVAR ──────────────────────────────────────────────────────────


    /// Salir, restablecer o cambiar contraseña propia invalida también la cookie que HTTPS acababa de renovar.
    /// El cambio propio conserva solo la nueva sesión; no se cierran las de otra persona.
    [Theory]
    [InlineData("salir")]
    [InlineData("restablecer")]
    [InlineData("cambiar-propia")]
    public async Task Operacion_De_Seguridad_Revoca_Cookie_Original_Y_Renovada(string operacion)
    {
        // Arrange
        var reloj = new RelojRevision();
        reloj.Avanzar(TimeSpan.FromMinutes(-16));
        using var proveedor = Entorno.Escenario.Servicios(reloj.Configurar);
        var nombre = "sesion-" + operacion;
        var objetivo = await CrearAsync(proveedor, nombre, "Superusuario");
        var original = await EntrarAsync(proveedor, nombre);
        string renovada;
        using (var respuesta = await ConsultarAsync(original))
        {
            Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
            renovada = LeerCookie(respuesta);
        }
        string? actual = null;
        using var proveedorActual = Entorno.Escenario.Servicios();

        // Act + Assert: cada operación debe confirmar el resultado antes de comprobar las cookies.
        using (var solicitud = new SolicitudIdentidad(proveedorActual,
            operacion == "restablecer" ? Entorno.Cookie : renovada))
        {
            Assert.True(await solicitud.AutenticarAsync());
            switch (operacion)
            {
                case "salir":
                    await solicitud.Obtener<IServicioAcceso>().CerrarSesionAsync();
                    break;
                case "restablecer":
                    Assert.True((await solicitud.Obtener<IServicioUsuarios>().RestablecerContrasenaAsync(objetivo)).Exito);
                    break;
                default:
                    Assert.True((await solicitud.Obtener<IServicioContrasena>()
                        .CambiarContrasenaPropiaAsync(Contrasena, NuevaContrasena)).Exito);
                    actual = solicitud.CookieEmitida();
                    break;
            }
        }

        // Assert
        foreach (var cookie in new[] { original, renovada })
        {
            using var respuesta = await ConsultarAsync(cookie);
            Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
            Assert.Empty(await respuesta.Content.ReadAsStringAsync());
        }
        if (actual is not null)
        {
            using var respuesta = await ConsultarAsync(actual);
            Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        }
        using var ajena = await ConsultarAsync(Entorno.Cookie);
        Assert.Equal(HttpStatusCode.OK, ajena.StatusCode);
    }


    // ──── SOLICITUDES Y COOKIES EN MEMORIA ─────────────────────────────────────────────────


    /// Envía la cookie a una consulta protegida existente, sin añadir rutas de prueba al sistema.
    private async Task<HttpResponseMessage> ConsultarAsync(string cookie)
    {
        using var solicitud = new HttpRequestMessage(HttpMethod.Get, new Uri(Entorno.Https, Entorno.Consulta));
        solicitud.Headers.Add("Cookie", cookie);
        return await Entorno.Cliente.SendAsync(solicitud);
    }

    /// Localiza solo la cookie de sesión emitida, sin escribir su valor en la evidencia.
    private static string CabeceraDeSesion(HttpResponseMessage respuesta)
    {
        Assert.True(respuesta.Headers.TryGetValues("Set-Cookie", out var cabeceras));
        var sesiones = cabeceras!.Where(c => c.StartsWith("TherapEase.Sesion=", StringComparison.Ordinal)).ToArray();
        Assert.True(sesiones.Length == 1, "La respuesta no contiene una única cookie de sesión.");
        return sesiones[0];
    }

    /// Devuelve nombre y valor para la siguiente solicitud, sin los atributos de la cabecera.
    private static string LeerCookie(HttpResponseMessage respuesta) => CabeceraDeSesion(respuesta).Split(';', 2)[0];
}
