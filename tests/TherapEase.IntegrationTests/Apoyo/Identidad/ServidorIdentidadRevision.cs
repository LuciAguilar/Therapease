using System.Data.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using TherapEase.Application.Identidad.Interfaces.Repositorios;
using TherapEase.Application.Identidad.Interfaces.Servicios;
using TherapEase.Application.Identidad.Modelos;
using TherapEase.Domain.Auditoria.Entidades;
using TherapEase.Domain.Auditoria.Entidades.Enums;
using TherapEase.Infrastructure.Identidad.Entidades;

namespace TherapEase.IntegrationTests;

/// <summary>
/// Ayuda a las pruebas: crea usuarios ficticios y abre sus sesiones.
/// También lee lo guardado en la base de datos para comparar antes y después.
/// No crea páginas ni cambia el sistema.
/// </summary>
public static class ServidorIdentidadRevision
{


    // ──── USUARIOS Y SOLICITUDES ───────────────────────────────────────────────────────────


    // Contraseñas ficticias: solo existen en estas pruebas.

    public const string Contrasena = "contrasenaficticiaservidor";
    public const string NuevaContrasena = "nuevacontrasenaficticiaservidor";

    /// Crea un usuario ficticio directo en la base de datos, sin generar auditoría.
    public static async Task<Guid> CrearAsync(ServiceProvider proveedor, string nombre,
        string rol = "Usuaria", bool temporal = false, string? nombreCompleto = null, string? correo = null)
    {
        using var alcance = proveedor.CreateScope();
        var id = Guid.NewGuid();
        var resultado = await alcance.ServiceProvider.GetRequiredService<IGestorIdentidad>()
            .CrearAsync(id, nombre, rol, Contrasena, temporal, nombreCompleto, correo);
        Assert.Equal(ResultadoCreacionDeUsuario.Creado, resultado);
        return id;
    }

    /// Inicia sesión con el usuario ficticio y devuelve su cookie, sin guardarla en archivos.
    public static async Task<string> EntrarAsync(ServiceProvider proveedor, string nombre,
        string contrasena = Contrasena)
    {
        using var solicitud = new SolicitudIdentidad(proveedor);
        Assert.True((await solicitud.Obtener<IServicioAcceso>()
            .IniciarSesionAsync(nombre, contrasena)).Exito, "No se abrió la sesión ficticia.");
        return solicitud.CookieEmitida();
    }

    /// Crea al Superusuario ficticio y devuelve la cookie de su sesión.
    public static async Task<string> PrepararSuperusuarioAsync(ServiceProvider proveedor)
    {
        await CrearAsync(proveedor, "operador-ficticio", "Superusuario");
        return await EntrarAsync(proveedor, "operador-ficticio");
    }

    /// Comprueba si la cookie aún permite entrar, sin guardar su valor ni cambiar la prueba.
    public static async Task<bool> CookieSigueSirviendoAsync(ServiceProvider proveedor, string cookie)
    {
        using var solicitud = new SolicitudIdentidad(proveedor, cookie);
        return await solicitud.AutenticarAsync();
    }

    /// Comprueba una contraseña con Identity, sin mostrarla ni escribirla en archivos.
    public static async Task<bool> ContrasenaFuncionaAsync(ServiceProvider proveedor, Guid id, string contrasena)
    {
        using var alcance = proveedor.CreateScope();
        var gestor = alcance.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
        var usuario = await gestor.FindByIdAsync(id.ToString());
        Assert.NotNull(usuario);
        return await gestor.CheckPasswordAsync(usuario, contrasena);
    }

    /// Hace fallar el guardado de la auditoría para comprobar que tampoco se guarde el cambio.
    public static Task SimularFalloDeAuditoriaAsync(Escenario escenario) =>
        PostgreSqlRevision.Sql(escenario.Migrador, """
            CREATE FUNCTION rechazar_evento_de_revision() RETURNS trigger LANGUAGE plpgsql AS $$
            BEGIN RAISE EXCEPTION 'Fallo ficticio de auditoria'; END $$;
            CREATE TRIGGER rechazar_evento_de_revision BEFORE INSERT ON "EventoAuditoria"
            FOR EACH ROW EXECUTE FUNCTION rechazar_evento_de_revision();
            """);


    // ──── CONSULTA DE LO GUARDADO ──────────────────────────────────────────────────────────


