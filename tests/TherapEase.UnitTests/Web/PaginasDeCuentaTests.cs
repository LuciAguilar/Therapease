using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TherapEase.Application.Compartido.Modelos;
using TherapEase.Application.Identidad.Interfaces.Servicios;
using TherapEase.Application.Identidad.Modelos;
using TherapEase.Web.Pages.Cuenta;
using TherapEase.Web.Pages.Usuarios;
using TherapEase.Web.Seguridad;
using AccesoModel = TherapEase.Web.Pages.Login.IndexModel;

namespace TherapEase.UnitTests.Web;

public class PaginasDeCuentaTests
{
    private const string Nueva = "clave de prueba larga";
    private static readonly DatosUsuario Cuenta = new(Guid.NewGuid(), "prueba", "Usuaria", true, false);

    [Theory]
    [InlineData(null, Nueva)]
    [InlineData("corta", "corta")]
    [InlineData(Nueva, "otra confirmación")]
    public async Task CambioInvalidoNoSeEnviaAlServicio(string? nueva, string confirmacion)
    {
        var servicio = new Contrasenas();
        var pagina = CrearCambio(servicio, true);
        pagina.Entrada.ContrasenaNueva = nueva;
        pagina.Entrada.Confirmacion = confirmacion;

        Assert.IsType<PageResult>(await pagina.OnPostAsync());
        Assert.NotNull(pagina.MensajeError);
        Assert.Null(pagina.MensajeExito);
        Assert.Null(pagina.Entrada.ContrasenaNueva);
        Assert.Equal(0, servicio.Llamadas);
    }

    [Fact]
    public async Task CambioOrdinarioSinActualNoSeEnviaAlServicio()
    {
        var servicio = new Contrasenas();
        var pagina = CrearCambio(servicio, false);
        pagina.Entrada.ContrasenaActual = null;

        Assert.IsType<PageResult>(await pagina.OnPostAsync());
        Assert.Equal(0, servicio.Llamadas);
    }

    [Theory]
    [InlineData(true, null, "/Index")]
    [InlineData(false, "actual de prueba", null)]
    public async Task CambioConfirmadoUsaElModoDelServidor(bool temporal, string? actual, string? destino)
    {
        var servicio = new Contrasenas();
        var pagina = CrearCambio(servicio, temporal);

        var resultado = Assert.IsType<RedirectToPageResult>(await pagina.OnPostAsync());

        Assert.Equal(destino, resultado.PageName);
        Assert.Equal(actual, servicio.Actual);
        Assert.Equal(Nueva, servicio.Nueva);
        Assert.Equal(1, servicio.Llamadas);
        Assert.NotNull(pagina.MensajeExito);
    }

    [Fact]
    public async Task RechazoDelCambioSeMuestraSinConfirmarExito()
    {
        var servicio = new Contrasenas { Resultado = RespuestaServicio<DatosUsuario>.Fallida("Cambio rechazado.") };
        var pagina = CrearCambio(servicio, false);

        Assert.IsType<PageResult>(await pagina.OnPostAsync());
        Assert.Equal("Cambio rechazado.", pagina.MensajeError);
        Assert.Null(pagina.MensajeExito);
        Assert.Null(pagina.Entrada.ContrasenaActual);
    }

    [Fact]
    public async Task FalloInesperadoSePropagaSinReintentoNiConfirmacion()
    {
        var servicio = new Contrasenas { Fallo = new InvalidOperationException() };
        var pagina = CrearCambio(servicio, true);

        await Assert.ThrowsAsync<InvalidOperationException>(() => pagina.OnPostAsync());

        Assert.Equal(1, servicio.Llamadas);
        Assert.Null(pagina.MensajeExito);
    }

    [Theory]
    [InlineData("Cuenta inexistente")]
    [InlineData("Contraseña incorrecta")]
    [InlineData("Cuenta bloqueada tras el quinto fallo")]
    public async Task AccesoRechazadoSiempreMuestraElMismoMensaje(string motivo)
    {
        var pagina = new AccesoModel(new Acceso(RespuestaServicio<DatosUsuario>.Fallida(motivo)))
        {
            PageContext = Contexto(),
            Entrada = new() { NombreUsuario = "prueba", Contrasena = "valor ficticio" }
        };

        Assert.IsType<PageResult>(await pagina.OnPostAsync());
        Assert.Equal("Usuario o contraseña incorrectos.", pagina.MensajeError);
        Assert.Null(pagina.Entrada.Contrasena);
    }

