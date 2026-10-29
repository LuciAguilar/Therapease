using Microsoft.Extensions.Logging;
using TherapEase.Application.Compartido.Interfaces.Servicios;

namespace TherapEase.Infrastructure.Compartido.Servicios;

// Usa el registro que ya incluye .NET. Los avisos son textos fijos y sin parámetros: no se les puede pasar el nombre de usuario, la excepción ni la solicitud.
public sealed partial class RegistroTecnico : IRegistroTecnico
{
    private readonly ILogger<RegistroTecnico> _registro;

    public RegistroTecnico(ILogger<RegistroTecnico> registro)
    {
        _registro = registro;
    }

    public void EventoDeSalidaNoGuardado() => AvisoSalidaSinEvento(_registro);

    public void EventoDeBloqueoNoGuardado() => AvisoBloqueoSinEvento(_registro);

    [LoggerMessage(EventId = 4001, Level = LogLevel.Warning,
        Message = "La sesión se cerró, pero no se pudo guardar el evento de cierre de sesión en la auditoría.")]
    private static partial void AvisoSalidaSinEvento(ILogger registro);

    [LoggerMessage(EventId = 4002, Level = LogLevel.Warning,
        Message = "La cuenta quedó bloqueada, pero no se pudo guardar el evento de bloqueo de acceso en la auditoría.")]
    private static partial void AvisoBloqueoSinEvento(ILogger registro);
}
