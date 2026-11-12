using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using TherapEase.Application.Compartido.Interfaces.Servicios;
using TherapEase.Application.Compartido.Modelos;
using TherapEase.Application.Identidad.Constantes;
using TherapEase.Application.Identidad.Interfaces.Servicios;
using TherapEase.Domain.Compartido.Entidades.Enums;
using TherapEase.Web.Seguridad;
using TherapEase.Web.Seguridad.Requisitos;
using static TherapEase.IntegrationTests.ServidorIdentidadRevision;

namespace TherapEase.IntegrationTests;

/// <summary>
/// Comprueba quién puede consultar o cambiar usuarios con los servicios reales y PostgreSQL.
/// Una operación sin permiso no entrega datos ni modifica usuarios, roles o auditoría.
/// </summary>
public class PermisosUsuariosTests(PostgreSqlRevision postgres) : IClassFixture<PostgreSqlRevision>
{


    // ──── OPERACIONES SIN PERMISO ──────────────────────────────────────────────────────────


    /// Combina las siete operaciones con falta de sesión, rol Usuaria y contraseña temporal.
    public static TheoryData<string, string> CasosSinPermiso()
    {
        var casos = new TheoryData<string, string>();
        foreach (var estado in new[] { "sin-sesion", "usuaria", "usuaria-temporal", "superusuario-temporal" })
        foreach (var operacion in new[] { "listar", "ficha", "crear", "editar", "rol", "desactivar", "restablecer" })
            casos.Add(estado, operacion);
        return casos;
    }

    /// Sin permiso, cada operación responde igual, no entrega datos y deja toda la base como estaba.
    [Theory]
    [MemberData(nameof(CasosSinPermiso))]
    public async Task Administracion_Sin_Permiso_No_Lee_Ni_Modifica_Usuarios(string estado, string operacion)
    {
        // Arrange
        var escenario = await postgres.Crear();
        using var proveedor = escenario.Servicios();
        await CrearAsync(proveedor, "operador-ficticio", "Superusuario");
        var objetivo = await CrearAsync(proveedor, "objetivo-ficticio");
        string? cookie = null;
        if (estado != "sin-sesion")
        {
            var rol = estado == "superusuario-temporal" ? "Superusuario" : "Usuaria";
            await CrearAsync(proveedor, "actor-ficticio", rol, estado.EndsWith("temporal", StringComparison.Ordinal));
            cookie = await EntrarAsync(proveedor, "actor-ficticio");
        }
        using var solicitud = new SolicitudIdentidad(proveedor, cookie);
        Assert.Equal(estado != "sin-sesion", await solicitud.AutenticarAsync());
        var anterior = await EstadoAsync(escenario);

        // Act
        var resultado = await EjecutarAdministracionAsync(solicitud.Obtener<IServicioUsuarios>(), objetivo, operacion);

        // Assert
        Assert.False(resultado.Exito);
        Assert.True(resultado.SinDatos);
        Assert.Equal(MensajesDeIdentidad.SinPermiso, resultado.Mensaje);
        await ComprobarSinCambiosAsync(escenario, anterior);
    }


    // ──── PERMISOS POR ROL ─────────────────────────────────────────────────────────────────


    /// Los permisos reales coinciden con la lista aprobada por rol; con temporal se niegan todos.
    /// La regla web permite cambiar la contraseña propia, pero no abre otras funciones con temporal.
    [Theory]
    [InlineData("Superusuario", false)]
    [InlineData("Usuaria", false)]
    [InlineData("Superusuario", true)]
    [InlineData("Usuaria", true)]
    public async Task Rol_Y_Temporal_Respetan_Permisos_Explicitos_Y_Reglas_Web(string rol, bool temporal)
    {
        // Arrange
        var escenario = await postgres.Crear();
        using var proveedor = escenario.Servicios();
        await CrearAsync(proveedor, "actor-ficticio", rol, temporal);
        var cookie = await EntrarAsync(proveedor, "actor-ficticio");
        using var solicitud = new SolicitudIdentidad(proveedor, cookie);
        Assert.True(await solicitud.AutenticarAsync());
        var anterior = await EstadoAsync(escenario);
        var esperados = temporal ? Array.Empty<Permiso>() : rol == "Superusuario"
            ? new[] { Permiso.AdministrarUsuarios, Permiso.ConsultarAuditoria, Permiso.DarDeBajaPacientes, Permiso.RegistrarPacientes, Permiso.ConsultarPacientes, Permiso.ActualizarPacientes }
            : new[] { Permiso.DarDeBajaPacientes, Permiso.RegistrarPacientes, Permiso.ConsultarPacientes, Permiso.ActualizarPacientes };
        var permisos = solicitud.Obtener<IAutorizacion>();
        var reglasWeb = solicitud.Obtener<IAuthorizationService>();
        var permitidos = new List<Permiso>();

        // Act
        foreach (var permiso in Enum.GetValues<Permiso>())
            if (await permisos.TienePermisoAsync(permiso)) permitidos.Add(permiso);
        var administrar = await reglasWeb.AuthorizeAsync(solicitud.Contexto.User, PoliticasDeAutorizacion.AdministrarUsuarios);
        var auditoria = await reglasWeb.AuthorizeAsync(solicitud.Contexto.User, PoliticasDeAutorizacion.ConsultarAuditoria);
        var operativo = await reglasWeb.AuthorizeAsync(solicitud.Contexto.User, PoliticasDeAutorizacion.AccesoOperativo);
        var propia = await reglasWeb.AuthorizeAsync(solicitud.Contexto.User, PoliticasDeAutorizacion.CambiarContrasenaPropia);

        // Assert
        Assert.Equal(esperados, permitidos);
        Assert.Equal(!temporal && rol == "Superusuario", administrar.Succeeded);
        Assert.Equal(!temporal && rol == "Superusuario", auditoria.Succeeded);
        Assert.Equal(!temporal, operativo.Succeeded);
        Assert.True(propia.Succeeded);
        await ComprobarSinCambiosAsync(escenario, anterior);

        // Act + Assert: un número que no corresponde a ningún permiso siempre se rechaza.
        foreach (var desconocido in new[] { (Permiso)(-1), (Permiso)int.MaxValue })
        {
            Assert.False(await permisos.TienePermisoAsync(desconocido));
            Assert.False((await reglasWeb.AuthorizeAsync(solicitud.Contexto.User, null,
                new[] { new RequisitoDePermiso(desconocido) })).Succeeded);
        }
    }


