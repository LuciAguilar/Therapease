using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using Xunit.Sdk;
using Cookie = Microsoft.Playwright.Cookie;

namespace TherapEase.E2ETests;

/// <summary>
/// Reúne los pasos comunes para abrir páginas, entrar, enviar formularios y consultar auditoría.
/// Comprueba los resultados y oculta los valores de formularios si una prueba falla.
/// </summary>
public sealed partial class AccesoSesionesYPermisosTests(NavegadorRevision entorno) : IClassFixture<NavegadorRevision>
{


    // ──── CLIENTES Y COMPROBACIONES SEGURAS ────────────────────────────────────────────────


    private readonly List<IBrowserContext> _contextos = [];
    private string _paso = "preparación";

    /// Busca un botón por su nombre visible para usar el mismo control que la persona.
    private static ILocator Boton(IPage p, string nombre) => p.GetByRole(AriaRole.Button, new() { Name = nombre, Exact = true });

    /// Lee solo la ruta actual para comprobar el destino sin guardar datos de la dirección.
    private static string Ruta(IPage p) => new Uri(p.Url).AbsolutePath;

    /// Compara el resultado sin imprimir valores del formulario cuando la prueba falla.
    private static void Exigir(bool valor) => Assert.True(valor, "El resultado no cumple la comprobación indicada.");

    /// Evita que los errores de Playwright impriman formularios o valores secretos en el TRX.
    private async Task Verificar(Func<Task> recorrido)
    {
        try
        {
            await recorrido();
            Exigir(entorno.Servidor.RegistrosSinSecretos() && entorno.Servidor.RegistrosSinValores(entorno.Privados));
        }
        catch (Exception e)
        {
            var linea = new System.Diagnostics.StackTrace(e, true).GetFrames().FirstOrDefault(f => f.GetFileName()?.EndsWith("Tests.cs") == true)?.GetFileLineNumber();
            throw new XunitException($"Recorrido no conforme en {_paso}, línea {linea} ({e.GetType().Name}). Sin datos del formulario en evidencia.");
        }
        finally
        {
            foreach (var contexto in _contextos)
                await contexto.CloseAsync();
            _contextos.Clear();
        }
    }

    /// Abre una página con una sesión independiente para no mezclar los recorridos.
    private async Task<IPage> Pagina()
    {
        var contexto = await entorno.Navegador.NewContextAsync(new() { IgnoreHTTPSErrors = true });
        contexto.SetDefaultTimeout(10000);
        _contextos.Add(contexto);
        return await contexto.NewPageAsync();
    }

    /// Abre una página real y conserva una referencia segura al paso que se está comprobando.
    private async Task<IResponse> Abrir(IPage p, string ruta)
    {
        _paso = "abrir " + ruta.Split('?')[0].Split('/').ElementAtOrDefault(1);
        return (await p.GotoAsync(entorno.Url + ruta))!;
    }

    /// Completa el acceso y espera su destino; conserva las cookies solo en memoria para revisar los registros.
    private async Task Entrar(IPage p, CuentaFicticia cuenta, string retorno = "")
    {
        await Abrir(p, "/Acceso" + (retorno.Length == 0 ? "" : "?ReturnUrl=" + Uri.EscapeDataString(retorno)));
        _paso = "inicio de sesión";
        await p.Locator("[name='Entrada.NombreUsuario']").FillAsync(cuenta.Usuario);
        await p.Locator("[name='Entrada.Contrasena']").FillAsync(cuenta.Clave);
        await Boton(p, "Iniciar sesión").ClickAsync();
        await p.WaitForURLAsync(u => new Uri(u).AbsolutePath != "/Acceso");
        foreach (var cookie in await p.Context.CookiesAsync())
            entorno.Privados.Add(cookie.Value);
    }

    /// Lee la protección del formulario para enviar una solicitud de la misma sesión.
    private async Task<string> Token(IPage p)
    {
        var valor = (await p.Locator("[name='__RequestVerificationToken']").First.GetAttributeAsync("value"))!;
        entorno.Privados.Add(valor);
        return valor;
    }

    /// Envía el formulario con la cookie actual y permite comprobar la respuesta sin seguir redirecciones.
    private async Task<IAPIResponse> Enviar(IPage p, string ruta, Dictionary<string, string>? datos = null, bool protegido = true)
    {
        _paso = "POST protegido";
        datos ??= [];
        if (protegido)
            datos["__RequestVerificationToken"] = await Token(p);
        var formulario = p.APIRequest.CreateFormData();
        foreach (var dato in datos)
            formulario.Set(dato.Key, dato.Value);
        return await p.APIRequest.PostAsync(entorno.Url + ruta, new() { Form = formulario, MaxRedirects = 0 });
    }

    /// Usa el formulario real de contraseña y espera el mensaje de confirmación.
    private async Task Cambiar(IPage p, string? actual, string nueva)
    {
        await Abrir(p, "/Cuenta/Contrasena");
        _paso = "cambio de contraseña";
        entorno.Privados.Add(nueva);
        if (actual is not null)
            await p.Locator("[name='Entrada.ContrasenaActual']").FillAsync(actual);
        await p.Locator("[name='Entrada.ContrasenaNueva']").FillAsync(nueva);
        await p.Locator("[name='Entrada.Confirmacion']").FillAsync(nueva);
        await Boton(p, "Cambiar contraseña").ClickAsync();
        await Assertions.Expect(p.Locator("body")).ToContainTextAsync("Contraseña actualizada.");
    }

    /// Consulta el evento desde la misma ventana autenticada y devuelve su contenido para comprobarlo.
    private async Task<JsonElement> Auditoria(IPage p, Guid id)
    {
        var desde = Uri.EscapeDataString(DateTimeOffset.UtcNow.AddDays(-1).ToString("O"));
        var hasta = Uri.EscapeDataString(DateTimeOffset.UtcNow.AddDays(1).ToString("O"));
        var respuesta = await Abrir(p, $"/api/auditoria/eventos?idRegistro={id}&desde={desde}&hasta={hasta}");
        Exigir(respuesta.Status == 200);
        return JsonDocument.Parse(await p.Locator("body").InnerTextAsync()).RootElement.Clone();
    }
}
