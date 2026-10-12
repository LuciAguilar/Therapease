using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using TherapEase.UnitTests.Dobles;
using TherapEase.Web.Seguridad;

namespace TherapEase.UnitTests.Web;

public class CabecerasDeSeguridadTests
{
    private static DefaultHttpContext CrearContexto(RespuestaHttpQueEjecutaOnStarting? respuesta = null, object? metadatoDelPunto = null, bool conPunto = true)
    {
        var caracteristicas = new FeatureCollection();
        caracteristicas.Set<IHttpRequestFeature>(new HttpRequestFeature());
        caracteristicas.Set<IHttpResponseFeature>(respuesta ?? new RespuestaHttpQueEjecutaOnStarting());
        var contexto = new DefaultHttpContext(caracteristicas);
        if (conPunto)
        {
            var metadatos = metadatoDelPunto is null ? new EndpointMetadataCollection() : new EndpointMetadataCollection(metadatoDelPunto);
            contexto.SetEndpoint(new Endpoint(null, metadatos, "punto de prueba"));
        }

        return contexto;
    }

    private static void AssertLasCuatroCabeceras(IHeaderDictionary cabeceras)
    {
        Assert.Equal("nosniff", cabeceras.XContentTypeOptions);
        Assert.Equal("frame-ancestors 'none'", cabeceras.ContentSecurityPolicy);
        Assert.Equal("DENY", cabeceras.XFrameOptions);
        Assert.Equal("no-referrer", cabeceras["Referrer-Policy"]);
    }

    [Fact]
    public void Aplicar_PoneLasCuatroCabecerasConLosValoresAprobados()
    {
        var contexto = CrearContexto(metadatoDelPunto: new AllowAnonymousAttribute());

        CabecerasDeSeguridad.Aplicar(contexto);

        AssertLasCuatroCabeceras(contexto.Response.Headers);
    }

    [Fact]
    public void Aplicar_SinPuntoDeAccesoEncontrado_PoneLasCuatroPeroNoNoStore()
    {
        var contexto = CrearContexto(conPunto: false);

        CabecerasDeSeguridad.Aplicar(contexto);

        AssertLasCuatroCabeceras(contexto.Response.Headers);
        Assert.False(contexto.Response.Headers.ContainsKey("Cache-Control"));
    }

    [Fact]
    public void Aplicar_ConContenidoQueExigeSesion_AgregaNoStore()
    {
        var contexto = CrearContexto();

        CabecerasDeSeguridad.Aplicar(contexto);

        Assert.Equal("no-store", contexto.Response.Headers.CacheControl);
    }

    [Fact]
    public void Aplicar_ConContenidoProtegidoPorAuthorize_AgregaNoStore()
    {
        var contexto = CrearContexto(metadatoDelPunto: new AuthorizeAttribute());

        CabecerasDeSeguridad.Aplicar(contexto);

        Assert.Equal("no-store", contexto.Response.Headers.CacheControl);
    }

    [Fact]
    public void Aplicar_ConContenidoPrivadoQueDeclaraUnaCacheMasPermisiva_LaSustituyePorNoStore()
    {
        var contexto = CrearContexto();
        contexto.Response.Headers.CacheControl = "public, max-age=3600";

        CabecerasDeSeguridad.Aplicar(contexto);

        Assert.Equal("no-store", contexto.Response.Headers.CacheControl);
    }

    [Fact]
    public void Aplicar_ConContenidoAnonimoComoArchivosEstaticos_NoAplicaNoStore()
    {
        var contexto = CrearContexto(metadatoDelPunto: new AllowAnonymousAttribute());
        contexto.Response.Headers.CacheControl = "public, max-age=31536000, immutable";

        CabecerasDeSeguridad.Aplicar(contexto);

        Assert.Equal("public, max-age=31536000, immutable", contexto.Response.Headers.CacheControl);
    }

    [Fact]
    public void Aplicar_ConContenidoAnonimoSinCacheDeclarada_NoLeAgregaNoStore()
    {
        var contexto = CrearContexto(metadatoDelPunto: new AllowAnonymousAttribute());

        CabecerasDeSeguridad.Aplicar(contexto);

        Assert.False(contexto.Response.Headers.ContainsKey("Cache-Control"));
    }

    [Fact]
    public async Task Middleware_LasCabecerasLlegaAlIniciarLaRespuesta_AunConCodigoDeError()
    {
        var respuesta = new RespuestaHttpQueEjecutaOnStarting();
        var contexto = CrearContexto(respuesta);
        var aplicacion = new ApplicationBuilder(new ServiceCollection().BuildServiceProvider());
        aplicacion.UsarCabecerasDeSeguridad();
        aplicacion.Run(c =>
        {
            c.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        });

        await aplicacion.Build()(contexto);
        Assert.False(contexto.Response.Headers.ContainsKey("X-Frame-Options"));
        await respuesta.IniciarAsync();

        Assert.Equal(StatusCodes.Status401Unauthorized, contexto.Response.StatusCode);
        AssertLasCuatroCabeceras(contexto.Response.Headers);
        Assert.Equal("no-store", contexto.Response.Headers.CacheControl);
    }

    [Fact]
    public async Task Middleware_SiLaSiguienteEtapaLanzaUnaExcepcion_LasCabecerasQuedanRegistradas()
    {
        var respuesta = new RespuestaHttpQueEjecutaOnStarting();
        var contexto = CrearContexto(respuesta);
        var aplicacion = new ApplicationBuilder(new ServiceCollection().BuildServiceProvider());
        aplicacion.UsarCabecerasDeSeguridad();
        aplicacion.Run(_ => throw new InvalidOperationException("fallo simulado"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => aplicacion.Build()(contexto));
        await respuesta.IniciarAsync();

        AssertLasCuatroCabeceras(contexto.Response.Headers);
    }
}
