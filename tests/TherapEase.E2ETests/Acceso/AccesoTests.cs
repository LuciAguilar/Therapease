using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using Xunit.Sdk;
using Cookie = Microsoft.Playwright.Cookie;

namespace TherapEase.E2ETests;

/// <summary>
/// Comprueba la entrada al sistema, los rechazos y las opciones que ve cada rol.
/// Revisa que una página privada pida sesión y que un destino externo no se acepte.
/// </summary>
public sealed partial class AccesoSesionesYPermisosTests
{


    // ──── ACCESO ───────────────────────────────────────────────────────────


    /// Sin sesión, abrir una página privada debe llevar al acceso y no mostrar sus datos.
    [Theory]
    [InlineData("/")]
    [InlineData("/Usuarios")]
    [InlineData("/Usuarios/Crear")]
    [InlineData("/Usuarios/Detalle/00000000-0000-0000-0000-000000000001")]
    [InlineData("/Cuenta/Contrasena")]
    public Task AnonimoDebeIniciarSesion(string ruta) => Verificar(async () =>
    {

        // Arrange
        var p = await Pagina();

        // Act
        await Abrir(p, ruta);

        // Assert
        Exigir(Ruta(p) == "/Acceso");
    });

    /// Cada rol ve sus opciones; un destino externo no debe sacar a la persona del sistema.
    [Theory]
    [InlineData("Usuaria", false)]
    [InlineData("Superusuario", true)]
    public Task MenuYRetornoRespetanElRol(string rol, bool administra) => Verificar(async () =>
    {

        // Arrange
        var p = await Pagina();
        var cuenta = await entorno.CrearCuentaAsync(rol);

        // Act
        await Entrar(p, cuenta, "https://example.invalid/ajeno");

        // Assert
        Exigir(Ruta(p) == "/" && new Uri(p.Url).GetLeftPart(UriPartial.Authority) == entorno.Url);
        Exigir(await p.GetByRole(AriaRole.Link, new() { Name = "Usuarios", Exact = true }).CountAsync() == (administra ? 1 : 0));
        foreach (var nombre in new[] { "Pacientes", "Agenda" })
            Exigir(await p.GetByRole(AriaRole.Link, new() { Name = nombre, Exact = true }).CountAsync() == 0);
        Exigir(await p.Locator("h1").InnerTextAsync() == "Hola, " + cuenta.Usuario);
    });

    /// Una cuenta inexistente, una contraseña incorrecta y un bloqueo muestran el mismo rechazo sin abrir sesión.
    [Fact]
    public Task AccesoRechazadoYBloqueadoConservanMensajeGenerico() => Verificar(async () =>
    {

        // Arrange
        var p = await Pagina();
        var cuenta = await entorno.CrearCuentaAsync();
        foreach (var intento in Enumerable.Range(0, 7))
        {

            // Act
            await Abrir(p, "/Acceso");
            var r = await Enviar(p, "/Acceso", new() { ["Entrada.NombreUsuario"] = intento == 0 ? "no-existe" : cuenta.Usuario, ["Entrada.Contrasena"] = intento == 6 ? cuenta.Clave : "fallo-ficticio" });
            var cuerpo = WebUtility.HtmlDecode(await r.TextAsync());

            // Assert
            Exigir(r.Status == 200 && cuerpo.Contains("Usuario o contraseña incorrectos.") && !cuerpo.Contains("fallo-ficticio") && !cuerpo.Contains(cuenta.Clave));
        }

        // Act
        await Abrir(p, "/");

        // Assert
        Exigir(Ruta(p) == "/Acceso");
    });

}
