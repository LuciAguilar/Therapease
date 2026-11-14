using TherapEase.Application.Compartido.Modelos;
using TherapEase.Application.Pacientes.Modelos;

namespace TherapEase.Application.Pacientes.Interfaces.Servicios;

// Contrato público de Pacientes para que Citas consulte lo mínimo sin conocer la persistencia ni la ficha completa (ADR-03, propuesta B03 D7).
// Ambas operaciones toman a la persona de la sesión, comprueban ConsultarPacientes en el servidor antes de leer y devuelven modelos sin datos personales,
// salvo el nombre que la agenda necesita y solo de pacientes vigentes. Exito falso solo significa que no hay permiso.
public interface IConsultaDePacientesParaCitas
{
    // Requiere ConsultarPacientes. Una sola consulta. No existir se indica con Condicion nula; de una baja, los ámbitos vienen vacíos.
    Task<RespuestaServicio<CondicionYAmbitosDePaciente>> ConsultarCondicionYAmbitosAsync(Guid idPaciente, CancellationToken cancelacion = default);

    // Requiere ConsultarPacientes. Una sola consulta. Solo los pacientes vigentes tienen nombre en el resultado: quien falte está de baja o no existe.
    Task<RespuestaServicio<IReadOnlyDictionary<Guid, string>>> ConsultarNombresParaAgendaAsync(
        IReadOnlyCollection<Guid> idsPacientes, CancellationToken cancelacion = default);
}
