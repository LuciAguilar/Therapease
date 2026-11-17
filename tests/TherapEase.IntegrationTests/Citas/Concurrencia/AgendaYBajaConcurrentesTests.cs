using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TherapEase.Application.Citas.Constantes;
using TherapEase.Application.Citas.Interfaces.Servicios;
using TherapEase.Application.Citas.Modelos;
using TherapEase.Application.Coordinadores.Interfaces.Servicios;
using TherapEase.Domain.Auditoria.Entidades.Enums;
using TherapEase.Domain.Citas.Entidades;
using TherapEase.Domain.Compartido.Entidades.Enums;
using TherapEase.Domain.Pacientes.Entidades;
using TherapEase.Domain.Pacientes.Entidades.Enums;

namespace TherapEase.IntegrationTests;

/// <summary>
/// Fuerza cambios entre comprobar y guardar una cita o una baja.
/// Usa sesiones reales de QA y comprueba lo confirmado desde otra conexión.
/// </summary>
public sealed partial class AgendaYBajaConcurrentesTests(PostgreSqlRevision postgres) : IClassFixture<PostgreSqlRevision>
{


    // ──── BAJA Y CITA ENTRE LA COMPROBACIÓN Y EL GUARDADO ────────────────────────────────


    /// La baja confirma después de comprobar el paciente y antes de guardar la cita.
    [Fact]
    public async Task BajaConfirmaAntesDelGuardadoDeCitaNoDejaUnaCitaActiva()
    {
        // Arrange
        var pausa = new PausaDeGuardado<Cita>(EntityState.Added);
        using var entorno = await PacientesRevision.CrearAsync(postgres, interceptores: [pausa]);
        var paciente = await entorno.RegistrarAsync();
        pausa.Activar();

        // Act
        var respuesta = await MientrasSePausaAsync(pausa,
            () => entorno.EnSolicitudAsync(s => s.Obtener<IServicioCitas>().AgendarAsync(Guid.NewGuid(), Datos(paciente))),
            async () =>
            {
                var baja = await entorno.EnSolicitudAsync(s =>
                    s.Obtener<ICoordinadorDeBajaDePaciente>().DarDeBajaAsync(paciente, 1));
                Assert.True(baja.Exito, baja.Mensaje);
            });

        // Assert
        Assert.False(respuesta.Exito);
        Assert.Equal(MensajesDeCitas.PacienteDeBaja, respuesta.Mensaje);
        Assert.Equal(CondicionRegistro.Baja, (await entorno.FichaUnicaAsync()).Condicion);
        await using var contexto = entorno.Escenario.Contexto();
        Assert.Empty(await contexto.Citas.ToListAsync());
        Assert.Empty(await contexto.EventosAuditoria.Where(e => e.TipoRegistro == TipoRegistroAuditoria.Cita).ToListAsync());
        Assert.Single(await contexto.EventosAuditoria.Where(e => e.Accion == AccionAuditoria.Baja).ToListAsync());
        Assert.Equal(1, pausa.Llegadas);
    }

    /// La cita confirma después de comprobar las citas del paciente y antes de guardar la baja.
    [Fact]
    public async Task CitaConfirmaAntesDelGuardadoDeBajaConservaPacienteVigente()
    {
        // Arrange
        var pausa = new PausaDeGuardado<Paciente>(EntityState.Modified);
        using var entorno = await PacientesRevision.CrearAsync(postgres, interceptores: [pausa]);
        var paciente = await entorno.RegistrarAsync();
        pausa.Activar();

        // Act
        var respuesta = await MientrasSePausaAsync(pausa,
            () => entorno.EnSolicitudAsync(s => s.Obtener<ICoordinadorDeBajaDePaciente>().DarDeBajaAsync(paciente, 1)),
            async () =>
            {
                var cita = await entorno.EnSolicitudAsync(s =>
                    s.Obtener<IServicioCitas>().AgendarAsync(Guid.NewGuid(), Datos(paciente)));
                Assert.True(cita.Exito, cita.Mensaje);
            });

        // Assert
        Assert.False(respuesta.Exito);
        Assert.Equal("No puede haber una cita activa de un paciente dado de baja.", respuesta.Mensaje);
        Assert.Equal(CondicionRegistro.Vigente, (await entorno.FichaUnicaAsync()).Condicion);
        await using var contexto = entorno.Escenario.Contexto();
        Assert.Single(await contexto.Citas.ToListAsync());
        Assert.Single(await contexto.EventosAuditoria.Where(e => e.TipoRegistro == TipoRegistroAuditoria.Cita).ToListAsync());
        Assert.Empty(await contexto.EventosAuditoria.Where(e => e.Accion == AccionAuditoria.Baja).ToListAsync());
        Assert.Equal(1, pausa.Llegadas);
    }


