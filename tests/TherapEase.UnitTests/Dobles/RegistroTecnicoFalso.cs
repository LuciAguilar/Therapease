using TherapEase.Application.Compartido.Interfaces.Servicios;

namespace TherapEase.UnitTests.Dobles;

internal sealed class RegistroTecnicoFalso : IRegistroTecnico
{
    public int AvisosDeSalida { get; private set; }

    public int AvisosDeBloqueo { get; private set; }

    public int Total => AvisosDeSalida + AvisosDeBloqueo;

    public void EventoDeSalidaNoGuardado() => AvisosDeSalida++;

    public void EventoDeBloqueoNoGuardado() => AvisosDeBloqueo++;
}
