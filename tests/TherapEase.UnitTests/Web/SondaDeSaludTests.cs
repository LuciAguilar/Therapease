using System.Net;
using TherapEase.Web.Salud;

namespace TherapEase.UnitTests.Web;

public class SondaDeSaludTests
{
    private sealed class ManejadorFalso(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        public List<Uri?> Solicitudes { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage solicitud, CancellationToken cancelacion)
        {
            Solicitudes.Add(solicitud.RequestUri);
            return Task.FromResult(responder(solicitud));
        }
    }

    [Fact]
    public async Task Ejecutar_ConRespuestaExitosa_DevuelveCeroYConsultaLaUrl()
    {
        var manejador = new ManejadorFalso(_ => new HttpResponseMessage(HttpStatusCode.OK));

        var resultado = await SondaDeSalud.EjecutarAsync("http://localhost:8080/salud", manejador);

        Assert.Equal(0, resultado);
        Assert.Equal(new Uri("http://localhost:8080/salud"), Assert.Single(manejador.Solicitudes));
    }

    [Fact]
    public async Task Ejecutar_ConRespuestaNoSaludable_DevuelveUno()
    {
        var manejador = new ManejadorFalso(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));

        var resultado = await SondaDeSalud.EjecutarAsync("http://localhost:8080/salud", manejador);

        Assert.Equal(1, resultado);
    }

    [Fact]
    public async Task Ejecutar_ConFalloDeConexion_DevuelveUno()
    {
        var manejador = new ManejadorFalso(_ => throw new HttpRequestException("sin conexion"));

        var resultado = await SondaDeSalud.EjecutarAsync("http://localhost:8080/salud", manejador);

        Assert.Equal(1, resultado);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("no-es-una-url")]
    [InlineData("ftp://localhost/salud")]
    [InlineData("http://ejemplo.test/salud")]
    public async Task Ejecutar_ConUrlInvalidaONoLocal_DevuelveUnoSinConsultar(string? url)
    {
        var manejador = new ManejadorFalso(_ => new HttpResponseMessage(HttpStatusCode.OK));

        var resultado = await SondaDeSalud.EjecutarAsync(url, manejador);

        Assert.Equal(1, resultado);
        Assert.Empty(manejador.Solicitudes);
    }
}
