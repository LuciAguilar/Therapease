using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using TherapEase.Application.Identidad.Interfaces.Repositorios;
using TherapEase.Application.Identidad.Interfaces.Servicios;
using TherapEase.Application.Identidad.Modelos;

namespace TherapEase.IntegrationTests;

/// <summary>
/// Prepara usuarios ficticios y solicitudes para ampliar las pruebas del servidor de acceso.
/// Consulta lo guardado por otra conexión, sin crear páginas ni cambiar servicios del sistema.
/// </summary>
public static class ServidorIdentidadB01Revision
{


    // ──── USUARIOS Y SOLICITUDES ───────────────────────────────────────────────────────────


    public const string Contrasena = "contrasenaficticiaservidorb01";
    public const string NuevaContrasena = "nuevacontrasenaficticiaservidorb01";

    /// Prepara una cuenta antes de probar la operación; esta preparación no genera eventos.
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

    /// Obtiene una sesión real para la cuenta ficticia, sin guardar su cookie en la evidencia.
    public static async Task<string> EntrarAsync(ServiceProvider proveedor, string nombre,
        string contrasena = Contrasena)
    {
        using var solicitud = new SolicitudIdentidad(proveedor);
        Assert.True((await solicitud.Obtener<IServicioAcceso>()
            .IniciarSesionAsync(nombre, contrasena)).Exito, "No se abrió la sesión ficticia.");
        return solicitud.CookieEmitida();
    }



    // ──── CONSULTA DE LO GUARDADO ──────────────────────────────────────────────────────────


    /// Lee usuarios, roles y eventos en orden estable para detectar cualquier escritura.
    /// El resultado solo se compara en memoria: contiene datos internos que no deben publicarse.
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

    /// Informa solo si hubo cambios; un fallo no imprime contraseñas ni datos internos.
    public static async Task ComprobarSinCambiosAsync(Escenario escenario, string anterior) =>
        Assert.True(anterior == await EstadoAsync(escenario), "La operación rechazada dejó cambios guardados.");
}


/// <summary>
/// Hace que dos solicitudes terminen de contar los superusuarios antes de modificar uno.
/// Así la prueba obliga a que ambas vean dos activos y que la base proteja al último.
/// </summary>
public sealed class EsperaSuperusuariosB01Revision : DbCommandInterceptor
{


    // ──── CONTROL DE LA ESPERA ─────────────────────────────────────────────────────────────


    private readonly TaskCompletionSource _continuar = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _lecturas;

    public int Lecturas => Volatile.Read(ref _lecturas);


    // ──── CONSULTAS SIMULTÁNEAS ────────────────────────────────────────────────────────────


    /// Espera solo las primeras dos consultas del recuento; el límite evita una prueba colgada.
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
