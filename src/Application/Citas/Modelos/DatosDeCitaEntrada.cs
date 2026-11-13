using TherapEase.Domain.Pacientes.Entidades.Enums;

namespace TherapEase.Application.Citas.Modelos;

// Lo que captura el formulario de una cita, sin validar. Las páginas usan este modelo y nunca las entidades de Domain.
// Todo es anulable para distinguir «no llegó» de un valor por omisión (0001-01-01, 00:00 o el primer ámbito).
// Fecha y horas son la hora de pared de Hermosillo, sin zona ni desfase: la zona del navegador no interviene.
// El texto de ToString no incluye valores: ningún registro debe imprimir datos por accidente.
public sealed record DatosDeCitaEntrada(
    Guid? IdPaciente,
    AmbitoAtencion? Ambito,
    DateOnly? Fecha,
    TimeOnly? HoraInicio,
    TimeOnly? HoraFin)
{
    public override string ToString() => nameof(DatosDeCitaEntrada);
}
