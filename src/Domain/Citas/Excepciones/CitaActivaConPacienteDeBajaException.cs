using TherapEase.Domain.Compartido.Excepciones;

namespace TherapEase.Domain.Citas.Excepciones;

public class CitaActivaConPacienteDeBajaException(Exception? interna = null)
    : ReglaDeNegocioException("No puede haber una cita activa de un paciente dado de baja.", interna)
{
}
