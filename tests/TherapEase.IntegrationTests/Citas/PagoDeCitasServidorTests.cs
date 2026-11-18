using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TherapEase.Application.Citas.Constantes;
using TherapEase.Application.Citas.Interfaces.Servicios;
using TherapEase.Application.Citas.Modelos;
using TherapEase.Application.Compartido.Modelos;
using TherapEase.Domain.Auditoria.Entidades;
using TherapEase.Domain.Auditoria.Entidades.Enums;
using TherapEase.Domain.Citas.Entidades;
using TherapEase.Domain.Citas.Entidades.Enums;
using TherapEase.Domain.Citas.Reglas;
using TherapEase.Domain.Compartido.Entidades.Enums;
using TherapEase.Domain.Compartido.Excepciones;
using TherapEase.Domain.Pacientes.Entidades.Enums;

namespace TherapEase.IntegrationTests;

/// <summary>
/// Cambiar el estado de pago de una cita con PostgreSQL real: lo guardado se relee desde otra conexión
/// y el evento se compara con la cita. Las citas canceladas y de baja se preparan con SQL directo,
/// sin crear operaciones de cancelación ni de baja.
/// </summary>
public sealed class PagoDeCitasServidorTests(PostgreSqlRevision postgres) : IClassFixture<PostgreSqlRevision>
{
    private const string HechoEsperado = "Se cambió el estado de pago de una cita.";

    // La reserva utiliza una fecha y hora locales fijas de Hermosillo.
    private static DatosDeCitaEntrada Datos(Guid paciente) =>
        new(paciente, AmbitoAtencion.Escolar, new DateOnly(2026, 12, 1), new TimeOnly(10, 0), new TimeOnly(11, 0));

    /// Registra un paciente ficticio y agenda su cita con los servicios reales: nace Pendiente, versión 1.
    private static async Task<(Guid Paciente, Guid Cita)> PrepararAsync(PacientesRevision e)
    {
        var paciente = await e.RegistrarAsync();
        var idCita = Guid.NewGuid();
        var alta = await e.EnSolicitudAsync(s => s.Obtener<IServicioCitas>().AgendarAsync(idCita, Datos(paciente)));
        Assert.True(alta.Exito, alta.Mensaje);
        return (paciente, idCita);
    }

    private static Task<RespuestaServicio<ResultadoDeCambioDePago>> CambiarAsync(PacientesRevision e, Guid idCita, int version, EstadoPago? pago) =>
        e.EnSolicitudAsync(s => s.Obtener<IServicioCitas>().CambiarPagoAsync(idCita, version, pago));

    private static async Task<Cita> CitaGuardadaAsync(PacientesRevision e, Guid idCita)
    {
        await using var contexto = e.Escenario.Contexto();
        return await contexto.Citas.AsNoTracking().SingleAsync(c => c.IdCita == idCita);
    }

    private static async Task<List<EventoAuditoria>> EventosDeCitaAsync(PacientesRevision e, AccionAuditoria? accion = null)
    {
        await using var contexto = e.Escenario.Contexto();
        return await contexto.EventosAuditoria.AsNoTracking()
            .Where(v => v.TipoRegistro == TipoRegistroAuditoria.Cita && (accion == null || v.Accion == accion))
            .OrderBy(v => v.IdEvento)
            .ToListAsync();
    }

    /// Resume la cita y sus eventos en un texto: si algo cambia, el texto también.
    private static async Task<string> HuellaAsync(PacientesRevision e) =>
        await PostgreSqlRevision.Valor<string>(e.Escenario.App, """SELECT COALESCE(json_agg(c ORDER BY c."IdCita")::text, '[]') FROM "Cita" c""")
        + await PostgreSqlRevision.Valor<string>(e.Escenario.App, """SELECT COALESCE(json_agg(v ORDER BY v."IdEvento")::text, '[]') FROM "EventoAuditoria" v""");

