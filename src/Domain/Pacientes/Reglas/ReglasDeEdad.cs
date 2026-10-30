using TherapEase.Domain.Compartido.Reglas;
using TherapEase.Domain.Pacientes.Constantes;

namespace TherapEase.Domain.Pacientes.Reglas;

// La edad no se guarda: se calcula desde la fecha de nacimiento con la fecha de hoy en America/Hermosillo.
public static class ReglasDeEdad
{
    public static DateOnly HoyEnHermosillo(DateTimeOffset ahora) => DateOnly.FromDateTime(HoraHermosillo.ALocal(ahora));

    public static int Calcular(DateOnly nacimiento, DateOnly hoy)
    {
        var edad = hoy.Year - nacimiento.Year;
        return hoy < Cumpleanos(nacimiento, hoy.Year) ? edad - 1 : edad;
    }

    public static bool EsMenorDeEdad(DateOnly nacimiento, DateOnly hoy) => Calcular(nacimiento, hoy) < LimitesDePaciente.EdadDeMayoria;

    // Quien nació un 29 de febrero cumple el 1 de marzo en los años que no son bisiestos: así no se le trata como adulto un día antes de tiempo.
    private static DateOnly Cumpleanos(DateOnly nacimiento, int anio) =>
        nacimiento is { Month: 2, Day: 29 } && !DateTime.IsLeapYear(anio)
            ? new DateOnly(anio, 3, 1)
            : new DateOnly(anio, nacimiento.Month, nacimiento.Day);
}
