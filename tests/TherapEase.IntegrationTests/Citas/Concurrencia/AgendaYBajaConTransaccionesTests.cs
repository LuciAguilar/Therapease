using System.Data.Common;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;
using TherapEase.Application.Citas.Constantes;
using TherapEase.Application.Citas.Interfaces.Servicios;
using TherapEase.Application.Citas.Modelos;
using TherapEase.Application.Coordinadores.Constantes;
using TherapEase.Application.Coordinadores.Interfaces.Servicios;
using TherapEase.Domain.Auditoria.Entidades.Enums;
using TherapEase.Domain.Compartido.Entidades.Enums;
using TherapEase.Domain.Pacientes.Entidades.Enums;

namespace TherapEase.IntegrationTests;

/// <summary>
/// Comprueba las esperas reales entre una baja y una cita, y el reenvío antes de comprobar el paciente.
/// Las señales y los bloqueos observados en PostgreSQL fijan el orden de las operaciones.
/// </summary>
public sealed partial class AgendaYBajaConcurrentesTests
{


    // ──── TRANSACCIONES ABIERTAS ──────────────────────────────────────────────────────────


    /// La segunda operación espera al cambio abierto; confirmar o revertir conserva juntos los datos y sus eventos.
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task BajaYCitaEsperanUnaTransaccionRealYRespetanSuResultado(bool citaPrimero, bool revertir)
    {

        // Arrange
        var pausa = new PausaDeConfirmacion(revertir);
        var escritura = new ObservarSegundaEscritura(citaPrimero ? "UPDATE \"Paciente\"" : "INSERT INTO \"Cita\"");
        using var entorno = await PacientesRevision.CrearAsync(postgres, interceptores: [pausa, escritura]);
        var paciente = await entorno.RegistrarAsync();
        pausa.Activar();
        (bool Exito, string Mensaje) primera = default;
        Task<(bool Exito, string Mensaje)>? segunda = null;

        // Act
        var primeraEnCurso = Record.ExceptionAsync(async () => primera = await OperarAsync(citaPrimero));
        try
        {
            var primeroPid = await pausa.Alcanzada.Task.WaitAsync(TimeSpan.FromSeconds(20));
            escritura.Activar();
            segunda = OperarAsync(!citaPrimero);
            var segundoPid = await escritura.Alcanzada.Task.WaitAsync(TimeSpan.FromSeconds(20));
            await ComprobarEsperaRealAsync(entorno.Escenario, primeroPid, segundoPid, segunda);
        }
        finally
        {
            pausa.Continuar.TrySetResult();
            await primeraEnCurso.WaitAsync(TimeSpan.FromSeconds(20));
            if (segunda is not null)
                await segunda.WaitAsync(TimeSpan.FromSeconds(20));
        }

        // Assert
        var errorPrimero = await primeraEnCurso;
        Assert.NotNull(segunda);
        var resultadoSegundo = await segunda;
        if (revertir)
        {
            Assert.IsType<InvalidOperationException>(errorPrimero);
            Assert.True(resultadoSegundo.Exito, resultadoSegundo.Mensaje);
        }
        else
        {
            Assert.Null(errorPrimero);
            Assert.True(primera.Exito, primera.Mensaje);
            Assert.False(resultadoSegundo.Exito);
            Assert.Equal(citaPrimero ? "No puede haber una cita activa de un paciente dado de baja." : MensajesDeCitas.PacienteDeBaja, resultadoSegundo.Mensaje);
        }

        var quedaCita = citaPrimero != revertir;
        await using var contexto = entorno.Escenario.Contexto();
        Assert.Equal(quedaCita ? CondicionRegistro.Vigente : CondicionRegistro.Baja, (await contexto.Pacientes.SingleAsync()).Condicion);
        Assert.Equal(quedaCita ? 1 : 0, await contexto.Citas.CountAsync());
        Assert.Equal(quedaCita ? 1 : 0, await contexto.EventosAuditoria.CountAsync(e => e.TipoRegistro == TipoRegistroAuditoria.Cita));
        Assert.Equal(quedaCita ? 0 : 1, await contexto.EventosAuditoria.CountAsync(e => e.Accion == AccionAuditoria.Baja));

        // Cada operación usa una solicitud, contexto y conexión propios.

        async Task<(bool Exito, string Mensaje)> OperarAsync(bool esCita)
        {
            if (esCita)
            {
                var r = await entorno.EnSolicitudAsync(s => s.Obtener<IServicioCitas>().AgendarAsync(Guid.NewGuid(), Datos(paciente)));
                return (r.Exito, r.Mensaje);
            }

            var baja = await entorno.EnSolicitudAsync(s => s.Obtener<ICoordinadorDeBajaDePaciente>().DarDeBajaAsync(paciente, 1));
            return (baja.Exito, baja.Mensaje);
        }
    }


