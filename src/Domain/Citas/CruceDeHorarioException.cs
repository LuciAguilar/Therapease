using TherapEase.Domain.Compartido;

namespace TherapEase.Domain.Citas;

public class CruceDeHorarioException(Exception? interna = null)
    : ReglaDeNegocioException("El horario se cruza con otra cita activa.", interna)
{
}
