using TherapEase.Domain.Pacientes.Entidades.Enums;

namespace TherapEase.Application.Pacientes.Modelos;

// Ficha de un paciente vigente. La edad no se guarda: se calcula y es nula si la ficha anterior no tiene nacimiento (edad desconocida, nunca cero).
// La versión solo sirve para detectar un conflicto al guardar. ToString no incluye valores.
public sealed record FichaDePaciente(
    Guid IdPaciente,
    int Version,
    string Nombre,
    DateOnly? FechaNacimiento,
    int? Edad,
    string? Telefono,
    string? Correo,
    string? Escuela,
    string? Grupo,
    IReadOnlyList<AmbitoAtencion> Ambitos,
    DatosDeTutorEntrada? Tutor)
{
    public override string ToString() => nameof(FichaDePaciente);
}
