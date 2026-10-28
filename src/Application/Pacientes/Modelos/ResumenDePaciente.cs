namespace TherapEase.Application.Pacientes.Modelos;

// Una fila de búsqueda o de la lista de parecidos: identificador y nombre. El nacimiento solo viaja cuando hace falta distinguir a dos personas con el mismo nombre;
// si esa persona no tiene fecha guardada (ficha anterior) se indica con NacimientoDesconocido, sin inventar un valor.
public sealed record ResumenDePaciente(Guid IdPaciente, string Nombre, DateOnly? FechaNacimiento = null, bool NacimientoDesconocido = false)
{
    public override string ToString() => nameof(ResumenDePaciente);
}

// Hay más resultados que los mostrados: conviene afinar el texto. Nunca se afirma que la lista sea completa si llegó al límite.
public sealed record ResultadoDeBusqueda(IReadOnlyList<ResumenDePaciente> Pacientes, bool HayMas)
{
    public override string ToString() => nameof(ResultadoDeBusqueda);
}
