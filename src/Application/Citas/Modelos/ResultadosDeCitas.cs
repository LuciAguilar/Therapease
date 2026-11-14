using TherapEase.Domain.Citas.Entidades.Enums;
using TherapEase.Domain.Pacientes.Entidades.Enums;

namespace TherapEase.Application.Citas.Modelos;

public enum VistaDeAgenda
{
    // Agendada y vigente: las únicas citas que ocupan horario.
    Activas,

    // Cancelada y vigente. Nunca aparece en la vista de activas.
    Canceladas
}

// Una cita de la agenda, con las horas ya convertidas a la hora de pared de Hermosillo en el servidor: la página no convierte nada.
// IdPaciente y NombreDelPaciente son nulos si el paciente está de baja: no se revela su nombre ni se enlaza su ficha.
public sealed record CitaDeAgenda(
    Guid IdCita,
    DateTime InicioLocal,
    DateTime FinLocal,
    AmbitoAtencion Ambito,
    EstadoCita Estado,
    Guid? IdPaciente,
    string? NombreDelPaciente,
    bool PacienteDeBaja,
    bool RegistradaDespuesDeSuHorario)
{
    public override string ToString() => nameof(CitaDeAgenda);
}

// Lo mismo que una cita de la agenda, más el estado de pago y la versión.
public sealed record DetalleDeCita(
    Guid IdCita,
    DateTime InicioLocal,
    DateTime FinLocal,
    AmbitoAtencion Ambito,
    EstadoCita Estado,
    Guid? IdPaciente,
    string? NombreDelPaciente,
    bool PacienteDeBaja,
    bool RegistradaDespuesDeSuHorario,
    EstadoPago EstadoPago,
    int Version)
{
    public override string ToString() => nameof(DetalleDeCita);
}

// HoyEnHermosillo permite marcar el día actual sin usar el reloj del navegador. HayMas avisa que el periodo trae más citas que el límite de filas.
public sealed record ResultadoDeAgenda(
    IReadOnlyList<CitaDeAgenda> Citas,
    DateOnly Desde,
    DateOnly Hasta,
    DateOnly HoyEnHermosillo,
    VistaDeAgenda Vista,
    bool HayMas)
{
    public override string ToString() => nameof(ResultadoDeAgenda);
}

public enum EstadoDeAgendar
{
    // Se guardó ahora: la cita y su evento se confirmaron.
    Agendada,

    // Era la misma reserva de antes, ya confirmada: sin segunda cita ni evento.
    YaAgendada
}

// No decidir la navegación solo por Exito: ambos estados son correctos pero dicen cosas distintas a la usuaria.
public sealed record ResultadoDeAgendar(EstadoDeAgendar Estado, DetalleDeCita Cita)
{
    public override string ToString() => nameof(ResultadoDeAgendar);
}

// Lectura del puerto de Citas para la agenda: solo la cita, sin datos del paciente. Los nombres los decide Pacientes.
public sealed record FilaDeCitaDeAgenda(
    Guid IdCita,
    Guid IdPaciente,
    AmbitoAtencion Ambito,
    EstadoCita Estado,
    DateTimeOffset Inicio,
    DateTimeOffset Fin,
    bool RegistradaDespuesDeSuHorario)
{
    public override string ToString() => nameof(FilaDeCitaDeAgenda);
}
