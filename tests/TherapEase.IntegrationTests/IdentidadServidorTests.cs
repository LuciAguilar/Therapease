using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using TherapEase.Application.Identidad.Interfaces.Repositorios;
using TherapEase.Application.Identidad.Interfaces.Servicios;
using TherapEase.Application.Identidad.Modelos;
using TherapEase.Infrastructure.Data;
using TherapEase.Infrastructure.Identidad.Entidades;
using TherapEase.Web.Seguridad;

namespace TherapEase.IntegrationTests;

/// <summary>
/// Pruebas de Identity con los servicios reales y un PostgreSQL desechable: contraseñas,
/// acceso, bloqueo, contraseña temporal, cambio y restablecimiento, salida, roles,
/// desactivación, cookies de sesión y atomicidad con la auditoría.
/// Todas las cuentas y contraseñas son ficticias.
/// </summary>
public class IdentidadServidorTests(PostgreSqlRevision postgres) : IClassFixture<PostgreSqlRevision>
{


    // ──── DATOS Y AYUDAS ─────────────────────────────────────────────────────────────────────

    // Contraseñas ficticias fáciles de reconocer: si aparecen en auditoría o registros, la prueba lo detecta.

    private const string Contrasena = "datosensibleficticioidentidad";
    private const string Nueva = "nuevodatosensibleficticioclave";


    /// Crea un usuario ficticio directamente con el gestor de Identity y devuelve su identificador.
    private static async Task<Guid> CrearAsync(ServiceProvider proveedor, string nombre = "usuaria-ficticia",
        string rol = "Usuaria", bool temporal = false, string contrasena = Contrasena)
    {
        using var alcance = proveedor.CreateScope();
        var id = Guid.NewGuid();
        var resultado = await alcance.ServiceProvider.GetRequiredService<IGestorIdentidad>()
            .CrearAsync(id, nombre, rol, contrasena, temporal);
        Assert.Equal(ResultadoCreacionDeUsuario.Creado, resultado);
        return id;
    }


    /// Inicia sesión con el servicio real de acceso y devuelve la cookie emitida.
    private static async Task<string> EntrarAsync(ServiceProvider proveedor, string nombre = "usuaria-ficticia", string contrasena = Contrasena)
    {
        using var solicitud = new SolicitudIdentidad(proveedor);
        var resultado = await solicitud.Obtener<IServicioAcceso>().IniciarSesionAsync(nombre, contrasena);
        Assert.True(resultado.Exito, "No se pudo abrir la sesión ficticia.");
        return solicitud.CookieEmitida();
    }


    /// Indica si una cookie sigue siendo una sesión válida.
    private static async Task<bool> AutenticarAsync(ServiceProvider proveedor, string cookie)
    {
        using var solicitud = new SolicitudIdentidad(proveedor, cookie);
        return await solicitud.AutenticarAsync();
    }


    // ──── CONTRASEÑAS Y ACCESO ───────────────────────────────────────────────────────────────


