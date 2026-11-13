using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TherapEase.Application.Citas.Constantes;
using TherapEase.Application.Citas.Interfaces.Servicios;
using TherapEase.Application.Citas.Modelos;
using TherapEase.Application.Compartido.Modelos;
using TherapEase.Domain.Auditoria.Entidades.Enums;
using TherapEase.Domain.Citas.Entidades;
using TherapEase.Domain.Pacientes.Entidades.Enums;

namespace TherapEase.IntegrationTests;

/// <summary>
/// Revisa dobles envíos y reservas distintas que ya terminaron sus comprobaciones antes de guardar.
/// También comprueba horario, consultas y qué queda si se pierde una respuesta del servicio.
/// </summary>
public sealed partial class AgendaYReservasTests(PostgreSqlRevision postgres) : IClassFixture<PostgreSqlRevision>
{


    // ──── ENVÍOS CONCURRENTES ───────────────────────────────────────────────────────────────


    /// Dos o diez reservas llegan al guardado juntas; una confirma y las demás reciben un conflicto real.
    [Theory]
    [InlineData(2)]
    [InlineData(10)]
    public async Task ReservasDistintasCoincidenAntesDeInsertarYUnaSolaConfirma(int cantidad)
    {

        // Arrange
        var barrera = new BarreraDeCitas(cantidad);
        using var e = await PacientesRevision.CrearAsync(postgres, interceptores: [barrera]);
        var paciente = await e.RegistrarAsync();

        // Act
        var resultados = await Task.WhenAll(Enumerable.Range(0, cantidad).Select(_ =>
            e.EnSolicitudAsync(s => s.Obtener<IServicioCitas>().AgendarAsync(Guid.NewGuid(), Datos(paciente)))));

        // Assert
        Assert.Equal(cantidad, barrera.Llegadas);
        Assert.Single(resultados, r => r.Exito && r.Datos!.Estado == EstadoDeAgendar.Agendada);
        Assert.Equal(cantidad - 1, resultados.Count(r => !r.Exito && r.Mensaje == MensajesDeCitas.HorarioOcupado));
        await ComprobarUnaCitaYUnEventoAsync(e);
    }

    /// Un número compartido reconoce solo los mismos datos y actor, sin duplicar la cita ni revelar otra reserva.
    [Theory]
    [InlineData("mismos")]
    [InlineData("otros datos")]
    [InlineData("otro actor")]
    public async Task DobleEnvioAntesDelGuardadoComparaDatosYActor(string variante)
    {

        // Arrange
        var barrera = new BarreraDeCitas(2);
        using var e = await PacientesRevision.CrearAsync(postgres, interceptores: [barrera]);
        var paciente = await e.RegistrarAsync();
        var numero = Guid.NewGuid();
        var cookie = e.Cookie;
        if (variante == "otro actor")
        {
            await ServidorIdentidadRevision.CrearAsync(e.Proveedor, "segunda-operadora-ficticia", "Usuaria");
            cookie = await ServidorIdentidadRevision.EntrarAsync(e.Proveedor, "segunda-operadora-ficticia");
        }

        var datosSegundo = variante == "otros datos"
            ? Datos(paciente) with { HoraInicio = new TimeOnly(12, 0), HoraFin = new TimeOnly(13, 0) }
            : Datos(paciente);

        // Act
        var resultados = await Task.WhenAll(
            e.EnSolicitudAsync(s => s.Obtener<IServicioCitas>().AgendarAsync(numero, Datos(paciente))),
            e.EnSolicitudAsync(s => s.Obtener<IServicioCitas>().AgendarAsync(numero, datosSegundo), cookie));

        // Assert
        Assert.Equal(2, barrera.Llegadas);
        Assert.Single(resultados, r => r.Datos?.Estado == EstadoDeAgendar.Agendada);
        if (variante == "mismos")
        {
            Assert.All(resultados, r => Assert.True(r.Exito, r.Mensaje));
            Assert.Single(resultados, r => r.Datos!.Estado == EstadoDeAgendar.YaAgendada);
        }
        else
        {
            var rechazada = Assert.Single(resultados, r => !r.Exito);
            Assert.Equal(MensajesDeCitas.SolicitudYaUsada, rechazada.Mensaje);
            Assert.Null(rechazada.Datos);
        }

        await ComprobarUnaCitaYUnEventoAsync(e);
    }


