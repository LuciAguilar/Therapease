using TherapEase.Domain.Citas.Constantes;
using TherapEase.Domain.Citas.Entidades;
using TherapEase.Domain.Citas.Entidades.Enums;
using TherapEase.Domain.Compartido.Entidades.Enums;
using TherapEase.Domain.Compartido.Excepciones;
using TherapEase.Domain.Compartido.Reglas;
using TherapEase.Domain.Pacientes.Entidades.Enums;

namespace TherapEase.Domain.Citas.Reglas;

public static class ReglasDeCita
{
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
        MetadatosDeAuditoria.RegistrarAlta(cita, idActor, ahora);
        return cita;
    }

    // Nombres de los campos que lleva el evento de alta de una cita. Nunca valores: ni el paciente, ni las horas.
    public static IReadOnlyList<string> CamposDeAlta() =>
    [
        CamposDeCita.IdPaciente, CamposDeCita.Ambito, CamposDeCita.Inicio, CamposDeCita.Fin, CamposDeCita.Estado, CamposDeCita.EstadoPago
    ];

    // Reconoce el reenvío de una reserva ya confirmada: sigue agendada y vigente, la creó el mismo actor y coinciden paciente, ámbito, inicio y fin.
    // Los instantes se comparan en UTC. Cualquier otro caso (otro actor, otros datos, cancelada o de baja) no es la misma reserva.
    public static bool EsLaMismaReserva(Cita existente, Cita candidata) =>
        existente.EsActiva
        && existente.IdUsuarioAlta == candidata.IdUsuarioAlta
        && existente.IdPaciente == candidata.IdPaciente
        && existente.Ambito == candidata.Ambito
        && existente.Inicio == candidata.Inicio
        && existente.Fin == candidata.Fin;

    // «Registrada después de su horario» (decisión de Lucía, 01-nov-2026): la cita se guardó cuando ya había empezado. FechaAlta la pone el servidor, nunca la página.
    // Las consultas de agenda calculan la misma comparación en la base; ambas deben coincidir.
    public static bool FueRegistradaDespuesDeSuHorario(Cita cita) => cita.FechaAlta > cita.Inicio;

    // Cancelar libera el horario pero no da de baja ni cambia el pago.
    public static void Cancelar(Cita cita, int versionEsperada, Guid idActor, DateTimeOffset ahora)
    {
        if (!cita.EsActiva)
        {
            throw new ReglaDeNegocioException("Solo puede cancelarse una cita activa.");
        }

        ExigirVersion(cita, versionEsperada);
        MetadatosDeAuditoria.RegistrarActualizacion(cita, idActor, ahora);
        cita.Estado = EstadoCita.Cancelada;
        cita.Version++;
    }

    public static void DarDeBaja(Cita cita, int versionEsperada, Guid idActor, DateTimeOffset ahora)
    {
        if (cita.Condicion == CondicionRegistro.Baja)
        {
            throw new ReglaDeNegocioException("La cita ya está dada de baja.");
        }

        ExigirVersion(cita, versionEsperada);
        MetadatosDeAuditoria.RegistrarBaja(cita, idActor, ahora);
        cita.Condicion = CondicionRegistro.Baja;
        cita.Version++;
    }

    // Que el paciente esté vigente y el horario libre lo garantiza la base al guardar.
    public static void Recuperar(Cita cita, int versionEsperada, Guid idActor, DateTimeOffset ahora)
    {
        if (cita.Condicion == CondicionRegistro.Vigente)
        {
            throw new ReglaDeNegocioException("La cita ya está vigente.");
        }

        MetadatosDeAuditoria.ExigirActor(idActor);
        ExigirVersion(cita, versionEsperada);
        MetadatosDeAuditoria.LimpiarBaja(cita);
        cita.Condicion = CondicionRegistro.Vigente;
        cita.Version++;
    }

    private static void ExigirVersion(Cita cita, int versionEsperada)
    {
        if (versionEsperada != cita.Version)
        {
            throw new ConflictoDeVersionException();
        }
    }
}
