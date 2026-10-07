using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using TherapEase.Application.Compartido.Interfaces.Servicios;
using TherapEase.Application.Identidad.Interfaces.Servicios;
using TherapEase.Infrastructure.Identidad.Entidades;
using TherapEase.Web.Configuracion;
using TherapEase.Web.Seguridad;
using Npgsql;
using Testcontainers.PostgreSql;
using TherapEase.Application.Identidad.Interfaces.Repositorios;
using TherapEase.Domain.Pacientes.Entidades.Enums;
using TherapEase.Domain.Pacientes.Reglas;
using TherapEase.Infrastructure.Configuracion;
using TherapEase.Infrastructure.Data;
using Xunit;

namespace TherapEase.IntegrationTests;

/// <summary>
/// Comprueba contra un PostgreSQL real las protecciones de datos del B00: usuarios de base
/// separados, auditoría que solo admite inserciones, cruces de citas, control de versión,
/// bajas, último superusuario, bloqueo por intentos y permisos de la consulta de auditoría.
/// Son las 11 comprobaciones iniciales de QA; todos los datos son ficticios.
/// </summary>
public class RevisionDatosTests : IClassFixture<PostgreSqlRevision>
{


    // ──── DATOS COMUNES ──────────────────────────────────────────────────────────────────────


    private readonly PostgreSqlRevision _postgres;
    private static readonly Guid Actor = Guid.Parse("10000000-0000-4000-8000-000000000001");
    private static readonly DateTimeOffset Ahora = DateTimeOffset.Parse("2026-10-03T12:00:00Z");
    public RevisionDatosTests(PostgreSqlRevision postgres) => _postgres = postgres;


    // ──── AYUDAS PARA PREPARAR DATOS ─────────────────────────────────────────────────────────


    /// Registra un paciente ficticio vigente usando la regla real del dominio.
    private static async Task<Guid> Paciente(Escenario e)
    {
        await using var contexto = e.Contexto();
        var p = ReglasDePaciente.Registrar(Guid.NewGuid(), "Paciente Ficticio", null, [AmbitoAtencion.Independiente], Actor, Ahora);
        contexto.Pacientes.Add(p);
        await contexto.SaveChangesAsync();
        return p.IdPaciente;
    }


    /// Arma el INSERT de una cita para escribirla directo en la base, sin pasar por la aplicación,
    /// y así comprobar que la propia base rechaza los cruces.
    private static string Cita(Guid paciente, Guid id, string inicio, string fin, string estado = "Agendada") => $"""
        INSERT INTO "Cita" ("IdCita","IdPaciente","Ambito","Inicio","Fin","Estado","EstadoPago","Condicion","Version","FechaAlta","IdUsuarioAlta")
        VALUES ('{id}','{paciente}','Independiente','{inicio}','{fin}','{estado}','Pendiente','Vigente',1,'2026-10-03T12:00:00Z','{Actor}')
        """;


    // ──── BASE Y AUDITORÍA ───────────────────────────────────────────────────────────────────


    /// Las dos migraciones reales se aplican y la aplicación se conecta con su propio usuario,
    /// que no es administrador de PostgreSQL.
    [Fact]
    public async Task Migraciones_Reales_Con_Usuario_Separado()
    {

        // Act: crear la base aplica las migraciones reales

        var e = await _postgres.Crear();

        // Assert

        Assert.Equal(2L, await PostgreSqlRevision.Valor<long>(e.Migrador, "SELECT count(*) FROM \"__EFMigrationsHistory\""));
        Assert.Equal("therapease_app", await PostgreSqlRevision.Valor<string>(e.App, "SELECT current_user"));
        Assert.False(await PostgreSqlRevision.Valor<bool>(e.App, "SELECT rolsuper FROM pg_roles WHERE rolname=current_user"));
    }


