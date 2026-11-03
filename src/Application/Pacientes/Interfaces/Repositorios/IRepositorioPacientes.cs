using TherapEase.Application.Pacientes.Modelos;
using TherapEase.Domain.Compartido.Entidades.Enums;
using TherapEase.Domain.Pacientes.Entidades;

namespace TherapEase.Application.Pacientes.Interfaces.Repositorios;

public interface IRepositorioPacientes
{
    Task AgregarAsync(Paciente paciente, CancellationToken cancelacion = default);

    // Entidad completa con sus ámbitos, para escribir. Lo que se lee con ella se compara con lo que se va a guardar; no es la lectura para mostrar.
    Task<Paciente?> ObtenerAsync(Guid idPaciente, CancellationToken cancelacion = default);

    // Entidad completa sin rastrear, solo para comparar con otra (reenvío de un alta). Nunca devuelve lo que el contexto tenga pendiente de guardar: es
    // la lectura segura tras un guardado rechazado, cuando la ficha candidata puede seguir en el contexto.
    Task<Paciente?> ObtenerParaComparacionAsync(Guid idPaciente, CancellationToken cancelacion = default);

    // Lectura para mostrar una ficha por identificador, en UNA sola consulta (con la información de los ámbitos).
    // El contenido se proyecta en la base solo para una ficha vigente; para una baja no se trae. Nulo si no existe.
    Task<LecturaDePaciente?> ObtenerParaConsultaAsync(Guid idPaciente, CancellationToken cancelacion = default);

    // Solo la condición, sin leer el contenido de la ficha. Nulo si no existe.
    Task<CondicionRegistro?> ObtenerCondicionAsync(Guid idPaciente, CancellationToken cancelacion = default);

    // Fichas vigentes cuyo nombre normalizado contiene todas las palabras (ya normalizadas), con el nacimiento exacto opcional.
    // Orden estable por nombre normalizado y, para empates, por identificador. Se piden limite filas; quien llama pide una más para saber si hay más.
    Task<IReadOnlyList<FilaDeBusquedaDePaciente>> BuscarVigentesAsync(
        IReadOnlyList<string> palabras, DateOnly? nacimiento, int limite, CancellationToken cancelacion = default);

    // Fichas vigentes con exactamente ese nombre normalizado, en el mismo orden estable.
    Task<IReadOnlyList<ParecidoDePaciente>> BuscarParecidosAsync(
        string nombreNormalizado, int limite, CancellationToken cancelacion = default);

    // Los ámbitos que una edición quitó de la ficha: se borran de la base junto con el resto del guardado.
    void QuitarAmbitos(IEnumerable<PacienteAmbito> ambitos);
}
