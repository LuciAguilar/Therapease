namespace TherapEase.Web.Salud;

// Sondeo para el HEALTHCHECK de Docker: la imagen de ejecucion no incluye curl ni wget.
public static class SondaDeSalud
{
    private static readonly TimeSpan TiempoMaximo = TimeSpan.FromSeconds(5);

    public static async Task<int> EjecutarAsync(string? url, HttpMessageHandler? manejador = null)
    {
        // Solo se sondea la propia maquina.
        if (!Uri.TryCreate(url, UriKind.Absolute, out var destino)
            || (destino.Scheme != Uri.UriSchemeHttp && destino.Scheme != Uri.UriSchemeHttps)
            || !destino.IsLoopback)
        {
            return 1;
        }

        using var cliente = manejador is null ? new HttpClient() : new HttpClient(manejador);
        cliente.Timeout = TiempoMaximo;

        try
        {
            using var respuesta = await cliente.GetAsync(destino);
            return respuesta.IsSuccessStatusCode ? 0 : 1;
        }
        catch (Exception excepcion) when (excepcion is HttpRequestException or TaskCanceledException)
        {
            return 1;
        }
    }
}
