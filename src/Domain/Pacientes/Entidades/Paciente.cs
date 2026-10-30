using TherapEase.Domain.Compartido.Entidades;
using TherapEase.Domain.Compartido.Entidades.Enums;
using TherapEase.Domain.Pacientes.Constantes;

namespace TherapEase.Domain.Pacientes.Entidades;

// Solo datos de la ficha; las operaciones (registrar, actualizar, baja, recuperar) están en ReglasDePaciente.
public class Paciente : EntidadAuditable
{
    public const int LongitudMaximaNombre = LimitesDePaciente.LongitudMaximaNombre;

    private readonly List<PacienteAmbito> _ambitos = [];

    internal Paciente()
    {
    }

    public Guid IdPaciente { get; internal set; }

    public string Nombre { get; internal set; } = string.Empty;

    // Clave de búsqueda del nombre (NormalizacionDeNombres); siempre coherente con Nombre.
    public string NombreNormalizado { get; internal set; } = string.Empty;

    // Anulable para conservar las fichas anteriores sin inventar fechas. La edad no se guarda: se calcula (ReglasDeEdad).
    public DateOnly? FechaNacimiento { get; internal set; }

    public string? Telefono { get; internal set; }

    public string? Correo { get; internal set; }

    public string? Escuela { get; internal set; }

    public string? Grupo { get; internal set; }

    // Un solo tutor en la misma ficha: obligatorio para menores de 18 años, opcional para adultos.
    public string? TutorNombre { get; internal set; }

    public string? TutorParentesco { get; internal set; }

    public string? TutorTelefono { get; internal set; }

    public string? TutorCorreo { get; internal set; }

    public CondicionRegistro Condicion { get; internal set; }

    public int Version { get; internal set; }

    // Hacia afuera es de solo lectura: los ámbitos cambian únicamente mediante ReglasDePaciente. EF Core llena el campo por convención.
    public IReadOnlyCollection<PacienteAmbito> Ambitos => _ambitos.AsReadOnly();

    internal ICollection<PacienteAmbito> AmbitosEditables => _ambitos;
}
