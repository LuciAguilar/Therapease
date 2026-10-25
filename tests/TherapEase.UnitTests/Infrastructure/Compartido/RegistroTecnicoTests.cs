using Microsoft.Extensions.Logging;
using TherapEase.Application.Compartido.Interfaces.Servicios;
using TherapEase.Infrastructure.Compartido.Servicios;

namespace TherapEase.UnitTests.Infrastructure.Compartido;

public class RegistroTecnicoTests
{
    private const string AvisoDeSalida = "La sesión se cerró, pero no se pudo guardar el evento de cierre de sesión en la auditoría.";
    private const string AvisoDeBloqueo = "La cuenta quedó bloqueada, pero no se pudo guardar el evento de bloqueo de acceso en la auditoría.";

    private readonly RegistroEnMemoria _registro = new();
    private readonly RegistroTecnico _tecnico;

    public RegistroTecnicoTests()
    {
        _tecnico = new RegistroTecnico(_registro);
    }

    [Fact]
    public void EventoDeSalidaNoGuardado_DejaUnAvisoFijoSinExcepcionNiDatos()
    {
        _tecnico.EventoDeSalidaNoGuardado();

        var entrada = Assert.Single(_registro.Entradas);
        Assert.Equal(LogLevel.Warning, entrada.Nivel);
        Assert.Equal(AvisoDeSalida, entrada.Mensaje);
        Assert.Null(entrada.Excepcion);
        Assert.Equal(4001, entrada.Evento.Id);
    }

    [Fact]
    public void EventoDeBloqueoNoGuardado_DejaUnAvisoFijoSinExcepcionNiDatos()
    {
        _tecnico.EventoDeBloqueoNoGuardado();

        var entrada = Assert.Single(_registro.Entradas);
        Assert.Equal(LogLevel.Warning, entrada.Nivel);
        Assert.Equal(AvisoDeBloqueo, entrada.Mensaje);
        Assert.Null(entrada.Excepcion);
        Assert.Equal(4002, entrada.Evento.Id);
    }

    [Fact]
    public void CadaAviso_EsSiempreElMismoTextoEnCadaLlamada()
    {
        _tecnico.EventoDeSalidaNoGuardado();
        _tecnico.EventoDeSalidaNoGuardado();
        _tecnico.EventoDeBloqueoNoGuardado();
        _tecnico.EventoDeBloqueoNoGuardado();

        Assert.Equal([AvisoDeSalida, AvisoDeSalida, AvisoDeBloqueo, AvisoDeBloqueo], _registro.Entradas.Select(e => e.Mensaje));
    }

    // Que ningún aviso reciba parámetros garantiza que no se les pueda pasar un nombre de usuario, una contraseña ni datos de la solicitud.
    [Fact]
    public void LosAvisos_NoRecibenParametros()
    {
        var metodos = typeof(IRegistroTecnico).GetMethods();

        Assert.NotEmpty(metodos);
        Assert.All(metodos, m => Assert.Empty(m.GetParameters()));
        Assert.All(metodos, m => Assert.Equal(typeof(void), m.ReturnType));
    }

    private sealed class RegistroEnMemoria : ILogger<RegistroTecnico>
    {
        public List<(LogLevel Nivel, EventId Evento, string Mensaje, Exception? Excepcion)> Entradas { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Entradas.Add((logLevel, eventId, formatter(state, exception), exception));
    }
}
