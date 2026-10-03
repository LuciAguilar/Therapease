using TherapEase.Application.Compartido.Interfaces.Servicios;

namespace TherapEase.UnitTests.Dobles;

internal sealed class UsuarioActualFalso : IUsuarioActual
{
    public Guid? IdUsuario { get; set; }
}
