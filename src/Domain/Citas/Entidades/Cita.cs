using TherapEase.Domain.Citas.Entidades.Enums;
using TherapEase.Domain.Compartido.Entidades;
using TherapEase.Domain.Compartido.Entidades.Enums;
using TherapEase.Domain.Pacientes.Entidades.Enums;

namespace TherapEase.Domain.Citas.Entidades;

// Solo datos de la cita; las operaciones (agendar, cancelar, baja, recuperar) están en ReglasDeCita.
public class Cita : EntidadAuditable
{
    internal Cita()
    {
    }

    public Guid IdCita { get; internal set; }

    public Guid IdPaciente { get; internal set; }

    public AmbitoAtencion Ambito { get; internal set; }

    public DateTimeOffset Inicio { get; internal set; }

    public DateTimeOffset Fin { get; internal set; }

    public EstadoCita Estado { get; internal set; }

    public EstadoPago EstadoPago { get; internal set; }

    public CondicionRegistro Condicion { get; internal set; }

    public int Version { get; internal set; }

    // Solo las citas activas ocupan horario (Q06).
    public bool EsActiva => Estado == EstadoCita.Agendada && Condicion == CondicionRegistro.Vigente;
}