    // ──── RESPUESTA PERDIDA EN EL SERVICIO ──────────────────────────────────────────────────


    /// Tras perder el resultado antes o después de confirmar, reenviar reconoce únicamente lo guardado de verdad.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReenvioTrasPerderResultadoDelServicioNoInventaNiDuplicaGuardado(bool confirmado)
    {

        // Arrange
        var fallo = new FallarAntesDeGuardar();
        using var e = await PacientesRevision.CrearAsync(postgres, interceptores: [fallo]);
        var paciente = await e.RegistrarAsync();
        var numero = Guid.NewGuid();
        if (!confirmado)
            fallo.Activar();

        // Act
        var error = await Record.ExceptionAsync(async () =>
        {
            var guardada = await e.EnSolicitudAsync(s => s.Obtener<IServicioCitas>().AgendarAsync(numero, Datos(paciente)));
            Assert.True(guardada.Exito, guardada.Mensaje);
            throw new InvalidOperationException("Resultado ficticio perdido después de confirmar.");
        });

        // Assert
        Assert.IsType<InvalidOperationException>(error);
        Assert.Equal(confirmado ? 0 : 1, fallo.Llegadas);
        await using (var antes = e.Escenario.Contexto())
        {
            Assert.Equal(confirmado ? 1 : 0, await antes.Citas.CountAsync());
            Assert.Equal(confirmado ? 1 : 0, await antes.EventosAuditoria.CountAsync(v => v.TipoRegistro == TipoRegistroAuditoria.Cita));
        }

        // Act
        var resultadoRecibido = await e.EnSolicitudAsync(s => s.Obtener<IServicioCitas>().AgendarAsync(numero, Datos(paciente)));

        // Assert
        Assert.True(resultadoRecibido.Exito, resultadoRecibido.Mensaje);
        Assert.Equal(confirmado ? EstadoDeAgendar.YaAgendada : EstadoDeAgendar.Agendada, resultadoRecibido.Datos!.Estado);
        await ComprobarUnaCitaYUnEventoAsync(e);
    }


    // ──── HORARIO Y ENTRADAS LÍMITE ─────────────────────────────────────────────────────────


    /// El servicio acepta los horarios decididos y rechaza segundos, cierres tardíos o fechas no representables sin guardar.
    [Theory]
    [InlineData(9, 0, 10, 0, 0, false, true)]
    [InlineData(14, 30, 15, 30, 0, false, true)]
    [InlineData(15, 15, 15, 45, 0, false, true)]
    [InlineData(18, 30, 19, 0, 0, false, true)]
    [InlineData(18, 31, 19, 1, 0, false, false)]
    [InlineData(14, 30, 15, 0, 0, false, false)]
    [InlineData(9, 0, 10, 0, 1, false, false)]
    [InlineData(17, 0, 18, 0, 0, true, false)]
    public async Task HorariosYFechaLimiteSeValidanAntesDeGuardar(int hi, int mi, int hf, int mf, int segundos, bool fechaMaxima, bool valido)
    {

        // Arrange
        using var e = await PacientesRevision.CrearAsync(postgres);
        var paciente = await e.RegistrarAsync();
        var datos = Datos(paciente) with
        {
            Fecha = fechaMaxima ? DateOnly.MaxValue : new DateOnly(2026, 12, 1),
            HoraInicio = new TimeOnly(hi, mi, segundos),
            HoraFin = new TimeOnly(hf, mf)
        };

        // Act
        var resultado = await e.EnSolicitudAsync(s => s.Obtener<IServicioCitas>().AgendarAsync(Guid.NewGuid(), datos));

        // Assert
        Assert.Equal(valido, resultado.Exito);
        await using var db = e.Escenario.Contexto();
        Assert.Equal(valido ? 1 : 0, await db.Citas.CountAsync());
        Assert.Equal(valido ? 1 : 0, await db.EventosAuditoria.CountAsync(v => v.TipoRegistro == TipoRegistroAuditoria.Cita));
        if (valido)
        {
            var cita = await db.Citas.SingleAsync();
            Assert.Equal(new DateTimeOffset(new DateTime(2026, 12, 1, hi, mi, segundos, DateTimeKind.Utc).AddHours(7)), cita.Inicio);
        }
    }


