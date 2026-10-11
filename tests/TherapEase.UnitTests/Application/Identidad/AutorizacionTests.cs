using TherapEase.Application.Identidad.Servicios;
using TherapEase.Domain.Compartido.Entidades.Enums;
using TherapEase.Domain.Identidad.Constantes;
using TherapEase.UnitTests.Dobles;

namespace TherapEase.UnitTests.Application.Identidad;

public class AutorizacionTests
{
    private readonly GestorIdentidadFalso _gestor = new();
    private readonly UsuarioActualFalso _usuarioActual = new();

    private Autorizacion Crear() => new(_usuarioActual, _gestor);

    [Theory]
    [InlineData(Permiso.AdministrarUsuarios)]
    [InlineData(Permiso.ConsultarAuditoria)]
    public async Task Superusuario_ActivoSinContrasenaTemporal_TienePermiso(Permiso permiso)
    {
        var usuario = _gestor.Agregar("super", NombresDeRol.Superusuario);
        _usuarioActual.IdUsuario = usuario.IdUsuario;

        Assert.True(await Crear().TienePermisoAsync(permiso));
    }

    [Fact]
    public async Task Superusuario_ConContrasenaTemporalPendiente_NoTienePermiso()
    {
        var usuario = _gestor.Agregar("super", NombresDeRol.Superusuario, debeCambiar: true);
        _usuarioActual.IdUsuario = usuario.IdUsuario;

        Assert.False(await Crear().TienePermisoAsync(Permiso.AdministrarUsuarios));
    }

    [Fact]
    public async Task Superusuario_Desactivado_NoTienePermiso()
    {
        var usuario = _gestor.Agregar("super", NombresDeRol.Superusuario, activo: false);
        _usuarioActual.IdUsuario = usuario.IdUsuario;

        Assert.False(await Crear().TienePermisoAsync(Permiso.ConsultarAuditoria));
    }

    [Theory]
    [InlineData(Permiso.AdministrarUsuarios)]
    [InlineData(Permiso.ConsultarAuditoria)]
    public async Task Usuaria_NoTienePermiso(Permiso permiso)
    {
        var usuario = _gestor.Agregar("usuaria", NombresDeRol.Usuaria);
        _usuarioActual.IdUsuario = usuario.IdUsuario;

        Assert.False(await Crear().TienePermisoAsync(permiso));
    }

    [Fact]
    public async Task RolDesconocido_NoTienePermiso()
    {
        var usuario = _gestor.Agregar("capturista", "Capturista");
        _usuarioActual.IdUsuario = usuario.IdUsuario;

        Assert.False(await Crear().TienePermisoAsync(Permiso.AdministrarUsuarios));
    }

    [Fact]
    public async Task SinSesion_NoTienePermiso()
    {
        _gestor.Agregar("super", NombresDeRol.Superusuario);

        Assert.False(await Crear().TienePermisoAsync(Permiso.AdministrarUsuarios));
    }

    [Fact]
    public async Task UsuarioQueYaNoExiste_NoTienePermiso()
    {
        _usuarioActual.IdUsuario = Guid.NewGuid();

        Assert.False(await Crear().TienePermisoAsync(Permiso.AdministrarUsuarios));
    }

    [Fact]
    public async Task UnCambioDeRolSeNotaEnLaSiguienteConsulta()
    {
        var usuario = _gestor.Agregar("super", NombresDeRol.Superusuario);
        _usuarioActual.IdUsuario = usuario.IdUsuario;
        var autorizacion = Crear();
        Assert.True(await autorizacion.TienePermisoAsync(Permiso.ConsultarAuditoria));

        _gestor.Usuarios[usuario.IdUsuario] = usuario with { Rol = NombresDeRol.Usuaria };

        Assert.False(await autorizacion.TienePermisoAsync(Permiso.ConsultarAuditoria));
    }
}