    // ──── REENVÍO ANTES DE COMPROBAR EL PACIENTE ────────────────────────────────────────────


    /// Si la gemela confirma y se retira el ámbito antes de consultar el paciente, se reconoce solo la misma reserva.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GemelaConfirmadaAntesDeConsultarPacienteResuelveElRechazoDeAmbito(bool otrosDatos)
    {

        // Arrange
        var pausa = new PausaAntesDeConsultarPaciente();
        using var entorno = await PacientesRevision.CrearAsync(postgres, interceptores: [pausa]);
        var paciente = await entorno.RegistrarAsync();
        var numero = Guid.NewGuid();
        pausa.Activar();

        // Act
        var enCurso = entorno.EnSolicitudAsync(s => s.Obtener<IServicioCitas>().AgendarAsync(numero, Datos(paciente)));
        try
        {
            await pausa.Alcanzada.Task.WaitAsync(TimeSpan.FromSeconds(20));
            var datosGemela = otrosDatos ? Datos(paciente) with { HoraInicio = new TimeOnly(12, 0), HoraFin = new TimeOnly(13, 0) } : Datos(paciente);
            var gemela = await entorno.EnSolicitudAsync(s => s.Obtener<IServicioCitas>().AgendarAsync(numero, datosGemela));
            Assert.True(gemela.Exito, gemela.Mensaje);
            var retiro = await entorno.ServicioAsync(s => s.ActualizarAsync(paciente, 1,
                PacientesRevision.Adulto() with { Ambitos = [AmbitoAtencion.Independiente] }));
            Assert.True(retiro.Exito, retiro.Mensaje);
        }
        finally
        {
            pausa.Continuar.TrySetResult();
            await enCurso.WaitAsync(TimeSpan.FromSeconds(20));
        }

        // Assert
        var respuesta = await enCurso;
        Assert.Equal(!otrosDatos, respuesta.Exito);
        if (otrosDatos)
        {
            Assert.Equal(MensajesDeCitas.SolicitudYaUsada, respuesta.Mensaje);
            Assert.Null(respuesta.Datos);
        }
        else
        {
            Assert.Equal(EstadoDeAgendar.YaAgendada, respuesta.Datos!.Estado);
        }

        await using var contexto = entorno.Escenario.Contexto();
        Assert.Equal(numero, (await contexto.Citas.SingleAsync()).IdCita);
        Assert.Single(await contexto.EventosAuditoria.Where(e => e.TipoRegistro == TipoRegistroAuditoria.Cita).ToListAsync());
        Assert.DoesNotContain((await contexto.Pacientes.Include(p => p.Ambitos).SingleAsync()).Ambitos, a => a.Ambito == AmbitoAtencion.Escolar);
        Assert.Equal(1, pausa.Llegadas);
    }


    // ──── SEÑALES Y OBSERVACIÓN DE POSTGRESQL ──────────────────────────────────────────────


