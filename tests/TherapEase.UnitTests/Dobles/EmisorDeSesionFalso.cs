using TherapEase.Application.Identidad.Interfaces.Servicios;

namespace TherapEase.UnitTests.Dobles;

internal sealed class EmisorDeSesionFalso : IEmisorDeSesion
{
    public List<Guid> Abiertas { get; } = [];

    public List<Guid> Refrescadas { get; } = [];

    public int Cierres { get; private set; }

    public Task AbrirAsync(Guid idUsuario, CancellationToken cancelacion = default)
    {
        Abiertas.Add(idUsuario);
        return Task.CompletedTask;
    }

    public Task RefrescarAsync(Guid idUsuario, CancellationToken cancelacion = default)
    {
        Refrescadas.Add(idUsuario);
        return Task.CompletedTask;
    }

    public Task CerrarAsync(CancellationToken cancelacion = default)
    {
        Cierres++;
        return Task.CompletedTask;
    }
}
