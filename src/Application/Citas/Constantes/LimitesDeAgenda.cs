namespace TherapEase.Application.Citas.Constantes;

// Límites de la consulta de agenda (D5 de la propuesta B03, aprobada por Lucía el 05-nov-2026).
public static class LimitesDeAgenda
{
    public const int DiasMinimos = 1;
    public const int DiasMaximos = 35;

    // Sin fechas: hoy y los seis días siguientes.
    public const int DiasPorOmision = 7;

    // 35 días por 14 citas posibles al día son 490 como máximo con las reglas de horario; con más filas se avisa y la página pide acotar.
    public const int MaximoDeFilas = 500;
}
