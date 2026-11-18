using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using TherapEase.Domain.Auditoria.Entidades;

namespace TherapEase.E2ETests;

/// <summary>
/// Comprueba consulta, reserva y recuperación de citas en las páginas reales.
/// Usa PostgreSQL, HTTPS y cuentas ficticias; no guarda formularios ni contraseñas en la evidencia.
/// </summary>
public sealed partial class AccesoSesionesYPermisosTests
{


    // ──── DATOS Y FORMULARIOS DE CITAS ───────────────────────────────────


    private static int _diaDeAgenda;

    /// Compara los campos enviados sin confundir el límite aleatorio de cada formulario multipart con un cambio de datos.
    private static string CompararContenido(string? contenido) => System.Text.RegularExpressions.Regex.Replace(
        contenido ?? "", @"(?m)^--[^\r\n]+", "--LIMITE");

    /// Separa los horarios de cada prueba para que una reserva no ocupe el hueco de otra.
    private static string FechaDeAgenda() => new DateOnly(2053, 1, 1)
        .AddDays(Interlocked.Increment(ref _diaDeAgenda)).ToString("yyyy-MM-dd");

    /// Prepara únicamente los campos que la persona puede enviar al reservar.
    private static Dictionary<string, string> DatosDeReserva(Guid id, Guid paciente, string fecha,
        string inicio = "14:30", string fin = "15:30") => new()
    {
        ["IdSolicitud"] = id.ToString(),
        ["Entrada.IdPaciente"] = paciente.ToString(),
        ["Entrada.Ambito"] = "Escolar",
        ["Entrada.Fecha"] = fecha,
        ["Entrada.HoraInicio"] = inicio,
        ["Entrada.HoraFin"] = fin
    };

    /// Completa el formulario existente y devuelve su número, conservándolo ante un envío incierto.
    private static async Task<Guid> CapturarReserva(ILocator formulario, string fecha)
    {
        await formulario.Locator("[name='Entrada.Ambito']").SelectOptionAsync("Escolar");
        await formulario.Locator("[name='Entrada.Fecha']").FillAsync(fecha);
        await formulario.Locator("[name='Entrada.HoraInicio']").FillAsync("14:30");
        await formulario.Locator("[name='Entrada.HoraFin']").FillAsync("15:30");
        return Guid.Parse(await formulario.Locator("[name='IdSolicitud']").InputValueAsync());
    }


    // ──── ACCESO Y CONSULTA ─────────────────────────────────────────────


    /// Sin sesión, ninguna página de citas entrega datos y todas llevan al acceso.
    [Theory]
    [InlineData("/Agenda")]
    [InlineData("/Agenda/Crear")]
    [InlineData("/Agenda/Detalle/00000000-0000-0000-0000-000000000001")]
    public Task AgendaAnonimaExigeSesion(string ruta) => Verificar(async () =>
    {
        // Arrange
        var p = await Pagina();
        // Act
        await Abrir(p, ruta);
        // Assert
        Exigir(Ruta(p) == "/Acceso");
    });

    /// Una cuenta con temporal solo puede cambiar su contraseña, tampoco puede abrir citas directamente.
    [Theory]
    [InlineData("/Agenda")]
    [InlineData("/Agenda/Crear")]
    [InlineData("/Agenda/Detalle/00000000-0000-0000-0000-000000000001")]
    public Task AgendaConTemporalPermaneceRestringida(string ruta) => Verificar(async () =>
    {
        // Arrange
        var p = await Pagina();
        await Entrar(p, await entorno.CrearCuentaAsync(temporal: true));
        // Act
        await Abrir(p, ruta);
        // Assert
        Exigir(Ruta(p) == "/Cuenta/Contrasena");
    });

