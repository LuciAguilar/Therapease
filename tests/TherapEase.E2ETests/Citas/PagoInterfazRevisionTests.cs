using System.Diagnostics;
using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Playwright;
using TherapEase.Domain.Citas.Entidades.Enums;

namespace TherapEase.E2ETests;

/// <summary>
/// Comprueba doble envío, respuestas perdidas y escrituras simultáneas de pago.
/// Controla el orden desde las pruebas y consulta la base sin añadir pausas al producto.
/// </summary>
public sealed partial class AccesoSesionesYPermisosTests
{


    // ──── RESPUESTAS PERDIDAS Y DOBLE CLIC ──────────────────────────────


    /// Un pago recibido puede volver a pendiente; al recargar conserva el cambio y sus dos eventos.
    [Fact]
    public Task PagoInterfazPermiteVolverDePagadoAPendiente() => Verificar(async () =>
    {
        // Arrange
        var p = await Pagina();
        var cita = await PrepararPagoRevision(p);
        await p.Locator(".pago-editor summary").ClickAsync();
        var formulario = p.Locator("[data-formulario-pago]");
        await formulario.GetByText("Pago recibido", new() { Exact = true }).ClickAsync();
        await formulario.GetByRole(AriaRole.Button, new() { Name = "Guardar cambio", Exact = true }).ClickAsync();
        await AvisoPagoRevision(p, "Estado de pago actualizado.");
        await Assertions.Expect(p.Locator(".pago-estado")).ToHaveTextAsync("Pagado");
        await ComprobarPagoRevision(cita.Id, EstadoPago.Pagado, 2, 1, cita.Cuenta.Id);
        await p.Locator(".notificacion-cerrar:visible").ClickAsync();
        await p.Locator(".pago-editor summary").ClickAsync();
        formulario = p.Locator("[data-formulario-pago]");
        // Act
        await formulario.GetByText("Aún por pagar", new() { Exact = true }).ClickAsync();
        await formulario.GetByRole(AriaRole.Button, new() { Name = "Guardar cambio", Exact = true }).ClickAsync();
        // Assert
        await AvisoPagoRevision(p, "Estado de pago actualizado.");
        await Assertions.Expect(p.Locator(".pago-estado")).ToHaveTextAsync("Pendiente");
        Exigir(await p.Locator("[name='VersionEsperada']").InputValueAsync() == "3");
        await ComprobarPagoRevision(cita.Id, EstadoPago.Pendiente, 3, 2, cita.Cuenta.Id);
        // Act
        await Abrir(p, "/Agenda/Detalle/" + cita.Id);
        // Assert
        await Assertions.Expect(p.Locator(".pago-estado")).ToHaveTextAsync("Pendiente");
        await Assertions.Expect(p.Locator("[data-formulario-pago] input[value='Pendiente']")).ToBeCheckedAsync();
        Exigir(await p.Locator("[name='VersionEsperada']").InputValueAsync() == "3");
        await ComprobarPagoRevision(cita.Id, EstadoPago.Pendiente, 3, 2, cita.Cuenta.Id);
    });

