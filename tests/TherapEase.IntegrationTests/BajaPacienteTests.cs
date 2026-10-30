using System.Data.Common;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using TherapEase.Application.Coordinadores.Interfaces.Servicios;
using TherapEase.Application.Identidad.Interfaces.Repositorios;
using TherapEase.Application.Identidad.Interfaces.Servicios;
using TherapEase.Application.Identidad.Modelos;
using TherapEase.Domain.Citas.Reglas;
using TherapEase.Domain.Compartido.Entidades.Enums;
using TherapEase.Domain.Pacientes.Entidades.Enums;
using TherapEase.Domain.Pacientes.Reglas;
using TherapEase.Infrastructure.Data;

namespace TherapEase.IntegrationTests;

/// <summary>
/// Revisa la baja de pacientes con los servicios y PostgreSQL reales del PR.
/// Usa datos ficticios y conexiones separadas para comprobar qué quedó guardado.
/// </summary>
public sealed class BajaPacienteTests(PostgreSqlRevision postgres) : IClassFixture<PostgreSqlRevision>
{


    // ──── GUARDADO Y RECHAZOS ──────────────────────────────────────────────────────────────


    /// Usuaria y Superusuario pueden dar de baja; una contraseña temporal lo impide sin cambiar datos.
    [Theory]
    [InlineData("Usuaria", false)]
    [InlineData("Superusuario", false)]
    [InlineData("Usuaria", true)]
    [InlineData("Superusuario", true)]
    public async Task Baja_Respeta_Rol_Y_Restriccion_De_Temporal(string rol, bool temporal)
    {
        // Arrange
        using var p = await PrepararAsync(rol: rol, temporal: temporal);
        using var solicitud = p.Solicitud();
        Assert.True(await solicitud.AutenticarAsync());

        // Act
        var respuesta = await solicitud.Obtener<ICoordinadorDeBajaDePaciente>().DarDeBajaAsync(p.Paciente, 1);

        // Assert
        Assert.Equal(!temporal, respuesta.Exito);
        await ComprobarAsync(p, temporal ? CondicionRegistro.Vigente : CondicionRegistro.Baja,
            temporal ? 1 : 2, temporal ? 0 : 1);
    }

    /// Sin sesión no se permite la baja y no queda un evento de un cambio rechazado.
    [Fact]
    public async Task Baja_Sin_Sesion_No_Cambia_Paciente_Ni_Auditoria()
    {
        // Arrange
        using var p = await PrepararAsync();
        using var solicitud = new SolicitudIdentidad(p.Proveedor);
        Assert.False(await solicitud.AutenticarAsync());

        // Act
        var respuesta = await solicitud.Obtener<ICoordinadorDeBajaDePaciente>().DarDeBajaAsync(p.Paciente, 1);

        // Assert
        Assert.False(respuesta.Exito);
        await ComprobarAsync(p, CondicionRegistro.Vigente, 1, 0);
    }

    /// La baja guarda una versión nueva y un solo evento; las citas inactivas no la impiden.
    [Theory]
    [InlineData("ninguna")]
    [InlineData("cancelada")]
    [InlineData("baja")]
    public async Task Baja_Confirmada_Con_Auditoria_Y_Sin_Citas_Activas(string tipoCita)
    {
        // Arrange
        using var p = await PrepararAsync();
        if (tipoCita != "ninguna") await AgregarCitaAsync(p, tipoCita);
        using var solicitud = p.Solicitud();
        Assert.True(await solicitud.AutenticarAsync());

        // Act
        var respuesta = await solicitud.Obtener<ICoordinadorDeBajaDePaciente>().DarDeBajaAsync(p.Paciente, 1);

        // Assert
        Assert.True(respuesta.Exito);
        Assert.Equal(2, respuesta.Datos!.Version);
        await ComprobarAsync(p, CondicionRegistro.Baja, 2, 1);
        await using var lectura = p.Escenario.Contexto();
        var evento = await lectura.EventosAuditoria.SingleAsync(x => x.IdRegistro == p.Paciente);
        Assert.Equal(p.Actor, evento.IdUsuarioActor);
        Assert.Equal(new[] { "Condicion", "FechaBaja", "IdUsuarioBaja" }, evento.CamposAfectados);
        Assert.DoesNotContain("Paciente ficticio de revisión", evento.Hecho);
    }

