using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using Xunit.Sdk;
using Cookie = Microsoft.Playwright.Cookie;

namespace TherapEase.E2ETests;

/// <summary>
/// Comprueba caducidad, renovación y protección de las cookies de sesión.
/// Revisa la salida, el historial del navegador y el cierre de sesiones tras cambios de cuenta.
/// </summary>
public sealed partial class AccesoSesionesYPermisosTests
{


    // ──── SESIONES ───────────────────────────────────────────────────────────


    /// El servidor rechaza una sesión de 31 minutos y renueva las de 16 o 29 minutos al recibir una solicitud.
    [Theory]
    [InlineData(29)]
    [InlineData(31)]
    [InlineData(16)]
    public Task CaducidadYRenovacionSeDecidenEnElServidor(int minutos) => Verificar(async () =>
    {

        // Arrange
        var cuenta = await entorno.CrearCuentaAsync();
        var p = await Pagina();
        var valor = await entorno.CookieAnteriorAsync(cuenta, minutos);
        await p.Context.AddCookiesAsync([new Cookie { Name = "TherapEase.Sesion", Value = valor, Url = entorno.Url, Secure = true, HttpOnly = true, SameSite = SameSiteAttribute.Lax }]);

        // Act
        await Abrir(p, "/");

        // Assert
        if (minutos == 31)
        {
            Exigir(Ruta(p) == "/Acceso");
        }
        else
        {
            Exigir(Ruta(p) == "/");
            Exigir((await p.Context.CookiesAsync()).Any(c => c.Name == "TherapEase.Sesion" && c.Value != valor));
        }
    });

    /// La cookie de sesión conserva sus protecciones y una copia alterada no permite abrir páginas privadas.
    [Fact]
    public Task CookieProtegidaYAlteradaNoAceptada() => Verificar(async () =>
    {

        // Arrange
        var p = await Pagina();

        // Act
        await Entrar(p, await entorno.CrearCuentaAsync());
        var cookie = (await p.Context.CookiesAsync()).Single(c => c.Name == "TherapEase.Sesion");

        // Assert
        Exigir(cookie.Secure && cookie.HttpOnly && cookie.SameSite == SameSiteAttribute.Lax);
        var otra = await Pagina();

        // Arrange
        await otra.Context.AddCookiesAsync([new Cookie { Name = cookie.Name, Value = cookie.Value[..^8] + "alterada", Url = entorno.Url, Secure = true, HttpOnly = true, SameSite = SameSiteAttribute.Lax }]);

        // Act
        await Abrir(otra, "/");

        // Assert
        Exigir(Ruta(otra) == "/Acceso");
    });

