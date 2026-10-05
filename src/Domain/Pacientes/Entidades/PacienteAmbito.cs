using TherapEase.Domain.Pacientes.Entidades.Enums;

namespace TherapEase.Domain.Pacientes.Entidades;

public class PacienteAmbito
{
    internal PacienteAmbito()
    {
    }

    public Guid IdPaciente { get; internal set; }

    public AmbitoAtencion Ambito { get; internal set; }
}