    /// Una cita activa impide la baja y no genera eventos de un cambio que no se hizo.
    [Fact]
    public async Task Cita_Activa_Rechaza_Baja_Sin_Cambios()
    {
        // Arrange
        using var p = await PrepararAsync();
        await AgregarCitaAsync(p, "activa");
        using var solicitud = p.Solicitud();
        Assert.True(await solicitud.AutenticarAsync());

        // Act
        var respuesta = await solicitud.Obtener<ICoordinadorDeBajaDePaciente>().DarDeBajaAsync(p.Paciente, 1);

        // Assert
        Assert.False(respuesta.Exito);
        await ComprobarAsync(p, CondicionRegistro.Vigente, 1, 0);
    }

    /// Si se agenda después de consultar las citas, PostgreSQL rechaza la baja y conserva al paciente vigente.
    [Fact]
    public async Task Cita_Agregada_Entre_Consulta_Y_Guardado_Impide_Baja()
    {
        // Arrange
        var corte = new AntesDeOperacionRevision();
        using var p = await PrepararAsync(corte);
        using var solicitud = p.Solicitud();
        Assert.True(await solicitud.AutenticarAsync());
        corte.Accion = () => AgregarCitaAsync(p, "activa");

        // Act
        var respuesta = await solicitud.Obtener<ICoordinadorDeBajaDePaciente>().DarDeBajaAsync(p.Paciente, 1);

        // Assert
        Assert.True(corte.Ejecutado);
        Assert.False(respuesta.Exito);
        await ComprobarAsync(p, CondicionRegistro.Vigente, 1, 0);
        await using var lectura = p.Escenario.Contexto();
        Assert.Equal(1, await lectura.Citas.CountAsync());
    }

    /// Una versión guardada por otra conexión no se sobrescribe con la baja desactualizada.
    [Fact]
    public async Task Version_Cambiada_Entre_Lectura_Y_Guardado_No_Se_Sobrescribe()
    {
        // Arrange
        var corte = new AntesDeOperacionRevision();
        using var p = await PrepararAsync(corte);
        using var solicitud = p.Solicitud();
        Assert.True(await solicitud.AutenticarAsync());
        corte.Accion = async () => await PostgreSqlRevision.Sql(p.Escenario.App,
            $"UPDATE \"Paciente\" SET \"Version\"=2 WHERE \"IdPaciente\"='{p.Paciente}'");

        // Act
        var respuesta = await solicitud.Obtener<ICoordinadorDeBajaDePaciente>().DarDeBajaAsync(p.Paciente, 1);

        // Assert
        Assert.True(corte.Ejecutado);
        Assert.False(respuesta.Exito);
        await ComprobarAsync(p, CondicionRegistro.Vigente, 2, 0);
    }

    /// Si no se puede insertar el evento, tampoco se guarda la baja del paciente.
    [Fact]
    public async Task Fallo_De_Auditoria_Revierte_Baja()
    {
        // Arrange
        using var p = await PrepararAsync();
        using var solicitud = p.Solicitud();
        Assert.True(await solicitud.AutenticarAsync());
        await PostgreSqlRevision.Sql(p.Escenario.Migrador, "REVOKE INSERT ON \"EventoAuditoria\" FROM therapease_app");
        try
        {
            // Act + Assert
            var error = await Assert.ThrowsAsync<DbUpdateException>(() =>
                solicitud.Obtener<ICoordinadorDeBajaDePaciente>().DarDeBajaAsync(p.Paciente, 1));
            Assert.Equal("42501", Assert.IsType<PostgresException>(error.InnerException).SqlState);
            await ComprobarAsync(p, CondicionRegistro.Vigente, 1, 0);
        }
        finally
        {
            await PostgreSqlRevision.Sql(p.Escenario.Migrador, "GRANT INSERT ON \"EventoAuditoria\" TO therapease_app");
        }
    }

