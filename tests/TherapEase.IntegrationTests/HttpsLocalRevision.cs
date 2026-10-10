using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TherapEase.Application.Identidad.Interfaces.Repositorios;
using TherapEase.Application.Identidad.Interfaces.Servicios;
using TherapEase.Application.Identidad.Modelos;
using TherapEase.Domain.Auditoria.Entidades.Enums;
using TherapEase.Domain.Auditoria.Reglas;

namespace TherapEase.IntegrationTests;

/// <summary>
/// Inicia la aplicación real con HTTPS local y una base de datos desechable.
/// Usa un certificado temporal que solo aceptan los clientes de estas pruebas.
/// </summary>
public sealed class HttpsLocalRevision : IAsyncLifetime
{


    // ──── ENTORNO LOCAL ───────────────────────────────────────────────────────────────────


    private readonly PostgreSqlRevision _postgres = new();
    private readonly ConcurrentQueue<string> _registros = new();
    private const string Nombre = "https-ficticio";
    private const string Contrasena = "contrasenaficticiahttpslocal";
    private readonly string _claveCertificado = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    private X509Certificate2? _raiz;
    private Process? _proceso;
    private Task? _salida;
    private Task? _errores;
    private string? _archivoCertificado;
    private int _liberado;
    public const string Anfitrion = "therapease.local.test";
    public string Evidencia { get; }
    public Escenario Escenario { get; private set; } = null!;
    public HttpClient Cliente { get; private set; } = null!;
    public Uri Http { get; private set; } = null!;
    public Uri Https { get; private set; } = null!;
    public string Cookie { get; private set; } = "";
    public Guid Evento { get; } = Guid.NewGuid();
    public Guid Registro { get; } = Guid.NewGuid();
    public string Consulta => $"/api/auditoria/eventos?desde=2026-10-10T00:00:00Z&hasta=2026-10-11T00:00:00Z&idRegistro={Registro}";

    public HttpsLocalRevision() : this(null) { }

    /// Permite reutilizar HTTPS en otra revisión, guardando su evidencia en la carpeta correspondiente.
    internal HttpsLocalRevision(string? carpetaEvidencia)
    {
        Evidencia = carpetaEvidencia ?? Environment.GetEnvironmentVariable("THERAPEASE_EVIDENCIA_HTTPS")
            ?? Path.Combine(ServidorRevision.RaizRepositorio(), "TestResults", "https", Guid.NewGuid().ToString("N"));
    }