    /// Salir cierra también la otra sesión; volver atrás o adelante no recupera páginas privadas ni acceso a auditoría.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task SalirRevocaSesionesYElHistorialNoDevuelveDatos(bool temporal) => Verificar(async () =>
    {

        // Arrange
        var cuenta = await entorno.CrearCuentaAsync(temporal: temporal);
        var p = await Pagina();
        var otra = await Pagina();

        // Act
        await Entrar(p, cuenta);
        await Entrar(otra, cuenta);
        await Abrir(p, "/Cuenta/Contrasena");
        await Boton(p, "Salir").ClickAsync();

        // Assert
        Exigir(Ruta(p) == "/Acceso");

        // Act
        await p.GoBackAsync();

        // Assert
        Exigir(Ruta(p) == "/Acceso");

        // Act
        await p.GoForwardAsync();

        // Assert
        Exigir(Ruta(p) == "/Acceso");

        // Act
        await Abrir(otra, "/");

        // Assert
        Exigir(Ruta(otra) == "/Acceso");

        // Act + Assert
        Exigir((await otra.APIRequest.GetAsync(entorno.Url + "/api/auditoria/eventos")).Status == 401);
    });

    /// Volver al acceso con una sesión abierta ofrece salir; después, las páginas privadas vuelven a pedir acceso.
    [Fact]
    public Task AccesoAutenticadoOfreceSalidaProtegida() => Verificar(async () =>
    {

        // Arrange
        var p = await Pagina();

        // Act
        await Entrar(p, await entorno.CrearCuentaAsync());
        await Abrir(p, "/Acceso");
        await Boton(p, "Cerrar sesión y volver al acceso").ClickAsync();
        await Abrir(p, "/");

        // Assert
        Exigir(Ruta(p) == "/Acceso");
    });

    /// Abrir Salir por dirección no cierra una sesión válida; con sesión vencida permite entrar y continuar a Inicio.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task GetSalirConservaSesionOCompletaRetornoTrasAcceder(bool vencida) => Verificar(async () =>
    {

        // Arrange
        var p = await Pagina();
        var cuenta = await entorno.CrearCuentaAsync();
        if (vencida)
        {
            var cookie = await entorno.CookieAnteriorAsync(cuenta, 31);
            await p.Context.AddCookiesAsync([new Cookie { Name = "TherapEase.Sesion", Value = cookie, Url = entorno.Url, Secure = true, HttpOnly = true }]);

            // Act
            await Abrir(p, "/Cuenta/Salir");

            // Assert
            Exigir(Ruta(p) == "/Acceso" && p.Url.Contains("ReturnUrl="));

            // Act
            await Entrar(p, cuenta, "/Cuenta/Salir");
        }
        else
        {
            await Entrar(p, cuenta);
            await Abrir(p, "/Cuenta/Salir");
        }

        // Assert
        Exigir(Ruta(p) == "/");
    });

    /// Cambiar el rol, desactivar o restablecer la contraseña retira la sesión anterior en su siguiente solicitud.
    [Theory]
    [InlineData("Rol")]
    [InlineData("Desactivar")]
    [InlineData("Restablecer")]
    public Task CambiosAdministrativosRevocanLaSesionAfectada(string operacion) => Verificar(async () =>
    {

        // Arrange
        var cuenta = await entorno.CrearCuentaAsync("Superusuario");
        var afectada = await Pagina();
        var admin = await Pagina();

        // Act
        await Entrar(afectada, cuenta);
        await Entrar(admin, await entorno.CrearCuentaAsync("Superusuario"));
        await Abrir(admin, "/Usuarios/Detalle/" + cuenta.Id);
        var nombre = operacion == "Rol" ? "Cambiar rol" : operacion == "Desactivar" ? "Desactivar cuenta" : "Generar temporal";
        var form = admin.Locator("form").Filter(new() { Has = Boton(admin, nombre) });
        if (operacion == "Rol")

            // Arrange
            await form.Locator("[name='rol']").SelectOptionAsync("Usuaria");
        if (operacion == "Restablecer")

            // Assert
            Exigir((await form.InnerTextAsync()).Contains("número previamente conocido"));

        // Arrange
        await form.Locator("[name='confirmar']").CheckAsync();

        // Act
        await Boton(form.Page, nombre).ClickAsync();
        if (operacion == "Restablecer")
        {
            var clave = await admin.Locator(".secreto code").InnerTextAsync();
            entorno.Privados.Add(clave);
            var p = await Pagina();
            await Entrar(p, cuenta with { Clave = clave });

            // Assert
            Exigir(Ruta(p) == "/Cuenta/Contrasena");

            // Act
            await admin.GetByRole(AriaRole.Link, new() { Name = "Continuar a la cuenta" }).ClickAsync();

            // Assert
            Exigir(!(await admin.ContentAsync()).Contains(clave));
        }

        if (operacion == "Desactivar")
            Exigir((await admin.Locator("body").InnerTextAsync()).Contains("Desactivada"));

        // Act
        await Abrir(afectada, "/");

        // Assert
        Exigir(Ruta(afectada) == "/Acceso");

        // Act + Assert
        Exigir((await afectada.APIRequest.GetAsync(entorno.Url + "/api/auditoria/eventos")).Status == 401);
    });

}
