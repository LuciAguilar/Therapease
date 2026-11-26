using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using TherapEase.Domain.Citas.Entidades.Enums;

namespace TherapEase.E2ETests;

/// <summary>
/// Comprueba las confirmaciones compartidas y el recorrido completo de paciente, cita y pago.
/// Solo publica resultados seguros de prueba, sin trazas ni capturas con identidad.
/// </summary>
public sealed partial class AccesoSesionesYPermisosTests
{


    // ──── CONFIRMACIONES EN LAS PANTALLAS AFECTADAS ─────────────────────


    /// Cada confirmación conserva seis segundos, pausa al enfocar y cierre tras reanudar.
    [Theory]
    [InlineData("cita")]
    [InlineData("agenda")]
    [InlineData("pago")]
    [InlineData("sin-cambios")]
    [InlineData("paciente")]
    [InlineData("usuario")]
    [InlineData("contrasena")]
    [InlineData("temporal")]
    public Task PagoAvisosConservanPausaYDuracionEnCadaPantalla(string origen) => Verificar(async () =>
    {
        // Arrange
        var p = await Pagina();
        await MostrarAvisoRevision(p, origen);
        var aviso = p.Locator(".aviso-confirmacion:visible");
        await Assertions.Expect(aviso).ToHaveAttributeAsync("popover", "manual");
        var barra = aviso.Locator(".notificacion-tiempo");
        var cerrar = aviso.GetByRole(AriaRole.Button, new() { Name = "Cerrar notificación" });
        // Act
        await p.Mouse.MoveAsync(1, 1);
        await cerrar.FocusAsync();
        // Assert
        Exigir(await barra.EvaluateAsync<bool>("e => e.getAnimations().some(a => a.playState === 'paused' && a.effect.getTiming().duration === 6000)"));
        var caja = await aviso.BoundingBoxAsync();
        Exigir(caja is { X: >= 0, Y: >= 0 } && await cerrar.IsVisibleAsync());
        // Act: se controla el reloj de la animación para comprobar su último tramo, sin dormir seis segundos.
        await barra.EvaluateAsync("e => e.getAnimations().forEach(a => a.currentTime = 5500)");
        await cerrar.EvaluateAsync("e => e.blur()");
        await p.Mouse.MoveAsync(1, 1);
        // Assert
        await Assertions.Expect(aviso).ToBeHiddenAsync(new() { Timeout = 4000 });
    });

    /// El botón cierra inmediatamente y no deja una confirmación encima del resto del formulario.
    [Fact]
    public Task PagoAvisosPermitenCierreManual() => Verificar(async () =>
    {
        // Arrange
        var p = await Pagina();
        await MostrarAvisoRevision(p, "pago");
        var aviso = p.Locator(".aviso-confirmacion:visible");
        // Act
        await aviso.GetByRole(AriaRole.Button, new() { Name = "Cerrar notificación" }).ClickAsync();
        // Assert
        await Assertions.Expect(p.Locator(".aviso-confirmacion:visible")).ToHaveCountAsync(0);
    });

    /// Prepara una confirmación con operaciones reales, sin fabricar HTML ni cambiar el producto.
    private async Task MostrarAvisoRevision(IPage p, string origen)
    {
        if (origen is "contrasena" or "temporal")
        {
            var cuenta = await entorno.CrearCuentaAsync(temporal: origen == "temporal");
            await Entrar(p, cuenta);
            await Cambiar(p, origen == "temporal" ? null : cuenta.Clave, "Ficticia-" + Guid.NewGuid().ToString("N"));
            return;
        }
        if (origen == "usuario")
        {
            var cuenta = await entorno.CrearCuentaAsync("Superusuario");
            await Entrar(p, cuenta);
            await Abrir(p, "/Usuarios/Detalle/" + cuenta.Id);
            Exigir((await Enviar(p, "/Usuarios/Detalle/" + cuenta.Id + "?handler=Datos",
                new() { ["nombreCompleto"] = "Persona Ficticia Revisada" })).Status == 302);
            await Abrir(p, "/Usuarios/Detalle/" + cuenta.Id);
            return;
        }
        if (origen == "paciente")
        {
            await Entrar(p, await entorno.CrearCuentaAsync());
            await RegistrarPacientePorPaginaRevision(p);
            return;
        }
        if (origen == "cita")
        {
            await Entrar(p, await entorno.CrearCuentaAsync());
            var paciente = await PrepararPaciente("Aviso de cita ficticia " + Guid.NewGuid().ToString("N"));
            await Abrir(p, "/Agenda/Crear?paciente=" + paciente);
            var formulario = p.Locator("[data-formulario-cita]");
            await CapturarReserva(formulario, FechaDeAgenda());
            await formulario.GetByRole(AriaRole.Button, new() { Name = "Agendar cita", Exact = true }).ClickAsync();
            await Assertions.Expect(p.Locator(".pago-estado")).ToHaveTextAsync("Pendiente");
            return;
        }
        if (origen == "agenda")
        {
            await Entrar(p, await entorno.CrearCuentaAsync());
            var nombre = "Aviso de agenda ficticia " + Guid.NewGuid().ToString("N");
            await PrepararPaciente(nombre);
            var fecha = FechaDeAgenda();
            await Abrir(p, "/Agenda?Desde=" + fecha + "&Hasta=" + fecha);
            await p.Locator("a.agenda-nueva").First.ClickAsync();
            var panel = p.Locator("#panel-nueva-cita");
            await Assertions.Expect(panel).ToBeVisibleAsync();
            await panel.Locator("[name='Texto']").FillAsync(nombre);
            await panel.GetByRole(AriaRole.Button, new() { NameRegex = new("^" + nombre) }).ClickAsync();
            var formulario = panel.Locator("[data-formulario-cita]");
            var id = await CapturarReserva(formulario, fecha);
            await formulario.GetByRole(AriaRole.Button, new() { Name = "Agendar cita", Exact = true }).ClickAsync();
            await Assertions.Expect(panel).Not.ToBeVisibleAsync();
            await AvisoPagoRevision(p, "Cita agendada.");
            Exigir(Ruta(p) == "/Agenda");
            await ComprobarPagoRevision(id, EstadoPago.Pendiente, 1, 0);
            return;
        }
        var cita = await PrepararPagoRevision(p);
        Exigir((await Enviar(p, "/Agenda/Pago/" + cita.Id, DatosPagoRevision(1, origen == "sin-cambios" ? "Pendiente" : "Pagado"))).Status == 302);
        await Abrir(p, "/Agenda/Detalle/" + cita.Id);
    }


