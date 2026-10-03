using TherapEase.Application.Compartido.Interfaces.Servicios;
using TherapEase.Domain.Compartido.Entidades.Enums;

namespace TherapEase.UnitTests.Dobles;

internal sealed class AutorizacionFalsa : IAutorizacion
{
    public HashSet<Permiso> Permisos { get; } = [];

    public Task<bool> TienePermisoAsync(Permiso permiso, CancellationToken cancelacion = default) =>
        Task.FromResult(Permisos.Contains(permiso));
}