    /// Ambos roles reservan por el panel; el detalle conserva pago pendiente y abre la ficha del paciente.
    [Theory]
    [InlineData("Usuaria")]
    [InlineData("Superusuario")]
    public Task ReservaDesdePanelGuardaUnaCitaYUnEvento(string rol) => Verificar(async () =>
    {
        // Arrange
        var nombre = "Agenda Ficticia " + Guid.NewGuid().ToString("N");
        var paciente = await PrepararPaciente(nombre);
        var fecha = FechaDeAgenda();
        var p = await Pagina();
        await Entrar(p, await entorno.CrearCuentaAsync(rol));
        await Abrir(p, "/Agenda?Desde=" + fecha + "&Hasta=" + fecha);
        // Act
        await p.Locator("a.agenda-nueva").First.ClickAsync();
        var panel = p.Locator("#panel-nueva-cita");
        await Assertions.Expect(panel).ToBeVisibleAsync();
        await panel.Locator("[name='Texto']").FillAsync(nombre);
        await panel.GetByRole(AriaRole.Button, new() { NameRegex = new("^" + nombre) }).ClickAsync();
        var formulario = panel.Locator("[data-formulario-cita]");
        await Assertions.Expect(formulario.GetByText("Paciente seleccionado: " + nombre, new() { Exact = true })).ToBeVisibleAsync();
        var id = await CapturarReserva(formulario, fecha);
        _paso = "guardar reserva desde panel";
        await p.RunAndWaitForResponseAsync(
            () => formulario.GetByRole(AriaRole.Button, new() { Name = "Agendar cita", Exact = true }).ClickAsync(),
            r => r.Request.Method == "POST" && new Uri(r.Url).AbsolutePath == "/Agenda/Crear");
        await Assertions.Expect(panel).Not.ToBeVisibleAsync();
        await p.WaitForLoadStateAsync();
        var confirmaciones = await p.Locator(".aviso-confirmacion").CountAsync();
        await Abrir(p, "/Agenda?Desde=" + fecha + "&Hasta=" + fecha);
        await p.Locator("a.agenda-cita").ClickAsync();
        // Assert
        var detalle = p.Locator("#panel-detalle-cita");
        await Assertions.Expect(detalle).ToContainTextAsync("Pendiente");
        await Assertions.Expect(detalle).ToContainTextAsync("14:30");
        await detalle.Locator("a.cita-enlace-paciente").ClickAsync();
        await Assertions.Expect(p.Locator("#panel-contenido-ficha")).ToContainTextAsync(nombre);
        await using var db = entorno.Servidor.Escenario.Contexto();
        Exigir(await db.Citas.CountAsync(x => x.IdCita == id) == 1);
        Exigir(await db.Set<EventoAuditoria>().CountAsync(x => x.IdRegistro == id) == 1);
        await File.WriteAllTextAsync(Path.Combine(entorno.Servidor.Evidencia, "confirmacion-panel-" + rol + ".json"),
            System.Text.Json.JsonSerializer.Serialize(new { Rol = rol, Citas = 1, Eventos = 1, AvisosAlVolverALaAgenda = confirmaciones }));
        _paso = "confirmacion visible despues del panel";
        Exigir(confirmaciones == 1);
    });

    /// Una agenda vacía conserva su cuadrícula; Semana y Mes permiten moverse a otro periodo.
    [Fact]
    public Task AgendaVaciaPermiteCambiarPeriodoSinInventarCitas() => Verificar(async () =>
    {
        // Arrange
        var p = await Pagina();
        await Entrar(p, await entorno.CrearCuentaAsync());
        // Act
        await Abrir(p, "/Agenda?Desde=2058-02-01&Hasta=2058-02-07");
        // Assert
        await Assertions.Expect(p.GetByRole(AriaRole.Region, new() { Name = "Calendario del periodo" })).ToBeVisibleAsync();
        await Assertions.Expect(p.Locator(".agenda-columna")).ToHaveCountAsync(7);
        await Task.WhenAll(p.WaitForURLAsync(u => u.Contains("periodo=mes")), Boton(p, "Mes").ClickAsync());
        await Assertions.Expect(p.Locator(".agenda-columna")).ToHaveCountAsync(28);
        await Task.WhenAll(p.WaitForURLAsync(u => u.Contains("2058-03")),
            p.GetByRole(AriaRole.Link, new() { Name = "Periodo siguiente" }).ClickAsync());
        await Assertions.Expect(p.Locator(".agenda-columna")).ToHaveCountAsync(31);
    });

