using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using TherapEase.Application.Identidad.Interfaces.Servicios;
using TherapEase.Application.Identidad.Modelos;
using TherapEase.Infrastructure.Identidad.Entidades;
using TherapEase.Infrastructure.Data;
using static TherapEase.IntegrationTests.ServidorIdentidadRevision;

namespace TherapEase.IntegrationTests;

/// <summary>
/// Comprueba la lista de usuarios, el alta y la edición de nombre y correo en una base de datos de prueba.
/// Revisa lo guardado, las sesiones y la auditoría, sin páginas ni datos reales.
/// </summary>
public class ConsultaYEdicionUsuariosTests(PostgreSqlRevision postgres) : IClassFixture<PostgreSqlRevision>
{


    // ──── CONSULTAS Y ALTAS ────────────────────────────────────────────────────────────────


    /// La búsqueda encuentra el texto, ordena por nombre de usuario y muestra 100 como máximo.
    /// Los símbolos % y _ o un texto como «' OR 1=1» se buscan tal cual, no como órdenes para la base de datos.
    [Fact]
    public async Task Lista_Busca_Texto_Y_Limita_Cien_Usuarios()
    {
        // Arrange
        var escenario = await postgres.Crear();
        using var servicios = escenario.Servicios();
        var buscada = await CrearAsync(servicios, "usuaria-ficticia",
            nombreCompleto: "María Fernández");

        // Crea 105 usuarios ficticios para comprobar que la lista se corta en 100.

        for (var i = 0; i < 105; i++)
            await CrearAsync(servicios, $"fila-{i:000}");
        var cookie = await PrepararSuperusuarioAsync(servicios);
        using var solicitud = new SolicitudIdentidad(servicios, cookie);
        Assert.True(await solicitud.AutenticarAsync());
        var usuarios = solicitud.Obtener<IServicioUsuarios>();

        // Act
        var lista = await usuarios.ListarUsuariosAsync("fila-");
        var busqueda = await usuarios.ListarUsuariosAsync(" FERN ");
        var porcentaje = await usuarios.ListarUsuariosAsync("%");
        var guionBajo = await usuarios.ListarUsuariosAsync("_");
        var manipulada = await usuarios.ListarUsuariosAsync("' OR 1=1;--");

        // Assert
        Assert.True(lista.Exito);
        Assert.Equal(100, lista.Datos!.Count);
        Assert.Equal("fila-000", lista.Datos[0].NombreUsuario);
        Assert.Equal("fila-099", lista.Datos[^1].NombreUsuario);
        Assert.Equal(buscada, Assert.Single(busqueda.Datos!).IdUsuario);
        Assert.Empty(porcentaje.Datos!);
        Assert.Empty(guionBajo.Datos!);
        Assert.Empty(manipulada.Datos!);
        await using var lectura = escenario.Contexto();
        Assert.Empty(await lectura.EventosAuditoria.ToListAsync());
    }

    /// El alta guarda el nombre y el correo.
    /// Su registro de auditoría solo nombra los campos, sin sus valores ni la contraseña temporal.
    [Fact]
    public async Task Alta_Guarda_Datos_Y_Evento_Sin_Valores_Privados()
    {
        // Arrange
        var escenario = await postgres.Crear();
        using var servicios = escenario.Servicios();
        var cookie = await PrepararSuperusuarioAsync(servicios);
        using var solicitud = new SolicitudIdentidad(servicios, cookie);
        Assert.True(await solicitud.AutenticarAsync());

        // Act
        var respuesta = await solicitud.Obtener<IServicioUsuarios>().CrearUsuarioAsync(
            "nueva-ficticia", "Usuaria", "  Nombre Marcador Ficticio  ", "  MARCADOR@Ficticio.MX ");

        // Assert
        Assert.True(respuesta.Exito);
        var entrega = Assert.IsType<ContrasenaTemporalEntregada>(respuesta.Datos);
        await using var lectura = escenario.Contexto();
        var usuario = await lectura.Users.AsNoTracking().SingleAsync(u => u.Id == entrega.Usuario.IdUsuario);
        Assert.True(usuario.NombreCompleto == "Nombre Marcador Ficticio");
        Assert.True(usuario.Email == "marcador@ficticio.mx");
        Assert.True(usuario.DebeCambiarContrasena);
        using var alcance = servicios.CreateScope();
        var gestor = alcance.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
        var entidad = await gestor.FindByIdAsync(usuario.Id.ToString());
        Assert.True(await gestor.CheckPasswordAsync(entidad!, entrega.ContrasenaTemporal));
        var evento = Assert.Single(await lectura.EventosAuditoria.ToListAsync());
        Assert.Equal(new[] { "NombreUsuario", "Rol", "DebeCambiarContrasena", "NombreCompleto", "Correo" },
            evento.CamposAfectados);
        Assert.DoesNotContain("Nombre Marcador Ficticio", evento.Hecho);
        Assert.DoesNotContain("marcador@ficticio.mx", evento.Hecho);
        Assert.False(evento.Hecho.Contains(entrega.ContrasenaTemporal, StringComparison.Ordinal));
    }


