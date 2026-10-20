using TherapEase.Application.Identidad.Constantes;
using TherapEase.Application.Identidad.Interfaces.Servicios;
using TherapEase.Domain.Auditoria.Entidades.Enums;
using static TherapEase.IntegrationTests.ServidorIdentidadRevision;

namespace TherapEase.IntegrationTests;

/// <summary>
/// Comprueba cambios y restablecimientos de contraseñas en una base de datos de prueba.
/// Revisa el límite de doce caracteres, el bloqueo y el reemplazo de temporales.
/// Las contraseñas y cookies quedan en memoria, sin guardarlas en la evidencia.
/// </summary>
public class ContrasenasServidorTests(PostgreSqlRevision postgres) : IClassFixture<PostgreSqlRevision>
{


    // ──── CAMBIO PROPIO ────────────────────────────────────────────────────────────────────


    /// Combina los dos roles, la primera entrada y tres formas de escribir doce caracteres.
    public static TheoryData<string, bool, string> CasosDeDoceCaracteres()
    {
        var casos = new TheoryData<string, bool, string>();
        foreach (var rol in new[] { "Usuaria", "Superusuario" })
        foreach (var temporal in new[] { false, true })
        foreach (var forma in new[] { "minusculas", "mayusculas", "numeros" })
            casos.Add(rol, temporal, forma);
        return casos;
    }

    /// Doce caracteres bastan sin mezclar letras y números, en ambos roles y con o sin temporal.
    /// Solo cambia el usuario de la sesión; sus cookies anteriores dejan de servir.
    [Theory]
    [MemberData(nameof(CasosDeDoceCaracteres))]
    public async Task Cambio_Propio_Acepta_Doce_Caracteres_Sin_Composicion(
        string rol, bool temporal, string forma)
    {
        // Arrange
        var escenario = await postgres.Crear();
        using var proveedor = escenario.Servicios();
        var objetivo = await CrearAsync(proveedor, "propia-ficticia", rol, temporal);
        var ajeno = await CrearAsync(proveedor, "ajena-ficticia");
        var cookieAjena = await EntrarAsync(proveedor, "ajena-ficticia");
        var cookie = await EntrarAsync(proveedor, "propia-ficticia");
        var otraPropia = await EntrarAsync(proveedor, "propia-ficticia");
        var anteriorAjena = await CredencialGuardadaAsync(escenario, ajeno);
        var nueva = forma switch
        {
            "minusculas" => "abcdefghijkl",
            "mayusculas" => "ABCDEFGHIJKL",
            _ => "123456789012"
        };
        string actualizada;
        using (var solicitud = new SolicitudIdentidad(proveedor, cookie))
        {
            Assert.True(await solicitud.AutenticarAsync());

            // Act
            var respuesta = await solicitud.Obtener<IServicioContrasena>()
                .CambiarContrasenaPropiaAsync(temporal ? null : Contrasena, nueva);

            // Assert
            Assert.True(respuesta.Exito);
            Assert.Equal(objetivo, respuesta.Datos!.IdUsuario);
            Assert.Equal(rol, respuesta.Datos.Rol);
            Assert.False(respuesta.Datos.DebeCambiarContrasena);
            actualizada = solicitud.CookieEmitida();
        }
        Assert.True(await ContrasenaFuncionaAsync(proveedor, objetivo, nueva));
        Assert.False(await ContrasenaFuncionaAsync(proveedor, objetivo, Contrasena));
        Assert.False(await CookieSigueSirviendoAsync(proveedor, cookie));
        Assert.False(await CookieSigueSirviendoAsync(proveedor, otraPropia));
        Assert.True(await CookieSigueSirviendoAsync(proveedor, actualizada));
        Assert.True(await CookieSigueSirviendoAsync(proveedor, cookieAjena));
        Assert.True(anteriorAjena == await CredencialGuardadaAsync(escenario, ajeno),
            "El cambio propio modificó la contraseña o la marca de seguridad de otro usuario.");
        var evento = Assert.Single(await EventosDeAccionAsync(escenario, AccionAuditoria.CambioContrasena));
        Assert.Equal(objetivo, evento.IdUsuarioActor);
        Assert.Equal(objetivo, evento.IdRegistro);
        Assert.Equal(new[] { "Contrasena" }, evento.CamposAfectados);
        Assert.False(evento.Hecho.Contains(nueva, StringComparison.Ordinal));
    }