    /// Periodos y vistas inválidos muestran un rechazo controlado y nunca un error 500.
    [Theory]
    [InlineData("Desde=2054-01-01")]
    [InlineData("Desde=2054-02-01&Hasta=2054-01-01")]
    [InlineData("Desde=2054-01-01&Hasta=2054-03-01")]
    [InlineData("Desde=9999-12-31&Hasta=9999-12-31")]
    [InlineData("Vista=999")]
    public Task AgendaRechazaEntradasLimiteSinFallarElServidor(string consulta) => Verificar(async () =>
    {
        // Arrange
        var p = await Pagina();
        await Entrar(p, await entorno.CrearCuentaAsync());
        // Act
        var respuesta = await Abrir(p, "/Agenda?" + consulta);
        // Assert
        Exigir(respuesta.Status == 200);
        await Assertions.Expect(p.GetByRole(AriaRole.Alert)).ToBeVisibleAsync();
    });


    // ──── GUARDADO, PROTECCIÓN Y REENVÍOS ────────────────────────────────


    /// Intervalos no permitidos se rechazan y conservan el número y la captura sin crear una cita.
    [Theory]
    [InlineData("08:00", "09:00")]
    [InlineData("14:30", "15:00")]
    [InlineData("18:30", "19:30")]
    public Task ReservaInvalidaPorHttpConservaLaCaptura(string inicio, string fin) => Verificar(async () =>
    {
        // Arrange
        var paciente = await PrepararPaciente("Horario " + Guid.NewGuid().ToString("N"));
        var p = await Pagina();
        await Entrar(p, await entorno.CrearCuentaAsync());
        await Abrir(p, "/Agenda/Crear?paciente=" + paciente);
        var id = Guid.NewGuid();
        var datos = DatosDeReserva(id, paciente, FechaDeAgenda(), inicio, fin);
        // Act
        var respuesta = await Enviar(p, "/Agenda/Crear", datos);
        // Assert
        Exigir(respuesta.Status == 200);
        var html = await respuesta.TextAsync();
        Exigir(html.Contains(id.ToString()) && html.Contains("role=\"alert\""));
        Exigir(html.Contains(inicio) && html.Contains(fin));
        await using var db = entorno.Servidor.Escenario.Contexto();
        Exigir(!await db.Citas.AnyAsync(x => x.IdCita == id));
    });

    /// Repetir la misma reserva reconoce el guardado; cambiar sus datos no crea otra cita ni evento.
    [Fact]
    public Task DoblePostDeReservaConservaUnaSolaCitaAuditada() => Verificar(async () =>
    {
        // Arrange
        var paciente = await PrepararPaciente("Reenvio " + Guid.NewGuid().ToString("N"));
        var p = await Pagina();
        await Entrar(p, await entorno.CrearCuentaAsync());
        await Abrir(p, "/Agenda/Crear?paciente=" + paciente);
        var id = Guid.NewGuid();
        var datos = DatosDeReserva(id, paciente, FechaDeAgenda());
        // Act
        var primero = await Enviar(p, "/Agenda/Crear", datos);
        var segundo = await Enviar(p, "/Agenda/Crear", datos);
        datos["Entrada.HoraInicio"] = "15:30";
        datos["Entrada.HoraFin"] = "16:30";
        var distinto = await Enviar(p, "/Agenda/Crear", datos);
        // Assert
        Exigir(primero.Status == 200 && segundo.Status == 200 && distinto.Status == 200);
        Exigir((await segundo.TextAsync()).Contains("ya estaba"));
        Exigir((await distinto.TextAsync()).Contains("role=\"alert\""));
        await using var db = entorno.Servidor.Escenario.Contexto();
        Exigir(await db.Citas.CountAsync(x => x.IdCita == id) == 1);
        Exigir(await db.Set<EventoAuditoria>().CountAsync(x => x.IdRegistro == id) == 1);
    });

