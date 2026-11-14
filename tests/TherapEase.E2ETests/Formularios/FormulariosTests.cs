using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using Xunit.Sdk;
using Cookie = Microsoft.Playwright.Cookie;

namespace TherapEase.E2ETests;

/// <summary>
/// Comprueba la protección de los envíos, la búsqueda y los cambios en las cuentas.
/// Revisa la entrega de temporales, la auditoría y qué queda guardado si se pierde una respuesta.
/// </summary>
public sealed partial class AccesoSesionesYPermisosTests
{


    // ──── FORMULARIOS ───────────────────────────────────────────────────────────


    /// Cada formulario rechaza un envío sin protección o con la protección emitida para otro navegador.
    [Theory]
    [InlineData("/Acceso")]
    [InlineData("/Usuarios/Crear")]
    [InlineData("/Cuenta/Contrasena")]
    [InlineData("/Cuenta/Salir")]
    [InlineData("Datos")]
    [InlineData("Rol")]
    [InlineData("Desactivar")]
    [InlineData("Restablecer")]
    public Task CadaPostRechazaTokenAusenteYTokenDeOtraCookie(string ruta) => Verificar(async () =>
    {

        // Arrange
        var cuenta = await entorno.CrearCuentaAsync("Superusuario");
        var p = await Pagina();
        var otra = await Pagina();

        // Act
        await Entrar(p, cuenta);
        await Entrar(otra, cuenta);
        await Abrir(p, "/Usuarios/Crear");
        await Abrir(otra, "/Usuarios/Crear");
        var ajeno = await Token(otra);
        if (!ruta.StartsWith('/'))
            ruta = "/Usuarios/Detalle/" + cuenta.Id + "?handler=" + ruta;

        // Act + Assert
        Exigir((await Enviar(p, ruta, protegido: false)).Status == 400);
        Exigir((await Enviar(p, ruta, new() { ["__RequestVerificationToken"] = ajeno }, false)).Status == 400);
    });

    /// La búsqueda conserva lo escrito; acepta hasta 150 caracteres y muestra validación al superar ese límite.
    [Theory]
    [InlineData(101)]
    [InlineData(150)]
    [InlineData(151)]
    public Task BusquedaConservaEntradaYValidaElLimite(int longitud) => Verificar(async () =>
    {

        // Arrange
        var p = await Pagina();

        // Act
        await Entrar(p, await entorno.CrearCuentaAsync("Superusuario"));
        var texto = new string ('z', longitud);
        var r = await Abrir(p, "/Usuarios?busqueda=" + texto);

        // Assert
        Exigir(r.Status == 200 && await p.Locator("[name='busqueda']").InputValueAsync() == texto);
        Exigir(await p.Locator("[name='busqueda']").GetAttributeAsync("maxlength") == "150");
        Exigir((await p.Locator("body").InnerTextAsync()).Contains(longitud > 150 ? "El texto de búsqueda no es válido." : "No se encontraron cuentas."));
        Exigir(await p.Locator("ul.cuentas li").CountAsync() == 0);
    });

    /// Una cuenta sin nombre completo muestra su usuario; las búsquedas con instrucciones no ejecutan código ni amplían los resultados.
    [Fact]
    public Task CuentaSinNombreCompletoYBusquedaMaliciosaSePresentanComoTexto() => Verificar(async () =>
    {

        // Arrange
        var p = await Pagina();

        // Act
        await Entrar(p, await entorno.CrearCuentaAsync("Superusuario"));
        await Abrir(p, "/Usuarios?busqueda=https-ficticio");

        // Assert
        Exigir(await p.Locator(".identidad strong").InnerTextAsync() == "https-ficticio");
        Exigir(await p.Locator(".identidad .secundario").CountAsync() == 0);

        // Act
        await Abrir(p, "/Usuarios?busqueda=" + Uri.EscapeDataString("<img src=x onerror=window.pruebaEjecutada=1>"));

        // Assert
        Exigir(await p.EvaluateAsync<bool>("() => window.pruebaEjecutada === undefined") && await p.Locator("img[src=x]").CountAsync() == 0);

        // Act
        await Abrir(p, "/Usuarios?busqueda=" + Uri.EscapeDataString("' OR 1=1 --"));

        // Assert
        Exigir(await p.Locator("ul.cuentas li").CountAsync() == 0);
    });