    /// Una nueva contraseña ausente, vacía o de once caracteres no cambia la contraseña guardada.
    /// Si había temporal pendiente, sigue pendiente y la sesión conserva su cookie anterior.
    [Theory]
    [InlineData(false, "ausente")]
    [InlineData(false, "vacia")]
    [InlineData(false, "once")]
    [InlineData(true, "ausente")]
    [InlineData(true, "vacia")]
    [InlineData(true, "once")]
    public async Task Nueva_Invalida_Conserva_Credencial_Y_No_Emite_Cookie(bool temporal, string caso)
    {
        // Arrange
        var escenario = await postgres.Crear();
        using var proveedor = escenario.Servicios();
        var objetivo = await CrearAsync(proveedor, "propia-ficticia", temporal: temporal);
        var cookie = await EntrarAsync(proveedor, "propia-ficticia");
        var anterior = await CredencialGuardadaAsync(escenario, objetivo);
        var nueva = caso == "ausente" ? null : caso == "vacia" ? string.Empty : "abcdefghijk";
        using (var solicitud = new SolicitudIdentidad(proveedor, cookie))
        {
            Assert.True(await solicitud.AutenticarAsync());

            // Act
            var respuesta = await solicitud.Obtener<IServicioContrasena>()
                .CambiarContrasenaPropiaAsync(temporal ? null : Contrasena, nueva!);

            // Assert
            Assert.False(respuesta.Exito);
            Assert.Null(respuesta.Datos);
            Assert.Equal(caso == "once" ? MensajesDeIdentidad.ContrasenaNuevaNoCumpleReglas
                : MensajesDeIdentidad.CambioDeContrasenaNoRealizado, respuesta.Mensaje);
            Assert.Equal(0, solicitud.Contexto.Response.Headers.SetCookie.Count);
        }
        Assert.True(anterior == await CredencialGuardadaAsync(escenario, objetivo),
            "La contraseña rechazada dejó cambios en la credencial.");
        Assert.True(await CookieSigueSirviendoAsync(proveedor, cookie));
        Assert.Empty(await EventosDeAccionAsync(escenario, AccionAuditoria.CambioContrasena));
    }

    /// En el cambio ordinario, omitir la contraseña actual no cambia nada ni cuenta como intento fallido.
    [Theory]
    [InlineData("Usuaria", false)]
    [InlineData("Usuaria", true)]
    [InlineData("Superusuario", false)]
    [InlineData("Superusuario", true)]
    public async Task Cambio_Ordinario_Exige_Actual_Sin_Modificar_Al_Omitirla(string rol, bool vacia)
    {
        // Arrange
        var escenario = await postgres.Crear();
        using var proveedor = escenario.Servicios();
        await CrearAsync(proveedor, "propia-ficticia", rol);
        var cookie = await EntrarAsync(proveedor, "propia-ficticia");
        using var solicitud = new SolicitudIdentidad(proveedor, cookie);
        Assert.True(await solicitud.AutenticarAsync());
        var anterior = await EstadoAsync(escenario);

        // Act
        var respuesta = await solicitud.Obtener<IServicioContrasena>()
            .CambiarContrasenaPropiaAsync(vacia ? string.Empty : null, NuevaContrasena);

        // Assert
        Assert.False(respuesta.Exito);
        Assert.Null(respuesta.Datos);
        Assert.Equal(MensajesDeIdentidad.CambioDeContrasenaNoRealizado, respuesta.Mensaje);
        Assert.Equal(0, solicitud.Contexto.Response.Headers.SetCookie.Count);
        await ComprobarSinCambiosAsync(escenario, anterior);
    }


    // ──── SESIÓN Y BLOQUEO ─────────────────────────────────────────────────────────────────


