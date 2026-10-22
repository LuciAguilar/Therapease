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

    private const string NombreCompletoValido = "Usuaria Ficticia Pérez";

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
        var respuesta = await _servicio.CrearUsuarioAsync("usuaria1", NombresDeRol.Usuaria, NombreCompletoValido);

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
        var respuesta = await _servicio.CrearUsuarioAsync("usuaria1", NombresDeRol.Usuaria, NombreCompletoValido);

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

        var respuesta = await _servicio.CrearUsuarioAsync("usuaria1", NombresDeRol.Usuaria, NombreCompletoValido);

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
        var respuesta = await _servicio.CrearUsuarioAsync("usuaria1", rol, NombreCompletoValido);

        Assert.False(respuesta.Exito);
        Assert.Empty(_gestor.Creaciones);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CrearUsuario_SinNombre_Rechaza(string nombre)
    {
        var respuesta = await _servicio.CrearUsuarioAsync(nombre, NombresDeRol.Usuaria, NombreCompletoValido);

        Assert.False(respuesta.Exito);
        Assert.Empty(_gestor.Creaciones);
    }

    [Fact]
    public async Task CrearUsuario_SinPermisoDeAdministracion_RechazaSinTocarNada()
    {
        _autorizacion.Permisos.Clear();

        var respuesta = await _servicio.CrearUsuarioAsync("usuaria1", NombresDeRol.Usuaria, NombreCompletoValido);

        Assert.False(respuesta.Exito);
        Assert.Equal(MensajesDeIdentidad.SinPermiso, respuesta.Mensaje);
        Assert.Empty(_gestor.Creaciones);
        Assert.Equal(0, _unidad.Confirmaciones);
    }

    [Fact]
    public async Task CrearUsuario_SiLaAuditoriaFalla_NoConfirmaElCambio()
    {
        _repositorioAuditoria.ExcepcionAlAgregar = new InvalidOperationException("fallo simulado");

        await Assert.ThrowsAsync<InvalidOperationException>(() => _servicio.CrearUsuarioAsync("usuaria1", NombresDeRol.Usuaria, NombreCompletoValido));

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
        var creada = await _servicio.CrearUsuarioAsync("usuaria_canario", NombresDeRol.Usuaria, NombreCompletoValido);
        var idCreada = creada.Datos!.Usuario.IdUsuario;
        var restablecida = await _servicio.RestablecerContrasenaAsync(idCreada);
        await _servicio.CambiarRolAsync(idCreada, NombresDeRol.Superusuario);

        var texto = string.Join('|', _repositorioAuditoria.Eventos.Select(e => $"{e.Hecho}|{string.Join(',', e.CamposAfectados)}"));
        Assert.Equal(3, _repositorioAuditoria.Eventos.Count);
        Assert.DoesNotContain(creada.Datos.ContrasenaTemporal, texto);
        Assert.DoesNotContain(restablecida.Datos!.ContrasenaTemporal, texto);
        Assert.DoesNotContain("usuaria_canario", texto);
    }

    // ──── Lista y ficha de usuarios ────

    private ServicioUsuarios CrearServicioConAutorizacionReal()
    {
        var autorizacion = new Autorizacion(_actual, _gestor);
        var auditoria = new ServicioAuditoria(_repositorioAuditoria, autorizacion, new RelojFijo(Ahora));
        return new ServicioUsuarios(_gestor, _unidad, auditoria, autorizacion, _actual);
    }

    private void AssertSinEscrituras()
    {
        Assert.Empty(_repositorioAuditoria.Eventos);
        Assert.Equal(0, _unidad.Confirmaciones);
        Assert.Empty(_gestor.Creaciones);
        Assert.Empty(_gestor.ContrasenasEstablecidas);
        Assert.Empty(_gestor.SesionesRevocadas);
    }

    [Fact]
    public async Task ListarUsuarios_ConSuperusuarioActivo_DevuelveTodasLasCuentasOrdenadasPorNombreIncluidasLasDesactivadas()
    {
        _gestor.Agregar("zoe", NombresDeRol.Usuaria);
        _gestor.Agregar("Ana", NombresDeRol.Usuaria, activo: false);
        _gestor.Agregar("mariana", NombresDeRol.Usuaria, debeCambiar: true);

        var respuesta = await _servicio.ListarUsuariosAsync();

        Assert.True(respuesta.Exito);
        var usuarios = respuesta.Datos!;
        Assert.Equal(["Ana", "mariana", "super1", "zoe"], usuarios.Select(u => u.NombreUsuario));
        Assert.False(usuarios.Single(u => u.NombreUsuario == "Ana").Activo);
        Assert.True(usuarios.Single(u => u.NombreUsuario == "mariana").DebeCambiarContrasena);
        Assert.Equal(NombresDeRol.Superusuario, usuarios.Single(u => u.NombreUsuario == "super1").Rol);
    }

    [Fact]
    public async Task ListarUsuarios_ConTexto_FiltraPorNombreSinDistinguirMayusculas()
    {
        _gestor.Agregar("mariana.ficticia", NombresDeRol.Usuaria);
        _gestor.Agregar("MARIO.ficticio", NombresDeRol.Usuaria);
        _gestor.Agregar("luis.ficticio", NombresDeRol.Usuaria);

        var respuesta = await _servicio.ListarUsuariosAsync("mar");

        Assert.True(respuesta.Exito);
        Assert.Equal(["mariana.ficticia", "MARIO.ficticio"], respuesta.Datos!.Select(u => u.NombreUsuario));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ListarUsuarios_SinTextoOSoloEspacios_NoFiltra(string? busqueda)
    {
        _gestor.Agregar("usuaria1", NombresDeRol.Usuaria);

        var respuesta = await _servicio.ListarUsuariosAsync(busqueda);

        Assert.True(respuesta.Exito);
        Assert.Equal(2, respuesta.Datos!.Count);
        Assert.Null(Assert.Single(_gestor.Listados).Busqueda);
    }

    [Fact]
    public async Task ListarUsuarios_QuitaLosEspaciosExterioresDelTexto()
    {
        _gestor.Agregar("usuaria1", NombresDeRol.Usuaria);

        var respuesta = await _servicio.ListarUsuariosAsync("  usuaria  ");

        Assert.Equal("usuaria", Assert.Single(_gestor.Listados).Busqueda);
        Assert.Equal("usuaria1", Assert.Single(respuesta.Datos!).NombreUsuario);
    }

    [Fact]
    public async Task ListarUsuarios_ConMasDeCienCuentas_DevuelveSoloCien()
    {
        for (var i = 0; i < 120; i++)
        {
            _gestor.Agregar($"cuenta{i:000}", NombresDeRol.Usuaria);
        }

        var respuesta = await _servicio.ListarUsuariosAsync();

        Assert.Equal(100, LimitesDeIdentidad.MaximoDeUsuariosListados);
        Assert.Equal(100, Assert.Single(_gestor.Listados).Maximo);
        Assert.Equal(100, respuesta.Datos!.Count);
    }

    [Fact]
    public async Task ListarUsuarios_ConTextoSinCoincidencias_DevuelveListaVaciaCorrecta()
    {
        var respuesta = await _servicio.ListarUsuariosAsync("nadie");

        Assert.True(respuesta.Exito);
        Assert.Empty(respuesta.Datos!);
    }

    [Fact]
    public async Task ListarUsuarios_ConTextoDeLongitudMaximaSeAcepta_YConUnCaracterMasSeRechaza()
    {
        var limite = new string('a', LimitesDeIdentidad.LongitudMaximaBusquedaDeUsuarios);

        var aceptada = await _servicio.ListarUsuariosAsync(limite);
        var rechazada = await _servicio.ListarUsuariosAsync(limite + "a");

        Assert.True(aceptada.Exito);
        Assert.False(rechazada.Exito);
        Assert.Equal(MensajesDeIdentidad.BusquedaDeUsuariosInvalida, rechazada.Mensaje);
        Assert.Null(rechazada.Datos);
        Assert.Single(_gestor.Listados);
    }

    [Fact]
    public async Task ListarUsuarios_SinPermisoDeAdministracion_RechazaSinConsultar()
    {
        _autorizacion.Permisos.Clear();

        var respuesta = await _servicio.ListarUsuariosAsync();

        Assert.False(respuesta.Exito);
        Assert.Equal(MensajesDeIdentidad.SinPermiso, respuesta.Mensaje);
        Assert.Null(respuesta.Datos);
        Assert.Empty(_gestor.Listados);
    }

    [Fact]
    public async Task ListarUsuarios_NoEscribeNiRegistraEventos()
    {
        _gestor.Agregar("usuaria1", NombresDeRol.Usuaria);

        await _servicio.ListarUsuariosAsync();
        await _servicio.ListarUsuariosAsync("usuaria");

        AssertSinEscrituras();
    }

    // Cuenta con permiso por rol pero sin sesión válida, desactivada o con la contraseña temporal pendiente: la autorización real rechaza ambas consultas.
    [Theory]
    [InlineData("usuaria", false, false)]
    [InlineData("desactivado", true, false)]
    [InlineData("conTemporal", false, true)]
    [InlineData("sinSesion", false, false)]
    public async Task Consultas_SinSesionValidaOConCuentaSinPermisoVigente_SeRechazanConLaAutorizacionReal(string caso, bool desactivado, bool temporalPendiente)
    {
        var rol = caso == "usuaria" ? NombresDeRol.Usuaria : NombresDeRol.Superusuario;
        var cuenta = _gestor.Agregar($"cuenta-{caso}", rol, activo: !desactivado, debeCambiar: temporalPendiente);
        _actual.IdUsuario = caso == "sinSesion" ? null : cuenta.IdUsuario;
        var servicio = CrearServicioConAutorizacionReal();

        var lista = await servicio.ListarUsuariosAsync();
        var ficha = await servicio.ObtenerUsuarioAsync(_superusuario.IdUsuario);

        Assert.False(lista.Exito);
        Assert.Equal(MensajesDeIdentidad.SinPermiso, lista.Mensaje);
        Assert.False(ficha.Exito);
        Assert.Equal(MensajesDeIdentidad.SinPermiso, ficha.Mensaje);
        Assert.Null(ficha.Datos);
        Assert.Empty(_gestor.Listados);
    }

    [Fact]
    public async Task Consultas_ConSuperusuarioActivoYSinTemporalPendiente_FuncionanConLaAutorizacionReal()
    {
        _actual.IdUsuario = _superusuario.IdUsuario;
        var servicio = CrearServicioConAutorizacionReal();

        var lista = await servicio.ListarUsuariosAsync();
        var ficha = await servicio.ObtenerUsuarioAsync(_superusuario.IdUsuario);

        Assert.True(lista.Exito);
        Assert.True(ficha.Exito);
    }

    [Fact]
    public async Task ObtenerUsuario_ConSuperusuarioActivo_DevuelveLosDatosYElCorreo()
    {
        var cuenta = _gestor.Agregar("usuaria1", NombresDeRol.Usuaria, debeCambiar: true);
        _gestor.Correos[cuenta.IdUsuario] = "usuaria1@ficticio.mx";

        var respuesta = await _servicio.ObtenerUsuarioAsync(cuenta.IdUsuario);

        Assert.True(respuesta.Exito);
        Assert.Equal(cuenta, respuesta.Datos!.Datos);
        Assert.Equal("usuaria1@ficticio.mx", respuesta.Datos.Correo);
    }

    [Fact]
    public async Task ObtenerUsuario_DeUnaCuentaDesactivadaSinCorreo_LaDevuelveConCorreoNulo()
    {
        var cuenta = _gestor.Agregar("usuaria1", NombresDeRol.Usuaria, activo: false);

        var respuesta = await _servicio.ObtenerUsuarioAsync(cuenta.IdUsuario);

        Assert.True(respuesta.Exito);
        Assert.False(respuesta.Datos!.Datos.Activo);
        Assert.Null(respuesta.Datos.Correo);
    }

    [Fact]
    public async Task ObtenerUsuario_DeUnaCuentaInexistente_RespondeNoEncontradoSinOtrosDatos()
    {
        var respuesta = await _servicio.ObtenerUsuarioAsync(Guid.NewGuid());

        Assert.False(respuesta.Exito);
        Assert.Equal(MensajesDeIdentidad.UsuarioNoEncontrado, respuesta.Mensaje);
        Assert.Null(respuesta.Datos);
    }

    [Fact]
    public async Task ObtenerUsuario_SinPermisoDeAdministracion_RespondeIgualParaCuentaExistenteQueInexistente()
    {
        var existente = _gestor.Agregar("usuaria1", NombresDeRol.Usuaria);
        _autorizacion.Permisos.Clear();

        var deExistente = await _servicio.ObtenerUsuarioAsync(existente.IdUsuario);
        var deInexistente = await _servicio.ObtenerUsuarioAsync(Guid.NewGuid());

        Assert.False(deExistente.Exito);
        Assert.False(deInexistente.Exito);
        Assert.Equal(MensajesDeIdentidad.SinPermiso, deExistente.Mensaje);
        Assert.Equal(deExistente.Mensaje, deInexistente.Mensaje);
        Assert.Null(deExistente.Datos);
    }

    [Fact]
    public async Task ObtenerUsuario_NoEscribeNiRegistraEventos()
    {
        var cuenta = _gestor.Agregar("usuaria1", NombresDeRol.Usuaria);

        await _servicio.ObtenerUsuarioAsync(cuenta.IdUsuario);

        AssertSinEscrituras();
    }

    [Fact]
    public void LosModelosDeConsulta_SoloExponenLosCamposPrevistos_SinHashSelloNiContadores()
    {
        static string[] Propiedades(Type tipo) => tipo.GetProperties().Select(p => p.Name).Order().ToArray();

        Assert.Equal(["Activo", "DebeCambiarContrasena", "IdUsuario", "NombreCompleto", "NombreUsuario", "Rol"], Propiedades(typeof(DatosUsuario)));
        Assert.Equal(["Correo", "Datos"], Propiedades(typeof(FichaDeUsuario)));
    }

    // ──── Nombre completo y correo al crear ────

    [Fact]
    public async Task CrearUsuario_ConNombreCompletoYCorreo_LosGuardaRecortadosYElCorreoEnMinusculas()
    {
        var respuesta = await _servicio.CrearUsuarioAsync("usuaria1", NombresDeRol.Usuaria, "  Ana Ficticia  ", "  ANA@Ficticio.MX ");

        Assert.True(respuesta.Exito);
        var creacion = Assert.Single(_gestor.Creaciones);
        Assert.Equal("Ana Ficticia", creacion.NombreCompleto);
        Assert.Equal("ana@ficticio.mx", creacion.Correo);
        Assert.Equal("Ana Ficticia", respuesta.Datos!.Usuario.NombreCompleto);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CrearUsuario_SinCorreo_NoGuardaNingunCorreo(string? correo)
    {
        var respuesta = await _servicio.CrearUsuarioAsync("usuaria1", NombresDeRol.Usuaria, NombreCompletoValido, correo);

        Assert.True(respuesta.Exito);
        Assert.Null(Assert.Single(_gestor.Creaciones).Correo);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("A")]
    [InlineData("Ana\tFicticia")]
    public async Task CrearUsuario_ConNombreCompletoInvalido_RechazaSinCrearNiRegistrarEvento(string? nombreCompleto)
    {
        var respuesta = await _servicio.CrearUsuarioAsync("usuaria1", NombresDeRol.Usuaria, nombreCompleto!);

        Assert.False(respuesta.Exito);
        Assert.Equal(MensajesDeIdentidad.NombreCompletoInvalido, respuesta.Mensaje);
        Assert.Null(respuesta.Datos);
        AssertSinEscrituras();
    }

    [Fact]
    public async Task CrearUsuario_ConNombreCompletoDeMasDeCientoCincuentaCaracteres_Rechaza()
    {
        var respuesta = await _servicio.CrearUsuarioAsync("usuaria1", NombresDeRol.Usuaria, new string('a', 151));

        Assert.False(respuesta.Exito);
        Assert.Equal(MensajesDeIdentidad.NombreCompletoInvalido, respuesta.Mensaje);
        AssertSinEscrituras();
    }

    [Theory]
    [InlineData("erika@ficticio")]
    [InlineData("erika@.com")]
    [InlineData("erika@ficticio..mx")]
    [InlineData("erika")]
    [InlineData("eri ka@ficticio.mx")]
    public async Task CrearUsuario_ConCorreoInvalido_RechazaSinCrearNiRegistrarEvento(string correo)
    {
        var respuesta = await _servicio.CrearUsuarioAsync("usuaria1", NombresDeRol.Usuaria, NombreCompletoValido, correo);

        Assert.False(respuesta.Exito);
        Assert.Equal(MensajesDeIdentidad.CorreoInvalido, respuesta.Mensaje);
        Assert.Null(respuesta.Datos);
        AssertSinEscrituras();
    }

    [Fact]
    public async Task CrearUsuario_ConNombreDeUsuarioYNombreCompletoInvalidos_InformaPrimeroDelNombreDeUsuario()
    {
        var respuesta = await _servicio.CrearUsuarioAsync("", NombresDeRol.Usuaria, "A");

        Assert.Equal(MensajesDeIdentidad.DatosDeUsuarioInvalidos, respuesta.Mensaje);
        AssertSinEscrituras();
    }

    [Fact]
    public async Task CrearUsuario_ElEventoDeAltaListaElNombreCompletoYElCorreoSoloSiSeCapturo()
    {
        await _servicio.CrearUsuarioAsync("usuaria1", NombresDeRol.Usuaria, NombreCompletoValido);
        await _servicio.CrearUsuarioAsync("usuaria2", NombresDeRol.Usuaria, NombreCompletoValido, "usuaria2@ficticio.mx");

        Assert.Equal(2, _repositorioAuditoria.Eventos.Count);
        Assert.Equal(["NombreUsuario", "Rol", "DebeCambiarContrasena", "NombreCompleto"], _repositorioAuditoria.Eventos[0].CamposAfectados);
        Assert.Equal(["NombreUsuario", "Rol", "DebeCambiarContrasena", "NombreCompleto", "Correo"], _repositorioAuditoria.Eventos[1].CamposAfectados);
        Assert.All(_repositorioAuditoria.Eventos, e => Assert.Equal(AccionAuditoria.Alta, e.Accion));
    }

    [Fact]
    public async Task CrearUsuario_ElEventoDeAltaNoContieneNombreCompletoNiCorreo()
    {
        await _servicio.CrearUsuarioAsync("usuaria1", NombresDeRol.Usuaria, "Canario Nombre Ficticio", "canario@ficticio.mx");

        var evento = Assert.Single(_repositorioAuditoria.Eventos);
        var texto = $"{evento.Hecho}|{string.Join(',', evento.CamposAfectados)}";
        Assert.DoesNotContain("Canario", texto);
        Assert.DoesNotContain("canario@ficticio.mx", texto);
    }

    // ──── Actualizar nombre completo y correo ────

    private DatosUsuario CrearCuentaConDatos(string nombreCompleto = "Ana Ficticia", string? correo = "ana@ficticio.mx", bool activo = true)
    {
        var cuenta = _gestor.Agregar("usuaria1", NombresDeRol.Usuaria, activo: activo, nombreCompleto: nombreCompleto);
        if (correo is not null)
        {
            _gestor.Correos[cuenta.IdUsuario] = correo;
        }

        return cuenta;
    }

    [Fact]
    public async Task ActualizarDatosDeCuenta_ConNombreYCorreoNuevos_GuardaYDevuelveLaFichaActualizada()
    {
        var cuenta = CrearCuentaConDatos();

        var respuesta = await _servicio.ActualizarDatosDeCuentaAsync(cuenta.IdUsuario, "  Ana María Ficticia  ", "  ANA.MARIA@Ficticio.MX ");

        Assert.True(respuesta.Exito);
        Assert.Equal("Datos actualizados.", respuesta.Mensaje);
        Assert.Equal("Ana María Ficticia", respuesta.Datos!.Datos.NombreCompleto);
        Assert.Equal("ana.maria@ficticio.mx", respuesta.Datos.Correo);
        Assert.Equal("Ana María Ficticia", _gestor.Usuarios[cuenta.IdUsuario].NombreCompleto);
        Assert.Equal("ana.maria@ficticio.mx", _gestor.Correos[cuenta.IdUsuario]);
        Assert.Equal(1, _unidad.Confirmaciones);
    }

    [Theory]
    [InlineData("Ana María Ficticia", "ana@ficticio.mx", new[] { "NombreCompleto" })]
    [InlineData("Ana Ficticia", "otra@ficticio.mx", new[] { "Correo" })]
    [InlineData("Ana María Ficticia", "otra@ficticio.mx", new[] { "NombreCompleto", "Correo" })]
    [InlineData("Ana Ficticia", null, new[] { "Correo" })]
    public async Task ActualizarDatosDeCuenta_RegistraUnEventoSoloConLosCamposQueCambiaron(string nombreNuevo, string? correoNuevo, string[] camposEsperados)
    {
        var cuenta = CrearCuentaConDatos();

        await _servicio.ActualizarDatosDeCuentaAsync(cuenta.IdUsuario, nombreNuevo, correoNuevo);

        var evento = Assert.Single(_repositorioAuditoria.Eventos);
        Assert.Equal(_superusuario.IdUsuario, evento.IdUsuarioActor);
        Assert.Equal(cuenta.IdUsuario, evento.IdRegistro);
        Assert.Equal(TipoRegistroAuditoria.Usuario, evento.TipoRegistro);
        Assert.Equal(AccionAuditoria.Actualizacion, evento.Accion);
        Assert.Equal(camposEsperados, evento.CamposAfectados);
        Assert.Equal(Ahora, evento.FechaEvento);
    }

    [Fact]
    public async Task ActualizarDatosDeCuenta_SinCambios_NoRegistraEventoNiEscribeNada()
    {
        var cuenta = CrearCuentaConDatos();

        var respuesta = await _servicio.ActualizarDatosDeCuentaAsync(cuenta.IdUsuario, "Ana Ficticia", "ana@ficticio.mx");

        Assert.True(respuesta.Exito);
        Assert.Equal("No hubo cambios.", respuesta.Mensaje);
        Assert.Empty(_repositorioAuditoria.Eventos);
    }

    [Fact]
    public async Task ActualizarDatosDeCuenta_ConElMismoCorreoEscritoConOtrasMayusculasYEspacios_NoLoTomaComoCambio()
    {
        var cuenta = CrearCuentaConDatos();

        var respuesta = await _servicio.ActualizarDatosDeCuentaAsync(cuenta.IdUsuario, "  Ana Ficticia ", "  ANA@Ficticio.MX ");

        Assert.Equal("No hubo cambios.", respuesta.Mensaje);
        Assert.Empty(_repositorioAuditoria.Eventos);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ActualizarDatosDeCuenta_SinCorreo_QuitaElCorreoGuardado(string? correoNuevo)
    {
        var cuenta = CrearCuentaConDatos();

        var respuesta = await _servicio.ActualizarDatosDeCuentaAsync(cuenta.IdUsuario, "Ana Ficticia", correoNuevo);

        Assert.True(respuesta.Exito);
        Assert.Null(respuesta.Datos!.Correo);
        Assert.False(_gestor.Correos.ContainsKey(cuenta.IdUsuario));
        Assert.Equal(["Correo"], Assert.Single(_repositorioAuditoria.Eventos).CamposAfectados);
    }

    [Fact]
    public async Task ActualizarDatosDeCuenta_CompletaElNombreDeUnaCuentaQueNoLoTenia()
    {
        var cuenta = CrearCuentaConDatos(nombreCompleto: "", correo: null);

        var respuesta = await _servicio.ActualizarDatosDeCuentaAsync(cuenta.IdUsuario, "Ana Ficticia", null);

        Assert.True(respuesta.Exito);
        Assert.Equal(["NombreCompleto"], Assert.Single(_repositorioAuditoria.Eventos).CamposAfectados);
    }

    [Fact]
    public async Task ActualizarDatosDeCuenta_NoCierraSesionesNiTocaElResto()
    {
        var cuenta = CrearCuentaConDatos();

        await _servicio.ActualizarDatosDeCuentaAsync(cuenta.IdUsuario, "Ana María Ficticia", "nuevo@ficticio.mx");

        Assert.Empty(_gestor.SesionesRevocadas);
        Assert.Empty(_gestor.ContrasenasEstablecidas);
        var despues = _gestor.Usuarios[cuenta.IdUsuario];
        Assert.Equal(cuenta.Rol, despues.Rol);
        Assert.Equal(cuenta.Activo, despues.Activo);
        Assert.Equal(cuenta.DebeCambiarContrasena, despues.DebeCambiarContrasena);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("A")]
    public async Task ActualizarDatosDeCuenta_ConNombreInvalido_RechazaSinTocarNada(string? nombreNuevo)
    {
        var cuenta = CrearCuentaConDatos();

        var respuesta = await _servicio.ActualizarDatosDeCuentaAsync(cuenta.IdUsuario, nombreNuevo!, "ana@ficticio.mx");

        Assert.False(respuesta.Exito);
        Assert.Equal(MensajesDeIdentidad.NombreCompletoInvalido, respuesta.Mensaje);
        Assert.Empty(_gestor.ActualizacionesDeDatos);
        AssertSinEscrituras();
    }

    [Fact]
    public async Task ActualizarDatosDeCuenta_ConCorreoInvalido_RechazaSinTocarNada()
    {
        var cuenta = CrearCuentaConDatos();

        var respuesta = await _servicio.ActualizarDatosDeCuentaAsync(cuenta.IdUsuario, "Ana Ficticia", "ana@ficticio");

        Assert.False(respuesta.Exito);
        Assert.Equal(MensajesDeIdentidad.CorreoInvalido, respuesta.Mensaje);
        Assert.Empty(_gestor.ActualizacionesDeDatos);
        Assert.Equal("ana@ficticio.mx", _gestor.Correos[cuenta.IdUsuario]);
        AssertSinEscrituras();
    }

    [Fact]
    public async Task ActualizarDatosDeCuenta_SinPermisoDeAdministracion_RechazaSinTocarNada()
    {
        var cuenta = CrearCuentaConDatos();
        _autorizacion.Permisos.Clear();

        var respuesta = await _servicio.ActualizarDatosDeCuentaAsync(cuenta.IdUsuario, "Otro Nombre", "otro@ficticio.mx");

        Assert.False(respuesta.Exito);
        Assert.Equal(MensajesDeIdentidad.SinPermiso, respuesta.Mensaje);
        Assert.Empty(_gestor.ActualizacionesDeDatos);
        Assert.Equal("Ana Ficticia", _gestor.Usuarios[cuenta.IdUsuario].NombreCompleto);
        AssertSinEscrituras();
    }

    [Fact]
    public async Task ActualizarDatosDeCuenta_DeUnaCuentaInexistente_Rechaza()
    {
        var respuesta = await _servicio.ActualizarDatosDeCuentaAsync(Guid.NewGuid(), "Ana Ficticia", null);

        Assert.False(respuesta.Exito);
        Assert.Equal(MensajesDeIdentidad.UsuarioNoEncontrado, respuesta.Mensaje);
        Assert.Empty(_gestor.ActualizacionesDeDatos);
        AssertSinEscrituras();
    }

    [Fact]
    public async Task ActualizarDatosDeCuenta_SiLaAuditoriaFalla_NoConfirmaElCambio()
    {
        var cuenta = CrearCuentaConDatos();
        _repositorioAuditoria.ExcepcionAlAgregar = new InvalidOperationException("fallo simulado");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _servicio.ActualizarDatosDeCuentaAsync(cuenta.IdUsuario, "Ana María Ficticia", "ana@ficticio.mx"));

        Assert.Equal(0, _unidad.Confirmaciones);
        Assert.Equal(1, _unidad.Reversiones);
    }

    [Fact]
    public async Task ActualizarDatosDeCuenta_PermiteCorregirUnaCuentaDesactivadaYLaPropiaDelSuperusuario()
    {
        var desactivada = CrearCuentaConDatos(activo: false);

        var deLaDesactivada = await _servicio.ActualizarDatosDeCuentaAsync(desactivada.IdUsuario, "Ana Corregida", null);
        var propia = await _servicio.ActualizarDatosDeCuentaAsync(_superusuario.IdUsuario, "Super Ficticio", null);

        Assert.True(deLaDesactivada.Exito);
        Assert.True(propia.Exito);
        Assert.Equal(2, _repositorioAuditoria.Eventos.Count);
    }

    [Fact]
    public async Task ActualizarDatosDeCuenta_ElEventoNoContieneNombreNiCorreo()
    {
        var cuenta = CrearCuentaConDatos();

        await _servicio.ActualizarDatosDeCuentaAsync(cuenta.IdUsuario, "Canario Nombre Ficticio", "canario@ficticio.mx");

        var evento = Assert.Single(_repositorioAuditoria.Eventos);
        var texto = $"{evento.Hecho}|{string.Join(',', evento.CamposAfectados)}";
        Assert.DoesNotContain("Canario", texto);
        Assert.DoesNotContain("canario@ficticio.mx", texto);
        Assert.DoesNotContain("Ana Ficticia", texto);
    }

    [Fact]
    public async Task ListarUsuarios_ConTexto_BuscaTambienPorNombreCompletoYLoDevuelveEnLaLista()
    {
        _gestor.Agregar("usuaria1", NombresDeRol.Usuaria, nombreCompleto: "María Fernández");
        _gestor.Agregar("usuaria2", NombresDeRol.Usuaria, nombreCompleto: "Luis Ficticio");

        var respuesta = await _servicio.ListarUsuariosAsync("FERN");

        var coincidencia = Assert.Single(respuesta.Datos!);
        Assert.Equal("usuaria1", coincidencia.NombreUsuario);
        Assert.Equal("María Fernández", coincidencia.NombreCompleto);
    }
}