    /// Una respuesta perdida después de confirmar deja un solo cambio y un solo evento aunque se repita la solicitud.
    [Fact]
    public async Task Respuesta_Perdida_Y_Nueva_Solicitud_No_Duplican_Baja()
    {
        // Arrange
        var corte = new CorteGuardadoRevision(postgres.Contenedor.GetConnectionString());
        using var p = await PrepararAsync(corte);
        using (var solicitud = p.Solicitud())
        {
            Assert.True(await solicitud.AutenticarAsync());
            corte.Activar("despues");

            // Act + Assert
            await Assert.ThrowsAsync<IOException>(() =>
                solicitud.Obtener<ICoordinadorDeBajaDePaciente>().DarDeBajaAsync(p.Paciente, 1));
        }
        await ComprobarAsync(p, CondicionRegistro.Baja, 2, 1);
        using var siguiente = p.Solicitud();
        Assert.True(await siguiente.AutenticarAsync());

        // Act
        var repetida = await siguiente.Obtener<ICoordinadorDeBajaDePaciente>().DarDeBajaAsync(p.Paciente, 1);

        // Assert
        Assert.False(repetida.Exito);
        await ComprobarAsync(p, CondicionRegistro.Baja, 2, 1);
    }


    // ──── CAMBIOS SIMULTÁNEOS ──────────────────────────────────────────────────────────────


    /// Una cita espera mientras se confirma la baja y después es rechazada; nunca queda activa con paciente de baja.
    [Fact]
    public async Task Baja_Primero_Bloquea_Y_Rechaza_Cita_Simultanea()
    {
        // Arrange
        var corte = new CorteGuardadoRevision(postgres.Contenedor.GetConnectionString());
        using var p = await PrepararAsync(corte);
        using var solicitud = p.Solicitud();
        Assert.True(await solicitud.AutenticarAsync());
        corte.Activar("esperar");
        var baja = solicitud.Obtener<ICoordinadorDeBajaDePaciente>().DarDeBajaAsync(p.Paciente, 1);
        Task<Exception?>? cita = null;
        try
        {
            await corte.Alcanzado.Task.WaitAsync(TimeSpan.FromSeconds(10));
            await using var otra = p.Escenario.Contexto();
            await otra.Database.OpenConnectionAsync();
            var pid = ((NpgsqlConnection)otra.Database.GetDbConnection()).ProcessID;
            otra.Citas.Add(ReglasDeCita.Agendar(Guid.NewGuid(), p.Paciente, AmbitoAtencion.Independiente,
                DateTimeOffset.UtcNow.AddDays(1), DateTimeOffset.UtcNow.AddDays(1).AddHours(1), p.Actor, DateTimeOffset.UtcNow));

            // Act
            cita = Record.ExceptionAsync(() => otra.SaveChangesAsync());
            var tiempo = Stopwatch.StartNew();
            var bloqueada = false;
            while (!cita.IsCompleted && tiempo.Elapsed < TimeSpan.FromSeconds(5))
            {
                bloqueada = await PostgreSqlRevision.Valor<bool>(p.Escenario.App,
                    $"SELECT EXISTS(SELECT 1 FROM pg_stat_activity WHERE pid={pid} AND wait_event_type='Lock')");
                if (bloqueada) break;
                await Task.Delay(20);
            }
            corte.Continuar.TrySetResult();

            // Assert
            Assert.True(bloqueada, "La cita debe esperar el guardado de la baja.");
            Assert.True((await baja.WaitAsync(TimeSpan.FromSeconds(10))).Exito);
            var error = Assert.IsType<DbUpdateException>(await cita.WaitAsync(TimeSpan.FromSeconds(10)));
            Assert.Equal("TE001", Assert.IsType<PostgresException>(error.InnerException).SqlState);
            await ComprobarAsync(p, CondicionRegistro.Baja, 2, 1);
            await using var lectura = p.Escenario.Contexto();
            Assert.Equal(0, await lectura.Citas.CountAsync());
        }
        finally
        {
            corte.Continuar.TrySetResult();
            try { await baja.WaitAsync(TimeSpan.FromSeconds(12)); } catch { }
            if (cita is not null) try { await cita.WaitAsync(TimeSpan.FromSeconds(12)); } catch { }
        }
    }


    // ──── PREPARACIÓN Y CONSULTAS ──────────────────────────────────────────────────────────


