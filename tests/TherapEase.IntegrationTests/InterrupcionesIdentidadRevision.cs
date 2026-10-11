using System.Data.Common;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using TherapEase.Application.Identidad.Interfaces.Repositorios;
using TherapEase.Application.Identidad.Interfaces.Servicios;
using TherapEase.Application.Identidad.Modelos;
using TherapEase.Infrastructure.Data;
using TherapEase.Infrastructure.Identidad.Entidades;

namespace TherapEase.IntegrationTests;

/// <summary>
/// Prepara cuentas ficticias y servicios reales para interrumpir un guardado de Identity.
/// Permite consultar el resultado desde otra conexión y guardar evidencia sin contraseñas.
/// </summary>
public sealed class InterrupcionesIdentidadRevision : IDisposable
{


    // ──── CUENTAS Y SERVICIOS ─────────────────────────────────────────────────────────────


    private const string Contrasena = "contrasenaficticiainterrupciones";
    private const string NombreObjetivo = "objetivo-ficticio-q08";
    private readonly ServiceProvider _proveedor;
    private readonly string _cookieActor;
    private readonly string? _cookieObjetivo;
    private readonly Guid _objetivo;
    public Escenario Escenario { get; }
    public CorteGuardadoRevision Corte { get; }
    public string Evidencia { get; } = Environment.GetEnvironmentVariable("THERAPEASE_EVIDENCIA_Q08")
        ?? Path.Combine(ServidorRevision.RaizRepositorio(), "TestResults", "interrupciones", Guid.NewGuid().ToString("N"));

    private InterrupcionesIdentidadRevision(Escenario escenario, string cookieActor,
        string? cookieObjetivo, Guid objetivo, string administrador)
    {
        Escenario = escenario;
        _cookieActor = cookieActor;
        _cookieObjetivo = cookieObjetivo;
        _objetivo = objetivo;
        Corte = new CorteGuardadoRevision(administrador);
        _proveedor = escenario.Servicios(servicios =>
            servicios.AddDbContext<ContextoDeDatos>(opciones => opciones.AddInterceptors(Corte)));
        Directory.CreateDirectory(Evidencia);
    }

    /// Crea al operador y, salvo para probar el alta, al usuario que se modificará.
    public static async Task<InterrupcionesIdentidadRevision> CrearAsync(PostgreSqlRevision postgres, string operacion)
    {
        var escenario = await postgres.Crear();
        using var proveedor = escenario.Servicios();
        using (var alcance = proveedor.CreateScope())
        {
            var gestor = alcance.ServiceProvider.GetRequiredService<IGestorIdentidad>();
            if (await gestor.CrearAsync(Guid.NewGuid(), "operador-ficticio-q08", "Superusuario", Contrasena, false)
                != ResultadoCreacionDeUsuario.Creado)
                throw new InvalidOperationException("No se creó el operador ficticio.");
        }
        var objetivo = Guid.NewGuid();
        if (operacion != "crear")
        {
            using var alcance = proveedor.CreateScope();
            if (await alcance.ServiceProvider.GetRequiredService<IGestorIdentidad>()
                .CrearAsync(objetivo, NombreObjetivo, "Usuaria", Contrasena, operacion == "cambiar-propia-temporal") != ResultadoCreacionDeUsuario.Creado)
                throw new InvalidOperationException("No se creó el usuario ficticio.");
        }
        var actor = await EntrarAsync(proveedor, "operador-ficticio-q08");
        var cookie = operacion == "crear" ? null : await EntrarAsync(proveedor, NombreObjetivo);
        return new(escenario, actor, cookie, objetivo, postgres.Contenedor.GetConnectionString());
    }

    /// Obtiene una cookie real antes de activar cualquier interrupción.
    private static async Task<string> EntrarAsync(ServiceProvider proveedor, string nombre)
    {
        using var solicitud = new SolicitudIdentidad(proveedor);
        if (!(await solicitud.Obtener<IServicioAcceso>().IniciarSesionAsync(nombre, Contrasena)).Exito)
            throw new InvalidOperationException("No se abrió la sesión ficticia.");
        return solicitud.CookieEmitida();
    }


    // ──── OPERACIONES Y CONSULTA INDEPENDIENTE ────────────────────────────────────────────


