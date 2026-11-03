namespace TherapEase.Domain.Pacientes.Constantes;

// Límites de la ficha. Los comparten las reglas, la base de datos y los formularios. Valores aprobados por Lucía el 26-oct-2026, provisionales para datos de prueba.
public static class LimitesDePaciente
{
    public const int LongitudMinimaNombre = 2;
    public const int LongitudMaximaNombre = 200;

    // Quitar acentos y descomponer puede alargar algunos alfabetos (una sílaba hangul son hasta tres letras): el triple del nombre admitido.
    public const int LongitudMaximaNombreNormalizado = LongitudMaximaNombre * 3;

    public const int AnioMinimoDeNacimiento = 1900;
    public const int EdadDeMayoria = 18;

    public const int MinimoDeDigitosDeTelefono = 7;
    public const int MaximoDeDigitosDeTelefono = 15;
    public const int LongitudMaximaTelefono = 20;

    public const int LongitudMaximaCorreo = 254;
    public const int LongitudMaximaEscuela = 150;
    public const int LongitudMaximaGrupo = 30;

    public const int LongitudMaximaNombreDeTutor = 200;
    public const int LongitudMinimaParentesco = 2;
    public const int LongitudMaximaParentesco = 40;
}
