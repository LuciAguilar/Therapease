using TherapEase.Application.Auditoria.Servicios;
using TherapEase.Application.Compartido.Interfaces.Repositorios;
using TherapEase.Application.Coordinadores.Constantes;
using TherapEase.Application.Coordinadores.Servicios;
using TherapEase.Domain.Auditoria.Entidades.Enums;
using TherapEase.Domain.Citas.Entidades;
using TherapEase.Domain.Citas.Entidades.Enums;
using TherapEase.Domain.Citas.Excepciones;
using TherapEase.Domain.Citas.Reglas;
using TherapEase.Domain.Compartido.Entidades.Enums;
using TherapEase.Domain.Compartido.Excepciones;
using TherapEase.Domain.Pacientes.Entidades;
using TherapEase.Domain.Pacientes.Entidades.Enums;
using TherapEase.Domain.Pacientes.Reglas;
using TherapEase.UnitTests.Dobles;

namespace TherapEase.UnitTests.Application.Coordinadores;

public class CoordinadorDeBajaDePacienteTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 10, 12, 15, 0, 0, TimeSpan.Zero);

    private readonly RepositorioPacientesFalso _pacientes = new();
    private readonly RepositorioCitasFalso _citas = new();
    private readonly UnidadDeTrabajoFalsa _unidad = new();
    private readonly RepositorioAuditoriaFalso _repositorioAuditoria = new();
    private readonly AutorizacionFalsa _autorizacion = new();
    private readonly UsuarioActualFalso _actual = new();
    private readonly Guid _idActor = Guid.NewGuid();
    private readonly CoordinadorDeBajaDePaciente _coordinador;

    public CoordinadorDeBajaDePacienteTests()
    {
        _actual.IdUsuario = _idActor;
        _autorizacion.Permisos.Add(Permiso.DarDeBajaPacientes);
        _coordinador = Crear(_unidad);
    }

    private CoordinadorDeBajaDePaciente Crear(IUnidadDeTrabajo unidad)
    {
        var reloj = new RelojFijo(Ahora);
        var auditoria = new ServicioAuditoria(_repositorioAuditoria, _autorizacion, reloj);
        return new CoordinadorDeBajaDePaciente(_pacientes, _citas, unidad, auditoria, _autorizacion, _actual, reloj);
    }

    private Paciente AgregarPaciente()
    {
        var paciente = ReglasDePaciente.Registrar(Guid.NewGuid(), "Paciente ficticio", null, [AmbitoAtencion.Escolar], _idActor, Ahora);
        _pacientes.Pacientes[paciente.IdPaciente] = paciente;
        return paciente;
    }

    private Cita AgregarCita(Paciente paciente, int hora)
    {
        var inicio = Ahora.AddDays(3).AddHours(hora);
        var cita = ReglasDeCita.Agendar(Guid.NewGuid(), paciente.IdPaciente, AmbitoAtencion.Escolar, inicio, inicio.AddHours(1), _idActor, Ahora);
        _citas.Citas[cita.IdCita] = cita;
        return cita;
    }

    private sealed class UnidadDeTrabajoQueRechazaAlConfirmar : IUnidadDeTrabajo
    {
        public async Task EjecutarAsync(Func<CancellationToken, Task> operacion, CancellationToken cancelacion = default)
        {
            await operacion(cancelacion);
            throw new CitaActivaConPacienteDeBajaException();
        }
    }

    [Fact]
    public async Task DarDeBaja_SinCitasActivas_DaDeBajaAlPacienteYDevuelveSuNuevaVersion()
    {
        var paciente = AgregarPaciente();

        var respuesta = await _coordinador.DarDeBajaAsync(paciente.IdPaciente, paciente.Version);

        Assert.True(respuesta.Exito);
        Assert.Equal(CondicionRegistro.Baja, paciente.Condicion);
        Assert.Equal(2, paciente.Version);
        Assert.Equal(paciente.IdPaciente, respuesta.Datos!.IdPaciente);
        Assert.Equal(2, respuesta.Datos.Version);
        Assert.Equal(1, _unidad.Confirmaciones);
    }

    [Fact]
    public async Task DarDeBaja_RegistraUnEventoDelActorSinDatosDelPaciente()
    {
        var paciente = AgregarPaciente();

        await _coordinador.DarDeBajaAsync(paciente.IdPaciente, paciente.Version);

        var evento = Assert.Single(_repositorioAuditoria.Eventos);
        Assert.Equal(_idActor, evento.IdUsuarioActor);
        Assert.Equal(TipoRegistroAuditoria.Paciente, evento.TipoRegistro);
        Assert.Equal(paciente.IdPaciente, evento.IdRegistro);
        Assert.Equal(AccionAuditoria.Baja, evento.Accion);
        Assert.Equal(Ahora, evento.FechaEvento);
        Assert.DoesNotContain(paciente.Nombre, evento.Hecho);
    }

    [Fact]
    public async Task DarDeBaja_ConPacienteSinCitasActivasPeroConCanceladaYDeBaja_DaDeBaja()
    {
        var paciente = AgregarPaciente();
        var cancelada = AgregarCita(paciente, hora: 9);
        ReglasDeCita.Cancelar(cancelada, cancelada.Version, _idActor, Ahora);
        var deBaja = AgregarCita(paciente, hora: 11);
        ReglasDeCita.DarDeBaja(deBaja, deBaja.Version, _idActor, Ahora);

        var respuesta = await _coordinador.DarDeBajaAsync(paciente.IdPaciente, paciente.Version);

        Assert.True(respuesta.Exito);
        Assert.Equal(CondicionRegistro.Baja, paciente.Condicion);
    }

    [Fact]
    public async Task DarDeBaja_ConCitaActiva_RechazaSinCancelarNiAuditarNiCambiarNada()
    {
        var paciente = AgregarPaciente();
        var cita = AgregarCita(paciente, hora: 9);

        var respuesta = await _coordinador.DarDeBajaAsync(paciente.IdPaciente, paciente.Version);

        Assert.False(respuesta.Exito);
        Assert.Equal(MensajesDeCoordinacion.PacienteConCitasActivas, respuesta.Mensaje);
        Assert.Equal(CondicionRegistro.Vigente, paciente.Condicion);
        Assert.Equal(1, paciente.Version);
        Assert.Equal(EstadoCita.Agendada, cita.Estado);
        Assert.Equal(CondicionRegistro.Vigente, cita.Condicion);
        Assert.Empty(_repositorioAuditoria.Eventos);
        Assert.Equal(0, _unidad.Confirmaciones);
    }

    [Fact]
    public async Task DarDeBaja_SinPermiso_Rechaza()
    {
        var paciente = AgregarPaciente();
        _autorizacion.Permisos.Clear();

        var respuesta = await _coordinador.DarDeBajaAsync(paciente.IdPaciente, paciente.Version);

        Assert.False(respuesta.Exito);
        Assert.Equal(MensajesDeCoordinacion.SinPermiso, respuesta.Mensaje);
        Assert.Equal(CondicionRegistro.Vigente, paciente.Condicion);
        Assert.Empty(_repositorioAuditoria.Eventos);
    }

    [Fact]
    public async Task DarDeBaja_SinSesion_Rechaza()
    {
        var paciente = AgregarPaciente();
        _actual.IdUsuario = null;

        var respuesta = await _coordinador.DarDeBajaAsync(paciente.IdPaciente, paciente.Version);

        Assert.False(respuesta.Exito);
        Assert.Equal(MensajesDeCoordinacion.SinPermiso, respuesta.Mensaje);
        Assert.Equal(CondicionRegistro.Vigente, paciente.Condicion);
    }

    [Fact]
    public async Task DarDeBaja_ConPacienteInexistente_Rechaza()
    {
        var respuesta = await _coordinador.DarDeBajaAsync(Guid.NewGuid(), 1);

        Assert.False(respuesta.Exito);
        Assert.Equal(MensajesDeCoordinacion.PacienteNoEncontrado, respuesta.Mensaje);
        Assert.Empty(_repositorioAuditoria.Eventos);
    }

    [Fact]
    public async Task DarDeBaja_ConVersionDesactualizada_RechazaSinSobrescribir()
    {
        var paciente = AgregarPaciente();

        var respuesta = await _coordinador.DarDeBajaAsync(paciente.IdPaciente, paciente.Version + 1);

        Assert.False(respuesta.Exito);
        Assert.Equal(new ConflictoDeVersionException().Message, respuesta.Mensaje);
        Assert.Equal(CondicionRegistro.Vigente, paciente.Condicion);
        Assert.Empty(_repositorioAuditoria.Eventos);
        Assert.Equal(1, _unidad.Reversiones);
    }

    [Fact]
    public async Task DarDeBaja_ConPacienteYaDeBaja_InformaSuCondicionSinNuevoEvento()
    {
        var paciente = AgregarPaciente();
        await _coordinador.DarDeBajaAsync(paciente.IdPaciente, paciente.Version);
        _repositorioAuditoria.Eventos.Clear();

        var respuesta = await _coordinador.DarDeBajaAsync(paciente.IdPaciente, paciente.Version);

        Assert.False(respuesta.Exito);
        Assert.Contains("ya está dada de baja", respuesta.Mensaje);
        Assert.Empty(_repositorioAuditoria.Eventos);
    }

    [Fact]
    public async Task DarDeBaja_SiLaBaseRechazaAlConfirmar_NoReportaExito()
    {
        var paciente = AgregarPaciente();
        var coordinador = Crear(new UnidadDeTrabajoQueRechazaAlConfirmar());

        var respuesta = await coordinador.DarDeBajaAsync(paciente.IdPaciente, paciente.Version);

        Assert.False(respuesta.Exito);
        Assert.Equal(new CitaActivaConPacienteDeBajaException().Message, respuesta.Mensaje);
    }

    [Fact]
    public async Task DarDeBaja_SiFallaLaAuditoria_NoConfirmaYPropagaLaExcepcion()
    {
        var paciente = AgregarPaciente();
        _repositorioAuditoria.ExcepcionAlAgregar = new InvalidOperationException("fallo simulado");

        await Assert.ThrowsAsync<InvalidOperationException>(() => _coordinador.DarDeBajaAsync(paciente.IdPaciente, paciente.Version));

        Assert.Equal(0, _unidad.Confirmaciones);
        Assert.Equal(1, _unidad.Reversiones);
    }
}
