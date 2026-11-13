namespace TherapEase.Domain.Citas.Constantes;

// Mensajes fijos de las reglas de horario: nunca incluyen la fecha ni las horas escritas. Las páginas los comparan con estas constantes, no con el texto.
public static class MensajesDeHorarioDeCita
{
    public const string HorasConSegundos = "La hora de inicio y la de fin no pueden llevar segundos.";
    public const string FinNoPosteriorAlInicio = "La hora de fin debe ser posterior a la de inicio, el mismo día.";
    public const string FueraDelHorarioDeAtencion = "El horario de atención va de las 9:00 a las 19:00.";
    public const string DuracionAntesDeLas15 = "Antes de las 15:00 la cita dura una hora.";
    public const string DuracionDesdeLas15 = "Desde las 15:00 la cita dura 30 minutos o una hora.";
    public const string FechaNoRepresentable = "La fecha indicada no es válida.";
}
