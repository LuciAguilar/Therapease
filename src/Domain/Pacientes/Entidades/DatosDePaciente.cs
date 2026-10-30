namespace TherapEase.Domain.Pacientes.Entidades;

// Lo que se captura de una ficha, sin validar. Las reglas lo validan y lo devuelven normalizado.
// El texto de ToString no incluye valores: ningún registro debe imprimir datos personales por accidente.
public sealed record DatosDePaciente(
    string? Nombre,
    DateOnly? FechaNacimiento = null,
    string? Telefono = null,
    string? Correo = null,
    string? Escuela = null,
    string? Grupo = null,
    DatosDeTutor? Tutor = null)
{
    public override string ToString() => nameof(DatosDePaciente);
}

public sealed record DatosDeTutor(
    string? Nombre = null,
    string? Parentesco = null,
    string? Telefono = null,
    string? Correo = null)
{
    public override string ToString() => nameof(DatosDeTutor);
}
