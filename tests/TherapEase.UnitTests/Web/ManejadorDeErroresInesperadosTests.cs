using System.Diagnostics;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using TherapEase.UnitTests.Dobles;
using TherapEase.Web.Configuracion;

namespace TherapEase.UnitTests.Web;

public class ManejadorDeErroresInesperadosTests
{
    private const string TextoPrivado = "Password=clave-ficticia;Host=servidor-privado;usuario-ficticio";

    // Un proveedor que falla si alguien le pide un servicio: demuestra que el manejador no usa Identity, la base ni nada de la solicitud.
    private sealed class ProveedorQueNoDebeUsarse : IServiceProvider
    {
        public object? GetService(Type serviceType) => throw new InvalidOperationException("El manejador no debe pedir servicios.");
    }

    private static DefaultHttpContext CrearContexto(string ruta, Exception? error = null)
    {
        var caracteristicas = new FeatureCollection();
        caracteristicas.Set<IHttpRequestFeature>(new HttpRequestFeature { Path = ruta, QueryString = "?dato=" + TextoPrivado });
        caracteristicas.Set<IHttpResponseFeature>(new RespuestaHttpQueEjecutaOnStarting());
        caracteristicas.Set<IHttpResponseBodyFeature>(new StreamResponseBodyFeature(new MemoryStream()));
        var contexto = new DefaultHttpContext(caracteristicas) { RequestServices = new ProveedorQueNoDebeUsarse() };
        contexto.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "usuario-ficticio")], "prueba"));
        if (error is not null)
        {
            contexto.Features.Set<IExceptionHandlerFeature>(new ExceptionHandlerFeature { Error = error, Path = ruta });
        }

        return contexto;
    }

    private static string Cuerpo(HttpContext contexto)
    {
        contexto.Response.Body.Position = 0;
        return new StreamReader(contexto.Response.Body, Encoding.UTF8).ReadToEnd();
    }

    [Fact]
    public async Task Responder_EnUnaRutaTecnica_DevuelveElJsonFijoConEstado500()
    {
        var contexto = CrearContexto("/api/auditoria/eventos", new InvalidOperationException(TextoPrivado));

        await ManejadorDeErroresInesperados.ResponderAsync(contexto);

        Assert.Equal(500, contexto.Response.StatusCode);
        Assert.StartsWith("application/json", contexto.Response.ContentType);
        var cuerpo = Cuerpo(contexto);
        Assert.Equal(ManejadorDeErroresInesperados.CuerpoJson, cuerpo);
        using var documento = JsonDocument.Parse(cuerpo);
        Assert.Equal(ManejadorDeErroresInesperados.MensajeGenerico, documento.RootElement.GetProperty("error").GetString());
    }

    [Fact]
    public async Task Responder_EnUnaPagina_DevuelveElHtmlFijoConEstado500()
    {
        var contexto = CrearContexto("/Pacientes", new InvalidOperationException(TextoPrivado));

        await ManejadorDeErroresInesperados.ResponderAsync(contexto);

        Assert.Equal(500, contexto.Response.StatusCode);
        Assert.StartsWith("text/html", contexto.Response.ContentType);
        Assert.Equal(ManejadorDeErroresInesperados.CuerpoHtml, Cuerpo(contexto));
    }

    [Theory]
    [InlineData("/api/auditoria/eventos")]
    [InlineData("/Pacientes")]
    public async Task Responder_NoIncluyeDatosDeLaExcepcionDeLaSolicitudNiDelUsuario(string ruta)
    {
        var contexto = CrearContexto(ruta, new InvalidOperationException(TextoPrivado));

        await ManejadorDeErroresInesperados.ResponderAsync(contexto);

        var todo = Cuerpo(contexto) + string.Join(";", contexto.Response.Headers.Select(h => h.Key + "=" + h.Value));
        Assert.DoesNotContain("clave-ficticia", todo);
        Assert.DoesNotContain("servidor-privado", todo);
        Assert.DoesNotContain("usuario-ficticio", todo);
        Assert.DoesNotContain("Exception", todo);
        Assert.DoesNotContain("StackTrace", todo);
    }

    [Fact]
    public async Task Responder_NoPideServiciosDeLaSolicitud_ParaNoDependerDeIdentityNiDeLaBase()
    {
        var contexto = CrearContexto("/api/auditoria/eventos", new InvalidOperationException("fallo de base"));

        var excepcion = await Record.ExceptionAsync(() => ManejadorDeErroresInesperados.ResponderAsync(contexto));

        Assert.Null(excepcion);
        Assert.Equal(500, contexto.Response.StatusCode);
    }

    [Fact]
    public async Task Responder_IncluyeLasCabecerasDeSeguridadYNoStore()
    {
        var contexto = CrearContexto("/api/auditoria/eventos");

        await ManejadorDeErroresInesperados.ResponderAsync(contexto);

        Assert.Equal("nosniff", contexto.Response.Headers.XContentTypeOptions);
        Assert.Equal("frame-ancestors 'none'", contexto.Response.Headers.ContentSecurityPolicy);
        Assert.Equal("DENY", contexto.Response.Headers.XFrameOptions);
        Assert.Equal("no-referrer", contexto.Response.Headers["Referrer-Policy"]);
        Assert.Equal("no-store", contexto.Response.Headers.CacheControl);
    }

    [Fact]
    public async Task Responder_ElCuerpoEsIgualParaCualquierExcepcion()
    {
        var uno = CrearContexto("/api/x", new InvalidOperationException("uno"));
        var otro = CrearContexto("/api/x", new TimeoutException(TextoPrivado));

        await ManejadorDeErroresInesperados.ResponderAsync(uno);
        await ManejadorDeErroresInesperados.ResponderAsync(otro);

        Assert.Equal(Cuerpo(uno), Cuerpo(otro));
    }

    [Fact]
    public async Task UsarRespuestaDeErrorGenerica_ConUnaExcepcionEnElPipeline_ResponderConElCuerpoFijo()
    {
        var servicios = new ServiceCollection();
        servicios.AddLogging();
        servicios.AddMetrics();
        servicios.AddSingleton(new DiagnosticListener("pruebas"));
        var aplicacion = new ApplicationBuilder(servicios.BuildServiceProvider());
        aplicacion.UsarRespuestaDeErrorGenerica();
        aplicacion.Run(_ => throw new InvalidOperationException(TextoPrivado));
        var contexto = CrearContexto("/api/auditoria/eventos");
        contexto.RequestServices = servicios.BuildServiceProvider();

        await aplicacion.Build()(contexto);

        Assert.Equal(500, contexto.Response.StatusCode);
        Assert.Equal(ManejadorDeErroresInesperados.CuerpoJson, Cuerpo(contexto));
    }
}