    /// Sin una sesión aceptada, el cambio de contraseña propia se rechaza y no entrega una cookie nueva.
    [Theory]
    [InlineData("ausente")]
    [InlineData("alterada")]
    [InlineData("revocada")]
    [InlineData("inactivo")]
    public async Task Sesion_Rechazada_No_Cambia_Contrasena(string estado)
    {
        // Arrange
        var escenario = await postgres.Crear();
        using var proveedor = escenario.Servicios();
        var objetivo = await CrearAsync(proveedor, "propia-ficticia");
        var original = await EntrarAsync(proveedor, "propia-ficticia");
        if (estado == "revocada")
        {
            using var salida = new SolicitudIdentidad(proveedor, original);
            Assert.True(await salida.AutenticarAsync());
            await salida.Obtener<IServicioAcceso>().CerrarSesionAsync();
        }
        if (estado == "inactivo")
            await PostgreSqlRevision.Sql(escenario.App,
                $"""UPDATE "AspNetUsers" SET "Activo"=false WHERE "Id"='{objetivo}'""");
        var cookie = estado == "ausente" ? null : estado == "alterada"
            ? "TherapEase.Sesion=valor-ficticio-no-valido" : original;
        using var solicitud = new SolicitudIdentidad(proveedor, cookie);
        Assert.False(await solicitud.AutenticarAsync());
        var cookiesAntes = solicitud.Contexto.Response.Headers.SetCookie.ToArray();
        var anterior = await EstadoAsync(escenario);

        // Act
        var respuesta = await solicitud.Obtener<IServicioContrasena>()
            .CambiarContrasenaPropiaAsync(Contrasena, NuevaContrasena);

        // Assert
        Assert.False(respuesta.Exito);
        Assert.Null(respuesta.Datos);
        Assert.Equal(MensajesDeIdentidad.CambioDeContrasenaNoRealizado, respuesta.Mensaje);
        Assert.True(cookiesAntes.SequenceEqual(solicitud.Contexto.Response.Headers.SetCookie),
            "El intento sin sesión aceptada modificó las cookies de la respuesta.");
        await ComprobarSinCambiosAsync(escenario, anterior);
    }

    /// Cinco errores de contraseña actual bloquean quince minutos, sin cambiar la contraseña guardada.
    /// Acertar después no permite cambiarla ni entrar mientras siga el bloqueo.
    [Fact]
    public async Task Cinco_Fallos_En_Cambio_Propio_Bloquean_Y_Conservan_Credencial()
    {
        // Arrange
        var escenario = await postgres.Crear();
        using var proveedor = escenario.Servicios();
        var objetivo = await CrearAsync(proveedor, "propia-ficticia");
        var cookie = await EntrarAsync(proveedor, "propia-ficticia");
        var anterior = await CredencialGuardadaAsync(escenario, objetivo);
        var antes = DateTimeOffset.UtcNow;

        // Act + Assert: cada error suma; el quinto reinicia el contador y activa el bloqueo.
        for (var intento = 1; intento <= 5; intento++)
        {
            using var solicitud = new SolicitudIdentidad(proveedor, cookie);
            Assert.True(await solicitud.AutenticarAsync());
            var respuesta = await solicitud.Obtener<IServicioContrasena>()
                .CambiarContrasenaPropiaAsync("incorrecta-ficticia", NuevaContrasena);
            Assert.False(respuesta.Exito);
            Assert.Null(respuesta.Datos);
            Assert.Equal(0, solicitud.Contexto.Response.Headers.SetCookie.Count);
            Assert.Equal(intento < 5 ? intento : 0, await IntentosFallidosAsync(escenario, objetivo));
        }
        var despues = DateTimeOffset.UtcNow;

        // Assert
        var fin = await FinDeBloqueoAsync(escenario, objetivo);
        Assert.NotNull(fin);
        Assert.InRange(fin.Value, antes.AddMinutes(15), despues.AddMinutes(15));
        Assert.True(anterior == await CredencialGuardadaAsync(escenario, objetivo),
            "Los intentos fallidos modificaron la contraseña o su marca de seguridad.");

        // Act + Assert: acertar no levanta el bloqueo vigente.
        using (var solicitud = new SolicitudIdentidad(proveedor, cookie))
        {

            // El bloqueo impide entrar de nuevo, pero no cierra la sesión ya abierta.

            Assert.True(await solicitud.AutenticarAsync());
            var respuesta = await solicitud.Obtener<IServicioContrasena>()
                .CambiarContrasenaPropiaAsync(Contrasena, NuevaContrasena);
            Assert.False(respuesta.Exito);
            Assert.Null(respuesta.Datos);
            Assert.Equal(0, solicitud.Contexto.Response.Headers.SetCookie.Count);
        }
        using (var acceso = new SolicitudIdentidad(proveedor))
        {
            var respuesta = await acceso.Obtener<IServicioAcceso>()
                .IniciarSesionAsync("propia-ficticia", Contrasena);
            Assert.False(respuesta.Exito);
            Assert.Equal(MensajesDeIdentidad.CredencialesInvalidas, respuesta.Mensaje);
            Assert.Equal(0, acceso.Contexto.Response.Headers.SetCookie.Count);
        }

        // Assert
        Assert.Equal(fin, await FinDeBloqueoAsync(escenario, objetivo));
        Assert.True(anterior == await CredencialGuardadaAsync(escenario, objetivo));
        Assert.Empty(await EventosDeAccionAsync(escenario, AccionAuditoria.CambioContrasena));
    }


