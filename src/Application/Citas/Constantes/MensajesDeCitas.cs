namespace TherapEase.Application.Citas.Constantes;

// Mensajes fijos de los servicios de citas: sin nombres, fechas, horas ni contactos. Las páginas los comparan con estas constantes, no con el texto.
public static class MensajesDeCitas
{
    public const string SinPermiso = "No tienes permiso para realizar esta operación.";
    public const string SolicitudInvalida = "La solicitud no es válida.";
    public const string PeriodoInvalido = "El periodo no es válido: indica dos fechas, la primera no posterior a la segunda, de hasta 35 días.";
    public const string PacienteNoEncontrado = "No se encontró el paciente.";

    // Mismo texto que usa Pacientes al abrir una baja: quien agenda ya tiene permiso para consultar pacientes.
    public const string PacienteDeBaja = "Está dada de baja.";
    public const string AmbitoNoPermitido = "El paciente no tiene ese ámbito de atención.";
    public const string HorarioOcupado = "El horario ya está ocupado por otra cita.";
    public const string CitaNoEncontrada = "No se encontró la cita.";
    public const string SolicitudYaUsada = "No fue posible agendar la cita con esa solicitud. Abre de nuevo el formulario.";

    public const string CitaAgendada = "Cita agendada.";
    public const string CitaYaAgendada = "Esa cita ya estaba agendada.";

    // Textos para mostrar en la agenda y el detalle.
    public const string LeyendaRegistradaDespuesDeSuHorario = "Registrada después de su horario";
    public const string LeyendaPacienteDeBaja = "Paciente dado de baja";
}
