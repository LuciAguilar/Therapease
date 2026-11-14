using TherapEase.Application.Pacientes.Constantes;
using TherapEase.Application.Pacientes.Servicios;
using TherapEase.Domain.Compartido.Entidades.Enums;
using TherapEase.Domain.Pacientes.Entidades;
using TherapEase.Domain.Pacientes.Entidades.Enums;
using TherapEase.Domain.Pacientes.Reglas;
using TherapEase.UnitTests.Dobles;

namespace TherapEase.UnitTests.Application.Pacientes;

public class ConsultaDePacientesParaCitasTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 10, 5, 15, 0, 0, TimeSpan.Zero);

    private readonly RepositorioPacientesFalso _pacientes = new();
    private readonly AutorizacionFalsa _autorizacion = new();
    private readonly UsuarioActualFalso _actual = new();
    private readonly Guid _idActor = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly ConsultaDePacientesParaCitas _consulta;

    public ConsultaDePacientesParaCitasTests()
    {
        _actual.IdUsuario = _idActor;
        _autorizacion.Permisos.Add(Permiso.ConsultarPacientes);
        _consulta = new ConsultaDePacientesParaCitas(_pacientes, _autorizacion, _actual);
    }

    private Paciente Existente(string nombre, params AmbitoAtencion[] ambitos)
    {
        var datos = new DatosDePaciente(nombre, new DateOnly(1990, 1, 15), Correo: "canario@ficticio.test");
        var paciente = ReglasDePaciente.Registrar(Guid.NewGuid(), datos, ambitos.Length == 0 ? [AmbitoAtencion.Escolar] : ambitos, _idActor, Ahora);
        _pacientes.Pacientes[paciente.IdPaciente] = paciente;
        return paciente;
    }

    private static void DarDeBaja(Paciente paciente) => ReglasDePaciente.DarDeBaja(paciente, paciente.Version, Guid.NewGuid(), Ahora);

    [Fact]
    public async Task Condicion_SinElPermisoDeConsulta_RechazaAntesDeLeerNada()
    {
        var paciente = Existente("Paciente Ficticio");
        _autorizacion.Permisos.Remove(Permiso.ConsultarPacientes);

        var respuesta = await _consulta.ConsultarCondicionYAmbitosAsync(paciente.IdPaciente);

        Assert.False(respuesta.Exito);
        Assert.Equal(MensajesDePacientes.SinPermiso, respuesta.Mensaje);
        Assert.Null(respuesta.Datos);
        Assert.Equal(0, _pacientes.Lecturas);
    }

    [Fact]
    public async Task Condicion_SinSesion_RechazaAntesDeLeerNada()
    {
        var paciente = Existente("Paciente Ficticio");
        _actual.IdUsuario = null;

        var respuesta = await _consulta.ConsultarCondicionYAmbitosAsync(paciente.IdPaciente);

        Assert.Equal(MensajesDePacientes.SinPermiso, respuesta.Mensaje);
        Assert.Equal(0, _pacientes.Lecturas);
    }

    [Fact]
    public async Task Condicion_DeUnPacienteVigente_DevuelveSuCondicionYSusAmbitos()
    {
        var paciente = Existente("Paciente Ficticio", AmbitoAtencion.Escolar, AmbitoAtencion.Independiente);

        var respuesta = await _consulta.ConsultarCondicionYAmbitosAsync(paciente.IdPaciente);

        Assert.True(respuesta.Exito);
        Assert.Equal(CondicionRegistro.Vigente, respuesta.Datos!.Condicion);
        Assert.Equal([AmbitoAtencion.Escolar, AmbitoAtencion.Independiente], respuesta.Datos.Ambitos);
        Assert.Equal(1, _pacientes.LecturasDeCondicionYAmbitos);
        Assert.Equal(1, _pacientes.Lecturas);
    }

    [Theory]
    [InlineData(AmbitoAtencion.Escolar)]
    [InlineData(AmbitoAtencion.Independiente)]
    public async Task Condicion_DeUnPacienteConUnSoloAmbito_DevuelveSoloEse(AmbitoAtencion ambito)
    {
        var paciente = Existente("Paciente Ficticio", ambito);

        var respuesta = await _consulta.ConsultarCondicionYAmbitosAsync(paciente.IdPaciente);

        Assert.Equal([ambito], respuesta.Datos!.Ambitos);
    }

    // De una baja se entrega solo la condición: los ámbitos son parte de la ficha.
    [Fact]
    public async Task Condicion_DeUnPacienteDeBaja_DevuelveLaBajaSinAmbitos()
    {
        var paciente = Existente("Paciente Ficticio", AmbitoAtencion.Escolar, AmbitoAtencion.Independiente);
        DarDeBaja(paciente);

        var respuesta = await _consulta.ConsultarCondicionYAmbitosAsync(paciente.IdPaciente);

        Assert.True(respuesta.Exito);
        Assert.Equal(CondicionRegistro.Baja, respuesta.Datos!.Condicion);
        Assert.Empty(respuesta.Datos.Ambitos);
    }

    [Fact]
    public async Task Condicion_DeUnPacienteInexistenteOConIdentificadorVacio_TieneCondicionNula()
    {
        var inexistente = await _consulta.ConsultarCondicionYAmbitosAsync(Guid.NewGuid());
        var vacio = await _consulta.ConsultarCondicionYAmbitosAsync(Guid.Empty);

        Assert.True(inexistente.Exito);
        Assert.Null(inexistente.Datos!.Condicion);
        Assert.Empty(inexistente.Datos.Ambitos);
        Assert.Null(vacio.Datos!.Condicion);
    }

    [Fact]
    public async Task Condicion_NoEntregaNingunDatoPersonal()
    {
        var paciente = Existente("Nombre Privado");

        var respuesta = await _consulta.ConsultarCondicionYAmbitosAsync(paciente.IdPaciente);

        Assert.DoesNotContain("Nombre Privado", respuesta.Datos!.ToString());
        Assert.Equal(nameof(TherapEase.Application.Pacientes.Modelos.CondicionYAmbitosDePaciente), respuesta.Datos.ToString());
    }

    [Fact]
    public async Task Nombres_SinElPermisoDeConsulta_RechazaAntesDeLeerNada()
    {
        var paciente = Existente("Paciente Ficticio");
        _autorizacion.Permisos.Remove(Permiso.ConsultarPacientes);

        var respuesta = await _consulta.ConsultarNombresParaAgendaAsync([paciente.IdPaciente]);

        Assert.False(respuesta.Exito);
        Assert.Equal(MensajesDePacientes.SinPermiso, respuesta.Mensaje);
        Assert.Equal(0, _pacientes.Lecturas);
    }

    [Fact]
    public async Task Nombres_DevuelveSoloLosDePacientesVigentesEnUnaSolaConsulta()
    {
        var vigente = Existente("Paciente Uno");
        var otro = Existente("Paciente Dos");
        var deBaja = Existente("Nombre Privado");
        DarDeBaja(deBaja);

        var respuesta = await _consulta.ConsultarNombresParaAgendaAsync([vigente.IdPaciente, otro.IdPaciente, deBaja.IdPaciente, Guid.NewGuid()]);

        Assert.True(respuesta.Exito);
        Assert.Equal(2, respuesta.Datos!.Count);
        Assert.Equal("Paciente Uno", respuesta.Datos[vigente.IdPaciente]);
        Assert.Equal("Paciente Dos", respuesta.Datos[otro.IdPaciente]);
        Assert.False(respuesta.Datos.ContainsKey(deBaja.IdPaciente));
        Assert.Equal(1, _pacientes.LecturasDeNombres);
        Assert.Equal(1, _pacientes.Lecturas);
    }

    [Fact]
    public async Task Nombres_QuitaRepetidosYVaciosAntesDeConsultar()
    {
        var paciente = Existente("Paciente Uno");

        var respuesta = await _consulta.ConsultarNombresParaAgendaAsync([paciente.IdPaciente, paciente.IdPaciente, Guid.Empty]);

        Assert.Equal([paciente.IdPaciente], _pacientes.UltimosIdsDeNombres!);
        Assert.Single(respuesta.Datos!);
    }

    [Fact]
    public async Task Nombres_SinIdentificadores_NoConsultaLaBase()
    {
        var respuesta = await _consulta.ConsultarNombresParaAgendaAsync([]);

        Assert.True(respuesta.Exito);
        Assert.Empty(respuesta.Datos!);
        Assert.Equal(0, _pacientes.Lecturas);
    }
}