    // ──── DATOS PROPIOS Y EDICIÓN ──────────────────────────────────────────────────────────


    /// Cada sesión puede leer sus propios datos, aun con contraseña temporal.
    /// Eso no le permite ver la lista ni la ficha de otros si su rol o la contraseña temporal lo impiden.
    [Theory]
    [InlineData("Usuaria", false)]
    [InlineData("Usuaria", true)]
    [InlineData("Superusuario", true)]
    public async Task Datos_Propios_No_Autorizan_Administrar_Otros(string rol, bool temporal)
    {
        // Arrange
        var escenario = await postgres.Crear();
        using var servicios = escenario.Servicios();
        var ajeno = await CrearAsync(servicios, "operador-ficticio", "Superusuario");
        var propia = await CrearAsync(servicios, "propia-ficticia", rol, temporal);
        var cookie = await EntrarAsync(servicios, "propia-ficticia");
        using var solicitud = new SolicitudIdentidad(servicios, cookie);
        Assert.True(await solicitud.AutenticarAsync());

        // Act
        var datos = await solicitud.Obtener<IServicioCuentaPropia>().ObtenerMisDatosAsync();
        var lista = await solicitud.Obtener<IServicioUsuarios>().ListarUsuariosAsync();
        var ficha = await solicitud.Obtener<IServicioUsuarios>().ObtenerUsuarioAsync(ajeno);

        // Assert
        Assert.True(datos.Exito);
        Assert.Equal(propia, datos.Datos!.Datos.IdUsuario);
        Assert.Equal(temporal, datos.Datos.Datos.DebeCambiarContrasena);
        Assert.False(lista.Exito);
        Assert.Null(lista.Datos);
        Assert.False(ficha.Exito);
        Assert.Null(ficha.Datos);
    }

    /// Editar el nombre y el correo no cierra las sesiones abiertas del usuario.
    /// Repetir los mismos datos no crea un segundo registro en la auditoría.
    [Fact]
    public async Task Edicion_Conserva_Sesiones_Y_No_Duplica_Evento()
    {
        // Arrange
        var escenario = await postgres.Crear();
        using var servicios = escenario.Servicios();
        await CrearAsync(servicios, "operador-ficticio", "Superusuario");
        var objetivo = await CrearAsync(servicios, "objetivo-ficticio");
        var primera = await EntrarAsync(servicios, "objetivo-ficticio");
        var segunda = await EntrarAsync(servicios, "objetivo-ficticio");

        // Marca de seguridad del usuario (sello): si cambiara, sus sesiones abiertas se cerrarían.

        string? sello;
        await using (var lectura = escenario.Contexto())
            sello = (await lectura.Users.AsNoTracking().SingleAsync(u => u.Id == objetivo)).SecurityStamp;
        var cookie = await EntrarAsync(servicios, "operador-ficticio");

        // Act + Assert: ambas solicitudes deben confirmar la operación.
        using (var solicitud = new SolicitudIdentidad(servicios, cookie))
        {
            Assert.True(await solicitud.AutenticarAsync());
            Assert.True((await solicitud.Obtener<IServicioUsuarios>()
                .ActualizarDatosDeCuentaAsync(objetivo, "Nombre Corregido Ficticio", "NUEVO@Ficticio.MX")).Exito);
        }
        using (var solicitud = new SolicitudIdentidad(servicios, cookie))
        {
            Assert.True(await solicitud.AutenticarAsync());
            Assert.True((await solicitud.Obtener<IServicioUsuarios>()
                .ActualizarDatosDeCuentaAsync(objetivo, "  Nombre Corregido Ficticio  ", "nuevo@ficticio.mx")).Exito);
        }

        // Assert
        await using var contexto = escenario.Contexto();
        var usuario = await contexto.Users.AsNoTracking().SingleAsync(u => u.Id == objetivo);
        Assert.True(usuario.NombreCompleto == "Nombre Corregido Ficticio" && usuario.Email == "nuevo@ficticio.mx");
        Assert.True(usuario.SecurityStamp == sello, "La edición cambió el sello de seguridad.");
        var evento = Assert.Single(await contexto.EventosAuditoria.ToListAsync());
        Assert.Equal(new[] { "NombreCompleto", "Correo" }, evento.CamposAfectados);
        Assert.True(await CookieSigueSirviendoAsync(servicios, primera));
        Assert.True(await CookieSigueSirviendoAsync(servicios, segunda));
    }

