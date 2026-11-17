using System.Collections.Concurrent;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TherapEase.IntegrationTests;

/// <summary>
/// Simula una sola solicitud web para usar los servicios reales de acceso, contraseña y
/// usuarios sin abrir páginas. Puede llevar una cookie de sesión y leer la cookie que el
/// sistema emite, para comprobar si una sesión sigue válida o quedó revocada.
/// </summary>
public sealed class SolicitudIdentidad : IDisposable
{


    // ──── PROPIEDADES ────────────────────────────────────────────────────────────────────────


    private readonly IServiceScope _alcance;

    /// Servicios de esta solicitud; viven y se liberan junto con ella.
    public IServiceProvider Servicios => _alcance.ServiceProvider;

    /// Datos de la solicitud y su respuesta (cabeceras, cookies y usuario autenticado).
    public DefaultHttpContext Contexto { get; }

    /// Prepara una solicitud HTTPS a localhost; si recibe una cookie, la envía como lo haría el navegador.
    public SolicitudIdentidad(ServiceProvider proveedor, string? cookie = null)
    {
        _alcance = proveedor.CreateScope();
        Contexto = new DefaultHttpContext { RequestServices = Servicios };
        Contexto.Request.Scheme = "https";   // Las cookies Secure solo se emiten en HTTPS.
        Contexto.Request.Host = new HostString("localhost");
        if (cookie is not null) Contexto.Request.Headers.Cookie = cookie;

        // Los servicios leen al usuario actual desde aquí, igual que en el servidor real.

        Servicios.GetRequiredService<IHttpContextAccessor>().HttpContext = Contexto;
    }


    // ──── USO EN LAS PRUEBAS ─────────────────────────────────────────────────────────────────


    /// Obtiene un servicio real del sistema dentro de esta solicitud.
    public T Obtener<T>() where T : notnull => Servicios.GetRequiredService<T>();


    /// Valida la cookie recibida como lo hace el servidor en cada solicitud (incluye el sello
    /// de seguridad, marca que se renueva al cerrar sesiones, y que el usuario siga activo). Devuelve false si la sesión ya no vale.
    public async Task<bool> AutenticarAsync()
    {
        var resultado = await Contexto.AuthenticateAsync(IdentityConstants.ApplicationScheme);
        if (resultado.Succeeded) Contexto.User = resultado.Principal!;
        return resultado.Succeeded;
    }


    /// Devuelve la cookie de sesión que el sistema emitió en esta respuesta, lista para
    /// enviarla en otra solicitud (solo «nombre=valor», sin sus atributos).
    public string CookieEmitida()
    {
        var cabecera = Contexto.Response.Headers.SetCookie.Single(valor =>
            valor?.StartsWith("TherapEase.Sesion=", StringComparison.Ordinal) == true)!;
        return cabecera.Split(';', 2)[0];
    }


    /// Libera la solicitud y deja de exponerla como la solicitud actual.
    public void Dispose()
    {
        Servicios.GetRequiredService<IHttpContextAccessor>().HttpContext = null;
        _alcance.Dispose();
    }
}



/// <summary>
/// Reloj controlable solo para pruebas: permite «adelantar» el tiempo y comprobar que la
/// sesión caduca a los 30 minutos sin esperar ese tiempo de verdad.
/// </summary>
public sealed class RelojRevision : TimeProvider
{
    private DateTimeOffset _ahora = DateTimeOffset.UtcNow;

    /// Hora que ven la cookie y la validación del sello en lugar de la hora real.
    public override DateTimeOffset GetUtcNow() => _ahora;

    /// Mueve el reloj hacia adelante la cantidad indicada.
    public void Avanzar(TimeSpan tiempo) => _ahora += tiempo;

    /// Hace que la cookie de sesión y la validación del sello usen este reloj.
    public void Configurar(IServiceCollection servicios)
    {
        servicios.Configure<CookieAuthenticationOptions>(IdentityConstants.ApplicationScheme, opciones => opciones.TimeProvider = this);
        servicios.Configure<SecurityStampValidatorOptions>(opciones => opciones.TimeProvider = this);
    }
}



/// <summary>
/// Guarda en memoria todos los mensajes de registro (logs) del sistema durante una prueba,
/// para comprobar que no contienen contraseñas, cookies ni conexiones.
/// </summary>
public sealed class RegistroRevision : ILoggerProvider
{
    private readonly ConcurrentQueue<string> _mensajes = new();

    /// Todo lo registrado, unido en un solo texto para buscar valores prohibidos.
    public string Texto => string.Join('\n', _mensajes);

    public ILogger CreateLogger(string categoryName) => new Registro(_mensajes);
    public void Dispose() { }

    /// <summary>
    /// Recoge mensajes de cualquier nivel para comprobar que no revelan datos sensibles.
    /// </summary>
    private sealed class Registro(ConcurrentQueue<string> mensajes) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) => mensajes.Enqueue(formatter(state, exception));
    }
}
