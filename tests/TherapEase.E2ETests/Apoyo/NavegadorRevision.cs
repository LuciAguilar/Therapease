using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using TherapEase.Application.Identidad.Interfaces.Repositorios;
using TherapEase.Application.Identidad.Interfaces.Servicios;
using TherapEase.Application.Identidad.Modelos;
using TherapEase.IntegrationTests;

namespace TherapEase.E2ETests;

/// <summary>
/// Comparte el servidor HTTPS y PostgreSQL desechable con las pruebas de navegador.
/// Cada recorrido crea cuentas y contextos nuevos; los secretos permanecen en memoria.
/// </summary>
public sealed class NavegadorRevision : IAsyncLifetime
{


    // ──── ENTORNO Y DATOS FICTICIOS ────────────────────────────────────────────────────────


    public HttpsLocalRevision Servidor { get; } = new();
    public IBrowser Navegador { get; private set; } = null!;
    public string Url => "https://localhost:" + Servidor.Https.Port;

    private IPlaywright? _playwright;
    internal List<string> Privados { get; } = [];

    /// Prepara PostgreSQL, HTTPS y el navegador para ejecutar las páginas reales con datos ficticios.
    public async Task InitializeAsync()
    {
        try
        {
            await Servidor.InitializeAsync();
            await Servidor.ProtocoloAsync();
            _playwright = await Playwright.CreateAsync();
            Navegador = await _playwright.Chromium.LaunchAsync(new() { Channel = Environment.GetEnvironmentVariable("THERAPEASE_CANAL_NAVEGADOR"), Headless = true });
            await File.WriteAllTextAsync(Path.Combine(Servidor.Evidencia, "navegador.json"), System.Text.Json.JsonSerializer.Serialize(new { Version = Navegador.Version, Canal = Environment.GetEnvironmentVariable("THERAPEASE_CANAL_NAVEGADOR") ?? "chromium", Servidor = "HTTPS local, Production, PostgreSQL desechable", Certificado = "TLS y nombre comprobados por cliente .NET. Navegador acepta el certificado ficticio solo en su contexto.", Trazas = "No se guardan formularios, cookies, contraseñas, capturas ni trazas de identidad." }));
        }
        catch
        {
            await DisposeAsync();
            throw new InvalidOperationException("No arrancó el entorno de navegador. Comprueba Docker, la publicación local y playwright install chromium.");
        }
    }

    /// Crea una cuenta diferente para cada recorrido y conserva su contraseña solo en memoria.
    internal async Task<CuentaFicticia> CrearCuentaAsync(string rol = "Usuaria", bool temporal = false)
    {
        var cuenta = new CuentaFicticia(Guid.NewGuid(), "qa-" + Guid.NewGuid().ToString("N"), "Ficticia-" + Guid.NewGuid().ToString("N"));
        Privados.Add(cuenta.Clave);
        using var proveedor = Servidor.Escenario.Servicios();
        using var alcance = proveedor.CreateScope();
        var resultado = await alcance.ServiceProvider.GetRequiredService<IGestorIdentidad>().CrearAsync(cuenta.Id, cuenta.Usuario, rol, cuenta.Clave, temporal, "Persona Ficticia");
        Assert.True(resultado == ResultadoCreacionDeUsuario.Creado, "No se preparó la cuenta ficticia.");
        return cuenta;
    }

    /// Emite una cookie con reloj controlado; el servidor real decide si caducó o debe renovarla.
    internal async Task<string> CookieAnteriorAsync(CuentaFicticia cuenta, int minutos)
    {
        var reloj = new RelojRevision();
        reloj.Avanzar(TimeSpan.FromMinutes(-minutos));
        using var proveedor = Servidor.Escenario.Servicios(reloj.Configurar);
        using var solicitud = new SolicitudIdentidad(proveedor);
        Assert.True((await solicitud.Obtener<IServicioAcceso>().IniciarSesionAsync(cuenta.Usuario, cuenta.Clave)).Exito);
        var valor = solicitud.CookieEmitida().Split('=', 2)[1];
        Privados.Add(valor);
        return valor;
    }

    /// Cierra el navegador y retira el servidor y la base de datos desechables al terminar.
    public async Task DisposeAsync()
    {
        try
        {
            if (Navegador is not null)
                await Navegador.CloseAsync();
        }
        finally
        {
            _playwright?.Dispose();
            await Servidor.DisposeAsync();
        }
    }
}

/// <summary>
/// Mantiene el identificador, usuario y contraseña ficticios de un recorrido.
/// Estos valores permanecen en memoria y no se guardan en los resultados.
/// </summary>
internal sealed record CuentaFicticia(Guid Id, string Usuario, string Clave);
