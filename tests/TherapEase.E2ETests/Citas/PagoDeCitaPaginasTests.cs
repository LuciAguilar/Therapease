using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using TherapEase.Domain.Citas.Entidades.Enums;

namespace TherapEase.E2ETests;

public sealed partial class AccesoSesionesYPermisosTests
{
    private async Task<(Guid Id, string Fecha)> AbrirCitaParaPago(IPage pagina, string origen)
    {
        var paciente = await PrepararPaciente("Pago ficticio " + Guid.NewGuid().ToString("N"));
        var fecha = FechaDeAgenda();
        await Abrir(pagina, "/Agenda/Crear?paciente=" + paciente);
        var formulario = pagina.Locator("[data-formulario-cita]");
        var id = await CapturarReserva(formulario, fecha);
        _paso = "alta por URL antes de corregir pago";
        await formulario.GetByRole(AriaRole.Button, new() { Name = "Agendar cita", Exact = true }).ClickAsync();
        await Assertions.Expect(pagina.Locator(".cita-pago .pago-estado")).ToHaveTextAsync("Pendiente");
        if (origen == "detalle") await Abrir(pagina, "/Agenda/Detalle/" + id);
        if (origen == "panel")
        {
            await Abrir(pagina, "/Agenda?Desde=" + fecha + "&Hasta=" + fecha);
            await pagina.Locator("a.agenda-cita").ClickAsync();
            await Assertions.Expect(pagina.Locator("#panel-detalle-cita")).ToBeVisibleAsync();
        }
        await pagina.Locator(".pago-editor summary").ClickAsync();
        return (id, fecha);
    }

    [Theory]
    [InlineData("alta", "Usuaria")]
    [InlineData("detalle", "Usuaria")]
    [InlineData("panel", "Usuaria")]
    [InlineData("alta", "Superusuario")]
    [InlineData("detalle", "Superusuario")]
    [InlineData("panel", "Superusuario")]
    public Task CambioDePagoConfirmaYRecargaDesdeCadaDetalle(string origen, string rol) => Verificar(async () =>
    {
        var p = await Pagina();
        await Entrar(p, await entorno.CrearCuentaAsync(rol));
        var (id, _) = await AbrirCitaParaPago(p, origen);
        var formulario = p.Locator("[data-formulario-pago]");
        var envios = new List<bool>();
        p.Request += (_, solicitud) =>
        {
            if (solicitud.Method == "POST" && new Uri(solicitud.Url).AbsolutePath == "/Agenda/Pago/" + id)
                envios.Add(solicitud.IsNavigationRequest);
        };
        await formulario.GetByText("Pago recibido", new() { Exact = true }).ClickAsync();
        _paso = "corregir pago desde " + origen;
        await formulario.GetByRole(AriaRole.Button, new() { Name = "Guardar cambio", Exact = true }).ClickAsync();
        await Assertions.Expect(p.Locator(".cita-pago .pago-estado")).ToHaveTextAsync("Pagado");
        await Assertions.Expect(p.Locator(".aviso-confirmacion:visible strong")).ToHaveTextAsync("Estado de pago actualizado.");
        Exigir(envios.Count == 1 && envios[0] == (origen == "alta"));
        Exigir(await p.Locator("[name='VersionEsperada']").InputValueAsync() == "2");
        if (origen == "alta") Exigir(Ruta(p) == "/Agenda/Detalle/" + id);
        if (origen == "panel") await Assertions.Expect(p.Locator("#panel-detalle-cita")).ToBeVisibleAsync();
        if (origen == "detalle") await Assertions.Expect(p.Locator("h1")).ToBeFocusedAsync();
        if (origen == "panel") await Assertions.Expect(p.Locator("#titulo-detalle-cita")).ToBeFocusedAsync();
        // Una consulta posterior conserva el valor sin repetir el POST.
        await Abrir(p, "/Agenda/Detalle/" + id);
        await Assertions.Expect(p.Locator(".cita-pago .pago-estado")).ToHaveTextAsync("Pagado");
        Exigir(envios.Count == 1);
        await using var db = entorno.Servidor.Escenario.Contexto();
        var cita = await db.Citas.AsNoTracking().SingleAsync(c => c.IdCita == id);
        Exigir(cita.EstadoPago == EstadoPago.Pagado && cita.Version == 2);
    });

