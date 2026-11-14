using TherapEase.Application.Compartido.Modelos;
using TherapEase.Application.Pacientes.Constantes;
using TherapEase.Application.Pacientes.Interfaces.Servicios;
using TherapEase.Application.Pacientes.Modelos;
using TherapEase.Domain.Compartido.Entidades.Enums;
using TherapEase.Domain.Pacientes.Entidades.Enums;

namespace TherapEase.UnitTests.Dobles;

// Contrato de Pacientes para Citas con lo mínimo que las pruebas del servicio de citas necesitan controlar: qué paciente existe, su condición y sus ámbitos.
internal sealed class ConsultaDePacientesParaCitasFalsa : IConsultaDePacientesParaCitas
{
    public Dictionary<Guid, (CondicionRegistro Condicion, string Nombre, AmbitoAtencion[] Ambitos)> Pacientes { get; } = [];

    // Simula que Pacientes niega el permiso en el momento de la consulta.
    public bool SinPermiso { get; set; }

    // Se ejecuta una sola vez, justo antes de responder la condición del paciente: es el momento entre la primera lectura de la cita y esa comprobación.
    public Action? AntesDeConsultarCondicion { get; set; }

    public int ConsultasDeCondicion { get; private set; }

    public int ConsultasDeNombres { get; private set; }

    public Guid[]? UltimosIdsDeNombres { get; private set; }

    public Guid Agregar(string nombre = "Paciente Ficticio", CondicionRegistro condicion = CondicionRegistro.Vigente, params AmbitoAtencion[] ambitos)
    {
        var id = Guid.NewGuid();
        Pacientes[id] = (condicion, nombre, ambitos.Length == 0 ? [AmbitoAtencion.Escolar] : ambitos);
        return id;
    }

    public Task<RespuestaServicio<CondicionYAmbitosDePaciente>> ConsultarCondicionYAmbitosAsync(Guid idPaciente, CancellationToken cancelacion = default)
    {
        ConsultasDeCondicion++;
        var gancho = AntesDeConsultarCondicion;
        AntesDeConsultarCondicion = null;
        gancho?.Invoke();

        if (SinPermiso)
        {
            return Task.FromResult(RespuestaServicio<CondicionYAmbitosDePaciente>.Fallida(MensajesDePacientes.SinPermiso));
        }

        // Igual que el contrato real: de una baja los ámbitos van vacíos; un paciente inexistente tiene condición nula.
        var datos = Pacientes.TryGetValue(idPaciente, out var p)
            ? new CondicionYAmbitosDePaciente(p.Condicion, p.Condicion == CondicionRegistro.Vigente ? p.Ambitos : [])
            : new CondicionYAmbitosDePaciente(null, []);
        return Task.FromResult(RespuestaServicio<CondicionYAmbitosDePaciente>.Correcta(datos));
    }

    public Task<RespuestaServicio<IReadOnlyDictionary<Guid, string>>> ConsultarNombresParaAgendaAsync(
        IReadOnlyCollection<Guid> idsPacientes, CancellationToken cancelacion = default)
    {
        ConsultasDeNombres++;
        UltimosIdsDeNombres = idsPacientes.ToArray();

        if (SinPermiso)
        {
            return Task.FromResult(RespuestaServicio<IReadOnlyDictionary<Guid, string>>.Fallida(MensajesDePacientes.SinPermiso));
        }

        IReadOnlyDictionary<Guid, string> nombres = idsPacientes
            .Where(id => Pacientes.TryGetValue(id, out var p) && p.Condicion == CondicionRegistro.Vigente)
            .Distinct()
            .ToDictionary(id => id, id => Pacientes[id].Nombre);
        return Task.FromResult(RespuestaServicio<IReadOnlyDictionary<Guid, string>>.Correcta(nombres));
    }
}