    // ──── DATOS PERMITIDOS ─────────────────────────────────────────────────────────────────


    /// El Superusuario autorizado lee el correo en la ficha, pero la lista no lo incluye.
    /// Ambas consultas dejan usuarios y auditoría sin cambios.
    [Fact]
    public async Task Superusuario_Lee_Correo_Solo_En_Ficha_Sin_Escribir()
    {
        // Arrange
        var escenario = await postgres.Crear();
        using var proveedor = escenario.Servicios();
        var objetivo = await CrearAsync(proveedor, "ficha-ficticia",
            nombreCompleto: "Nombre Ficticio", correo: "ficha@ficticio.mx");
        var cookie = await PrepararSuperusuarioAsync(proveedor);
        using var solicitud = new SolicitudIdentidad(proveedor, cookie);
        Assert.True(await solicitud.AutenticarAsync());
        var anterior = await EstadoAsync(escenario);

        // Act
        var servicio = solicitud.Obtener<IServicioUsuarios>();
        var lista = await servicio.ListarUsuariosAsync("ficha-ficticia");
        var ficha = await servicio.ObtenerUsuarioAsync(objetivo);

        // Assert
        Assert.True(lista.Exito);
        Assert.True(ficha.Exito);
        Assert.Equal(objetivo, Assert.Single(lista.Datos!).IdUsuario);
        Assert.Equal(objetivo, ficha.Datos!.Datos.IdUsuario);
        Assert.Equal("Nombre Ficticio", ficha.Datos.Datos.NombreCompleto);
        Assert.Equal("ficha@ficticio.mx", ficha.Datos.Correo);
        var contenidoLista = JsonSerializer.Serialize(lista.Datos);
        Assert.DoesNotContain("Correo", contenidoLista, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ficha@ficticio.mx", contenidoLista, StringComparison.Ordinal);
        await ComprobarSinCambiosAsync(escenario, anterior);
    }

    /// Sin sesión, la consulta de datos propios responde sin permiso y no entrega datos.
    [Fact]
    public async Task Sin_Sesion_No_Obtiene_Datos_Propios()
    {
        // Arrange
        var escenario = await postgres.Crear();
        using var proveedor = escenario.Servicios();
        await CrearAsync(proveedor, "operador-ficticio", "Superusuario");
        using var solicitud = new SolicitudIdentidad(proveedor);
        Assert.False(await solicitud.AutenticarAsync());
        var anterior = await EstadoAsync(escenario);

        // Act
        var respuesta = await solicitud.Obtener<IServicioCuentaPropia>().ObtenerMisDatosAsync();

        // Assert
        Assert.False(respuesta.Exito);
        Assert.Null(respuesta.Datos);
        Assert.Equal(MensajesDeIdentidad.SinPermiso, respuesta.Mensaje);
        await ComprobarSinCambiosAsync(escenario, anterior);
    }


    // ──── LLAMADAS DE ADMINISTRACIÓN ───────────────────────────────────────────────────────


    /// Ejecuta cada operación con datos válidos para comprobar únicamente el rechazo por permiso.
    private static async Task<(bool Exito, bool SinDatos, string Mensaje)> EjecutarAdministracionAsync(
        IServicioUsuarios servicio, Guid objetivo, string operacion) => operacion switch
        {
            "listar" => Resumir(await servicio.ListarUsuariosAsync()),
            "ficha" => Resumir(await servicio.ObtenerUsuarioAsync(objetivo)),
            "crear" => Resumir(await servicio.CrearUsuarioAsync("nuevo-ficticio", "Usuaria", "Nombre Ficticio")),
            "editar" => Resumir(await servicio.ActualizarDatosDeCuentaAsync(objetivo, "Nombre Cambiado Ficticio", "nuevo@ficticio.mx")),
            "rol" => Resumir(await servicio.CambiarRolAsync(objetivo, "Superusuario")),
            "desactivar" => Resumir(await servicio.DesactivarUsuarioAsync(objetivo)),
            "restablecer" => Resumir(await servicio.RestablecerContrasenaAsync(objetivo)),
            _ => throw new ArgumentOutOfRangeException(nameof(operacion))
        };

    /// Compara las mismas partes de respuesta sin guardar datos privados en la evidencia.
    private static (bool Exito, bool SinDatos, string Mensaje) Resumir<T>(RespuestaServicio<T> respuesta) =>
        (respuesta.Exito, respuesta.Datos is null, respuesta.Mensaje);
}