    /// Exige que PostgreSQL identifique a la primera conexión como quien bloquea a la segunda.
    private static async Task ComprobarEsperaRealAsync(Escenario escenario, int primero, int segundo, Task operacion)
    {
        Assert.NotEqual(primero, segundo);
        await using var conexion = new NpgsqlConnection(escenario.Migrador);
        await conexion.OpenAsync();
        var reloj = Stopwatch.StartNew();
        while (reloj.Elapsed < TimeSpan.FromSeconds(15) && !operacion.IsCompleted)
        {
            await using var comando = new NpgsqlCommand("SELECT @primero = ANY(pg_blocking_pids(@segundo))", conexion);
            comando.Parameters.AddWithValue("primero", primero);
            comando.Parameters.AddWithValue("segundo", segundo);
            if ((bool)(await comando.ExecuteScalarAsync())!)
                return;
            await Task.Delay(20);
        }

        Assert.Fail("PostgreSQL no confirmó la espera entre ambas conexiones.");
    }

    /// <summary>
    /// Detiene la primera transacción elegida después de escribir cambio y evento, antes de confirmar.
    /// Puede provocar su reversión y siempre deja una señal para liberar la operación.
    /// </summary>
    private sealed class PausaDeConfirmacion(bool revertir) : DbTransactionInterceptor
    {
        private int _activa;
        public TaskCompletionSource<int> Alcanzada { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Continuar { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        /// Activa la pausa después de preparar la ficha y la sesión.
        public void Activar() => Volatile.Write(ref _activa, 1);

        /// Retiene una sola confirmación y conserva la conexión para demostrar quién bloquea a la otra.
        public override async ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaccion,
            TransactionEventData datos, InterceptionResult resultado, CancellationToken cancelacion = default)
        {
            if (Interlocked.Exchange(ref _activa, 0) == 1)
            {
                Alcanzada.TrySetResult(((NpgsqlConnection)transaccion.Connection!).ProcessID);
                await Continuar.Task.WaitAsync(TimeSpan.FromSeconds(20), cancelacion);
                if (revertir)
                    throw new InvalidOperationException("Reversión ficticia antes de confirmar.");
            }

            return resultado;
        }
    }

    /// <summary>
    /// Señala la conexión de la segunda escritura justo antes de enviarla a PostgreSQL.
    /// La observación posterior de bloqueos demuestra que no basta con haber iniciado la tarea.
    /// </summary>
    private sealed class ObservarSegundaEscritura(string fragmento) : DbCommandInterceptor
    {
        private int _activa;
        public TaskCompletionSource<int> Alcanzada { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        /// Espera la escritura que se iniciará mientras la primera transacción siga abierta.
        public void Activar() => Volatile.Write(ref _activa, 1);

        /// Anota solo el identificador técnico de conexión, sin SQL ni parámetros de datos.
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand comando,
            CommandEventData datos, InterceptionResult<DbDataReader> resultado, CancellationToken cancelacion = default)
        {
            if (comando.CommandText.Contains(fragmento, StringComparison.Ordinal) && Interlocked.Exchange(ref _activa, 0) == 1)
                Alcanzada.TrySetResult(((NpgsqlConnection)comando.Connection!).ProcessID);
            return new(resultado);
        }
    }

    /// <summary>
    /// Pausa una lectura de condición y ámbitos, después de la primera búsqueda de la cita por número.
    /// Permite confirmar la gemela y retirar el ámbito antes de que el paciente se compruebe.
    /// </summary>
    private sealed class PausaAntesDeConsultarPaciente : DbCommandInterceptor
    {
        private int _activa;
        public int Llegadas { get; private set; }
        public TaskCompletionSource Alcanzada { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Continuar { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        /// Activa una sola pausa después del alta del paciente.
        public void Activar() => Volatile.Write(ref _activa, 1);

        /// La consulta todavía no se ejecutó: la otra solicitud puede cambiar los ámbitos antes de leerlos.
        public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand comando,
            CommandEventData datos, InterceptionResult<DbDataReader> resultado, CancellationToken cancelacion = default)
        {
            if (comando.CommandText.Contains("\"PacienteAmbito\"", StringComparison.Ordinal) && Interlocked.Exchange(ref _activa, 0) == 1)
            {
                Llegadas++;
                Alcanzada.TrySetResult();
                await Continuar.Task.WaitAsync(TimeSpan.FromSeconds(20), cancelacion);
            }

            return resultado;
        }
    }
}