    private static Task PrepararBajaAsync(PacientesRevision e, Guid idCita) =>
        e.SqlAsync("""UPDATE "Cita" SET "Condicion"='Baja', "FechaBaja"=now(), "IdUsuarioBaja"=@actor, "Version"="Version"+1 WHERE "IdCita"=@cita""",
            ("actor", e.Actor), ("cita", idCita));

    private static Task PrepararCancelacionAsync(PacientesRevision e, Guid idCita) =>
        e.SqlAsync("""UPDATE "Cita" SET "Estado"='Cancelada', "Version"="Version"+1 WHERE "IdCita"=@cita""", ("cita", idCita));

    // ──── CAMBIO CONFIRMADO ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task CambiarPago_DePendienteAPagado_GuardaPagoVersionYMetadatosConUnEventoSinValores()
    {
        // Arrange
        using var e = await PacientesRevision.CrearAsync(postgres);
        var (_, idCita) = await PrepararAsync(e);
        var antes = await CitaGuardadaAsync(e, idCita);
        e.Comandos.Limpiar();

        // Act
        var respuesta = await CambiarAsync(e, idCita, 1, EstadoPago.Pagado);

        // Assert
        Assert.True(respuesta.Exito, respuesta.Mensaje);
        Assert.Equal(MensajesDeCitas.PagoActualizado, respuesta.Mensaje);
        Assert.Equal(EstadoDeCambioDePago.Actualizado, respuesta.Datos!.Estado);
        Assert.Equal(new EstadoDePagoDeCita(idCita, EstadoPago.Pagado, EstadoCita.Agendada, 2), respuesta.Datos.Pago);

        var despues = await CitaGuardadaAsync(e, idCita);
        Assert.Equal(EstadoPago.Pagado, despues.EstadoPago);
        Assert.Equal(2, despues.Version);
        Assert.Equal(e.Actor, despues.IdUsuarioActualizacion);
        Assert.NotNull(despues.FechaActualizacion);
        Assert.Equal((antes.IdPaciente, antes.Ambito, antes.Inicio, antes.Fin, antes.Estado, antes.Condicion),
            (despues.IdPaciente, despues.Ambito, despues.Inicio, despues.Fin, despues.Estado, despues.Condicion));
        Assert.Equal((antes.FechaAlta, antes.IdUsuarioAlta), (despues.FechaAlta, despues.IdUsuarioAlta));

        var evento = Assert.Single(await EventosDeCitaAsync(e, AccionAuditoria.Actualizacion));
        Assert.Equal(idCita, evento.IdRegistro);
        Assert.Equal(e.Actor, evento.IdUsuarioActor);
        Assert.Equal(["EstadoPago"], evento.CamposAfectados);
        Assert.Equal(HechoEsperado, evento.Hecho);
        Assert.Single(await EventosDeCitaAsync(e, AccionAuditoria.Alta));

        // El evento no lleva el valor nuevo, el anterior, el paciente ni el horario.
        var texto = JsonSerializer.Serialize(evento);
        Assert.DoesNotContain("Pagado", texto, StringComparison.Ordinal);
        Assert.DoesNotContain("Pendiente", texto, StringComparison.Ordinal);
        Assert.DoesNotContain("Ejemplo", texto, StringComparison.Ordinal);

        // Una sola lectura de la cita y ninguna consulta a Pacientes.
        Assert.Single(e.Comandos.Textos, t => t.Contains("FROM \"Cita\"", StringComparison.Ordinal));
        Assert.DoesNotContain(e.Comandos.Textos, t => t.Contains("\"Paciente\"", StringComparison.Ordinal));
    }

    [Fact]
    public async Task CambiarPago_TrasRecargar_ElDetalleMuestraElPagoYLaVersionNuevos()
    {
        // Arrange
        using var e = await PacientesRevision.CrearAsync(postgres);
        var (_, idCita) = await PrepararAsync(e);
        var inicial = await e.EnSolicitudAsync(s => s.Obtener<IServicioCitas>().ObtenerCitaAsync(idCita));
        Assert.Equal((EstadoPago.Pendiente, 1), (inicial.Datos!.EstadoPago, inicial.Datos.Version));

        // Act
        var cambio = await CambiarAsync(e, idCita, inicial.Datos.Version, EstadoPago.Pagado);
        var recargado = await e.EnSolicitudAsync(s => s.Obtener<IServicioCitas>().ObtenerCitaAsync(idCita));

        // Assert
        Assert.True(cambio.Exito, cambio.Mensaje);
        Assert.True(recargado.Exito, recargado.Mensaje);
        Assert.Equal((EstadoPago.Pagado, 2), (recargado.Datos!.EstadoPago, recargado.Datos.Version));
    }

