using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using TherapEase.Domain.Pacientes.Entidades.Enums;
using TherapEase.Domain.Pacientes.Reglas;
using TherapEase.Infrastructure.Data;

namespace TherapEase.IntegrationTests;

/// <summary>
/// Comprueba con PostgreSQL real el disparador de Cita que toma el bloqueo de asesoría de transacción (clave 7301) antes de escribir una cita activa:
/// cuándo se toma y cuándo no, que se tome antes del FOR SHARE del paciente, que serialice las altas sin interbloqueos (la segunda recibe el cruce real),
/// que una transacción revertida lo libere y que la migración suba, baje y vuelva a subir sin tocar la restricción de cruces ni los controles del paciente.
/// Todos los datos son ficticios.
/// </summary>
public class BloqueoDeAgendaEnCitaActivaTests(PostgreSqlRevision postgres) : IClassFixture<PostgreSqlRevision>
{
    private const long ClaveDeBloqueo = 7301;
    private const string MigracionAnterior = "20261028032404_DatosDePacienteYTutor";
    private const string MigracionNueva = "20261110025757_BloqueoDeAgendaEnCitaActiva";
    private const string ActorTexto = "10000000-0000-4000-8000-000000000001";

    private static readonly Guid Actor = Guid.Parse(ActorTexto);
    private static readonly DateTimeOffset Ahora = DateTimeOffset.Parse("2026-10-03T12:00:00Z");

    // ──── PREPARACIÓN ────────────────────────────────────────────────────────────────────────

    private static async Task<Guid> PacienteAsync(Escenario e)
    {
        await using var contexto = e.Contexto();
        var paciente = ReglasDePaciente.Registrar(Guid.NewGuid(), PacientesDePrueba.Adulto("Paciente Ficticio"), [AmbitoAtencion.Independiente], Actor, Ahora);
        contexto.Pacientes.Add(paciente);
        await contexto.SaveChangesAsync();
        return paciente.IdPaciente;
    }

    // INSERT directo de una cita, sin pasar por la aplicación. De una baja se llenan los dos campos que exige la restricción de coherencia.
    private static string InsertarCita(Guid id, Guid paciente, string inicio, string fin, string estado = "Agendada", string condicion = "Vigente")
    {
        var fechaBaja = condicion == "Baja" ? "'2026-10-03T12:00:00Z'" : "NULL";
        var usuarioBaja = condicion == "Baja" ? $"'{ActorTexto}'" : "NULL";
        return $"""
            INSERT INTO "Cita" ("IdCita","IdPaciente","Ambito","Inicio","Fin","Estado","EstadoPago","Condicion","Version","FechaAlta","IdUsuarioAlta","FechaBaja","IdUsuarioBaja")
            VALUES ('{id}','{paciente}','Independiente','{inicio}','{fin}','{estado}','Pendiente','{condicion}',1,'2026-10-03T12:00:00Z','{ActorTexto}',{fechaBaja},{usuarioBaja})
            """;
    }

    private static async Task<int> EjecutarAsync(NpgsqlConnection conexion, NpgsqlTransaction? transaccion, string sql)
    {
        await using var comando = new NpgsqlCommand(sql, conexion, transaccion) { CommandTimeout = 30 };
        return await comando.ExecuteNonQueryAsync();
    }

    private static async Task<long> BloqueosDeAsesoriaDeEstaSesionAsync(NpgsqlConnection conexion, NpgsqlTransaction? transaccion)
    {
        await using var comando = new NpgsqlCommand(
            $"SELECT count(*) FROM pg_locks WHERE locktype='advisory' AND granted AND pid=pg_backend_pid() AND classid=0 AND objid={ClaveDeBloqueo} AND objsubid=1",
            conexion, transaccion);
        return (long)(await comando.ExecuteScalarAsync())!;
    }

    private static Task<long> BloqueosConcedidosAAsync(Escenario e, int pid) =>
        PostgreSqlRevision.Valor<long>(e.App,
            $"SELECT count(*) FROM pg_locks WHERE locktype='advisory' AND granted AND pid={pid} AND classid=0 AND objid={ClaveDeBloqueo} AND objsubid=1");

