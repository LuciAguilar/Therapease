using System.Data.Common;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;
using TherapEase.Application.Citas.Constantes;
using TherapEase.Application.Citas.Modelos;
using TherapEase.Application.Compartido.Modelos;
using TherapEase.Domain.Citas.Entidades;
using TherapEase.Domain.Citas.Entidades.Enums;

namespace TherapEase.IntegrationTests;

/// <summary>
/// Fija el orden entre lecturas, escrituras y confirmación del pago.
/// Comprueba el bloqueo entre las conexiones concretas, no solo que exista alguna espera.
/// </summary>
public sealed partial class PagoDeCitasRevisionTests
{


    // ──── TRANSACCIÓN ABIERTA ─────────────────────────────────────────────────────────────


    /// Una segunda corrección espera a la primera; confirmar produce conflicto y revertir permite guardarla, en ambos sentidos del pago.
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task DosCorreccionesEsperanLaConexionCorrectaYRespetanConfirmacionOReversion(bool pagada, bool revertir)
    {
        // Arrange
        var pausa = new PausaDePago(fallar: revertir);
        var segundaEscritura = new ObservarGuardadoDePago();
        using var e = await PacientesRevision.CrearAsync(postgres, interceptores: [pausa, segundaEscritura]);
        var (_, cita) = await PrepararAsync(e);
        if (pagada) Assert.True((await CambiarAsync(e, cita, 1, EstadoPago.Pagado)).Exito);
        var version = pagada ? 2 : 1;
        var destino = pagada ? EstadoPago.Pendiente : EstadoPago.Pagado;
        pausa.Activar();
        RespuestaServicio<ResultadoDeCambioDePago>? respuestaPrimera = null;
        Task<RespuestaServicio<ResultadoDeCambioDePago>>? segunda = null;

        // Act
        var primera = Record.ExceptionAsync(async () => respuestaPrimera = await CambiarAsync(e, cita, version, destino));
        try
        {
            var pidPrimera = await pausa.Alcanzada.Task.WaitAsync(TimeSpan.FromSeconds(20));
            segundaEscritura.Activar();
            segunda = CambiarAsync(e, cita, version, destino);
            var pidSegunda = await segundaEscritura.Alcanzada.Task.WaitAsync(TimeSpan.FromSeconds(20));
            await ExigirBloqueoAsync(e, pidPrimera, pidSegunda, segunda);
        }
        finally
        {
            pausa.Continuar.TrySetResult();
            await primera.WaitAsync(TimeSpan.FromSeconds(20));
            if (segunda is not null) await segunda.WaitAsync(TimeSpan.FromSeconds(20));
        }

        // Assert
        var error = await primera;
        Assert.NotNull(segunda);
        if (revertir)
        {
            Assert.IsType<InvalidOperationException>(error);
            Assert.Null(respuestaPrimera);
            Assert.Equal(EstadoDeCambioDePago.Actualizado, (await segunda).Datos!.Estado);
        }
        else
        {
            Assert.Null(error);
            Assert.True(respuestaPrimera!.Exito);
            Assert.Equal(MensajesDeCitas.ConflictoDeVersion, (await segunda).Mensaje);
            Assert.Null((await segunda).Datos);
        }
        var guardada = await LeerAsync(e, cita);
        Assert.Equal((destino, version + 1), (guardada.EstadoPago, guardada.Version));
        Assert.Equal((pagada ? 1 : 0) + 1, await EventosAsync(e, cita));
    }


    // ──── RESPUESTA INTERRUMPIDA ───────────────────────────────────────────────────────────


