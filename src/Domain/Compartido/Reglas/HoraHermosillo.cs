using TherapEase.Domain.Compartido.Excepciones;

namespace TherapEase.Domain.Compartido.Reglas;

public static class HoraHermosillo
{
    public const string IdZona = "America/Hermosillo";

    private static readonly TimeZoneInfo Zona = TimeZoneInfo.FindSystemTimeZoneById(IdZona);

    // Se exige fecha y hora: una fecha sola no es un instante (Q09).
    public static DateTimeOffset AInstante(DateOnly fecha, TimeOnly hora)
    {
        var local = fecha.ToDateTime(hora, DateTimeKind.Unspecified);

        if (Zona.IsInvalidTime(local) || Zona.IsAmbiguousTime(local))
        {
            throw new ReglaDeNegocioException("La hora indicada es ambigua o no existe en America/Hermosillo.");
        }

        return new DateTimeOffset(local, Zona.GetUtcOffset(local)).ToUniversalTime();
    }

    // Dice si la fecha y hora dan un instante UTC dentro del rango de DateTimeOffset. Hermosillo está detrás de UTC, así que el límite es el final de 9999.
    public static bool EsRepresentable(DateOnly fecha, TimeOnly hora)
    {
        var local = fecha.ToDateTime(hora, DateTimeKind.Unspecified);
        var ticksUtc = local.Ticks - Zona.GetUtcOffset(local).Ticks;
        return ticksUtc >= DateTime.MinValue.Ticks && ticksUtc <= DateTime.MaxValue.Ticks;
    }

    public static DateTime ALocal(DateTimeOffset instante) =>
        TimeZoneInfo.ConvertTime(instante, Zona).DateTime;
}
