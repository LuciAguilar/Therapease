using TherapEase.Domain.Pacientes.Entidades.Enums;

namespace TherapEase.Application.Pacientes.Modelos;

// Lo que captura el formulario de una ficha, sin validar. Las páginas usan este modelo y nunca las entidades de Domain.
// El texto de ToString no incluye valores: ningún registro debe imprimir datos personales por accidente.
public sealed record DatosDePacienteEntrada(
    string? Nombre,
    DateOnly? FechaNacimiento,
    string? Telefono,
    string? Correo,
    string? Escuela,
    string? Grupo,
    IReadOnlyList<AmbitoAtencion>? Ambitos,
    DatosDeTutorEntrada? Tutor = null)
{
    public override string ToString() => nameof(DatosDePacienteEntrada);
}

public sealed record DatosDeTutorEntrada(string? Nombre, string? Parentesco, string? Telefono, string? Correo)
{
    public override string ToString() => nameof(DatosDeTutorEntrada);
}