    /// Crear una cuenta entrega la temporal una vez, no la recupera al volver atrás y guarda un solo evento de alta.
    [Fact]
    public Task AltaEntregaTemporalUnaVezYUnSoloEvento() => Verificar(async () =>
    {

        // Arrange
        var p = await Pagina();

        // Act
        await Entrar(p, await entorno.CrearCuentaAsync("Superusuario"));
        await Abrir(p, "/Usuarios/Crear");
        var usuario = "qa-" + Guid.NewGuid().ToString("N");

        // Arrange
        await p.Locator("[name='Entrada.NombreUsuario']").FillAsync(usuario);
        await p.Locator("[name='Entrada.NombreCompleto']").FillAsync("Persona Ficticia");
        var respuestas = 0;
        p.Response += (_, r) =>
        {
            if (r.Request.Method == "POST" && new Uri(r.Url).AbsolutePath == "/Usuarios/Crear")
                Interlocked.Increment(ref respuestas);
        };

        // Act
        var respuesta = await p.RunAndWaitForResponseAsync(() => Boton(p, "Crear usuario").ClickAsync(), r => r.Request.Method == "POST");

        // Assert
        Exigir(respuesta.Status == 200 && respuesta.Headers["cache-control"].Contains("no-store"));
        var clave = await p.Locator(".secreto code").InnerTextAsync();
        entorno.Privados.Add(clave);
        var enlace = p.GetByRole(AriaRole.Link, new() { Name = "Continuar a la cuenta" });
        var id = Guid.Parse((await enlace.GetAttributeAsync("href"))!.Split('/').Last());
        Exigir(!p.Url.Contains(clave) && !(await p.Context.CookiesAsync()).Any(c => c.Value.Contains(clave)));

        // Act
        await enlace.ClickAsync();

        // Assert
        Exigir(!(await p.ContentAsync()).Contains(clave));
        try
        {

            // Act
            await p.GoBackAsync();
        }
        catch (PlaywrightException e)when (e.Message.Contains("ERR_CACHE_MISS"))
        {
        }

        await p.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        // Chrome puede cambiar al documento de error al volver a un POST sin caché.
        // Las aserciones del navegador esperan ese documento sin volver a enviar el formulario.


        // Assert
        await Assertions.Expect(p.Locator(".secreto code")).ToHaveCountAsync(0);
        await Assertions.Expect(p.Locator("body")).Not.ToContainTextAsync(clave);
        Exigir(respuestas == 1);
        var eventos = await Auditoria(p, id);
        Exigir(eventos.EnumerateArray().Count(v => v.GetProperty("accion").GetString() == "Alta") == 1 && !eventos.GetRawText().Contains(clave));
    });

