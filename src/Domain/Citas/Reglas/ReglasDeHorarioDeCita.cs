using TherapEase.Domain.Citas.Constantes;
using TherapEase.Domain.Citas.Entidades;
using TherapEase.Domain.Compartido.Excepciones;
using TherapEase.Domain.Compartido.Reglas;

namespace TherapEase.Domain.Citas.Reglas;

// Única copia de la regla de horario (decisión de Lucía del 01-nov-2026, D3 de la propuesta B03): vive solo aquí, sin CHECK en PostgreSQL.
// Trabaja con la fecha y las horas de pared de Hermosillo y solo después convierte a instantes UTC con HoraHermosillo, sin depender de la zona del equipo.
// Se aplica al agendar; las citas anteriores o de prueba fuera de horario no se validan ni se cambian al leerlas.
public static class ReglasDeHorarioDeCita
{
    // Valida las horas y devuelve los instantes UTC, o lanza ReglaDeNegocioException con un mensaje fijo de MensajesDeHorarioDeCita.
    public static HorarioDeCita Calcular(DateOnly fecha, TimeOnly horaInicio, TimeOnly horaFin)
    {
        if (ValidarHoras(horaInicio, horaFin) is { } rechazo)
        {
            throw new ReglaDeNegocioException(rechazo);
        }

        // Un día cerca de 9999-12-31 puede caer fuera del rango de los instantes UTC: se rechaza de forma controlada, no con una excepción de rango.
        if (!HoraHermosillo.EsRepresentable(fecha, horaInicio) || !HoraHermosillo.EsRepresentable(fecha, horaFin))
        {
            throw new ReglaDeNegocioException(MensajesDeHorarioDeCita.FechaNoRepresentable);
        }

        return new HorarioDeCita(HoraHermosillo.AInstante(fecha, horaInicio), HoraHermosillo.AInstante(fecha, horaFin));
    }

    // Solo las horas, sin fecha ni conversión. Devuelve el mensaje fijo del primer rechazo o nulo si el horario es válido.
    // No lanza excepciones: así una prueba puede recorrer todas las combinaciones de minutos.
    public static string? ValidarHoras(TimeOnly horaInicio, TimeOnly horaFin)
    {
        if (TieneSegundos(horaInicio) || TieneSegundos(horaFin))
        {
            return MensajesDeHorarioDeCita.HorasConSegundos;
        }

        var inicio = MinutosDesdeMedianoche(horaInicio);
        var fin = MinutosDesdeMedianoche(horaFin);

        // Hay una sola fecha: un fin «del día siguiente» no se puede expresar y se rechaza como un fin que no es posterior al inicio.
        if (fin <= inicio)
        {
            return MensajesDeHorarioDeCita.FinNoPosteriorAlInicio;
        }

        if (inicio < HorarioDeAtencion.MinutoDeApertura || fin > HorarioDeAtencion.MinutoDeCierre)
        {
            return MensajesDeHorarioDeCita.FueraDelHorarioDeAtencion;
        }

        var duracion = fin - inicio;

        // Antes de las 15:00 dura exactamente una hora, aunque termine después de las 15:00 (14:30 a 15:30 sí se permite).
        if (inicio < HorarioDeAtencion.MinutoDeCambioDeDuracion)
        {
            return duracion == HorarioDeAtencion.DuracionLargaEnMinutos ? null : MensajesDeHorarioDeCita.DuracionAntesDeLas15;
        }

        return duracion is HorarioDeAtencion.DuracionCortaEnMinutos or HorarioDeAtencion.DuracionLargaEnMinutos
            ? null
            : MensajesDeHorarioDeCita.DuracionDesdeLas15;
    }

    // Sin segundos ni fracciones: la hora cae exactamente en un minuto.
    private static bool TieneSegundos(TimeOnly hora) => hora.Ticks % TimeSpan.TicksPerMinute != 0;

    private static int MinutosDesdeMedianoche(TimeOnly hora) => hora.Hour * 60 + hora.Minute;
}
