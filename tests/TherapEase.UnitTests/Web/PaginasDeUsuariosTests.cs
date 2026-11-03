using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TherapEase.Application.Compartido.Modelos;
using TherapEase.Application.Identidad.Interfaces.Servicios;
using TherapEase.Application.Identidad.Modelos;
using TherapEase.Web.Pages.Usuarios;

namespace TherapEase.UnitTests.Web;

public class PaginasDeUsuariosTests
{
    private static readonly DatosUsuario Cuenta = new(Guid.NewGuid(), "prueba", "Usuaria", true, true);
    private static readonly ContrasenaTemporalEntregada Temporal = new(Cuenta, "temporal ficticia");

    [Fact]
    public async Task BusquedaLimitaEntradaYRepresentaListaVacia()
    {
        var servicio = new Usuarios();
        var pagina = new IndexModel(servicio) { PageContext = Contexto() };

        Assert.IsType<PageResult>(await pagina.OnGetAsync(new string('a', 101)));

        Assert.Equal(new string('a', 100), servicio.Busqueda);
        Assert.Empty(pagina.Cuentas);
    }

    [Fact]
    public async Task AltaInvalidaNoLlamaAlServicio()
    {
        var servicio = new Usuarios();
        var pagina = new CrearModel(servicio) { PageContext = Contexto() };
        pagina.ModelState.AddModelError("Entrada.NombreUsuario", "Campo obligatorio.");

        Assert.IsType<PageResult>(await pagina.OnPostAsync());
        Assert.Equal(0, servicio.Altas);
        Assert.Null(pagina.Entrega);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AltaSoloEntregaTemporalCuandoElServicioConfirma(bool exito)
    {
        var servicio = new Usuarios { Entrega = exito
            ? RespuestaServicio<ContrasenaTemporalEntregada>.Correcta(Temporal)
            : RespuestaServicio<ContrasenaTemporalEntregada>.Fallida("Alta rechazada.") };
        var pagina = new CrearModel(servicio)
        {
            PageContext = Contexto(),
            Entrada = new() { NombreUsuario = "prueba", NombreCompleto = "Cuenta de prueba" }
        };

        Assert.IsType<PageResult>(await pagina.OnPostAsync());
        Assert.Equal(1, servicio.Altas);
        Assert.Equal(exito ? Temporal : null, pagina.Entrega);
        Assert.Equal(exito ? null : "Alta rechazada.", pagina.Error);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RestablecerExigeConfirmacionYEntregaTemporalEnLaRespuesta(bool confirmar)
    {
        var servicio = new Usuarios();
        var pagina = new DetalleModel(servicio) { PageContext = Contexto() };

        Assert.IsType<PageResult>(await pagina.OnPostRestablecerAsync(Cuenta.IdUsuario, confirmar));

        Assert.Equal(confirmar ? Cuenta.IdUsuario : (Guid?)null, servicio.Restablecido);
        Assert.Equal(confirmar ? Temporal : null, pagina.Entrega);
        Assert.Null(pagina.Confirmacion);
        Assert.Equal(!confirmar, pagina.Error is not null);
    }

    [Fact]
    public async Task FichaInexistenteDevuelveAvisoYEstado404()
    {
        var pagina = new DetalleModel(new Usuarios()) { PageContext = Contexto() };

        Assert.IsType<PageResult>(await pagina.OnGetAsync(Guid.NewGuid()));
        Assert.Equal(404, pagina.Response.StatusCode);
        Assert.Null(pagina.Ficha);
        Assert.Equal("Cuenta inexistente.", pagina.Error);
    }

    private static PageContext Contexto() => new() { HttpContext = new DefaultHttpContext() };

    private sealed class Usuarios : IServicioUsuarios
    {
        public string? Busqueda { get; private set; }
        public int Altas { get; private set; }
        public Guid? Restablecido { get; private set; }
        public RespuestaServicio<ContrasenaTemporalEntregada> Entrega { get; init; } =
            RespuestaServicio<ContrasenaTemporalEntregada>.Correcta(Temporal);

        public Task<RespuestaServicio<IReadOnlyList<DatosUsuario>>> ListarUsuariosAsync(
            string? busqueda = null, CancellationToken cancelacion = default)
        {
            Busqueda = busqueda;
            return Task.FromResult(RespuestaServicio<IReadOnlyList<DatosUsuario>>.Correcta([]));
        }

        public Task<RespuestaServicio<FichaDeUsuario>> ObtenerUsuarioAsync(Guid id, CancellationToken cancelacion = default) =>
            Task.FromResult(id == Cuenta.IdUsuario
                ? RespuestaServicio<FichaDeUsuario>.Correcta(new FichaDeUsuario(Cuenta, null))
                : RespuestaServicio<FichaDeUsuario>.Fallida("Cuenta inexistente."));

        public Task<RespuestaServicio<ContrasenaTemporalEntregada>> CrearUsuarioAsync(
            string nombre, string rol, string nombreCompleto, string? correo = null, CancellationToken cancelacion = default)
        {
            Altas++;
            return Task.FromResult(Entrega);
        }

        public Task<RespuestaServicio<ContrasenaTemporalEntregada>> RestablecerContrasenaAsync(Guid id, CancellationToken cancelacion = default)
        {
            Restablecido = id;
            return Task.FromResult(Entrega);
        }

        public Task<RespuestaServicio<FichaDeUsuario>> ActualizarDatosDeCuentaAsync(
            Guid id, string nombreCompleto, string? correo, CancellationToken cancelacion = default) => throw new NotSupportedException();

        public Task<RespuestaServicio<DatosUsuario>> CambiarRolAsync(
            Guid id, string rol, CancellationToken cancelacion = default) => throw new NotSupportedException();

        public Task<RespuestaServicio<DatosUsuario>> DesactivarUsuarioAsync(
            Guid id, CancellationToken cancelacion = default) => throw new NotSupportedException();
    }
}
