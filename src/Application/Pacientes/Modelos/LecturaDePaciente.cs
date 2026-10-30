using TherapEase.Domain.Compartido.Entidades.Enums;

namespace TherapEase.Application.Pacientes.Modelos;

// Lo que devuelve la lectura de una ficha por identificador, en una sola consulta. El contenido (todo menos la condición) viene lleno solo si la ficha está vigente:
// para una baja la base no lo entrega. Nunca sale de Application hacia las páginas; ToString no incluye valores.
public sealed record LecturaDePaciente(
    Guid IdPaciente,
    CondicionRegistro Condicion,
    int Version,
    string? Nombre,
    DateOnly? FechaNacimiento,
    string? Telefono,
    string? Correo,
    string? Escuela,
    string? Grupo,
    string? TutorNombre,
    string? TutorParentesco,
    string? TutorTelefono,
    string? TutorCorreo,
    bool TieneAmbitoEscolar,
    bool TieneAmbitoIndependiente)
{
    public override string ToString() => nameof(LecturaDePaciente);
}

// Una coincidencia de búsqueda. NombreRepetido es verdadero si otra ficha vigente tiene el mismo nombre normalizado: solo entonces se muestra el nacimiento.
public sealed record FilaDeBusquedaDePaciente(Guid IdPaciente, string Nombre, DateOnly? FechaNacimiento, bool NombreRepetido)
{
    public override string ToString() => nameof(FilaDeBusquedaDePaciente);
}

public sealed record ParecidoDePaciente(Guid IdPaciente, string Nombre, DateOnly? FechaNacimiento)
{
    public override string ToString() => nameof(ParecidoDePaciente);
}