    // ──── ÁMBITO RETIRADO Y DOBLE ENVÍO ────────────────────────────────────────────────


    /// Tras comprobar el ámbito, retirarlo permite conservarlo en la cita y el gemelo no duplica.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AmbitoRetiradoTrasComprobacionConservaCitaYReconoceGemela(bool confirmarGemela)
    {
        // Arrange
        var pausa = new PausaDeGuardado<Cita>(EntityState.Added);
        using var entorno = await PacientesRevision.CrearAsync(postgres, interceptores: [pausa]);
        var paciente = await entorno.RegistrarAsync();
        var numero = Guid.NewGuid();
        pausa.Activar();

        // Act
        var respuesta = await MientrasSePausaAsync(pausa,
            () => entorno.EnSolicitudAsync(s => s.Obtener<IServicioCitas>().AgendarAsync(numero, Datos(paciente))),
            async () =>
            {
                if (confirmarGemela)
                {
                    var gemela = await entorno.EnSolicitudAsync(s =>
                        s.Obtener<IServicioCitas>().AgendarAsync(numero, Datos(paciente)));
                    Assert.True(gemela.Exito, gemela.Mensaje);
                    Assert.Equal(EstadoDeAgendar.Agendada, gemela.Datos!.Estado);
                }

                var nuevosDatos = PacientesRevision.Adulto() with { Ambitos = [AmbitoAtencion.Independiente] };
                var edicion = await entorno.ServicioAsync(s => s.ActualizarAsync(paciente, 1, nuevosDatos));
                Assert.True(edicion.Exito, edicion.Mensaje);
            });

        // Assert
        Assert.True(respuesta.Exito, respuesta.Mensaje);
        Assert.Equal(confirmarGemela ? EstadoDeAgendar.YaAgendada : EstadoDeAgendar.Agendada, respuesta.Datos!.Estado);
        await using var contexto = entorno.Escenario.Contexto();
        var cita = Assert.Single(await contexto.Citas.ToListAsync());
        Assert.Equal(numero, cita.IdCita);
        Assert.Equal(AmbitoAtencion.Escolar, cita.Ambito);
        Assert.Single(await contexto.EventosAuditoria.Where(e => e.TipoRegistro == TipoRegistroAuditoria.Cita).ToListAsync());
        var ficha = await contexto.Pacientes.Include(p => p.Ambitos).SingleAsync();
        Assert.DoesNotContain(ficha.Ambitos, a => a.Ambito == AmbitoAtencion.Escolar);
        Assert.Equal(1, pausa.Llegadas);
    }


