namespace TherapEase.Domain.Compartido;

public class ConflictoDeVersionException(Exception? interna = null)
    : ReglaDeNegocioException("El registro cambió mientras se editaba. Recarga la información antes de decidir.", interna)
{
}