    /// Con 11 caracteres se rechaza y con 12 se acepta, sin exigir números ni símbolos. La
    /// contraseña se guarda como hash de Identity (no en claro ni como SHA-256) y el nombre
    /// de usuario no se repite aunque cambien mayúsculas.
    [Fact]
    public async Task Contrasena_Longitud_Sin_Composicion_Hash_Identity_Y_Nombre_Unico()
    {

        // Arrange

        var escenario = await postgres.Crear();
        using var proveedor = escenario.Servicios();
        using var alcance = proveedor.CreateScope();
        var gestor = alcance.ServiceProvider.GetRequiredService<IGestorIdentidad>();

        // Act + Assert: 11 caracteres se rechazan

        Assert.Equal(ResultadoCreacionDeUsuario.DatosInvalidos,
            await gestor.CrearAsync(Guid.NewGuid(), "corta-ficticia", "Usuaria", "abcdefghijk", false));

        // Act: 12 caracteres sin números ni símbolos se aceptan

        var id = await CrearAsync(proveedor, contrasena: "abcdefghijkl");

        // Act + Assert: el mismo nombre con mayúsculas se rechaza

        Assert.Equal(ResultadoCreacionDeUsuario.NombreDuplicado,
            await gestor.CrearAsync(Guid.NewGuid(), "USUARIA-FICTICIA", "Usuaria", Contrasena, false));

        // Assert: se guardó un hash de Identity, no la contraseña

        var usuario = await alcance.ServiceProvider.GetRequiredService<UserManager<Usuario>>().FindByIdAsync(id.ToString());
        Assert.NotNull(usuario?.PasswordHash);
        Assert.NotEqual("abcdefghijkl", usuario.PasswordHash);
        Assert.NotEqual(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("abcdefghijkl"))), usuario.PasswordHash);
        var hasher = alcance.ServiceProvider.GetRequiredService<IPasswordHasher<Usuario>>();
        Assert.NotEqual(PasswordVerificationResult.Failed, hasher.VerifyHashedPassword(usuario, usuario.PasswordHash, "abcdefghijkl"));
        Assert.Equal(PasswordVerificationResult.Failed, hasher.VerifyHashedPassword(usuario, usuario.PasswordHash, "incorrecta-ficticia"));
        Assert.Equal(1L, await PostgreSqlRevision.Valor<long>(escenario.App, "SELECT count(*) FROM \"AspNetUsers\""));
    }


    /// Usuario inexistente, contraseña incorrecta, cuenta inactiva o bloqueada reciben el mismo
    /// mensaje y ninguna cookie, para no revelar qué cuentas existen.
    [Fact]
    public async Task Acceso_Errores_Indistinguibles_Y_Sin_Cookie()
    {

        // Arrange

        var escenario = await postgres.Crear();
        using var proveedor = escenario.Servicios();
        await CrearAsync(proveedor);
        var inactivo = await CrearAsync(proveedor, "inactiva-ficticia");
        await PostgreSqlRevision.Sql(escenario.App, $"UPDATE \"AspNetUsers\" SET \"Activo\"=false WHERE \"Id\"='{inactivo}'");
        var mensajes = new List<string>();

        // Act + Assert: inexistente, incorrecta e inactiva fallan sin cookie

        foreach (var nombre in new[] { "inexistente-ficticia", "usuaria-ficticia", "inactiva-ficticia" })
        {
            using var solicitud = new SolicitudIdentidad(proveedor);
            var respuesta = await solicitud.Obtener<IServicioAcceso>().IniciarSesionAsync(nombre, "incorrecta-ficticia");
            Assert.False(respuesta.Exito);
            Assert.Null(respuesta.Datos);
            Assert.Equal(0, solicitud.Contexto.Response.Headers.SetCookie.Count);
            mensajes.Add(respuesta.Mensaje);
        }

        // Act: completar cinco fallos para bloquear la cuenta

        for (var intento = 0; intento < 4; intento++)
        {
            using var solicitud = new SolicitudIdentidad(proveedor);
            await solicitud.Obtener<IServicioAcceso>().IniciarSesionAsync("usuaria-ficticia", "incorrecta-ficticia");
        }

        // Act + Assert: la correcta, ya bloqueada, también falla

        using (var solicitud = new SolicitudIdentidad(proveedor))
        {
            var respuesta = await solicitud.Obtener<IServicioAcceso>().IniciarSesionAsync("usuaria-ficticia", Contrasena);
            Assert.False(respuesta.Exito);
            mensajes.Add(respuesta.Mensaje);
            Assert.Equal(0, solicitud.Contexto.Response.Headers.SetCookie.Count);
        }

        // Assert: todos los mensajes son iguales

        Assert.Single(mensajes.Distinct());
    }


    /// Cinco fallos bloquean la cuenta 15 minutos; durante el bloqueo ni la contraseña correcta
    /// entra, y al vencer el bloqueo vuelve a admitirse.
    [Fact]
    public async Task Bloqueo_Dura_Quince_Minutos_Y_Al_Expirar_Admite_Contrasena_Correcta()
    {

        // Arrange

        var escenario = await postgres.Crear();
        using var proveedor = escenario.Servicios();
        var id = await CrearAsync(proveedor);
        var antes = DateTimeOffset.UtcNow;

        // Act: cinco intentos fallidos

        for (var intento = 0; intento < 5; intento++)
        {
            using var solicitud = new SolicitudIdentidad(proveedor);
            Assert.False((await solicitud.Obtener<IServicioAcceso>().IniciarSesionAsync("usuaria-ficticia", "fallo-ficticio")).Exito);
        }

        // Assert: bloqueo de 15 minutos

        using (var alcance = proveedor.CreateScope())
        {
            var usuario = await alcance.ServiceProvider.GetRequiredService<UserManager<Usuario>>().FindByIdAsync(id.ToString());
            Assert.InRange(usuario!.LockoutEnd!.Value, antes.AddMinutes(15), DateTimeOffset.UtcNow.AddMinutes(15));
        }

        // Act + Assert: la correcta no entra durante el bloqueo

        using (var solicitud = new SolicitudIdentidad(proveedor))
            Assert.False((await solicitud.Obtener<IServicioAcceso>().IniciarSesionAsync("usuaria-ficticia", Contrasena)).Exito);

        // Act: vencer el bloqueo
        // Se coloca el vencimiento en el pasado; no se espera quince minutos ni se cambia el reloj de producción.

        await PostgreSqlRevision.Sql(escenario.App, $"UPDATE \"AspNetUsers\" SET \"LockoutEnd\"=CURRENT_TIMESTAMP-INTERVAL '1 second' WHERE \"Id\"='{id}'");

        // Assert: vuelve a entrar y se reinician los fallos

        Assert.True(await AutenticarAsync(proveedor, await EntrarAsync(proveedor)));
        Assert.Equal(0, await PostgreSqlRevision.Valor<int>(escenario.App, "SELECT \"AccessFailedCount\" FROM \"AspNetUsers\""));
    }


    // ──── CAMBIO, SALIDA Y RESTABLECIMIENTO ──────────────────────────────────────────────────


    /// Con contraseña temporal solo se puede cambiar la propia contraseña; ninguna otra operación.
    /// Al cambiarla, la sesión vieja deja de valer y se entrega una nueva.
    [Fact]
    public async Task Temporal_Solo_Permite_Cambio_Propio_Y_No_Operaciones()
    {

        // Arrange

        var escenario = await postgres.Crear();
        using var proveedor = escenario.Servicios();
        var id = await CrearAsync(proveedor, rol: "Superusuario", temporal: true);
        var cookie = await EntrarAsync(proveedor);
        using var solicitud = new SolicitudIdentidad(proveedor, cookie);
        Assert.True(await solicitud.AutenticarAsync());

        // Act + Assert: con temporal solo se permite el cambio propio

        var autorizacion = solicitud.Obtener<IAuthorizationService>();
        Assert.True((await autorizacion.AuthorizeAsync(solicitud.Contexto.User, PoliticasDeAutorizacion.CambiarContrasenaPropia)).Succeeded);
        Assert.False((await autorizacion.AuthorizeAsync(solicitud.Contexto.User, PoliticasDeAutorizacion.AccesoOperativo)).Succeeded);
        Assert.False((await autorizacion.AuthorizeAsync(solicitud.Contexto.User, PoliticasDeAutorizacion.AdministrarUsuarios)).Succeeded);
        Assert.False((await solicitud.Obtener<IServicioUsuarios>().CrearUsuarioAsync("no-permitida", "Usuaria")).Exito);

        // Act: cambiar la contraseña

        var resultado = await solicitud.Obtener<IServicioContrasena>().CambiarContrasenaPropiaAsync(null, Nueva);

        // Assert: ya no es temporal y solo vale la sesión nueva

        Assert.True(resultado.Exito);
        Assert.False(resultado.Datos!.DebeCambiarContrasena);
        var cookieNueva = solicitud.CookieEmitida();
        Assert.False(await AutenticarAsync(proveedor, cookie));
        Assert.True(await AutenticarAsync(proveedor, cookieNueva));
    }


    /// Cambiar la contraseña propia cierra las otras sesiones abiertas y solo conserva la actual.
    [Fact]
    public async Task Cambio_Propio_Revoca_Dos_Sesiones_Y_Conserva_Solo_La_Refrescada()
    {

        // Arrange

        var escenario = await postgres.Crear();
        using var proveedor = escenario.Servicios();
        await CrearAsync(proveedor);
        var primera = await EntrarAsync(proveedor);
        var segunda = await EntrarAsync(proveedor);
        string nuevaCookie;

        // Act

        using (var solicitud = new SolicitudIdentidad(proveedor, primera))
        {
            Assert.True(await solicitud.AutenticarAsync());
            Assert.True((await solicitud.Obtener<IServicioContrasena>().CambiarContrasenaPropiaAsync(Contrasena, Nueva)).Exito);
            nuevaCookie = solicitud.CookieEmitida();
        }

        // Assert

        Assert.False(await AutenticarAsync(proveedor, primera));
        Assert.False(await AutenticarAsync(proveedor, segunda));
        Assert.True(await AutenticarAsync(proveedor, nuevaCookie));
        using var acceso = new SolicitudIdentidad(proveedor);
        Assert.False((await acceso.Obtener<IServicioAcceso>().IniciarSesionAsync("usuaria-ficticia", Contrasena)).Exito);
        Assert.True((await acceso.Obtener<IServicioAcceso>().IniciarSesionAsync("usuaria-ficticia", Nueva)).Exito);
    }


    /// Si la contraseña actual es incorrecta o la nueva es muy corta, no cambia nada: ni la clave,
    /// ni el sello de seguridad, ni se registra auditoría.
    [Theory]
    [InlineData("incorrecta-ficticia", Nueva)]
    [InlineData(Contrasena, "corta")]
    public async Task Cambio_Invalido_No_Modifica_Clave_Sello_Ni_Auditoria(string actual, string nueva)
    {

        // Arrange

        var escenario = await postgres.Crear();
        using var proveedor = escenario.Servicios();
        await CrearAsync(proveedor);
        var cookie = await EntrarAsync(proveedor);
        var sello = await PostgreSqlRevision.Valor<string>(escenario.App, "SELECT \"SecurityStamp\" FROM \"AspNetUsers\"");

        // Act

        using (var solicitud = new SolicitudIdentidad(proveedor, cookie))
        {
            Assert.True(await solicitud.AutenticarAsync());
            Assert.False((await solicitud.Obtener<IServicioContrasena>().CambiarContrasenaPropiaAsync(actual, nueva)).Exito);
        }

        // Assert

        Assert.Equal(sello, await PostgreSqlRevision.Valor<string>(escenario.App, "SELECT \"SecurityStamp\" FROM \"AspNetUsers\""));
        Assert.Equal(0L, await PostgreSqlRevision.Valor<long>(escenario.App, "SELECT count(*) FROM \"EventoAuditoria\""));
        Assert.True(await AutenticarAsync(proveedor, cookie));
        await EntrarAsync(proveedor);
    }


    /// Salir cierra todas las sesiones del usuario y borra la cookie; salir de nuevo sin sesión no falla.
    [Fact]
    public async Task Salida_Revoca_Ambas_Sesiones_Y_Admite_Repeticion_Sin_Sesion()
    {

        // Arrange

        var escenario = await postgres.Crear();
        using var proveedor = escenario.Servicios();
        await CrearAsync(proveedor);
        var primera = await EntrarAsync(proveedor);
        var segunda = await EntrarAsync(proveedor);

        // Act

        using (var solicitud = new SolicitudIdentidad(proveedor, primera))
        {
            Assert.True(await solicitud.AutenticarAsync());
            await solicitud.Obtener<IServicioAcceso>().CerrarSesionAsync();
            Assert.Contains(solicitud.Contexto.Response.Headers.SetCookie, valor => valor!.StartsWith("TherapEase.Sesion=;", StringComparison.Ordinal));
        }

        // Assert

        Assert.False(await AutenticarAsync(proveedor, primera));
        Assert.False(await AutenticarAsync(proveedor, segunda));

        // Act + Assert: salir otra vez sin sesión no falla

        using var repetida = new SolicitudIdentidad(proveedor);
        await repetida.Obtener<IServicioAcceso>().CerrarSesionAsync();
    }


    /// El superusuario restablece la contraseña: se entrega una temporal y se cierra la sesión
    /// anterior, pero el bloqueo vigente se conserva (decisión de Lucía del 04-oct).
    [Fact]
    public async Task Restablecimiento_Conserva_Bloqueo_Revoca_Cookie_Y_Entrega_Temporal()
    {

        // Arrange: cuenta bloqueada con una sesión abierta

        var escenario = await postgres.Crear();
        using var proveedor = escenario.Servicios();
        await CrearAsync(proveedor, "actor-ficticio", "Superusuario");
        var id = await CrearAsync(proveedor);
        var actorCookie = await EntrarAsync(proveedor, "actor-ficticio");
        var cookieAnterior = await EntrarAsync(proveedor);
        for (var intento = 0; intento < 5; intento++)
        {
            using var fallo = new SolicitudIdentidad(proveedor);
            await fallo.Obtener<IServicioAcceso>().IniciarSesionAsync("usuaria-ficticia", "fallo-ficticio");
        }
        var bloqueo = await PostgreSqlRevision.Valor<DateTime>(escenario.App, $"SELECT \"LockoutEnd\" FROM \"AspNetUsers\" WHERE \"Id\"='{id}'");

        // Act: el superusuario restablece la contraseña

        string temporal;
        using (var solicitud = new SolicitudIdentidad(proveedor, actorCookie))
        {
            Assert.True(await solicitud.AutenticarAsync());
            var respuesta = await solicitud.Obtener<IServicioUsuarios>().RestablecerContrasenaAsync(id);
            Assert.True(respuesta.Exito);
            temporal = respuesta.Datos!.ContrasenaTemporal;
            Assert.True(respuesta.Datos.Usuario.DebeCambiarContrasena);
        }

        // Assert: el bloqueo sigue y la sesión anterior ya no vale

        Assert.Equal(bloqueo, await PostgreSqlRevision.Valor<DateTime>(escenario.App, $"SELECT \"LockoutEnd\" FROM \"AspNetUsers\" WHERE \"Id\"='{id}'"));
        Assert.False(await AutenticarAsync(proveedor, cookieAnterior));
        using (var solicitud = new SolicitudIdentidad(proveedor))
            Assert.False((await solicitud.Obtener<IServicioAcceso>().IniciarSesionAsync("usuaria-ficticia", temporal)).Exito);

        // Act: vencer el bloqueo

        await PostgreSqlRevision.Sql(escenario.App, $"UPDATE \"AspNetUsers\" SET \"LockoutEnd\"=CURRENT_TIMESTAMP-INTERVAL '1 second' WHERE \"Id\"='{id}'");

        // Assert: solo la temporal entra y obliga a cambiarla

        using (var solicitud = new SolicitudIdentidad(proveedor))
        {
            Assert.False((await solicitud.Obtener<IServicioAcceso>().IniciarSesionAsync("usuaria-ficticia", Contrasena)).Exito);
            Assert.True((await solicitud.Obtener<IServicioAcceso>().IniciarSesionAsync("usuaria-ficticia", temporal)).Datos!.DebeCambiarContrasena);
        }
    }


    // ──── ROLES Y DESACTIVACIÓN ──────────────────────────────────────────────────────────────


    /// Al cambiar el rol se invalida la sesión anterior; la nueva sesión ya usa solo los permisos
    /// del rol nuevo, al subir o al bajar de privilegio.
    [Theory]
    [InlineData("Usuaria", "Superusuario")]
    [InlineData("Superusuario", "Usuaria")]
    public async Task Cambio_Rol_Revoca_Cookie_Anterior_Y_Nueva_Sesion_Usa_Permiso_Actual(string inicial, string nuevo)
    {

        // Arrange

        var escenario = await postgres.Crear();
        using var proveedor = escenario.Servicios();
        await CrearAsync(proveedor, "actor-ficticio", "Superusuario");
        var id = await CrearAsync(proveedor, rol: inicial);
        var actorCookie = await EntrarAsync(proveedor, "actor-ficticio");
        var anterior = await EntrarAsync(proveedor);

        // Act

        using (var solicitud = new SolicitudIdentidad(proveedor, actorCookie))
        {
            Assert.True(await solicitud.AutenticarAsync());
            Assert.True((await solicitud.Obtener<IServicioUsuarios>().CambiarRolAsync(id, nuevo)).Exito);
        }

        // Assert

        Assert.False(await AutenticarAsync(proveedor, anterior));
        var actual = await EntrarAsync(proveedor);
        using var renovada = new SolicitudIdentidad(proveedor, actual);
        Assert.True(await renovada.AutenticarAsync());
        Assert.True(renovada.Contexto.User.IsInRole(nuevo));
        Assert.Equal(nuevo == "Superusuario", (await renovada.Obtener<IAuthorizationService>()
            .AuthorizeAsync(renovada.Contexto.User, PoliticasDeAutorizacion.AdministrarUsuarios)).Succeeded);
    }


    /// Desactivar a un usuario invalida su sesión y le impide volver a entrar.
    [Fact]
    public async Task Desactivacion_Revoca_Cookie_Y_No_Admite_Acceso()
    {

        // Arrange

        var escenario = await postgres.Crear();
        using var proveedor = escenario.Servicios();
        await CrearAsync(proveedor, "actor-ficticio", "Superusuario");
        var id = await CrearAsync(proveedor);
        var anterior = await EntrarAsync(proveedor);
        var actor = await EntrarAsync(proveedor, "actor-ficticio");

        // Act

        using (var solicitud = new SolicitudIdentidad(proveedor, actor))
        {
            Assert.True(await solicitud.AutenticarAsync());
            Assert.True((await solicitud.Obtener<IServicioUsuarios>().DesactivarUsuarioAsync(id)).Exito);
        }

        // Assert

        Assert.False(await AutenticarAsync(proveedor, anterior));
        using var acceso = new SolicitudIdentidad(proveedor);
        Assert.False((await acceso.Obtener<IServicioAcceso>().IniciarSesionAsync("usuaria-ficticia", Contrasena)).Exito);
    }


    /// El servicio no deja desactivar ni quitar el rol al único superusuario, y no escribe nada.
    [Fact]
    public async Task Ultimo_Superusuario_Servicio_Rechaza_Baja_Y_Cambio_Rol_Sin_Escribir()
    {

        // Arrange

        var escenario = await postgres.Crear();
        using var proveedor = escenario.Servicios();
        var id = await CrearAsync(proveedor, rol: "Superusuario");
        var cookie = await EntrarAsync(proveedor);
        using var solicitud = new SolicitudIdentidad(proveedor, cookie);
        Assert.True(await solicitud.AutenticarAsync());

        // Act + Assert: desactivar o quitar el rol se rechaza

        Assert.False((await solicitud.Obtener<IServicioUsuarios>().DesactivarUsuarioAsync(id)).Exito);
        Assert.False((await solicitud.Obtener<IServicioUsuarios>().CambiarRolAsync(id, "Usuaria")).Exito);

        // Assert: sigue un superusuario y no hay auditoría

        Assert.Equal(1, await solicitud.Obtener<IGestorIdentidad>().ContarSuperusuariosActivosAsync());
        Assert.Equal(0L, await PostgreSqlRevision.Valor<long>(escenario.App, "SELECT count(*) FROM \"EventoAuditoria\""));
    }


    // ──── COOKIES Y SESIÓN ───────────────────────────────────────────────────────────────────


    /// La cookie sale con HttpOnly, Secure y SameSite=Lax. Con el reloj de prueba, la sesión
    /// sigue válida a los 29 minutos y caduca a los 31 (30 minutos sin actividad).
    [Fact]
    public async Task Sesion_Configura_Cookie_Segura_Y_Cookie_Caduca_Tras_Treinta_Minutos()
    {

        // Arrange

        var escenario = await postgres.Crear();
        var reloj = new RelojRevision();
        using var proveedor = escenario.Servicios(reloj.Configurar);
        await CrearAsync(proveedor);
        string cookie;

        // Act + Assert: la cookie sale con atributos seguros

        using (var solicitud = new SolicitudIdentidad(proveedor))
        {
            Assert.True((await solicitud.Obtener<IServicioAcceso>().IniciarSesionAsync("usuaria-ficticia", Contrasena)).Exito);
            var cabecera = solicitud.Contexto.Response.Headers.SetCookie.Single(valor => valor!.StartsWith("TherapEase.Sesion=", StringComparison.Ordinal))!;
            Assert.Contains("httponly", cabecera, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("secure", cabecera, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("samesite=lax", cabecera, StringComparison.OrdinalIgnoreCase);
            var opciones = solicitud.Obtener<IOptionsMonitor<CookieAuthenticationOptions>>().Get(IdentityConstants.ApplicationScheme);
            Assert.Equal(TimeSpan.FromMinutes(30), opciones.ExpireTimeSpan);
            Assert.True(opciones.SlidingExpiration);
            cookie = solicitud.CookieEmitida();
        }

        // Act: pasan 29 minutos

        reloj.Avanzar(TimeSpan.FromMinutes(29));

        // Assert: la sesión sigue válida

        Assert.True(await AutenticarAsync(proveedor, cookie));

        // Act: pasan 31 minutos

        reloj.Avanzar(TimeSpan.FromMinutes(2));

        // Assert: la sesión caducó
        // Se reutiliza la cookie original: no se simula que el navegador reciba una renovada.

        Assert.False(await AutenticarAsync(proveedor, cookie));
    }


    /// Sin cookie, o con una cookie alterada en un solo carácter, no se autentica a nadie.
    [Fact]
    public async Task Cookie_Ausente_O_Manipulada_No_Autentica()
    {

        // Arrange

        var escenario = await postgres.Crear();
        using var proveedor = escenario.Servicios();
        await CrearAsync(proveedor);

        // Act + Assert: sin cookie no se autentica

        using (var solicitud = new SolicitudIdentidad(proveedor)) Assert.False(await solicitud.AutenticarAsync());

        // Arrange: alterar un carácter de una cookie válida

        var cookie = await EntrarAsync(proveedor);
        var indice = cookie.IndexOf('=') + 15;
        var manipulada = cookie[..indice] + (cookie[indice] == 'A' ? "B" : "A") + cookie[(indice + 1)..];

        // Act + Assert

        Assert.False(await AutenticarAsync(proveedor, manipulada));
    }


    /// Las claves que protegen la cookie se guardan en PostgreSQL, así que otra instancia de la
    /// aplicación puede leer la misma sesión.
    [Fact]
    public async Task Claves_DataProtection_Persisten_Y_Otro_Proveedor_Lee_La_Sesion()
    {

        // Arrange

        var escenario = await postgres.Crear();
        string cookie;

        // Act: iniciar sesión en una instancia

        using (var primero = escenario.Servicios())
        {
            await CrearAsync(primero);
            cookie = await EntrarAsync(primero);
        }

        // Assert: las claves quedaron en PostgreSQL

        Assert.True(await PostgreSqlRevision.Valor<long>(escenario.App, "SELECT count(*) FROM \"DataProtectionKeys\"") > 0);

        // Act + Assert: otra instancia lee la misma sesión

        using var segundo = escenario.Servicios();
        Assert.True(await AutenticarAsync(segundo, cookie));
    }


    // ──── AUDITORÍA Y DATOS SENSIBLES ────────────────────────────────────────────────────────


    /// Si falla el registro de auditoría, el cambio (restablecer, rol, desactivar o contraseña propia)
    /// se deshace por completo y la sesión del usuario sigue como estaba (Q03, Q08).
    [Theory]
    [InlineData("restablecer")]
    [InlineData("cambiar-rol")]
    [InlineData("desactivar")]
    [InlineData("cambiar-propia")]
    public async Task Fallo_De_Auditoria_Revierte_Cambio_De_Identidad_Y_Conserva_Sesion(string operacion)
    {

        // Arrange

        var escenario = await postgres.Crear();
        using var proveedor = escenario.Servicios();
        await CrearAsync(proveedor, "actor-ficticio", "Superusuario");
        var id = await CrearAsync(proveedor);
        var usuarioCookie = await EntrarAsync(proveedor);
        var actorCookie = await EntrarAsync(proveedor, "actor-ficticio");
        var anterior = await PostgreSqlRevision.Valor<string>(escenario.App, $"SELECT row_to_json(u)::text FROM \"AspNetUsers\" u WHERE \"Id\"='{id}'");

        // Falla provocada solo en esta base de prueba: toda inserción de auditoría da error.

        await PostgreSqlRevision.Sql(escenario.Migrador, """
            CREATE FUNCTION fallo_auditoria_qa() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN RAISE EXCEPTION 'Fallo ficticio QA'; END $$;
            CREATE TRIGGER fallo_auditoria_qa BEFORE INSERT ON "EventoAuditoria" FOR EACH ROW EXECUTE FUNCTION fallo_auditoria_qa();
            """);

        // Act + Assert: la operación falla

        using (var solicitud = new SolicitudIdentidad(proveedor, operacion == "cambiar-propia" ? usuarioCookie : actorCookie))
        {
            Assert.True(await solicitud.AutenticarAsync());
            await Assert.ThrowsAsync<DbUpdateException>(async () =>
            {
                switch (operacion)
                {
                    case "restablecer": await solicitud.Obtener<IServicioUsuarios>().RestablecerContrasenaAsync(id); break;
                    case "cambiar-rol": await solicitud.Obtener<IServicioUsuarios>().CambiarRolAsync(id, "Superusuario"); break;
                    case "desactivar": await solicitud.Obtener<IServicioUsuarios>().DesactivarUsuarioAsync(id); break;
                    case "cambiar-propia": await solicitud.Obtener<IServicioContrasena>().CambiarContrasenaPropiaAsync(Contrasena, Nueva); break;
                }
            });
        }

        // Assert: el usuario quedó igual y su sesión sigue válida

        Assert.Equal(anterior, await PostgreSqlRevision.Valor<string>(escenario.App, $"SELECT row_to_json(u)::text FROM \"AspNetUsers\" u WHERE \"Id\"='{id}'"));
        Assert.Equal(0L, await PostgreSqlRevision.Valor<long>(escenario.App, "SELECT count(*) FROM \"EventoAuditoria\""));
        Assert.True(await AutenticarAsync(proveedor, usuarioCookie));
    }


    /// Ni la auditoría ni los registros (logs) contienen contraseñas, la temporal, el hash, la cookie
    /// o la cadena de conexión.
    [Fact]
    public async Task Alta_Temporal_Y_Cambio_No_Filtran_Datos_Sensibles_En_Auditoria_Ni_Registros()
    {

        // Arrange

        var escenario = await postgres.Crear();
        using var registro = new RegistroRevision();
        using var proveedor = escenario.Servicios(servicios => servicios.AddLogging(opciones => opciones.AddProvider(registro)));
        await CrearAsync(proveedor, "actor-ficticio", "Superusuario");
        var actorCookie = await EntrarAsync(proveedor, "actor-ficticio");
        ContrasenaTemporalEntregada creada;

        // Act: crear usuario, entrar con la temporal y cambiarla

        using (var solicitud = new SolicitudIdentidad(proveedor, actorCookie))
        {
            Assert.True(await solicitud.AutenticarAsync());
            var respuesta = await solicitud.Obtener<IServicioUsuarios>().CrearUsuarioAsync("datoRastreable-nombre-ficticio", "Usuaria");
            Assert.True(respuesta.Exito);
            creada = respuesta.Datos!;
        }
        var cookie = await EntrarAsync(proveedor, "datoRastreable-nombre-ficticio", creada.ContrasenaTemporal);
        using (var solicitud = new SolicitudIdentidad(proveedor, cookie))
        {
            Assert.True(await solicitud.AutenticarAsync());
            Assert.True((await solicitud.Obtener<IServicioContrasena>().CambiarContrasenaPropiaAsync(null, Nueva)).Exito);
        }

        // Assert: ningún valor sensible aparece en auditoría ni registros

        var auditoria = await PostgreSqlRevision.Valor<string>(escenario.App, "SELECT json_agg(e)::text FROM \"EventoAuditoria\" e");
        Assert.Equal(2L, await PostgreSqlRevision.Valor<long>(escenario.App, "SELECT count(*) FROM \"EventoAuditoria\""));
        var hash = await PostgreSqlRevision.Valor<string>(escenario.App, $"SELECT \"PasswordHash\" FROM \"AspNetUsers\" WHERE \"Id\"='{creada.Usuario.IdUsuario}'");
        foreach (var datoRastreable in new[] { Contrasena, Nueva, creada.ContrasenaTemporal, hash, cookie.Split('=', 2)[1], escenario.App })
        {
            Assert.DoesNotContain(datoRastreable, auditoria, StringComparison.Ordinal);
            Assert.DoesNotContain(datoRastreable, registro.Texto, StringComparison.Ordinal);
        }
    }
}
