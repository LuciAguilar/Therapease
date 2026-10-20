namespace TherapEase.IntegrationTests;

/// <summary>
/// Prepara el servidor HTTPS de las pruebas de sesiones con evidencia separada.
/// Evita que otra revisión escriba sobre sus certificados o resultados temporales.
/// </summary>
public sealed class SesionesHttpsRevision : IAsyncLifetime
{


    // ──── SERVIDOR DE PRUEBA ───────────────────────────────────────────────────────────────


    /// Servidor existente de QA; conserva la comprobación del certificado y la base desechable.
    public HttpsLocalRevision Servidor { get; }

    /// Cada servidor usa su carpeta, aunque todas las pruebas reciban la misma ruta principal.
    public SesionesHttpsRevision()
    {
        var raiz = Environment.GetEnvironmentVariable("THERAPEASE_EVIDENCIA_HTTPS")
            ?? Path.Combine(ServidorRevision.RaizRepositorio(), "TestResults", "https");
        Servidor = new HttpsLocalRevision(Path.Combine(raiz, "sesiones", Guid.NewGuid().ToString("N")));
    }

    /// Prepara certificado, base ficticia y aplicación local con la ayuda existente.
    public Task InitializeAsync() => Servidor.InitializeAsync();

    /// Detiene el entorno y retira el certificado privado temporal al terminar.
    public Task DisposeAsync() => Servidor.DisposeAsync();
}
