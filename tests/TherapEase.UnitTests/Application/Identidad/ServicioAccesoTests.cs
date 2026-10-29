using TherapEase.Application.Auditoria.Servicios;
using TherapEase.Application.Compartido.Interfaces.Repositorios;
using TherapEase.Application.Identidad.Constantes;
using TherapEase.Application.Identidad.Modelos;
using TherapEase.Application.Identidad.Servicios;
using TherapEase.Domain.Auditoria.Entidades.Enums;
using TherapEase.Domain.Identidad.Constantes;
using TherapEase.UnitTests.Dobles;

namespace TherapEase.UnitTests.Application.Identidad;

public class ServicioAccesoTests
{
    private const string Contrasena = "contrasena-valida-123";
    private const string Incorrecta = "incorrecta-123456";

    private readonly GestorIdentidadFalso _gestor = new();
    private readonly EmisorDeSesionFalso _emisor = new();
    private readonly UnidadDeTrabajoFalsa _unidad = new();
    private readonly RepositorioAuditoriaFalso _repositorioAuditoria = new();
    private readonly RegistroTecnicoFalso _registro = new();
    private readonly UsuarioActualFalso _actual = new();
    private readonly ServicioAcceso _servicio;

    public ServicioAccesoTests()
    {
        _servicio = Crear(_gestor, _unidad);
    }

    private ServicioAcceso Crear(GestorIdentidadFalso gestor, IUnidadDeTrabajo unidad)
    {
        var auditoria = new ServicioAuditoria(_repositorioAuditoria, new AutorizacionFalsa(), new RelojFijo(new DateTimeOffset(2026, 10, 20, 15, 0, 0, TimeSpan.Zero)));
        return new ServicioAcceso(gestor, _emisor, unidad, auditoria, _registro, _actual);
    }

    // ──── Entrar ────

    [Fact]
    public async Task IniciarSesion_ConCredencialesValidas_AbreLaSesionConSuRol()
    {
        var usuaria = _gestor.Agregar("usuaria1", NombresDeRol.Usuaria, Contrasena);

        var respuesta = await _servicio.IniciarSesionAsync("usuaria1", Contrasena);

        Assert.True(respuesta.Exito);
        Assert.Equal(NombresDeRol.Usuaria, respuesta.Datos!.Rol);
        Assert.Equal([usuaria.IdUsuario], _emisor.Abiertas);
    }

    [Fact]
    public async Task IniciarSesion_ConContrasenaTemporal_AbreSesionMarcandoQueDebeCambiarla()
    {
        _gestor.Agregar("usuaria1", NombresDeRol.Usuaria, Contrasena, debeCambiar: true);

        var respuesta = await _servicio.IniciarSesionAsync("usuaria1", Contrasena);

        Assert.True(respuesta.Exito);
        Assert.True(respuesta.Datos!.DebeCambiarContrasena);
    }

    [Fact]
    public async Task IniciarSesion_UsuarioInexistenteContrasenaIncorrectaBloqueadoYDesactivado_Reciben_ElMismoMensaje()
    {
        _gestor.Agregar("usuaria1", NombresDeRol.Usuaria, Contrasena);
        var bloqueada = _gestor.Agregar("bloqueada", NombresDeRol.Usuaria, Contrasena);
        _gestor.Bloqueados.Add(bloqueada.IdUsuario);
        _gestor.Agregar("inactiva", NombresDeRol.Usuaria, Contrasena, activo: false);

        var mensajes = new[]
        {
            (await _servicio.IniciarSesionAsync("no-existe", Contrasena)).Mensaje,
            (await _servicio.IniciarSesionAsync("usuaria1", Incorrecta)).Mensaje,
            (await _servicio.IniciarSesionAsync("bloqueada", Contrasena)).Mensaje,
            (await _servicio.IniciarSesionAsync("inactiva", Contrasena)).Mensaje,
            (await _servicio.IniciarSesionAsync("", Contrasena)).Mensaje,
            (await _servicio.IniciarSesionAsync("usuaria1", "")).Mensaje
        };

        Assert.All(mensajes, m => Assert.Equal(MensajesDeIdentidad.CredencialesInvalidas, m));
        Assert.Empty(_emisor.Abiertas);
    }