    [Theory]
    [InlineData("alta")]
    [InlineData("detalle")]
    [InlineData("panel")]
    public Task ConflictoDePagoExigeRecargarSinReenvioNiCambioDeVersion(string origen) => Verificar(async () =>
    {
        var p = await Pagina();
        await Entrar(p, await entorno.CrearCuentaAsync());
        var (id, _) = await AbrirCitaParaPago(p, origen);
        // Una edición competidora ya confirmada deja obsoleta la versión del formulario visible.
        Exigir((await Enviar(p, "/Agenda/Pago/" + id, new()
        {
            ["VersionEsperada"] = "1", ["NuevoPago"] = "Pagado"
        })).Status == 302);
        var envios = 0;
        p.Request += (_, solicitud) =>
        {
            if (solicitud.Method == "POST" && new Uri(solicitud.Url).AbsolutePath == "/Agenda/Pago/" + id) envios++;
        };
        var formulario = p.Locator("[data-formulario-pago]");
        Exigir(await formulario.Locator("[name='VersionEsperada']").InputValueAsync() == "1");
        await formulario.GetByText("Aún por pagar", new() { Exact = true }).ClickAsync();
        _paso = "rechazo por conflicto desde " + origen;
        await formulario.GetByRole(AriaRole.Button, new() { Name = "Guardar cambio", Exact = true }).ClickAsync();
        await Assertions.Expect(p.Locator("[data-contenido-detalle-cita]")).ToContainTextAsync("Selección pendiente de guardar: Pendiente");
        await Assertions.Expect(p.Locator("[data-contenido-detalle-cita] [role='alert']")).Not.ToBeEmptyAsync();
        Exigir(await p.Locator("[data-formulario-pago]").CountAsync() == 0 && envios == 1);
        if (origen != "alta") await Assertions.Expect(p.Locator("[role='alert']")).ToBeFocusedAsync();
        await p.Locator("[data-recargar-pago]").ClickAsync();
        await Assertions.Expect(p.Locator(".pago-estado")).ToHaveTextAsync("Pagado");
        Exigir(await p.Locator("[name='VersionEsperada']").InputValueAsync() == "2" && envios == 1);
        await using var db = entorno.Servidor.Escenario.Contexto();
        var cita = await db.Citas.AsNoTracking().SingleAsync(c => c.IdCita == id);
        Exigir(cita.EstadoPago == EstadoPago.Pagado && cita.Version == 2);
    });

    [Fact]
    public Task PagoSinJavaScriptRedirigeYElAvisoSeOcultaConCss() => Verificar(async () =>
    {
        var contexto = await entorno.Navegador.NewContextAsync(new() { IgnoreHTTPSErrors = true, JavaScriptEnabled = false });
        _contextos.Add(contexto);
        var p = await contexto.NewPageAsync();
        await Entrar(p, await entorno.CrearCuentaAsync());
        var (id, _) = await AbrirCitaParaPago(p, "alta");
        var formulario = p.Locator("[data-formulario-pago]");
        await formulario.GetByText("Pago recibido", new() { Exact = true }).ClickAsync();
        await formulario.GetByRole(AriaRole.Button, new() { Name = "Guardar cambio", Exact = true }).ClickAsync();
        await Assertions.Expect(p.Locator(".pago-estado")).ToHaveTextAsync("Pagado");
        Exigir(Ruta(p) == "/Agenda/Detalle/" + id);
        var aviso = p.Locator(".aviso-confirmacion");
        await Assertions.Expect(aviso).ToBeVisibleAsync();
        await Assertions.Expect(aviso).ToContainTextAsync("Estado de pago actualizado.");
        await Assertions.Expect(aviso).ToBeHiddenAsync(new() { Timeout = 8500 });
    });
}
