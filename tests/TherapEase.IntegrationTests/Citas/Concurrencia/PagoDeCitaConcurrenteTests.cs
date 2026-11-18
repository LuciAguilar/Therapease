using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TherapEase.Application.Citas.Constantes;
using TherapEase.Application.Citas.Interfaces.Servicios;
using TherapEase.Application.Citas.Modelos;
using TherapEase.Application.Compartido.Modelos;
using TherapEase.Domain.Auditoria.Entidades.Enums;
using TherapEase.Domain.Citas.Entidades;
using TherapEase.Domain.Citas.Entidades.Enums;
using TherapEase.Domain.Compartido.Entidades.Enums;
using TherapEase.Domain.Pacientes.Entidades.Enums;

namespace TherapEase.IntegrationTests;

/// <summary>
/// Fuerza cambios simultáneos entre comprobar y guardar un cambio de pago, con sesiones reales de QA.
/// Las barreras y las pausas fijan el orden sin depender de que dos envíos coincidan por suerte;
/// lo confirmado se relee desde otra conexión.
/// </summary>
public sealed class PagoDeCitaConcurrenteTests(PostgreSqlRevision postgres) : IClassFixture<PostgreSqlRevision>
{
    // La reserva utiliza una fecha y hora locales fijas de Hermosillo.
    private static DatosDeCitaEntrada Datos(Guid paciente) =>
        new(paciente, AmbitoAtencion.Escolar, new DateOnly(2026, 12, 1), new TimeOnly(10, 0), new TimeOnly(11, 0));

    private static async Task<Guid> PrepararCitaAsync(PacientesRevision e)
    {
        var paciente = await e.RegistrarAsync();
        var idCita = Guid.NewGuid();
        var alta = await e.EnSolicitudAsync(s => s.Obtener<IServicioCitas>().AgendarAsync(idCita, Datos(paciente)));
        Assert.True(alta.Exito, alta.Mensaje);
        return idCita;
    }

    private static Task<RespuestaServicio<ResultadoDeCambioDePago>> CambiarAsync(PacientesRevision e, Guid idCita, int version, EstadoPago pago) =>
        e.EnSolicitudAsync(s => s.Obtener<IServicioCitas>().CambiarPagoAsync(idCita, version, pago));

    // ──── ENVÍOS IGUALES AL MISMO TIEMPO ─────────────────────────────────────────────────

    /// Dos o diez envíos con la misma versión llegan juntos al guardado: uno confirma y los demás reciben el conflicto real de la base.
    [Theory]
    [InlineData(2)]
    [InlineData(10)]
    public async Task EnviosIgualesConLaMismaVersion_UnoConfirmaYLosDemasDanConflicto(int cantidad)
    {
        // Arrange
        var barrera = new BarreraDeGuardadoDeCita(cantidad);
        using var e = await PacientesRevision.CrearAsync(postgres, interceptores: [barrera]);
        var idCita = await PrepararCitaAsync(e);

        // Act
        var resultados = await Task.WhenAll(Enumerable.Range(0, cantidad).Select(_ => CambiarAsync(e, idCita, 1, EstadoPago.Pagado)));

        // Assert
        Assert.Equal(cantidad, barrera.Llegadas);
        var confirmada = Assert.Single(resultados, r => r.Exito);
        Assert.Equal(EstadoDeCambioDePago.Actualizado, confirmada.Datos!.Estado);
        Assert.Equal(cantidad - 1, resultados.Count(r => !r.Exito && r.Mensaje == MensajesDeCitas.ConflictoDeVersion && r.Datos is null));

        await using var db = e.Escenario.Contexto();
        var cita = await db.Citas.SingleAsync();
        Assert.Equal((EstadoPago.Pagado, 2), (cita.EstadoPago, cita.Version));
        var evento = await db.EventosAuditoria.SingleAsync(v => v.TipoRegistro == TipoRegistroAuditoria.Cita && v.Accion == AccionAuditoria.Actualizacion);
        Assert.Equal(cita.IdCita, evento.IdRegistro);
        Assert.Equal(e.Actor, evento.IdUsuarioActor);
    }

    // ──── OTRO CAMBIO DE LA MISMA CITA ENTRE COMPROBAR Y GUARDAR ─────────────────────────

