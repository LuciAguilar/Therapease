using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TherapEase.Application.Identidad.Interfaces.Servicios;
using TherapEase.Domain.Auditoria.Entidades;
using TherapEase.Domain.Auditoria.Entidades.Enums;
using TherapEase.Infrastructure.Compartido.Servicios;
using static TherapEase.IntegrationTests.ServidorIdentidadRevision;

namespace TherapEase.IntegrationTests;

/// <summary>
/// Comprueba los nuevos eventos con una base de datos desechable y los servicios reales.
/// </summary>
public class EventosAccesoServidorTests(PostgreSqlRevision postgres) : IClassFixture<PostgreSqlRevision>
{


    // ──── ENTRADA Y SALIDA ─────────────────────────────────────────────────────────────────


    /// Entrar guarda su evento y reinicia los cuatro intentos anteriores antes de emitir la cookie.
    [Fact]
    public async Task Entrada_Guarda_Evento_Y_Reinicia_Intentos()
    {
        // Arrange
        var escenario = await postgres.Crear();
        using var proveedor = escenario.Servicios();
        var id = await CrearAsync(proveedor, "entradaficticia");
        await FallarEntradaAsync(proveedor, "entradaficticia", 4);

        // Act
        var cookie = await EntrarAsync(proveedor, "entradaficticia");

        // Assert
        ComprobarEvento(Assert.Single(await EventosDeAccionAsync(escenario, AccionAuditoria.InicioSesion)),
            id, "Inició sesión.");
        Assert.Equal(0, await IntentosFallidosAsync(escenario, id));
        Assert.True(await CookieSigueSirviendoAsync(proveedor, cookie));
    }

    /// Si no se guarda la entrada, conserva el contador anterior y no entrega ninguna cookie.
    [Fact]
    public async Task Entrada_Sin_Evento_Revierte_Contador_Y_No_Abre_Sesion()
    {
        // Arrange
        var escenario = await postgres.Crear();
        using var proveedor = escenario.Servicios();
        var id = await CrearAsync(proveedor, "entradaficticia");
        await FallarEntradaAsync(proveedor, "entradaficticia", 4);
        await SimularFalloDeAuditoriaAsync(escenario);
        using var solicitud = new SolicitudIdentidad(proveedor);

        // Act + Assert
        await Assert.ThrowsAsync<DbUpdateException>(() => solicitud.Obtener<IServicioAcceso>()
            .IniciarSesionAsync("entradaficticia", Contrasena));
        Assert.Equal(4, await IntentosFallidosAsync(escenario, id));
        Assert.True(solicitud.Contexto.Response.Headers.SetCookie.Count == 0,
            "El fallo de entrada emitió una cookie.");
        Assert.Empty(await EventosDeAccionAsync(escenario, AccionAuditoria.InicioSesion));
    }

    /// Salir deja su evento y la cookie anterior ya no permite autenticarse.
    [Fact]
    public async Task Salida_Guarda_Evento_Y_Revoca_La_Cookie()
    {
        // Arrange
        var escenario = await postgres.Crear();
        using var proveedor = escenario.Servicios();
        var id = await CrearAsync(proveedor, "salidaficticia");
        var cookie = await EntrarAsync(proveedor, "salidaficticia");
        using (var solicitud = new SolicitudIdentidad(proveedor, cookie))
        {
            Assert.True(await solicitud.AutenticarAsync());

            // Act
            await solicitud.Obtener<IServicioAcceso>().CerrarSesionAsync();
        }

        // Assert
        ComprobarEvento(Assert.Single(await EventosDeAccionAsync(escenario, AccionAuditoria.CierreSesion)),
            id, "Cerró sesión.");
        Assert.False(await CookieSigueSirviendoAsync(proveedor, cookie));
    }

