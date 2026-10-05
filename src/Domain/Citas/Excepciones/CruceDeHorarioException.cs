using TherapEase.Domain.Compartido.Excepciones;

namespace TherapEase.Domain.Citas.Excepciones;

public class CruceDeHorarioException(Exception? interna = null)
    : ReglaDeNegocioException("El horario se cruza con otra cita activa.", interna)
{
}
