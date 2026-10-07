using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using TherapEase.Application.Identidad.Interfaces.Repositorios;
using TherapEase.Application.Identidad.Interfaces.Servicios;
using TherapEase.Application.Identidad.Modelos;
using TherapEase.Web.Seguridad;

namespace TherapEase.IntegrationTests;

/// <summary>
/// Pruebas adicionales de Identity: cambios simultáneos sobre el último superusuario, operaciones
/// de administración sin permiso y los comandos locales del operador para crear o
/// restablecer al superusuario. Datos exclusivamente ficticios.
/// </summary>
public class IdentidadComplementariaTests(PostgreSqlRevision postgres) : IClassFixture<PostgreSqlRevision>
{


    // ──── ÚLTIMO SUPERUSUARIO ────────────────────────────────────────────────────────────────


    /// Con dos superusuarios, quitarles el rol a ambos al mismo tiempo (o quitar uno y desactivar
    /// al otro) deja siempre a uno activo: la base rechaza el segundo cambio (TE002).
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Dos_Superusuarios_Cambio_Rol_Concurrente_O_Mixto_Conserva_Uno(bool mixta)
    {

        // Arrange

        var escenario = await postgres.Crear();
        var primero = Guid.NewGuid();
        var segundo = Guid.NewGuid();
        using (var proveedor = escenario.Servicios())
        using (var alcance = proveedor.CreateScope())
        {
            var gestor = alcance.ServiceProvider.GetRequiredService<IGestorIdentidad>();
            Assert.Equal(ResultadoCreacionDeUsuario.Creado, await gestor.CrearAsync(primero, "super-a", "Superusuario", "ficticiacontrasenaconcurrencia", false));
            Assert.Equal(ResultadoCreacionDeUsuario.Creado, await gestor.CrearAsync(segundo, "super-b", "Superusuario", "ficticiacontrasenaconcurrencia", false));
        }
        var sqlPrimero = mixta
            ? $"UPDATE \"AspNetUsers\" SET \"Activo\"=false WHERE \"Id\"='{primero}'"
            : $"DELETE FROM \"AspNetUserRoles\" WHERE \"UserId\"='{primero}'";

        // Act

        var resultados = await ConcurrenciaSqlRevision.EjecutarAsync(escenario, sqlPrimero,
            $"DELETE FROM \"AspNetUserRoles\" WHERE \"UserId\"='{segundo}'");

        // Assert

        Assert.Equal(new[] { "TE002", "confirmada" }, resultados.Order(StringComparer.Ordinal).ToArray());
        using var final = escenario.Servicios();
        using var revision = final.CreateScope();
        Assert.Equal(1, await revision.ServiceProvider.GetRequiredService<IGestorIdentidad>().ContarSuperusuariosActivosAsync());
    }


    // ──── PERMISOS ───────────────────────────────────────────────────────────────────────────


    /// Una Usuaria, o un superusuario que aún tiene contraseña temporal, no puede crear,
    /// restablecer, desactivar ni cambiar roles; no queda ninguna escritura.
    [Theory]
    [InlineData("Usuaria", false)]
    [InlineData("Superusuario", true)]
    public async Task Sin_Permiso_No_Crea_Restablece_Desactiva_Ni_Cambia_Rol(string rol, bool temporal)
    {

        // Arrange

        var escenario = await postgres.Crear();
        using var proveedor = escenario.Servicios();
        var id = Guid.NewGuid();
        using (var alcance = proveedor.CreateScope())
            Assert.Equal(ResultadoCreacionDeUsuario.Creado, await alcance.ServiceProvider.GetRequiredService<IGestorIdentidad>()
                .CrearAsync(id, "actor-ficticio", rol, "ficticiacontrasenapermisos", temporal));
        string cookie;
        using (var entrada = new SolicitudIdentidad(proveedor))
        {
            Assert.True((await entrada.Obtener<IServicioAcceso>().IniciarSesionAsync("actor-ficticio", "ficticiacontrasenapermisos")).Exito);
            cookie = entrada.CookieEmitida();
        }
        using var solicitud = new SolicitudIdentidad(proveedor, cookie);
        Assert.True(await solicitud.AutenticarAsync());

        // Act + Assert: ninguna operación de administración se permite

        var servicio = solicitud.Obtener<IServicioUsuarios>();
        Assert.False((await servicio.CrearUsuarioAsync("intrusa-ficticia", "Usuaria")).Exito);
        Assert.False((await servicio.RestablecerContrasenaAsync(id)).Exito);
        Assert.False((await servicio.DesactivarUsuarioAsync(id)).Exito);
        Assert.False((await servicio.CambiarRolAsync(id, rol == "Usuaria" ? "Superusuario" : "Usuaria")).Exito);

        // Assert: no quedó ninguna escritura

        Assert.Equal(0L, await PostgreSqlRevision.Valor<long>(escenario.App, "SELECT count(*) FROM \"EventoAuditoria\""));
        Assert.Equal(1L, await PostgreSqlRevision.Valor<long>(escenario.App, "SELECT count(*) FROM \"AspNetUsers\""));
    }