    /// Otra operación cambia la fila (reprogramación o baja, preparadas con SQL) y confirma antes de que el pago se guarde:
    /// el pago da conflicto, conserva lo confirmado y no deja evento. La versión cierra la carrera sin un disparador nuevo.
    [Theory]
    [InlineData("reprogramación")]
    [InlineData("baja")]
    public async Task OtroCambioConfirmadoAntesDeGuardar_DaConflictoYConservaLoConfirmado(string otraOperacion)
    {
        // Arrange
        var pausa = new PausaAntesDeGuardarCita();
        using var e = await PacientesRevision.CrearAsync(postgres, interceptores: [pausa]);
        var idCita = await PrepararCitaAsync(e);
        var inicioOriginal = (await LeerCitaAsync(e)).Inicio;
        pausa.Activar();

        // Act
        var enCurso = CambiarAsync(e, idCita, 1, EstadoPago.Pagado);
        try
        {
            await pausa.Alcanzada.Task.WaitAsync(TimeSpan.FromSeconds(20));
            await e.SqlAsync(otraOperacion == "baja"
                    ? """UPDATE "Cita" SET "Condicion"='Baja', "FechaBaja"=now(), "IdUsuarioBaja"=@actor, "Version"="Version"+1 WHERE "IdCita"=@cita"""
                    : """UPDATE "Cita" SET "Inicio"="Inicio"+interval '1 hour', "Fin"="Fin"+interval '1 hour', "Version"="Version"+1 WHERE "IdCita"=@cita""",
                ("actor", e.Actor), ("cita", idCita));
        }
        finally
        {
            pausa.Continuar.TrySetResult();
            await enCurso.WaitAsync(TimeSpan.FromSeconds(20));
        }

        // Assert
        var respuesta = await enCurso;
        Assert.False(respuesta.Exito);
        Assert.Equal(MensajesDeCitas.ConflictoDeVersion, respuesta.Mensaje);
        Assert.Null(respuesta.Datos);
        Assert.Equal(1, pausa.Llegadas);

        var cita = await LeerCitaAsync(e);
        Assert.Equal((EstadoPago.Pendiente, 2), (cita.EstadoPago, cita.Version));
        Assert.Equal(otraOperacion == "baja" ? CondicionRegistro.Baja : CondicionRegistro.Vigente, cita.Condicion);
        Assert.Equal(otraOperacion == "baja" ? inicioOriginal : inicioOriginal.AddHours(1), cita.Inicio);
        await using var db = e.Escenario.Contexto();
        Assert.Empty(await db.EventosAuditoria.Where(v => v.Accion == AccionAuditoria.Actualizacion).ToListAsync());
    }

    private static async Task<Cita> LeerCitaAsync(PacientesRevision e)
    {
        await using var db = e.Escenario.Contexto();
        return await db.Citas.AsNoTracking().SingleAsync();
    }

    // ──── SEÑALES ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Retiene todos los cambios de cita elegidos antes de enviar sus escrituras, hasta que llegue la cantidad prevista.
    /// Demuestra que todos leyeron la misma versión antes de que alguno guardara.
    /// </summary>
    private sealed class BarreraDeGuardadoDeCita(int cantidad) : SaveChangesInterceptor
    {
        private int _llegadas;
        private readonly TaskCompletionSource _todas = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int Llegadas => Volatile.Read(ref _llegadas);

        /// Libera las solicitudes solo cuando todas alcanzaron el guardado de una cita modificada.
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData datos,
            InterceptionResult<int> resultado, CancellationToken cancelacion = default)
        {
            if (datos.Context!.ChangeTracker.Entries<Cita>().Any(c => c.State == EntityState.Modified))
            {
                if (Interlocked.Increment(ref _llegadas) == cantidad)
                {
                    _todas.TrySetResult();
                }

                await _todas.Task.WaitAsync(TimeSpan.FromSeconds(20), cancelacion);
            }

            return resultado;
        }
    }

    /// <summary>
    /// Detiene una única escritura de una cita modificada, después de leerla y comprobarla y antes de enviar sus cambios a PostgreSQL.
    /// </summary>
    private sealed class PausaAntesDeGuardarCita : SaveChangesInterceptor
    {
        private int _activa;
        public int Llegadas { get; private set; }
        public TaskCompletionSource Alcanzada { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Continuar { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        /// Activa la pausa después de preparar paciente, cita y sesión.
        public void Activar() => Volatile.Write(ref _activa, 1);

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData datos,
            InterceptionResult<int> resultado, CancellationToken cancelacion = default)
        {
            if (datos.Context!.ChangeTracker.Entries<Cita>().Any(c => c.State == EntityState.Modified)
                && Interlocked.Exchange(ref _activa, 0) == 1)
            {
                Llegadas++;
                Alcanzada.TrySetResult();
                await Continuar.Task.WaitAsync(TimeSpan.FromSeconds(20), cancelacion);
            }

            return resultado;
        }
    }
}
