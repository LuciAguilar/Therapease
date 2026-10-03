using TherapEase.Application.Compartido.Interfaces.Repositorios;

namespace TherapEase.UnitTests.Dobles;

internal sealed class UnidadDeTrabajoFalsa : IUnidadDeTrabajo
{
    public int Confirmaciones { get; private set; }

    public int Reversiones { get; private set; }

    public async Task EjecutarAsync(Func<CancellationToken, Task> operacion, CancellationToken cancelacion = default)
    {
        try
        {
            await operacion(cancelacion);
            Confirmaciones++;
        }
        catch
        {
            Reversiones++;
            throw;
        }
    }
}