    /// Un POST sin token o con token de otra sesión no llega a guardar ninguna cita.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task ReservaExigeAntifalsificacionDeLaMismaSesion(bool tokenAjeno) => Verificar(async () =>
    {
        // Arrange
        var paciente = await PrepararPaciente("Proteccion " + Guid.NewGuid().ToString("N"));
        var p = await Pagina();
        await Entrar(p, await entorno.CrearCuentaAsync());
        await Abrir(p, "/Agenda/Crear?paciente=" + paciente);
        var id = Guid.NewGuid();
        var datos = DatosDeReserva(id, paciente, FechaDeAgenda());
        if (tokenAjeno)
        {
            var otra = await Pagina();
            await Entrar(otra, await entorno.CrearCuentaAsync());
            await Abrir(otra, "/Agenda/Crear");
            datos["__RequestVerificationToken"] = await Token(otra);
        }
        // Act
        var respuesta = await Enviar(p, "/Agenda/Crear", datos, protegido: false);
        // Assert
        Exigir(respuesta.Status == 400);
        await using var db = entorno.Servidor.Escenario.Contexto();
        Exigir(!await db.Citas.AnyAsync(x => x.IdCita == id));
    });

    /// Tras un corte antes o después de guardar, comprobar no escribe y reenviar conserva una sola cita auditada.
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public Task ReservaConRespuestaPerdidaRecuperaElMismoNumero(bool despuesDeGuardar, bool comprobar) => Verificar(async () =>
    {
        // Arrange
        var paciente = await PrepararPaciente("Respuesta perdida " + Guid.NewGuid().ToString("N"));
        var p = await Pagina();
        await Entrar(p, await entorno.CrearCuentaAsync());
        await Abrir(p, "/Agenda/Crear?paciente=" + paciente);
        var formulario = p.Locator("[data-formulario-cita]");
        var id = await CapturarReserva(formulario, FechaDeAgenda());
        var envios = 0;
        string? contenidoOriginal = null;
        var mismoContenido = true;
        var estadoDelEnvioPreparado = 0;
        string? falloDelEnvioPreparado = null;
        await p.RouteAsync("**/Agenda/Crear", async ruta =>
        {
            if (ruta.Request.Method != "POST")
            {
                await ruta.ContinueAsync();
                return;
            }
            envios++;
            if (envios == 1)
            {
                contenidoOriginal = CompararContenido(ruta.Request.PostData);
                if (despuesDeGuardar)
                {
                    // El cliente de prueba acepta solo el certificado ficticio y conserva la solicitud interceptada.
                    try
                    {
                        var guardado = await p.APIRequest.FetchAsync(ruta.Request,
                            new() { IgnoreHTTPSErrors = true, MaxRedirects = 0 });
                        estadoDelEnvioPreparado = guardado.Status;
                    }
                    catch (Exception error)
                    {
                        falloDelEnvioPreparado = error.GetType().Name + ":" + string.Join(",",
                            new[] { "certificate", "disposed", "closed", "timeout", "ECONN", "headers", "multipart", "PostData" }
                                .Where(texto => error.Message.Contains(texto, StringComparison.OrdinalIgnoreCase)));
                    }
                }
                await ruta.AbortAsync();
            }
            else
            {
                // Se compara el contenido dentro de memoria; no se imprime el token de protección.
                mismoContenido &= CompararContenido(ruta.Request.PostData) == contenidoOriginal;
                await ruta.ContinueAsync();
            }
        });
        // Act
        await formulario.GetByRole(AriaRole.Button, new() { Name = "Agendar cita", Exact = true }).ClickAsync();
        await Assertions.Expect(formulario.Locator("[data-estado-envio]")).ToContainTextAsync("No se pudo confirmar el resultado");
        await Assertions.Expect(formulario.Locator("[data-acciones-recuperacion]")).ToBeVisibleAsync();
        // Assert
        _paso = "comprobar guardado antes del reenvio";
        await using (var antes = entorno.Servidor.Escenario.Contexto())
        {
            var citasAntes = await antes.Citas.CountAsync(x => x.IdCita == id);
            await File.WriteAllTextAsync(Path.Combine(entorno.Servidor.Evidencia, "corte-" + despuesDeGuardar + "-comprobar-" + comprobar + ".json"),
                System.Text.Json.JsonSerializer.Serialize(new { DespuesDeGuardar = despuesDeGuardar,
                    EstadoHttp = estadoDelEnvioPreparado, Fallo = falloDelEnvioPreparado, CitasAntes = citasAntes, Envios = envios }));
            Exigir(citasAntes == (despuesDeGuardar ? 1 : 0));
        }
        Exigir(envios == 1);
        if (comprobar)
        {
            // Act: abrir la consulta en otra pestaña conserva la captura original.
            _paso = "comprobar la reserva despues del corte";
            var consulta = await p.RunAndWaitForPopupAsync(
                () => formulario.Locator("[data-comprobar-cita]").ClickAsync());
            await consulta.WaitForLoadStateAsync();
            // Assert
            Exigir(new Uri(consulta.Url).AbsolutePath == "/Agenda/Detalle/" + id);
            if (despuesDeGuardar)
            {
                await Assertions.Expect(consulta.Locator(".cita-detalle")).ToBeVisibleAsync();
            }
            else
            {
                await Assertions.Expect(consulta.GetByRole(AriaRole.Alert)).ToHaveTextAsync("No se encontró la cita.");
                Exigir(await consulta.Locator(".cita-detalle").CountAsync() == 0);
            }
            Exigir(await formulario.Locator("[name='IdSolicitud']").InputValueAsync() == id.ToString());
            Exigir(envios == 1);
            await using var soloConsulta = entorno.Servidor.Escenario.Contexto();
            var esperado = despuesDeGuardar ? 1 : 0;
            Exigir(await soloConsulta.Citas.CountAsync(x => x.IdCita == id) == esperado);
            Exigir(await soloConsulta.Set<EventoAuditoria>().CountAsync(x => x.IdRegistro == id) == esperado);
            return;
        }
        // Act
        await Boton(p, "Reenviar la misma reserva").ClickAsync();
        await Assertions.Expect(p.Locator(".cita-detalle")).ToBeVisibleAsync();
        _paso = "comprobar campos originales del reenvio manual";
        Exigir(envios == 2 && mismoContenido);
        await using var db = entorno.Servidor.Escenario.Contexto();
        Exigir(await db.Citas.CountAsync(x => x.IdCita == id) == 1);
        Exigir(await db.Set<EventoAuditoria>().CountAsync(x => x.IdRegistro == id) == 1);
    });

    /// Mientras la reserva espera al servidor, solo muestra el estado de envío, sin ofrecer todavía un reenvío.
    [Fact]
    public Task RecuperacionPermaneceOcultaDuranteElEnvio() => Verificar(async () =>
    {
        // Arrange
        var paciente = await PrepararPaciente("Acciones durante envio " + Guid.NewGuid().ToString("N"));
        var p = await Pagina();
        await Entrar(p, await entorno.CrearCuentaAsync());
        await Abrir(p, "/Agenda/Crear?paciente=" + paciente);
        var formulario = p.Locator("[data-formulario-cita]");
        await CapturarReserva(formulario, FechaDeAgenda());
        var retenido = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var continuar = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await p.RouteAsync("**/Agenda/Crear", async ruta =>
        {
            retenido.TrySetResult();
            await continuar.Task;
            await ruta.AbortAsync();
        });
        // Act
        await formulario.GetByRole(AriaRole.Button, new() { Name = "Agendar cita", Exact = true }).ClickAsync();
        await retenido.Task.WaitAsync(TimeSpan.FromSeconds(10));
        _paso = "recuperacion oculta mientras se espera al servidor";
        try
        {
            // Assert
            await Assertions.Expect(formulario.Locator("[data-estado-envio]")).ToHaveTextAsync("Enviando reserva…");
            var acciones = formulario.Locator("[data-acciones-recuperacion]");
            var atributoOculto = await acciones.GetAttributeAsync("hidden") is not null;
            var visible = await acciones.IsVisibleAsync();
            await File.WriteAllTextAsync(Path.Combine(entorno.Servidor.Evidencia, "acciones-durante-envio.json"),
                System.Text.Json.JsonSerializer.Serialize(new { AtributoHidden = atributoOculto, Visible = visible }));
            await Assertions.Expect(acciones).Not.ToBeVisibleAsync();
        }
        finally
        {
            continuar.TrySetResult();
        }
    });


    // ──── PRESENTACIÓN Y PRIVACIDAD ──────────────────────────────────────


    /// El nombre se muestra como texto en agenda y detalle; cambiar la zona del navegador no mueve las horas.
    [Theory]
    [InlineData("Asia/Tokyo")]
    [InlineData("America/New_York")]
    public Task AgendaConservaHermosilloYNoEjecutaElNombre(string zona) => Verificar(async () =>
    {
        // Arrange
        var nombre = "Ficticia <script>globalThis.qaAgenda=1</script> " + Guid.NewGuid().ToString("N");
        var paciente = await PrepararPaciente(nombre);
        var fecha = FechaDeAgenda();
        var contexto = await entorno.Navegador.NewContextAsync(new() { IgnoreHTTPSErrors = true, TimezoneId = zona });
        contexto.SetDefaultTimeout(10000);
        _contextos.Add(contexto);
        var p = await contexto.NewPageAsync();
        await Entrar(p, await entorno.CrearCuentaAsync());
        await Abrir(p, "/Agenda/Crear?paciente=" + paciente);
        var id = Guid.NewGuid();
        Exigir((await Enviar(p, "/Agenda/Crear", DatosDeReserva(id, paciente, fecha))).Status == 200);
        // Act
        await Abrir(p, "/Agenda?Desde=" + fecha + "&Hasta=" + fecha);
        // Assert
        await Assertions.Expect(p.Locator("a.agenda-cita")).ToContainTextAsync(nombre);
        await Assertions.Expect(p.Locator("a.agenda-cita")).ToContainTextAsync("14:30–15:30");
        Exigir(await p.EvaluateAsync<int>("() => globalThis.qaAgenda || 0") == 0);
        await p.Locator("a.agenda-cita").ClickAsync();
        await Assertions.Expect(p.Locator("#panel-detalle-cita")).ToContainTextAsync(nombre);
        Exigir(await p.Locator(".cita-detalle script").CountAsync() == 0);
        Exigir(await p.EvaluateAsync<int>("() => globalThis.qaAgenda || 0") == 0);
    });

    /// El formulario se puede abrir y cerrar por teclado en un teléfono y una pantalla de escritorio.
    [Theory]
    [InlineData(390)]
    [InlineData(1280)]
    public Task PanelDeCitaConservaElCierrePorTeclado(int ancho) => Verificar(async () =>
    {
        // Arrange
        var p = await Pagina();
        await p.SetViewportSizeAsync(ancho, 844);
        await Entrar(p, await entorno.CrearCuentaAsync());
        await Abrir(p, "/Agenda?Desde=2058-04-01&Hasta=2058-04-07");
        // Act
        await p.Locator("a.agenda-nueva").First.ClickAsync();
        // Assert
        await Assertions.Expect(p.Locator("#panel-nueva-cita [data-formulario-cita]")).ToBeVisibleAsync();
        await p.Keyboard.PressAsync("Escape");
        await Assertions.Expect(p.Locator("#panel-nueva-cita")).Not.ToBeVisibleAsync();
        Exigir(await p.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= innerWidth"));
    });

    /// Dar de baja al paciente después de abrir la captura impide guardar y conserva el número del formulario.
    [Fact]
    public Task PacienteRetiradoDespuesDeAbrirLaCapturaNoSeAgenda() => Verificar(async () =>
    {
        // Arrange
        var paciente = await PrepararPaciente("Baja preparada " + Guid.NewGuid().ToString("N"));
        var p = await Pagina();
        await Entrar(p, await entorno.CrearCuentaAsync());
        await Abrir(p, "/Agenda/Crear?paciente=" + paciente);
        var formulario = p.Locator("[data-formulario-cita]");
        var id = await CapturarReserva(formulario, FechaDeAgenda());
        await using (var cambio = entorno.Servidor.Escenario.Contexto())
        {
            await cambio.Database.ExecuteSqlInterpolatedAsync($"UPDATE \"Paciente\" SET \"Condicion\"='Baja', \"FechaBaja\"={DateTimeOffset.UtcNow}, \"IdUsuarioBaja\"={Guid.NewGuid()} WHERE \"IdPaciente\"={paciente}");
        }
        // Act
        await formulario.GetByRole(AriaRole.Button, new() { Name = "Agendar cita", Exact = true }).ClickAsync();
        // Assert
        await Assertions.Expect(p.Locator("[data-formulario-cita] [role='alert']")).ToBeVisibleAsync();
        Exigir(await p.Locator("[name='IdSolicitud']").InputValueAsync() == id.ToString());
        await using var db = entorno.Servidor.Escenario.Contexto();
        Exigir(!await db.Citas.AnyAsync(x => x.IdCita == id));
        Exigir(!await db.Set<EventoAuditoria>().AnyAsync(x => x.IdRegistro == id));
    });

    /// El doble clic durante una petición retenida envía una sola reserva y registra un único evento.
    [Fact]
    public Task DobleClicConEnvioRetenidoNoRepiteLaPeticion() => Verificar(async () =>
    {
        // Arrange
        var paciente = await PrepararPaciente("Doble clic " + Guid.NewGuid().ToString("N"));
        var p = await Pagina();
        await Entrar(p, await entorno.CrearCuentaAsync());
        await Abrir(p, "/Agenda/Crear?paciente=" + paciente);
        var formulario = p.Locator("[data-formulario-cita]");
        var id = await CapturarReserva(formulario, FechaDeAgenda());
        var retenido = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var continuar = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var envios = 0;
        await p.RouteAsync("**/Agenda/Crear", async ruta =>
        {
            envios++;
            retenido.TrySetResult();
            await continuar.Task;
            await ruta.ContinueAsync();
        });
        // Act
        var clics = formulario.GetByRole(AriaRole.Button, new() { Name = "Agendar cita", Exact = true }).DblClickAsync();
        try
        {
            await retenido.Task.WaitAsync(TimeSpan.FromSeconds(10));
            // Assert
            await Assertions.Expect(formulario.Locator("fieldset")).ToHaveAttributeAsync("disabled", "");
            Exigir(envios == 1);
        }
        finally
        {
            continuar.TrySetResult();
        }
        await clics;
        await Assertions.Expect(p.Locator(".cita-detalle")).ToBeVisibleAsync();
        await using var db = entorno.Servidor.Escenario.Contexto();
        Exigir(await db.Citas.CountAsync(x => x.IdCita == id) == 1 && envios == 1);
        Exigir(await db.Set<EventoAuditoria>().CountAsync(x => x.IdRegistro == id) == 1);
    });

    /// El selector elige un día y minutos concretos, y Escape cierra primero el selector sin cerrar el formulario.
    [Fact]
    public Task SelectoresDeCitaConservanFechaYMinutosElegidos() => Verificar(async () =>
    {
        // Arrange
        var paciente = await PrepararPaciente("Selectores " + Guid.NewGuid().ToString("N"));
        var p = await Pagina();
        await Entrar(p, await entorno.CrearCuentaAsync());
        await Abrir(p, "/Agenda/Crear?paciente=" + paciente);
        await p.Locator("[name='Entrada.Fecha']").FillAsync("2054-03-10");
        // Act
        await p.Locator("[name='Entrada.Fecha']").ClickAsync();
        var calendario = p.GetByRole(AriaRole.Group, new() { Name = "Calendario de la cita" });
        await calendario.Locator(".cita-calendario-dias button").Nth(14).ClickAsync();
        await p.Locator("[name='Entrada.HoraInicio']").ClickAsync();
        var horario = p.GetByRole(AriaRole.Group, new() { Name = "Selector de hora" });
        await horario.GetByLabel("Hora", new() { Exact = true }).SelectOptionAsync("18");
        await horario.GetByLabel("Minutos", new() { Exact = true }).SelectOptionAsync("30");
        await Boton(p, "Aplicar hora seleccionada").ClickAsync();
        // Assert
        Exigir(await p.Locator("[name='Entrada.Fecha']").InputValueAsync() == "2054-03-15");
        Exigir(await p.Locator("[name='Entrada.HoraInicio']").InputValueAsync() == "18:30");
        await p.Locator("[name='Entrada.Fecha']").ClickAsync();
        await p.Keyboard.PressAsync("Escape");
        await Assertions.Expect(p.GetByRole(AriaRole.Group, new() { Name = "Calendario de la cita" })).Not.ToBeVisibleAsync();
        await Assertions.Expect(p.Locator("[data-formulario-cita]")).ToBeVisibleAsync();
    });
}