    /// Lee usuarios, roles y auditoría siempre en el mismo orden, para saber si algo cambió.
    /// No se guarda en archivos porque contiene datos internos.
    public static async Task<string> EstadoAsync(Escenario escenario)
    {
        var partes = new List<string>();
        foreach (var sql in new[]
        {
            """SELECT COALESCE(json_agg(u ORDER BY u."Id")::text,'[]') FROM "AspNetUsers" u""",
            """SELECT COALESCE(json_agg(r ORDER BY r."UserId",r."RoleId")::text,'[]') FROM "AspNetUserRoles" r""",
            """SELECT COALESCE(json_agg(e ORDER BY e."IdEvento")::text,'[]') FROM "EventoAuditoria" e"""
        })
            partes.Add(await PostgreSqlRevision.Valor<string>(escenario.App, sql));
        return string.Join('\n', partes);
    }

    /// Falla si algo cambió en la base de datos.
    /// El mensaje de error no muestra contraseñas ni datos internos.
    public static async Task ComprobarSinCambiosAsync(Escenario escenario, string anterior) =>
        Assert.True(anterior == await EstadoAsync(escenario),
            "La operación rechazada dejó cambios guardados.");

    /// Lee la fila de un usuario para comparar al rechazado en los cambios simultáneos.
    public static Task<string> UsuarioGuardadoAsync(Escenario escenario, Guid id) =>
        PostgreSqlRevision.Valor<string>(escenario.App,
            $"""SELECT row_to_json(u)::text FROM "AspNetUsers" u WHERE "Id"='{id}'""");

    /// Lee los eventos de una acción para comprobar lo que quedó guardado.
    public static async Task<List<EventoAuditoria>> EventosDeAccionAsync(Escenario escenario, AccionAuditoria accion)
    {
        await using var lectura = escenario.Contexto();
        return await lectura.EventosAuditoria.AsNoTracking().Where(e => e.Accion == accion).ToListAsync();
    }

    /// Condición SQL para contar solo cambios de datos, sobre la tabla con el alias «e».
    /// Deja fuera entradas, salidas y bloqueos: estas pruebas inician sesión para prepararse y esos eventos no son lo que miden.
    public const string SoloCambiosSql = """e."Accion" NOT IN ('InicioSesion','CierreSesion','BloqueoAcceso')""";

    /// Lo mismo para consultas de EF: solo los eventos de cambios de datos.
    public static IQueryable<EventoAuditoria> SoloCambios(this IQueryable<EventoAuditoria> eventos) =>
        eventos.Where(e => e.Accion != AccionAuditoria.InicioSesion
            && e.Accion != AccionAuditoria.CierreSesion && e.Accion != AccionAuditoria.BloqueoAcceso);

    /// Lee cuántos intentos fallidos tiene el usuario.
    public static async Task<int> IntentosFallidosAsync(Escenario escenario, Guid id)
    {
        await using var lectura = escenario.Contexto();
        return await lectura.Users.Where(u => u.Id == id).Select(u => u.AccessFailedCount).SingleAsync();
    }

    /// Lee hasta cuándo está bloqueado el usuario; sin bloqueo devuelve null.
    public static async Task<DateTimeOffset?> FinDeBloqueoAsync(Escenario escenario, Guid id)
    {
        await using var lectura = escenario.Contexto();
        return await lectura.Users.Where(u => u.Id == id).Select(u => u.LockoutEnd).SingleAsync();
    }
}


/// <summary>
/// Detiene dos solicitudes hasta que ambas terminen de contar los superusuarios activos.
/// Así las dos ven «hay dos» a la vez, y la prueba comprueba
/// que la base de datos no deja quedarse sin superusuarios.
/// </summary>
public sealed class EsperaSuperusuariosRevision : DbCommandInterceptor
{


    // ──── CONTROL DE LA ESPERA ─────────────────────────────────────────────────────────────


    private readonly TaskCompletionSource _continuar = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _lecturas;

    /// Cuántos conteos se detuvieron; la prueba espera dos.
    public int Lecturas => Volatile.Read(ref _lecturas);


    // ──── CONSULTAS SIMULTÁNEAS ────────────────────────────────────────────────────────────


    /// Detiene solo los dos primeros conteos de superusuarios.
    /// Espera 15 segundos como máximo, para que la prueba no se quede detenida si algo falla.
    public override async ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command,
        CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = default)
    {
        if (command.CommandText.Contains("count(*)", StringComparison.OrdinalIgnoreCase)
            && command.CommandText.Contains("AspNetUserRoles", StringComparison.Ordinal))
        {
            var lectura = Interlocked.Increment(ref _lecturas);
            if (lectura == 2) _continuar.TrySetResult();
            if (lectura <= 2) await _continuar.Task.WaitAsync(TimeSpan.FromSeconds(15), cancellationToken);
        }
        return result;
    }
}