    /// Un fallo antes de confirmar revierte; uno después no borra lo confirmado. La recarga y el reenvío antiguo distinguen ambos casos.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RespuestaPerdidaAntesODespuesDeConfirmarNoAtribuyeUnExitoIncierto(bool confirmado)
    {
        // Arrange
        var pausa = new PausaDePago(fallar: true, despues: confirmado);
        using var e = await PacientesRevision.CrearAsync(postgres, interceptores: [pausa]);
        var (_, cita) = await PrepararAsync(e);
        pausa.Activar();
        RespuestaServicio<ResultadoDeCambioDePago>? respuesta = null;

        // Act
        var enCurso = Record.ExceptionAsync(async () => respuesta = await CambiarAsync(e, cita, 1, EstadoPago.Pagado));
        try
        {
            await pausa.Alcanzada.Task.WaitAsync(TimeSpan.FromSeconds(20));
            var observada = await LeerAsync(e, cita);
            Assert.Equal(confirmado ? EstadoPago.Pagado : EstadoPago.Pendiente, observada.EstadoPago);
        }
        finally
        {
            pausa.Continuar.TrySetResult();
            await enCurso.WaitAsync(TimeSpan.FromSeconds(20));
        }

        // Assert
        Assert.IsType<InvalidOperationException>(await enCurso);
        Assert.Null(respuesta);
        Assert.Equal(confirmado ? 1 : 0, await EventosAsync(e, cita));
        Assert.Equal(confirmado ? 2 : 1, (await LeerAsync(e, cita)).Version);

        // Act: un envío explícito posterior conserva la versión original.
        var reenvio = await CambiarAsync(e, cita, 1, EstadoPago.Pagado);

        // Assert
        if (confirmado)
        {
            Assert.Equal(MensajesDeCitas.ConflictoDeVersion, reenvio.Mensaje);
            Assert.Null(reenvio.Datos);
        }
        else Assert.Equal(EstadoDeCambioDePago.Actualizado, reenvio.Datos!.Estado);
        Assert.Equal(1, await EventosAsync(e, cita));
        Assert.Equal(2, (await LeerAsync(e, cita)).Version);
    }


    // ──── SIN CAMBIOS AL MOMENTO DE LEER ───────────────────────────────────────────────────


    /// La lectura sin cambios conserva su instante aunque otra solicitud cambie el pago antes de responder; no genera un segundo evento.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SinCambiosDescribeLaLecturaAunqueOtraSolicitudConfirmeDespues(bool pagada)
    {
        // Arrange
        var pausa = new PausaDeLecturaDePago(1);
        using var e = await PacientesRevision.CrearAsync(postgres, interceptores: [pausa]);
        var (_, cita) = await PrepararAsync(e);
        if (pagada) Assert.True((await CambiarAsync(e, cita, 1, EstadoPago.Pagado)).Exito);
        var version = pagada ? 2 : 1;
        var inicial = pagada ? EstadoPago.Pagado : EstadoPago.Pendiente;
        var otro = pagada ? EstadoPago.Pendiente : EstadoPago.Pagado;
        pausa.Activar();

        // Act
        var enCurso = CambiarAsync(e, cita, version, inicial);
        try
        {
            await pausa.Alcanzada.Task.WaitAsync(TimeSpan.FromSeconds(20));
            Assert.Equal(EstadoDeCambioDePago.Actualizado, (await CambiarAsync(e, cita, version, otro)).Datos!.Estado);
        }
        finally
        {
            pausa.Continuar.TrySetResult();
            await enCurso.WaitAsync(TimeSpan.FromSeconds(20));
        }

        // Assert
        var respuesta = await enCurso;
        Assert.Equal(EstadoDeCambioDePago.SinCambios, respuesta.Datos!.Estado);
        Assert.Equal((inicial, version), (respuesta.Datos.Pago.EstadoPago, respuesta.Datos.Pago.Version));
        var guardada = await LeerAsync(e, cita);
        Assert.Equal((otro, version + 1), (guardada.EstadoPago, guardada.Version));
        Assert.Equal((pagada ? 1 : 0) + 1, await EventosAsync(e, cita));
    }

    /// Dos solicitudes cuyo pago ya coincide terminan ambas sin cambios, sin escribir ni generar evento.
    [Fact]
    public async Task DosEnviosQueYaCoincidenRespondenSinCambiosSinEscribir()
    {
        // Arrange
        var pausa = new PausaDeLecturaDePago(2);
        using var e = await PacientesRevision.CrearAsync(postgres, interceptores: [pausa]);
        var (_, cita) = await PrepararAsync(e);
        e.Comandos.Limpiar();
        pausa.Activar();

        // Act
        var tareas = new[] { CambiarAsync(e, cita, 1, EstadoPago.Pendiente), CambiarAsync(e, cita, 1, EstadoPago.Pendiente) };
        try { await pausa.Alcanzada.Task.WaitAsync(TimeSpan.FromSeconds(20)); }
        finally
        {
            pausa.Continuar.TrySetResult();
            await Task.WhenAll(tareas).WaitAsync(TimeSpan.FromSeconds(20));
        }

        // Assert
        Assert.All(await Task.WhenAll(tareas), r => Assert.Equal(EstadoDeCambioDePago.SinCambios, r.Datos!.Estado));
        Assert.Equal(1, (await LeerAsync(e, cita)).Version);
        Assert.Equal(0, await EventosAsync(e, cita));
        Assert.DoesNotContain(e.Comandos.Textos, t => t.Contains("UPDATE \"Cita\"", StringComparison.Ordinal)
            || t.Contains("INSERT INTO \"EventoAuditoria\"", StringComparison.Ordinal));
    }


