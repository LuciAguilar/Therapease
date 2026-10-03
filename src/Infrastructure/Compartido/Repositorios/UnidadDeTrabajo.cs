using TherapEase.Application.Compartido.Interfaces.Repositorios;
using TherapEase.Infrastructure.Data;

namespace TherapEase.Infrastructure.Compartido.Repositorios;

public class UnidadDeTrabajo : IUnidadDeTrabajo
{
    private readonly ContextoDeDatos _contexto;

    public UnidadDeTrabajo(ContextoDeDatos contexto)
    {
        _contexto = contexto;
    }

    public async Task EjecutarAsync(Func<CancellationToken, Task> operacion, CancellationToken cancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(operacion);

        if (_contexto.Database.CurrentTransaction is not null)
        {
            throw new InvalidOperationException("Ya hay una operación en curso en esta unidad de trabajo.");
        }

        // Si algo falla antes del commit, liberar la transacción la revierte.
        await using var transaccion = await _contexto.Database.BeginTransactionAsync(cancelacion);
        try
        {
            await operacion(cancelacion);
            await _contexto.SaveChangesAsync(cancelacion);
            await transaccion.CommitAsync(cancelacion);
        }
        catch (Exception excepcion) when (TraductorDeErroresDeBase.Traducir(excepcion) is not null)
        {
            throw TraductorDeErroresDeBase.Traducir(excepcion)!;
        }
    }
}
