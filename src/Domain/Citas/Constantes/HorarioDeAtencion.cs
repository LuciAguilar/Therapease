namespace TherapEase.Domain.Citas.Constantes;

// Horario de las citas, en minutos desde la medianoche de America/Hermosillo. Decisión de Lucía del 01-nov-2026: los siete días con el mismo horario.
// Contenedor sin lógica: las reglas están en ReglasDeHorarioDeCita.
public static class HorarioDeAtencion
{
    // La primera hora de inicio posible (9:00) y la última hora de fin posible (19:00).
    public const int MinutoDeApertura = 9 * 60;
    public const int MinutoDeCierre = 19 * 60;

    // Antes de este minuto (15:00) la cita dura una hora; desde él puede durar 30 minutos o una hora.
    public const int MinutoDeCambioDeDuracion = 15 * 60;

    public const int DuracionCortaEnMinutos = 30;
    public const int DuracionLargaEnMinutos = 60;
}
