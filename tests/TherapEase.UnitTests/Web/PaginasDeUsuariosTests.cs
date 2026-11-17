using System.ComponentModel.DataAnnotations;
using TherapEase.Application.Identidad.Constantes;
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

    [Theory]
    [InlineData(101)]
    [InlineData(150)]
    public async Task BusquedaConservaEntradaYRepresentaListaVacia(int longitud)
    {
        var servicio = new Usuarios();
        var pagina = new IndexModel(servicio) { PageContext = Contexto() };

        Assert.IsType<PageResult>(await pagina.OnGetAsync(new string('a', longitud)));

        Assert.Equal(new string('a', longitud), servicio.Busqueda);
        Assert.Empty(pagina.Cuentas);
    }

    [Fact]
    public async Task AltaInvalidaNoLlamaAlServicio()
    {
        var servicio = new Usuarios();
        var pagina = new CrearModel(servicio) { PageContext = Contexto() };
        var errores = new List<ValidationResult>();
        Assert.False(Validator.TryValidateObject(pagina.Entrada, new ValidationContext(pagina.Entrada), errores, true));
        foreach (var error in errores)
            foreach (var miembro in error.MemberNames)
                pagina.ModelState.AddModelError("Entrada." + miembro, error.ErrorMessage!);

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
            Entrada = new() { NombreUsuario = "prueba", NombreCompleto = "Cuenta de prueba", Correo = "cuenta@example.test" }
        };

        Assert.IsType<PageResult>(await pagina.OnPostAsync());
        Assert.Equal(1, servicio.Altas);
        Assert.Equal(("prueba", "Usuaria", "Cuenta de prueba", "cuenta@example.test"), servicio.ArgumentosAlta);
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

    [Theory]
    [InlineData(MensajesDeIdentidad.SinPermiso, true)]
    [InlineData(MensajesDeIdentidad.BusquedaDeUsuariosInvalida, false)]
    public async Task BusquedaDistinguePermisoDeValidacion(string mensaje, bool sinPermiso)
    {
        var servicio = new Usuarios { ErrorLista = mensaje };
        var pagina = new IndexModel(servicio) { PageContext = Contexto() };
        var resultado = await pagina.OnGetAsync(new string('a', 151));
        Assert.Equal(151, servicio.Busqueda!.Length);
        if (sinPermiso) Assert.IsType<ForbidResult>(resultado);
        else { Assert.IsType<PageResult>(resultado); Assert.Equal(mensaje, pagina.Error); }
    }

    [Fact]
    public async Task ConsultaDeFichaCorrectaMuestraDatos()
    {
        var pagina = new DetalleModel(new Usuarios()) { PageContext = Contexto() };
        Assert.IsType<PageResult>(await pagina.OnGetAsync(Cuenta.IdUsuario));
        Assert.Equal(Cuenta, pagina.Ficha!.Datos);
        Assert.Null(pagina.Error);
    }

    [Fact]
    public async Task RestablecimientoFallidoNoEntregaTemporal()
    {
        var pagina = new DetalleModel(new Usuarios { Entrega = RespuestaServicio<ContrasenaTemporalEntregada>.Fallida("No se restableció.") }) { PageContext = Contexto() };
        Assert.IsType<PageResult>(await pagina.OnPostRestablecerAsync(Cuenta.IdUsuario, true));
        Assert.Equal("No se restableció.", pagina.Error);
        Assert.Null(pagina.Entrega);
        Assert.Null(pagina.Confirmacion);
    }

    [Theory]
    [InlineData(51, 2, 0)]
    [InlineData(1, 1, 0)]
    [InlineData(1, 151, 0)]
    [InlineData(1, 2, 255)]
    public void AltaValidaLasLongitudesReales(int usuario, int nombre, int correo)
    {
        var entrada = new CrearModel.DatosDeAlta { NombreUsuario = new string('u', usuario), NombreCompleto = new string('n', nombre), Correo = new string('c', correo) };
        Assert.False(Validator.TryValidateObject(entrada, new ValidationContext(entrada), new List<ValidationResult>(), true));
    }

    private static PageContext Contexto() => new() { HttpContext = new DefaultHttpContext() };

    private sealed class Usuarios : IServicioUsuarios
    {
        public string? ErrorLista { get; init; }
        public (string, string, string, string?) ArgumentosAlta { get; private set; }
        public string? Busqueda { get; private set; }
        public int Altas { get; private set; }
        public Guid? Restablecido { get; private set; }
        public RespuestaServicio<ContrasenaTemporalEntregada> Entrega { get; init; } =
            RespuestaServicio<ContrasenaTemporalEntregada>.Correcta(Temporal);

        public Task<RespuestaServicio<IReadOnlyList<DatosUsuario>>> ListarUsuariosAsync(
            string? busqueda = null, CancellationToken cancelacion = default)
        {
            Busqueda = busqueda;
            return Task.FromResult(ErrorLista is null ? RespuestaServicio<IReadOnlyList<DatosUsuario>>.Correcta([]) : RespuestaServicio<IReadOnlyList<DatosUsuario>>.Fallida(ErrorLista));
        }

        public Task<RespuestaServicio<FichaDeUsuario>> ObtenerUsuarioAsync(Guid id, CancellationToken cancelacion = default) =>
            Task.FromResult(id == Cuenta.IdUsuario
                ? RespuestaServicio<FichaDeUsuario>.Correcta(new FichaDeUsuario(Cuenta, null))
                : RespuestaServicio<FichaDeUsuario>.Fallida("Cuenta inexistente."));

        public Task<RespuestaServicio<ContrasenaTemporalEntregada>> CrearUsuarioAsync(
            string nombre, string rol, string nombreCompleto, string? correo = null, CancellationToken cancelacion = default)
        {
            Altas++;
            ArgumentosAlta = (nombre, rol, nombreCompleto, correo);
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