    // Espera hasta ver en PostgreSQL que la sesión está detenida esperando un bloqueo. Si no espera, las operaciones no fueron simultáneas y la prueba no demuestra nada.
    private static async Task EsperarQueEsteBloqueadaAsync(Escenario e, int pid, Task operacion)
    {
        var espera = Stopwatch.StartNew();
        while (espera.Elapsed < TimeSpan.FromSeconds(10) && !operacion.IsCompleted)
        {
            if (await PostgreSqlRevision.Valor<bool>(e.App, $"SELECT EXISTS(SELECT 1 FROM pg_stat_activity WHERE pid={pid} AND wait_event_type='Lock')"))
            {
                return;
            }

            await Task.Delay(20);
        }

        Assert.Fail("La operación no quedó esperando un bloqueo: no se demostró la simultaneidad.");
    }

    private static async Task MigrarAsync(Escenario e, string destino)
    {
        await using var contexto = new ContextoDeDatos(new DbContextOptionsBuilder<ContextoDeDatos>().UseNpgsql(e.Migrador).Options);
        await contexto.GetService<IMigrator>().MigrateAsync(destino);
    }

    // ──── CUÁNDO SE TOMA EL BLOQUEO ──────────────────────────────────────────────────────────

    [Fact]
    public async Task UnaAltaActiva_TomaElBloqueoHastaConfirmar()
    {
        var e = await postgres.Crear();
        var paciente = await PacienteAsync(e);
        await using var conexion = new NpgsqlConnection(e.App);
        await conexion.OpenAsync();
        await using var transaccion = await conexion.BeginTransactionAsync();
        Assert.Equal(0, await BloqueosDeAsesoriaDeEstaSesionAsync(conexion, transaccion));

        await EjecutarAsync(conexion, transaccion, InsertarCita(Guid.NewGuid(), paciente, "2026-10-04T10:00:00Z", "2026-10-04T11:00:00Z"));

        Assert.Equal(1, await BloqueosDeAsesoriaDeEstaSesionAsync(conexion, transaccion));
        await transaccion.CommitAsync();
        Assert.Equal(0, await BloqueosDeAsesoriaDeEstaSesionAsync(conexion, null));
    }

    [Theory]
    [InlineData("Cancelada", "Vigente")]
    [InlineData("Agendada", "Baja")]
    [InlineData("Cancelada", "Baja")]
    public async Task UnaAltaQueNoQuedaAgendadaYVigente_NoTomaElBloqueo(string estado, string condicion)
    {
        var e = await postgres.Crear();
        var paciente = await PacienteAsync(e);
        await using var conexion = new NpgsqlConnection(e.App);
        await conexion.OpenAsync();
        await using var transaccion = await conexion.BeginTransactionAsync();

        var filas = await EjecutarAsync(conexion, transaccion,
            InsertarCita(Guid.NewGuid(), paciente, "2026-10-04T10:00:00Z", "2026-10-04T11:00:00Z", estado, condicion));

        Assert.Equal(1, filas);
        Assert.Equal(0, await BloqueosDeAsesoriaDeEstaSesionAsync(conexion, transaccion));
    }