    // ──── RECORRIDO COMPLETO CON AMBOS ROLES ────────────────────────────


    /// Entra, registra paciente, agenda, paga, recarga y sale mediante las páginas reales.
    [Theory]
    [InlineData("Usuaria")]
    [InlineData("Superusuario")]
    public Task RecorridoPacienteCitaYPagoTerminaEnSalida(string rol) => Verificar(async () =>
    {
        // Arrange
        var p = await Pagina();
        var cuenta = await entorno.CrearCuentaAsync(rol);
        // Act: acceso y paciente por interfaz, sin preparar el paciente mediante servicios.
        await Entrar(p, cuenta);
        var paciente = await RegistrarPacientePorPaginaRevision(p);
        await Assertions.Expect(p.Locator(".aviso-confirmacion:visible")).ToContainTextAsync("Paciente registrado.");
        await p.Locator(".notificacion-cerrar:visible").ClickAsync();
        await Abrir(p, "/Agenda/Crear?paciente=" + paciente);
        var fecha = FechaDeAgenda();
        var formulario = p.Locator("[data-formulario-cita]");
        var id = await CapturarReserva(formulario, fecha);
        await formulario.GetByRole(AriaRole.Button, new() { Name = "Agendar cita", Exact = true }).ClickAsync();
        // Assert
        await Assertions.Expect(p.Locator(".pago-estado")).ToHaveTextAsync("Pendiente");
        await AvisoPagoRevision(p, "Cita agendada.");
        await ComprobarPagoRevision(id, EstadoPago.Pendiente, 1, 0);
        // Act: pagar desde el resultado del alta usa la navegación normal corregida.
        await p.Locator(".notificacion-cerrar:visible").ClickAsync();
        await p.Locator(".pago-editor summary").ClickAsync();
        await p.Locator("[data-formulario-pago]").GetByText("Pago recibido", new() { Exact = true }).ClickAsync();
        await p.Locator("[data-formulario-pago] button[type='submit']").ClickAsync();
        // Assert
        await AvisoPagoRevision(p, "Estado de pago actualizado.");
        await ComprobarPagoRevision(id, EstadoPago.Pagado, 2, 1, cuenta.Id);
        // Act
        await Abrir(p, "/Agenda/Detalle/" + id);
        // Assert
        await Assertions.Expect(p.Locator(".pago-estado")).ToHaveTextAsync("Pagado");
        Exigir(await p.Locator("[name='VersionEsperada']").InputValueAsync() == "2");
        if (rol == "Superusuario")
        {
            var evento = await Auditoria(p, id);
            var texto = evento.GetRawText();
            Exigir(texto.Contains("EstadoPago") && texto.Contains("Se cambió el estado de pago de una cita.") && !texto.Contains("\"Pagado\""));
            await Abrir(p, "/Agenda/Detalle/" + id);
        }
        else Exigir((await p.APIRequest.GetAsync(entorno.Url + "/api/auditoria/eventos")).Status == 403);
        // Act
        await p.GetByRole(AriaRole.Button, new() { Name = "Salir", Exact = true }).ClickAsync();
        await Assertions.Expect(p.Locator("[name='Entrada.NombreUsuario']")).ToBeVisibleAsync();
        var posterior = await p.APIRequest.GetAsync(entorno.Url + "/Agenda/Detalle/" + id, new() { MaxRedirects = 0 });
        // Assert
        Exigir(DenegadoPagoRevision(posterior));
        await ComprobarPagoRevision(id, EstadoPago.Pagado, 2, 1, cuenta.Id);
    });

    /// Completa un alta de paciente adulta desde su formulario y conserva el identificador mostrado.
    private async Task<Guid> RegistrarPacientePorPaginaRevision(IPage p)
    {
        await Abrir(p, "/Pacientes/Crear");
        var id = Guid.Parse(await p.Locator("[name='IdSolicitud']").InputValueAsync());
        await p.GetByLabel("Nombre completo", new() { Exact = true }).FillAsync("Paciente Ficticia QA " + Guid.NewGuid().ToString("N"));
        await p.GetByLabel("Fecha de nacimiento", new() { Exact = true }).FillAsync("1990-05-08");
        await p.GetByLabel("Escolar", new() { Exact = true }).CheckAsync();
        await Task.WhenAll(p.WaitForURLAsync(u => new Uri(u).AbsolutePath == "/Pacientes"), Boton(p, "Registrar paciente").ClickAsync());
        await Assertions.Expect(p.Locator(".aviso-confirmacion:visible")).ToContainTextAsync("Paciente registrado.");
        return id;
    }
}
