using System.Collections.Concurrent;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using TherapEase.Application.Compartido.Interfaces.Servicios;
using TherapEase.Application.Coordinadores.Interfaces.Servicios;
using TherapEase.Application.Pacientes.Constantes;
using TherapEase.Application.Pacientes.Interfaces.Repositorios;
using TherapEase.Application.Pacientes.Interfaces.Servicios;
using TherapEase.Application.Pacientes.Modelos;
using TherapEase.Domain.Auditoria.Entidades;
using TherapEase.Domain.Auditoria.Entidades.Enums;
using TherapEase.Domain.Compartido.Entidades.Enums;
using TherapEase.Domain.Pacientes.Constantes;
using TherapEase.Domain.Pacientes.Entidades;
using TherapEase.Domain.Pacientes.Entidades.Enums;
using TherapEase.Domain.Pacientes.Reglas;
using TherapEase.Infrastructure.Configuracion;
using TherapEase.Infrastructure.Data;
using TherapEase.Web.Configuracion;
using static TherapEase.IntegrationTests.ServidorIdentidadRevision;

namespace TherapEase.IntegrationTests;

/// <summary>
/// Comprueba con los servicios reales y un PostgreSQL desechable el registro, la búsqueda, la consulta y la edición de fichas:
/// guardado y evento en una sola transacción, permisos de la matriz real, reenvío y carreras del mismo alta, conflicto de versión,
/// ámbitos que se quitan, una sola consulta al abrir una ficha y restricciones de la base. Todos los datos son ficticios.
/// </summary>
public class PacientesServidorTests(PostgreSqlRevision postgres) : IClassFixture<PostgreSqlRevision>
{
    private static readonly DateOnly NacimientoAdulto = PacientesDePrueba.NacimientoDeAdulto;
    // Diez anios cumplidos hoy en Hermosillo, que es la fecha con la que el sistema calcula la edad; evita depender de la hora UTC de la ejecucion.
    private static readonly DateOnly NacimientoMenor = ReglasDeEdad.HoyEnHermosillo(DateTimeOffset.UtcNow).AddYears(-10);

    // ──── PREPARACIÓN ────────────────────────────────────────────────────────────────────────

    private sealed class UsuarioActualMutable : IUsuarioActual
    {
        public Guid? IdUsuario { get; set; }
    }

    private sealed class RegistroDeComandos : DbCommandInterceptor
    {
        private readonly ConcurrentQueue<string> _textos = new();

        public IReadOnlyList<string> Textos => [.. _textos];

