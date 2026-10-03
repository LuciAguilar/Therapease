using TherapEase.Domain.Compartido;

namespace TherapEase.Domain.Citas;

public class CitaActivaConPacienteDeBajaException(Exception? interna = null)
    : ReglaDeNegocioException("No puede haber una cita activa de un paciente dado de baja.", interna)
{
}