    /// Prepara datos ficticios, certificado y servidor; espera hasta que HTTPS responda.
    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(Evidencia);
        try
        {
            await _postgres.InitializeAsync();
            Escenario = await _postgres.Crear();
            await PrepararSesionAsync();
            await CrearCertificadoAsync();
            var puertoHttp = PuertoLibre();
            var puertoHttps = PuertoLibre();
            while (puertoHttp == puertoHttps) puertoHttps = PuertoLibre();
            Http = new Uri($"http://{Anfitrion}:{puertoHttp}");
            Https = new Uri($"https://{Anfitrion}:{puertoHttps}");
            Cliente = CrearCliente();
            ArrancarServidor(puertoHttp, puertoHttps);
            var espera = Stopwatch.StartNew();
            while (espera.Elapsed < TimeSpan.FromSeconds(40) && !_proceso!.HasExited)
            {
                try
                {
                    using var respuesta = await Cliente.GetAsync(new Uri(Https, "/salud"));
                    if (respuesta.StatusCode == HttpStatusCode.OK) return;
                }
                catch (HttpRequestException) { }
                catch (TaskCanceledException) { }
                await Task.Delay(100);
            }
            throw new InvalidOperationException("El servidor HTTPS local no estuvo disponible dentro del plazo.");
        }
        catch
        {
            await DisposeAsync();
            throw;
        }
    }

    /// Reserva un puerto libre; el servidor lo usará después de soltar la reserva.
    private static int PuertoLibre()
    {
        var reserva = new TcpListener(IPAddress.Loopback, 0);
        reserva.Start();
        var puerto = ((IPEndPoint)reserva.LocalEndpoint).Port;
        reserva.Stop();
        return puerto;
    }

    /// Crea una sesión anterior a la mitad de su duración para provocar una renovación real por HTTPS.
    private async Task PrepararSesionAsync()
    {
        var reloj = new RelojRevision();
        reloj.Avanzar(TimeSpan.FromMinutes(-16));
        using var proveedor = Escenario.Servicios(reloj.Configurar);
        using (var alcance = proveedor.CreateScope())
        {
            var creado = await alcance.ServiceProvider.GetRequiredService<IGestorIdentidad>()
                .CrearAsync(Registro, Nombre, "Superusuario", Contrasena, false);
            if (creado != ResultadoCreacionDeUsuario.Creado)
                throw new InvalidOperationException("No se creó el usuario ficticio para HTTPS.");
        }
        using (var solicitud = new SolicitudIdentidad(proveedor))
        {
            if (!(await solicitud.Obtener<IServicioAcceso>().IniciarSesionAsync(Nombre, Contrasena)).Exito)
                throw new InvalidOperationException("No se inició la sesión ficticia para HTTPS.");
            Cookie = solicitud.CookieEmitida();
        }
        await using var contexto = Escenario.Contexto();
        contexto.EventosAuditoria.Add(ReglasDeEventoAuditoria.Registrar(Evento,
            new DateTimeOffset(2026, 10, 10, 12, 0, 0, TimeSpan.Zero), Registro,
            TipoRegistroAuditoria.Paciente, Registro, AccionAuditoria.Alta, ["Nombre"], "Alta ficticia HTTPS"));
        await contexto.SaveChangesAsync();
    }


    // ──── CERTIFICADO Y CLIENTES DE PRUEBA ─────────────────────────────────────────────────


    /// Genera una autoridad y un certificado de un día, sin agregarlos a la confianza de Windows.
    private async Task CrearCertificadoAsync()
    {
        var ahora = DateTimeOffset.UtcNow;
        using var claveRaiz = RSA.Create(2048);
        var solicitudRaiz = new CertificateRequest("CN=Autoridad ficticia B00", claveRaiz,
            HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        solicitudRaiz.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
        solicitudRaiz.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign, true));
        using var raizPrivada = solicitudRaiz.CreateSelfSigned(ahora.AddMinutes(-5), ahora.AddDays(2));
        _raiz = X509CertificateLoader.LoadCertificate(raizPrivada.Export(X509ContentType.Cert));
        using var claveServidor = RSA.Create(2048);
        var solicitud = new CertificateRequest($"CN={Anfitrion}", claveServidor,
            HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        solicitud.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
        solicitud.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, true));
        solicitud.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection { new("1.3.6.1.5.5.7.3.1") }, true));
        var nombres = new SubjectAlternativeNameBuilder();
        nombres.AddDnsName(Anfitrion);
        nombres.AddDnsName("localhost");
        nombres.AddIpAddress(IPAddress.Loopback);
        solicitud.CertificateExtensions.Add(nombres.Build());
        using var publico = solicitud.Create(raizPrivada, ahora.AddMinutes(-5), ahora.AddDays(1), RandomNumberGenerator.GetBytes(16));
        using var privado = publico.CopyWithPrivateKey(claveServidor);
        _archivoCertificado = Path.Combine(Evidencia, "temporal-" + Guid.NewGuid().ToString("N") + ".pfx");
        await File.WriteAllBytesAsync(_archivoCertificado, privado.Export(X509ContentType.Pfx, _claveCertificado));
        await File.WriteAllTextAsync(Path.Combine(Evidencia, "certificado-publico.json"), JsonSerializer.Serialize(new
        {
            Anfitrion, Huella = publico.Thumbprint, Raiz = _raiz.Thumbprint, publico.NotBefore, publico.NotAfter,
            Confianza = "Solo clientes de QA; no se modifica Windows", Revocacion = "Autoridad temporal sin servicio de revocación"
        }, new JsonSerializerOptions { WriteIndented = true }));
    }

    /// Confía solo en la autoridad temporal; mantiene la comprobación del nombre del servidor.
    private X509ChainPolicy PoliticaCertificado()
    {
        var politica = new X509ChainPolicy
        {
            TrustMode = X509ChainTrustMode.CustomRootTrust,
            RevocationMode = X509RevocationMode.NoCheck,
            DisableCertificateDownloads = true
        };
        politica.CustomTrustStore.Add(_raiz!);
        return politica;
    }

    /// Conecta únicamente a la computadora local, sin cambiar DNS ni el archivo de hosts.
    private static async ValueTask<Stream> ConectarLocalAsync(SocketsHttpConnectionContext contexto, CancellationToken cancelacion)
    {
        var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        try
        {
            await socket.ConnectAsync(IPAddress.Loopback, contexto.DnsEndPoint.Port, cancelacion);
            return new NetworkStream(socket, ownsSocket: true);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }

    /// Crea un cliente sin redirecciones; puede usar cookies automáticas o rechazar la autoridad ficticia.
    public HttpClient CrearCliente(bool confiar = true, CookieContainer? cookies = null)
    {
        var manejador = new SocketsHttpHandler
        {
            AllowAutoRedirect = false, UseProxy = false, UseCookies = cookies is not null,
            ConnectCallback = ConectarLocalAsync
        };
        if (cookies is not null) manejador.CookieContainer = cookies;
        if (confiar) manejador.SslOptions.CertificateChainPolicy = PoliticaCertificado();
        return new HttpClient(manejador) { Timeout = TimeSpan.FromSeconds(5) };
    }

    /// Lee la versión TLS realmente acordada, validando certificado y nombre del servidor.
    public async Task<SslProtocols> ProtocoloAsync()
    {
        using var limite = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        using var conexion = new TcpClient();
        await conexion.ConnectAsync(IPAddress.Loopback, Https.Port, limite.Token);
        await using var canal = new SslStream(conexion.GetStream());
        await canal.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
        {
            TargetHost = Anfitrion, CertificateChainPolicy = PoliticaCertificado()
        }, limite.Token);
        await File.WriteAllTextAsync(Path.Combine(Evidencia, "tls.json"), JsonSerializer.Serialize(new
        { ProtocoloNegociado = canal.SslProtocol.ToString(), CertificadoYNombreValidados = true }));
        return canal.SslProtocol;
    }


    // ──── SERVIDOR Y EVIDENCIA SIN SECRETOS ─────────────────────────────────────────────────


    /// Ejecuta Web en modo Production solo en puertos locales para comprobar su configuración HTTPS y HSTS.
    private void ArrancarServidor(int puertoHttp, int puertoHttps)
    {
        var configuracion = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
        var dll = Path.Combine(ServidorRevision.RaizRepositorio(), "src", "Web", "bin", configuracion, "net10.0", "TherapEase.Web.dll");
        var inicio = new ProcessStartInfo("dotnet")
        {
            UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true,
            CreateNoWindow = true, WorkingDirectory = Path.GetDirectoryName(dll)!
        };
        inicio.ArgumentList.Add(dll);
        inicio.ArgumentList.Add("--urls");
        inicio.ArgumentList.Add($"http://127.0.0.1:{puertoHttp};https://127.0.0.1:{puertoHttps}");
        inicio.Environment["ConnectionStrings__TherapEase"] = Escenario.App;
        inicio.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";
        inicio.Environment["ASPNETCORE_HTTPS_PORT"] = puertoHttps.ToString(System.Globalization.CultureInfo.InvariantCulture);
        inicio.Environment["Kestrel__Certificates__Default__Path"] = _archivoCertificado;
        inicio.Environment["Kestrel__Certificates__Default__Password"] = _claveCertificado;
        _proceso = Process.Start(inicio) ?? throw new InvalidOperationException("No arrancó el servidor HTTPS.");
        _salida = LeerRegistrosAsync(_proceso.StandardOutput);
        _errores = LeerRegistrosAsync(_proceso.StandardError);
    }

    /// Lee mensajes en memoria para evitar bloqueos; no escribe su contenido en los archivos de evidencia.
    private async Task LeerRegistrosAsync(StreamReader lector)
    {
        while (await lector.ReadLineAsync() is { } linea) _registros.Enqueue(linea);
    }

    /// Envía la sesión ficticia por HTTPS y devuelve la respuesta real, incluida su renovación de cookie.
    public async Task<HttpResponseMessage> ConsultarAsync()
    {
        using var solicitud = new HttpRequestMessage(HttpMethod.Get, new Uri(Https, Consulta));
        solicitud.Headers.Add("Cookie", Cookie);
        return await Cliente.SendAsync(solicitud);
    }

    /// Busca únicamente los secretos de este entorno en los mensajes recibidos hasta ese momento.
    public bool RegistrosSinSecretos()
    {
        var texto = string.Join('\n', _registros);
        return texto.Length > 0 && new[] { Contrasena, _claveCertificado, Cookie, Escenario.App }
            .All(secreto => !texto.Contains(secreto, StringComparison.Ordinal));
    }

    /// Guarda solo cabeceras permitidas y el código HTTP; nunca guarda valores de cookies.
    public async Task GuardarCabecerasAsync(HttpResponseMessage respuesta, string nombre)
    {
        var permitidas = new[] { "Strict-Transport-Security", "X-Content-Type-Options", "Content-Security-Policy",
            "X-Frame-Options", "Referrer-Policy", "Cache-Control", "Content-Type" };
        var cabeceras = permitidas.ToDictionary(c => c, c => respuesta.Headers.TryGetValues(c, out var valores)
            ? string.Join(", ", valores) : respuesta.Content.Headers.TryGetValues(c, out valores) ? string.Join(", ", valores) : null);
        await File.WriteAllTextAsync(Path.Combine(Evidencia, nombre + ".json"), JsonSerializer.Serialize(new
        { Estado = (int)respuesta.StatusCode, Cabeceras = cabeceras }, new JsonSerializerOptions { WriteIndented = true }));
    }


    // ──── LIMPIEZA ─────────────────────────────────────────────────────────────────────────


    /// Detiene el servidor, elimina la clave temporal del disco y retira el PostgreSQL desechable.
    public async Task DisposeAsync()
    {
        if (Interlocked.Exchange(ref _liberado, 1) != 0) return;
        var servidorDetenido = _proceso is null;
        try
        {
            Cliente?.Dispose();
            if (_proceso is not null)
            {
                if (!_proceso.HasExited) _proceso.Kill(entireProcessTree: true);
                await _proceso.WaitForExitAsync();
                servidorDetenido = _proceso.HasExited;
                if (_salida is not null) await _salida;
                if (_errores is not null) await _errores;
                _proceso.Dispose();
            }
        }
        finally
        {
            try
            {
                if (_archivoCertificado is not null) File.Delete(_archivoCertificado);
                _raiz?.Dispose();
            }
            finally
            {
                await _postgres.DisposeAsync();
                await File.WriteAllTextAsync(Path.Combine(Evidencia, "limpieza.json"), JsonSerializer.Serialize(new
                { CertificadoPrivadoRetirado = _archivoCertificado is null || !File.Exists(_archivoCertificado), ServidorDetenido = servidorDetenido, PostgreSqlRetirado = true }));
            }
        }
    }
}