    // ──── COMANDOS DEL OPERADOR ──────────────────────────────────────────────────────────────


    /// El comando del operador crea al primer superusuario una sola vez y muestra la temporal
    /// solo en la consola; el de recuperación entrega otra temporal. Ninguna aparece en errores
    /// ni en auditoría, y la nueva obliga a cambiarla al entrar.
    [Fact]
    public async Task Comando_Local_Crea_Una_Vez_Entrega_Temporal_Solo_En_Consola_Y_Restablece()
    {

        // Arrange

        var escenario = await postgres.Crear();

        // Ejecuta la aplicación compilada como lo haría el operador en su máquina.

        async Task<(int Codigo, string Salida, string Error)> EjecutarAsync(string comando)
        {
            var configuracion = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
            var dll = Path.Combine(ServidorRevision.RaizRepositorio(), "src", "Web", "bin", configuracion, "net10.0", "TherapEase.Web.dll");
            var inicio = new ProcessStartInfo("dotnet")
            {
                UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true,
                CreateNoWindow = true, WorkingDirectory = Path.GetDirectoryName(dll)!
            };
            inicio.ArgumentList.Add(dll);
            inicio.ArgumentList.Add(comando);
            inicio.ArgumentList.Add("operador-ficticio");
            inicio.Environment["ConnectionStrings__TherapEase"] = escenario.App;
            using var proceso = Process.Start(inicio)!;
            var salida = proceso.StandardOutput.ReadToEndAsync();
            var error = proceso.StandardError.ReadToEndAsync();
            try { await proceso.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(30)); }
            finally { if (!proceso.HasExited) proceso.Kill(entireProcessTree: true); }
            return (proceso.ExitCode, await salida, await error);
        }

        // Toma la contraseña temporal de la línea que imprime el comando.

        static string Temporal(string salida) => salida.Split('\n')
            .Single(linea => linea.StartsWith("Contraseña temporal: ", StringComparison.Ordinal))
            ["Contraseña temporal: ".Length..].Trim();

        // Act: crear el primer superusuario

        var alta = await EjecutarAsync("--crear-superusuario");

        // Assert

        Assert.True(alta.Codigo == 0, "El comando local de alta falló.");
        var primeraClave = Temporal(alta.Salida);
        Assert.True(primeraClave.Length >= 12);
        Assert.DoesNotContain(primeraClave, alta.Error, StringComparison.Ordinal);

        // Act: intentar crearlo otra vez

        var repetida = await EjecutarAsync("--crear-superusuario");

        // Assert

        Assert.Equal(1, repetida.Codigo);
        Assert.DoesNotContain("Contraseña temporal:", repetida.Salida, StringComparison.Ordinal);
        Assert.Equal(1L, await PostgreSqlRevision.Valor<long>(escenario.App, "SELECT count(*) FROM \"AspNetUsers\""));

        // Act: restablecer al superusuario

        var restablecida = await EjecutarAsync("--restablecer-superusuario");

        // Assert

        Assert.True(restablecida.Codigo == 0, "El comando local de recuperación falló.");
        var segundaClave = Temporal(restablecida.Salida);
        Assert.NotEqual(primeraClave, segundaClave);
        var auditoria = await PostgreSqlRevision.Valor<string>(escenario.App, "SELECT json_agg(e)::text FROM \"EventoAuditoria\" e");
        foreach (var clave in new[] { primeraClave, segundaClave })
        {
            Assert.DoesNotContain(clave, auditoria, StringComparison.Ordinal);
            Assert.DoesNotContain(clave, restablecida.Error, StringComparison.Ordinal);
        }

        // Act + Assert: solo la nueva temporal entra y obliga a cambiarla

        using var proveedor = escenario.Servicios();
        using var solicitud = new SolicitudIdentidad(proveedor);
        Assert.False((await solicitud.Obtener<IServicioAcceso>().IniciarSesionAsync("operador-ficticio", primeraClave)).Exito);
        var acceso = await solicitud.Obtener<IServicioAcceso>().IniciarSesionAsync("operador-ficticio", segundaClave);
        Assert.True(acceso.Exito);
        Assert.True(acceso.Datos!.DebeCambiarContrasena);
        Assert.Equal(2L, await PostgreSqlRevision.Valor<long>(escenario.App, "SELECT count(*) FROM \"EventoAuditoria\""));
    }
}
