namespace TherapEase.Domain.Compartido.Excepciones;

// La base abortó la transacción por interbloqueo (con citas, dos altas conflictivas que llegan a la vez pueden bloquearse entre sí en la restricción de cruce).
// No es una regla de negocio: quien no la espera la deja subir y la aplicación responde su error general, igual que antes. Solo el alta de citas la trata como conflicto.
public class InterbloqueoDeBaseException(Exception? interna = null)
    : Exception("La base de datos abortó la operación por un conflicto de concurrencia.", interna)
{
}
