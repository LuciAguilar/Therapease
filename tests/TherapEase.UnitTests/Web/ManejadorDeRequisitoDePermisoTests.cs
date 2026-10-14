using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using TherapEase.Application.Compartido.Interfaces.Servicios;
using TherapEase.Domain.Compartido.Entidades.Enums;
using TherapEase.UnitTests.Dobles;
using TherapEase.Web.Seguridad;
using TherapEase.Web.Seguridad.Manejadores;
using TherapEase.Web.Seguridad.Requisitos;

namespace TherapEase.UnitTests.Web;

public class ManejadorDeRequisitoDePermisoTests
{
    private static async Task<bool> Evaluar(IAutorizacion autorizacion, Permiso permiso)
    {
        var requisito = new RequisitoDePermiso(permiso);
        var contexto = new AuthorizationHandlerContext([requisito], new ClaimsPrincipal(new ClaimsIdentity("prueba")), null);
        await new ManejadorDeRequisitoDePermiso(autorizacion).HandleAsync(contexto);
        return contexto.HasSucceeded;
    }

    [Theory]
    [InlineData(Permiso.AdministrarUsuarios)]
    [InlineData(Permiso.ConsultarAuditoria)]
    public async Task ConPermisoConcedidoPorIAutorizacion_Autoriza(Permiso permiso)
    {
        var autorizacion = new AutorizacionFalsa { Permisos = { permiso } };

        Assert.True(await Evaluar(autorizacion, permiso));
    }

    [Theory]
    [InlineData(Permiso.AdministrarUsuarios)]
    [InlineData(Permiso.ConsultarAuditoria)]
    public async Task SinPermisoConcedidoPorIAutorizacion_NoAutoriza(Permiso permiso)
    {
        Assert.False(await Evaluar(new AutorizacionFalsa(), permiso));
    }

    [Fact]
    public async Task PideAIAutorizacionElPermisoDelRequisito_NoOtro()
    {
        var autorizacion = new AutorizacionFalsa { Permisos = { Permiso.AdministrarUsuarios } };

        Assert.False(await Evaluar(autorizacion, Permiso.ConsultarAuditoria));
        Assert.True(await Evaluar(autorizacion, Permiso.AdministrarUsuarios));
    }

    [Fact]
    public async Task ElManejadorRegistradoSeResuelveYSeAislaPorSolicitud()
    {
        var servicios = new ServiceCollection();
        servicios.AddScoped<IAutorizacion, AutorizacionFalsa>();
        servicios.AgregarSeguridadWeb();
        await using var proveedor = servicios.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

        await using var solicitudA = proveedor.CreateAsyncScope();
        await using var solicitudB = proveedor.CreateAsyncScope();
        ((AutorizacionFalsa)solicitudA.ServiceProvider.GetRequiredService<IAutorizacion>()).Permisos.Add(Permiso.ConsultarAuditoria);

        var manejadorA = solicitudA.ServiceProvider.GetServices<IAuthorizationHandler>().OfType<ManejadorDeRequisitoDePermiso>().Single();
        var manejadorB = solicitudB.ServiceProvider.GetServices<IAuthorizationHandler>().OfType<ManejadorDeRequisitoDePermiso>().Single();

        Assert.NotSame(manejadorA, manejadorB);
        Assert.True(await EvaluarConManejador(manejadorA, Permiso.ConsultarAuditoria));
        Assert.False(await EvaluarConManejador(manejadorB, Permiso.ConsultarAuditoria));
    }

    private static async Task<bool> EvaluarConManejador(IAuthorizationHandler manejador, Permiso permiso)
    {
        var requisito = new RequisitoDePermiso(permiso);
        var contexto = new AuthorizationHandlerContext([requisito], new ClaimsPrincipal(new ClaimsIdentity("prueba")), null);
        await manejador.HandleAsync(contexto);
        return contexto.HasSucceeded;
    }
}