    /// Ejecuta una operación real; el corte se activa después de comprobar al operador.
    public async Task<ResultadoGuardadoRevision> EjecutarAsync(string operacion, string? corte = null,
        CancellationToken cancelacion = default)
    {
        using var solicitud = new SolicitudIdentidad(_proveedor,
            operacion is "cambiar-propia" or "cambiar-propia-temporal" ? _cookieObjetivo : _cookieActor);
        if (!await solicitud.AutenticarAsync()) throw new InvalidOperationException("La sesión del operador no es válida.");
        if (corte is not null) Corte.Activar(corte, operacion == "cambiar-propia" ? 1 : 0);
        if (operacion is "cambiar-propia" or "cambiar-propia-temporal")
        {
            var propia = await solicitud.Obtener<IServicioContrasena>()
                .CambiarContrasenaPropiaAsync(operacion == "cambiar-propia" ? Contrasena : null,
                    "nuevacontrasenaficticiaq08", cancelacion);
            return new(propia.Exito, false);
        }
        var servicio = solicitud.Obtener<IServicioUsuarios>();
        if (operacion is "crear" or "restablecer")
        {
            var respuesta = operacion == "crear"
                ? await servicio.CrearUsuarioAsync(NombreObjetivo, "Usuaria", cancelacion)
                : await servicio.RestablecerContrasenaAsync(_objetivo, cancelacion);
            var temporalVerificada = false;
            if (respuesta.Exito && respuesta.Datos is { } datos)
            {
                var usuarios = solicitud.Obtener<UserManager<Usuario>>();
                var usuario = await usuarios.FindByIdAsync(datos.Usuario.IdUsuario.ToString());
                temporalVerificada = usuario is not null && await usuarios.CheckPasswordAsync(usuario, datos.ContrasenaTemporal);
            }
            return new(respuesta.Exito, temporalVerificada);
        }
        var cambio = operacion switch
        {
            "desactivar" => await servicio.DesactivarUsuarioAsync(_objetivo, cancelacion),
            "cambiar-rol" => await servicio.CambiarRolAsync(_objetivo, "Superusuario", cancelacion),
            _ => throw new ArgumentOutOfRangeException(nameof(operacion))
        };
        return new(cambio.Exito, false);
    }

    /// Consulta usuarios, roles y auditoría por otra conexión, sin depender del estado en memoria de EF.
    public async Task<EstadoGuardadoRevision> EstadoAsync()
    {
        await using var conexion = new NpgsqlConnection(Escenario.App);
        await conexion.OpenAsync();
        var partes = new List<string>();
        foreach (var sql in new[]
        {
            """SELECT COALESCE(json_agg(u ORDER BY u."Id")::text,'[]') FROM "AspNetUsers" u""",
            """SELECT COALESCE(json_agg(r ORDER BY r."UserId",r."RoleId")::text,'[]') FROM "AspNetUserRoles" r""",
            """SELECT COALESCE(json_agg(e ORDER BY e."IdEvento")::text,'[]') FROM "EventoAuditoria" e"""
        })
        {
            await using var consulta = new NpgsqlCommand(sql, conexion);
            partes.Add((string)(await consulta.ExecuteScalarAsync())!);
        }
        await using var comando = new NpgsqlCommand("""
            SELECT u."Activo",u."DebeCambiarContrasena",r."Name",row_to_json(u)::text
            FROM "AspNetUsers" u
            LEFT JOIN "AspNetUserRoles" ur ON ur."UserId"=u."Id"
            LEFT JOIN "AspNetRoles" r ON r."Id"=ur."RoleId"
            WHERE u."NormalizedUserName"=@nombre
            """, conexion);
        comando.Parameters.AddWithValue("nombre", NombreObjetivo.ToUpperInvariant());
        bool? activo = null, temporal = null;
        string? rol = null, huellaUsuario = null;
        long cantidad = 0;
        await using (var lector = await comando.ExecuteReaderAsync())
        {
            while (await lector.ReadAsync())
            {
                cantidad++;
                activo = lector.GetBoolean(0);
                temporal = lector.GetBoolean(1);
                rol = lector.IsDBNull(2) ? null : lector.GetString(2);
                huellaUsuario = Huella(lector.GetString(3));
            }
        }
        var eventos = await PostgreSqlRevision.Valor<long>(Escenario.App, "SELECT count(*) FROM \"EventoAuditoria\"");
        return new(Huella(string.Join('\n', partes)), eventos, cantidad, activo, temporal, rol, huellaUsuario);
    }

    /// Comprueba desde una solicitud nueva si la cookie anterior del usuario todavía sirve.
    public async Task<bool> SesionObjetivoValidaAsync()
    {
        using var solicitud = new SolicitudIdentidad(_proveedor, _cookieObjetivo);
        return await solicitud.AutenticarAsync();
    }