    // ──── SEÑALES DE PRUEBA ────────────────────────────────────────────────────────────────


    /// Exige que PostgreSQL identifique a la primera conexión como bloqueo de la segunda.
    private static async Task ExigirBloqueoAsync(PacientesRevision e, int primera, int segunda, Task operacion)
    {
        Assert.NotEqual(primera, segunda);
        await using var conexion = new NpgsqlConnection(e.Escenario.Migrador);
        await conexion.OpenAsync();
        var reloj = Stopwatch.StartNew();
        while (reloj.Elapsed < TimeSpan.FromSeconds(15) && !operacion.IsCompleted)
        {
            await using var comando = new NpgsqlCommand("SELECT @primera = ANY(pg_blocking_pids(@segunda))", conexion);
            comando.Parameters.AddWithValue("primera", primera);
            comando.Parameters.AddWithValue("segunda", segunda);
            if ((bool)(await comando.ExecuteScalarAsync())!) return;
            await Task.Delay(20);
        }
        Assert.Fail("PostgreSQL no confirmó el bloqueo entre las conexiones elegidas.");
    }

    /// <summary>
    /// Pausa una confirmación elegida, antes o después de guardarse en PostgreSQL.
    /// El fallo opcional simula que se pierde el resultado del servicio, sin simular un corte HTTP.
    /// </summary>
    private sealed class PausaDePago(bool fallar = false, bool despues = false) : DbTransactionInterceptor
    {
        private int _activa;
        public TaskCompletionSource<int> Alcanzada { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Continuar { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public void Activar() => Volatile.Write(ref _activa, 1);

        public override async ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaccion,
            TransactionEventData datos, InterceptionResult resultado, CancellationToken cancelacion = default)
        {
            if (!despues) await PausarAsync(transaccion, cancelacion);
            return resultado;
        }

        public override async Task TransactionCommittedAsync(DbTransaction transaccion,
            TransactionEndEventData datos, CancellationToken cancelacion = default)
        {
            if (despues) await PausarAsync(transaccion, cancelacion);
        }

        private async Task PausarAsync(DbTransaction transaccion, CancellationToken cancelacion)
        {
            if (Interlocked.Exchange(ref _activa, 0) != 1) return;
            Alcanzada.TrySetResult(((NpgsqlConnection)transaccion.Connection!).ProcessID);
            await Continuar.Task.WaitAsync(TimeSpan.FromSeconds(20), cancelacion);
            if (fallar) throw new InvalidOperationException("Interrupción ficticia del resultado de pago.");
        }
    }

    /// <summary>
    /// Identifica la conexión del segundo pago antes de escribir, para observar después su bloqueo real.
    /// </summary>
    private sealed class ObservarGuardadoDePago : SaveChangesInterceptor
    {
        private int _activa;
        public TaskCompletionSource<int> Alcanzada { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public void Activar() => Volatile.Write(ref _activa, 1);

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData datos,
            InterceptionResult<int> resultado, CancellationToken cancelacion = default)
        {
            if (datos.Context!.ChangeTracker.Entries<Cita>().Any(c => c.State == EntityState.Modified)
                && Interlocked.Exchange(ref _activa, 0) == 1)
                Alcanzada.TrySetResult(((NpgsqlConnection)datos.Context.Database.GetDbConnection()).ProcessID);
            return new(resultado);
        }
    }

    /// <summary>
    /// Retiene resultados de lecturas ya ejecutadas, antes de que el servicio evalúe si hay cambios.
    /// Así la prueba fija la instantánea leída sin añadir esperas al código de producción.
    /// </summary>
    private sealed class PausaDeLecturaDePago(int cantidad) : DbCommandInterceptor
    {
        private int _activa;
        private int _llegadas;
        public TaskCompletionSource Alcanzada { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Continuar { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public void Activar() => Volatile.Write(ref _activa, 1);

        public override async ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand comando, CommandExecutedEventData datos,
            DbDataReader resultado, CancellationToken cancelacion = default)
        {
            if (Volatile.Read(ref _activa) == 1 && comando.CommandText.Contains("FROM \"Cita\"", StringComparison.Ordinal))
            {
                var llegada = Interlocked.Increment(ref _llegadas);
                if (llegada == cantidad)
                {
                    Volatile.Write(ref _activa, 0);
                    Alcanzada.TrySetResult();
                }
                if (llegada <= cantidad) await Continuar.Task.WaitAsync(TimeSpan.FromSeconds(20), cancelacion);
            }
            return resultado;
        }
    }
}
