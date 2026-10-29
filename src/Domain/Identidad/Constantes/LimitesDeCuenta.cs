namespace TherapEase.Domain.Identidad.Constantes;

// Límites de los datos personales de una cuenta; los comparten las reglas, la base de datos y los formularios.
public static class LimitesDeCuenta
{
    public const int LongitudMinimaNombreCompleto = 2;
    public const int LongitudMaximaNombreCompleto = 150;
    public const int LongitudMaximaCorreo = 254;
}
