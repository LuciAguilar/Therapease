using System.Collections.Concurrent;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using TherapEase.Application.Citas.Constantes;
using TherapEase.Application.Citas.Interfaces.Repositorios;
using TherapEase.Application.Citas.Interfaces.Servicios;
using TherapEase.Application.Citas.Modelos;
using TherapEase.Application.Compartido.Interfaces.Repositorios;
using TherapEase.Application.Compartido.Interfaces.Servicios;
using TherapEase.Application.Coordinadores.Interfaces.Servicios;
using TherapEase.Application.Pacientes.Interfaces.Servicios;
using TherapEase.Application.Pacientes.Modelos;
using TherapEase.Domain.Auditoria.Entidades;
using TherapEase.Domain.Auditoria.Entidades.Enums;
using TherapEase.Domain.Citas.Entidades;
using TherapEase.Domain.Citas.Entidades.Enums;
using TherapEase.Domain.Citas.Excepciones;
using TherapEase.Domain.Citas.Reglas;
using TherapEase.Domain.Compartido.Excepciones;
using TherapEase.Domain.Compartido.Reglas;
using TherapEase.Domain.Pacientes.Entidades.Enums;
using TherapEase.Infrastructure.Configuracion;
using TherapEase.Infrastructure.Data;
using TherapEase.Web.Configuracion;
using static TherapEase.IntegrationTests.ServidorIdentidadRevision;

namespace TherapEase.IntegrationTests;

