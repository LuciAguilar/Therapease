using TherapEase.Domain.Compartido;
using TherapEase.Domain.Pacientes;

namespace TherapEase.Domain.Citas;

public class Cita : EntidadAuditable
{
    private Cita()
    {
    }

    public Guid IdCita { get; private set; }

    public Guid IdPaciente { get; private set; }

    public AmbitoAtencion Ambito { get; private set; }

    public DateTimeOffset Inicio { get; private set; }

    public DateTimeOffset Fin { get; private set; }

    public EstadoCita Estado { get; private set; }

    public EstadoPago EstadoPago { get; private set; }

    public CondicionRegistro Condicion { get; private set; }

    public int Version { get; private set; }

    // Solo las citas activas ocupan horario (Q06).
    public bool EsActiva => Estado == EstadoCita.Agendada && Condicion == CondicionRegistro.Vigente;

    public static Cita Agendar(
        Guid idCita,
        Guid idPaciente,
        AmbitoAtencion ambito,
        DateTimeOffset inicio,
        DateTimeOffset fin,
        Guid idActor,
        DateTimeOffset ahora)
    {
        if (idCita == Guid.Empty || idPaciente == Guid.Empty)
        {
            throw new ReglaDeNegocioException("La cita necesita identificador y paciente.");
        }

        if (!Enum.IsDefined(ambito))
        {
            throw new ReglaDeNegocioException("El ámbito de la cita no es válido.");
        }

        var inicioUtc = inicio.ToUniversalTime();
        var finUtc = fin.ToUniversalTime();
        if (inicioUtc >= finUtc)
        {
            throw new ReglaDeNegocioException("El inicio de la cita debe ser anterior a su fin.");
        }

        var cita = new Cita
        {
            IdCita = idCita,
            IdPaciente = idPaciente,
            Ambito = ambito,
            Inicio = inicioUtc,
            Fin = finUtc,
            Estado = EstadoCita.Agendada,
            EstadoPago = EstadoPago.Pendiente,
            Condicion = CondicionRegistro.Vigente,
            Version = 1
        };
        cita.RegistrarAlta(idActor, ahora);
        return cita;
    }

    // Cancelar libera el horario pero no da de baja ni cambia el pago.
    public void Cancelar(int versionEsperada, Guid idActor, DateTimeOffset ahora)
    {
        if (!EsActiva)
        {
            throw new ReglaDeNegocioException("Solo puede cancelarse una cita activa.");
        }

        ExigirVersion(versionEsperada);
        RegistrarActualizacion(idActor, ahora);
        Estado = EstadoCita.Cancelada;
        Version++;
    }

    public void DarDeBaja(int versionEsperada, Guid idActor, DateTimeOffset ahora)
    {
        if (Condicion == CondicionRegistro.Baja)
        {
            throw new ReglaDeNegocioException("La cita ya está dada de baja.");
        }

        ExigirVersion(versionEsperada);
        RegistrarBaja(idActor, ahora);
        Condicion = CondicionRegistro.Baja;
        Version++;
    }

    // Que el paciente esté vigente y el horario libre lo garantiza la base al guardar.
    public void Recuperar(int versionEsperada, Guid idActor, DateTimeOffset ahora)
    {
        if (Condicion == CondicionRegistro.Vigente)
        {
            throw new ReglaDeNegocioException("La cita ya está vigente.");
        }

        ExigirActor(idActor);
        ExigirVersion(versionEsperada);
        LimpiarBaja();
        Condicion = CondicionRegistro.Vigente;
        Version++;
    }

    private void ExigirVersion(int versionEsperada)
    {
        if (versionEsperada != Version)
        {
            throw new ConflictoDeVersionException();
        }
    }
}
