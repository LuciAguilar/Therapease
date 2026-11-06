using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TherapEase.Application.Compartido.Modelos;
using TherapEase.Application.Identidad.Interfaces.Servicios;
using TherapEase.Application.Identidad.Modelos;
using TherapEase.Web.Pages.Usuarios;

namespace TherapEase.UnitTests.Web;

public class CambiosDeCuentaTests
{
    private static readonly DatosUsuario Cuenta = new(Guid.NewGuid(), "prueba", "Usuaria", true, false);

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task EditarEnviaNombreYCorreoVacioYReflejaElResultado(bool exito)
    {
        var servicio = new Usuarios(exito);
        var pagina = CrearPagina(servicio);

        var resultado = await pagina.OnPostDatosAsync(Cuenta.IdUsuario, "Nombre corregido", null);

        Assert.Equal((Cuenta.IdUsuario, "Nombre corregido", (string?)null), servicio.Edicion!.Value);
        ComprobarResultado(pagina, resultado, exito);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task CambiarRolRequiereConfirmacionYReflejaElResultado(bool confirmar, bool exito)
    {
        var servicio = new Usuarios(exito);
        var pagina = CrearPagina(servicio);

        var resultado = await pagina.OnPostRolAsync(Cuenta.IdUsuario, "Superusuario", confirmar);

        Assert.Equal(confirmar ? (Cuenta.IdUsuario, "Superusuario") : ((Guid, string)?)null, servicio.Rol);
        ComprobarResultado(pagina, resultado, exito, confirmar ? "Cambio rechazado." : "Confirma el cambio de rol.");
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task DesactivarRequiereConfirmacionYReflejaElResultado(bool confirmar, bool exito)
    {
        var servicio = new Usuarios(exito);
        var pagina = CrearPagina(servicio);

        var resultado = await pagina.OnPostDesactivarAsync(Cuenta.IdUsuario, confirmar);

        Assert.Equal(confirmar ? Cuenta.IdUsuario : (Guid?)null, servicio.Desactivado);
        ComprobarResultado(pagina, resultado, exito, confirmar ? "Cambio rechazado." : "Confirma la desactivación de la cuenta.");
    }

    private static DetalleModel CrearPagina(Usuarios servicio) => new(servicio)
    {
        PageContext = new PageContext { HttpContext = new DefaultHttpContext() }
    };

    private static void ComprobarResultado(DetalleModel pagina, IActionResult resultado, bool exito, string error = "Cambio rechazado.")
    {
        if (exito)
        {
            var redireccion = Assert.IsType<RedirectToPageResult>(resultado);
            Assert.Equal(Cuenta.IdUsuario, Assert.IsType<Guid>(redireccion.RouteValues!["id"]));
            Assert.Equal("Cambio confirmado.", pagina.Confirmacion);
            Assert.Null(pagina.Error);
        }
        else
        {
            Assert.IsType<PageResult>(resultado);
            Assert.Equal(error, pagina.Error);
            Assert.Null(pagina.Confirmacion);
        }
    }

    private sealed class Usuarios(bool exito) : IServicioUsuarios
    {
        public (Guid, string, string?)? Edicion { get; private set; }
        public (Guid, string)? Rol { get; private set; }
        public Guid? Desactivado { get; private set; }

        public Task<RespuestaServicio<FichaDeUsuario>> ObtenerUsuarioAsync(Guid id, CancellationToken cancelacion = default)
            => Task.FromResult(RespuestaServicio<FichaDeUsuario>.Correcta(new FichaDeUsuario(Cuenta, null)));

        public Task<RespuestaServicio<FichaDeUsuario>> ActualizarDatosDeCuentaAsync(
            Guid id, string nombreCompleto, string? correo, CancellationToken cancelacion = default)
        {
            Edicion = (id, nombreCompleto, correo);
            return Responder(new FichaDeUsuario(Cuenta with { NombreCompleto = nombreCompleto }, correo));
        }

        public Task<RespuestaServicio<DatosUsuario>> CambiarRolAsync(Guid id, string rol, CancellationToken cancelacion = default)
        {
            Rol = (id, rol);
            return Responder(Cuenta with { Rol = rol });
        }

        public Task<RespuestaServicio<DatosUsuario>> DesactivarUsuarioAsync(Guid id, CancellationToken cancelacion = default)
        {
            Desactivado = id;
            return Responder(Cuenta with { Activo = false });
        }

        public Task<RespuestaServicio<IReadOnlyList<DatosUsuario>>> ListarUsuariosAsync(
            string? busqueda = null, CancellationToken cancelacion = default) => throw new NotSupportedException();

        public Task<RespuestaServicio<ContrasenaTemporalEntregada>> CrearUsuarioAsync(
            string nombreUsuario, string rol, string nombreCompleto, string? correo = null,
            CancellationToken cancelacion = default) => throw new NotSupportedException();

        public Task<RespuestaServicio<ContrasenaTemporalEntregada>> RestablecerContrasenaAsync(
            Guid id, CancellationToken cancelacion = default) => throw new NotSupportedException();

        private Task<RespuestaServicio<T>> Responder<T>(T datos) => Task.FromResult(exito
            ? RespuestaServicio<T>.Correcta(datos, "Cambio confirmado.")
            : RespuestaServicio<T>.Fallida("Cambio rechazado."));
    }
}
