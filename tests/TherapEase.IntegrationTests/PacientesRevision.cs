using System.Collections.Concurrent;
using System.Data.Common;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using TherapEase.Application.Compartido.Modelos;
using TherapEase.Application.Pacientes.Interfaces.Servicios;
using TherapEase.Application.Pacientes.Modelos;
using TherapEase.Domain.Auditoria.Entidades;
using TherapEase.Domain.Auditoria.Entidades.Enums;
using TherapEase.Domain.Pacientes.Entidades;
using TherapEase.Domain.Pacientes.Entidades.Enums;
using TherapEase.Infrastructure.Data;
using static TherapEase.IntegrationTests.ServidorIdentidadRevision;

namespace TherapEase.IntegrationTests;

/// <summary>
/// Ayuda a las pruebas de pacientes: prepara una base de datos desechable,
/// un usuario ficticio con su sesión abierta y datos inventados de pacientes.
/// También lee lo guardado por separado para comparar antes y después.
/// Reutiliza la base, las sesiones y el reloj de prueba que ya existían; no cambia el sistema.
/// </summary>
public sealed class PacientesRevision : IDisposable
{


    // ──── PREPARACIÓN Y SESIÓN ─────────────────────────────────────────────────────────────


    public Escenario Escenario { get; }
    public ServiceProvider Proveedor { get; }

    /// Reloj de prueba: adelanta la fecha sin esperar (por ejemplo, para que pase un año).
    public RelojRevision Reloj { get; } = new();

    /// Guarda los mensajes que escribe el sistema, para buscar en ellos datos privados.
    public RegistroRevision Registro { get; } = new();

    /// Anota cada consulta que llega a la base de datos, para saber si se leyó o cambió alguna ficha.
    public ComandosDePacientesRevision Comandos { get; } = new();

    /// Usuario ficticio que realiza las operaciones.
    public Guid Actor { get; private set; }
    public string Cookie { get; private set; } = string.Empty;

    /// Fija «hoy» a las 18:00 UTC (11:00 en Hermosillo) y prepara los servicios reales
    /// con el reloj de prueba, la captura de mensajes y el registro de consultas.
    private PacientesRevision(Escenario escenario, DateOnly hoy, IInterceptor[] interceptores)
    {
        Escenario = escenario;
        var instante = new DateTimeOffset(hoy.ToDateTime(new TimeOnly(18, 0)), TimeSpan.Zero);
        Reloj.Avanzar(instante - Reloj.GetUtcNow());
        Proveedor = escenario.Servicios(servicios =>
        {
            Reloj.Configurar(servicios);
            servicios.AddSingleton<TimeProvider>(Reloj);
            servicios.AddLogging(o => o.AddProvider(Registro).SetMinimumLevel(LogLevel.Trace));
            servicios.AddDbContext<ContextoDeDatos>(o => o.AddInterceptors(Comandos).AddInterceptors(interceptores));
        });
    }

    /// Crea un usuario de prueba con el rol indicado y abre su sesión con el servicio real.
    public static async Task<PacientesRevision> CrearAsync(PostgreSqlRevision postgres,
        string rol = "Usuaria", DateOnly? hoy = null, params IInterceptor[] interceptores)
    {
        var e = new PacientesRevision(await postgres.Crear(), hoy ?? new DateOnly(2026, 10, 28), interceptores);
        try
        {
            e.Actor = await ServidorIdentidadRevision.CrearAsync(e.Proveedor, "operador-qa-ficticio", rol);
            e.Cookie = await EntrarAsync(e.Proveedor, "operador-qa-ficticio");

            // Adelanta el reloj un instante para que cada solicitud ocurra después de crear la cookie.
            // Así el sistema vuelve a revisar en la base de datos que la sesión siga vigente.

            e.Reloj.Avanzar(TimeSpan.FromMilliseconds(1));
            return e;
        }
        catch { e.Dispose(); throw; }
    }

    /// Ejecuta una operación dentro de una solicitud que valida la cookie, como en el sistema real.
    /// También sirve para probar sesiones rechazadas: en ese caso no se exige que la sesión sea válida.
    public async Task<T> EnSolicitudAsync<T>(Func<SolicitudIdentidad, Task<T>> actuar,
        string? cookie = null, bool exigirSesion = true)
    {
        using var solicitud = new SolicitudIdentidad(Proveedor, cookie ?? Cookie);
        var autenticada = await solicitud.AutenticarAsync();
        if (exigirSesion) Assert.True(autenticada, "La sesión ficticia debe seguir válida.");
        return await actuar(solicitud);
    }

    /// Ejecuta una operación del servicio de pacientes con la sesión del usuario de prueba.
    public Task<T> ServicioAsync<T>(Func<IServicioPacientes, Task<T>> actuar) =>
        EnSolicitudAsync(s => actuar(s.Obtener<IServicioPacientes>()));

    /// Registra una ficha con el servicio real y devuelve su número de ficha (identificador).
    public async Task<Guid> RegistrarAsync(DatosDePacienteEntrada? datos = null, bool confirmar = false)
    {
        var id = Guid.NewGuid();
        var r = await ServicioAsync(s => s.RegistrarAsync(id, datos ?? Adulto(), confirmar));
        Assert.True(r.Exito, r.Mensaje);
        return id;
    }

    /// Datos inventados de un paciente adulto con los dos tipos de atención.
    public static DatosDePacienteEntrada Adulto(string nombre = "Paciente Ejemplo Ficticio") =>
        new(nombre, new DateOnly(1990, 5, 8), "644 123 4567", "ejemplo@ficticio.test",
            "Escuela Ejemplo Ficticia", "Grupo Ejemplo", [AmbitoAtencion.Escolar, AmbitoAtencion.Independiente]);

