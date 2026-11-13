using TherapEase.Application.Citas.Modelos;
using TherapEase.Domain.Citas.Entidades;

namespace TherapEase.Application.Citas.Interfaces.Repositorios;

public interface IRepositorioCitas
{
    Task AgregarAsync(Cita cita, CancellationToken cancelacion = default);

    Task<Cita?> ObtenerAsync(Guid idCita, CancellationToken cancelacion = default);

    // Lectura por número sin rastrear: nunca devuelve lo que el contexto tenga pendiente de guardar, así que es la lectura segura para reconocer un
    // reenvío, también tras un guardado rechazado cuando la cita candidata puede seguir en el contexto. Nulo si no existe.
    Task<Cita?> ObtenerParaComparacionAsync(Guid idCita, CancellationToken cancelacion = default);

    // Citas vigentes de la vista que se solapan con [desde, hasta): Fin mayor que desde e Inicio menor que hasta. Orden estable por inicio y luego identificador.
    // Se piden limite filas; quien llama pide una más para saber si hay más. Sin datos del paciente y sin rastrear.
    Task<IReadOnlyList<FilaDeCitaDeAgenda>> ConsultarAgendaAsync(
        DateTimeOffset desde, DateTimeOffset hasta, VistaDeAgenda vista, int limite, CancellationToken cancelacion = default);

    // Activa = agendada y vigente; solo esas citas impiden la baja del paciente.
    Task<bool> ExistenActivasDePacienteAsync(Guid idPaciente, CancellationToken cancelacion = default);
}