    // Cada fila: estado y condición iniciales de la cita, el cambio y si ese cambio debe tomar el bloqueo.
    [Theory]
    [InlineData("Agendada", "Vigente", "\"Inicio\" = \"Inicio\" + interval '3 hours', \"Fin\" = \"Fin\" + interval '3 hours'", true)]
    [InlineData("Agendada", "Vigente", "\"Fin\" = \"Fin\" + interval '30 minutes'", true)]
    [InlineData("Cancelada", "Vigente", "\"Estado\" = 'Agendada'", true)]
    [InlineData("Agendada", "Baja", "\"Condicion\" = 'Vigente', \"FechaBaja\" = NULL, \"IdUsuarioBaja\" = NULL", true)]
    [InlineData("Agendada", "Vigente", "\"EstadoPago\" = 'Pagado', \"Version\" = 2", false)]
    [InlineData("Agendada", "Vigente", "\"Inicio\" = \"Inicio\"", false)]
    [InlineData("Agendada", "Vigente", "\"Estado\" = 'Cancelada'", false)]
    [InlineData("Agendada", "Vigente", $"\"Condicion\" = 'Baja', \"FechaBaja\" = '2026-10-03T12:00:00Z', \"IdUsuarioBaja\" = '{ActorTexto}'", false)]
    [InlineData("Cancelada", "Vigente", "\"EstadoPago\" = 'Pagado'", false)]
    public async Task UnCambioDeCita_TomaElBloqueoSoloSiQuedaActivaYCambiaInicioFinEstadoOCondicion(
        string estadoInicial, string condicionInicial, string cambio, bool tomaElBloqueo)
    {
        var e = await postgres.Crear();
        var paciente = await PacienteAsync(e);
        var id = Guid.NewGuid();
        await PostgreSqlRevision.Sql(e.App,
            InsertarCita(id, paciente, "2026-10-04T10:00:00Z", "2026-10-04T11:00:00Z", estadoInicial, condicionInicial));
        await using var conexion = new NpgsqlConnection(e.App);
        await conexion.OpenAsync();
        await using var transaccion = await conexion.BeginTransactionAsync();

        var filas = await EjecutarAsync(conexion, transaccion, $"UPDATE \"Cita\" SET {cambio} WHERE \"IdCita\" = '{id}'");

        Assert.Equal(1, filas);
        Assert.Equal(tomaElBloqueo ? 1 : 0, await BloqueosDeAsesoriaDeEstaSesionAsync(conexion, transaccion));
    }

    // ──── ORDEN CON EL CONTROL DEL PACIENTE ──────────────────────────────────────────────────

    [Fact]
    public async Task ElBloqueoSeTomaAntesDelBloqueoCompartidoDelPaciente()
    {
        var e = await postgres.Crear();
        var paciente = await PacienteAsync(e);

        // Una transacción retiene la fila del paciente: el alta se detiene en el FOR SHARE del control del paciente.
        await using var retenedora = new NpgsqlConnection(e.App);
        await retenedora.OpenAsync();
        await using var retencion = await retenedora.BeginTransactionAsync();
        await EjecutarAsync(retenedora, retencion, $"SELECT 1 FROM \"Paciente\" WHERE \"IdPaciente\" = '{paciente}' FOR UPDATE");

        await using var altaConexion = new NpgsqlConnection(e.App);
        await altaConexion.OpenAsync();
        await using var altaTransaccion = await altaConexion.BeginTransactionAsync();
        var alta = EjecutarAsync(altaConexion, altaTransaccion,
            InsertarCita(Guid.NewGuid(), paciente, "2026-10-04T10:00:00Z", "2026-10-04T11:00:00Z"));
        try
        {
            await EsperarQueEsteBloqueadaAsync(e, altaConexion.ProcessID, alta);

            // Detenida esperando al paciente y con el bloqueo de asesoría ya concedido: se tomó antes del FOR SHARE.
            Assert.Equal(1, await BloqueosConcedidosAAsync(e, altaConexion.ProcessID));
        }
        finally
        {
            await retencion.RollbackAsync();
        }

        Assert.Equal(1, await alta.WaitAsync(TimeSpan.FromSeconds(15)));
        await altaTransaccion.CommitAsync();
    }

    [Fact]
    public async Task LosDisparadoresDeAntesSeEjecutanEnEsteOrden_ElBloqueoPrimeroYElControlDelPacienteDespues()
    {
        var e = await postgres.Crear();

        var orden = await PostgreSqlRevision.Valor<string>(e.Migrador, """
            SELECT string_agg(tgname, ',' ORDER BY tgname COLLATE "C") FROM pg_trigger
            WHERE tgrelid = '"Cita"'::regclass AND NOT tgisinternal AND (tgtype & 2) = 2
            """);

        Assert.Equal("TR_Cita_00SerializaAltaActiva,TR_Cita_00SerializaCambioActivo,TR_Cita_ActivaExigePacienteVigente", orden);
    }

