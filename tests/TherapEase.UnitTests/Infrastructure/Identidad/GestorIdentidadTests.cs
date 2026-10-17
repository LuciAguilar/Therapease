using Microsoft.AspNetCore.Identity;
using TherapEase.Application.Identidad.Modelos;
using TherapEase.Infrastructure.Identidad.Entidades;
using TherapEase.UnitTests.Dobles;

namespace TherapEase.UnitTests.Infrastructure.Identidad;

public class GestorIdentidadTests
{
    private readonly UserManagerFalso _usuarios = new();
    private readonly GestorIdentidadSinBloqueoDeFila _gestor;
    private readonly Usuario _usuario = new() { Id = Guid.NewGuid(), UserName = "operador-ficticio", Activo = true };

    public GestorIdentidadTests()
    {
        _usuarios.Usuarios[_usuario.Id.ToString()] = _usuario;
        _gestor = new GestorIdentidadSinBloqueoDeFila(_usuarios) { UsuarioBloqueado = _usuario };
    }

    private void Falla(string operacion) => _usuarios.Resultados[operacion] = UserManagerFalso.Falla();

    private static void AssertMensajeSinDatosPrivados(Exception excepcion)
    {
        Assert.Contains("Identity rechazó la operación", excepcion.Message);
        Assert.Contains("FalloSimulado", excepcion.Message);
        Assert.DoesNotContain("operador-ficticio", excepcion.Message);
        Assert.DoesNotContain("datos privados", excepcion.Message);
    }

    // ──── Crear ────

    [Fact]
    public async Task CrearAsync_ConTodoCorrecto_DevuelveCreadoYAsignaElRol()
    {
        var resultado = await _gestor.CrearAsync(Guid.NewGuid(), "nuevo", "Usuaria", "contrasena-valida-123", true);

        Assert.Equal(ResultadoCreacionDeUsuario.Creado, resultado);
        Assert.Equal(["CreateAsync", "AddToRoleAsync"], _usuarios.Llamadas);
    }

    [Fact]
    public async Task CrearAsync_SiFallaLaAsignacionDelRol_NoLoDaPorCreado()
    {
        Falla("AddToRoleAsync");

        var excepcion = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _gestor.CrearAsync(Guid.NewGuid(), "nuevo", "Usuaria", "contrasena-valida-123", true));

