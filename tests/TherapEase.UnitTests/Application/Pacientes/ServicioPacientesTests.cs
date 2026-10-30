using TherapEase.Application.Auditoria.Servicios;
using TherapEase.Application.Compartido.Interfaces.Repositorios;
using TherapEase.Application.Pacientes.Constantes;
using TherapEase.Application.Pacientes.Modelos;
using TherapEase.Application.Pacientes.Servicios;
using TherapEase.Domain.Auditoria.Entidades.Enums;
using TherapEase.Domain.Compartido.Entidades.Enums;
using TherapEase.Domain.Compartido.Excepciones;
using TherapEase.Domain.Pacientes.Constantes;
using TherapEase.Domain.Pacientes.Entidades;
using TherapEase.Domain.Pacientes.Entidades.Enums;
using TherapEase.Domain.Pacientes.Excepciones;
using TherapEase.Domain.Pacientes.Reglas;
using TherapEase.UnitTests.Dobles;

namespace TherapEase.UnitTests.Application.Pacientes;

public class ServicioPacientesTests
{
    // 5-oct-2026, 08:00 en Hermosillo.
    private static readonly DateTimeOffset Ahora = new(2026, 10, 5, 15, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly NacimientoAdulto = new(1990, 1, 15);
    private static readonly DateOnly NacimientoMenor = new(2015, 3, 20);
    private static readonly Guid OtroUsuario = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private readonly RepositorioPacientesFalso _pacientes = new();
    private readonly UnidadDeTrabajoFalsa _unidad = new();
    private readonly RepositorioAuditoriaFalso _repositorioAuditoria = new();
    private readonly AutorizacionFalsa _autorizacion = new();
    private readonly UsuarioActualFalso _actual = new();
    private readonly Guid _idActor = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly ServicioPacientes _servicio;

    public ServicioPacientesTests()
    {
        _actual.IdUsuario = _idActor;
        _autorizacion.Permisos.UnionWith([Permiso.RegistrarPacientes, Permiso.ConsultarPacientes, Permiso.ActualizarPacientes]);
        _servicio = Crear(_unidad);
    }

    private ServicioPacientes Crear(IUnidadDeTrabajo unidad)
    {
        var reloj = new RelojFijo(Ahora);
        var auditoria = new ServicioAuditoria(_repositorioAuditoria, _autorizacion, reloj);
        return new ServicioPacientes(_pacientes, unidad, auditoria, _autorizacion, _actual, reloj);
    }

    private static DatosDePacienteEntrada Adulto(string nombre = "Paciente Ficticio", params AmbitoAtencion[] ambitos) =>
        new(nombre, NacimientoAdulto, null, "canario@ficticio.test", null, null, ambitos.Length == 0 ? [AmbitoAtencion.Escolar] : ambitos);

    private static DatosDePacienteEntrada Menor() =>
        new("Menor Ficticio", NacimientoMenor, null, null, "Escuela Ficticia", "3A", [AmbitoAtencion.Escolar],
            new DatosDeTutorEntrada("Tutora Ficticia", "Madre", "6441234567", null));

    // Guarda directo en el doble, sin pasar por el servicio ni por sus eventos.
    private Paciente Existente(string nombre = "Paciente Ficticio", DateOnly? nacimiento = null, Guid? creador = null, params AmbitoAtencion[] ambitos)
    {
        var datos = new DatosDePaciente(nombre, nacimiento ?? NacimientoAdulto, Correo: "canario@ficticio.test");
        var paciente = ReglasDePaciente.Registrar(
            Guid.NewGuid(), datos, ambitos.Length == 0 ? [AmbitoAtencion.Escolar] : ambitos, creador ?? _idActor, Ahora);
        _pacientes.Pacientes[paciente.IdPaciente] = paciente;
        return paciente;
    }

    // El setter es interno de Domain: la prueba lo alcanza por reflexión para armar fichas anteriores o con una fecha que el alta no admitiría.
    private static void ConNacimiento(Paciente paciente, DateOnly? nacimiento) =>
        typeof(Paciente).GetProperty(nameof(Paciente.FechaNacimiento))!.SetValue(paciente, nacimiento);

    private static void SinNacimiento(Paciente paciente) => ConNacimiento(paciente, null);

    private void AssertSinEfectos()
    {
        Assert.Equal(0, _pacientes.Lecturas);
        Assert.Equal(0, _unidad.Confirmaciones);
        Assert.Empty(_repositorioAuditoria.Eventos);
    }

    // ----- Permisos -----

    [Theory]
    [InlineData(Permiso.RegistrarPacientes)]
    [InlineData(Permiso.ConsultarPacientes)]
    public async Task Registrar_SinAlgunoDeLosDosPermisos_RechazaAntesDeLeerNada(Permiso faltante)
    {
        _autorizacion.Permisos.Remove(faltante);

        var respuesta = await _servicio.RegistrarAsync(Guid.NewGuid(), Adulto());

        Assert.False(respuesta.Exito);
        Assert.Equal(MensajesDePacientes.SinPermiso, respuesta.Mensaje);
        Assert.Null(respuesta.Datos);
        AssertSinEfectos();
    }

    [Fact]
    public async Task Buscar_SinPermisoDeConsulta_RechazaAntesDeLeerNada()
    {
        _autorizacion.Permisos.Remove(Permiso.ConsultarPacientes);

        var respuesta = await _servicio.BuscarAsync("paciente");

        Assert.Equal(MensajesDePacientes.SinPermiso, respuesta.Mensaje);
        AssertSinEfectos();
    }

    [Fact]
    public async Task Obtener_SinPermisoDeConsulta_RechazaAntesDeLeerNada()
    {
        var paciente = Existente();
        _autorizacion.Permisos.Remove(Permiso.ConsultarPacientes);

        var respuesta = await _servicio.ObtenerAsync(paciente.IdPaciente);

        Assert.Equal(MensajesDePacientes.SinPermiso, respuesta.Mensaje);
        AssertSinEfectos();
    }

    [Fact]
    public async Task Actualizar_SinPermisoDeActualizacion_RechazaAntesDeLeerNada()
    {
        var paciente = Existente();
        _autorizacion.Permisos.Remove(Permiso.ActualizarPacientes);

        var respuesta = await _servicio.ActualizarAsync(paciente.IdPaciente, 1, Adulto("Nombre Nuevo"));

        Assert.Equal(MensajesDePacientes.SinPermiso, respuesta.Mensaje);
        AssertSinEfectos();
        Assert.Equal("Paciente Ficticio", paciente.Nombre);
    }

    [Fact]
    public async Task ConsultarNoBastaParaRegistrarNiParaActualizar()
    {
        _autorizacion.Permisos.Clear();
        _autorizacion.Permisos.Add(Permiso.ConsultarPacientes);
        var paciente = Existente();

        var alta = await _servicio.RegistrarAsync(Guid.NewGuid(), Adulto("Otra Persona"));
        var edicion = await _servicio.ActualizarAsync(paciente.IdPaciente, 1, Adulto("Nombre Nuevo"));

        Assert.Equal(MensajesDePacientes.SinPermiso, alta.Mensaje);
        Assert.Equal(MensajesDePacientes.SinPermiso, edicion.Mensaje);
    }

    [Fact]
    public async Task SinSesion_TodasLasOperacionesRechazan()
    {
        _actual.IdUsuario = null;
        var paciente = Existente();
        var lecturasIniciales = _pacientes.Lecturas;

        var respuestas = new[]
        {
            (await _servicio.RegistrarAsync(Guid.NewGuid(), Adulto("Otra Persona"))).Mensaje,
            (await _servicio.BuscarAsync("paciente")).Mensaje,
            (await _servicio.ObtenerAsync(paciente.IdPaciente)).Mensaje,
            (await _servicio.ActualizarAsync(paciente.IdPaciente, 1, Adulto("Nombre Nuevo"))).Mensaje
        };

        Assert.All(respuestas, m => Assert.Equal(MensajesDePacientes.SinPermiso, m));
        Assert.Equal(lecturasIniciales, _pacientes.Lecturas);
        Assert.Empty(_repositorioAuditoria.Eventos);
    }

    // ----- Registrar -----

    [Fact]
    public async Task Registrar_ConDatosValidos_GuardaLaFichaConElIdentificadorDeLaSolicitudYUnSoloEvento()
    {
        var idSolicitud = Guid.NewGuid();

        var respuesta = await _servicio.RegistrarAsync(idSolicitud, Adulto());

        Assert.True(respuesta.Exito);
        Assert.Equal(EstadoDeAlta.Registrado, respuesta.Datos!.Estado);
        Assert.Equal(idSolicitud, respuesta.Datos.IdPaciente);
        Assert.Equal(MensajesDePacientes.PacienteRegistrado, respuesta.Mensaje);
        var paciente = Assert.Single(_pacientes.Pacientes.Values);
        Assert.Equal(idSolicitud, paciente.IdPaciente);
        Assert.Equal(1, paciente.Version);
        Assert.Equal(_idActor, paciente.IdUsuarioAlta);
        Assert.Equal(1, _unidad.Confirmaciones);
        Assert.Equal(0, _unidad.Reversiones);
    }

    [Fact]
    public async Task Registrar_EscribeUnEventoDeAltaConElActorDeLaSesionYSoloNombresDeCampos()
    {
        var idSolicitud = Guid.NewGuid();

        await _servicio.RegistrarAsync(idSolicitud, Adulto("Nombre Muy Privado"));

        var evento = Assert.Single(_repositorioAuditoria.Eventos);
        Assert.Equal(_idActor, evento.IdUsuarioActor);
        Assert.Equal(TipoRegistroAuditoria.Paciente, evento.TipoRegistro);
        Assert.Equal(idSolicitud, evento.IdRegistro);
        Assert.Equal(AccionAuditoria.Alta, evento.Accion);
        Assert.Equal(
            [CamposDePaciente.Nombre, CamposDePaciente.FechaNacimiento, CamposDePaciente.Correo, CamposDePaciente.Ambitos],
            evento.CamposAfectados);
        var texto = string.Join('|', evento.CamposAfectados) + evento.Hecho;
        Assert.DoesNotContain("Privado", texto);
        Assert.DoesNotContain("canario", texto);
    }

    [Fact]
    public async Task Registrar_TomaElActorDeLaSesionYNoDeLosDatos()
    {
        _actual.IdUsuario = OtroUsuario;

        await _servicio.RegistrarAsync(Guid.NewGuid(), Adulto());

        Assert.Equal(OtroUsuario, Assert.Single(_pacientes.Pacientes.Values).IdUsuarioAlta);
        Assert.Equal(OtroUsuario, Assert.Single(_repositorioAuditoria.Eventos).IdUsuarioActor);
    }

    [Fact]
    public async Task Registrar_UnMenorConTutor_Guarda()
    {
        var respuesta = await _servicio.RegistrarAsync(Guid.NewGuid(), Menor());

        Assert.True(respuesta.Exito);
        Assert.Equal("Tutora Ficticia", Assert.Single(_pacientes.Pacientes.Values).TutorNombre);
    }

    [Fact]
    public async Task Registrar_UnMenorSinTutor_RechazaConElMensajeDeLaReglaYNoGuardaNiAudita()
    {
        var datos = Menor() with { Tutor = null };

        var respuesta = await _servicio.RegistrarAsync(Guid.NewGuid(), datos);

        Assert.False(respuesta.Exito);
        Assert.Equal(MensajesDeDatosDePaciente.TutorObligatorioParaMenores, respuesta.Mensaje);
        Assert.Empty(_pacientes.Pacientes);
        Assert.Equal(0, _unidad.Confirmaciones);
        Assert.Empty(_repositorioAuditoria.Eventos);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task Registrar_SinNombre_RechazaSinRevelarElValor(string? nombre)
    {
        var respuesta = await _servicio.RegistrarAsync(Guid.NewGuid(), Adulto() with { Nombre = nombre });

        Assert.Equal(MensajesDeDatosDePaciente.NombreInvalido, respuesta.Mensaje);
        Assert.Empty(_pacientes.Pacientes);
    }

    [Fact]
    public async Task Registrar_SinAmbitos_Rechaza()
    {
        var respuesta = await _servicio.RegistrarAsync(Guid.NewGuid(), Adulto() with { Ambitos = null });

        Assert.Equal(MensajesDeDatosDePaciente.AmbitosInvalidos, respuesta.Mensaje);
        Assert.Empty(_pacientes.Pacientes);
    }

    [Fact]
    public async Task Registrar_ConDatosNulosOIdentificadorVacio_RechazaComoSolicitudInvalida()
    {
        var sinDatos = await _servicio.RegistrarAsync(Guid.NewGuid(), null!);
        var sinIdentificador = await _servicio.RegistrarAsync(Guid.Empty, Adulto());

        Assert.Equal(MensajesDePacientes.SolicitudInvalida, sinDatos.Mensaje);
        Assert.Equal(MensajesDePacientes.SolicitudInvalida, sinIdentificador.Mensaje);
        Assert.Empty(_pacientes.Pacientes);
    }

    // ----- Registrar: reenvío, parecidos y carreras -----

    [Fact]
    public async Task Registrar_ReenvioDeLaMismaSolicitudConLosMismosDatos_DevuelveYaRegistradoSinSegundaFichaNiEvento()
    {
        var idSolicitud = Guid.NewGuid();
        await _servicio.RegistrarAsync(idSolicitud, Adulto());

        var reenvio = await _servicio.RegistrarAsync(idSolicitud, Adulto());

        Assert.True(reenvio.Exito);
        Assert.Equal(EstadoDeAlta.YaRegistrado, reenvio.Datos!.Estado);
        Assert.Equal(idSolicitud, reenvio.Datos.IdPaciente);
        Assert.Equal(MensajesDePacientes.PacienteYaRegistrado, reenvio.Mensaje);
        Assert.Single(_pacientes.Pacientes);
        Assert.Single(_repositorioAuditoria.Eventos);
        Assert.Equal(1, _unidad.Confirmaciones);
        Assert.Equal(1, Assert.Single(_pacientes.Pacientes.Values).Version);
    }

    [Fact]
    public async Task Registrar_ReenvioConLosDatosEscritosConOtroFormato_SeReconoceComoLaMismaSolicitud()
    {
        var idSolicitud = Guid.NewGuid();
        await _servicio.RegistrarAsync(idSolicitud, Adulto("María Muñoz"));

        var reenvio = await _servicio.RegistrarAsync(idSolicitud, Adulto("  María Muñoz ") with { Correo = " CANARIO@ficticio.test " });

        Assert.Equal(EstadoDeAlta.YaRegistrado, reenvio.Datos!.Estado);
    }

    [Fact]
    public async Task Registrar_ReenvioDeUnIdentificadorConDatosDistintos_RechazaSinRevelarLaFichaExistente()
    {
        var idSolicitud = Guid.NewGuid();
        await _servicio.RegistrarAsync(idSolicitud, Adulto());

        var otro = await _servicio.RegistrarAsync(idSolicitud, Adulto("Persona Distinta"));

        Assert.False(otro.Exito);
        Assert.Equal(MensajesDePacientes.SolicitudYaUsada, otro.Mensaje);
        Assert.Null(otro.Datos);
        Assert.DoesNotContain("Paciente Ficticio", otro.Mensaje);
        Assert.Equal("Paciente Ficticio", Assert.Single(_pacientes.Pacientes.Values).Nombre);
        Assert.Single(_repositorioAuditoria.Eventos);
    }

    [Fact]
    public async Task Registrar_ReenvioDeLaMismaSolicitudPorOtraPersona_NoSeTrataComoYaRegistrado()
    {
        var idSolicitud = Guid.NewGuid();
        await _servicio.RegistrarAsync(idSolicitud, Adulto());
        _actual.IdUsuario = OtroUsuario;

        var respuesta = await _servicio.RegistrarAsync(idSolicitud, Adulto());

        Assert.Equal(MensajesDePacientes.SolicitudYaUsada, respuesta.Mensaje);
        Assert.Null(respuesta.Datos);
    }

    [Fact]
    public async Task Registrar_ReenvioCuandoLaFichaYaEstaDeBaja_NoDiceQueEstaRegistrada()
    {
        var idSolicitud = Guid.NewGuid();
        await _servicio.RegistrarAsync(idSolicitud, Adulto());
        ReglasDePaciente.DarDeBaja(_pacientes.Pacientes[idSolicitud], 1, _idActor, Ahora);

        var respuesta = await _servicio.RegistrarAsync(idSolicitud, Adulto());

        Assert.False(respuesta.Exito);
        Assert.Equal(MensajesDePacientes.SolicitudYaUsada, respuesta.Mensaje);
    }

    [Fact]
    public async Task Registrar_ConFichasVigentesDelMismoNombre_NoGuardaYDevuelveLasCoincidenciasPorConfirmar()
    {
        var existente = Existente("María Muñoz", new DateOnly(1985, 6, 1));

        var respuesta = await _servicio.RegistrarAsync(Guid.NewGuid(), Adulto("MARIA MUNOZ"));

        Assert.False(respuesta.Exito);
        Assert.Equal(MensajesDePacientes.CoincidenciasPorConfirmar, respuesta.Mensaje);
        Assert.Equal(EstadoDeAlta.CoincidenciasPorConfirmar, respuesta.Datos!.Estado);
        Assert.Null(respuesta.Datos.IdPaciente);
        var coincidencia = Assert.Single(respuesta.Datos.Coincidencias!);
        Assert.Equal(existente.IdPaciente, coincidencia.IdPaciente);
        Assert.Equal("María Muñoz", coincidencia.Nombre);
        Assert.Equal(new DateOnly(1985, 6, 1), coincidencia.FechaNacimiento);
        Assert.False(respuesta.Datos.HayMasCoincidencias);
        Assert.Single(_pacientes.Pacientes);
        Assert.Equal(0, _unidad.Confirmaciones);
        Assert.Empty(_repositorioAuditoria.Eventos);
    }

    [Fact]
    public async Task Registrar_ConUnNombreIgualSalvoMayusculasFueraDeAZ_DevuelveLaCoincidencia()
    {
        var existente = Existente("\u00D8scar");

        var respuesta = await _servicio.RegistrarAsync(Guid.NewGuid(), Adulto("\u00F8SCAR"));

        Assert.Equal(EstadoDeAlta.CoincidenciasPorConfirmar, respuesta.Datos!.Estado);
        Assert.Equal(existente.IdPaciente, Assert.Single(respuesta.Datos.Coincidencias!).IdPaciente);
    }

    private Paciente GuardarComoOtroEnvio(Guid id, string nombre = "Paciente Ficticio", Guid? creador = null)
    {
        var ficha = ReglasDePaciente.Registrar(
            id, new DatosDePaciente(nombre, NacimientoAdulto, Correo: "canario@ficticio.test"), [AmbitoAtencion.Escolar], creador ?? _idActor, Ahora);
        _pacientes.Pacientes[id] = ficha;
        return ficha;
    }

    [Fact]
    public async Task Registrar_SiElMismoEnvioSeGuardaEntreLaLecturaYLaBusquedaDeParecidos_RespondeComoReenvioYNoComoParecido()
    {
        var id = Guid.NewGuid();
        // El doble clic: otro envío con el mismo número y los mismos datos termina de guardarse justo antes de que este busque parecidos y su ficha aparece en la lista.
        _pacientes.AlBuscarParecidos = () => GuardarComoOtroEnvio(id);

        var respuesta = await _servicio.RegistrarAsync(id, Adulto());

        Assert.True(respuesta.Exito);
        Assert.Equal(EstadoDeAlta.YaRegistrado, respuesta.Datos!.Estado);
        Assert.Equal(id, respuesta.Datos.IdPaciente);
        Assert.Null(respuesta.Datos.Coincidencias);
        Assert.Equal(MensajesDePacientes.PacienteYaRegistrado, respuesta.Mensaje);
        Assert.Equal(2, _pacientes.LecturasParaComparacion);
        Assert.Equal(0, _unidad.Confirmaciones);
        Assert.Empty(_repositorioAuditoria.Eventos);
        Assert.Single(_pacientes.Pacientes);
    }

    [Fact]
    public async Task Registrar_SiElReenvioAparecioConOtrosDatos_NoSeReconoceNiMuestraLaFicha()
    {
        var id = Guid.NewGuid();
        _pacientes.AlBuscarParecidos = () => GuardarComoOtroEnvio(id, "Paciente Ficticio", OtroUsuario);

        var respuesta = await _servicio.RegistrarAsync(id, Adulto());

        Assert.False(respuesta.Exito);
        Assert.Equal(MensajesDePacientes.SolicitudYaUsada, respuesta.Mensaje);
        Assert.Null(respuesta.Datos);
    }

    [Fact]
    public async Task Registrar_ElReenvioNoOcultaOtrasFichasParecidasLegitimas_PeroSiLasMuestraSiNoHayReenvio()
    {
        // Control: una ficha de otra persona con el mismo nombre que aparece en ese momento, con otro número, sigue siendo un parecido por confirmar.
        _pacientes.AlBuscarParecidos = () => GuardarComoOtroEnvio(Guid.NewGuid());

        var respuesta = await _servicio.RegistrarAsync(Guid.NewGuid(), Adulto());

        Assert.False(respuesta.Exito);
        Assert.Equal(EstadoDeAlta.CoincidenciasPorConfirmar, respuesta.Datos!.Estado);
        Assert.Single(respuesta.Datos.Coincidencias!);
    }

    [Fact]
    public async Task Registrar_ConfirmandoQueEsOtraPersona_GuardaAunConCoincidencias()
    {
        Existente("María Muñoz");
        var idSolicitud = Guid.NewGuid();

        var respuesta = await _servicio.RegistrarAsync(idSolicitud, Adulto("Maria Munoz"), confirmaQueEsOtraPersona: true);

        Assert.True(respuesta.Exito);
        Assert.Equal(EstadoDeAlta.Registrado, respuesta.Datos!.Estado);
        Assert.Equal(2, _pacientes.Pacientes.Count);
        Assert.Equal(0, _pacientes.BusquedasDeParecidos);
    }

    [Fact]
    public async Task Registrar_UnaFichaDeBajaConElMismoNombre_NoCuentaComoCoincidencia()
    {
        var baja = Existente("María Muñoz");
        ReglasDePaciente.DarDeBaja(baja, 1, _idActor, Ahora);

        var respuesta = await _servicio.RegistrarAsync(Guid.NewGuid(), Adulto("Maria Munoz"));

        Assert.True(respuesta.Exito);
    }

    [Fact]
    public async Task Registrar_ElNacimientoDeUnaCoincidenciaAnteriorSeIndicaComoDesconocido()
    {
        var anterior = Existente("María Muñoz");
        SinNacimiento(anterior);

        var respuesta = await _servicio.RegistrarAsync(Guid.NewGuid(), Adulto("Maria Munoz"));

        var coincidencia = Assert.Single(respuesta.Datos!.Coincidencias!);
        Assert.Null(coincidencia.FechaNacimiento);
        Assert.True(coincidencia.NacimientoDesconocido);
    }

    [Fact]
    public async Task Registrar_ConMasDeDiezCoincidencias_MuestraDiezEIndicaQueHayMas()
    {
        for (var i = 0; i < LimitesDeBusquedaDePacientes.MaximoDeCoincidencias + 1; i++)
        {
            Existente("Ana Pérez", new DateOnly(1980, 1, 1).AddDays(i));
        }

        var respuesta = await _servicio.RegistrarAsync(Guid.NewGuid(), Adulto("Ana Perez"));

        Assert.Equal(LimitesDeBusquedaDePacientes.MaximoDeCoincidencias, respuesta.Datos!.Coincidencias!.Count);
        Assert.True(respuesta.Datos.HayMasCoincidencias);
    }

    [Fact]
    public async Task Registrar_ConElMismoNombreUnaSegundaVezSinConfirmar_LaPrimeraFichaNoCuentaComoCoincidenciaDeSuReenvio()
    {
        var idSolicitud = Guid.NewGuid();
        await _servicio.RegistrarAsync(idSolicitud, Adulto());
        var busquedasAntes = _pacientes.BusquedasDeParecidos;

        var reenvio = await _servicio.RegistrarAsync(idSolicitud, Adulto());

        Assert.Equal(EstadoDeAlta.YaRegistrado, reenvio.Datos!.Estado);
        Assert.Equal(busquedasAntes, _pacientes.BusquedasDeParecidos);
    }

    [Fact]
    public async Task Registrar_CuandoLaBaseRechazaElIdentificadorPorUnaCarrera_ConLosMismosDatosDevuelveYaRegistrado()
    {
        var idSolicitud = Guid.NewGuid();
        _pacientes.AlAgregar = candidato =>
        {
            // La competidora ganó: misma solicitud, mismos datos, misma persona.
            _pacientes.Pacientes[candidato.IdPaciente] = ReglasDePaciente.Registrar(
                candidato.IdPaciente, new DatosDePaciente("Paciente Ficticio", NacimientoAdulto, Correo: "canario@ficticio.test"),
                [AmbitoAtencion.Escolar], _idActor, Ahora);
            throw new IdentificadorDePacienteRepetidoException();
        };

        var respuesta = await _servicio.RegistrarAsync(idSolicitud, Adulto());

        Assert.True(respuesta.Exito);
        Assert.Equal(EstadoDeAlta.YaRegistrado, respuesta.Datos!.Estado);
        Assert.Equal(1, _unidad.Reversiones);
        Assert.Equal(0, _unidad.Confirmaciones);
        Assert.Empty(_repositorioAuditoria.Eventos);
    }

    [Fact]
    public async Task Registrar_CuandoLaBaseRechazaElIdentificadorPorUnaCarrera_ConOtrosDatosRechazaSinReconocerla()
    {
        _pacientes.AlAgregar = candidato =>
        {
            _pacientes.Pacientes[candidato.IdPaciente] = ReglasDePaciente.Registrar(
                candidato.IdPaciente, new DatosDePaciente("Otra Persona", NacimientoAdulto), [AmbitoAtencion.Escolar], _idActor, Ahora);
            throw new IdentificadorDePacienteRepetidoException();
        };

        var respuesta = await _servicio.RegistrarAsync(Guid.NewGuid(), Adulto());

        Assert.False(respuesta.Exito);
        Assert.Equal(MensajesDePacientes.SolicitudYaUsada, respuesta.Mensaje);
    }

    [Fact]
    public async Task Registrar_CuandoLaBaseRechazaElIdentificadorYSeRevocoElPermiso_RechazaSinPermiso()
    {
        _pacientes.AlAgregar = candidato =>
        {
            _pacientes.Pacientes[candidato.IdPaciente] = candidato;
            _autorizacion.Permisos.Remove(Permiso.RegistrarPacientes);
            throw new IdentificadorDePacienteRepetidoException();
        };

        var respuesta = await _servicio.RegistrarAsync(Guid.NewGuid(), Adulto());

        Assert.False(respuesta.Exito);
        Assert.Equal(MensajesDePacientes.SinPermiso, respuesta.Mensaje);
    }

    [Fact]
    public async Task Registrar_ConFalloInesperadoDeAuditoria_PropagaLaExcepcionYNoDaExito()
    {
        _repositorioAuditoria.ExcepcionAlAgregar = new InvalidOperationException("fallo de auditoría simulado");

        await Assert.ThrowsAsync<InvalidOperationException>(() => _servicio.RegistrarAsync(Guid.NewGuid(), Adulto()));

        Assert.Equal(1, _unidad.Reversiones);
        Assert.Equal(0, _unidad.Confirmaciones);
    }

    [Fact]
    public async Task Registrar_ConFalloInesperadoDeLaBase_NoSeConvierteEnMensajeDeValidacion()
    {
        _pacientes.AlAgregar = _ => throw new TimeoutException("base no disponible");

        await Assert.ThrowsAsync<TimeoutException>(() => _servicio.RegistrarAsync(Guid.NewGuid(), Adulto()));
    }

    [Fact]
    public async Task Registrar_ConUnaRegladeNegocioDeLaBase_DevuelveSuMensajeFijo()
    {
        _pacientes.AlAgregar = _ => throw new ReglaDeNegocioException("Mensaje fijo de la regla.");

        var respuesta = await _servicio.RegistrarAsync(Guid.NewGuid(), Adulto());

        Assert.False(respuesta.Exito);
        Assert.Equal("Mensaje fijo de la regla.", respuesta.Mensaje);
    }

    // ----- Buscar -----

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("a")]
    [InlineData("uno dos tres cuatro cinco seis siete")]
    [InlineData("Ana\u0007Perez")]
    public async Task Buscar_ConTextoInvalido_RechazaSinConsultar(string? texto)
    {
        var respuesta = await _servicio.BuscarAsync(texto);

        Assert.False(respuesta.Exito);
        Assert.Equal(MensajesDePacientes.BusquedaInvalida, respuesta.Mensaje);
        Assert.Equal(0, _pacientes.Lecturas);
    }

    [Fact]
    public async Task Buscar_ConMasDeCienCaracteres_Rechaza()
    {
        var respuesta = await _servicio.BuscarAsync(new string('a', LimitesDeBusquedaDePacientes.LongitudMaximaDelTexto + 1));

        Assert.Equal(MensajesDePacientes.BusquedaInvalida, respuesta.Mensaje);
    }

    [Fact]
    public async Task Buscar_ConCienCaracteresOSeisPalabras_EsValido()
    {
        var cien = await _servicio.BuscarAsync(new string('a', LimitesDeBusquedaDePacientes.LongitudMaximaDelTexto));
        var seis = await _servicio.BuscarAsync("uno dos tres cuatro cinco seis");

        Assert.True(cien.Exito);
        Assert.True(seis.Exito);
    }

    [Theory]
    [InlineData("maria")]
    [InlineData("MARÍA")]
    [InlineData("María")]
    [InlineData("  munoz  ")]
    [InlineData("MUÑOZ")]
    [InlineData("mari")]
    [InlineData("maria munoz")]
    [InlineData("munoz maria")]
    public async Task Buscar_NoDistingueAcentosNiMayusculasYExigeTodasLasPalabras(string texto)
    {
        var maria = Existente("María Muñoz");
        Existente("Luis Pérez");

        var respuesta = await _servicio.BuscarAsync(texto);

        Assert.True(respuesta.Exito);
        Assert.Equal(maria.IdPaciente, Assert.Single(respuesta.Datos!.Pacientes).IdPaciente);
        Assert.False(respuesta.Datos.HayMas);
    }

    [Theory]
    [InlineData("\u00D8scar", "\u00F8scar")]
    [InlineData("\u0416\u0443\u043A", "\u0436\u0443\u043A")]
    [InlineData("\u0130stanbul", "istanbul")]
    [InlineData("\u039F\u0394\u03A5\u03A3\u03A3\u0395\u03A5\u03A3", "\u03BF\u03B4\u03C5\u03C3\u03C3\u03B5\u03C5\u03C2")]
    public async Task Buscar_EncuentraUnNombreConMayusculasFueraDeAZEscribiendoSuMinuscula(string nombre, string texto)
    {
        var ficha = Existente(nombre);

        var respuesta = await _servicio.BuscarAsync(texto);

        Assert.Equal(ficha.IdPaciente, Assert.Single(respuesta.Datos!.Pacientes).IdPaciente);
    }

    [Fact]
    public async Task Buscar_ConUnaPalabraQueNoCoincide_NoDevuelveLaFicha()
    {
        Existente("María Muñoz");

        var respuesta = await _servicio.BuscarAsync("maria lopez");

        Assert.True(respuesta.Exito);
        Assert.Empty(respuesta.Datos!.Pacientes);
    }

    [Fact]
    public async Task Buscar_PasaAlRepositorioLasPalabrasNormalizadasYUnaFilaDeMasParaSaberSiHayMas()
    {
        await _servicio.BuscarAsync("  MARÍA   Muñoz ", new DateOnly(1990, 1, 15));

        var busqueda = _pacientes.UltimaBusqueda!.Value;
        Assert.Equal(["maria", "munoz"], busqueda.Palabras);
        Assert.Equal(new DateOnly(1990, 1, 15), busqueda.Nacimiento);
        Assert.Equal(LimitesDeBusquedaDePacientes.MaximoDeResultados + 1, busqueda.Limite);
    }

    [Fact]
    public async Task Buscar_SoloDevuelveFichasVigentes()
    {
        var vigente = Existente("Ana Pérez");
        var baja = Existente("Ana Pérez Baja");
        ReglasDePaciente.DarDeBaja(baja, 1, _idActor, Ahora);

        var respuesta = await _servicio.BuscarAsync("ana");

        Assert.Equal(vigente.IdPaciente, Assert.Single(respuesta.Datos!.Pacientes).IdPaciente);
    }

    [Fact]
    public async Task Buscar_FiltraPorNacimientoExacto()
    {
        var a = Existente("Ana Pérez", new DateOnly(1980, 1, 1));
        Existente("Ana Pérez", new DateOnly(1981, 1, 1));

        var respuesta = await _servicio.BuscarAsync("ana", new DateOnly(1980, 1, 1));

        Assert.Equal(a.IdPaciente, Assert.Single(respuesta.Datos!.Pacientes).IdPaciente);
    }

    [Fact]
    public async Task Buscar_MuestraElNacimientoSoloAQuienesComparteNombreConOtraFichaVigente()
    {
        var unica = Existente("Luis Pérez", new DateOnly(1975, 5, 5));
        var ana1 = Existente("Ana Pérez", new DateOnly(1980, 1, 1));
        var ana2 = Existente("ANA PEREZ", new DateOnly(1981, 2, 2));

        var respuesta = await _servicio.BuscarAsync("perez");

        var porId = respuesta.Datos!.Pacientes.ToDictionary(p => p.IdPaciente);
        Assert.Null(porId[unica.IdPaciente].FechaNacimiento);
        Assert.Equal(new DateOnly(1980, 1, 1), porId[ana1.IdPaciente].FechaNacimiento);
        Assert.Equal(new DateOnly(1981, 2, 2), porId[ana2.IdPaciente].FechaNacimiento);
    }

    [Fact]
    public async Task Buscar_UnNombreRepetidoCuyaFichaAnteriorNoTieneNacimiento_LoIndicaComoDesconocido()
    {
        Existente("Ana Pérez", new DateOnly(1980, 1, 1));
        var anterior = Existente("Ana Pérez");
        SinNacimiento(anterior);

        var respuesta = await _servicio.BuscarAsync("ana");

        var fila = respuesta.Datos!.Pacientes.Single(p => p.IdPaciente == anterior.IdPaciente);
        Assert.Null(fila.FechaNacimiento);
        Assert.True(fila.NacimientoDesconocido);
    }

    [Fact]
    public async Task Buscar_DevuelveUnOrdenEstablePorNombreNormalizadoYLuegoPorIdentificador()
    {
        Existente("Zoe Ramos");
        Existente("Ángel Ramos");
        Existente("Beto Ramos");

        var primera = await _servicio.BuscarAsync("ramos");
        var segunda = await _servicio.BuscarAsync("ramos");

        Assert.Equal(["Ángel Ramos", "Beto Ramos", "Zoe Ramos"], primera.Datos!.Pacientes.Select(p => p.Nombre));
        Assert.Equal(primera.Datos.Pacientes.Select(p => p.IdPaciente), segunda.Datos!.Pacientes.Select(p => p.IdPaciente));
    }

    [Fact]
    public async Task Buscar_ConMasDeCincuentaCoincidencias_DevuelveCincuentaEIndicaQueHayMas()
    {
        for (var i = 0; i < LimitesDeBusquedaDePacientes.MaximoDeResultados + 1; i++)
        {
            Existente($"Persona Ficticia {i:000}", new DateOnly(1980, 1, 1).AddDays(i));
        }

        var respuesta = await _servicio.BuscarAsync("persona");

        Assert.Equal(LimitesDeBusquedaDePacientes.MaximoDeResultados, respuesta.Datos!.Pacientes.Count);
        Assert.True(respuesta.Datos.HayMas);
    }

    [Fact]
    public async Task Buscar_ConExactamenteCincuentaCoincidencias_NoIndicaQueHayMas()
    {
        for (var i = 0; i < LimitesDeBusquedaDePacientes.MaximoDeResultados; i++)
        {
            Existente($"Persona Ficticia {i:000}", new DateOnly(1980, 1, 1).AddDays(i));
        }

        var respuesta = await _servicio.BuscarAsync("persona");

        Assert.Equal(LimitesDeBusquedaDePacientes.MaximoDeResultados, respuesta.Datos!.Pacientes.Count);
        Assert.False(respuesta.Datos.HayMas);
    }

    [Fact]
    public async Task Buscar_NoRegistraEventosNiGuardaElTextoBuscado()
    {
        Existente("María Muñoz");
        var eventosAntes = _repositorioAuditoria.Eventos.Count;

        await _servicio.BuscarAsync("texto-privado-buscado");
        await _servicio.BuscarAsync("maria");

        Assert.Equal(eventosAntes, _repositorioAuditoria.Eventos.Count);
        Assert.Equal(0, _unidad.Confirmaciones);
    }

    // ----- Obtener -----

    [Fact]
    public async Task Obtener_DeUnaFichaVigente_DevuelveElContenidoEdadCalculadaYUnaSolaLectura()
    {
        var idSolicitud = Guid.NewGuid();
        await _servicio.RegistrarAsync(idSolicitud, Menor() with { Ambitos = [AmbitoAtencion.Independiente, AmbitoAtencion.Escolar] });
        var lecturasAntes = _pacientes.Lecturas;

        var respuesta = await _servicio.ObtenerAsync(idSolicitud);

        Assert.True(respuesta.Exito);
        var ficha = respuesta.Datos!;
        Assert.Equal(idSolicitud, ficha.IdPaciente);
        Assert.Equal(1, ficha.Version);
        Assert.Equal("Menor Ficticio", ficha.Nombre);
        Assert.Equal(NacimientoMenor, ficha.FechaNacimiento);
        Assert.Equal(11, ficha.Edad);
        Assert.Equal("Escuela Ficticia", ficha.Escuela);
        Assert.Equal("3A", ficha.Grupo);
        Assert.Equal([AmbitoAtencion.Escolar, AmbitoAtencion.Independiente], ficha.Ambitos);
        Assert.Equal("Tutora Ficticia", ficha.Tutor!.Nombre);
        Assert.Equal("Madre", ficha.Tutor.Parentesco);
        Assert.Equal("6441234567", ficha.Tutor.Telefono);
        Assert.Equal(1, _pacientes.LecturasParaConsulta);
        Assert.Equal(lecturasAntes + 1, _pacientes.Lecturas);
    }

    [Fact]
    public async Task Obtener_UnaFichaAnteriorSinNacimiento_DevuelveEdadDesconocida()
    {
        var anterior = Existente();
        SinNacimiento(anterior);

        var respuesta = await _servicio.ObtenerAsync(anterior.IdPaciente);

        Assert.True(respuesta.Exito);
        Assert.Null(respuesta.Datos!.FechaNacimiento);
        Assert.Null(respuesta.Datos.Edad);
    }

    [Fact]
    public async Task Obtener_CalculaLaEdadConLaFechaDeHermosilloYNoConLaDeUtc()
    {
        // 6-oct 03:00 UTC es todavía 5-oct en Hermosillo: quien nació el 6-oct-2008 tiene 17 años y no 18.
        var tardeEnHermosillo = new DateTimeOffset(2026, 10, 6, 3, 0, 0, TimeSpan.Zero);
        var reloj = new RelojFijo(tardeEnHermosillo);
        var servicio = new ServicioPacientes(
            _pacientes, _unidad, new ServicioAuditoria(_repositorioAuditoria, _autorizacion, reloj), _autorizacion, _actual, reloj);
        var paciente = Existente("Persona Ficticia");
        ConNacimiento(paciente, new DateOnly(2008, 10, 6));

        var respuesta = await servicio.ObtenerAsync(paciente.IdPaciente);

        Assert.Equal(17, respuesta.Datos!.Edad);
    }

    [Fact]
    public async Task Obtener_UnaFichaDeBaja_SoloResponde_EstaDadaDeBaja_SinContenidoYConUnaSolaConsulta()
    {
        var paciente = Existente("Nombre Privado Muy Largo");
        ReglasDePaciente.DarDeBaja(paciente, 1, _idActor, Ahora);

        var respuesta = await _servicio.ObtenerAsync(paciente.IdPaciente);

        Assert.False(respuesta.Exito);
        Assert.Equal(MensajesDePacientes.FichaDeBaja, respuesta.Mensaje);
        Assert.Equal("Está dada de baja.", respuesta.Mensaje);
        Assert.Null(respuesta.Datos);
        Assert.DoesNotContain("Privado", respuesta.Mensaje);
        Assert.Equal(1, _pacientes.Lecturas);
        Assert.Equal(1, _pacientes.LecturasParaConsulta);
    }

    [Fact]
    public async Task Obtener_UnaFichaQueNoExiste_Informa()
    {
        var respuesta = await _servicio.ObtenerAsync(Guid.NewGuid());

        Assert.False(respuesta.Exito);
        Assert.Equal(MensajesDePacientes.PacienteNoEncontrado, respuesta.Mensaje);
    }

    [Fact]
    public async Task Obtener_ConIdentificadorVacio_InformaSinConsultar()
    {
        var respuesta = await _servicio.ObtenerAsync(Guid.Empty);

        Assert.Equal(MensajesDePacientes.PacienteNoEncontrado, respuesta.Mensaje);
        Assert.Equal(0, _pacientes.Lecturas);
    }

    [Fact]
    public async Task Obtener_NoRegistraEventosDeConsulta()
    {
        var paciente = Existente();

        await _servicio.ObtenerAsync(paciente.IdPaciente);

        Assert.Empty(_repositorioAuditoria.Eventos);
        Assert.Equal(0, _unidad.Confirmaciones);
    }

    // ----- Actualizar -----

    [Fact]
    public async Task Actualizar_ConLaVersionVigente_GuardaSubeLaVersionYRegistraUnEventoConLosCamposCambiados()
    {
        var paciente = Existente();

        var respuesta = await _servicio.ActualizarAsync(paciente.IdPaciente, 1, Adulto("Nombre Nuevo"));

        Assert.True(respuesta.Exito);
        Assert.Equal(EstadoDeEdicion.Actualizado, respuesta.Datos!.Estado);
        Assert.Equal(MensajesDePacientes.DatosActualizados, respuesta.Mensaje);
        Assert.Equal(2, respuesta.Datos.Ficha.Version);
        Assert.Equal("Nombre Nuevo", respuesta.Datos.Ficha.Nombre);
        Assert.Equal(2, paciente.Version);
        Assert.Equal(1, _unidad.Confirmaciones);
        var evento = Assert.Single(_repositorioAuditoria.Eventos);
        Assert.Equal(AccionAuditoria.Actualizacion, evento.Accion);
        Assert.Equal(paciente.IdPaciente, evento.IdRegistro);
        Assert.Equal(_idActor, evento.IdUsuarioActor);
        Assert.Equal([CamposDePaciente.Nombre], evento.CamposAfectados);
        Assert.DoesNotContain("Nuevo", evento.Hecho);
    }

    [Fact]
    public async Task Actualizar_LosCamposDelEventoSonSoloLosQueCambiaron_YElActorEsElDeLaSesion()
    {
        var paciente = Existente();
        _actual.IdUsuario = OtroUsuario;
        var datos = Adulto() with { Telefono = "6441234567", Escuela = "Escuela Nueva", Ambitos = [AmbitoAtencion.Independiente] };

        await _servicio.ActualizarAsync(paciente.IdPaciente, 1, datos);

        var evento = Assert.Single(_repositorioAuditoria.Eventos);
        Assert.Equal(
            [CamposDePaciente.Telefono, CamposDePaciente.Escuela, CamposDePaciente.Ambitos],
            evento.CamposAfectados);
        Assert.Equal(OtroUsuario, evento.IdUsuarioActor);
        Assert.Equal(OtroUsuario, paciente.IdUsuarioActualizacion);
    }

    [Fact]
    public async Task Actualizar_AlQuitarUnAmbito_PideAlRepositorioBorrarlo()
    {
        var paciente = Existente("Paciente Ficticio", null, null, AmbitoAtencion.Escolar, AmbitoAtencion.Independiente);
        var independiente = paciente.Ambitos.Single(a => a.Ambito == AmbitoAtencion.Independiente);

        var respuesta = await _servicio.ActualizarAsync(paciente.IdPaciente, 1, Adulto("Paciente Ficticio", AmbitoAtencion.Escolar));

        Assert.True(respuesta.Exito);
        Assert.Same(independiente, Assert.Single(_pacientes.AmbitosQuitados));
        Assert.Equal([AmbitoAtencion.Escolar], respuesta.Datos!.Ficha.Ambitos);
    }

    [Fact]
    public async Task Actualizar_SinCambiosConLaVersionVigente_NoEscribeNiAuditaNiSubeLaVersion()
    {
        var paciente = Existente();

        var respuesta = await _servicio.ActualizarAsync(paciente.IdPaciente, 1, Adulto());

        Assert.True(respuesta.Exito);
        Assert.Equal(EstadoDeEdicion.SinCambios, respuesta.Datos!.Estado);
        Assert.Equal(MensajesDePacientes.SinCambios, respuesta.Mensaje);
        Assert.Equal(1, respuesta.Datos.Ficha.Version);
        Assert.Equal(1, paciente.Version);
        Assert.Equal(0, _unidad.Confirmaciones);
        Assert.Empty(_repositorioAuditoria.Eventos);
        Assert.Empty(_pacientes.AmbitosQuitados);
    }

    [Fact]
    public async Task Actualizar_ConVersionAnteriorYDatosIguales_DaConflictoAunSinCambios()
    {
        var paciente = Existente();
        await _servicio.ActualizarAsync(paciente.IdPaciente, 1, Adulto("Nombre Nuevo"));
        var eventosAntes = _repositorioAuditoria.Eventos.Count;

        var respuesta = await _servicio.ActualizarAsync(paciente.IdPaciente, 1, Adulto("Nombre Nuevo"));

        Assert.False(respuesta.Exito);
        Assert.Equal(MensajesDePacientes.ConflictoDeVersion, respuesta.Mensaje);
        Assert.Null(respuesta.Datos);
        Assert.Equal(eventosAntes, _repositorioAuditoria.Eventos.Count);
        Assert.Equal(2, paciente.Version);
    }

    [Fact]
    public async Task Actualizar_ConVersionAnteriorYDatosDistintos_DaConflictoYNoCambiaNada()
    {
        var paciente = Existente();
        await _servicio.ActualizarAsync(paciente.IdPaciente, 1, Adulto("Primera Edicion"));

        var respuesta = await _servicio.ActualizarAsync(paciente.IdPaciente, 1, Adulto("Segunda Edicion"));

        Assert.Equal(MensajesDePacientes.ConflictoDeVersion, respuesta.Mensaje);
        Assert.Equal("Primera Edicion", paciente.Nombre);
        Assert.Single(_repositorioAuditoria.Eventos);
    }

    [Fact]
    public async Task Actualizar_UnaFichaDeBaja_RechazaSinLeerSuContenido()
    {
        var paciente = Existente();
        ReglasDePaciente.DarDeBaja(paciente, 1, _idActor, Ahora);

        var respuesta = await _servicio.ActualizarAsync(paciente.IdPaciente, 2, Adulto("Nombre Nuevo"));

        Assert.False(respuesta.Exito);
        Assert.Equal(MensajesDePacientes.FichaDeBajaNoSeActualiza, respuesta.Mensaje);
        Assert.Equal(1, _pacientes.LecturasDeCondicion);
        Assert.Equal(0, _pacientes.LecturasParaEscribir);
        Assert.Equal("Paciente Ficticio", paciente.Nombre);
        Assert.Equal(0, _unidad.Confirmaciones);
    }

    [Fact]
    public async Task Actualizar_UnaFichaQueNoExiste_Informa()
    {
        var respuesta = await _servicio.ActualizarAsync(Guid.NewGuid(), 1, Adulto());

        Assert.Equal(MensajesDePacientes.PacienteNoEncontrado, respuesta.Mensaje);
        Assert.Equal(0, _unidad.Confirmaciones);
    }

    [Fact]
    public async Task Actualizar_ConDatosNulosOIdentificadorVacio_RechazaComoSolicitudInvalida()
    {
        var paciente = Existente();

        var sinDatos = await _servicio.ActualizarAsync(paciente.IdPaciente, 1, null!);
        var sinIdentificador = await _servicio.ActualizarAsync(Guid.Empty, 1, Adulto());

        Assert.Equal(MensajesDePacientes.SolicitudInvalida, sinDatos.Mensaje);
        Assert.Equal(MensajesDePacientes.SolicitudInvalida, sinIdentificador.Mensaje);
    }

    [Fact]
    public async Task Actualizar_ConDatosInvalidos_DevuelveElMensajeDeLaReglaYNoGuardaNiAudita()
    {
        var paciente = Existente();

        var respuesta = await _servicio.ActualizarAsync(paciente.IdPaciente, 1, Adulto("   "));

        Assert.Equal(MensajesDeDatosDePaciente.NombreInvalido, respuesta.Mensaje);
        Assert.Equal("Paciente Ficticio", paciente.Nombre);
        Assert.Equal(1, paciente.Version);
        Assert.Equal(0, _unidad.Confirmaciones);
        Assert.Empty(_repositorioAuditoria.Eventos);
    }

    [Fact]
    public async Task Actualizar_UnaFichaAnteriorSinNacimientoConUnCambioReal_ExigeCompletarloYNoGuarda()
    {
        var anterior = Existente();
        SinNacimiento(anterior);

        var respuesta = await _servicio.ActualizarAsync(anterior.IdPaciente, 1, Adulto("Nombre Nuevo") with { FechaNacimiento = null });

        Assert.Equal(MensajesDeDatosDePaciente.NacimientoObligatorio, respuesta.Mensaje);
        Assert.Equal("Paciente Ficticio", anterior.Nombre);
        Assert.Equal(1, anterior.Version);
        Assert.Empty(_repositorioAuditoria.Eventos);
    }

    [Fact]
    public async Task Actualizar_UnaFichaAnteriorSinCambios_NoObligaAInventarNacimiento()
    {
        var anterior = Existente();
        SinNacimiento(anterior);

        var respuesta = await _servicio.ActualizarAsync(anterior.IdPaciente, 1, Adulto() with { FechaNacimiento = null });

        Assert.True(respuesta.Exito);
        Assert.Equal(EstadoDeEdicion.SinCambios, respuesta.Datos!.Estado);
        Assert.Null(respuesta.Datos.Ficha.FechaNacimiento);
        Assert.Null(respuesta.Datos.Ficha.Edad);
    }

    [Fact]
    public async Task Actualizar_PasarDeAdultoAMenorExigeTutorEnLaMismaEdicion()
    {
        var paciente = Existente();

        var sinTutor = await _servicio.ActualizarAsync(paciente.IdPaciente, 1, Adulto() with { FechaNacimiento = NacimientoMenor });
        var conTutor = await _servicio.ActualizarAsync(
            paciente.IdPaciente, 1,
            Adulto() with { FechaNacimiento = NacimientoMenor, Tutor = new DatosDeTutorEntrada("Tutora Ficticia", "Madre", null, "tutora@ficticio.test") });

        Assert.Equal(MensajesDeDatosDePaciente.TutorObligatorioParaMenores, sinTutor.Mensaje);
        Assert.True(conTutor.Exito);
        Assert.Equal(2, paciente.Version);
        var evento = Assert.Single(_repositorioAuditoria.Eventos);
        Assert.Contains(CamposDePaciente.FechaNacimiento, evento.CamposAfectados);
        Assert.Contains(CamposDePaciente.TutorNombre, evento.CamposAfectados);
    }

    [Fact]
    public async Task Actualizar_CuandoLaBaseDetectaUnConflictoDeVersion_DevuelveElMensajeDeConflicto()
    {
        var paciente = Existente();
        var servicio = Crear(new UnidadDeTrabajoQueRechazaPorVersion());

        var respuesta = await servicio.ActualizarAsync(paciente.IdPaciente, 1, Adulto("Nombre Nuevo"));

        Assert.False(respuesta.Exito);
        Assert.Equal(MensajesDePacientes.ConflictoDeVersion, respuesta.Mensaje);
    }

    [Fact]
    public async Task Actualizar_ConFalloInesperadoDeAuditoria_PropagaLaExcepcionYNoDaExito()
    {
        var paciente = Existente();
        _repositorioAuditoria.ExcepcionAlAgregar = new InvalidOperationException("fallo de auditoría simulado");

        await Assert.ThrowsAsync<InvalidOperationException>(() => _servicio.ActualizarAsync(paciente.IdPaciente, 1, Adulto("Nombre Nuevo")));

        Assert.Equal(1, _unidad.Reversiones);
        Assert.Equal(0, _unidad.Confirmaciones);
    }

    // ----- Resultados -----

    [Fact]
    public void LosModelosNoImprimenValoresPersonalesEnSuTexto()
    {
        var entrada = new DatosDePacienteEntrada(
            "Nombre Privado", NacimientoAdulto, "6441234567", "privado@ficticio.test", "Escuela Privada", "3A", [AmbitoAtencion.Escolar],
            new DatosDeTutorEntrada("Tutor Privado", "Madre", "6449999999", "tutor@ficticio.test"));
        var ficha = new FichaDePaciente(
            Guid.NewGuid(), 1, "Nombre Privado", NacimientoAdulto, 36, "6441234567", "privado@ficticio.test", "Escuela Privada", "3A",
            [AmbitoAtencion.Escolar], entrada.Tutor);
        var textos = new object[]
        {
            entrada, entrada.Tutor!, ficha, new ResumenDePaciente(Guid.NewGuid(), "Nombre Privado", NacimientoAdulto),
            new ResultadoDeBusqueda([new ResumenDePaciente(Guid.NewGuid(), "Nombre Privado")], false),
            new ResultadoDeAlta(EstadoDeAlta.Registrado, Guid.NewGuid()), new ResultadoDeEdicion(EstadoDeEdicion.Actualizado, ficha),
            new LecturaDePaciente(Guid.NewGuid(), CondicionRegistro.Vigente, 1, "Nombre Privado", NacimientoAdulto, null, null, null, null, null, null, null, null, true, false),
            new FilaDeBusquedaDePaciente(Guid.NewGuid(), "Nombre Privado", NacimientoAdulto, true),
            new ParecidoDePaciente(Guid.NewGuid(), "Nombre Privado", NacimientoAdulto)
        }.Select(o => o.ToString()!);

        Assert.All(textos, texto =>
        {
            Assert.DoesNotContain("Privad", texto);
            Assert.DoesNotContain("ficticio.test", texto);
            Assert.DoesNotContain("644", texto);
        });
    }

    private sealed class UnidadDeTrabajoQueRechazaPorVersion : IUnidadDeTrabajo
    {
        public async Task EjecutarAsync(Func<CancellationToken, Task> operacion, CancellationToken cancelacion = default)
        {
            await operacion(cancelacion);
            throw new ConflictoDeVersionException();
        }
    }
}