    /// Permite volver a entrar con la nueva clave conocida si se perdió la respuesta del cambio propio.
    public async Task<bool> NuevaClavePropiaValidaAsync()
    {
        using var solicitud = new SolicitudIdentidad(_proveedor);
        var respuesta = await solicitud.Obtener<IServicioAcceso>()
            .IniciarSesionAsync(NombreObjetivo, "nuevacontrasenaficticiaq08");
        return respuesta.Exito && !respuesta.Datos!.DebeCambiarContrasena;
    }

    /// Guarda solo resúmenes y huellas; no escribe los datos usados para calcularlas.
    public Task GuardarAsync(string nombre, object resumen) => File.WriteAllTextAsync(
        Path.Combine(Evidencia, nombre + ".json"), JsonSerializer.Serialize(resumen, new JsonSerializerOptions { WriteIndented = true }));

    /// Resume un contenido para comparar cambios sin publicarlo.
    private static string Huella(string contenido) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(contenido)));

    /// Libera los servicios; el contenedor pertenece a la clase de pruebas y se retira al terminar.
    public void Dispose() => _proveedor.Dispose();
}

/// <summary>
/// Detiene o interrumpe la confirmación de una transacción real, solo dentro de estas pruebas.
/// Distingue un cambio aún sin confirmar de otro confirmado cuya respuesta se pierde.
/// </summary>
public sealed class CorteGuardadoRevision(string administrador) : DbTransactionInterceptor
{


    // ──── PUNTOS DE INTERRUPCIÓN ──────────────────────────────────────────────────────────


    private string? _modo;
    private int _porSaltar;
    private bool _confirmacionIgnorada;
    public int AntesDeConfirmar { get; private set; }
    public int Confirmadas { get; private set; }
    public int ConfirmacionesPrevias { get; private set; }
    public bool ConexionTerminada { get; private set; }
    public TaskCompletionSource Alcanzado { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource Continuar { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// En el cambio propio ordinario deja pasar la comprobación de la clave actual y corta el cambio siguiente.
    public void Activar(string modo, int confirmarAntes = 0)
    {
        _modo = modo;
        _porSaltar = confirmarAntes;
    }

    /// Se ejecuta después de enviar las escrituras, justo antes de confirmar en PostgreSQL.
    public override async ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction,
        TransactionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
    {
        if (_modo is null) return result;
        _confirmacionIgnorada = _porSaltar > 0;
        if (_confirmacionIgnorada)
        {
            _porSaltar--;
            return result;
        }
        AntesDeConfirmar++;
        Alcanzado.TrySetResult();
        if (_modo == "esperar") await Continuar.Task.WaitAsync(cancellationToken);
        if (_modo == "antes")
        {
            _modo = null;
            throw new IOException("Interrupción ficticia antes de confirmar.");
        }
        if (_modo == "conexion")
        {
            _modo = null;
            await using var conexion = new NpgsqlConnection(administrador);
            await conexion.OpenAsync(cancellationToken);
            await using var comando = new NpgsqlCommand("SELECT pg_terminate_backend(@pid)", conexion);
            comando.Parameters.AddWithValue("pid", ((NpgsqlConnection)transaction.Connection!).ProcessID);
            ConexionTerminada = (bool)(await comando.ExecuteScalarAsync(cancellationToken))!;
        }
        return result;
    }

    /// PostgreSQL ya confirmó; una excepción aquí impide que el servicio entregue su respuesta.
    public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        if (_modo is null) return Task.CompletedTask;
        if (_confirmacionIgnorada)
        {
            ConfirmacionesPrevias++;
            _confirmacionIgnorada = false;
            return Task.CompletedTask;
        }
        Confirmadas++;
        var modo = _modo;
        _modo = null;
        if (modo == "despues") throw new IOException("Respuesta ficticia perdida después de confirmar.");
        return Task.CompletedTask;
    }
}

/// <summary>Resultado sin contraseña: indica si hubo éxito y si la temporal entregada corresponde al hash real.</summary>
public sealed record ResultadoGuardadoRevision(bool Exito, bool TemporalVerificada);

/// <summary>Resumen de filas consultadas desde otra conexión; conserva huellas y estados, sin datos privados.</summary>
public sealed record EstadoGuardadoRevision(string Huella, long Eventos, long UsuariosObjetivo,
    bool? Activo, bool? Temporal, string? Rol, string? HuellaUsuario);