    // ──── SERIALIZACIÓN, CRUCES REALES Y REVERSIÓN ───────────────────────────────────────────

    // Con el mismo horario la segunda alta espera a la primera y la restricción la rechaza con el cruce real (23P01), nunca con un interbloqueo (40P01).
    [Fact]
    public async Task DosAltasDelMismoHorario_LaSegundaEsperaYRecibeElCruceRealNoUnInterbloqueo()
    {
        var e = await postgres.Crear();
        var paciente = await PacienteAsync(e);

        var resultados = await ConcurrenciaSqlRevision.EjecutarAsync(e,
            InsertarCita(Guid.NewGuid(), paciente, "2026-10-04T10:00:00Z", "2026-10-04T11:00:00Z"),
            InsertarCita(Guid.NewGuid(), paciente, "2026-10-04T10:00:00Z", "2026-10-04T11:00:00Z"));

        Assert.Equal(["confirmada", "23P01"], resultados);
        Assert.Equal(1L, await PostgreSqlRevision.Valor<long>(e.Migrador, "SELECT count(*) FROM \"Cita\""));
    }

    // Aunque los horarios no se crucen, las altas activas se serializan: la segunda espera a la primera (a eso se debe el bloqueo, no al cruce).
    [Fact]
    public async Task DosAltasDeHorariosDistintos_SeSerializanYAmbasSeConfirman()
    {
        var e = await postgres.Crear();
        var paciente = await PacienteAsync(e);

        var resultados = await ConcurrenciaSqlRevision.EjecutarAsync(e,
            InsertarCita(Guid.NewGuid(), paciente, "2026-10-04T10:00:00Z", "2026-10-04T11:00:00Z"),
            InsertarCita(Guid.NewGuid(), paciente, "2026-10-04T12:00:00Z", "2026-10-04T13:00:00Z"));

        Assert.Equal(["confirmada", "confirmada"], resultados);
        Assert.Equal(2L, await PostgreSqlRevision.Valor<long>(e.Migrador, "SELECT count(*) FROM \"Cita\""));
    }

    [Fact]
    public async Task UnaTransaccionRevertida_LiberaElBloqueoYLaSiguienteAltaSeConfirma()
    {
        var e = await postgres.Crear();
        var paciente = await PacienteAsync(e);
        await using var primera = new NpgsqlConnection(e.App);
        await primera.OpenAsync();
        await using var primeraTransaccion = await primera.BeginTransactionAsync();
        await EjecutarAsync(primera, primeraTransaccion, InsertarCita(Guid.NewGuid(), paciente, "2026-10-04T10:00:00Z", "2026-10-04T11:00:00Z"));
        Assert.Equal(1, await BloqueosDeAsesoriaDeEstaSesionAsync(primera, primeraTransaccion));

        await using var segunda = new NpgsqlConnection(e.App);
        await segunda.OpenAsync();
        await using var segundaTransaccion = await segunda.BeginTransactionAsync();
        var altaDeLaSegunda = EjecutarAsync(segunda, segundaTransaccion,
            InsertarCita(Guid.NewGuid(), paciente, "2026-10-04T10:00:00Z", "2026-10-04T11:00:00Z"));
        try
        {
            await EsperarQueEsteBloqueadaAsync(e, segunda.ProcessID, altaDeLaSegunda);
            Assert.Equal(0, await BloqueosConcedidosAAsync(e, segunda.ProcessID));
        }
        finally
        {
            // Revertir la primera libera el bloqueo y su fila: la segunda ya no encuentra ningún cruce.
            await primeraTransaccion.RollbackAsync();
        }

        Assert.Equal(1, await altaDeLaSegunda.WaitAsync(TimeSpan.FromSeconds(15)));
        await segundaTransaccion.CommitAsync();
        Assert.Equal(1L, await PostgreSqlRevision.Valor<long>(e.Migrador, "SELECT count(*) FROM \"Cita\""));
    }

