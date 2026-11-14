using TherapEase.Domain.Compartido.Entidades.Enums;
using TherapEase.Domain.Pacientes.Entidades.Enums;

namespace TherapEase.Application.Pacientes.Modelos;

// Lo que Citas puede saber de un paciente para decidir si agenda: sin nombre ni otros datos personales.
// Condicion nula significa que el paciente no existe. De una baja los ámbitos vienen vacíos: son parte de la ficha y no se entregan.
public sealed record CondicionYAmbitosDePaciente(CondicionRegistro? Condicion, IReadOnlyList<AmbitoAtencion> Ambitos)
{
    public override string ToString() => nameof(CondicionYAmbitosDePaciente);
}

// Lectura del puerto de Pacientes, en una sola consulta. Los ámbitos se proyectan en la base solo para una ficha vigente.
public sealed record FilaDeCondicionYAmbitos(CondicionRegistro Condicion, bool TieneAmbitoEscolar, bool TieneAmbitoIndependiente)
{
    public override string ToString() => nameof(FilaDeCondicionYAmbitos);
}

// Nombre de un paciente vigente para mostrarlo en la agenda. Los pacientes de baja no aparecen: ni su nombre ni un indicio de su ficha.
public sealed record NombreDePacienteVigente(Guid IdPaciente, string Nombre)
{
    public override string ToString() => nameof(NombreDePacienteVigente);
}
