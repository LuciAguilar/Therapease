using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using Xunit.Sdk;
using Cookie = Microsoft.Playwright.Cookie;

namespace TherapEase.E2ETests;

/// <summary>
/// Comprueba que solo una cuenta autorizada pueda administrar usuarios o consultar auditoría.
/// También protege al último Superusuario activo frente a un cambio de rol o desactivación.
/// </summary>
public sealed partial class AccesoSesionesYPermisosTests
{


    // ──── PERMISOS ───────────────────────────────────────────────────────────


    /// Una Usuaria no puede consultar cuentas ajenas ni auditoría, aunque escriba la dirección directamente.
    [Theory]
    [InlineData("/Usuarios")]
    [InlineData("/Usuarios/Crear")]
    [InlineData("/Usuarios/Detalle/00000000-0000-0000-0000-000000000001")]
    [InlineData("/api/auditoria/eventos")]
    public Task UsuariaNoObtieneFuncionesAdministrativas(string ruta) => Verificar(async () =>
    {

        // Arrange
        var p = await Pagina();

        // Act
        await Entrar(p, await entorno.CrearCuentaAsync());
        var r = await p.APIRequest.GetAsync(entorno.Url + ruta);

        // Assert
        Exigir(r.Status == 403);
    });

    /// Un envío directo de una Usuaria no debe crear una cuenta ni conceder el rol de Superusuario.
    [Fact]
    public Task PostDeUsuariaNoCreaCuentaNiElevaPermisos() => Verificar(async () =>
    {

        // Arrange
        var p = await Pagina();

        // Act
        await Entrar(p, await entorno.CrearCuentaAsync());
        await Abrir(p, "/Cuenta/Contrasena");
        var nombre = "qa-" + Guid.NewGuid().ToString("N");
        var r = await Enviar(p, "/Usuarios/Crear", new() { ["Entrada.NombreUsuario"] = nombre, ["Entrada.NombreCompleto"] = "Cuenta Ficticia", ["Entrada.Rol"] = "Superusuario" });

        // Assert
        Exigir(r.Status == 403 || (r.Status == 302 && r.Headers["location"].Contains("/Acceso/Denegado")));
        await using var db = entorno.Servidor.Escenario.Contexto();
        Exigir(!await db.Users.AnyAsync(v => v.UserName == nombre));
    });

    /// Si solo queda un Superusuario activo, cambiar su rol o desactivarlo debe rechazarse y conservar su acceso.
    [Fact]
    public Task UltimoSuperusuarioNoPuedeDegradarseNiDesactivarse() => Verificar(async () =>
    {

        // Arrange
        var cuenta = await entorno.CrearCuentaAsync("Superusuario");
        var p = await Pagina();

        // Act
        await Entrar(p, cuenta);
        await using var db = entorno.Servidor.Escenario.Contexto();
        var otros = await (
            from ur in db.UserRoles
            join rol in db.Roles on ur.RoleId equals rol.Id
            join usuario in db.Users on ur.UserId equals usuario.Id
            where rol.Name == "Superusuario" && usuario.Activo && ur.UserId != cuenta.Id
            select ur.UserId).ToListAsync();
        foreach (var id in otros)
        {
            await Abrir(p, "/Usuarios/Detalle/" + id);
            var r = await Enviar(p, "/Usuarios/Detalle/" + id + "?handler=Rol", new() { ["rol"] = "Usuaria", ["confirmar"] = "true" });

            // Assert
            Exigir(r.Status == 302);
        }

        foreach (var accion in new[] { "Rol", "Desactivar" })
        {

            // Act
            await Abrir(p, "/Usuarios/Detalle/" + cuenta.Id);
            var r = await Enviar(p, "/Usuarios/Detalle/" + cuenta.Id + "?handler=" + accion, new() { ["rol"] = "Usuaria", ["confirmar"] = "true" });

            // Assert
            Exigir(r.Status == 200 && WebUtility.HtmlDecode(await r.TextAsync()).Contains("Superusuario"));

            // Act
            await Abrir(p, "/Usuarios");

            // Assert
            Exigir(Ruta(p) == "/Usuarios");
        }

        Exigir(await (
            from ur in db.UserRoles
            join rol in db.Roles on ur.RoleId equals rol.Id
            join usuario in db.Users on ur.UserId equals usuario.Id
            where rol.Name == "Superusuario" && usuario.Activo
            select ur.UserId).CountAsync() == 1);
    });

}
