using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using TherapEase.Application.Identidad.Interfaces.Repositorios;
using TherapEase.Application.Identidad.Interfaces.Servicios;
using TherapEase.Application.Identidad.Modelos;

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

    /// Crea un usuario ficticio antes de la prueba.
    /// Se guarda directo, sin dejar registro en la auditoría.
    public static async Task<Guid> CrearAsync(ServiceProvider proveedor, string nombre,
        string rol = "Usuaria", bool temporal = false)
    {
        using var alcance = proveedor.CreateScope();
        var id = Guid.NewGuid();
        var resultado = await alcance.ServiceProvider.GetRequiredService<IGestorIdentidad>()
            .CrearAsync(id, nombre, rol, Contrasena, temporal);
        Assert.Equal(ResultadoCreacionDeUsuario.Creado, resultado);
        return id;
    }

    /// Inicia sesión con el usuario ficticio y devuelve su cookie.
    /// La cookie no se guarda en los resultados de la prueba.
    public static async Task<string> EntrarAsync(ServiceProvider proveedor, string nombre,
        string contrasena = Contrasena)
    {
        using var solicitud = new SolicitudIdentidad(proveedor);
        Assert.True((await solicitud.Obtener<IServicioAcceso>()
            .IniciarSesionAsync(nombre, contrasena)).Exito, "No se abrió la sesión ficticia.");
        return solicitud.CookieEmitida();
    }


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
        Assert.True(anterior == await EstadoAsync(escenario), "La operación rechazada dejó cambios guardados.");
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
