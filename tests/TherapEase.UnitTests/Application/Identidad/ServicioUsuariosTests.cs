using TherapEase.Application.Auditoria.Servicios;
using TherapEase.Application.Identidad.Constantes;
using TherapEase.Application.Identidad.Modelos;
using TherapEase.Application.Identidad.Servicios;
using TherapEase.Domain.Auditoria.Entidades.Enums;
using TherapEase.Domain.Compartido.Entidades.Enums;
using TherapEase.Domain.Identidad.Constantes;
using TherapEase.Domain.Identidad.Excepciones;
using TherapEase.UnitTests.Dobles;

namespace TherapEase.UnitTests.Application.Identidad;

public class ServicioUsuariosTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 10, 5, 15, 0, 0, TimeSpan.Zero);

    private readonly GestorIdentidadFalso _gestor = new();
    private readonly UnidadDeTrabajoFalsa _unidad = new();
    private readonly RepositorioAuditoriaFalso _repositorioAuditoria = new();
    private readonly AutorizacionFalsa _autorizacion = new();
    private readonly UsuarioActualFalso _actual = new();
    private readonly ServicioUsuarios _servicio;
    private readonly DatosUsuario _superusuario;

    public ServicioUsuariosTests()
    {
        _superusuario = _gestor.Agregar("super1", NombresDeRol.Superusuario);
        _actual.IdUsuario = _superusuario.IdUsuario;
        _autorizacion.Permisos.Add(Permiso.AdministrarUsuarios);
        var auditoria = new ServicioAuditoria(_repositorioAuditoria, _autorizacion, new RelojFijo(Ahora));
        _servicio = new ServicioUsuarios(_gestor, _unidad, auditoria, _autorizacion, _actual);
    }

    [Fact]
    public async Task CrearUsuario_ConSuperusuarioActivo_CreaConContrasenaTemporalQueDebeCambiarse()
    {
        var respuesta = await _servicio.CrearUsuarioAsync("usuaria1", NombresDeRol.Usuaria);

        Assert.True(respuesta.Exito);
        var creacion = Assert.Single(_gestor.Creaciones);
        Assert.True(creacion.DebeCambiar);
        Assert.Equal(GeneradorDeContrasenaTemporal.Longitud, creacion.Contrasena.Length);
        Assert.Equal(creacion.Contrasena, respuesta.Datos!.ContrasenaTemporal);
        Assert.True(respuesta.Datos.Usuario.DebeCambiarContrasena);
        Assert.Equal(1, _unidad.Confirmaciones);
    }

    [Fact]
    public async Task CrearUsuario_RegistraEventoConElActorQueLoCreo()
    {
        var respuesta = await _servicio.CrearUsuarioAsync("usuaria1", NombresDeRol.Usuaria);

        var evento = Assert.Single(_repositorioAuditoria.Eventos);
        Assert.Equal(_superusuario.IdUsuario, evento.IdUsuarioActor);
        Assert.Equal(respuesta.Datos!.Usuario.IdUsuario, evento.IdRegistro);
        Assert.Equal(TipoRegistroAuditoria.Usuario, evento.TipoRegistro);
        Assert.Equal(AccionAuditoria.Alta, evento.Accion);
        Assert.Equal(Ahora, evento.FechaEvento);
    }

    [Fact]
    public async Task CrearUsuario_ConNombreDuplicado_RechazaYNoRegistraEvento()
    {
        _gestor.Agregar("usuaria1", NombresDeRol.Usuaria);

        var respuesta = await _servicio.CrearUsuarioAsync("usuaria1", NombresDeRol.Usuaria);

        Assert.False(respuesta.Exito);
        Assert.Equal(MensajesDeIdentidad.NombreDeUsuarioDuplicado, respuesta.Mensaje);
        Assert.Empty(_repositorioAuditoria.Eventos);
    }

    [Theory]
    [InlineData("Capturista")]
    [InlineData("")]
    [InlineData("Administrador")]
    public async Task CrearUsuario_ConRolInexistenteONoPermitido_RechazaSinCrear(string rol)
    {
        var respuesta = await _servicio.CrearUsuarioAsync("usuaria1", rol);

        Assert.False(respuesta.Exito);
        Assert.Empty(_gestor.Creaciones);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CrearUsuario_SinNombre_Rechaza(string nombre)
    {
        var respuesta = await _servicio.CrearUsuarioAsync(nombre, NombresDeRol.Usuaria);

        Assert.False(respuesta.Exito);
        Assert.Empty(_gestor.Creaciones);
    }

    [Fact]
    public async Task CrearUsuario_SinPermisoDeAdministracion_RechazaSinTocarNada()
    {
        _autorizacion.Permisos.Clear();

        var respuesta = await _servicio.CrearUsuarioAsync("usuaria1", NombresDeRol.Usuaria);

        Assert.False(respuesta.Exito);
        Assert.Equal(MensajesDeIdentidad.SinPermiso, respuesta.Mensaje);
        Assert.Empty(_gestor.Creaciones);
        Assert.Equal(0, _unidad.Confirmaciones);
    }

    [Fact]
    public async Task CrearUsuario_SiLaAuditoriaFalla_NoConfirmaElCambio()
    {
        _repositorioAuditoria.ExcepcionAlAgregar = new InvalidOperationException("fallo simulado");

        await Assert.ThrowsAsync<InvalidOperationException>(() => _servicio.CrearUsuarioAsync("usuaria1", NombresDeRol.Usuaria));

        Assert.Equal(0, _unidad.Confirmaciones);
        Assert.Equal(1, _unidad.Reversiones);
    }

    [Fact]
    public async Task CambiarRol_DelUnicoSuperusuarioActivo_Rechaza()
    {
        var respuesta = await _servicio.CambiarRolAsync(_superusuario.IdUsuario, NombresDeRol.Usuaria);

        Assert.False(respuesta.Exito);
        Assert.Equal(MensajesDeIdentidad.UltimoSuperusuario, respuesta.Mensaje);
        Assert.Equal(NombresDeRol.Superusuario, _gestor.Usuarios[_superusuario.IdUsuario].Rol);
    }

    [Fact]
    public async Task CambiarRol_ConDosSuperusuariosActivos_PermiteDegradarUno()
    {
        var otro = _gestor.Agregar("super2", NombresDeRol.Superusuario);

        var respuesta = await _servicio.CambiarRolAsync(otro.IdUsuario, NombresDeRol.Usuaria);

        Assert.True(respuesta.Exito);
        Assert.Equal(NombresDeRol.Usuaria, _gestor.Usuarios[otro.IdUsuario].Rol);
        Assert.Equal(AccionAuditoria.Actualizacion, Assert.Single(_repositorioAuditoria.Eventos).Accion);
    }

    [Fact]
    public async Task CambiarRol_UnDesactivadoQueEraSuperusuario_NoCuentaParaLaRegla()
    {
        var inactivo = _gestor.Agregar("super2", NombresDeRol.Superusuario, activo: false);

        var respuesta = await _servicio.CambiarRolAsync(inactivo.IdUsuario, NombresDeRol.Usuaria);

        Assert.True(respuesta.Exito);
    }

    [Fact]
    public async Task CambiarRol_SiLaBaseDetectaQueDejariaSinSuperusuario_RechazaConMensajeClaro()
    {
        var otro = _gestor.Agregar("super2", NombresDeRol.Superusuario);
        _gestor.ExcepcionAlCambiarRol = new UltimoSuperusuarioException();

        var respuesta = await _servicio.CambiarRolAsync(otro.IdUsuario, NombresDeRol.Usuaria);

        Assert.False(respuesta.Exito);
        Assert.Equal(MensajesDeIdentidad.UltimoSuperusuario, respuesta.Mensaje);
        Assert.Empty(_repositorioAuditoria.Eventos);
    }

    [Fact]
    public async Task CambiarRol_ConRolInexistenteOMismoRol_RechazaSinCambios()
    {
        var usuaria = _gestor.Agregar("usuaria1", NombresDeRol.Usuaria);

        Assert.False((await _servicio.CambiarRolAsync(usuaria.IdUsuario, "Capturista")).Exito);
        Assert.False((await _servicio.CambiarRolAsync(usuaria.IdUsuario, NombresDeRol.Usuaria)).Exito);
        Assert.False((await _servicio.CambiarRolAsync(Guid.NewGuid(), NombresDeRol.Usuaria)).Exito);
        Assert.Empty(_repositorioAuditoria.Eventos);
    }

    [Fact]
    public async Task DesactivarUsuario_DelUnicoSuperusuarioActivo_Rechaza()
    {
        var respuesta = await _servicio.DesactivarUsuarioAsync(_superusuario.IdUsuario);

        Assert.False(respuesta.Exito);
        Assert.Equal(MensajesDeIdentidad.UltimoSuperusuario, respuesta.Mensaje);
        Assert.True(_gestor.Usuarios[_superusuario.IdUsuario].Activo);
    }

    [Fact]
    public async Task DesactivarUsuario_ConDosSuperusuariosActivos_PermiteDesactivarUno()
    {
        var otro = _gestor.Agregar("super2", NombresDeRol.Superusuario);

        var respuesta = await _servicio.DesactivarUsuarioAsync(otro.IdUsuario);

        Assert.True(respuesta.Exito);
        Assert.False(_gestor.Usuarios[otro.IdUsuario].Activo);
        Assert.Equal(AccionAuditoria.Desactivacion, Assert.Single(_repositorioAuditoria.Eventos).Accion);
    }

    [Fact]
    public async Task DesactivarUsuario_YaDesactivado_InformaSinDuplicarElEvento()
    {
        var inactivo = _gestor.Agregar("usuaria1", NombresDeRol.Usuaria, activo: false);

        var respuesta = await _servicio.DesactivarUsuarioAsync(inactivo.IdUsuario);

        Assert.False(respuesta.Exito);
        Assert.Empty(_repositorioAuditoria.Eventos);
    }

    [Fact]
    public async Task RestablecerContrasena_EntregaTemporalQueDebeCambiarseYAudita()
    {
        var usuaria = _gestor.Agregar("usuaria1", NombresDeRol.Usuaria);

        var respuesta = await _servicio.RestablecerContrasenaAsync(usuaria.IdUsuario);

        Assert.True(respuesta.Exito);
        var establecida = Assert.Single(_gestor.ContrasenasEstablecidas);
        Assert.True(establecida.DebeCambiar);
        Assert.Equal(respuesta.Datos!.ContrasenaTemporal, establecida.Contrasena);
        Assert.Equal(AccionAuditoria.ReinicioContrasena, Assert.Single(_repositorioAuditoria.Eventos).Accion);
    }

    [Fact]
    public async Task RestablecerContrasena_DeUnUsuarioInexistenteODesactivado_Rechaza()
    {
        var inactivo = _gestor.Agregar("usuaria1", NombresDeRol.Usuaria, activo: false);

        Assert.False((await _servicio.RestablecerContrasenaAsync(inactivo.IdUsuario)).Exito);
        Assert.False((await _servicio.RestablecerContrasenaAsync(Guid.NewGuid())).Exito);
        Assert.Empty(_gestor.ContrasenasEstablecidas);
    }

    [Fact]
    public async Task Eventos_DeUsuarioRolYContrasena_NoContienenContrasenaNiNombreDeUsuario()
    {
        var creada = await _servicio.CrearUsuarioAsync("usuaria_canario", NombresDeRol.Usuaria);
        var idCreada = creada.Datos!.Usuario.IdUsuario;
        var restablecida = await _servicio.RestablecerContrasenaAsync(idCreada);
        await _servicio.CambiarRolAsync(idCreada, NombresDeRol.Superusuario);

        var texto = string.Join('|', _repositorioAuditoria.Eventos.Select(e => $"{e.Hecho}|{string.Join(',', e.CamposAfectados)}"));
        Assert.Equal(3, _repositorioAuditoria.Eventos.Count);
        Assert.DoesNotContain(creada.Datos.ContrasenaTemporal, texto);
        Assert.DoesNotContain(restablecida.Datos!.ContrasenaTemporal, texto);
        Assert.DoesNotContain("usuaria_canario", texto);
    }
}
