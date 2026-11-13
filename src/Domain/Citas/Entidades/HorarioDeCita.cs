namespace TherapEase.Domain.Citas.Entidades;

// Inicio y fin de una cita ya validados y convertidos a instantes UTC. Solo los produce ReglasDeHorarioDeCita.
public sealed record HorarioDeCita(DateTimeOffset Inicio, DateTimeOffset Fin);