        public void Limpiar() => _textos.Clear();

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand comando, CommandEventData datos, InterceptionResult<DbDataReader> resultado, CancellationToken cancelacion = default)
        {
            _textos.Enqueue(comando.CommandText);
            return new(resultado);
        }

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand comando, CommandEventData datos, InterceptionResult<int> resultado, CancellationToken cancelacion = default)
        {
            _textos.Enqueue(comando.CommandText);
            return new(resultado);
        }
    }

    // Ejecuta una accion, una sola vez, justo antes del siguiente guardado de una ficha (alta o edicion): simula a otra persona que se adelanta.
    private sealed class AdelantadaAlGuardar : SaveChangesInterceptor
    {
        private Func<Task>? _accion;

        public void Armar(Func<Task> accion) => Volatile.Write(ref _accion, accion);

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData datos, InterceptionResult<int> resultado, CancellationToken cancelacion = default)
        {
            var hayFicha = datos.Context!.ChangeTracker.Entries<Paciente>().Any(e => e.State is EntityState.Added or EntityState.Modified);
            if (hayFicha && Interlocked.Exchange(ref _accion, null) is { } accion)
            {
                await accion();
            }

            return resultado;
        }
    }

    // Ejecuta una acción, una sola vez, justo antes de la consulta de fichas parecidas de un alta: fuerza el momento en que otro envío igual ya se guardó.
    private sealed class AntesDeBuscarParecidos : DbCommandInterceptor
    {
        private Func<Task>? _accion;

        public void Armar(Func<Task> accion) => Volatile.Write(ref _accion, accion);

        public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand comando, CommandEventData datos, InterceptionResult<DbDataReader> resultado, CancellationToken cancelacion = default)
        {
            if (comando.CommandText.Contains("\"NombreNormalizado\" = @", StringComparison.Ordinal) && Interlocked.Exchange(ref _accion, null) is { } accion)
            {
                await accion();
            }

            return resultado;
        }
    }

    private sealed class Entorno(Escenario escenario, ServiceProvider proveedor, UsuarioActualMutable actual, RegistroDeComandos comandos) : IDisposable
    {
        public Escenario Escenario { get; } = escenario;

        public ServiceProvider Proveedor { get; } = proveedor;

        public UsuarioActualMutable Actual { get; } = actual;

        public RegistroDeComandos Comandos { get; } = comandos;

        public Guid Actor => Actual.IdUsuario!.Value;

        public async Task<T> EnAlcance<T>(Func<IServiceProvider, Task<T>> accion)
        {
            using var alcance = Proveedor.CreateScope();
            return await accion(alcance.ServiceProvider);
        }

        public Task<T> Servicio<T>(Func<IServicioPacientes, Task<T>> accion) =>
            EnAlcance(p => accion(p.GetRequiredService<IServicioPacientes>()));

        public async Task<List<Paciente>> PacientesAsync()
        {
            await using var contexto = Escenario.Contexto();
            return await contexto.Pacientes.AsNoTracking().Include(p => p.Ambitos).OrderBy(p => p.NombreNormalizado).ToListAsync();
        }

        public async Task<List<EventoAuditoria>> EventosDePacientesAsync()
        {
            await using var contexto = Escenario.Contexto();
            return await contexto.EventosAuditoria.AsNoTracking()
                .Where(e => e.TipoRegistro == TipoRegistroAuditoria.Paciente)
                .OrderBy(e => e.FechaEvento)
                .ToListAsync();
        }

        public void Dispose() => Proveedor.Dispose();
    }

    private async Task<Entorno> PrepararAsync(string rol = "Superusuario", bool temporal = false, Action<IServiceCollection>? configurar = null)
    {
        var escenario = await postgres.Crear();
        var actual = new UsuarioActualMutable();
        var comandos = new RegistroDeComandos();
        var servicios = new ServiceCollection();
        servicios.AddLogging();
        servicios.AgregarPersistencia(escenario.App);
        servicios.AgregarIdentidad();
        servicios.AgregarServiciosDeAplicacion();
        servicios.AddSingleton<IUsuarioActual>(actual);
        servicios.ConfigureDbContext<ContextoDeDatos>(o => o.AddInterceptors(comandos));
        configurar?.Invoke(servicios);
        var proveedor = servicios.BuildServiceProvider();
        actual.IdUsuario = await CrearAsync(proveedor, "operador-ficticio", rol, temporal);
        return new Entorno(escenario, proveedor, actual, comandos);
    }

    private static DatosDePacienteEntrada Adulto(string nombre = "Paciente Ficticio", params AmbitoAtencion[] ambitos) =>
        new(nombre, NacimientoAdulto, null, "canario@ficticio.test", null, null, ambitos.Length == 0 ? [AmbitoAtencion.Escolar] : ambitos);

    private static DatosDePacienteEntrada Menor() =>
        new("Menor Ficticio", NacimientoMenor, "644 123 4567", null, "Escuela Ficticia", "3A", [AmbitoAtencion.Escolar, AmbitoAtencion.Independiente],
            new DatosDeTutorEntrada("Tutora Ficticia", "Madre", null, "Tutora@Ficticio.test"));

    private static async Task<Guid> RegistrarAsync(Entorno e, DatosDePacienteEntrada datos, bool confirma = false)
    {
        var id = Guid.NewGuid();
        var respuesta = await e.Servicio(s => s.RegistrarAsync(id, datos, confirma));
        Assert.True(respuesta.Exito, respuesta.Mensaje);
        return id;
    }

    // ──── ALTA ───────────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Alta_GuardaLaFichaConTodosSusCamposYUnSoloEventoEnLaMismaTransaccion()
    {
        using var e = await PrepararAsync();
        var id = Guid.NewGuid();

        var respuesta = await e.Servicio(s => s.RegistrarAsync(id, Menor()));

        Assert.True(respuesta.Exito);
        Assert.Equal(EstadoDeAlta.Registrado, respuesta.Datos!.Estado);
        var paciente = Assert.Single(await e.PacientesAsync());
        Assert.Equal(id, paciente.IdPaciente);
        Assert.Equal("Menor Ficticio", paciente.Nombre);
        Assert.Equal("menor ficticio", paciente.NombreNormalizado);
        Assert.Equal(NacimientoMenor, paciente.FechaNacimiento);
        Assert.Equal("6441234567", paciente.Telefono);
        Assert.Null(paciente.Correo);
        Assert.Equal("Escuela Ficticia", paciente.Escuela);
        Assert.Equal("3A", paciente.Grupo);
        Assert.Equal("Tutora Ficticia", paciente.TutorNombre);
        Assert.Equal("Madre", paciente.TutorParentesco);
        Assert.Null(paciente.TutorTelefono);
        Assert.Equal("tutora@ficticio.test", paciente.TutorCorreo);
        Assert.Equal(CondicionRegistro.Vigente, paciente.Condicion);
        Assert.Equal(1, paciente.Version);
        Assert.Equal(e.Actor, paciente.IdUsuarioAlta);
        Assert.Equal(
            [AmbitoAtencion.Escolar, AmbitoAtencion.Independiente],
            paciente.Ambitos.Select(a => a.Ambito).Order());

        var evento = Assert.Single(await e.EventosDePacientesAsync());
        Assert.Equal(AccionAuditoria.Alta, evento.Accion);
        Assert.Equal(id, evento.IdRegistro);
        Assert.Equal(e.Actor, evento.IdUsuarioActor);
        Assert.Contains(CamposDePaciente.TutorNombre, evento.CamposAfectados);
        var texto = string.Join('|', evento.CamposAfectados) + evento.Hecho;
        Assert.DoesNotContain("Tutora", texto);
        Assert.DoesNotContain("6441234567", texto);
    }

    [Theory]
    [InlineData("Usuaria")]
    [InlineData("Superusuario")]
    public async Task LosDosRolesPuedenRegistrarBuscarConsultarYActualizarConLosPermisosReales(string rol)
    {
        using var e = await PrepararAsync(rol);
        var id = await RegistrarAsync(e, Adulto());

        var busqueda = await e.Servicio(s => s.BuscarAsync("paciente"));
        var ficha = await e.Servicio(s => s.ObtenerAsync(id));
        var edicion = await e.Servicio(s => s.ActualizarAsync(id, 1, Adulto("Paciente Editado")));

        Assert.Equal(id, Assert.Single(busqueda.Datos!.Pacientes).IdPaciente);
        Assert.True(ficha.Exito);
        Assert.True(edicion.Exito);
    }

    [Fact]
    public async Task ConContrasenaTemporalPendiente_NingunaOperacionDePacientesEstaPermitida()
    {
        using var e = await PrepararAsync("Usuaria", temporal: true);

        var alta = await e.Servicio(s => s.RegistrarAsync(Guid.NewGuid(), Adulto()));
        var busqueda = await e.Servicio(s => s.BuscarAsync("paciente"));

        Assert.Equal(MensajesDePacientes.SinPermiso, alta.Mensaje);
        Assert.Equal(MensajesDePacientes.SinPermiso, busqueda.Mensaje);
        Assert.Empty(await e.PacientesAsync());
        Assert.Empty(await e.EventosDePacientesAsync());
    }

    [Fact]
    public async Task Alta_ReenviadaDosVecesConElMismoIdentificador_DejaUnaFichaYUnEvento()
    {
        using var e = await PrepararAsync();
        var id = Guid.NewGuid();

        var primera = await e.Servicio(s => s.RegistrarAsync(id, Adulto()));
        var segunda = await e.Servicio(s => s.RegistrarAsync(id, Adulto()));

        Assert.Equal(EstadoDeAlta.Registrado, primera.Datos!.Estado);
        Assert.Equal(EstadoDeAlta.YaRegistrado, segunda.Datos!.Estado);
        Assert.Equal(1, Assert.Single(await e.PacientesAsync()).Version);
        Assert.Single(await e.EventosDePacientesAsync());
    }

    [Fact]
    public async Task Alta_SiOtraSolicitudIgualGuardaPrimeroLaMismaFicha_LaBaseRechazaLaClaveYSeDevuelveYaRegistrado()
    {
        var id = Guid.NewGuid();
        var adelantada = new AdelantadaAlGuardar();
        using var e = await PrepararAsync(configurar: servicios => servicios.ConfigureDbContext<ContextoDeDatos>(o => o.AddInterceptors(adelantada)));
        adelantada.Armar(async () =>
        {
            var competidora = await e.Servicio(s => s.RegistrarAsync(id, Adulto()));
            Assert.Equal(EstadoDeAlta.Registrado, competidora.Datos!.Estado);
        });

        var respuesta = await e.Servicio(s => s.RegistrarAsync(id, Adulto()));

        Assert.True(respuesta.Exito);
        Assert.Equal(EstadoDeAlta.YaRegistrado, respuesta.Datos!.Estado);
        Assert.Equal(id, respuesta.Datos.IdPaciente);
        Assert.Single(await e.PacientesAsync());
        Assert.Single(await e.EventosDePacientesAsync());
    }

    [Fact]
    public async Task Alta_SiOtraSolicitudConOtrosDatosGuardaPrimeroElMismoIdentificador_NoSeReconoceComoReenvio()
    {
        var id = Guid.NewGuid();
        var adelantada = new AdelantadaAlGuardar();
        using var e = await PrepararAsync(configurar: servicios => servicios.ConfigureDbContext<ContextoDeDatos>(o => o.AddInterceptors(adelantada)));
        adelantada.Armar(async () =>
        {
            var competidora = await e.Servicio(s => s.RegistrarAsync(id, Adulto("Otra Persona")));
            Assert.True(competidora.Exito);
        });

        var respuesta = await e.Servicio(s => s.RegistrarAsync(id, Adulto()));

        Assert.False(respuesta.Exito);
        Assert.Equal(MensajesDePacientes.SolicitudYaUsada, respuesta.Mensaje);
        Assert.Equal("Otra Persona", Assert.Single(await e.PacientesAsync()).Nombre);
        Assert.Single(await e.EventosDePacientesAsync());
    }

    [Fact]
    public async Task Alta_SiElMismoEnvioSeGuardaJustoAntesDeBuscarParecidos_SeResponde_YaRegistradoYNoComoParecido()
    {
        var id = Guid.NewGuid();
        var antes = new AntesDeBuscarParecidos();
        using var e = await PrepararAsync(configurar: servicios => servicios.ConfigureDbContext<ContextoDeDatos>(o => o.AddInterceptors(antes)));
        antes.Armar(async () =>
        {
            var otro = await e.Servicio(s => s.RegistrarAsync(id, Adulto()));
            Assert.Equal(EstadoDeAlta.Registrado, otro.Datos!.Estado);
        });

        // Este envío no encontró la ficha al empezar; cuando busca parecidos, el otro ya la guardó y su propia ficha aparecería como parecida.
        var respuesta = await e.Servicio(s => s.RegistrarAsync(id, Adulto()));

        Assert.True(respuesta.Exito, respuesta.Mensaje);
        Assert.Equal(EstadoDeAlta.YaRegistrado, respuesta.Datos!.Estado);
        Assert.Equal(id, respuesta.Datos.IdPaciente);
        Assert.Null(respuesta.Datos.Coincidencias);
        Assert.Single(await e.PacientesAsync());
        Assert.Single(await e.EventosDePacientesAsync());
    }

    [Fact]
    public async Task Alta_ConDosEnviosSimultaneosDelMismoIdentificador_SoloUnoGuardaYElOtroEsYaRegistrado()
    {
        using var e = await PrepararAsync();
        for (var ronda = 0; ronda < 5; ronda++)
        {
            var id = Guid.NewGuid();
            var datos = Adulto($"Persona Simultanea {ronda}");

            var resultados = await Task.WhenAll(
                e.Servicio(s => s.RegistrarAsync(id, datos)),
                e.Servicio(s => s.RegistrarAsync(id, datos)));

            Assert.All(resultados, r => Assert.True(r.Exito, r.Mensaje));
            Assert.Equal(1, resultados.Count(r => r.Datos!.Estado == EstadoDeAlta.Registrado));
            Assert.Equal(1, resultados.Count(r => r.Datos!.Estado == EstadoDeAlta.YaRegistrado));
        }

        Assert.Equal(5, (await e.PacientesAsync()).Count);
        Assert.Equal(5, (await e.EventosDePacientesAsync()).Count);
    }

    [Fact]
    public async Task Alta_SiFallaLaAuditoria_NoQuedaLaFichaNiSeDaExito()
    {
        using var e = await PrepararAsync();
        await SimularFalloDeAuditoriaAsync(e.Escenario);

        await Assert.ThrowsAsync<DbUpdateException>(() => e.Servicio(s => s.RegistrarAsync(Guid.NewGuid(), Adulto())));

        Assert.Empty(await e.PacientesAsync());
        Assert.Empty(await e.EventosDePacientesAsync());
    }

    [Fact]
    public async Task Alta_TrasUnaCarreraDelMismoIdentificador_LaSiguienteAltaDelMismoAlcanceSeGuarda()
    {
        var id = Guid.NewGuid();
        var adelantada = new AdelantadaAlGuardar();
        using var e = await PrepararAsync(configurar: servicios => servicios.ConfigureDbContext<ContextoDeDatos>(o => o.AddInterceptors(adelantada)));
        adelantada.Armar(async () =>
        {
            var competidora = await e.Servicio(s => s.RegistrarAsync(id, Adulto()));
            Assert.Equal(EstadoDeAlta.Registrado, competidora.Datos!.Estado);
        });
        var siguiente = Guid.NewGuid();

        // Las dos llamadas comparten el alcance, como las de una misma solicitud: el intento fallido no debe quedar pendiente en el contexto.
        var (primera, segunda) = await e.EnAlcance(async p =>
        {
            var servicio = p.GetRequiredService<IServicioPacientes>();
            var uno = await servicio.RegistrarAsync(id, Adulto());
            var dos = await servicio.RegistrarAsync(siguiente, Adulto("Otra Persona Distinta"));
            return (uno, dos);
        });

        Assert.Equal(EstadoDeAlta.YaRegistrado, primera.Datos!.Estado);
        Assert.True(segunda.Exito, segunda.Mensaje);
        Assert.Equal(EstadoDeAlta.Registrado, segunda.Datos!.Estado);
        Assert.Equal(new[] { id, siguiente }.Order(), (await e.PacientesAsync()).Select(p => p.IdPaciente).Order());
        Assert.Equal(2, (await e.EventosDePacientesAsync()).Count);
    }

    [Fact]
    public async Task Alta_TrasFallarLaAuditoria_LaSiguienteOperacionDelMismoAlcanceNoRepiteElCambioFallido()
    {
        using var e = await PrepararAsync();
        await SimularFalloDeAuditoriaAsync(e.Escenario);
        var segundo = Guid.NewGuid();

        var segunda = await e.EnAlcance(async p =>
        {
            var servicio = p.GetRequiredService<IServicioPacientes>();
            await Assert.ThrowsAsync<DbUpdateException>(() => servicio.RegistrarAsync(Guid.NewGuid(), Adulto("Primera Ficha Fallida")));
            await PostgreSqlRevision.Sql(e.Escenario.Migrador,
                "DROP TRIGGER rechazar_evento_de_revision ON \"EventoAuditoria\"; DROP FUNCTION rechazar_evento_de_revision();");
            return await servicio.RegistrarAsync(segundo, Adulto("Segunda Ficha Valida"));
        });

        Assert.True(segunda.Exito, segunda.Mensaje);
        var guardadas = await e.PacientesAsync();
        Assert.Equal(segundo, Assert.Single(guardadas).IdPaciente);
        Assert.Equal(segundo, Assert.Single(await e.EventosDePacientesAsync()).IdRegistro);
    }

    [Fact]
    public async Task Alta_ConParecidos_NoGuardaHastaConfirmarYLuegoConservaAmbasFichas()
    {
        using var e = await PrepararAsync();
        var existente = await RegistrarAsync(e, Adulto("María Muñoz"));

        var sinConfirmar = await e.Servicio(s => s.RegistrarAsync(Guid.NewGuid(), Adulto("MARIA MUNOZ")));
        var confirmada = await e.Servicio(s => s.RegistrarAsync(Guid.NewGuid(), Adulto("MARIA MUNOZ"), true));

        Assert.False(sinConfirmar.Exito);
        Assert.Equal(EstadoDeAlta.CoincidenciasPorConfirmar, sinConfirmar.Datos!.Estado);
        Assert.Equal(existente, Assert.Single(sinConfirmar.Datos.Coincidencias!).IdPaciente);
        Assert.True(confirmada.Exito);
        Assert.Equal(2, (await e.PacientesAsync()).Count);
        Assert.Equal(2, (await e.EventosDePacientesAsync()).Count);
    }

    // ──── EDICIÓN ────────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Edicion_GuardaLosCambiosSubeLaVersionYEscribeUnEventoConSoloNombresDeCampos()
    {
        using var e = await PrepararAsync();
        var id = await RegistrarAsync(e, Adulto());

        var respuesta = await e.Servicio(s => s.ActualizarAsync(id, 1, Adulto("Nombre Nuevo") with { Telefono = "6441234567", Escuela = "Escuela Nueva" }));

        Assert.True(respuesta.Exito);
        Assert.Equal(EstadoDeEdicion.Actualizado, respuesta.Datos!.Estado);
        var paciente = Assert.Single(await e.PacientesAsync());
        Assert.Equal("Nombre Nuevo", paciente.Nombre);
        Assert.Equal("nombre nuevo", paciente.NombreNormalizado);
        Assert.Equal(2, paciente.Version);
        Assert.Equal(e.Actor, paciente.IdUsuarioActualizacion);
        Assert.NotNull(paciente.FechaActualizacion);
        var eventos = await e.EventosDePacientesAsync();
        var edicion = eventos.Single(x => x.Accion == AccionAuditoria.Actualizacion);
        Assert.Equal(
            [CamposDePaciente.Escuela, CamposDePaciente.Nombre, CamposDePaciente.Telefono],
            edicion.CamposAfectados.Order(StringComparer.Ordinal));
        Assert.DoesNotContain("Nuevo", string.Join('|', edicion.CamposAfectados) + edicion.Hecho);
    }

    [Fact]
    public async Task Edicion_QuitarYAgregarAmbitosConPostgreSqlReal_ActualizaLasFilasSinTocarElResto()
    {
        using var e = await PrepararAsync();
        var id = await RegistrarAsync(e, Adulto("Paciente Ficticio", AmbitoAtencion.Escolar));

        var cambioDeAmbito = await e.Servicio(s => s.ActualizarAsync(id, 1, Adulto("Paciente Ficticio", AmbitoAtencion.Independiente)));
        var ambosAmbitos = await e.Servicio(s => s.ActualizarAsync(id, 2, Adulto("Paciente Ficticio", AmbitoAtencion.Independiente, AmbitoAtencion.Escolar)));
        var soloEscolar = await e.Servicio(s => s.ActualizarAsync(id, 3, Adulto("Paciente Ficticio", AmbitoAtencion.Escolar)));

        Assert.True(cambioDeAmbito.Exito, cambioDeAmbito.Mensaje);
        Assert.True(ambosAmbitos.Exito, ambosAmbitos.Mensaje);
        Assert.True(soloEscolar.Exito, soloEscolar.Mensaje);
        var paciente = Assert.Single(await e.PacientesAsync());
        Assert.Equal(AmbitoAtencion.Escolar, Assert.Single(paciente.Ambitos).Ambito);
        Assert.Equal(4, paciente.Version);
        Assert.Equal(3, (await e.EventosDePacientesAsync()).Count(x => x.Accion == AccionAuditoria.Actualizacion));
        Assert.Equal(1L, await PostgreSqlRevision.Valor<long>(e.Escenario.App, "SELECT count(*) FROM \"PacienteAmbito\""));
    }

    [Fact]
    public async Task Edicion_SinCambiosConLaVersionVigente_NoEjecutaEscrituraNiAudita()
    {
        using var e = await PrepararAsync();
        var id = await RegistrarAsync(e, Adulto());
        var antes = await e.PacientesAsync();
        e.Comandos.Limpiar();

        var respuesta = await e.Servicio(s => s.ActualizarAsync(id, 1, Adulto() with { Nombre = "  Paciente Ficticio ", Correo = " CANARIO@ficticio.test " }));

        Assert.True(respuesta.Exito);
        Assert.Equal(EstadoDeEdicion.SinCambios, respuesta.Datos!.Estado);
        Assert.DoesNotContain(e.Comandos.Textos, t => t.TrimStart().StartsWith("INSERT", StringComparison.OrdinalIgnoreCase)
            || t.TrimStart().StartsWith("UPDATE", StringComparison.OrdinalIgnoreCase)
            || t.TrimStart().StartsWith("DELETE", StringComparison.OrdinalIgnoreCase));
        var despues = Assert.Single(await e.PacientesAsync());
        Assert.Equal(1, despues.Version);
        Assert.Equal(antes[0].FechaActualizacion, despues.FechaActualizacion);
        Assert.Single(await e.EventosDePacientesAsync());
    }

    [Fact]
    public async Task Edicion_ConUnaVersionAnterior_DaConflictoAunConDatosIguales()
    {
        using var e = await PrepararAsync();
        var id = await RegistrarAsync(e, Adulto());
        await e.Servicio(s => s.ActualizarAsync(id, 1, Adulto("Primera Edicion")));

        var conDatosIguales = await e.Servicio(s => s.ActualizarAsync(id, 1, Adulto("Primera Edicion")));
        var conDatosDistintos = await e.Servicio(s => s.ActualizarAsync(id, 1, Adulto("Segunda Edicion")));

        Assert.Equal(MensajesDePacientes.ConflictoDeVersion, conDatosIguales.Mensaje);
        Assert.Equal(MensajesDePacientes.ConflictoDeVersion, conDatosDistintos.Mensaje);
        Assert.Equal("Primera Edicion", Assert.Single(await e.PacientesAsync()).Nombre);
        Assert.Equal(2, (await e.EventosDePacientesAsync()).Count);
    }

    [Fact]
    public async Task Edicion_SiOtraPersonaGuardaEntreLaLecturaYElGuardado_LaBaseDetectaElConflictoYSoloQuedaLaPrimera()
    {
        var adelantada = new AdelantadaAlGuardar();
        using var e = await PrepararAsync(configurar: servicios => servicios.ConfigureDbContext<ContextoDeDatos>(o => o.AddInterceptors(adelantada)));
        var id = await RegistrarAsync(e, Adulto());
        adelantada.Armar(async () =>
        {
            var competidora = await e.Servicio(s => s.ActualizarAsync(id, 1, Adulto("Edicion Que Llego Primero")));
            Assert.True(competidora.Exito, competidora.Mensaje);
        });

        var respuesta = await e.Servicio(s => s.ActualizarAsync(id, 1, Adulto("Edicion Que Llego Despues")));

        Assert.False(respuesta.Exito);
        Assert.Equal(MensajesDePacientes.ConflictoDeVersion, respuesta.Mensaje);
        var paciente = Assert.Single(await e.PacientesAsync());
        Assert.Equal("Edicion Que Llego Primero", paciente.Nombre);
        Assert.Equal(2, paciente.Version);
        Assert.Equal(1, (await e.EventosDePacientesAsync()).Count(x => x.Accion == AccionAuditoria.Actualizacion));
    }

    [Fact]
    public async Task Edicion_SiFallaLaAuditoria_NoSeConservaElCambio()
    {
        using var e = await PrepararAsync();
        var id = await RegistrarAsync(e, Adulto());
        await SimularFalloDeAuditoriaAsync(e.Escenario);

        await Assert.ThrowsAsync<DbUpdateException>(() => e.Servicio(s => s.ActualizarAsync(id, 1, Adulto("Nombre Nuevo", AmbitoAtencion.Independiente))));

        var paciente = Assert.Single(await e.PacientesAsync());
        Assert.Equal("Paciente Ficticio", paciente.Nombre);
        Assert.Equal(1, paciente.Version);
        Assert.Equal(AmbitoAtencion.Escolar, Assert.Single(paciente.Ambitos).Ambito);
    }

    [Fact]
    public async Task Edicion_DeUnaFichaDeBaja_SeRechazaYNoCambiaNada()
    {
        using var e = await PrepararAsync();
        var id = await RegistrarAsync(e, Adulto());
        var baja = await e.EnAlcance(p => p.GetRequiredService<ICoordinadorDeBajaDePaciente>().DarDeBajaAsync(id, 1));
        Assert.True(baja.Exito, baja.Mensaje);

        var respuesta = await e.Servicio(s => s.ActualizarAsync(id, 2, Adulto("Nombre Nuevo")));

        Assert.Equal(MensajesDePacientes.FichaDeBajaNoSeActualiza, respuesta.Mensaje);
        Assert.Equal("Paciente Ficticio", Assert.Single(await e.PacientesAsync()).Nombre);
    }

    // ──── CONSULTA ───────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Consulta_DeUnaFichaVigente_EjecutaUnaSolaConsultaALaTablaPaciente()
    {
        using var e = await PrepararAsync();
        var id = await RegistrarAsync(e, Menor());
        e.Comandos.Limpiar();

        var respuesta = await e.Servicio(s => s.ObtenerAsync(id));

        Assert.True(respuesta.Exito);
        Assert.Equal("Menor Ficticio", respuesta.Datos!.Nombre);
        Assert.Equal(10, respuesta.Datos.Edad);
        Assert.Equal([AmbitoAtencion.Escolar, AmbitoAtencion.Independiente], respuesta.Datos.Ambitos);
        Assert.Equal("tutora@ficticio.test", respuesta.Datos.Tutor!.Correo);
        var consultasAPaciente = e.Comandos.Textos.Where(t => t.Contains("FROM \"Paciente\"", StringComparison.Ordinal)).ToList();
        Assert.Single(consultasAPaciente);
        Assert.Contains("CASE", consultasAPaciente[0]);
    }

    [Fact]
    public async Task Consulta_DeUnaFichaDeBaja_EjecutaUnaSolaConsultaYLaBaseNoEntregaSuContenido()
    {
        using var e = await PrepararAsync();
        var id = await RegistrarAsync(e, Menor());
        Assert.True((await e.EnAlcance(p => p.GetRequiredService<ICoordinadorDeBajaDePaciente>().DarDeBajaAsync(id, 1))).Exito);
        e.Comandos.Limpiar();

        var respuesta = await e.Servicio(s => s.ObtenerAsync(id));
        var consultasAPaciente = e.Comandos.Textos.Where(t => t.Contains("FROM \"Paciente\"", StringComparison.Ordinal)).ToList();
        var lectura = await e.EnAlcance(p => p.GetRequiredService<IRepositorioPacientes>().ObtenerParaConsultaAsync(id));

        Assert.False(respuesta.Exito);
        Assert.Equal("Est\u00e1 dada de baja.", respuesta.Mensaje);
        Assert.Null(respuesta.Datos);
        Assert.Single(consultasAPaciente);
        Assert.Equal(CondicionRegistro.Baja, lectura!.Condicion);
        Assert.Null(lectura.Nombre);
        Assert.Null(lectura.FechaNacimiento);
        Assert.Null(lectura.Telefono);
        Assert.Null(lectura.Correo);
        Assert.Null(lectura.Escuela);
        Assert.Null(lectura.Grupo);
        Assert.Null(lectura.TutorNombre);
        Assert.Null(lectura.TutorParentesco);
        Assert.Null(lectura.TutorTelefono);
        Assert.Null(lectura.TutorCorreo);
        Assert.False(lectura.TieneAmbitoEscolar);
        Assert.False(lectura.TieneAmbitoIndependiente);
    }

    // ──── BÚSQUEDA ───────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("maria munoz")]
    [InlineData("MAR\u00cdA MU\u00d1OZ")]
    [InlineData("munoz")]
    [InlineData("mu")]
    [InlineData("\u00f1oz")]
    public async Task Busqueda_NoDistingueAcentosNiMayusculasConPostgreSqlReal(string texto)
    {
        using var e = await PrepararAsync();
        var maria = await RegistrarAsync(e, Adulto("Mar\u00eda Mu\u00f1oz"));
        await RegistrarAsync(e, Adulto("Luis P\u00e9rez"));

        var respuesta = await e.Servicio(s => s.BuscarAsync(texto));

        Assert.Equal(maria, Assert.Single(respuesta.Datos!.Pacientes).IdPaciente);
    }

    [Theory]
    [InlineData("\u00D8scar", "\u00F8scar")]
    [InlineData("\u00D8SCAR", "\u00F8scar")]
    [InlineData("\u0416\u0443\u043A", "\u0436\u0443\u043A")]
    [InlineData("\u0130stanbul", "istanbul")]
    [InlineData("\u039F\u0394\u03A5\u03A3\u03A3\u0395\u03A5\u03A3", "\u03BF\u03B4\u03C5\u03C3\u03C3\u03B5\u03C5\u03C2")]
    [InlineData("\u03A3\u039F\u03A6\u038A\u0391", "\u03C3\u03BF\u03C6\u03B9\u03B1")]
    public async Task Busqueda_EncuentraNombresConMayusculasFueraDeAZEscribiendoSuMinuscula(string nombre, string texto)
    {
        using var e = await PrepararAsync();
        var id = await RegistrarAsync(e, Adulto(nombre));
        await RegistrarAsync(e, Adulto("Luis P\u00E9rez"));

        var respuesta = await e.Servicio(s => s.BuscarAsync(texto));

        Assert.Equal(id, Assert.Single(respuesta.Datos!.Pacientes).IdPaciente);
    }

    [Fact]
    public async Task Busqueda_TrataPorcientoGuionBajoYDiagonalInversaComoTextoLiteral()
    {
        using var e = await PrepararAsync();
        var conGuion = await RegistrarAsync(e, Adulto("Ana_Perez"));
        var conPorciento = await RegistrarAsync(e, Adulto("Luis 100% Real"));
        var conDiagonal = await RegistrarAsync(e, Adulto(@"Marta\Lopez"));
        await RegistrarAsync(e, Adulto("AnaXPerez"));
        await RegistrarAsync(e, Adulto("Luis 1000 Real"));

        var guion = await e.Servicio(s => s.BuscarAsync("ana_perez"));
        var porciento = await e.Servicio(s => s.BuscarAsync("100%"));
        var diagonal = await e.Servicio(s => s.BuscarAsync(@"marta\lopez"));
        var soloComodin = await e.Servicio(s => s.BuscarAsync("%%"));

        Assert.Equal(conGuion, Assert.Single(guion.Datos!.Pacientes).IdPaciente);
        Assert.Equal(conPorciento, Assert.Single(porciento.Datos!.Pacientes).IdPaciente);
        Assert.Equal(conDiagonal, Assert.Single(diagonal.Datos!.Pacientes).IdPaciente);
        Assert.Empty(soloComodin.Datos!.Pacientes);
    }

    [Fact]
    public async Task Busqueda_SoloDevuelveVigentesYMuestraNacimientoSoloAHomonimos()
    {
        using var e = await PrepararAsync();
        var unica = await RegistrarAsync(e, Adulto("Luis Pérez"));
        var ana1 = await RegistrarAsync(e, Adulto("Ana Pérez") with { FechaNacimiento = new DateOnly(1980, 1, 1) });
        var ana2 = await RegistrarAsync(e, Adulto("ANA PEREZ") with { FechaNacimiento = new DateOnly(1981, 2, 2) }, confirma: true);
        var baja = await RegistrarAsync(e, Adulto("Ana Perez Baja"));
        Assert.True((await e.EnAlcance(p => p.GetRequiredService<ICoordinadorDeBajaDePaciente>().DarDeBajaAsync(baja, 1))).Exito);

        var respuesta = await e.Servicio(s => s.BuscarAsync("perez"));

        var filas = respuesta.Datos!.Pacientes.ToDictionary(p => p.IdPaciente);
        Assert.Equal(3, filas.Count);
        Assert.DoesNotContain(baja, filas.Keys);
        Assert.Null(filas[unica].FechaNacimiento);
        Assert.Equal(new DateOnly(1980, 1, 1), filas[ana1].FechaNacimiento);
        Assert.Equal(new DateOnly(1981, 2, 2), filas[ana2].FechaNacimiento);
    }

    [Fact]
    public async Task Busqueda_DaUnOrdenEstableYMarcaCuandoHayMasDeCincuenta()
    {
        using var e = await PrepararAsync();
        await using (var contexto = e.Escenario.Contexto())
        {
            for (var i = 0; i < LimitesDeBusquedaDePacientes.MaximoDeResultados + 1; i++)
            {
                contexto.Pacientes.Add(ReglasDePaciente.Registrar(
                    Guid.NewGuid(), new DatosDePaciente($"Persona Masiva {i:000}", NacimientoAdulto), [AmbitoAtencion.Escolar], e.Actor, DateTimeOffset.UtcNow));
            }

            await contexto.SaveChangesAsync();
        }

        var primera = await e.Servicio(s => s.BuscarAsync("masiva"));
        var segunda = await e.Servicio(s => s.BuscarAsync("masiva"));

        Assert.Equal(LimitesDeBusquedaDePacientes.MaximoDeResultados, primera.Datos!.Pacientes.Count);
        Assert.True(primera.Datos.HayMas);
        Assert.Equal("Persona Masiva 000", primera.Datos.Pacientes[0].Nombre);
        Assert.Equal("Persona Masiva 049", primera.Datos.Pacientes[^1].Nombre);
        Assert.Equal(primera.Datos.Pacientes.Select(p => p.IdPaciente), segunda.Datos!.Pacientes.Select(p => p.IdPaciente));
    }

    [Fact]
    public async Task Busqueda_NoEscribeEventosNiGuardaElTextoBuscado()
    {
        using var e = await PrepararAsync();
        await RegistrarAsync(e, Adulto("María Muñoz"));
        var eventosAntes = await e.EventosDePacientesAsync();
        e.Comandos.Limpiar();

        await e.Servicio(s => s.BuscarAsync("texto-privado-buscado"));
        await e.Servicio(s => s.BuscarAsync("maria"));

        Assert.Equal(eventosAntes.Count, (await e.EventosDePacientesAsync()).Count);
        Assert.DoesNotContain(e.Comandos.Textos, t => t.TrimStart().StartsWith("INSERT", StringComparison.OrdinalIgnoreCase));
    }

    // ──── RESTRICCIONES DE LA BASE ───────────────────────────────────────────────────────────

    private static string InsertarFicha(string nombreNormalizado, string extras = "", string columnasExtra = "") =>
        $"""
        INSERT INTO "Paciente" ("IdPaciente","Nombre","NombreNormalizado","Condicion","Version","FechaAlta","IdUsuarioAlta"{columnasExtra})
        VALUES ('{Guid.NewGuid()}','Nombre Ficticio','{nombreNormalizado}','Vigente',1,now(),'{Guid.NewGuid()}'{extras})
        """;

    [Fact]
    public async Task Restricciones_AceptanUnaFilaValidaYRechazanLasQueNoCumplenLaForma()
    {
        using var e = await PrepararAsync();
        var conexion = e.Escenario.App;

        Assert.Equal(1, await PostgreSqlRevision.Sql(conexion, InsertarFicha("nombre ficticio")));

        var rechazos = new (string Sql, string Restriccion)[]
        {
            (InsertarFicha("Nombre Ficticio"), "CK_Paciente_NombreNormalizadoFormato"),
            (InsertarFicha(""), "CK_Paciente_NombreNormalizadoFormato"),
            (InsertarFicha(" nombre ficticio"), "CK_Paciente_NombreNormalizadoFormato"),
            (InsertarFicha("nombre ficticio "), "CK_Paciente_NombreNormalizadoFormato"),
            (InsertarFicha("nombre  ficticio"), "CK_Paciente_NombreNormalizadoFormato"),
            (InsertarFicha("nombre\tficticio"), "CK_Paciente_NombreNormalizadoFormato"),
            (InsertarFicha("nombre ficti\u0301cio"), "CK_Paciente_NombreNormalizadoFormato"),
            (InsertarFicha("nombre ficticio", ",'1899-12-31'", ",\"FechaNacimiento\""), "CK_Paciente_FechaNacimientoMinima"),
            (InsertarFicha("nombre ficticio", ",'Solo Nombre'", ",\"TutorNombre\""), "CK_Paciente_TutorCompleto"),
            (InsertarFicha("nombre ficticio", ",'Tutor','Madre'", ",\"TutorNombre\",\"TutorParentesco\""), "CK_Paciente_TutorCompleto"),
            (InsertarFicha("nombre ficticio", ",'6441234567'", ",\"TutorTelefono\""), "CK_Paciente_TutorCompleto")
        };

        foreach (var (sql, restriccion) in rechazos)
        {
            var error = await Assert.ThrowsAsync<PostgresException>(() => PostgreSqlRevision.Sql(conexion, sql));
            Assert.Equal(PostgresErrorCodes.CheckViolation, error.SqlState);
            Assert.Equal(restriccion, error.ConstraintName);
        }

        Assert.Equal(1L, await PostgreSqlRevision.Valor<long>(conexion, "SELECT count(*) FROM \"Paciente\""));
    }

    [Fact]
    public async Task Restricciones_AceptanUnTutorCompletoYElNacimientoDelPrimeroDeEnero1900()
    {
        using var e = await PrepararAsync();

        Assert.Equal(1, await PostgreSqlRevision.Sql(e.Escenario.App, InsertarFicha(
            "nombre ficticio", ",'1900-01-01','Tutor','Madre','tutor@ficticio.test'",
            ",\"FechaNacimiento\",\"TutorNombre\",\"TutorParentesco\",\"TutorCorreo\"")));
    }

    [Fact]
    public async Task ElUsuarioDeLaAplicacionPuedeEscribirLasColumnasNuevasPeroNoBorrarFichas()
    {
        using var e = await PrepararAsync();
        var id = await RegistrarAsync(e, Menor());

        var error = await Assert.ThrowsAsync<PostgresException>(() =>
            PostgreSqlRevision.Sql(e.Escenario.App, $"DELETE FROM \"Paciente\" WHERE \"IdPaciente\"='{id}'"));

        Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, error.SqlState);
        Assert.Single(await e.PacientesAsync());
    }
}