    // ──── RESTABLECIMIENTO ─────────────────────────────────────────────────────────────────


    /// Un segundo restablecimiento invalida la primera temporal y la sesión que se abrió con ella.
    /// Solo sirve la nueva temporal y quedan dos cambios confirmados en la auditoría.
    [Fact]
    public async Task Nuevo_Restablecimiento_Invalida_Temporal_Y_Sesion_Anteriores()
    {
        // Arrange
        var escenario = await postgres.Crear();
        using var proveedor = escenario.Servicios();
        var objetivo = await CrearAsync(proveedor, "objetivo-ficticio");
        var original = await EntrarAsync(proveedor, "objetivo-ficticio");
        var cookie = await PrepararSuperusuarioAsync(proveedor);
        string primera;
        using (var solicitud = new SolicitudIdentidad(proveedor, cookie))
        {
            Assert.True(await solicitud.AutenticarAsync());

            // Act + Assert: el primer restablecimiento debe confirmarse antes de usar su temporal.
            var respuesta = await solicitud.Obtener<IServicioUsuarios>().RestablecerContrasenaAsync(objetivo);
            Assert.True(respuesta.Exito);
            primera = respuesta.Datos!.ContrasenaTemporal;
        }
        var sesionPrimera = await EntrarAsync(proveedor, "objetivo-ficticio", primera);
        string segunda;
        using (var solicitud = new SolicitudIdentidad(proveedor, cookie))
        {
            Assert.True(await solicitud.AutenticarAsync());

            // Act
            var respuesta = await solicitud.Obtener<IServicioUsuarios>().RestablecerContrasenaAsync(objetivo);

            // Assert
            Assert.True(respuesta.Exito);
            Assert.Equal(objetivo, respuesta.Datos!.Usuario.IdUsuario);
            Assert.True(respuesta.Datos.Usuario.DebeCambiarContrasena);
            segunda = respuesta.Datos.ContrasenaTemporal;
        }
        Assert.True(primera != segunda, "Los dos restablecimientos entregaron la misma temporal.");
        Assert.True(segunda.Length >= 12);
        Assert.False(await ContrasenaFuncionaAsync(proveedor, objetivo, primera));
        Assert.False(await ContrasenaFuncionaAsync(proveedor, objetivo, Contrasena));
        Assert.True(await ContrasenaFuncionaAsync(proveedor, objetivo, segunda));
        Assert.False(await CookieSigueSirviendoAsync(proveedor, original));
        Assert.False(await CookieSigueSirviendoAsync(proveedor, sesionPrimera));
        var nuevaSesion = await EntrarAsync(proveedor, "objetivo-ficticio", segunda);
        Assert.True(await CookieSigueSirviendoAsync(proveedor, nuevaSesion));
        var eventos = await EventosDeAccionAsync(escenario, AccionAuditoria.ReinicioContrasena);
        Assert.Equal(2, eventos.Count);
        Assert.All(eventos, e =>
        {
            Assert.Equal(objetivo, e.IdRegistro);
            Assert.Equal(new[] { "Contrasena" }, e.CamposAfectados);
            Assert.False(e.Hecho.Contains(primera, StringComparison.Ordinal));
            Assert.False(e.Hecho.Contains(segunda, StringComparison.Ordinal));
        });
    }


    // ──── DATOS DE CONTRASEÑA SOLO EN MEMORIA ──────────────────────────────────────────────


    /// Lee la contraseña protegida, su marca de seguridad y si hay temporal pendiente.
    /// Solo compara esos datos en memoria; los contadores de intentos se comprueban aparte.
    private static Task<string> CredencialGuardadaAsync(Escenario escenario, Guid id) =>
        PostgreSqlRevision.Valor<string>(escenario.App, $"""
            SELECT json_build_array("PasswordHash", "SecurityStamp", "DebeCambiarContrasena")::text
            FROM "AspNetUsers" WHERE "Id"='{id}'
            """);
}
