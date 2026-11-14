namespace TherapEase.Application.Pacientes.Modelos;

// Una página del listado de pacientes vigentes. Trae solo las filas de esa página; los totales vienen de una consulta aparte, de modo que
// nunca se cargan todos los pacientes. PaginaActual ya está ajustada al rango real: pedir una página que dejó de existir devuelve la última.
public sealed record PaginaDePacientes(
    IReadOnlyList<ResumenDePaciente> Pacientes,
    int PaginaActual,
    int TotalDePaginas,
    int TotalDePacientes,
    int TamanoDePagina)
{
    public bool HayAnterior => PaginaActual > 1;

    public bool HaySiguiente => PaginaActual < TotalDePaginas;

    // Posición (de 1 en adelante) del primer y último paciente de la página dentro del total; 0 cuando no hay ninguno.
    public int Desde => Pacientes.Count == 0 ? 0 : (PaginaActual - 1) * TamanoDePagina + 1;

    public int Hasta => Pacientes.Count == 0 ? 0 : Desde + Pacientes.Count - 1;

    public override string ToString() => nameof(PaginaDePacientes);
}