    [Fact]
    public async Task CambiarPago_DePagadoAPendiente_UsaElMismoHechoYSuma_UnEventoPorCambio()
    {
        // Arrange
        using var e = await PacientesRevision.CrearAsync(postgres);
        var (_, idCita) = await PrepararAsync(e);
        Assert.True((await CambiarAsync(e, idCita, 1, EstadoPago.Pagado)).Exito);

        // Act
        var respuesta = await CambiarAsync(e, idCita, 2, EstadoPago.Pendiente);

        // Assert
        Assert.True(respuesta.Exito, respuesta.Mensaje);
        var cita = await CitaGuardadaAsync(e, idCita);
        Assert.Equal((EstadoPago.Pendiente, 3), (cita.EstadoPago, cita.Version));
        var eventos = await EventosDeCitaAsync(e, AccionAuditoria.Actualizacion);
        Assert.Equal(2, eventos.Count);
        Assert.All(eventos, v =>
        {
            Assert.Equal(HechoEsperado, v.Hecho);
            Assert.Equal(["EstadoPago"], v.CamposAfectados);
        });
    }

    [Fact]
    public async Task CambiarPago_EnUnaCitaCancelada_SeCorrigeYSigueCancelada()
    {
        // Arrange
        using var e = await PacientesRevision.CrearAsync(postgres);
        var (_, idCita) = await PrepararAsync(e);
        await PrepararCancelacionAsync(e, idCita);

        // Act
        var respuesta = await CambiarAsync(e, idCita, 2, EstadoPago.Pagado);

        // Assert
        Assert.True(respuesta.Exito, respuesta.Mensaje);
        Assert.Equal(EstadoCita.Cancelada, respuesta.Datos!.Pago.Estado);
        var cita = await CitaGuardadaAsync(e, idCita);
        Assert.Equal((EstadoCita.Cancelada, EstadoPago.Pagado, CondicionRegistro.Vigente, 3), (cita.Estado, cita.EstadoPago, cita.Condicion, cita.Version));
        Assert.Single(await EventosDeCitaAsync(e, AccionAuditoria.Actualizacion));
    }