    /// Aunque falle la auditoría al salir, la cookie queda revocada y se registra el aviso fijo.
    [Fact]
    public async Task Salida_Sin_Evento_Conserva_Revocacion_Y_Avisa()
    {
        // Arrange
        var escenario = await postgres.Crear();
        var avisos = new AvisosEnMemoria();
        using var proveedor = escenario.Servicios(s => s.AddSingleton<ILogger<RegistroTecnico>>(avisos));
        await CrearAsync(proveedor, "salidaficticia");
        var cookie = await EntrarAsync(proveedor, "salidaficticia");
        await SimularFalloDeAuditoriaAsync(escenario);
        using (var solicitud = new SolicitudIdentidad(proveedor, cookie))
        {
            Assert.True(await solicitud.AutenticarAsync());

            // Act
            await solicitud.Obtener<IServicioAcceso>().CerrarSesionAsync();
        }

        // Assert
        Assert.False(await CookieSigueSirviendoAsync(proveedor, cookie));
        Assert.Empty(await EventosDeAccionAsync(escenario, AccionAuditoria.CierreSesion));
        ComprobarAviso(Assert.Single(avisos.Entradas), 4001);
    }


    // ──── BLOQUEO DE ACCESO ────────────────────────────────────────────────────────────────


    /// Catorce intentos simultáneos dejan un solo bloqueo y su evento; se repite en tres bases nuevas.
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task Bloqueo_Simultaneo_Deja_Un_Solo_Evento(int repeticion)
    {
        // Arrange
        var escenario = await postgres.Crear();
        using var proveedor = escenario.Servicios();
        var nombre = $"concurrenteficticio{repeticion}";
        var id = await CrearAsync(proveedor, nombre);

        // Los intentos esperan esta señal para empezar al mismo tiempo.

        var continuar = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var intentos = Enumerable.Range(0, 14).Select(async _ =>
        {
            await continuar.Task;
            using var solicitud = new SolicitudIdentidad(proveedor);
            return await solicitud.Obtener<IServicioAcceso>().IniciarSesionAsync(nombre, "incorrectaficticia");
        }).ToArray();

        // Act
        continuar.SetResult();
        var respuestas = await Task.WhenAll(intentos).WaitAsync(TimeSpan.FromSeconds(45));

        // Assert
        Assert.All(respuestas, r => Assert.False(r.Exito));
        ComprobarEvento(Assert.Single(await EventosDeAccionAsync(escenario, AccionAuditoria.BloqueoAcceso)),
            id, "Se bloqueó el acceso por cinco intentos fallidos.");
        Assert.Equal(0, await IntentosFallidosAsync(escenario, id));
        Assert.True(await FinDeBloqueoAsync(escenario, id) > DateTimeOffset.UtcNow);
    }

    /// Si falla el evento del quinto intento, el bloqueo sigue y solo se emite un aviso técnico.
    [Fact]
    public async Task Bloqueo_Sin_Evento_Se_Conserva_Y_Avisa()
    {
        // Arrange
        var escenario = await postgres.Crear();
        var avisos = new AvisosEnMemoria();
        using var proveedor = escenario.Servicios(s => s.AddSingleton<ILogger<RegistroTecnico>>(avisos));
        var id = await CrearAsync(proveedor, "bloqueoficticio");
        await SimularFalloDeAuditoriaAsync(escenario);

        // Act
        await FallarEntradaAsync(proveedor, "bloqueoficticio", 7);
        using var solicitud = new SolicitudIdentidad(proveedor);
        var correcta = await solicitud.Obtener<IServicioAcceso>().IniciarSesionAsync("bloqueoficticio", Contrasena);

        // Assert
        Assert.False(correcta.Exito);
        Assert.True(await FinDeBloqueoAsync(escenario, id) > DateTimeOffset.UtcNow);
        Assert.Empty(await EventosDeAccionAsync(escenario, AccionAuditoria.BloqueoAcceso));
        ComprobarAviso(Assert.Single(avisos.Entradas), 4002);
    }