    /// Un corte antes o después de confirmar exige consultar, sin repetir ni inventar éxito.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task PagoInterfazRespuestaPerdidaExigeDecisionNueva(bool confirmado) => Verificar(async () =>
    {
        // Arrange
        var p = await Pagina();
        var cita = await PrepararPagoRevision(p);
        await p.Locator(".pago-editor summary").ClickAsync();
        var envios = 0;
        var estadoGuardado = 0;
        await p.RouteAsync("**/Agenda/Pago/" + cita.Id, async ruta =>
        {
            if (ruta.Request.Method != "POST") { await ruta.ContinueAsync(); return; }
            if (Interlocked.Increment(ref envios) == 1)
            {
                if (confirmado)
                    estadoGuardado = (await p.APIRequest.FetchAsync(ruta.Request,
                        new() { IgnoreHTTPSErrors = true, MaxRedirects = 0 })).Status;
                await ruta.AbortAsync();
            }
            else await ruta.ContinueAsync();
        });
        var formulario = p.Locator("[data-formulario-pago]");
        await formulario.GetByText("Pago recibido", new() { Exact = true }).ClickAsync();
        // Act
        await formulario.GetByRole(AriaRole.Button, new() { Name = "Guardar cambio", Exact = true }).ClickAsync();
        await Assertions.Expect(formulario.Locator("[data-aviso-pago]")).ToContainTextAsync("No pudimos confirmar");
        // Assert
        Exigir(envios == 1 && (!confirmado || estadoGuardado == 302));
        await Assertions.Expect(formulario.Locator("button[type='submit']")).ToBeDisabledAsync();
        await ComprobarPagoRevision(cita.Id, confirmado ? EstadoPago.Pagado : EstadoPago.Pendiente, confirmado ? 2 : 1, confirmado ? 1 : 0);
        // Act: la consulta no reenvía ni atribuye el guardado a la solicitud perdida.
        await formulario.Locator("[data-recargar-pago]").ClickAsync();
        await Assertions.Expect(p.Locator(".pago-estado")).ToHaveTextAsync(confirmado ? "Pagado" : "Pendiente");
        await p.Locator(".pago-editor summary").ClickAsync();
        await p.Locator("[data-formulario-pago]").GetByText("Pago recibido", new() { Exact = true }).ClickAsync();
        await p.Locator("[data-formulario-pago] button[type='submit']").ClickAsync();
        // Assert
        await AvisoPagoRevision(p, confirmado ? "No hubo cambios." : "Estado de pago actualizado.");
        Exigir(envios == 2);
        await ComprobarPagoRevision(cita.Id, EstadoPago.Pagado, 2, 1, cita.Cuenta.Id);
    });

    /// El doble clic real conserva un cambio; un segundo POST antiguo muestra conflicto y recarga.
    [Theory]
    [InlineData("alta")]
    [InlineData("panel")]
    public Task PagoInterfazDobleClicConRespuestaRetenida(string origen) => Verificar(async () =>
    {
        // Arrange
        var p = await Pagina();
        await Entrar(p, await entorno.CrearCuentaAsync());
        var (id, _) = await AbrirCitaParaPago(p, origen);
        var confirmado = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var liberar = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var finalizado = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var envios = 0;
        var primerEstado = 0;
        await p.RouteAsync("**/Agenda/Pago/" + id, async ruta =>
        {
            if (ruta.Request.Method != "POST") { await ruta.ContinueAsync(); return; }
            if (Interlocked.Increment(ref envios) != 1) { await ruta.ContinueAsync(); return; }
            try
            {
                var respuesta = await p.APIRequest.FetchAsync(ruta.Request,
                    new() { IgnoreHTTPSErrors = true, MaxRedirects = 0 });
                primerEstado = respuesta.Status;
                confirmado.TrySetResult();
                await liberar.Task.WaitAsync(TimeSpan.FromSeconds(15));
                try { await ruta.FulfillAsync(new() { Response = respuesta }); }
                catch (PlaywrightException) when (origen == "alta" && envios > 1)
                {
                    // El segundo envío puede cancelar la primera navegación.
                }
            }
            catch (Exception error) { confirmado.TrySetException(error); }
            finally { finalizado.TrySetResult(); }
        });
        var formulario = p.Locator("[data-formulario-pago]");
        await formulario.GetByText("Pago recibido", new() { Exact = true }).ClickAsync();
        // Act
        var doble = formulario.Locator("button[type='submit']").DblClickAsync();
        Exception? falloDoble = null;
        try
        {
            await confirmado.Task.WaitAsync(TimeSpan.FromSeconds(15));
            // Assert: se consulta mientras la respuesta sigue retenida, después del commit real.
            Exigir(primerEstado == 302);
            await ComprobarPagoRevision(id, EstadoPago.Pagado, 2, 1);
            if (origen == "panel") await Assertions.Expect(formulario.Locator("button[type='submit']")).ToBeDisabledAsync();
        }
        catch (Exception error) { falloDoble = error; }
        finally { liberar.TrySetResult(); }
        await Task.WhenAll(doble, finalizado.Task).WaitAsync(TimeSpan.FromSeconds(15));
        if (falloDoble is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(falloDoble).Throw();
        // Assert
        if (envios > 1)
        {
            await Assertions.Expect(p.Locator("[data-contenido-detalle-cita]")).ToContainTextAsync("El registro cambió mientras se editaba");
            await Assertions.Expect(p.Locator("[data-recargar-pago]")).ToBeVisibleAsync();
        }
        else await Assertions.Expect(p.Locator(".pago-estado")).ToHaveTextAsync("Pagado");
        await ComprobarPagoRevision(id, EstadoPago.Pagado, 2, 1);
        await System.IO.File.WriteAllTextAsync(System.IO.Path.Combine(entorno.Servidor.Evidencia, "doble-pago-" + origen + ".json"),
            System.Text.Json.JsonSerializer.Serialize(new { Origen = origen, Envios = envios, Cambios = 1, Eventos = 1 }));
    });


    // ──── ESPERA REAL DE POSTGRESQL ────────────────────────────────────


    /// Un POST duplicado por el cliente de prueba conserva un cambio y su respuesta real de conflicto permite recargar.
    [Fact]
    public Task PagoInterfazSegundoPostTrasAltaMuestraConflicto() => Verificar(async () =>
    {
        // Arrange
        var p = await Pagina();
        await Entrar(p, await entorno.CrearCuentaAsync());
        var (id, _) = await AbrirCitaParaPago(p, "alta");
        var enviosServidor = 0;
        var primerEstado = 0;
        var segundoEstado = 0;
        await p.RouteAsync("**/Agenda/Pago/" + id, async ruta =>
        {
            if (ruta.Request.Method != "POST") { await ruta.ContinueAsync(); return; }
            // La misma solicitud se entrega dos veces al servidor, sin cambiar cuerpo, token ni versión.
            enviosServidor++;
            var primera = await p.APIRequest.FetchAsync(ruta.Request,
                new() { IgnoreHTTPSErrors = true, MaxRedirects = 0 });
            primerEstado = primera.Status;
            await ComprobarPagoRevision(id, EstadoPago.Pagado, 2, 1);
            enviosServidor++;
            var segunda = await p.APIRequest.FetchAsync(ruta.Request,
                new() { IgnoreHTTPSErrors = true, MaxRedirects = 0 });
            segundoEstado = segunda.Status;
            // El navegador recibe el HTML real del segundo envío, no una página fabricada por la prueba.
            await ruta.FulfillAsync(new() { Response = segunda });
        });
        var formulario = p.Locator("[data-formulario-pago]");
        await formulario.GetByText("Pago recibido", new() { Exact = true }).ClickAsync();
        // Act
        await formulario.Locator("button[type='submit']").ClickAsync();
        // Assert
        await Assertions.Expect(p.Locator("[data-contenido-detalle-cita]")).ToContainTextAsync("El registro cambió mientras se editaba");
        await Assertions.Expect(p.Locator("[data-recargar-pago]")).ToBeVisibleAsync();
        Exigir(enviosServidor == 2 && primerEstado == 302 && segundoEstado == 200);
        // Act
        await p.Locator("[data-recargar-pago]").ClickAsync();
        // Assert
        await Assertions.Expect(p.Locator(".pago-estado")).ToHaveTextAsync("Pagado");
        Exigir(await p.Locator("[name='VersionEsperada']").InputValueAsync() == "2" && enviosServidor == 2);
        await ComprobarPagoRevision(id, EstadoPago.Pagado, 2, 1);
    });

    /// Dos POST que esperan por la misma fila no sobrescriben ni dejan dos eventos.
    [Fact]
    public Task PagoInterfazDosPostConBloqueoRealDeFila() => Verificar(async () =>
    {
        // Arrange
        var p = await Pagina();
        var cita = await PrepararPagoRevision(p);
        await using var bloqueo = entorno.Servidor.Escenario.Contexto();
        await using var transaccion = await bloqueo.Database.BeginTransactionAsync();
        await bloqueo.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM \"Cita\" WHERE \"IdCita\"={cita.Id} FOR UPDATE");
        var conexion = bloqueo.Database.GetDbConnection();
        await using var comandoPid = conexion.CreateCommand();
        comandoPid.Transaction = transaccion.GetDbTransaction();
        comandoPid.CommandText = "SELECT pg_backend_pid()";
        var pid = Convert.ToInt32(await comandoPid.ExecuteScalarAsync());
        var token = await Token(p);
        // Act
        var primera = Enviar(p, "/Agenda/Pago/" + cita.Id, new() { ["VersionEsperada"] = "1", ["NuevoPago"] = "Pagado", ["__RequestVerificationToken"] = token }, protegido: false);
        Task<IAPIResponse>? segunda = null;
        Exception? fallo = null;
        try
        {
            await EsperarBloqueadosPagoRevision(pid, 1);
            segunda = Enviar(p, "/Agenda/Pago/" + cita.Id, new() { ["VersionEsperada"] = "1", ["NuevoPago"] = "Pagado", ["__RequestVerificationToken"] = token }, protegido: false);
            await EsperarBloqueadosPagoRevision(pid, 2);
            // Assert: las dos solicitudes alcanzaron el bloqueo antes de liberarlo.
            Exigir(!primera.IsCompleted && !segunda.IsCompleted);
        }
        catch (Exception error) { fallo = error; }
        finally { await transaccion.CommitAsync(); }
        var respuestas = segunda is null ? [await primera] : await Task.WhenAll(primera, segunda);
        if (fallo is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(fallo).Throw();
        // Assert
        Exigir(respuestas.Count(r => r.Status == 302) == 1 && respuestas.Count(r => r.Status == 200) == 1);
        Exigir(WebUtility.HtmlDecode(await respuestas.Single(r => r.Status == 200).TextAsync()).Contains("El registro cambió mientras se editaba"));
        await ComprobarPagoRevision(cita.Id, EstadoPago.Pagado, 2, 1, cita.Cuenta.Id);
    });

    /// Observa quién espera al bloqueo, con plazo máximo y sin adivinar por una demora fija.
    private async Task EsperarBloqueadosPagoRevision(int pid, int minimo)
    {
        await using var db = entorno.Servidor.Escenario.Contexto();
        await db.Database.OpenConnectionAsync();
        await using var comando = db.Database.GetDbConnection().CreateCommand();
        comando.CommandText = """
            WITH RECURSIVE espera(pid) AS (
                SELECT pid FROM pg_stat_activity WHERE datname=current_database() AND @pid=ANY(pg_blocking_pids(pid))
                UNION
                SELECT a.pid FROM pg_stat_activity a JOIN espera e ON e.pid=ANY(pg_blocking_pids(a.pid))
                WHERE a.datname=current_database()
            ) SELECT count(*) FROM espera
            """;
        var parametro = comando.CreateParameter();
        parametro.ParameterName = "pid";
        parametro.Value = pid;
        comando.Parameters.Add(parametro);
        var reloj = Stopwatch.StartNew();
        while (reloj.Elapsed < TimeSpan.FromSeconds(12))
        {
            if (Convert.ToInt32(await comando.ExecuteScalarAsync()) >= minimo) return;
            await Task.Delay(25);
        }
        throw new TimeoutException("No se observó el orden previsto de las solicitudes de pago.");
    }
}
