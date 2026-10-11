using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace TherapEase.IntegrationTests;

/// <summary>
/// Arranca el servidor web real del repositorio en un puerto local libre, conectado a la
/// base de una prueba. Permite hacer solicitudes HTTP como lo haría un cliente externo.
/// No agrega rutas de prueba a la aplicación.
/// </summary>
public sealed class ServidorRevision : IAsyncDisposable
{


    // ──── ESTADO DEL PROCESO ─────────────────────────────────────────────────────────────────


    private readonly Process _proceso;
    private readonly Task<string> _salida;   // Se lee para que el proceso no se detenga por buffer lleno.
    private readonly Task<string> _errores;
    private readonly HttpClient _cliente;

    private ServidorRevision(Process proceso, HttpClient cliente)
    {
        _proceso = proceso;
        _cliente = cliente;
        _salida = proceso.StandardOutput.ReadToEndAsync();
        _errores = proceso.StandardError.ReadToEndAsync();
    }


    // ──── ARRANQUE ───────────────────────────────────────────────────────────────────────────


    /// Busca hacia arriba la carpeta que contiene TherapEase.sln, para ubicar la app compilada.
    public static string RaizRepositorio()
    {
        for (var carpeta = new DirectoryInfo(AppContext.BaseDirectory); carpeta is not null; carpeta = carpeta.Parent)
            if (File.Exists(Path.Combine(carpeta.FullName, "TherapEase.sln"))) return carpeta.FullName;
        throw new InvalidOperationException("No se encontró la solución. Ejecuta las pruebas desde el repositorio.");
    }


    /// Inicia la aplicación compilada con la conexión del usuario de la aplicación y espera
    /// a que /salud responda. Si no arranca a tiempo, la detiene y avisa con un error.
    public static async Task<ServidorRevision> IniciarAsync(Escenario escenario)
    {

        // Pide al sistema un puerto libre para no chocar con otras pruebas o programas.

        var reserva = new TcpListener(IPAddress.Loopback, 0);
        reserva.Start();
        var puerto = ((IPEndPoint)reserva.LocalEndpoint).Port;
        reserva.Stop();
        var configuracion = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;   // Debug o Release, igual que las pruebas.
        var dll = Path.Combine(RaizRepositorio(), "src", "Web", "bin", configuracion, "net10.0", "TherapEase.Web.dll");
        var inicio = new ProcessStartInfo("dotnet")
        {
            UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true,
            CreateNoWindow = true, WorkingDirectory = Path.GetDirectoryName(dll)!
        };
        inicio.ArgumentList.Add(dll);
        inicio.ArgumentList.Add("--urls");
        inicio.ArgumentList.Add($"http://127.0.0.1:{puerto}");
        inicio.Environment["ConnectionStrings__TherapEase"] = escenario.App;
        inicio.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
        var proceso = Process.Start(inicio) ?? throw new InvalidOperationException("No arrancó el servidor local.");

        // Sin redirecciones ni cookies automáticas: cada prueba decide qué cookie envía y ve la respuesta tal cual.

        var servidor = new ServidorRevision(proceso, new HttpClient(new HttpClientHandler
        {
            AllowAutoRedirect = false, UseCookies = false
        }) { BaseAddress = new Uri($"http://127.0.0.1:{puerto}"), Timeout = TimeSpan.FromSeconds(3) });
        try
        {

            // Hasta unos 8 segundos de espera para que el servidor quede listo.

            for (var intento = 0; intento < 80 && !proceso.HasExited; intento++)
            {
                try
                {
                    if (await servidor.EstadoAsync(null, "/salud") == 200) return servidor;
                }
                catch (HttpRequestException) { }   // Todavía no escucha; se reintenta.
                await Task.Delay(100);
            }
            throw new InvalidOperationException("El servidor no estuvo disponible dentro del plazo de la prueba.");
        }
        catch
        {
            await servidor.DisposeAsync();
            throw;
        }
    }


    // ──── SOLICITUDES ────────────────────────────────────────────────────────────────────────


    /// Hace un GET a la ruta, con o sin cookie de sesión, y devuelve solo el código HTTP
    /// (200, 401, 403…), que es lo que las pruebas comparan.
    public async Task<int> EstadoAsync(string? cookie, string ruta)
    {
        using var solicitud = new HttpRequestMessage(HttpMethod.Get, ruta);
        if (cookie is not null) solicitud.Headers.Add("Cookie", cookie);
        using var respuesta = await _cliente.SendAsync(solicitud);
        return (int)respuesta.StatusCode;
    }


    // ──── CIERRE ─────────────────────────────────────────────────────────────────────────────


    /// Detiene el servidor y todos sus procesos hijos al terminar la prueba.
    public async ValueTask DisposeAsync()
    {
        _cliente.Dispose();
        if (!_proceso.HasExited) _proceso.Kill(entireProcessTree: true);
        await _proceso.WaitForExitAsync();

        // Se drenan las salidas sin imprimir cookies, credenciales ni conexiones en los resultados.

        await _salida;
        await _errores;
        _proceso.Dispose();
    }
}
