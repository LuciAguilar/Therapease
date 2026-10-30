using TherapEase.Domain.Pacientes.Entidades;

namespace TherapEase.Domain.Pacientes.Reglas;

// Resultado de editar una ficha: solo los nombres de los campos que cambiaron (para el evento de auditoría, nunca sus valores)
// y los ámbitos que se quitaron (la persistencia los borra de la base).
public sealed record CambiosDePaciente(IReadOnlyList<string> Campos, IReadOnlyList<PacienteAmbito> AmbitosQuitados)
{
    public static CambiosDePaciente Ninguno { get; } = new([], []);

    public bool Hubo => Campos.Count > 0;
}