    [Fact]
    public async Task IniciarSesion_AunqueSeNiegue_ConfirmaElConteoDeFallos()
    {
        var usuaria = _gestor.Agregar("usuaria1", NombresDeRol.Usuaria, Contrasena);

        await _servicio.IniciarSesionAsync("usuaria1", Incorrecta);

        Assert.Equal(1, _unidad.Confirmaciones);
        Assert.Equal(1, _gestor.IntentosFallidos[usuaria.IdUsuario]);
    }

    [Fact]
    public async Task IniciarSesion_SiFallaLaVerificacion_NoAbreSesion()
    {
        var servicio = Crear(new GestorQueFalla(), _unidad);

        await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.IniciarSesionAsync("usuaria1", Contrasena));

        Assert.Empty(_emisor.Abiertas);
        Assert.Empty(_repositorioAuditoria.Eventos);
    }

    // ──── Evento de entrada ────

    [Fact]
    public async Task IniciarSesion_Correcta_RegistraElEventoDeEntradaDeLaCuenta()
    {
        var usuaria = _gestor.Agregar("usuaria1", NombresDeRol.Usuaria, Contrasena);

        await _servicio.IniciarSesionAsync("usuaria1", Contrasena);

        var evento = Assert.Single(_repositorioAuditoria.Eventos);
        Assert.Equal(AccionAuditoria.InicioSesion, evento.Accion);
        Assert.Equal(TipoRegistroAuditoria.Usuario, evento.TipoRegistro);
        Assert.Equal(usuaria.IdUsuario, evento.IdUsuarioActor);
        Assert.Equal(usuaria.IdUsuario, evento.IdRegistro);
        Assert.Empty(evento.CamposAfectados);
        Assert.Equal("Inició sesión.", evento.Hecho);
    }

    [Fact]
    public async Task IniciarSesion_ConTemporalPendiente_TambienRegistraLaEntrada()
    {
        _gestor.Agregar("usuaria1", NombresDeRol.Usuaria, Contrasena, debeCambiar: true);

        await _servicio.IniciarSesionAsync("usuaria1", Contrasena);

        Assert.Equal(AccionAuditoria.InicioSesion, Assert.Single(_repositorioAuditoria.Eventos).Accion);
    }

    [Fact]
    public async Task IniciarSesion_Correcta_GuardaLaVerificacionYElEventoEnUnaSolaTransaccion()
    {
        _gestor.Agregar("usuaria1", NombresDeRol.Usuaria, Contrasena);

        await _servicio.IniciarSesionAsync("usuaria1", Contrasena);

        Assert.Equal(1, _unidad.Confirmaciones);
        Assert.Equal(0, _unidad.Reversiones);
    }

    [Fact]
    public async Task IniciarSesion_SiFallaElEventoDeEntrada_NoAbreSesionYLaExcepcionLlegaAlErrorGeneral()
    {
        _gestor.Agregar("usuaria1", NombresDeRol.Usuaria, Contrasena);
        _repositorioAuditoria.ExcepcionAlAgregar = new InvalidOperationException("fallo simulado");

        // No se convierte en «usuario o contraseña incorrectos»: eso ocultaría una falla de la base como si fuera un error de la persona.
        await Assert.ThrowsAsync<InvalidOperationException>(() => _servicio.IniciarSesionAsync("usuaria1", Contrasena));

        Assert.Empty(_emisor.Abiertas);
        Assert.Equal(0, _unidad.Confirmaciones);
        Assert.Equal(1, _unidad.Reversiones);
    }

    [Fact]
    public async Task IniciarSesion_Fallida_NoRegistraNingunEvento()
    {
        _gestor.Agregar("usuaria1", NombresDeRol.Usuaria, Contrasena);
        var bloqueada = _gestor.Agregar("bloqueada", NombresDeRol.Usuaria, Contrasena);
        _gestor.Bloqueados.Add(bloqueada.IdUsuario);
        _gestor.Agregar("inactiva", NombresDeRol.Usuaria, Contrasena, activo: false);

        await _servicio.IniciarSesionAsync("no-existe", Contrasena);
        await _servicio.IniciarSesionAsync("usuaria1", Incorrecta);
        await _servicio.IniciarSesionAsync("bloqueada", Contrasena);
        await _servicio.IniciarSesionAsync("inactiva", Contrasena);
        await _servicio.IniciarSesionAsync("", Contrasena);
        await _servicio.IniciarSesionAsync("usuaria1", "");

        Assert.Empty(_repositorioAuditoria.Eventos);
    }

    // ──── Bloqueo ────

    [Fact]
    public async Task IniciarSesion_ElQuintoFalloInicia_ElBloqueoYRegistraUnSoloEvento()
    {
        var usuaria = _gestor.Agregar("usuaria1", NombresDeRol.Usuaria, Contrasena);
        _gestor.LimiteDeFallos = 5;

        for (var i = 1; i <= 4; i++)
        {
            await _servicio.IniciarSesionAsync("usuaria1", Incorrecta);
            Assert.Empty(_repositorioAuditoria.Eventos);
        }

        var quinto = await _servicio.IniciarSesionAsync("usuaria1", Incorrecta);

        Assert.False(quinto.Exito);
        Assert.Equal(MensajesDeIdentidad.CredencialesInvalidas, quinto.Mensaje);
        var evento = Assert.Single(_repositorioAuditoria.Eventos);
        Assert.Equal(AccionAuditoria.BloqueoAcceso, evento.Accion);
        Assert.Equal(TipoRegistroAuditoria.Usuario, evento.TipoRegistro);
        Assert.Equal(usuaria.IdUsuario, evento.IdUsuarioActor);
        Assert.Equal(usuaria.IdUsuario, evento.IdRegistro);
        Assert.Empty(evento.CamposAfectados);
        Assert.Equal("Se bloqueó el acceso por cinco intentos fallidos.", evento.Hecho);
        Assert.Empty(_emisor.Abiertas);
    }

    [Fact]
    public async Task IniciarSesion_ConElBloqueoVigente_NoVuelveARegistrarloNiConLaContrasenaCorrecta()
    {
        _gestor.Agregar("usuaria1", NombresDeRol.Usuaria, Contrasena);
        _gestor.LimiteDeFallos = 3;
        for (var i = 0; i < 3; i++)
        {
            await _servicio.IniciarSesionAsync("usuaria1", Incorrecta);
        }

        var siguientes = new[]
        {
            await _servicio.IniciarSesionAsync("usuaria1", Incorrecta),
            await _servicio.IniciarSesionAsync("usuaria1", Contrasena),
            await _servicio.IniciarSesionAsync("usuaria1", Incorrecta)
        };

        Assert.All(siguientes, r => Assert.Equal(MensajesDeIdentidad.CredencialesInvalidas, r.Mensaje));
        Assert.Equal(AccionAuditoria.BloqueoAcceso, Assert.Single(_repositorioAuditoria.Eventos).Accion);
        Assert.Empty(_emisor.Abiertas);
    }

    [Fact]
    public async Task IniciarSesion_ElBloqueoSeConfirmaAntesDeRegistrarSuEvento()
    {
        _gestor.Agregar("usuaria1", NombresDeRol.Usuaria, Contrasena);
        _gestor.LimiteDeFallos = 1;
        var alEmpezar = new List<(int Bloqueadas, int Confirmaciones)>();
        var servicio = Crear(_gestor, new UnidadQueObserva(_unidad, () => alEmpezar.Add((_gestor.Bloqueados.Count, _unidad.Confirmaciones))));

        await servicio.IniciarSesionAsync("usuaria1", Incorrecta);

        // Primera transacción: la verificación. Cuando empieza la segunda, la del evento, el bloqueo ya está confirmado en la primera.
        Assert.Equal([(0, 0), (1, 1)], alEmpezar);
        Assert.Equal(2, _unidad.Confirmaciones);
    }

    [Fact]
    public async Task IniciarSesion_SiFallaElEventoDeBloqueo_ElBloqueoSigueYSoloQuedaElAvisoTecnico()
    {
        var usuaria = _gestor.Agregar("usuaria1", NombresDeRol.Usuaria, Contrasena);
        _gestor.LimiteDeFallos = 1;
        _repositorioAuditoria.ExcepcionAlAgregar = new InvalidOperationException("fallo simulado");

        var respuesta = await _servicio.IniciarSesionAsync("usuaria1", Incorrecta);

        Assert.False(respuesta.Exito);
        Assert.Equal(MensajesDeIdentidad.CredencialesInvalidas, respuesta.Mensaje);
        Assert.Contains(usuaria.IdUsuario, _gestor.Bloqueados);
        Assert.Equal(1, _registro.AvisosDeBloqueo);
        Assert.Equal(0, _registro.AvisosDeSalida);
        Assert.Empty(_repositorioAuditoria.Eventos);
        Assert.Equal(1, _unidad.Confirmaciones);
        Assert.Equal(1, _unidad.Reversiones);
    }

    [Fact]
    public async Task IniciarSesion_ConIntentosSimultaneosQueSuperanElLimite_RegistraUnSoloBloqueo()
    {
        _gestor.Agregar("usuaria1", NombresDeRol.Usuaria, Contrasena);
        _gestor.LimiteDeFallos = 5;

        var respuestas = await Task.WhenAll(Enumerable.Range(0, 14)
            .Select(_ => Task.Run(() => _servicio.IniciarSesionAsync("usuaria1", Incorrecta))));

        Assert.All(respuestas, r => Assert.Equal(MensajesDeIdentidad.CredencialesInvalidas, r.Mensaje));
        Assert.Equal(AccionAuditoria.BloqueoAcceso, Assert.Single(_repositorioAuditoria.Eventos).Accion);
        Assert.Equal(0, _registro.Total);
    }

    [Fact]
    public async Task IniciarSesion_ConBloqueosSimultaneosDeVariasCuentas_RegistraUnEventoPorCuenta()
    {
        var ana = _gestor.Agregar("ana", NombresDeRol.Usuaria, Contrasena);
        var luis = _gestor.Agregar("luis", NombresDeRol.Usuaria, Contrasena);
        _gestor.LimiteDeFallos = 5;

        await Task.WhenAll(Enumerable.Range(0, 20)
            .Select(i => Task.Run(() => _servicio.IniciarSesionAsync(i % 2 == 0 ? "ana" : "luis", Incorrecta))));

        Assert.Equal(2, _repositorioAuditoria.Eventos.Count);
        Assert.All(_repositorioAuditoria.Eventos, e => Assert.Equal(AccionAuditoria.BloqueoAcceso, e.Accion));
        Assert.Equal(new[] { ana.IdUsuario, luis.IdUsuario }.Order(), _repositorioAuditoria.Eventos.Select(e => e.IdRegistro).Order());
    }

    // ──── Salir ────

    [Fact]
    public async Task CerrarSesion_RenuevaElSelloYRetiraLaCookie()
    {
        var usuaria = _gestor.Agregar("usuaria1", NombresDeRol.Usuaria, Contrasena);
        _actual.IdUsuario = usuaria.IdUsuario;

        await _servicio.CerrarSesionAsync();

        Assert.Equal([usuaria.IdUsuario], _gestor.SesionesRevocadas);
        Assert.Equal(1, _emisor.Cierres);
    }

    [Fact]
    public async Task CerrarSesion_SinSesionOYaVencida_TerminaSinErrorYSinEvento()
    {
        _actual.IdUsuario = null;

        await _servicio.CerrarSesionAsync();
        await _servicio.CerrarSesionAsync();

        Assert.Empty(_gestor.SesionesRevocadas);
        Assert.Equal(2, _emisor.Cierres);
        Assert.Empty(_repositorioAuditoria.Eventos);
        Assert.Equal(0, _registro.Total);
    }

    [Fact]
    public async Task CerrarSesion_ConSesion_RegistraElEventoDeSalidaDeLaCuenta()
    {
        var usuaria = _gestor.Agregar("usuaria1", NombresDeRol.Usuaria, Contrasena);
        _actual.IdUsuario = usuaria.IdUsuario;

        await _servicio.CerrarSesionAsync();

        var evento = Assert.Single(_repositorioAuditoria.Eventos);
        Assert.Equal(AccionAuditoria.CierreSesion, evento.Accion);
        Assert.Equal(TipoRegistroAuditoria.Usuario, evento.TipoRegistro);
        Assert.Equal(usuaria.IdUsuario, evento.IdUsuarioActor);
        Assert.Equal(usuaria.IdUsuario, evento.IdRegistro);
        Assert.Empty(evento.CamposAfectados);
        Assert.Equal("Cerró sesión.", evento.Hecho);
        Assert.Equal(0, _registro.Total);
    }

    [Fact]
    public async Task CerrarSesion_CierraPrimeroYRegistraDespuesEnOtraTransaccion()
    {
        var usuaria = _gestor.Agregar("usuaria1", NombresDeRol.Usuaria, Contrasena);
        _actual.IdUsuario = usuaria.IdUsuario;
        var alEmpezar = new List<(int SellosRenovados, int CookiesRetiradas)>();
        var servicio = Crear(_gestor, new UnidadQueObserva(_unidad, () => alEmpezar.Add((_gestor.SesionesRevocadas.Count, _emisor.Cierres))));

        await servicio.CerrarSesionAsync();

        // Salir no abre más transacción que la del evento, y cuando empieza el sello ya se renovó y la cookie ya se retiró.
        Assert.Equal([(1, 1)], alEmpezar);
        Assert.Equal(1, _unidad.Confirmaciones);
    }

    [Fact]
    public async Task CerrarSesion_SiFallaElEvento_LaSesionQuedaCerradaYSoloQuedaElAvisoTecnico()
    {
        var usuaria = _gestor.Agregar("usuaria1", NombresDeRol.Usuaria, Contrasena);
        _actual.IdUsuario = usuaria.IdUsuario;
        _repositorioAuditoria.ExcepcionAlAgregar = new InvalidOperationException("fallo simulado");

        await _servicio.CerrarSesionAsync();

        Assert.Equal([usuaria.IdUsuario], _gestor.SesionesRevocadas);
        Assert.Equal(1, _emisor.Cierres);
        Assert.Equal(1, _registro.AvisosDeSalida);
        Assert.Equal(0, _registro.AvisosDeBloqueo);
        Assert.Empty(_repositorioAuditoria.Eventos);
        Assert.Equal(1, _unidad.Reversiones);
    }

    // ──── Sin datos privados ────

    [Fact]
    public async Task Eventos_DeEntradaSalidaYBloqueo_NoContienenNombresEscritosNiContrasenas()
    {
        var canario = _gestor.Agregar("usuaria_canario", NombresDeRol.Usuaria, Contrasena);
        _gestor.LimiteDeFallos = 2;

        // Se escribe como nombre algo que parece una contraseña: tampoco debe quedar guardado.
        await _servicio.IniciarSesionAsync("secreto-tecleado-por-error", Incorrecta);
        await _servicio.IniciarSesionAsync("usuaria_canario", "otra-incorrecta-987");
        await _servicio.IniciarSesionAsync("usuaria_canario", "otra-incorrecta-987");
        _gestor.Bloqueados.Clear();
        await _servicio.IniciarSesionAsync("usuaria_canario", Contrasena);
        _actual.IdUsuario = canario.IdUsuario;
        await _servicio.CerrarSesionAsync();

        Assert.Equal(
            [AccionAuditoria.BloqueoAcceso, AccionAuditoria.InicioSesion, AccionAuditoria.CierreSesion],
            _repositorioAuditoria.Eventos.Select(e => e.Accion));
        foreach (var evento in _repositorioAuditoria.Eventos)
        {
            var texto = $"{evento.Hecho}|{string.Join(',', evento.CamposAfectados)}";
            Assert.DoesNotContain("usuaria_canario", texto);
            Assert.DoesNotContain("secreto-tecleado-por-error", texto);
            Assert.DoesNotContain(Contrasena, texto);
            Assert.DoesNotContain(Incorrecta, texto);
            Assert.DoesNotContain("otra-incorrecta-987", texto);
        }
    }

    private sealed class GestorQueFalla : GestorIdentidadFalso
    {
        public override Task<ResultadoDeVerificacion> VerificarCredencialesAsync(string nombreUsuario, string contrasena, CancellationToken cancelacion = default) =>
            throw new InvalidOperationException("fallo simulado");
    }

    // Avisa justo antes de cada transacción, para comprobar qué estaba ya guardado cuando empezó.
    private sealed class UnidadQueObserva(UnidadDeTrabajoFalsa interna, Action alEmpezar) : IUnidadDeTrabajo
    {
        public Task EjecutarAsync(Func<CancellationToken, Task> operacion, CancellationToken cancelacion = default)
        {
            alEmpezar();
            return interna.EjecutarAsync(operacion, cancelacion);
        }
    }
}
