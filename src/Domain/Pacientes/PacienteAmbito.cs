namespace TherapEase.Domain.Pacientes;

public class PacienteAmbito
{
    private PacienteAmbito()
    {
    }

    internal PacienteAmbito(Guid idPaciente, AmbitoAtencion ambito)
    {
        IdPaciente = idPaciente;
        Ambito = ambito;
    }

    public Guid IdPaciente { get; private set; }

    public AmbitoAtencion Ambito { get; private set; }
}