    /// Datos inventados de un menor de edad con su tutora.
    public static DatosDePacienteEntrada Menor() => Adulto("Menor Ejemplo Ficticio") with
    {
        FechaNacimiento = new DateOnly(2016, 4, 9),
        Tutor = new("Tutora Ejemplo Ficticia", "Madre", "644 234 5678", "tutora@ficticio.test")
    };


    // ──── COMPROBAR LO GUARDADO ────────────────────────────────────────────────────────────


    /// Lee las fichas con una conexión aparte, no con la que las guardó.
    /// Así se ve lo que de verdad quedó en la base de datos.
    public async Task<List<Paciente>> FichasAsync()
    {
        await using var c = Escenario.Contexto();
        return await c.Pacientes.AsNoTracking().Include(p => p.Ambitos).OrderBy(p => p.IdPaciente).ToListAsync();
    }

    /// Lee solo los eventos de auditoría de pacientes, sin mezclarlos con los de iniciar sesión.
    public async Task<List<EventoAuditoria>> EventosAsync()
    {
        await using var c = Escenario.Contexto();
        return await c.EventosAuditoria.AsNoTracking().Where(e => e.TipoRegistro == TipoRegistroAuditoria.Paciente)
            .OrderBy(e => e.IdEvento).ToListAsync();
    }

    /// Exige que haya una sola ficha. Si falla, solo muestra números de ficha, no datos del paciente.
    public async Task<Paciente> FichaUnicaAsync()
    {
        var fichas = await FichasAsync();
        Assert.Single(fichas.Select(p => p.IdPaciente));
        return fichas[0];
    }

    /// Resume en un solo código todas las fichas, sus tipos de atención y sus eventos.
    /// Si cualquier dato cambia, el código también; así se comprueba que nada se modificó
    /// sin mostrar los datos cuando la prueba falla.
    /// Ordena los tipos de atención para que leerlos en otro orden no cuente como cambio.
    public async Task<string> HuellaAsync()
    {
        var fichas = (await FichasAsync()).Select(p =>
        {
            var fila = JsonSerializer.SerializeToNode(p)!;
            fila["Ambitos"] = JsonSerializer.SerializeToNode(p.Ambitos.OrderBy(a => a.Ambito));
            return fila;
        });
        var texto = JsonSerializer.Serialize(fichas) + JsonSerializer.Serialize(await EventosAsync());
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(texto)));
    }

    /// Cambia datos directo en la base de datos para preparar un caso (por ejemplo, desactivar al usuario).
    /// Usa parámetros y el usuario con permiso para cambiar la estructura de la base.
    public async Task SqlAsync(string sql, params (string Nombre, object Valor)[] parametros)
    {
        await using var conexion = new NpgsqlConnection(Escenario.Migrador);
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand(sql, conexion);
        foreach (var (nombre, valor) in parametros) comando.Parameters.AddWithValue(nombre, valor);
        await comando.ExecuteNonQueryAsync();
    }

    /// Retira el rechazo de auditoría preparado para la prueba, para poder guardar de nuevo.
    public Task QuitarFalloDeAuditoriaAsync() =>
        SqlAsync("DROP TRIGGER rechazar_evento_de_revision ON \"EventoAuditoria\"");

    public void Dispose() => Proveedor.Dispose();
}

/// <summary>
/// Anota el texto de cada consulta que la aplicación envía a la base de datos, sin sus valores.
/// Permite comprobar, por ejemplo, que una solicitud sin permiso no llegó a leer fichas
/// o que guardar sin cambios no escribió nada.
/// </summary>
public sealed class ComandosDePacientesRevision : DbCommandInterceptor
{


    // ──── CONSULTAS ANOTADAS ───────────────────────────────────────────────────────────────


    private readonly ConcurrentQueue<string> _textos = new();

    /// Consultas anotadas desde la última limpieza.
    public string[] Textos => _textos.ToArray();

    /// Borra lo anotado antes de la parte de la prueba que se quiere observar.
    public void Limpiar() => _textos.Clear();

    /// Anota las lecturas y las escrituras justo antes de enviarlas.
    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
        CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
    { _textos.Enqueue(command.CommandText); return new(result); }
    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command,
        CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    { _textos.Enqueue(command.CommandText); return new(result); }
}

/// <summary>
/// Detiene las dos primeras solicitudes justo antes de guardar una ficha, hasta que lleguen ambas.
/// Así se comprueba que de verdad coinciden en el tiempo, sin depender de pausas.
/// Recibe qué cambio esperar: una ficha nueva (registro) o una modificada (edición).
/// Si la segunda solicitud no llega en 20 segundos, la prueba falla.
/// </summary>
public sealed class EsperaGuardadoPacienteRevision(EntityState estado) : SaveChangesInterceptor
{


    // ──── CONTROL DE LA ESPERA ─────────────────────────────────────────────────────────────


    private int _llegadas;
    private readonly TaskCompletionSource _ambas = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// Cuántas solicitudes llegaron al guardado; las pruebas exigen dos.
    public int Llegadas => Volatile.Read(ref _llegadas);

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
        InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (eventData.Context!.ChangeTracker.Entries<Paciente>().Any(p => p.State == estado))
        {
            var llegada = Interlocked.Increment(ref _llegadas);
            if (llegada == 2) _ambas.TrySetResult();
            if (llegada <= 2) await _ambas.Task.WaitAsync(TimeSpan.FromSeconds(20), cancellationToken);
        }
        return result;
    }
}
