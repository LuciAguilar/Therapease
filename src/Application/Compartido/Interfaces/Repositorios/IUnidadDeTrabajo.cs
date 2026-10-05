namespace TherapEase.Application.Compartido.Interfaces.Repositorios;

public interface IUnidadDeTrabajo
{
    // Cambios y evento de auditoría se confirman juntos; el éxito solo existe después del commit (Q03/Q08).
    Task EjecutarAsync(Func<CancellationToken, Task> operacion, CancellationToken cancelacion = default);
}