    // ──── MIGRACIÓN: SUBIR, BAJAR Y VOLVER A SUBIR ───────────────────────────────────────────

    private static async Task<(long Disparadores, long Funciones, long Restriccion, long Controles)> InventarioAsync(Escenario e) =>
    (
        await PostgreSqlRevision.Valor<long>(e.Migrador,
            "SELECT count(*) FROM pg_trigger WHERE NOT tgisinternal AND tgname IN ('TR_Cita_00SerializaAltaActiva','TR_Cita_00SerializaCambioActivo')"),
        await PostgreSqlRevision.Valor<long>(e.Migrador, "SELECT count(*) FROM pg_proc WHERE proname = 'FnCitaSerializaCruceActivo'"),
        await PostgreSqlRevision.Valor<long>(e.Migrador, "SELECT count(*) FROM pg_constraint WHERE conname = 'EX_Cita_SinCruceActivo'"),
        await PostgreSqlRevision.Valor<long>(e.Migrador,
            "SELECT count(*) FROM pg_trigger WHERE NOT tgisinternal AND tgname IN ('TR_Cita_ActivaExigePacienteVigente','TR_Paciente_BajaSinCitasActivas')")
    );

    [Fact]
    public async Task LaMigracion_SubeBajaYVuelveASubir_SinTocarLaRestriccionNiLosControlesDelPaciente()
    {
        var e = await postgres.Crear();
        var paciente = await PacienteAsync(e);
        await PostgreSqlRevision.Sql(e.App, InsertarCita(Guid.NewGuid(), paciente, "2026-10-04T10:00:00Z", "2026-10-04T11:00:00Z"));
        Assert.Equal((2L, 1L, 1L, 2L), await InventarioAsync(e));

        // Bajar: desaparecen los dos disparadores y la función; la restricción, los controles del paciente y los datos siguen.
        await MigrarAsync(e, MigracionAnterior);

        Assert.Equal((0L, 0L, 1L, 2L), await InventarioAsync(e));
        Assert.Equal(1L, await PostgreSqlRevision.Valor<long>(e.Migrador, "SELECT count(*) FROM \"Cita\""));
        Assert.Equal(0L, await PostgreSqlRevision.Valor<long>(e.Migrador,
            $"SELECT count(*) FROM \"__EFMigrationsHistory\" WHERE \"MigrationId\" = '{MigracionNueva}'"));

        // Sin los disparadores el cruce sigue protegido por la restricción.
        var cruce = await Assert.ThrowsAsync<PostgresException>(() => PostgreSqlRevision.Sql(e.App,
            InsertarCita(Guid.NewGuid(), paciente, "2026-10-04T10:30:00Z", "2026-10-04T11:30:00Z")));
        Assert.Equal("23P01", cruce.SqlState);

        // Volver a subir: vuelven los dos disparadores y la función, con los datos intactos.
        await MigrarAsync(e, MigracionNueva);

        Assert.Equal((2L, 1L, 1L, 2L), await InventarioAsync(e));
        Assert.Equal(1L, await PostgreSqlRevision.Valor<long>(e.Migrador, "SELECT count(*) FROM \"Cita\""));
        Assert.Equal(1L, await PostgreSqlRevision.Valor<long>(e.Migrador,
            $"SELECT count(*) FROM \"__EFMigrationsHistory\" WHERE \"MigrationId\" = '{MigracionNueva}'"));
    }

    // La aplicación no puede cambiar el esquema: los disparadores solo los crea y elimina el migrador.
    [Fact]
    public async Task LaAplicacionNoPuedeEliminarLosDisparadores()
    {
        var e = await postgres.Crear();

        var error = await Assert.ThrowsAsync<PostgresException>(() =>
            PostgreSqlRevision.Sql(e.App, "DROP TRIGGER \"TR_Cita_00SerializaAltaActiva\" ON \"Cita\""));

        Assert.Equal("42501", error.SqlState);
    }
}
