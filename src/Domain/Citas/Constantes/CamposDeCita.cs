namespace TherapEase.Domain.Citas.Constantes;

// Nombres de los campos que se registran en los eventos de auditoría de una cita: solo el nombre del campo, nunca su valor.
public static class CamposDeCita
{
    public const string IdPaciente = nameof(IdPaciente);
    public const string Ambito = nameof(Ambito);
    public const string Inicio = nameof(Inicio);
    public const string Fin = nameof(Fin);
    public const string Estado = nameof(Estado);
    public const string EstadoPago = nameof(EstadoPago);
}