    /// Al perder la respuesta antes o después de guardar, el formulario no se reenvía solo y queda únicamente lo confirmado.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task RespuestaPerdidaAntesODespuesDeGuardarNoReenvia(bool confirmado) => Verificar(async () =>
    {

        // Arrange
        var p = await Pagina();

        // Act
        await Entrar(p, await entorno.CrearCuentaAsync("Superusuario"));
        await Abrir(p, "/Usuarios/Crear");
        var nombre = "qa-" + Guid.NewGuid().ToString("N");

        // Arrange
        await p.Locator("[name='Entrada.NombreUsuario']").FillAsync(nombre);
        await p.Locator("[name='Entrada.NombreCompleto']").FillAsync("Interrupcion Ficticia");
        var envios = 0;
        var guardadoConfirmado = false;
        await p.RouteAsync("**/Usuarios/Crear", async ruta =>
        {
            if (ruta.Request.Method != "POST")
            {

                // Act
                await ruta.ContinueAsync();
                return;
            }

            Interlocked.Increment(ref envios);
            if (confirmado)
            {
                var respuesta = await ruta.FetchAsync(new() { MaxRetries = 0 });
                guardadoConfirmado = respuesta.Status == 200;
                await respuesta.DisposeAsync();
            }

            await ruta.AbortAsync("failed");
        });
        try
        {
            await Boton(p, "Crear usuario").ClickAsync();
        }
        catch (PlaywrightException e)when (e.Message.Contains("net::ERR_FAILED"))
        {
        }

        await p.UnrouteAsync("**/Usuarios/Crear");
        await Abrir(p, "/Usuarios?busqueda=" + nombre);

        // Assert
        Exigir(await p.Locator("ul.cuentas li").CountAsync() == (confirmado ? 1 : 0) && envios == 1 && guardadoConfirmado == confirmado);
        if (confirmado)
        {
            var id = Guid.Parse((await p.GetByRole(AriaRole.Link, new() { Name = "Ver cuenta" }).GetAttributeAsync("href"))!.Split('/').Last());

            // Act + Assert
            Exigir((await Auditoria(p, id)).EnumerateArray().Count(v => v.GetProperty("accion").GetString() == "Alta") == 1);
        }
    });

    /// Editar nombre y correo debe guardar ambos; el correo no aparece en la lista y la auditoría no incluye sus valores.
    [Fact]
    public Task DatosEditadosNoSeExponenEnListaNiAuditoria() => Verificar(async () =>
    {

        // Arrange
        var cuenta = await entorno.CrearCuentaAsync();
        var p = await Pagina();

        // Act
        await Entrar(p, await entorno.CrearCuentaAsync("Superusuario"));
        await Abrir(p, "/Usuarios/Detalle/" + cuenta.Id);

        // Arrange
        await p.Locator("[name='nombreCompleto']").FillAsync("Nombre Ficticio Editado");
        await p.Locator("[name='correo']").FillAsync("ficticia@example.invalid");

        // Act
        await Boton(p, "Guardar datos").ClickAsync();

        // Assert
        Exigir(await p.Locator("[name='nombreCompleto']").InputValueAsync() == "Nombre Ficticio Editado");
        Exigir(await p.Locator("[name='correo']").InputValueAsync() == "ficticia@example.invalid");

        // Act
        await Abrir(p, "/Usuarios?busqueda=" + cuenta.Usuario);

        // Assert
        Exigir(!(await p.ContentAsync()).Contains("ficticia@example.invalid"));
        var texto = (await Auditoria(p, cuenta.Id)).GetRawText();
        Exigir(!texto.Contains("ficticia@example.invalid") && !texto.Contains("Nombre Ficticio Editado"));
    });

    /// Los datos incompletos o un rol inexistente deben rechazarse sin crear una cuenta ni entregar una temporal.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task AltaIncompletaORolInvalidoNoCreanCuenta(bool rolInvalido) => Verificar(async () =>
    {

        // Arrange
        var p = await Pagina();

        // Act
        await Entrar(p, await entorno.CrearCuentaAsync("Superusuario"));
        await Abrir(p, "/Usuarios/Crear");
        var nombre = "qa-" + Guid.NewGuid().ToString("N");
        var r = await Enviar(p, "/Usuarios/Crear", rolInvalido ? new() { ["Entrada.NombreUsuario"] = nombre, ["Entrada.NombreCompleto"] = "Persona Ficticia", ["Entrada.Rol"] = "AdministradorInexistente" } : []);

        // Assert
        Exigir(r.Status == 200 && !(await r.TextAsync()).Contains("class=\"secreto\""));
        await using var db = entorno.Servidor.Escenario.Contexto();
        Exigir(!await db.Users.AnyAsync(v => v.UserName == nombre));
    });

}
