using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using Xunit.Sdk;
using Cookie = Microsoft.Playwright.Cookie;

namespace TherapEase.E2ETests;

/// <summary>
/// Comprueba cómo se presenta el acceso en el navegador y qué protecciones devuelve.
/// Revisa las cabeceras, los estilos, la vista móvil y el mensaje de error general.
/// </summary>
public sealed partial class AccesoSesionesYPermisosTests
{


    // ──── SEGURIDAD ───────────────────────────────────────────────────────────


    /// El acceso conserva sus cabeceras y estilos, cabe en una pantalla móvil y la página de error da un aviso seguro.
    [Fact]
    public Task AccesoTieneEstilosCabecerasYVistaMovilYErrorSeguro() => Verificar(async () =>
    {

        // Arrange
        var p = await Pagina();

        // Act
        var r = await Abrir(p, "/Acceso");

        // Assert
        Exigir(r.Status == 200 && r.Headers["cache-control"].Contains("no-store") && r.Headers["x-frame-options"] == "DENY" && r.Headers["x-content-type-options"] == "nosniff" && r.Headers["referrer-policy"] == "no-referrer" && r.Headers["content-security-policy"].Contains("frame-ancestors 'none'"));
        var css = await p.APIRequest.GetAsync(entorno.Url + "/css/acceso.css");
        Exigir(css.Status == 200 && !css.Headers.GetValueOrDefault("cache-control", "").Contains("no-store"));
        Exigir(await p.Locator("main.acceso").EvaluateAsync<string>("e => getComputedStyle(e).display") == "grid");

        // Arrange
        await p.SetViewportSizeAsync(390, 844);

        // Assert
        Exigir(await Boton(p, "Iniciar sesión").IsVisibleAsync());
        Exigir(await p.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= innerWidth + 1"));

        // Act
        var error = await Abrir(p, "/Error");

        // Assert
        Exigir(error.Status == 200 && (await p.Locator("body").InnerTextAsync()).Contains("revisa si se guardó antes de repetirlo"));
    });

}
