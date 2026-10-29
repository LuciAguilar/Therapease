using Microsoft.AspNetCore.Http.Features;

namespace TherapEase.UnitTests.Dobles;

// DefaultHttpContext no ejecuta los callbacks de OnStarting; este doble los guarda y los ejecuta en orden inverso, como el servidor al iniciar la respuesta.
internal sealed class RespuestaHttpQueEjecutaOnStarting : HttpResponseFeature
{
    private readonly List<(Func<object, Task> Callback, object Estado)> _callbacks = [];

    public override void OnStarting(Func<object, Task> callback, object state) => _callbacks.Add((callback, state));

    public async Task IniciarAsync()
    {
        for (var i = _callbacks.Count - 1; i >= 0; i--)
        {
            await _callbacks[i].Callback(_callbacks[i].Estado);
        }
    }
}