    /// Fallar la contraseña actual también bloquea; un fallo de auditoría no cierra la sesión ya abierta.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Bloqueo_En_Cambio_Propio_Conserva_Sesion_Y_Credencial(bool fallaEvento)
    {
        // Arrange
        var escenario = await postgres.Crear();
        var avisos = new AvisosEnMemoria();
        using var proveedor = escenario.Servicios(s => s.AddSingleton<ILogger<RegistroTecnico>>(avisos));
        var id = await CrearAsync(proveedor, "cambioficticio");
        var cookie = await EntrarAsync(proveedor, "cambioficticio");
        if (fallaEvento) await SimularFalloDeAuditoriaAsync(escenario);

        // Act + Assert
        for (var i = 0; i < 5; i++)
        {
            using var solicitud = new SolicitudIdentidad(proveedor, cookie);
            Assert.True(await solicitud.AutenticarAsync());
            Assert.False((await solicitud.Obtener<IServicioContrasena>()
                .CambiarContrasenaPropiaAsync("incorrectaficticia", NuevaContrasena)).Exito);
        }

        // Assert
        Assert.True(await FinDeBloqueoAsync(escenario, id) > DateTimeOffset.UtcNow);
        Assert.True(await CookieSigueSirviendoAsync(proveedor, cookie));
        Assert.True(await ContrasenaFuncionaAsync(proveedor, id, Contrasena));
        Assert.False(await ContrasenaFuncionaAsync(proveedor, id, NuevaContrasena));
        var eventos = await EventosDeAccionAsync(escenario, AccionAuditoria.BloqueoAcceso);
        if (fallaEvento)
        {
            Assert.Empty(eventos);
            ComprobarAviso(Assert.Single(avisos.Entradas), 4002);
        }
        else
        {
            ComprobarEvento(Assert.Single(eventos), id,
                "Se bloqueó el acceso por cinco intentos fallidos.");
            Assert.Empty(avisos.Entradas);
        }
    }


    // ──── AYUDAS DE LA REVISIÓN ────────────────────────────────────────────────────────────


    /// Rechaza cada entrada ficticia sin mostrar contraseñas ni cookies en los resultados.
    private static async Task FallarEntradaAsync(ServiceProvider proveedor, string nombre, int cantidad)
    {
        for (var i = 0; i < cantidad; i++)
        {
            using var solicitud = new SolicitudIdentidad(proveedor);
            Assert.False((await solicitud.Obtener<IServicioAcceso>()
                .IniciarSesionAsync(nombre, "incorrectaficticia")).Exito);
        }
    }

    /// El evento solo describe la acción sobre el usuario, con identificadores y sin campos privados.
    private static void ComprobarEvento(EventoAuditoria evento, Guid id, string hecho)
    {
        Assert.Equal(id, evento.IdUsuarioActor);
        Assert.Equal(id, evento.IdRegistro);
        Assert.Equal(TipoRegistroAuditoria.Usuario, evento.TipoRegistro);
        Assert.Empty(evento.CamposAfectados);
        Assert.Equal(hecho, evento.Hecho);
    }

    /// El aviso usa el nivel y el identificador esperados, sin adjuntar la excepción original.
    private static void ComprobarAviso((int Id, LogLevel Nivel, bool TieneExcepcion) aviso, int id)
    {
        Assert.Equal(id, aviso.Id);
        Assert.Equal(LogLevel.Warning, aviso.Nivel);
        Assert.False(aviso.TieneExcepcion);
    }

    /// <summary>
    /// Recoge solo el tipo de aviso técnico y si traía una excepción.
    /// No guarda el mensaje de una excepción, datos de usuarios ni solicitudes.
    /// </summary>
    private sealed class AvisosEnMemoria : ILogger<RegistroTecnico>
    {
        /// Avisos recogidos sin guardar sus mensajes ni datos de las solicitudes.
        public ConcurrentQueue<(int Id, LogLevel Nivel, bool TieneExcepcion)> Entradas { get; } = new();
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter) =>
            Entradas.Enqueue((eventId.Id, logLevel, exception is not null));
    }
}
