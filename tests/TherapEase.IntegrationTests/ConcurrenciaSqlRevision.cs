using System.Diagnostics;
using Npgsql;

namespace TherapEase.IntegrationTests;

/// <summary>
/// Prueba la concurrencia: lanza dos cambios simultáneos en PostgreSQL (por ejemplo, dos
/// reservas del mismo horario). Demuestra que la base hace esperar al segundo cambio y que,
/// al final, solo uno de los dos puede confirmarse.
/// </summary>
public static class ConcurrenciaSqlRevision
{


    // ──── CAMBIOS SIMULTÁNEOS ────────────────────────────────────────────────────────────────


    /// Ejecuta «primero» y lo deja sin confirmar; lanza «segundo» en otra conexión y comprueba
    /// que queda esperando un bloqueo. Luego confirma el primero y devuelve el resultado de
    /// ambos: «confirmada» o el código de error de PostgreSQL (23P01 cruce, TE002 último superusuario).
    public static async Task<string[]> EjecutarAsync(Escenario escenario, string primero, string segundo)
    {

        // Primer cambio: hecho pero sin commit, para que mantenga su bloqueo.
        await using var conexion = new NpgsqlConnection(escenario.App); // conexión a la base de datos de prueba.
        await conexion.OpenAsync();
        await using var transaccion = await conexion.BeginTransactionAsync();
        await using (var comando = new NpgsqlCommand(primero, conexion, transaccion))
            await comando.ExecuteNonQueryAsync();


        // Avisa el identificador de la segunda conexión para poder observarla en PostgreSQL.
        var iniciado = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);


        // Segundo cambio: compite con el primero desde otra conexión.
        async Task<string> CompetirAsync()
        {
            await using var otra = new NpgsqlConnection(escenario.App);
            await otra.OpenAsync();
            await using var operacion = await otra.BeginTransactionAsync();
            iniciado.SetResult(otra.ProcessID);
            try
            {
                await using var comando = new NpgsqlCommand(segundo, otra, operacion) { CommandTimeout = 10 };
                await comando.ExecuteNonQueryAsync();
                await operacion.CommitAsync();
                return "confirmada";
            }
            catch (PostgresException error) { return error.SqlState; }   // La base rechazó el cambio.
        }

        var competidor = CompetirAsync();
        var pid = await iniciado.Task.WaitAsync(TimeSpan.FromSeconds(10));
        try
        {


            // Hasta 5 segundos para ver en pg_stat_activity que el segundo espera un bloqueo.
            var espera = Stopwatch.StartNew();
            var bloqueado = false;
            while (espera.Elapsed < TimeSpan.FromSeconds(5) && !competidor.IsCompleted)
            {
                bloqueado = await PostgreSqlRevision.Valor<bool>(escenario.App,
                    $"SELECT EXISTS(SELECT 1 FROM pg_stat_activity WHERE pid={pid} AND wait_event_type='Lock')");
                if (bloqueado) break;
                await Task.Delay(20);
            }


            // Si no esperó, los cambios no fueron simultáneos y la prueba no demostraría nada.
            Assert.True(bloqueado, "El segundo cambio no esperó al primero: no se demostró la concurrencia.");
            await transaccion.CommitAsync();
            return ["confirmada", await competidor.WaitAsync(TimeSpan.FromSeconds(12))];
        }
        finally
        {


            // En caso de fallo, liberar el primero permite terminar al competidor y limpiar el contenedor.
            await transaccion.DisposeAsync();
            try { await competidor.WaitAsync(TimeSpan.FromSeconds(12)); } catch { }
        }
    }
}