    [Fact]
    public async Task AccesoConTemporalPriorizaCambiarContrasena()
    {
        var pagina = new AccesoModel(new Acceso(RespuestaServicio<DatosUsuario>.Correcta(
            Cuenta with { DebeCambiarContrasena = true })))
        {
            PageContext = Contexto(),
            ReturnUrl = "/Usuarios"
        };

        var resultado = Assert.IsType<RedirectToPageResult>(await pagina.OnPostAsync());
        Assert.Equal("/Cuenta/Contrasena", resultado.PageName);
    }

    [Fact]
    public async Task SalirCierraSesionSoloEnPost()
    {
        var acceso = new Acceso(RespuestaServicio<DatosUsuario>.Correcta(Cuenta));
        var pagina = new SalirModel(acceso) { PageContext = Contexto() };

        Assert.Equal(405, Assert.IsType<StatusCodeResult>(pagina.OnGet()).StatusCode);
        Assert.Equal(0, acceso.Salidas);
        Assert.Equal("/Login/Index", Assert.IsType<RedirectToPageResult>(await pagina.OnPostAsync()).PageName);
        Assert.Equal(1, acceso.Salidas);
    }

    [Theory]
    [InlineData(typeof(CrearModel), PoliticasDeAutorizacion.AdministrarUsuarios)]
    [InlineData(typeof(DetalleModel), PoliticasDeAutorizacion.AdministrarUsuarios)]
    [InlineData(typeof(IndexModel), PoliticasDeAutorizacion.AdministrarUsuarios)]
    [InlineData(typeof(SalirModel), PoliticasDeAutorizacion.CambiarContrasenaPropia)]
    [InlineData(typeof(ContrasenaModel), PoliticasDeAutorizacion.CambiarContrasenaPropia)]
    public void CadaPaginaDeclaraLaPoliticaDeSuOperacion(Type pagina, string politica)
    {
        Assert.Equal(politica, Assert.Single(pagina.GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>()).Policy);
    }

    private static PageContext Contexto() => new() { HttpContext = new DefaultHttpContext() };

    private static ContrasenaModel CrearCambio(Contrasenas servicio, bool temporal) =>
        new(servicio, new CuentaPropia(temporal))
        {
            PageContext = Contexto(),
            Entrada = new() { ContrasenaActual = "actual de prueba", ContrasenaNueva = Nueva, Confirmacion = Nueva }
        };

    private sealed class CuentaPropia(bool temporal) : IServicioCuentaPropia
    {
        public Task<RespuestaServicio<FichaDeUsuario>> ObtenerMisDatosAsync(CancellationToken cancelacion = default)
            => Task.FromResult(RespuestaServicio<FichaDeUsuario>.Correcta(
                new FichaDeUsuario(Cuenta with { DebeCambiarContrasena = temporal }, null)));
    }

    private sealed class Contrasenas : IServicioContrasena
    {
        public RespuestaServicio<DatosUsuario> Resultado { get; init; } = RespuestaServicio<DatosUsuario>.Correcta(Cuenta);
        public Exception? Fallo { get; init; }
        public string? Actual { get; private set; }
        public string? Nueva { get; private set; }
        public int Llamadas { get; private set; }

        public Task<RespuestaServicio<DatosUsuario>> CambiarContrasenaPropiaAsync(
            string? actual, string nueva, CancellationToken cancelacion = default)
        {
            Llamadas++;
            Actual = actual;
            Nueva = nueva;
            return Fallo is null ? Task.FromResult(Resultado) : Task.FromException<RespuestaServicio<DatosUsuario>>(Fallo);
        }
    }

    private sealed class Acceso(RespuestaServicio<DatosUsuario> resultado) : IServicioAcceso
    {
        public int Salidas { get; private set; }

        public Task<RespuestaServicio<DatosUsuario>> IniciarSesionAsync(
            string nombreUsuario, string contrasena, CancellationToken cancelacion = default) => Task.FromResult(resultado);

        public Task CerrarSesionAsync(CancellationToken cancelacion = default)
        {
            Salidas++;
            return Task.CompletedTask;
        }
    }
}