/// <summary>
/// Comprueba con los servicios reales y un PostgreSQL desechable la consulta de agenda y el agendado de citas: guardado y evento en una sola transacción,
/// permisos de la matriz real, la traducción de la clave primaria de Cita, el reenvío de la misma reserva (incluida la gemela que termina antes del guardado
/// o antes del rechazo de ámbito), los cruces entre reservas simultáneas, el fallo de auditoría y las consultas de agenda. Todos los datos son ficticios.
/// El QA dirigido de agendar frente a baja y del ámbito retirado sin cita confirmada es independiente de estas pruebas.
/// </summary>
public class CitasServidorTests(PostgreSqlRevision postgres) : IClassFixture<PostgreSqlRevision>
{
    // Fecha futura fija: ningún caso depende de la hora de la ejecución salvo los de agenda, que parten de hoy en Hermosillo.
    private static readonly DateOnly DiaFuturo = new(2031, 7, 9);

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
    }

    // Ejecuta una acción, una sola vez, justo antes del siguiente guardado de una cita: simula a otra persona que se adelanta.
    private sealed class AdelantadaAlGuardarCita : SaveChangesInterceptor
    {
        private Func<Task>? _accion;

        public void Armar(Func<Task> accion) => Volatile.Write(ref _accion, accion);

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData datos, InterceptionResult<int> resultado, CancellationToken cancelacion = default)
        {
            var hayCita = datos.Context!.ChangeTracker.Entries<Cita>().Any(e => e.State == EntityState.Added);
            if (hayCita && Interlocked.Exchange(ref _accion, null) is { } accion)
            {
                await accion();
            }

            return resultado;
        }
    }

    // Hace que solo el INSERT del evento de auditoría falle con interbloqueo (40P01): el de la cita no. Sirve para comprobar que ese código no se confunde con un cruce.
    private static Task SimularInterbloqueoEnAuditoriaAsync(Escenario escenario) =>
        PostgreSqlRevision.Sql(escenario.Migrador, """
            CREATE FUNCTION rechazar_evento_con_interbloqueo() RETURNS trigger LANGUAGE plpgsql AS $$
            BEGIN RAISE EXCEPTION 'Interbloqueo ficticio de auditoria' USING ERRCODE = '40P01'; END $$;
            CREATE TRIGGER rechazar_evento_con_interbloqueo BEFORE INSERT ON "EventoAuditoria"
            FOR EACH ROW EXECUTE FUNCTION rechazar_evento_con_interbloqueo();
            """);

    private static IEnumerable<Exception> CadenaDe(Exception excepcion)
    {
        for (var actual = excepcion; actual is not null; actual = actual.InnerException)
        {
            yield return actual;
        }
    }

    // Ejecuta una acción, una sola vez, justo antes de leer la condición y los ámbitos del paciente: es el momento entre la primera lectura de la cita
    // (por número) y la comprobación del paciente.
    private sealed class AntesDeConsultarAmbitos : DbCommandInterceptor
    {
        private Func<Task>? _accion;

        public void Armar(Func<Task> accion) => Volatile.Write(ref _accion, accion);

        public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand comando, CommandEventData datos, InterceptionResult<DbDataReader> resultado, CancellationToken cancelacion = default)
        {
            if (comando.CommandText.Contains("\"PacienteAmbito\"", StringComparison.Ordinal) && Interlocked.Exchange(ref _accion, null) is { } accion)
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

        public Task<T> Citas<T>(Func<IServicioCitas, Task<T>> accion) => EnAlcance(p => accion(p.GetRequiredService<IServicioCitas>()));

        public Task<T> Pacientes<T>(Func<IServicioPacientes, Task<T>> accion) => EnAlcance(p => accion(p.GetRequiredService<IServicioPacientes>()));

        public async Task<List<Cita>> CitasGuardadasAsync()
        {
            await using var contexto = Escenario.Contexto();
            return await contexto.Citas.AsNoTracking().OrderBy(c => c.Inicio).ThenBy(c => c.IdCita).ToListAsync();
        }

        public async Task<List<EventoAuditoria>> EventosDeCitasAsync()
        {
            await using var contexto = Escenario.Contexto();
            return await contexto.EventosAuditoria.AsNoTracking()
                .Where(e => e.TipoRegistro == TipoRegistroAuditoria.Cita)
                .OrderBy(e => e.FechaEvento)
                .ToListAsync();
        }

        public async Task<List<EventoAuditoria>> EventosAsync(TipoRegistroAuditoria tipo)
        {
            await using var contexto = Escenario.Contexto();
            return await contexto.EventosAuditoria.AsNoTracking().Where(e => e.TipoRegistro == tipo).ToListAsync();
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

    private static async Task<Guid> RegistrarPacienteAsync(Entorno e, string nombre = "Paciente Ficticio", params AmbitoAtencion[] ambitos)
    {
        var id = Guid.NewGuid();
        var datos = new DatosDePacienteEntrada(
            nombre, PacientesDePrueba.NacimientoDeAdulto, null, "canario@ficticio.test", null, null,
            ambitos.Length == 0 ? [AmbitoAtencion.Escolar, AmbitoAtencion.Independiente] : ambitos);
        var respuesta = await e.Pacientes(s => s.RegistrarAsync(id, datos));
        Assert.True(respuesta.Exito, respuesta.Mensaje);
        return id;
    }

    private static DatosDeCitaEntrada Datos(
        Guid paciente, int horaInicio = 10, int minutoInicio = 0, int horaFin = 11, int minutoFin = 0,
        AmbitoAtencion ambito = AmbitoAtencion.Escolar, DateOnly? fecha = null) =>
        new(paciente, ambito, fecha ?? DiaFuturo, new TimeOnly(horaInicio, minutoInicio), new TimeOnly(horaFin, minutoFin));

    private static async Task<Guid> AgendarAsync(Entorno e, DatosDeCitaEntrada datos)
    {
        var id = Guid.NewGuid();
        var respuesta = await e.Citas(s => s.AgendarAsync(id, datos));
        Assert.True(respuesta.Exito, respuesta.Mensaje);
        return id;
    }

    // Inserta directo en la base, sin pasar por el servicio ni por sus eventos: para citas que el horario nuevo no admitiría o con una fecha de alta elegida.
    private static async Task<Guid> InsertarDirectoAsync(
        Entorno e, Guid paciente, DateTimeOffset inicio, DateTimeOffset fin, AmbitoAtencion ambito = AmbitoAtencion.Escolar, DateTimeOffset? alta = null)
    {
        await using var contexto = e.Escenario.Contexto();
        var cita = ReglasDeCita.Agendar(Guid.NewGuid(), paciente, ambito, inicio, fin, e.Actor, alta ?? DateTimeOffset.UtcNow);
        contexto.Citas.Add(cita);
        await contexto.SaveChangesAsync();
        return cita.IdCita;
    }

    private static async Task CancelarDirectoAsync(Entorno e, Guid idCita)
    {
        await using var contexto = e.Escenario.Contexto();
        var cita = await contexto.Citas.SingleAsync(c => c.IdCita == idCita);
        ReglasDeCita.Cancelar(cita, cita.Version, e.Actor, DateTimeOffset.UtcNow);
        await contexto.SaveChangesAsync();
    }

    private static DateOnly HoyEnHermosillo() => DateOnly.FromDateTime(HoraHermosillo.ALocal(DateTimeOffset.UtcNow));

    // ──── AGENDAR: GUARDADO Y PERMISOS ──────────────────────────────────────────────────────

    [Theory]
    [InlineData("Usuaria")]
    [InlineData("Superusuario")]
    public async Task Agendar_GuardaLaCitaYSuEventoEnLaMismaTransaccionConLosPermisosRealesDeCadaRol(string rol)
    {
        using var e = await PrepararAsync(rol);
        var paciente = await RegistrarPacienteAsync(e);
        var id = Guid.NewGuid();

        var respuesta = await e.Citas(s => s.AgendarAsync(id, Datos(paciente)));

        Assert.True(respuesta.Exito, respuesta.Mensaje);
        Assert.Equal(EstadoDeAgendar.Agendada, respuesta.Datos!.Estado);
        Assert.Equal("Paciente Ficticio", respuesta.Datos.Cita.NombreDelPaciente);
        Assert.Equal(new DateTime(2031, 7, 9, 10, 0, 0), respuesta.Datos.Cita.InicioLocal);

        var cita = Assert.Single(await e.CitasGuardadasAsync());
        Assert.Equal(id, cita.IdCita);
        Assert.Equal(paciente, cita.IdPaciente);
        Assert.Equal(AmbitoAtencion.Escolar, cita.Ambito);
        Assert.Equal(new DateTimeOffset(2031, 7, 9, 17, 0, 0, TimeSpan.Zero), cita.Inicio);
        Assert.Equal(new DateTimeOffset(2031, 7, 9, 18, 0, 0, TimeSpan.Zero), cita.Fin);
        Assert.Equal(EstadoCita.Agendada, cita.Estado);
        Assert.Equal(EstadoPago.Pendiente, cita.EstadoPago);
        Assert.Equal(1, cita.Version);
        Assert.Equal(e.Actor, cita.IdUsuarioAlta);

        var evento = Assert.Single(await e.EventosDeCitasAsync());
        Assert.Equal(AccionAuditoria.Alta, evento.Accion);
        Assert.Equal(id, evento.IdRegistro);
        Assert.Equal(e.Actor, evento.IdUsuarioActor);
        Assert.Equal("Se agendó una cita.", evento.Hecho);
        Assert.Equal(["IdPaciente", "Ambito", "Inicio", "Fin", "Estado", "EstadoPago"], evento.CamposAfectados);
        Assert.DoesNotContain("Paciente Ficticio", evento.Hecho + string.Join('|', evento.CamposAfectados));
    }

    [Fact]
    public async Task ConContrasenaTemporalPendiente_NingunaOperacionDeCitasEstaPermitida()
    {
        using var e = await PrepararAsync("Usuaria", temporal: true);

        var agendar = await e.Citas(s => s.AgendarAsync(Guid.NewGuid(), Datos(Guid.NewGuid())));
        var agenda = await e.Citas(s => s.ConsultarAgendaAsync(null, null));
        var detalle = await e.Citas(s => s.ObtenerCitaAsync(Guid.NewGuid()));

        Assert.Equal(MensajesDeCitas.SinPermiso, agendar.Mensaje);
        Assert.Equal(MensajesDeCitas.SinPermiso, agenda.Mensaje);
        Assert.Equal(MensajesDeCitas.SinPermiso, detalle.Mensaje);
        Assert.Empty(await e.CitasGuardadasAsync());
        Assert.Empty(await e.EventosDeCitasAsync());
    }

    [Fact]
    public async Task Agendar_ConElHorarioInvalido_NoGuardaNingunaCitaNiEvento()
    {
        using var e = await PrepararAsync();
        var paciente = await RegistrarPacienteAsync(e);

        var respuesta = await e.Citas(s => s.AgendarAsync(Guid.NewGuid(), Datos(paciente, 10, 0, 10, 30)));

        Assert.False(respuesta.Exito);
        Assert.Empty(await e.CitasGuardadasAsync());
        Assert.Empty(await e.EventosDeCitasAsync());
    }

    // ──── AGENDAR: CLAVE PRIMARIA Y REENVÍO ─────────────────────────────────────────────────

    [Fact]
    public async Task LaBase_TraduceLaClavePrimariaRepetidaDeCitaAUnaExcepcionDeDominio()
    {
        using var e = await PrepararAsync();
        var paciente = await RegistrarPacienteAsync(e);
        var id = Guid.NewGuid();
        var primera = ReglasDeCita.Agendar(
            id, paciente, AmbitoAtencion.Escolar, new DateTimeOffset(2031, 7, 9, 17, 0, 0, TimeSpan.Zero), new DateTimeOffset(2031, 7, 9, 18, 0, 0, TimeSpan.Zero), e.Actor, DateTimeOffset.UtcNow);
        var repetida = ReglasDeCita.Agendar(
            id, paciente, AmbitoAtencion.Escolar, new DateTimeOffset(2031, 7, 10, 17, 0, 0, TimeSpan.Zero), new DateTimeOffset(2031, 7, 10, 18, 0, 0, TimeSpan.Zero), e.Actor, DateTimeOffset.UtcNow);

        // Cada guardado en su propio alcance: así la segunda cita no choca con la primera en el seguimiento del contexto, sino en la base.
        await e.EnAlcance(async p =>
        {
            await p.GetRequiredService<IUnidadDeTrabajo>().EjecutarAsync(ct => p.GetRequiredService<IRepositorioCitas>().AgregarAsync(primera, ct));
            return 0;
        });
        await e.EnAlcance(async p =>
        {
            var unidad = p.GetRequiredService<IUnidadDeTrabajo>();
            var citas = p.GetRequiredService<IRepositorioCitas>();
            await Assert.ThrowsAsync<IdentificadorDeCitaRepetidoException>(() => unidad.EjecutarAsync(ct => citas.AgregarAsync(repetida, ct)));
            return 0;
        });

        Assert.Equal(new DateTimeOffset(2031, 7, 9, 17, 0, 0, TimeSpan.Zero), Assert.Single(await e.CitasGuardadasAsync()).Inicio);
    }

    [Fact]
    public async Task Agendar_ReenviadaDosVecesConElMismoIdentificador_DejaUnaCitaYUnEvento()
    {
        using var e = await PrepararAsync();
        var paciente = await RegistrarPacienteAsync(e);
        var id = Guid.NewGuid();

        var primera = await e.Citas(s => s.AgendarAsync(id, Datos(paciente)));
        var segunda = await e.Citas(s => s.AgendarAsync(id, Datos(paciente)));

        Assert.Equal(EstadoDeAgendar.Agendada, primera.Datos!.Estado);
        Assert.Equal(EstadoDeAgendar.YaAgendada, segunda.Datos!.Estado);
        Assert.Equal(MensajesDeCitas.CitaYaAgendada, segunda.Mensaje);
        Assert.Equal(primera.Datos.Cita, segunda.Datos.Cita);
        Assert.Equal(1, Assert.Single(await e.CitasGuardadasAsync()).Version);
        Assert.Single(await e.EventosDeCitasAsync());
    }

    [Fact]
    public async Task Agendar_SiLaGemelaGuardaAntesDelGuardado_LaBaseRechazaLaClaveYSeDevuelveYaAgendada()
    {
        var id = Guid.NewGuid();
        var adelantada = new AdelantadaAlGuardarCita();
        using var e = await PrepararAsync(configurar: s => s.ConfigureDbContext<ContextoDeDatos>(o => o.AddInterceptors(adelantada)));
        var paciente = await RegistrarPacienteAsync(e);
        adelantada.Armar(async () =>
        {
            var gemela = await e.Citas(s => s.AgendarAsync(id, Datos(paciente)));
            Assert.Equal(EstadoDeAgendar.Agendada, gemela.Datos!.Estado);
        });

        var respuesta = await e.Citas(s => s.AgendarAsync(id, Datos(paciente)));

        Assert.True(respuesta.Exito, respuesta.Mensaje);
        Assert.Equal(EstadoDeAgendar.YaAgendada, respuesta.Datos!.Estado);
        Assert.Equal(id, respuesta.Datos.Cita.IdCita);
        Assert.Single(await e.CitasGuardadasAsync());
        Assert.Single(await e.EventosDeCitasAsync());
    }

    [Fact]
    public async Task Agendar_SiLaGemelaGuardaOtrosDatosConElMismoIdentificador_NoSeReconoceComoReenvio()
    {
        var id = Guid.NewGuid();
        var adelantada = new AdelantadaAlGuardarCita();
        using var e = await PrepararAsync(configurar: s => s.ConfigureDbContext<ContextoDeDatos>(o => o.AddInterceptors(adelantada)));
        var paciente = await RegistrarPacienteAsync(e);
        adelantada.Armar(async () =>
        {
            var competidora = await e.Citas(s => s.AgendarAsync(id, Datos(paciente, 12, 0, 13, 0)));
            Assert.True(competidora.Exito);
        });

        var respuesta = await e.Citas(s => s.AgendarAsync(id, Datos(paciente)));

        Assert.False(respuesta.Exito);
        Assert.Equal(MensajesDeCitas.SolicitudYaUsada, respuesta.Mensaje);
        Assert.Null(respuesta.Datos);
        Assert.Equal(new DateTimeOffset(2031, 7, 9, 19, 0, 0, TimeSpan.Zero), Assert.Single(await e.CitasGuardadasAsync()).Inicio);
        Assert.Single(await e.EventosDeCitasAsync());
    }

    // La gemela confirma entre la primera lectura por número y la comprobación del paciente: el guardado choca con la clave y se reconoce lo confirmado.
    [Fact]
    public async Task Agendar_SiLaGemelaConfirmaDespuesDeLaPrimeraLectura_SeReconoceSinOtraCitaNiEvento()
    {
        var id = Guid.NewGuid();
        var antes = new AntesDeConsultarAmbitos();
        using var e = await PrepararAsync(configurar: s => s.ConfigureDbContext<ContextoDeDatos>(o => o.AddInterceptors(antes)));
        var paciente = await RegistrarPacienteAsync(e);
        antes.Armar(async () =>
        {
            var gemela = await e.Citas(s => s.AgendarAsync(id, Datos(paciente)));
            Assert.Equal(EstadoDeAgendar.Agendada, gemela.Datos!.Estado);
        });

        var respuesta = await e.Citas(s => s.AgendarAsync(id, Datos(paciente)));

        Assert.True(respuesta.Exito, respuesta.Mensaje);
        Assert.Equal(EstadoDeAgendar.YaAgendada, respuesta.Datos!.Estado);
        Assert.Single(await e.CitasGuardadasAsync());
        Assert.Single(await e.EventosDeCitasAsync());
    }

    // A no encuentra su identificador y se pausa antes de consultar al paciente; la gemela confirma cita y evento; otra solicitud confirma el retiro del ámbito.
    // A continúa y debe reconocer su propia reserva activa con los mismos datos, sin restaurar el ámbito ni crear otro evento.
    [Fact]
    public async Task Agendar_LaGemelaConfirmaYSeRetiraElAmbitoAntesDelRechazo_ReconoceLaReservaSinRestaurarElAmbito()
    {
        var id = Guid.NewGuid();
        var antes = new AntesDeConsultarAmbitos();
        using var e = await PrepararAsync(configurar: s => s.ConfigureDbContext<ContextoDeDatos>(o => o.AddInterceptors(antes)));
        var paciente = await RegistrarPacienteAsync(e, "Paciente Ficticio", AmbitoAtencion.Escolar, AmbitoAtencion.Independiente);
        antes.Armar(async () =>
        {
            var gemela = await e.Citas(s => s.AgendarAsync(id, Datos(paciente)));
            Assert.Equal(EstadoDeAgendar.Agendada, gemela.Datos!.Estado);

            var retiro = await e.Pacientes(s => s.ActualizarAsync(paciente, 1, new DatosDePacienteEntrada(
                "Paciente Ficticio", PacientesDePrueba.NacimientoDeAdulto, null, "canario@ficticio.test", null, null, [AmbitoAtencion.Independiente])));
            Assert.True(retiro.Exito, retiro.Mensaje);
        });

        var respuesta = await e.Citas(s => s.AgendarAsync(id, Datos(paciente)));

        Assert.True(respuesta.Exito, respuesta.Mensaje);
        Assert.Equal(EstadoDeAgendar.YaAgendada, respuesta.Datos!.Estado);
        Assert.Equal(AmbitoAtencion.Escolar, respuesta.Datos.Cita.Ambito);
        var cita = Assert.Single(await e.CitasGuardadasAsync());
        Assert.Equal(AmbitoAtencion.Escolar, cita.Ambito);
        Assert.Single(await e.EventosDeCitasAsync());

        // El ámbito retirado no se restaura al paciente y su evento de edición es el único de pacientes además del alta.
        var ficha = await e.Pacientes(s => s.ObtenerAsync(paciente));
        Assert.Equal([AmbitoAtencion.Independiente], ficha.Datos!.Ambitos);
        Assert.Equal(2, (await e.EventosAsync(TipoRegistroAuditoria.Paciente)).Count);
    }

    // ──── AGENDAR: CRUCES ───────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Agendar_DosReservasDistintasPorElMismoHorario_SoloUnaGuardaYLaOtraRecibeHorarioOcupado()
    {
        using var e = await PrepararAsync();
        var paciente = await RegistrarPacienteAsync(e);
        var otro = await RegistrarPacienteAsync(e, "Otra Persona");
        for (var ronda = 0; ronda < 3; ronda++)
        {
            var fecha = DiaFuturo.AddDays(ronda);
            var resultados = await Task.WhenAll(
                e.Citas(s => s.AgendarAsync(Guid.NewGuid(), Datos(paciente, fecha: fecha))),
                e.Citas(s => s.AgendarAsync(Guid.NewGuid(), Datos(otro, fecha: fecha, ambito: AmbitoAtencion.Independiente))));

            Assert.Equal(1, resultados.Count(r => r.Exito));
            var perdedora = Assert.Single(resultados, r => !r.Exito);
            Assert.Equal(MensajesDeCitas.HorarioOcupado, perdedora.Mensaje);
        }

        Assert.Equal(3, (await e.CitasGuardadasAsync()).Count);
        Assert.Equal(3, (await e.EventosDeCitasAsync()).Count);
    }

    // El disparador de Cita toma un bloqueo de asesoría antes de escribir una cita activa: las altas simultáneas se serializan y la restricción compara contra filas
    // ya confirmadas. Las demás reciben el cruce real (23P01); ningún interbloqueo (40P01) llega hasta aquí porque ya no se traduce, así que uno solo haría fallar la prueba.
    [Fact]
    public async Task Agendar_DiezReservasSimultaneasPorElMismoHorario_SoloUnaGuardaYLasDemasRecibenHorarioOcupado()
    {
        using var e = await PrepararAsync();
        var paciente = await RegistrarPacienteAsync(e);
        for (var ronda = 0; ronda < 2; ronda++)
        {
            var fecha = DiaFuturo.AddDays(ronda);

            var resultados = await Task.WhenAll(Enumerable.Range(0, 10)
                    .Select(_ => e.Citas(s => s.AgendarAsync(Guid.NewGuid(), Datos(paciente, fecha: fecha)))))
                .WaitAsync(TimeSpan.FromSeconds(90));

            Assert.Equal(1, resultados.Count(r => r.Exito));
            Assert.Equal(9, resultados.Count(r => !r.Exito && r.Mensaje == MensajesDeCitas.HorarioOcupado));
        }

        Assert.Equal(2, (await e.CitasGuardadasAsync()).Count);
        Assert.Equal(2, (await e.EventosDeCitasAsync()).Count);
    }

    // Regresión: un 40P01 provocado solo al guardar el evento de auditoría NO es un conflicto de horario. Sube como error general, sin relectura de la cita,
    // sin anunciar éxito y sin dejar ni la cita ni su evento.
    [Fact]
    public async Task Agendar_SiLaAuditoriaFallaConInterbloqueo_SubeComoErrorGeneralSinRelecturaNiCambios()
    {
        using var e = await PrepararAsync();
        var paciente = await RegistrarPacienteAsync(e);
        await SimularInterbloqueoEnAuditoriaAsync(e.Escenario);
        e.Comandos.Limpiar();

        var error = await Assert.ThrowsAnyAsync<Exception>(() => e.Citas(s => s.AgendarAsync(Guid.NewGuid(), Datos(paciente))));

        Assert.IsNotAssignableFrom<ReglaDeNegocioException>(error);
        Assert.Contains(CadenaDe(error), x => x is PostgresException { SqlState: PostgresErrorCodes.DeadlockDetected });
        Assert.Empty(await e.CitasGuardadasAsync());
        Assert.Empty(await e.EventosDeCitasAsync());

        // Solo la primera lectura por número: ninguna relectura tras el fallo.
        var lecturasDeCitas = e.Comandos.Textos.Count(t => t.Contains("FROM \"Cita\"", StringComparison.Ordinal));
        Assert.Equal(1, lecturasDeCitas);
    }

    // Las pausas del plan B03 §4.1 siguen funcionando con el bloqueo: A se detiene justo antes de guardar y la gemela B confirma su reserva completa mientras tanto.
    // Como el bloqueo se toma al insertar y no al iniciar la transacción, B no queda esperando a A; al reanudar, A reconoce la reserva ya confirmada.
    [Fact]
    public async Task Agendar_LaGemelaConfirmaCompletaMientrasLaOtraEstaEnPausaAntesDeGuardar_NoQuedaEsperandoElBloqueo()
    {
        var id = Guid.NewGuid();
        var adelantada = new AdelantadaAlGuardarCita();
        using var e = await PrepararAsync(configurar: s => s.ConfigureDbContext<ContextoDeDatos>(o => o.AddInterceptors(adelantada)));
        var paciente = await RegistrarPacienteAsync(e);
        adelantada.Armar(async () =>
        {
            // Si el bloqueo se hubiera tomado al iniciar la transacción de A, esta llamada esperaría a A, que espera a esta, y se agotaría el tiempo.
            var gemela = await e.Citas(s => s.AgendarAsync(id, Datos(paciente))).WaitAsync(TimeSpan.FromSeconds(20));
            Assert.Equal(EstadoDeAgendar.Agendada, gemela.Datos!.Estado);
        });

        var respuesta = await e.Citas(s => s.AgendarAsync(id, Datos(paciente)));

        Assert.True(respuesta.Exito, respuesta.Mensaje);
        Assert.Equal(EstadoDeAgendar.YaAgendada, respuesta.Datos!.Estado);
        Assert.Single(await e.CitasGuardadasAsync());
        Assert.Single(await e.EventosDeCitasAsync());
    }

    [Fact]
    public async Task Agendar_ContiguasSePermitenYUnCruceEntreAmbitosSeRechaza()
    {
        using var e = await PrepararAsync();
        var paciente = await RegistrarPacienteAsync(e);

        var primera = await e.Citas(s => s.AgendarAsync(Guid.NewGuid(), Datos(paciente, 10, 0, 11, 0)));
        var contigua = await e.Citas(s => s.AgendarAsync(Guid.NewGuid(), Datos(paciente, 11, 0, 12, 0, AmbitoAtencion.Independiente)));
        var cruzada = await e.Citas(s => s.AgendarAsync(Guid.NewGuid(), Datos(paciente, 10, 30, 11, 30, AmbitoAtencion.Independiente)));

        Assert.True(primera.Exito);
        Assert.True(contigua.Exito);
        Assert.False(cruzada.Exito);
        Assert.Equal(MensajesDeCitas.HorarioOcupado, cruzada.Mensaje);
        Assert.Equal(2, (await e.CitasGuardadasAsync()).Count);
        Assert.Equal(2, (await e.EventosDeCitasAsync()).Count);
    }

    // Solo las citas activas ocupan horario: tras cancelar, el mismo horario queda libre.
    [Fact]
    public async Task Agendar_UnaCitaCanceladaNoOcupaHorario()
    {
        using var e = await PrepararAsync();
        var paciente = await RegistrarPacienteAsync(e);
        var cancelada = await AgendarAsync(e, Datos(paciente));
        await CancelarDirectoAsync(e, cancelada);

        var respuesta = await e.Citas(s => s.AgendarAsync(Guid.NewGuid(), Datos(paciente)));

        Assert.True(respuesta.Exito, respuesta.Mensaje);
        Assert.Equal(2, (await e.CitasGuardadasAsync()).Count);
    }

    // ──── AGENDAR: FALLO DE AUDITORÍA ───────────────────────────────────────────────────────

    [Fact]
    public async Task Agendar_SiFallaLaAuditoria_NoQuedaLaCitaNiSeDaExito()
    {
        using var e = await PrepararAsync();
        var paciente = await RegistrarPacienteAsync(e);
        await SimularFalloDeAuditoriaAsync(e.Escenario);

        await Assert.ThrowsAsync<DbUpdateException>(() => e.Citas(s => s.AgendarAsync(Guid.NewGuid(), Datos(paciente))));

        Assert.Empty(await e.CitasGuardadasAsync());
        Assert.Empty(await e.EventosDeCitasAsync());
    }

    [Fact]
    public async Task Agendar_TrasFallarLaAuditoria_LaSiguienteOperacionDelMismoAlcanceNoRepiteElCambioFallido()
    {
        using var e = await PrepararAsync();
        var paciente = await RegistrarPacienteAsync(e);
        await SimularFalloDeAuditoriaAsync(e.Escenario);
        var segunda = Guid.NewGuid();

        var respuesta = await e.EnAlcance(async p =>
        {
            var servicio = p.GetRequiredService<IServicioCitas>();
            await Assert.ThrowsAsync<DbUpdateException>(() => servicio.AgendarAsync(Guid.NewGuid(), Datos(paciente, 10, 0, 11, 0)));
            await PostgreSqlRevision.Sql(e.Escenario.Migrador,
                "DROP TRIGGER rechazar_evento_de_revision ON \"EventoAuditoria\"; DROP FUNCTION rechazar_evento_de_revision();");
            return await servicio.AgendarAsync(segunda, Datos(paciente, 12, 0, 13, 0));
        });

        Assert.True(respuesta.Exito, respuesta.Mensaje);
        Assert.Equal(segunda, Assert.Single(await e.CitasGuardadasAsync()).IdCita);
        Assert.Equal(segunda, Assert.Single(await e.EventosDeCitasAsync()).IdRegistro);
    }

    // ──── AGENDAR: PACIENTE ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Agendar_ConPacienteDeBaja_RechazaConElTextoDeBajaSinGuardarNada()
    {
        using var e = await PrepararAsync();
        var paciente = await RegistrarPacienteAsync(e);
        var baja = await e.EnAlcance(p => p.GetRequiredService<ICoordinadorDeBajaDePaciente>().DarDeBajaAsync(paciente, 1));
        Assert.True(baja.Exito, baja.Mensaje);

        var respuesta = await e.Citas(s => s.AgendarAsync(Guid.NewGuid(), Datos(paciente)));

        Assert.False(respuesta.Exito);
        Assert.Equal(MensajesDeCitas.PacienteDeBaja, respuesta.Mensaje);
        Assert.Empty(await e.CitasGuardadasAsync());
        Assert.Empty(await e.EventosDeCitasAsync());
    }

    [Fact]
    public async Task Agendar_ConPacienteInexistenteOAmbitoQueNoTiene_RechazaSinGuardarNada()
    {
        using var e = await PrepararAsync();
        var soloEscolar = await RegistrarPacienteAsync(e, "Solo Escolar", AmbitoAtencion.Escolar);

        var inexistente = await e.Citas(s => s.AgendarAsync(Guid.NewGuid(), Datos(Guid.NewGuid())));
        var sinAmbito = await e.Citas(s => s.AgendarAsync(Guid.NewGuid(), Datos(soloEscolar, ambito: AmbitoAtencion.Independiente)));

        Assert.Equal(MensajesDeCitas.PacienteNoEncontrado, inexistente.Mensaje);
        Assert.Equal(MensajesDeCitas.AmbitoNoPermitido, sinAmbito.Mensaje);
        Assert.Empty(await e.CitasGuardadasAsync());
        Assert.Empty(await e.EventosDeCitasAsync());
    }

    // ──── CONSULTAS: UNA SOLA CONSULTA POR LECTURA ──────────────────────────────────────────

    [Fact]
    public async Task ConsultarAgenda_UsaUnaConsultaDeCitasYUnaDeNombresSinUnirModulos()
    {
        using var e = await PrepararAsync();
        var paciente = await RegistrarPacienteAsync(e);
        await AgendarAsync(e, Datos(paciente, fecha: HoyEnHermosillo().AddDays(1)));
        e.Comandos.Limpiar();

        var respuesta = await e.Citas(s => s.ConsultarAgendaAsync(null, null));

        Assert.True(respuesta.Exito, respuesta.Mensaje);
        Assert.Single(respuesta.Datos!.Citas);
        var consultas = e.Comandos.Textos;
        var deCitas = consultas.Where(t => t.Contains("FROM \"Cita\"", StringComparison.Ordinal)).ToArray();
        var deNombres = consultas.Where(t => t.Contains("FROM \"Paciente\"", StringComparison.Ordinal)).ToArray();
        Assert.Single(deCitas);
        Assert.Single(deNombres);
        Assert.DoesNotContain("JOIN", deCitas[0], StringComparison.Ordinal);
        Assert.DoesNotContain("\"Paciente\"", deCitas[0], StringComparison.Ordinal);
        Assert.DoesNotContain("PacienteAmbito", deNombres[0], StringComparison.Ordinal);
    }

    [Fact]
    public async Task ElContratoDePacientesParaCitas_LeeCondicionYAmbitosEnUnaSolaConsultaYNoEntregaBajas()
    {
        using var e = await PrepararAsync();
        var vigente = await RegistrarPacienteAsync(e, "Paciente Vigente", AmbitoAtencion.Independiente);
        var deBaja = await RegistrarPacienteAsync(e, "Paciente Baja");
        var baja = await e.EnAlcance(p => p.GetRequiredService<ICoordinadorDeBajaDePaciente>().DarDeBajaAsync(deBaja, 1));
        Assert.True(baja.Exito, baja.Mensaje);
        e.Comandos.Limpiar();

        var condicionVigente = await e.EnAlcance(p => p.GetRequiredService<IConsultaDePacientesParaCitas>().ConsultarCondicionYAmbitosAsync(vigente));
        var consultasDeCondicion = e.Comandos.Textos.Where(t => t.Contains("\"Paciente\"", StringComparison.Ordinal)).ToArray();
        var condicionBaja = await e.EnAlcance(p => p.GetRequiredService<IConsultaDePacientesParaCitas>().ConsultarCondicionYAmbitosAsync(deBaja));
        var inexistente = await e.EnAlcance(p => p.GetRequiredService<IConsultaDePacientesParaCitas>().ConsultarCondicionYAmbitosAsync(Guid.NewGuid()));
        var nombres = await e.EnAlcance(p => p.GetRequiredService<IConsultaDePacientesParaCitas>()
            .ConsultarNombresParaAgendaAsync([vigente, deBaja, Guid.NewGuid()]));

        Assert.Single(consultasDeCondicion);
        Assert.Equal(TherapEase.Domain.Compartido.Entidades.Enums.CondicionRegistro.Vigente, condicionVigente.Datos!.Condicion);
        Assert.Equal([AmbitoAtencion.Independiente], condicionVigente.Datos.Ambitos);
        Assert.Equal(TherapEase.Domain.Compartido.Entidades.Enums.CondicionRegistro.Baja, condicionBaja.Datos!.Condicion);
        Assert.Empty(condicionBaja.Datos.Ambitos);
        Assert.Null(inexistente.Datos!.Condicion);
        Assert.Equal("Paciente Vigente", Assert.Single(nombres.Datos!).Value);
    }

    // ──── CONSULTAS: AGENDA CON POSTGRESQL REAL ─────────────────────────────────────────────

    [Fact]
    public async Task ConsultarAgenda_ConPostgreSqlReal_OrdenaFiltraPorVistaYNuncaMuestraNombresDeBaja()
    {
        using var e = await PrepararAsync();
        var hoy = HoyEnHermosillo();
        var paciente = await RegistrarPacienteAsync(e, "Paciente Ficticio");
        var otro = await RegistrarPacienteAsync(e, "Otra Persona", AmbitoAtencion.Escolar);

        var dia1 = hoy.AddDays(1);
        var dia2 = hoy.AddDays(2);
        var a = await AgendarAsync(e, Datos(paciente, 10, 0, 11, 0, fecha: dia2));
        var b = await AgendarAsync(e, Datos(otro, 15, 0, 15, 30, fecha: dia1));
        var c = await AgendarAsync(e, Datos(paciente, 9, 0, 10, 0, AmbitoAtencion.Independiente, dia1));

        // D12: una cita que el horario nuevo no admitiría se conserva y la agenda la muestra tal cual.
        var fuera = await InsertarDirectoAsync(
            e, paciente, HoraHermosillo.AInstante(dia1, new TimeOnly(23, 45)), HoraHermosillo.AInstante(dia1, new TimeOnly(23, 59)));

        var activas = await e.Citas(s => s.ConsultarAgendaAsync(null, null));

        Assert.True(activas.Exito, activas.Mensaje);
        Assert.Equal([c, b, fuera, a], activas.Datos!.Citas.Select(x => x.IdCita));
        Assert.Equal(hoy, activas.Datos.HoyEnHermosillo);
        Assert.False(activas.Datos.HayMas);
        var filaC = activas.Datos.Citas[0];
        Assert.Equal(dia1.ToDateTime(new TimeOnly(9, 0)), filaC.InicioLocal);
        Assert.Equal(dia1.ToDateTime(new TimeOnly(10, 0)), filaC.FinLocal);
        Assert.Equal(AmbitoAtencion.Independiente, filaC.Ambito);
        Assert.Equal("Paciente Ficticio", filaC.NombreDelPaciente);
        Assert.Equal(dia1.ToDateTime(new TimeOnly(23, 45)), activas.Datos.Citas[2].InicioLocal);
        Assert.Equal("Otra Persona", activas.Datos.Citas[1].NombreDelPaciente);

        // Cancelar libera el horario y la mueve a la vista de canceladas; el paciente de baja sigue sin nombre ni identificador.
        await CancelarDirectoAsync(e, b);
        var baja = await e.EnAlcance(p => p.GetRequiredService<ICoordinadorDeBajaDePaciente>().DarDeBajaAsync(otro, 1));
        Assert.True(baja.Exito, baja.Mensaje);

        var activasDespues = await e.Citas(s => s.ConsultarAgendaAsync(hoy, hoy.AddDays(6), VistaDeAgenda.Activas));
        var canceladas = await e.Citas(s => s.ConsultarAgendaAsync(hoy, hoy.AddDays(6), VistaDeAgenda.Canceladas));

        Assert.Equal([c, fuera, a], activasDespues.Datos!.Citas.Select(x => x.IdCita));
        var cancelada = Assert.Single(canceladas.Datos!.Citas);
        Assert.Equal(b, cancelada.IdCita);
        Assert.Equal(EstadoCita.Cancelada, cancelada.Estado);
        Assert.True(cancelada.PacienteDeBaja);
        Assert.Null(cancelada.NombreDelPaciente);
        Assert.Null(cancelada.IdPaciente);
    }

    [Fact]
    public async Task ConsultarAgenda_ConPostgreSqlReal_IncluyeLasQueSeSolapanConElPeriodoYExcluyeLasContiguas()
    {
        using var e = await PrepararAsync();
        var dia = HoyEnHermosillo().AddDays(10);
        var paciente = await RegistrarPacienteAsync(e);

        // Termina justo cuando empieza el día y otra empieza justo cuando el día termina: ninguna es del periodo de ese solo día.
        var termina = await InsertarDirectoAsync(
            e, paciente, HoraHermosillo.AInstante(dia.AddDays(-1), new TimeOnly(23, 0)), HoraHermosillo.AInstante(dia, TimeOnly.MinValue));
        var empieza = await InsertarDirectoAsync(
            e, paciente, HoraHermosillo.AInstante(dia.AddDays(1), TimeOnly.MinValue), HoraHermosillo.AInstante(dia.AddDays(1), new TimeOnly(1, 0)));

        // Cruza la medianoche entre dos días: se solapa con el periodo de cualquiera de los dos.
        var cruza = await InsertarDirectoAsync(
            e, paciente, HoraHermosillo.AInstante(dia.AddDays(3), new TimeOnly(23, 30)), HoraHermosillo.AInstante(dia.AddDays(4), new TimeOnly(0, 30)));

        var soloElDia = await e.Citas(s => s.ConsultarAgendaAsync(dia, dia));
        var diaAnterior = await e.Citas(s => s.ConsultarAgendaAsync(dia.AddDays(-1), dia.AddDays(-1)));
        var diaSiguiente = await e.Citas(s => s.ConsultarAgendaAsync(dia.AddDays(1), dia.AddDays(1)));
        var antesDeLaMedianoche = await e.Citas(s => s.ConsultarAgendaAsync(dia.AddDays(3), dia.AddDays(3)));
        var despuesDeLaMedianoche = await e.Citas(s => s.ConsultarAgendaAsync(dia.AddDays(4), dia.AddDays(4)));

        Assert.Empty(soloElDia.Datos!.Citas);
        Assert.Equal([termina], diaAnterior.Datos!.Citas.Select(x => x.IdCita));
        Assert.Equal([empieza], diaSiguiente.Datos!.Citas.Select(x => x.IdCita));
        Assert.Equal([cruza], antesDeLaMedianoche.Datos!.Citas.Select(x => x.IdCita));
        Assert.Equal([cruza], despuesDeLaMedianoche.Datos!.Citas.Select(x => x.IdCita));
    }

    [Fact]
    public async Task ConsultarAgenda_ConPostgreSqlReal_LaLeyendaDeRegistroTardioSaleDeLaFechaDeAltaDelServidor()
    {
        using var e = await PrepararAsync();
        var hoy = HoyEnHermosillo();
        var paciente = await RegistrarPacienteAsync(e);

        // Agendada por el servicio para ayer: el servidor la guarda ahora, después de su horario.
        var tardia = await AgendarAsync(e, Datos(paciente, fecha: hoy.AddDays(-1)));

        // Insertada con la fecha de alta anterior a su inicio: no lleva la leyenda.
        var inicio = HoraHermosillo.AInstante(hoy.AddDays(1), new TimeOnly(10, 0));
        var puntual = await InsertarDirectoAsync(e, paciente, inicio, inicio.AddHours(1), alta: inicio.AddDays(-1));

        var respuesta = await e.Citas(s => s.ConsultarAgendaAsync(hoy.AddDays(-1), hoy.AddDays(1)));

        var filas = respuesta.Datos!.Citas.ToDictionary(x => x.IdCita);
        Assert.True(filas[tardia].RegistradaDespuesDeSuHorario);
        Assert.False(filas[puntual].RegistradaDespuesDeSuHorario);
        var evento = Assert.Single(await e.EventosDeCitasAsync());
        Assert.Equal(tardia, evento.IdRegistro);
        Assert.Equal("Se agendó una cita registrada después de su horario.", evento.Hecho);
    }

    [Fact]
    public async Task ObtenerCita_ConPostgreSqlReal_MuestraElDetalleYNoEncuentraLasDeBajaNiLasInexistentes()
    {
        using var e = await PrepararAsync();
        var paciente = await RegistrarPacienteAsync(e);
        var id = await AgendarAsync(e, Datos(paciente));

        var detalle = await e.Citas(s => s.ObtenerCitaAsync(id));
        var inexistente = await e.Citas(s => s.ObtenerCitaAsync(Guid.NewGuid()));

        Assert.True(detalle.Exito, detalle.Mensaje);
        Assert.Equal(EstadoPago.Pendiente, detalle.Datos!.EstadoPago);
        Assert.Equal(1, detalle.Datos.Version);
        Assert.Equal("Paciente Ficticio", detalle.Datos.NombreDelPaciente);
        Assert.Equal(MensajesDeCitas.CitaNoEncontrada, inexistente.Mensaje);

        // Una cita dada de baja se trata igual que una inexistente.
        await using (var contexto = e.Escenario.Contexto())
        {
            var cita = await contexto.Citas.SingleAsync(c => c.IdCita == id);
            ReglasDeCita.DarDeBaja(cita, cita.Version, e.Actor, DateTimeOffset.UtcNow);
            await contexto.SaveChangesAsync();
        }

        var deBaja = await e.Citas(s => s.ObtenerCitaAsync(id));

        Assert.Equal(MensajesDeCitas.CitaNoEncontrada, deBaja.Mensaje);
    }
}