    /// La aplicación puede agregar eventos de auditoría, pero no modificarlos, borrarlos ni crear
    /// tablas: la base responde 42501 (permiso denegado).
    [Fact]
    public async Task Auditoria_Insertar_Si_Actualizar_Borrar_Crear_Esquema_No()
    {

        // Arrange

        var e = await _postgres.Crear();
        var id = Guid.NewGuid();

        // Act: insertar un evento sí se permite

        await PostgreSqlRevision.Sql(e.App, $"""
            INSERT INTO "EventoAuditoria" VALUES ('{id}','2026-10-03T12:00:00Z','{Actor}','Paciente','{Guid.NewGuid()}','Alta',ARRAY['Nombre'],'Alta ficticia')
            """);

        // Act + Assert: modificar, borrar o crear tablas se rechaza

        foreach (var sql in new[] { $"UPDATE \"EventoAuditoria\" SET \"Hecho\"='Cambio' WHERE \"IdEvento\"='{id}'", $"DELETE FROM \"EventoAuditoria\" WHERE \"IdEvento\"='{id}'", "CREATE TABLE public.no_permitida(id int)" })
        {
            var error = await Assert.ThrowsAsync<PostgresException>(() => PostgreSqlRevision.Sql(e.App, sql));
            Assert.Equal("42501", error.SqlState);
        }
    }


    // ──── CITAS Y PACIENTES ──────────────────────────────────────────────────────────────────


    /// Dos citas activas que se enciman se rechazan (23P01). Una cita que empieza justo cuando
    /// termina la otra se admite; esa regla de citas seguidas sigue provisional.
    [Fact]
    public async Task Cruce_SQL_Directo_Rechazado_Contiguas_Provisionales_Admitidas()
    {

        // Arrange: un paciente con una cita de 10:00 a 11:00

        var e = await _postgres.Crear(); var p = await Paciente(e);
        await PostgreSqlRevision.Sql(e.App, Cita(p, Guid.NewGuid(), "2026-10-04T10:00:00Z", "2026-10-04T11:00:00Z"));

        // Act + Assert: una cita encimada se rechaza

        var error = await Assert.ThrowsAsync<PostgresException>(() => PostgreSqlRevision.Sql(e.App, Cita(p, Guid.NewGuid(), "2026-10-04T10:30:00Z", "2026-10-04T11:30:00Z")));
        Assert.Equal("23P01", error.SqlState);

        // Act + Assert: una cita seguida (11:00 a 12:00) se admite

        await PostgreSqlRevision.Sql(e.App, Cita(p, Guid.NewGuid(), "2026-10-04T11:00:00Z", "2026-10-04T12:00:00Z"));
    }


    /// Dos reservas del mismo horario al mismo tiempo: solo una se guarda y la otra se rechaza.
    [Fact]
    public async Task Cruce_Concurrente_Una_Reserva_Confirmada()
    {

        // Arrange

        var e = await _postgres.Crear();
        var paciente = await Paciente(e);

        // Act

        var resultados = await ConcurrenciaSqlRevision.EjecutarAsync(e,
            Cita(paciente, Guid.NewGuid(), "2026-10-04T10:00:00Z", "2026-10-04T11:00:00Z"),
            Cita(paciente, Guid.NewGuid(), "2026-10-04T10:00:00Z", "2026-10-04T11:00:00Z"));

        // Assert

        Assert.Equal(new[] { "23P01", "confirmada" }, resultados.Order(StringComparer.Ordinal).ToArray());
        Assert.Equal(1L, await PostgreSqlRevision.Valor<long>(e.App, "SELECT count(*) FROM \"Cita\""));
    }