    // ──── DATOS, VERIFICACIÓN Y SEÑALES ────────────────────────────────────────────────────


    /// La reserva utiliza una fecha y hora locales fijas de Hermosillo.
    private static DatosDeCitaEntrada Datos(Guid paciente) =>
        new(paciente, AmbitoAtencion.Escolar, new DateOnly(2026, 12, 1), new TimeOnly(10, 0), new TimeOnly(11, 0));

    /// Relee desde otra conexión para comparar la cita confirmada con su único evento.
    private static async Task ComprobarUnaCitaYUnEventoAsync(PacientesRevision e)
    {
        await using var db = e.Escenario.Contexto();
        var cita = await db.Citas.SingleAsync();
        var evento = await db.EventosAuditoria.SingleAsync(v => v.TipoRegistro == TipoRegistroAuditoria.Cita);
        Assert.Equal(cita.IdCita, evento.IdRegistro);
        Assert.Equal(cita.IdUsuarioAlta, evento.IdUsuarioActor);
    }

    /// <summary>
    /// Retiene todas las reservas elegidas antes de enviar sus escrituras, hasta que llegue la cantidad prevista.
    /// Demuestra que pasaron las comprobaciones sin depender de que el sistema las ejecute a la vez por suerte.
    /// </summary>
    private sealed class BarreraDeCitas(int cantidad) : SaveChangesInterceptor
    {
        private int _llegadas;
        private readonly TaskCompletionSource _todas = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int Llegadas => Volatile.Read(ref _llegadas);

        /// Libera las solicitudes solo cuando todas alcanzaron el guardado de una cita nueva.
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData datos,
            InterceptionResult<int> resultado, CancellationToken cancelacion = default)
        {
            if (datos.Context!.ChangeTracker.Entries<Cita>().Any(c => c.State == EntityState.Added))
            {
                if (Interlocked.Increment(ref _llegadas) == cantidad)
                    _todas.TrySetResult();
                await _todas.Task.WaitAsync(TimeSpan.FromSeconds(20), cancelacion);
            }

            return resultado;
        }
    }

    /// <summary>
    /// Provoca un fallo de prueba antes de guardar una cita y su evento, una sola vez.
    /// El segundo envío podrá intentar el alta porque el primero no confirmó nada.
    /// </summary>
    private sealed class FallarAntesDeGuardar : SaveChangesInterceptor
    {
        private int _activo;
        public int Llegadas { get; private set; }

        /// Prepara el fallo después del registro del paciente.
        public void Activar() => Volatile.Write(ref _activo, 1);

        /// Rechaza la primera escritura elegida sin enviar sus cambios a PostgreSQL.
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData datos,
            InterceptionResult<int> resultado, CancellationToken cancelacion = default)
        {
            if (datos.Context!.ChangeTracker.Entries<Cita>().Any(c => c.State == EntityState.Added)
                && Interlocked.Exchange(ref _activo, 0) == 1)
            {
                Llegadas++;
                throw new InvalidOperationException("Fallo ficticio antes de guardar.");
            }
            return new(resultado);
        }
    }
}
