namespace TherapEase.Application.Pacientes.Constantes;

// Límites de la búsqueda ordinaria y del listado paginado de pacientes. El listado nunca trae más de TamanoDePagina fichas por consulta.
public static class LimitesDeBusquedaDePacientes
{
    public const int LongitudMinimaDelTexto = 2;
    public const int LongitudMaximaDelTexto = 100;
    public const int MaximoDePalabras = 6;
    public const int MaximoDeResultados = 50;
    public const int MaximoDeCoincidencias = 10;
    public const int TamanoDePagina = 6;
}