        AssertMensajeSinDatosPrivados(excepcion);
    }

    [Fact]
    public async Task CrearAsync_ConNombreDuplicado_DevuelveNombreDuplicadoSinAsignarRol()
    {
        _usuarios.Resultados["CreateAsync"] = IdentityResult.Failed(new IdentityError { Code = nameof(IdentityErrorDescriber.DuplicateUserName) });

        var resultado = await _gestor.CrearAsync(Guid.NewGuid(), "nuevo", "Usuaria", "contrasena-valida-123", true);

        Assert.Equal(ResultadoCreacionDeUsuario.NombreDuplicado, resultado);
        Assert.Equal(["CreateAsync"], _usuarios.Llamadas);
    }

    [Fact]
    public async Task CrearAsync_ConOtroRechazoDeCreacion_DevuelveDatosInvalidos()
    {
        Falla("CreateAsync");

        var resultado = await _gestor.CrearAsync(Guid.NewGuid(), "nuevo", "Usuaria", "contrasena-valida-123", true);

        Assert.Equal(ResultadoCreacionDeUsuario.DatosInvalidos, resultado);
    }

    // ──── Cambiar rol ────

    [Fact]
    public async Task CambiarRolAsync_ConTodoCorrecto_RetiraAsignaYRenuevaElSello()
    {
        await _gestor.CambiarRolAsync(_usuario.Id, "Superusuario");

        Assert.Equal(["RemoveFromRolesAsync", "AddToRoleAsync", "UpdateSecurityStampAsync"], _usuarios.Llamadas);
    }

    [Theory]
    [InlineData("RemoveFromRolesAsync", new[] { "RemoveFromRolesAsync" })]
    [InlineData("AddToRoleAsync", new[] { "RemoveFromRolesAsync", "AddToRoleAsync" })]
    [InlineData("UpdateSecurityStampAsync", new[] { "RemoveFromRolesAsync", "AddToRoleAsync", "UpdateSecurityStampAsync" })]
    public async Task CambiarRolAsync_SiUnaOperacionEsRechazada_SeDetieneEnEsePunto(string falla, string[] llamadasEsperadas)
    {
        Falla(falla);

        var excepcion = await Assert.ThrowsAsync<InvalidOperationException>(() => _gestor.CambiarRolAsync(_usuario.Id, "Superusuario"));

        AssertMensajeSinDatosPrivados(excepcion);
        Assert.Equal(llamadasEsperadas, _usuarios.Llamadas);
    }

    // ──── Desactivar ────

    [Fact]
    public async Task DesactivarAsync_ConTodoCorrecto_DesactivaYRenuevaElSello()
    {
        await _gestor.DesactivarAsync(_usuario.Id);

        Assert.False(_usuario.Activo);
        Assert.Equal(["UpdateAsync", "UpdateSecurityStampAsync"], _usuarios.Llamadas);
    }

    [Theory]
    [InlineData("UpdateAsync", new[] { "UpdateAsync" })]
    [InlineData("UpdateSecurityStampAsync", new[] { "UpdateAsync", "UpdateSecurityStampAsync" })]
    public async Task DesactivarAsync_SiUnaOperacionEsRechazada_NoLaConfirma(string falla, string[] llamadasEsperadas)
    {
        Falla(falla);

        var excepcion = await Assert.ThrowsAsync<InvalidOperationException>(() => _gestor.DesactivarAsync(_usuario.Id));

        AssertMensajeSinDatosPrivados(excepcion);
        Assert.Equal(llamadasEsperadas, _usuarios.Llamadas);
    }

    // ──── Establecer contraseña ────

    [Fact]
    public async Task EstablecerContrasenaAsync_ConTodoCorrecto_QuitaAgregaYMarcaElCambioPendiente()
    {
        var resultado = await _gestor.EstablecerContrasenaAsync(_usuario.Id, "contrasena-temporal-123", true);

        Assert.Equal(ResultadoDeContrasena.Establecida, resultado);
        Assert.True(_usuario.DebeCambiarContrasena);
        Assert.Equal(["RemovePasswordAsync", "AddPasswordAsync", "UpdateAsync"], _usuarios.Llamadas);
    }

    [Theory]
    [InlineData("RemovePasswordAsync", new[] { "RemovePasswordAsync" })]
    [InlineData("AddPasswordAsync", new[] { "RemovePasswordAsync", "AddPasswordAsync" })]
    [InlineData("UpdateAsync", new[] { "RemovePasswordAsync", "AddPasswordAsync", "UpdateAsync" })]
    public async Task EstablecerContrasenaAsync_SiUnaOperacionEsRechazada_NoDevuelveEstablecida(string falla, string[] llamadasEsperadas)
    {
        Falla(falla);

        var excepcion = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _gestor.EstablecerContrasenaAsync(_usuario.Id, "contrasena-temporal-123", true));

        AssertMensajeSinDatosPrivados(excepcion);
        Assert.Equal(llamadasEsperadas, _usuarios.Llamadas);
    }

    private sealed class ValidadorQueRechaza : IPasswordValidator<Usuario>
    {
        public Task<IdentityResult> ValidateAsync(UserManager<Usuario> manager, Usuario user, string? password) =>
            Task.FromResult(UserManagerFalso.Falla("PasswordTooShort"));
    }

    [Fact]
    public async Task EstablecerContrasenaAsync_ConContrasenaQueNoCumpleReglas_NoRetiraLaActual()
    {
        _usuarios.PasswordValidators.Add(new ValidadorQueRechaza());

        var resultado = await _gestor.EstablecerContrasenaAsync(_usuario.Id, "corta", true);

        Assert.Equal(ResultadoDeContrasena.NoCumpleReglas, resultado);
        Assert.Empty(_usuarios.Llamadas);
    }

    // ──── Revocar sesiones ────

    [Fact]
    public async Task RevocarSesionesAsync_ConUsuarioExistente_RenuevaElSello()
    {
        await _gestor.RevocarSesionesAsync(_usuario.Id);

        Assert.Equal(["UpdateSecurityStampAsync"], _usuarios.Llamadas);
    }

    [Fact]
    public async Task RevocarSesionesAsync_ConUsuarioInexistente_NoHaceNada()
    {
        await _gestor.RevocarSesionesAsync(Guid.NewGuid());

        Assert.Empty(_usuarios.Llamadas);
    }

    [Fact]
    public async Task RevocarSesionesAsync_SiElSelloNoSeRenueva_NoLoDaPorHecho()
    {
        Falla("UpdateSecurityStampAsync");

        var excepcion = await Assert.ThrowsAsync<InvalidOperationException>(() => _gestor.RevocarSesionesAsync(_usuario.Id));

        AssertMensajeSinDatosPrivados(excepcion);
    }

    // ──── Verificar credenciales ────

    [Fact]
    public async Task VerificarCredencialesAsync_ConContrasenaCorrecta_DevuelveElUsuarioYReiniciaLosIntentos()
    {
        var datos = await _gestor.VerificarCredencialesAsync("operador-ficticio", "contrasena-correcta");

        Assert.Equal(new DatosUsuario(_usuario.Id, "operador-ficticio", "Usuaria", true, false), datos);
        Assert.Equal(["ResetAccessFailedCountAsync"], _usuarios.Llamadas);
    }

    [Fact]
    public async Task VerificarCredencialesAsync_ConContrasenaIncorrecta_RegistraElIntentoFallido()
    {
        _usuarios.ContrasenaCorrecta = false;

        var datos = await _gestor.VerificarCredencialesAsync("operador-ficticio", "contrasena-incorrecta");

        Assert.Null(datos);
        Assert.Equal(["AccessFailedAsync"], _usuarios.Llamadas);
    }

    [Fact]
    public async Task VerificarCredencialesAsync_SiNoSePuedeRegistrarElIntentoFallido_NoLoTrataComoUnaContrasenaSimplementeIncorrecta()
    {
        _usuarios.ContrasenaCorrecta = false;
        Falla("AccessFailedAsync");

        var excepcion = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _gestor.VerificarCredencialesAsync("operador-ficticio", "contrasena-incorrecta"));

        AssertMensajeSinDatosPrivados(excepcion);
    }

    [Fact]
    public async Task VerificarCredencialesAsync_SiNoSeReiniciaElContador_NoAbreSesion()
    {
        Falla("ResetAccessFailedCountAsync");

        var excepcion = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _gestor.VerificarCredencialesAsync("operador-ficticio", "contrasena-correcta"));

        AssertMensajeSinDatosPrivados(excepcion);
    }

    [Fact]
    public async Task VerificarCredencialesAsync_ConUsuarioDesconocido_DevuelveNuloSinTocarContadores()
    {
        _gestor.UsuarioBloqueado = null;

        var datos = await _gestor.VerificarCredencialesAsync("nadie", "cualquiera");

        Assert.Null(datos);
        Assert.Empty(_usuarios.Llamadas);
    }

    [Fact]
    public async Task VerificarCredencialesAsync_ConBloqueoVigente_NoEvaluaLaContrasena()
    {
        _usuarios.Bloqueado = true;

        var datos = await _gestor.VerificarCredencialesAsync("operador-ficticio", "contrasena-correcta");

        Assert.Null(datos);
        Assert.Empty(_usuarios.Llamadas);
    }

    [Fact]
    public async Task VerificarCredencialesAsync_ConUsuarioInactivo_NoEvaluaLaContrasena()
    {
        _usuario.Activo = false;

        var datos = await _gestor.VerificarCredencialesAsync("operador-ficticio", "contrasena-correcta");

        Assert.Null(datos);
        Assert.Empty(_usuarios.Llamadas);
    }

    // ──── Verificar contraseña actual ────

    [Fact]
    public async Task VerificarContrasenaActualAsync_ConContrasenaCorrecta_DevuelveVerdadero()
    {
        Assert.True(await _gestor.VerificarContrasenaActualAsync(_usuario.Id, "contrasena-correcta"));
        Assert.Equal(["ResetAccessFailedCountAsync"], _usuarios.Llamadas);
    }

    [Fact]
    public async Task VerificarContrasenaActualAsync_ConContrasenaIncorrecta_RegistraElIntento()
    {
        _usuarios.ContrasenaCorrecta = false;

        Assert.False(await _gestor.VerificarContrasenaActualAsync(_usuario.Id, "incorrecta"));
        Assert.Equal(["AccessFailedAsync"], _usuarios.Llamadas);
    }

    [Fact]
    public async Task VerificarContrasenaActualAsync_SiNoSePuedeRegistrarElIntento_Detiene()
    {
        _usuarios.ContrasenaCorrecta = false;
        Falla("AccessFailedAsync");

        var excepcion = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _gestor.VerificarContrasenaActualAsync(_usuario.Id, "incorrecta"));

        AssertMensajeSinDatosPrivados(excepcion);
    }

    // ──── Ficha ────

    [Fact]
    public async Task ObtenerFichaAsync_DevuelveLosDatosDeLaCuentaYSuCorreo_SinTocarLaCuenta()
    {
        _usuario.Email = "operador@ficticio.mx";
        _usuario.Activo = false;
        _usuario.DebeCambiarContrasena = true;

        var ficha = await _gestor.ObtenerFichaAsync(_usuario.Id);

        Assert.NotNull(ficha);
        Assert.Equal(new DatosUsuario(_usuario.Id, "operador-ficticio", "Usuaria", false, true), ficha.Datos);
        Assert.Equal("operador@ficticio.mx", ficha.Correo);
        Assert.Empty(_usuarios.Llamadas);
    }

    [Fact]
    public async Task ObtenerFichaAsync_SinCorreoGuardado_DevuelveCorreoNulo()
    {
        var ficha = await _gestor.ObtenerFichaAsync(_usuario.Id);

        Assert.NotNull(ficha);
        Assert.Null(ficha.Correo);
    }

    [Fact]
    public async Task ObtenerFichaAsync_DeUnaCuentaInexistente_DevuelveNulo()
    {
        Assert.Null(await _gestor.ObtenerFichaAsync(Guid.NewGuid()));
    }

    // ──── Nombre completo y correo ────

    [Fact]
    public async Task CrearAsync_ConNombreCompletoYCorreo_LosAsignaALaCuentaCreada()
    {
        await _gestor.CrearAsync(Guid.NewGuid(), "nuevo", "Usuaria", "contrasena-valida-123", true, "Ana Ficticia", "ana@ficticio.mx");

        var creada = Assert.Single(_usuarios.UsuariosCreados);
        Assert.Equal("Ana Ficticia", creada.NombreCompleto);
        Assert.Equal("ana@ficticio.mx", creada.Email);
    }

    // El comando de arranque crea al primer superusuario sin nombre ni correo; el nombre queda vacío hasta que el superusuario lo capture.
    [Fact]
    public async Task CrearAsync_SinNombreCompletoNiCorreo_DejaElNombreVacioYSinCorreo()
    {
        await _gestor.CrearAsync(Guid.NewGuid(), "primer-superusuario", "Superusuario", "contrasena-valida-123", true);

        var creada = Assert.Single(_usuarios.UsuariosCreados);
        Assert.Equal(string.Empty, creada.NombreCompleto);
        Assert.Null(creada.Email);
    }

    [Fact]
    public async Task ObtenerAsync_IncluyeElNombreCompletoDeLaCuenta()
    {
        _usuario.NombreCompleto = "Operador Ficticio";

        var datos = await _gestor.ObtenerAsync(_usuario.Id);

        Assert.Equal("Operador Ficticio", datos!.NombreCompleto);
    }

    [Fact]
    public async Task ObtenerFichaAsync_IncluyeElNombreCompletoYElCorreo()
    {
        _usuario.NombreCompleto = "Operador Ficticio";
        _usuario.Email = "operador@ficticio.mx";

        var ficha = await _gestor.ObtenerFichaAsync(_usuario.Id);

        Assert.Equal("Operador Ficticio", ficha!.Datos.NombreCompleto);
        Assert.Equal("operador@ficticio.mx", ficha.Correo);
    }

    [Fact]
    public async Task ActualizarDatosAsync_ConCambios_GuardaConUpdateAsyncYNuncaRenuevaElSello()
    {
        _usuario.NombreCompleto = "Operador Ficticio";
        _usuario.Email = "operador@ficticio.mx";

        var cambios = await _gestor.ActualizarDatosAsync(_usuario.Id, "Operador Nuevo", "nuevo@ficticio.mx");

        Assert.Equal(new CamposActualizados(true, true), cambios);
        Assert.Equal("Operador Nuevo", _usuario.NombreCompleto);
        Assert.Equal("nuevo@ficticio.mx", _usuario.Email);
        // Sin UpdateSecurityStampAsync ni SetEmailAsync: el sello no cambia y las sesiones abiertas siguen válidas.
        Assert.Equal(["UpdateAsync"], _usuarios.Llamadas);
    }

    [Fact]
    public async Task ActualizarDatosAsync_SinCambios_NoEscribeNada()
    {
        _usuario.NombreCompleto = "Operador Ficticio";
        _usuario.Email = "operador@ficticio.mx";

        var cambios = await _gestor.ActualizarDatosAsync(_usuario.Id, "Operador Ficticio", "operador@ficticio.mx");

        Assert.False(cambios.Hubo);
        Assert.Empty(_usuarios.Llamadas);
    }

    [Theory]
    [InlineData("Operador Nuevo", "operador@ficticio.mx", true, false)]
    [InlineData("Operador Ficticio", "otro@ficticio.mx", false, true)]
    [InlineData("Operador Ficticio", null, false, true)]
    public async Task ActualizarDatosAsync_InformaSoloDeLosCamposQueCambiaron(string nombre, string? correo, bool cambioNombre, bool cambioCorreo)
    {
        _usuario.NombreCompleto = "Operador Ficticio";
        _usuario.Email = "operador@ficticio.mx";

        var cambios = await _gestor.ActualizarDatosAsync(_usuario.Id, nombre, correo);

        Assert.Equal(new CamposActualizados(cambioNombre, cambioCorreo), cambios);
        Assert.Equal(correo, _usuario.Email);
    }

    [Fact]
    public async Task ActualizarDatosAsync_CompletaElNombreDeUnaCuentaSinNombreNiCorreo()
    {
        var cambios = await _gestor.ActualizarDatosAsync(_usuario.Id, "Operador Ficticio", null);

        Assert.Equal(new CamposActualizados(true, false), cambios);
        Assert.Equal("Operador Ficticio", _usuario.NombreCompleto);
    }

    [Fact]
    public async Task ActualizarDatosAsync_SiIdentityRechazaElCambio_DetieneSinRevelarNombreNiCorreo()
    {
        Falla("UpdateAsync");

        var excepcion = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _gestor.ActualizarDatosAsync(_usuario.Id, "Nombre Canario", "canario@ficticio.mx"));

        AssertMensajeSinDatosPrivados(excepcion);
        Assert.DoesNotContain("Canario", excepcion.Message);
        Assert.DoesNotContain("canario@ficticio.mx", excepcion.Message);
    }

    [Fact]
    public async Task ActualizarDatosAsync_DeUnaCuentaInexistente_Falla()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _gestor.ActualizarDatosAsync(Guid.NewGuid(), "Nombre Ficticio", null));
    }
}