    /// Crea una cuenta y un paciente ficticios; las interrupciones se activan después de entrar.
    private async Task<Preparacion> PrepararAsync(DbTransactionInterceptor? corte = null,
        string rol = "Superusuario", bool temporal = false)
    {
        var e = await postgres.Crear();
        var proveedor = e.Servicios(servicios =>
        {
            if (corte is not null) servicios.AddDbContext<ContextoDeDatos>(opciones => opciones.AddInterceptors(corte));
        });
        try
        {
            var actor = Guid.NewGuid();
            using (var alcance = proveedor.CreateScope())
            {
                Assert.Equal(ResultadoCreacionDeUsuario.Creado,
                    await alcance.ServiceProvider.GetRequiredService<IGestorIdentidad>()
                        .CrearAsync(actor, "operador-ficticio-cu04", rol, "contrasenaficticiadecu04", temporal));
            }
            string cookie;
            using (var acceso = new SolicitudIdentidad(proveedor))
            {
                Assert.True((await acceso.Obtener<IServicioAcceso>()
                    .IniciarSesionAsync("operador-ficticio-cu04", "contrasenaficticiadecu04")).Exito);
                cookie = acceso.CookieEmitida();
            }
            var paciente = Guid.NewGuid();
            await using var datos = e.Contexto();
            datos.Pacientes.Add(ReglasDePaciente.Registrar(paciente, PacientesDePrueba.Adulto("Paciente ficticio de revisión"),
                [AmbitoAtencion.Independiente], actor, DateTimeOffset.UtcNow));
            await datos.SaveChangesAsync();
            return new Preparacion(e, proveedor, cookie, actor, paciente);
        }
        catch
        {
            proveedor.Dispose();
            throw;
        }
    }

    /// Inserta una cita desde otra conexión y aplica su estado antes de guardarla.
    private static async Task AgregarCitaAsync(Preparacion p, string tipo)
    {
        await using var datos = p.Escenario.Contexto();
        var cita = ReglasDeCita.Agendar(Guid.NewGuid(), p.Paciente, AmbitoAtencion.Independiente,
            DateTimeOffset.UtcNow.AddDays(1), DateTimeOffset.UtcNow.AddDays(1).AddHours(1), p.Actor, DateTimeOffset.UtcNow);
        if (tipo == "cancelada") ReglasDeCita.Cancelar(cita, 1, p.Actor, DateTimeOffset.UtcNow);
        if (tipo == "baja") ReglasDeCita.DarDeBaja(cita, 1, p.Actor, DateTimeOffset.UtcNow);
        datos.Citas.Add(cita);
        await datos.SaveChangesAsync();
    }

    /// Lee desde otro contexto para comprobar el estado real, sin usar los datos que el servicio mantiene en memoria.
    private static async Task ComprobarAsync(Preparacion p, CondicionRegistro condicion, int version, int eventos)
    {
        await using var lectura = p.Escenario.Contexto();
        var paciente = await lectura.Pacientes.SingleAsync(x => x.IdPaciente == p.Paciente);
        Assert.Equal(condicion, paciente.Condicion);
        Assert.Equal(version, paciente.Version);
        Assert.Equal(eventos, await lectura.EventosAuditoria.CountAsync(x => x.IdRegistro == p.Paciente));
    }

    /// <summary>Conserva los servicios y la sesión ficticia de una prueba; libera los servicios al terminar.</summary>
    private sealed record Preparacion(Escenario Escenario, ServiceProvider Proveedor, string Cookie, Guid Actor, Guid Paciente) : IDisposable
    {
        /// Abre la solicitud en la misma ejecución que usará sus servicios; la prueba valida después su cookie.
        public SolicitudIdentidad Solicitud() => new(Proveedor, Cookie);

        public void Dispose() => Proveedor.Dispose();
    }

    /// <summary>Introduce un cambio desde otra conexión después de las consultas previas a la baja.</summary>
    private sealed class AntesDeOperacionRevision : DbTransactionInterceptor
    {
        public Func<Task>? Accion { get; set; }
        public bool Ejecutado { get; private set; }

        /// Ejecuta una sola interrupción al iniciar la transacción real de la operación.
        public override async ValueTask<DbTransaction> TransactionStartedAsync(DbConnection connection,
            TransactionEndEventData eventData, DbTransaction result, CancellationToken cancellationToken = default)
        {
            var accion = Accion;
            Accion = null;
            if (accion is not null)
            {
                await accion();
                Ejecutado = true;
            }
            return result;
        }
    }
}
