using TherapEase.Application.Auditoria.Interfaces.Servicios;
using TherapEase.Application.Auditoria.Servicios;
using TherapEase.Application.Compartido.Interfaces.Servicios;
using TherapEase.Application.Identidad.Interfaces.Servicios;
using TherapEase.Application.Identidad.Servicios;

namespace TherapEase.Web.Configuracion;

public static class ServiciosDeAplicacion
{
    public static IServiceCollection AgregarServiciosDeAplicacion(this IServiceCollection servicios)
    {
        servicios.AddSingleton(TimeProvider.System);
        servicios.AddScoped<IAutorizacion, Autorizacion>();
        servicios.AddScoped<IServicioAuditoria, ServicioAuditoria>();
        servicios.AddScoped<IServicioIdentidad, ServicioIdentidad>();
        servicios.AddScoped<IServicioAcceso, ServicioAcceso>();
        servicios.AddScoped<IServicioUsuarios, ServicioUsuarios>();
        servicios.AddScoped<IServicioContrasena, ServicioContrasena>();
        servicios.AddScoped<IServicioArranqueDeIdentidad, ServicioArranqueDeIdentidad>();
        return servicios;
    }
}
