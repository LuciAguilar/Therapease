namespace TherapEase.Domain.Compartido;

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

    public static DateTime ALocal(DateTimeOffset instante) =>
        TimeZoneInfo.ConvertTime(instante, Zona).DateTime;
}