    /// Un fallo 40P01 preparado solo en auditoría debe conservar el error técnico y la reversión.
    [Fact]
    public async Task FalloDeAuditoriaConInterbloqueoNoDebeAnunciarHorarioOcupado()
    {
        // Arrange: el código se inyecta en el evento, sin provocar un cruce de citas.
        using var entorno = await PacientesRevision.CrearAsync(postgres);
        var paciente = await entorno.RegistrarAsync();
        await entorno.SqlAsync("""
            CREATE FUNCTION fallo_evento_revision() RETURNS trigger LANGUAGE plpgsql AS $$
            BEGIN
                RAISE EXCEPTION USING ERRCODE = '40P01', MESSAGE = 'Fallo ficticio de auditoria';
            END $$;
            CREATE TRIGGER fallo_evento_revision BEFORE INSERT ON "EventoAuditoria"
                FOR EACH ROW EXECUTE FUNCTION fallo_evento_revision();
            """);
        entorno.Comandos.Limpiar();
        TherapEase.Application.Compartido.Modelos.RespuestaServicio<ResultadoDeAgendar>? respuesta = null;

        // Act
        var error = await Record.ExceptionAsync(async () =>
        {
            respuesta = await entorno.EnSolicitudAsync(s =>
                s.Obtener<IServicioCitas>().AgendarAsync(Guid.NewGuid(), Datos(paciente)));
        });

        // Assert: la lectura independiente debe mostrar la reversión antes de juzgar el mensaje.
        await using var contexto = entorno.Escenario.Contexto();
        var citas = await contexto.Citas.CountAsync();
        var eventos = await contexto.EventosAuditoria.CountAsync(e => e.TipoRegistro == TipoRegistroAuditoria.Cita);
        var carpeta = Environment.GetEnvironmentVariable("THERAPEASE_EVIDENCIA_CITAS")
            ?? Path.Combine(ServidorRevision.RaizRepositorio(), "TestResults", "citas");
        Directory.CreateDirectory(carpeta);
        await File.WriteAllTextAsync(Path.Combine(carpeta, "auditoria-interbloqueo.json"),
            System.Text.Json.JsonSerializer.Serialize(new
            {
                FalloPreparado = "SQLSTATE 40P01 en disparador de auditoria, sin cruce",
                DevolvioHorarioOcupado = respuesta?.Mensaje == MensajesDeCitas.HorarioOcupado,
                ErrorTecnico = error is not null,
                CitasGuardadas = citas,
                EventosDeCitas = eventos
            }));
        Assert.Equal(0, citas);
        Assert.Equal(0, eventos);
        Assert.Single(entorno.Comandos.Textos, t => t.Contains("FROM \"Cita\"", StringComparison.Ordinal));
        Assert.NotNull(error);
        var tecnico = error;
        while (tecnico is not null && tecnico is not Npgsql.PostgresException)
            tecnico = tecnico.InnerException;
        Assert.True(tecnico is Npgsql.PostgresException { SqlState: "40P01" }, "Debe propagarse el fallo preparado de auditoría.");
        Assert.Null(respuesta);
        Assert.False(error is TherapEase.Domain.Compartido.Excepciones.ReglaDeNegocioException);
    }


    // ──── DATOS Y PAUSA CONTROLADA ───────────────────────────────────────────────────


    /// La fecha y las horas se interpretan como hora de Hermosillo.
    private static DatosDeCitaEntrada Datos(Guid paciente) =>
        new(paciente, AmbitoAtencion.Escolar, new DateOnly(2026, 12, 1), new TimeOnly(10, 0), new TimeOnly(11, 0));

    /// Espera la señal del guardado, ejecuta el otro cambio y libera también si falla una comprobación.
    private static async Task<TResult> MientrasSePausaAsync<TEntity, TResult>(
        PausaDeGuardado<TEntity> pausa, Func<Task<TResult>> iniciar, Func<Task> otroCambio) where TEntity : class
    {
        var enCurso = iniciar();
        try
        {
            await pausa.Alcanzada.Task.WaitAsync(TimeSpan.FromSeconds(20));
            await otroCambio();
        }
        finally
        {
            pausa.Continuar.TrySetResult();
            await enCurso.WaitAsync(TimeSpan.FromSeconds(20));
        }

        return await enCurso;
    }

    /// <summary>
    /// Detiene una única escritura del tipo indicado antes de enviar sus cambios a PostgreSQL.
    /// Las señales demuestran que la primera operación llegó al punto previsto.
    /// </summary>
    private sealed class PausaDeGuardado<TEntity>(EntityState estado) : SaveChangesInterceptor where TEntity : class
    {
        private int _activada;
        public int Llegadas { get; private set; }
        public TaskCompletionSource Alcanzada { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Continuar { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        /// Activa la pausa después de preparar paciente y sesión.
        public void Activar() => Volatile.Write(ref _activada, 1);

        /// Solo la primera escritura elegida se pausa, para que la otra solicitud pueda terminar.
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData datos, InterceptionResult<int> resultado, CancellationToken cancelacion = default)
        {
            if (datos.Context!.ChangeTracker.Entries<TEntity>().Any(e => e.State == estado)
                && Interlocked.Exchange(ref _activada, 0) == 1)
            {
                Llegadas++;
                Alcanzada.TrySetResult();
                await Continuar.Task.WaitAsync(TimeSpan.FromSeconds(20), cancelacion);
            }

            return resultado;
        }
    }
}