    // ──── SIN CAMBIOS Y VERSIÓN ──────────────────────────────────────────────────────────

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CambiarPago_ConLaVersionVigenteYElMismoPago_NoEscribeNiCambiaLaVersionNiRegistraEvento(bool pagada)
    {
        // Arrange
        using var e = await PacientesRevision.CrearAsync(postgres);
        var (_, idCita) = await PrepararAsync(e);
        var version = 1;
        if (pagada)
        {
            Assert.True((await CambiarAsync(e, idCita, 1, EstadoPago.Pagado)).Exito);
            version = 2;
        }

        var huella = await HuellaAsync(e);
        e.Comandos.Limpiar();

        // Act
        var respuesta = await CambiarAsync(e, idCita, version, pagada ? EstadoPago.Pagado : EstadoPago.Pendiente);

        // Assert
        Assert.True(respuesta.Exito, respuesta.Mensaje);
        Assert.Equal(MensajesDeCitas.SinCambios, respuesta.Mensaje);
        Assert.Equal(EstadoDeCambioDePago.SinCambios, respuesta.Datos!.Estado);
        Assert.Equal(huella, await HuellaAsync(e));
        Assert.DoesNotContain(e.Comandos.Textos, t => t.Contains("UPDATE ", StringComparison.Ordinal) || t.Contains("INSERT ", StringComparison.Ordinal));
        Assert.Single(e.Comandos.Textos, t => t.Contains("FROM \"Cita\"", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(EstadoPago.Pendiente)]
    [InlineData(EstadoPago.Pagado)]
    public async Task CambiarPago_ConUnaVersionAnterior_DaConflictoAunqueElPagoCoincida(EstadoPago pedido)
    {
        // Arrange
        using var e = await PacientesRevision.CrearAsync(postgres);
        var (_, idCita) = await PrepararAsync(e);
        Assert.True((await CambiarAsync(e, idCita, 1, EstadoPago.Pagado)).Exito);
        var huella = await HuellaAsync(e);

        // Act
        var respuesta = await CambiarAsync(e, idCita, 1, pedido);

        // Assert
        Assert.False(respuesta.Exito);
        Assert.Equal(MensajesDeCitas.ConflictoDeVersion, respuesta.Mensaje);
        Assert.Null(respuesta.Datos);
        Assert.Equal(huella, await HuellaAsync(e));
    }

    // ──── CITA DE BAJA ───────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(EstadoPago.Pendiente)]
    [InlineData(EstadoPago.Pagado)]
    public async Task CambiarPago_EnUnaCitaDeBaja_SeRechazaAunqueElValorSeaElMismoYNoTocaLaFila(EstadoPago pedido)
    {
        // Arrange
        using var e = await PacientesRevision.CrearAsync(postgres);
        var (_, idCita) = await PrepararAsync(e);
        await PrepararBajaAsync(e, idCita);
        var huella = await HuellaAsync(e);

        // Act
        var respuesta = await CambiarAsync(e, idCita, 2, pedido);

        // Assert
        Assert.False(respuesta.Exito);
        Assert.Equal(MensajesDeCitas.PagoDeCitaDeBajaNoSeActualiza, respuesta.Mensaje);
        Assert.Null(respuesta.Datos);
        Assert.Equal(huella, await HuellaAsync(e));
    }

    // La consulta del pago de una cita de baja se aplazó a B06: el detalle B03 la sigue tratando como inexistente.
    [Fact]
    public async Task ObtenerCita_DeUnaCitaDeBaja_SigueDiciendoQueNoSeEncontro()
    {
        // Arrange
        using var e = await PacientesRevision.CrearAsync(postgres);
        var (_, idCita) = await PrepararAsync(e);
        await PrepararBajaAsync(e, idCita);

        // Act
        var respuesta = await e.EnSolicitudAsync(s => s.Obtener<IServicioCitas>().ObtenerCitaAsync(idCita));

        // Assert
        Assert.False(respuesta.Exito);
        Assert.Equal(MensajesDeCitas.CitaNoEncontrada, respuesta.Mensaje);
        Assert.Null(respuesta.Datos);
    }

    // Cancelar, dar de baja y recuperar con las reglas de Domain conservan el pago guardado (HU13 CA3).
    [Fact]
    public async Task CancelarDarDeBajaYRecuperar_ConservanElPagoGuardadoEnLaBase()
    {
        // Arrange
        using var e = await PacientesRevision.CrearAsync(postgres);
        var (_, idCita) = await PrepararAsync(e);
        Assert.True((await CambiarAsync(e, idCita, 1, EstadoPago.Pagado)).Exito);
        var ahora = DateTimeOffset.UtcNow;

        // Act + Assert
        await AplicarReglaAsync(e, idCita, (cita, version) => ReglasDeCita.Cancelar(cita, version, e.Actor, ahora));
        var cancelada = await CitaGuardadaAsync(e, idCita);
        Assert.Equal((EstadoCita.Cancelada, CondicionRegistro.Vigente, EstadoPago.Pagado), (cancelada.Estado, cancelada.Condicion, cancelada.EstadoPago));

        await AplicarReglaAsync(e, idCita, (cita, version) => ReglasDeCita.DarDeBaja(cita, version, e.Actor, ahora));
        var deBaja = await CitaGuardadaAsync(e, idCita);
        Assert.Equal((CondicionRegistro.Baja, EstadoPago.Pagado), (deBaja.Condicion, deBaja.EstadoPago));

        await AplicarReglaAsync(e, idCita, (cita, version) => ReglasDeCita.Recuperar(cita, version, e.Actor, ahora));
        var recuperada = await CitaGuardadaAsync(e, idCita);
        Assert.Equal((CondicionRegistro.Vigente, EstadoPago.Pagado), (recuperada.Condicion, recuperada.EstadoPago));
    }

    private static async Task AplicarReglaAsync(PacientesRevision e, Guid idCita, Action<Cita, int> regla)
    {
        await using var contexto = e.Escenario.Contexto();
        var cita = await contexto.Citas.SingleAsync(c => c.IdCita == idCita);
        regla(cita, cita.Version);
        await contexto.SaveChangesAsync();
    }

    // ──── PERMISOS Y ENTRADAS ────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("desactivado")]
    [InlineData("contraseña temporal pendiente")]
    public async Task CambiarPago_SinPermisoVigente_RechazaAntesDeLeerLaCita(string situacion)
    {
        // Arrange
        using var e = await PacientesRevision.CrearAsync(postgres);
        var (_, idCita) = await PrepararAsync(e);
        await e.SqlAsync(situacion == "desactivado"
                ? """UPDATE "AspNetUsers" SET "Activo"=false WHERE "Id"=@actor"""
                : """UPDATE "AspNetUsers" SET "DebeCambiarContrasena"=true WHERE "Id"=@actor""",
            ("actor", e.Actor));
        var huella = await HuellaAsync(e);
        e.Comandos.Limpiar();

        // Act
        var respuesta = await e.EnSolicitudAsync(
            s => s.Obtener<IServicioCitas>().CambiarPagoAsync(idCita, 1, EstadoPago.Pagado), exigirSesion: false);

        // Assert
        Assert.False(respuesta.Exito);
        Assert.Equal(MensajesDeCitas.SinPermiso, respuesta.Mensaje);
        Assert.DoesNotContain(e.Comandos.Textos, t => t.Contains("\"Cita\"", StringComparison.Ordinal));
        Assert.Equal(huella, await HuellaAsync(e));
    }

    [Fact]
    public async Task CambiarPago_ConUnaCitaInexistente_DiceQueNoSeEncontroYNoEscribe()
    {
        // Arrange
        using var e = await PacientesRevision.CrearAsync(postgres);
        await PrepararAsync(e);
        var huella = await HuellaAsync(e);

        // Act
        var respuesta = await CambiarAsync(e, Guid.NewGuid(), 1, EstadoPago.Pagado);

        // Assert
        Assert.Equal(MensajesDeCitas.CitaNoEncontrada, respuesta.Mensaje);
        Assert.Equal(huella, await HuellaAsync(e));
    }

    [Theory]
    [InlineData(null, 1)]
    [InlineData((EstadoPago)2, 1)]
    [InlineData(EstadoPago.Pagado, 0)]
    [InlineData(EstadoPago.Pagado, -3)]
    public async Task CambiarPago_ConEntradaInvalida_SeRechazaSinLeerNiEscribir(EstadoPago? pago, int version)
    {
        // Arrange
        using var e = await PacientesRevision.CrearAsync(postgres);
        var (_, idCita) = await PrepararAsync(e);
        var huella = await HuellaAsync(e);
        e.Comandos.Limpiar();

        // Act
        var respuesta = await CambiarAsync(e, idCita, version, pago);

        // Assert
        Assert.Equal(MensajesDeCitas.SolicitudInvalida, respuesta.Mensaje);
        Assert.DoesNotContain(e.Comandos.Textos, t => t.Contains("\"Cita\"", StringComparison.Ordinal));
        Assert.Equal(huella, await HuellaAsync(e));
    }

    // ──── AUDITORÍA FALLIDA ──────────────────────────────────────────────────────────────

    /// Si el evento no se puede guardar, la cita vuelve a su estado: error general, ni éxito ni conflicto.
    [Fact]
    public async Task CambiarPago_SiLaAuditoriaFalla_SubeElErrorYDejaLaCitaYLosEventosIntactos()
    {
        // Arrange
        using var e = await PacientesRevision.CrearAsync(postgres);
        var (_, idCita) = await PrepararAsync(e);
        await ServidorIdentidadRevision.SimularFalloDeAuditoriaAsync(e.Escenario);
        var huella = await HuellaAsync(e);

        // Act
        RespuestaServicio<ResultadoDeCambioDePago>? respuesta = null;
        var error = await Record.ExceptionAsync(async () => respuesta = await CambiarAsync(e, idCita, 1, EstadoPago.Pagado));

        // Assert
        Assert.NotNull(error);
        Assert.Null(respuesta);
        Assert.False(error is ReglaDeNegocioException);
        Assert.Equal(huella, await HuellaAsync(e));
    }

    /// Un 40P01 provocado solo en el evento no se confunde con un conflicto ni se reintenta con otra lectura.
    [Fact]
    public async Task CambiarPago_SiLaAuditoriaFallaConInterbloqueo_SubeComoErrorGeneralSinRelectura()
    {
        // Arrange
        using var e = await PacientesRevision.CrearAsync(postgres);
        var (_, idCita) = await PrepararAsync(e);
        await e.SqlAsync("""
            CREATE FUNCTION fallo_evento_revision() RETURNS trigger LANGUAGE plpgsql AS $$
            BEGIN
                RAISE EXCEPTION USING ERRCODE = '40P01', MESSAGE = 'Fallo ficticio de auditoria';
            END $$;
            CREATE TRIGGER fallo_evento_revision BEFORE INSERT ON "EventoAuditoria"
                FOR EACH ROW EXECUTE FUNCTION fallo_evento_revision();
            """);
        var huella = await HuellaAsync(e);
        e.Comandos.Limpiar();

        // Act
        RespuestaServicio<ResultadoDeCambioDePago>? respuesta = null;
        var error = await Record.ExceptionAsync(async () => respuesta = await CambiarAsync(e, idCita, 1, EstadoPago.Pagado));

        // Assert
        Assert.NotNull(error);
        Assert.Null(respuesta);
        Assert.False(error is ReglaDeNegocioException);
        var tecnico = error;
        while (tecnico is not null and not PostgresException)
        {
            tecnico = tecnico.InnerException;
        }

        Assert.True(tecnico is PostgresException { SqlState: "40P01" }, "Debe propagarse el fallo preparado de auditoría.");
        Assert.Single(e.Comandos.Textos, t => t.Contains("FROM \"Cita\"", StringComparison.Ordinal));
        Assert.Equal(huella, await HuellaAsync(e));
    }

    // ──── SIN SERIALIZARSE CON LA AGENDA ─────────────────────────────────────────────────

    /// Cambiar el pago no cambia horario, estado ni condición: no toma el bloqueo de la agenda (clave fija 7301 de la migración),
    /// así que termina aunque otra conexión lo tenga retenido.
    [Fact]
    public async Task CambiarPago_NoEsperaAlBloqueoDeAgendaRetenidoPorOtraConexion()
    {
        // Arrange
        using var e = await PacientesRevision.CrearAsync(postgres);
        var (_, idCita) = await PrepararAsync(e);
        await using var otraConexion = new NpgsqlConnection(e.Escenario.Migrador);
        await otraConexion.OpenAsync();
        await using (var tomar = new NpgsqlCommand("SELECT pg_advisory_lock(7301)", otraConexion))
        {
            await tomar.ExecuteNonQueryAsync();
        }

        try
        {
            // Act
            var respuesta = await CambiarAsync(e, idCita, 1, EstadoPago.Pagado).WaitAsync(TimeSpan.FromSeconds(20));

            // Assert
            Assert.True(respuesta.Exito, respuesta.Mensaje);
            Assert.Equal(EstadoPago.Pagado, (await CitaGuardadaAsync(e, idCita)).EstadoPago);
        }
        finally
        {
            await using var soltar = new NpgsqlCommand("SELECT pg_advisory_unlock(7301)", otraConexion);
            await soltar.ExecuteNonQueryAsync();
        }
    }
}