    /// Cambiar solo el correo conserva el nombre y registra únicamente Correo en la auditoría.
    [Fact]
    public async Task Cambio_Solo_Correo_Registra_Unicamente_Correo()
    {
        // Arrange
        var escenario = await postgres.Crear();
        using var servicios = escenario.Servicios();
        var objetivo = await CrearAsync(servicios, "objetivo-ficticio",
            nombreCompleto: "Nombre Ficticio", correo: "contacto@ficticio.mx");
        string nombreAnterior;
        string? selloAnterior;
        await using (var lectura = escenario.Contexto())
        {
            var anterior = await lectura.Users.AsNoTracking().SingleAsync(u => u.Id == objetivo);
            nombreAnterior = anterior.NombreCompleto;
            selloAnterior = anterior.SecurityStamp;
            Assert.Equal("contacto@ficticio.mx", anterior.Email);
        }
        var cookie = await PrepararSuperusuarioAsync(servicios);
        using var solicitud = new SolicitudIdentidad(servicios, cookie);
        Assert.True(await solicitud.AutenticarAsync());

        // Act
        var respuesta = await solicitud.Obtener<IServicioUsuarios>()
            .ActualizarDatosDeCuentaAsync(objetivo, nombreAnterior, "SOLO-CORREO@Ficticio.MX");

        // Assert
        Assert.True(respuesta.Exito);
        await using var contexto = escenario.Contexto();
        var usuario = await contexto.Users.AsNoTracking().SingleAsync(u => u.Id == objetivo);
        Assert.Equal(nombreAnterior, usuario.NombreCompleto);
        Assert.Equal("solo-correo@ficticio.mx", usuario.Email);
        Assert.Equal(selloAnterior, usuario.SecurityStamp);
        var evento = Assert.Single(await contexto.EventosAuditoria.ToListAsync());
        Assert.Equal(objetivo, evento.IdRegistro);
        Assert.Equal(new[] { "Correo" }, evento.CamposAfectados);
    }

    /// Si la auditoría no se puede guardar, el nombre y el correo quedan como estaban.
    /// No queda ningún cambio a medias.
    [Fact]
    public async Task Fallo_De_Evento_Revierte_Nombre_Y_Correo()
    {
        // Arrange
        var escenario = await postgres.Crear();
        using var servicios = escenario.Servicios();
        var objetivo = await CrearAsync(servicios, "objetivo-ficticio");
        var cookie = await PrepararSuperusuarioAsync(servicios);
        using var solicitud = new SolicitudIdentidad(servicios, cookie);
        Assert.True(await solicitud.AutenticarAsync());
        var anterior = await EstadoAsync(escenario);

        // Simula que la base de datos no puede guardar la auditoría.

        await SimularFalloDeAuditoriaAsync(escenario);

        // Act + Assert
        await Assert.ThrowsAsync<DbUpdateException>(() => solicitud.Obtener<IServicioUsuarios>()
            .ActualizarDatosDeCuentaAsync(objetivo, "Nombre Nuevo Ficticio", "nuevo@ficticio.mx"));

        // Assert
        await ComprobarSinCambiosAsync(escenario, anterior);
    }


    // ──── CAMBIO EN LA BASE DE DATOS ───────────────────────────────────────────────────────


    /// Deshacer y rehacer el cambio que agregó el nombre completo a la base de datos no borra al usuario.
    /// Al rehacerlo, el nombre completo queda vacío.
    [Fact]
    public async Task Migracion_Revierte_Y_Reaplica_Sin_Perder_Usuario()
    {
        // Arrange
        var escenario = await postgres.Crear();
        using var servicios = escenario.Servicios();
        var id = await CrearAsync(servicios, "operador-ficticio", "Superusuario",
            nombreCompleto: "Nombre Ficticio");
        var anterior = await PostgreSqlRevision.Valor<string>(escenario.App,
            $"""SELECT (to_jsonb(u)-'NombreCompleto')::text FROM "AspNetUsers" u WHERE "Id"='{id}'""");

        // Conexión con el usuario que sí puede cambiar la estructura de la base de datos.

        await using var migrador = new ContextoDeDatos(new DbContextOptionsBuilder<ContextoDeDatos>()
            .UseNpgsql(escenario.Migrador).Options);

        // Act: vuelve a la versión sin la columna y después rehace el cambio
        await migrador.GetService<IMigrator>().MigrateAsync("20261003202126_IdentidadYAcceso");
        var sinColumna = await PostgreSqlRevision.Valor<long>(escenario.App,
            """
            SELECT count(*) FROM information_schema.columns
            WHERE table_name='AspNetUsers' AND column_name='NombreCompleto'
            """);
        await migrador.Database.MigrateAsync();

        // Assert
        Assert.Equal(0L, sinColumna);
        var posterior = await PostgreSqlRevision.Valor<string>(escenario.App,
            $"""SELECT (to_jsonb(u)-'NombreCompleto')::text FROM "AspNetUsers" u WHERE "Id"='{id}'""");
        Assert.True(anterior == posterior, "La migración alteró los datos anteriores del usuario.");
        Assert.Equal(string.Empty, await PostgreSqlRevision.Valor<string>(escenario.App,
            $"""SELECT "NombreCompleto" FROM "AspNetUsers" WHERE "Id"='{id}'"""));
    }

}