    /// Dos personas editan el mismo paciente desde la misma versión: el segundo guardado se
    /// rechaza en lugar de sobrescribir en silencio el primero (Q07).
    [Fact]
    public async Task Version_EF_Impide_Sobrescribir()
    {

        // Arrange: dos ediciones de la misma versión; la primera ya se guardó

        var e = await _postgres.Crear(); var id = await Paciente(e);
        await using var a = e.Contexto(); await using var b = e.Contexto();
        var pa = await a.Pacientes.SingleAsync(p => p.IdPaciente == id); var pb = await b.Pacientes.SingleAsync(p => p.IdPaciente == id);
        ReglasDePaciente.Actualizar(pa,"Primera edicion ficticia",null,1,Actor,Ahora); await a.SaveChangesAsync();
        ReglasDePaciente.Actualizar(pb,"Segunda edicion ficticia",null,1,Actor,Ahora);

        // Act + Assert: la segunda se rechaza

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => b.SaveChangesAsync());
    }


    /// Dar de baja no borra al paciente; al recuperarlo se limpian los datos de baja y sube la versión.
    [Fact]
    public async Task Baja_Conserva_Paciente_Recuperacion_Limpia_Baja()
    {

        // Arrange

        var e = await _postgres.Crear(); var id = await Paciente(e);
        await using var c = e.Contexto(); var p = await c.Pacientes.SingleAsync(p => p.IdPaciente == id);

        // Act: dar de baja

        ReglasDePaciente.DarDeBaja(p,1,Actor,Ahora); await c.SaveChangesAsync();

        // Assert: el paciente sigue en la base

        Assert.Equal(1L,await PostgreSqlRevision.Valor<long>(e.App,"SELECT count(*) FROM \"Paciente\""));

        // Act: recuperar

        ReglasDePaciente.Recuperar(p,2,Actor,Ahora); await c.SaveChangesAsync();

        // Assert: se limpió la baja y subió la versión

        Assert.Null(p.FechaBaja); Assert.Equal(3,p.Version);
    }


    // ──── IDENTIDAD Y ACCESO ─────────────────────────────────────────────────────────────────


    /// La base impide desactivar al único superusuario activo (TE002), aunque se intente por SQL.
    [Fact]
    public async Task Ultimo_Superusuario_No_Se_Desactiva()
    {

        // Arrange

        var e = await _postgres.Crear();
        using var proveedor=e.Servicios(); using var scope=proveedor.CreateScope();
        var gestor=scope.ServiceProvider.GetRequiredService<IGestorIdentidad>(); var id=Guid.NewGuid();
        await gestor.CrearAsync(id,"super-ficticio","Superusuario","Ficticio-Password-Revision",false);

        // Act + Assert

        var error=await Assert.ThrowsAsync<PostgresException>(() => PostgreSqlRevision.Sql(e.App,$"UPDATE \"AspNetUsers\" SET \"Activo\"=false WHERE \"Id\"='{id}'"));
        Assert.Equal("TE002",error.SqlState);
    }


    /// Con dos superusuarios, desactivar a ambos al mismo tiempo deja siempre a uno activo.
    [Fact]
    public async Task Dos_Superusuarios_Desactivacion_Concurrente_Conserva_Uno()
    {

        // Arrange

        var e = await _postgres.Crear();
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        using (var proveedor = e.Servicios())
        using (var alcance = proveedor.CreateScope())
        {
            var gestor = alcance.ServiceProvider.GetRequiredService<IGestorIdentidad>();
            Assert.Equal(TherapEase.Application.Identidad.Modelos.ResultadoCreacionDeUsuario.Creado,
                await gestor.CrearAsync(a, "super-a", "Superusuario", "Ficticio-Password-Revision", false));
            Assert.Equal(TherapEase.Application.Identidad.Modelos.ResultadoCreacionDeUsuario.Creado,
                await gestor.CrearAsync(b, "super-b", "Superusuario", "Ficticio-Password-Revision", false));
        }

        // Act

        var resultados = await ConcurrenciaSqlRevision.EjecutarAsync(e,
            $"UPDATE \"AspNetUsers\" SET \"Activo\"=false WHERE \"Id\"='{a}'",
            $"UPDATE \"AspNetUsers\" SET \"Activo\"=false WHERE \"Id\"='{b}'");

        // Assert

        Assert.Equal(new[] { "TE002", "confirmada" }, resultados.Order(StringComparer.Ordinal).ToArray());
        Assert.Equal(1L, await PostgreSqlRevision.Valor<long>(e.App, "SELECT count(*) FROM \"AspNetUsers\" WHERE \"Activo\""));
    }


    /// Cinco intentos fallidos simultáneos bloquean la cuenta; después ni la contraseña correcta entra.
    [Fact]
    public async Task Fallos_Concurrentes_Cinco_Bloquean_Contrasena_Correcta()
    {

        // Arrange

        var e=await _postgres.Crear();
        using(var proveedor=e.Servicios()) using(var scope=proveedor.CreateScope())
            await scope.ServiceProvider.GetRequiredService<IGestorIdentidad>().CrearAsync(Guid.NewGuid(),"usuario-ficticio","Usuaria","Ficticio-Password-Revision",false);

        // Cada intento usa su propia conexión y transacción, como dos navegadores distintos.

        async Task Fallar()
        {
            using var proveedor=e.Servicios(); using var scope=proveedor.CreateScope();
            var c=scope.ServiceProvider.GetRequiredService<ContextoDeDatos>();
            await using var t=await c.Database.BeginTransactionAsync();
            Assert.Null(await scope.ServiceProvider.GetRequiredService<IGestorIdentidad>().VerificarCredencialesAsync("usuario-ficticio","Incorrecta-Ficticia"));
            await t.CommitAsync();
        }

        // Act: cinco intentos fallidos a la vez

        await Task.WhenAll(Enumerable.Range(0,5).Select(_=>Fallar()));

        // Act + Assert: la contraseña correcta tampoco entra

        using var p=e.Servicios(); using var s=p.CreateScope(); var contexto=s.ServiceProvider.GetRequiredService<ContextoDeDatos>();
        await using var tx=await contexto.Database.BeginTransactionAsync();
        Assert.Null(await s.ServiceProvider.GetRequiredService<IGestorIdentidad>().VerificarCredencialesAsync("usuario-ficticio","Ficticio-Password-Revision"));
        await tx.CommitAsync();

        // Assert: la cuenta quedó bloqueada

        Assert.True(await PostgreSqlRevision.Valor<bool>(e.App,"SELECT \"LockoutEnd\">CURRENT_TIMESTAMP FROM \"AspNetUsers\""));
    }


    /// Consulta de auditoría por HTTP con el servidor real: 401 sin sesión, 403 para Usuaria o con
    /// contraseña temporal, 200 para superusuario, 400 si la fecha no trae zona horaria, y 401
    /// otra vez después de revocar la sesión o desactivar al usuario.
    [Fact]
    public async Task Auditoria_Http_Cookie_Protegida_Permisos_Temporal_Revocacion()
    {

        // Arrange

        var e=await _postgres.Crear();
        var servicios=new ServiceCollection(); servicios.AddLogging();
        servicios.AgregarPersistencia(e.App); servicios.AgregarIdentidad();
        servicios.AgregarServiciosDeAplicacion(); servicios.AgregarSeguridadWeb();
        using var proveedor=servicios.BuildServiceProvider();
        using var scope=proveedor.CreateScope();
        var gestor=scope.ServiceProvider.GetRequiredService<IGestorIdentidad>();
        var super=Guid.NewGuid(); var normal=Guid.NewGuid(); var temporal=Guid.NewGuid();
        await gestor.CrearAsync(super,"super-http","Superusuario","Ficticio-Password-Revision",false);
        await gestor.CrearAsync(normal,"normal-http","Usuaria","Ficticio-Password-Revision",false);
        await gestor.CrearAsync(temporal,"temporal-http","Superusuario","Ficticio-Password-Revision",true);
        async Task<string> Cookie(Guid id)
        {

            // Cookie válida generada con el formato real de Identity; no simula el formulario de login ni TLS.

            using var s=proveedor.CreateScope();
            var usuario=await s.ServiceProvider.GetRequiredService<UserManager<Usuario>>().FindByIdAsync(id.ToString());
            var principal=await s.ServiceProvider.GetRequiredService<IUserClaimsPrincipalFactory<Usuario>>().CreateAsync(usuario!);
            var opciones=s.ServiceProvider.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>().Get(IdentityConstants.ApplicationScheme);
            var propiedades=new AuthenticationProperties{IssuedUtc=DateTimeOffset.UtcNow,ExpiresUtc=DateTimeOffset.UtcNow.AddMinutes(5)};
            return "TherapEase.Sesion="+opciones.TicketDataFormat.Protect(new AuthenticationTicket(principal,propiedades,IdentityConstants.ApplicationScheme));
        }
        await using var servidor = await ServidorRevision.IniciarAsync(e);
        const string ruta = "/api/auditoria/eventos?desde=2026-10-01T00:00:00Z&hasta=2026-10-04T00:00:00Z";

        // Act + Assert: cada tipo de acceso recibe su código

        Assert.Equal(401, await servidor.EstadoAsync(null, ruta));
        Assert.Equal(403, await servidor.EstadoAsync(await Cookie(normal), ruta));
        Assert.Equal(403, await servidor.EstadoAsync(await Cookie(temporal), ruta));
        var cookieSuper = await Cookie(super);
        Assert.Equal(200, await servidor.EstadoAsync(cookieSuper, ruta));
        Assert.Equal(400, await servidor.EstadoAsync(cookieSuper,
            "/api/auditoria/eventos?desde=2026-10-01T00:00:00&hasta=2026-10-04T00:00:00Z"));

        // Act: revocar la sesión

        await gestor.RevocarSesionesAsync(super);

        // Assert

        Assert.Equal(401, await servidor.EstadoAsync(cookieSuper, ruta));

        // Act: desactivar al usuario

        var cookieNueva = await Cookie(super);
        await gestor.DesactivarAsync(super);

        // Assert

        Assert.Equal(401, await servidor.EstadoAsync(cookieNueva, ruta));
    }


    // ──── ATOMICIDAD ─────────────────────────────────────────────────────────────────────────


    /// Si falla el registro de auditoría, el alta del usuario también se deshace: no queda
    /// un cambio guardado sin su evento (Q03, Q08).
    [Fact]
    public async Task Servicio_Usuarios_Revierte_Alta_Si_Falla_Auditoria_Real()
    {

        // Arrange

        var e=await _postgres.Crear();var actor=Guid.NewGuid();
        var servicios=new ServiceCollection();servicios.AddLogging();
        servicios.AgregarPersistencia(e.App);servicios.AgregarIdentidad();servicios.AgregarServiciosDeAplicacion();
        servicios.AddSingleton<IUsuarioActual>(new UsuarioRevision(actor));
        using var proveedor=servicios.BuildServiceProvider();
        using(var s=proveedor.CreateScope()) await s.ServiceProvider.GetRequiredService<IGestorIdentidad>().CrearAsync(actor,"actor-ficticio","Superusuario","Ficticio-Password-Revision",false);

        // Falla provocada solo en esta base de prueba: toda inserción de auditoría da error.

        await PostgreSqlRevision.Sql(e.Migrador,"""
            CREATE FUNCTION fallo_auditoria_qa() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN RAISE EXCEPTION 'Fallo ficticio QA'; END $$;
            CREATE TRIGGER fallo_auditoria_qa BEFORE INSERT ON "EventoAuditoria" FOR EACH ROW EXECUTE FUNCTION fallo_auditoria_qa();
            """);

        // Act + Assert: el alta falla

        using(var s=proveedor.CreateScope())
            await Assert.ThrowsAsync<DbUpdateException>(()=>s.ServiceProvider.GetRequiredService<IServicioUsuarios>().CrearUsuarioAsync("nuevo-ficticio","Usuaria"));

        // Assert: no quedó el usuario ni su evento

        Assert.Equal(0L,await PostgreSqlRevision.Valor<long>(e.App,"SELECT count(*) FROM \"AspNetUsers\" WHERE \"UserName\"='nuevo-ficticio'"));
        Assert.Equal(0L,await PostgreSqlRevision.Valor<long>(e.App,"SELECT count(*) FROM \"EventoAuditoria\""));
    }

    // Usuario actual fijo, para ejecutar el servicio como el superusuario ficticio.

    /// <summary>
    /// Representa al usuario ficticio que realiza el cambio durante esta prueba.
    /// </summary>
    private sealed class UsuarioRevision(Guid id) : IUsuarioActual
    {
        public Guid? IdUsuario=>id;
    }
}
