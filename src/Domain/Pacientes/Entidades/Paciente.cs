using TherapEase.Domain.Compartido.Entidades;
using TherapEase.Domain.Compartido.Entidades.Enums;

namespace TherapEase.Domain.Pacientes.Entidades;

// Solo datos de la ficha; las operaciones (registrar, actualizar, baja, recuperar) están en ReglasDePaciente.
public class Paciente : EntidadAuditable
{
    public const int LongitudMaximaNombre = 200;
    public const int LongitudMaximaContacto = 200;

    private readonly List<PacienteAmbito> _ambitos = [];

    internal Paciente()
    {
    }

    public Guid IdPaciente { get; internal set; }

    public string Nombre { get; internal set; } = string.Empty;

    public string? Contacto { get; internal set; }

    public CondicionRegistro Condicion { get; internal set; }

    public int Version { get; internal set; }

    // Hacia afuera es de solo lectura: los ámbitos cambian únicamente mediante ReglasDePaciente. EF Core llena el campo por convención.
    public IReadOnlyCollection<PacienteAmbito> Ambitos => _ambitos.AsReadOnly();

    internal ICollection<PacienteAmbito> AmbitosEditables => _ambitos;
}
