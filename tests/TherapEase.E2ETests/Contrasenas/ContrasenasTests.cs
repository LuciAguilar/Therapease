using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using Xunit.Sdk;
using Cookie = Microsoft.Playwright.Cookie;

namespace TherapEase.E2ETests;

/// <summary>
/// Comprueba el uso de contraseñas temporales y el cambio de contraseña propia.
/// Revisa que los rechazos no revelen secretos, que se cierren otras sesiones y que un bloqueo siga vigente.
/// </summary>
public sealed partial class AccesoSesionesYPermisosTests
{


    // ──── CONTRASENAS ───────────────────────────────────────────────────────────


    /// Una temporal conduce al cambio propio sin pedir la contraseña actual y no permite usar otras funciones.
    [Theory]
    [InlineData("/")]
    [InlineData("/Usuarios")]
    [InlineData("/api/auditoria/eventos")]
    public Task TemporalSoloAccedeACambioPropio(string ruta) => Verificar(async () =>
    {

        // Arrange
        var p = await Pagina();

        // Act
        await Entrar(p, await entorno.CrearCuentaAsync(temporal: true));

        // Assert
        Exigir(Ruta(p) == "/Cuenta/Contrasena" && await p.Locator("[name='Entrada.ContrasenaActual']").CountAsync() == 0);

        // Act + Assert
        if (ruta.StartsWith("/api"))
        {
            Exigir((await p.APIRequest.GetAsync(entorno.Url + ruta)).Status == 403);
        }
        else
        {

            // Act
            await Abrir(p, ruta);

            // Assert
            Exigir(Ruta(p) == "/Cuenta/Contrasena");
        }
    });

    /// Cambiar la contraseña conserva esta sesión, cierra la otra y deja de aceptar la contraseña anterior.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public Task CambioPropioRetiraOtraSesionYRechazaLaAnterior(bool temporal) => Verificar(async () =>
    {

        // Arrange
        var cuenta = await entorno.CrearCuentaAsync(temporal: temporal);
        var p = await Pagina();
        var otra = await Pagina();

        // Act
        await Entrar(p, cuenta);
        await Entrar(otra, cuenta);
        await Cambiar(p, temporal ? null : cuenta.Clave, "sololetrasficticias");

        // Assert
        Exigir(Ruta(p) == (temporal ? "/" : "/Cuenta/Contrasena"));

        // Act
        await Abrir(otra, "/");

        // Assert
        Exigir(Ruta(otra) == "/Acceso");

        // Act
        await Abrir(otra, "/Acceso");
        var r = await Enviar(otra, "/Acceso", new() { ["Entrada.NombreUsuario"] = cuenta.Usuario, ["Entrada.Contrasena"] = cuenta.Clave });

        // Assert
        Exigir(WebUtility.HtmlDecode(await r.TextAsync()).Contains("Usuario o contraseña incorrectos."));
    });

    /// Una contraseña corta, confirmación distinta o campos extra no guardan cambios ni vuelven a mostrar contraseñas.
    [Theory]
    [InlineData("corta", "corta", false)]
    [InlineData("NuevaFicticiaLarga123", "otra", true)]
    [InlineData("NuevaFicticiaLarga123", "NuevaFicticiaLarga123", false)]
    public Task RechazosDeContrasenaNoConservanSecretosNiAceptanCuentaAjena(string nueva, string confirmacion, bool temporal) => Verificar(async () =>
    {

        // Arrange
        var cuenta = await entorno.CrearCuentaAsync(temporal: temporal);
        var p = await Pagina();

        // Act
        await Entrar(p, cuenta);
        await Abrir(p, "/Cuenta/Contrasena");
        var datos = new Dictionary<string, string>
        {
            ["Entrada.ContrasenaNueva"] = nueva,
            ["Entrada.Confirmacion"] = confirmacion,
            ["EsTemporal"] = "true",
            ["idUsuario"] = entorno.Servidor.Registro.ToString(),
            ["Rol"] = "Superusuario"
        };
        if (nueva == "corta")
            datos["Entrada.ContrasenaActual"] = cuenta.Clave;
        var r = await Enviar(p, "/Cuenta/Contrasena", datos);
        var cuerpo = WebUtility.HtmlDecode(await r.TextAsync());
        var esperado = temporal ? "La confirmación no coincide" : nueva == "corta" ? "La contraseña nueva no cumple las reglas de seguridad." : "Completa todos los campos.";

        // Assert
        Exigir(r.Status == 200 && cuerpo.Contains(esperado) && !cuerpo.Contains(cuenta.Clave) && !cuerpo.Contains("value=\"" + nueva + "\""));
    });

    /// Restablecer una contraseña bloqueada entrega otra temporal, pero no permite entrar mientras siga el bloqueo.
    [Fact]
    public Task RestablecerConservaElBloqueoVigente() => Verificar(async () =>
    {

        // Arrange
        var cuenta = await entorno.CrearCuentaAsync();
        var p = await Pagina();
        for (var i = 0; i < 5; i++)
        {

            // Act
            await Abrir(p, "/Acceso");
            await Enviar(p, "/Acceso", new() { ["Entrada.NombreUsuario"] = cuenta.Usuario, ["Entrada.Contrasena"] = "fallo-ficticio" });
        }

        var admin = await Pagina();
        await Entrar(admin, await entorno.CrearCuentaAsync("Superusuario"));
        await Abrir(admin, "/Usuarios/Detalle/" + cuenta.Id);
        var form = admin.Locator("form").Filter(new() { Has = Boton(admin, "Generar temporal") });

        // Arrange
        await form.Locator("[name='confirmar']").CheckAsync();

        // Act
        await Boton(admin, "Generar temporal").ClickAsync();
        var clave = await admin.Locator(".secreto code").InnerTextAsync();
        entorno.Privados.Add(clave);
        await Abrir(p, "/Acceso");
        var r = await Enviar(p, "/Acceso", new() { ["Entrada.NombreUsuario"] = cuenta.Usuario, ["Entrada.Contrasena"] = clave });

        // Assert
        Exigir(WebUtility.HtmlDecode(await r.TextAsync()).Contains("Usuario o contraseña incorrectos."));

        // Act
        await Abrir(p, "/");

        // Assert
        Exigir(Ruta(p) == "/Acceso");
    });

}
