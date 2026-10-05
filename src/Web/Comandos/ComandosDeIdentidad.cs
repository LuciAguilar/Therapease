using TherapEase.Application.Compartido.Modelos;
using TherapEase.Application.Identidad.Interfaces.Servicios;
using TherapEase.Application.Identidad.Modelos;
using TherapEase.Infrastructure.Configuracion;
using TherapEase.Web.Configuracion;

namespace TherapEase.Web.Comandos;

// Propuesta de Miguel, pendiente de decisión de Lucía: procedimientos de operador que entregan la contraseña temporal por la salida estándar, una sola vez.
public static class ComandosDeIdentidad
{
    public const string CrearSuperusuario = "--crear-superusuario";
    public const string RestablecerSuperusuario = "--restablecer-superusuario";

    public static bool EsComando(string[] args) =>
        Array.IndexOf(args, CrearSuperusuario) >= 0 || Array.IndexOf(args, RestablecerSuperusuario) >= 0;

    public static async Task<int> EjecutarAsync(string[] args)
    {
        var crear = Array.IndexOf(args, CrearSuperusuario);
        var indice = crear >= 0 ? crear : Array.IndexOf(args, RestablecerSuperusuario);
        var nombreUsuario = args.ElementAtOrDefault(indice + 1);
        if (string.IsNullOrWhiteSpace(nombreUsuario) || nombreUsuario.StartsWith("--", StringComparison.Ordinal))
        {
            Console.Error.WriteLine($"Uso: dotnet TherapEase.Web.dll {(crear >= 0 ? CrearSuperusuario : RestablecerSuperusuario)} <nombre-de-usuario>");
            return 2;
        }

        var constructor = Host.CreateApplicationBuilder(args);
        constructor.Logging.SetMinimumLevel(LogLevel.Warning);
        constructor.Services.AgregarPersistencia(constructor.Configuration.GetConnectionString("TherapEase"));
        constructor.Services.AgregarIdentidad();
        constructor.Services.AgregarServiciosDeAplicacion();

        using var host = constructor.Build();
        using var alcance = host.Services.CreateScope();
        var servicio = alcance.ServiceProvider.GetRequiredService<IServicioArranqueDeIdentidad>();

        var respuesta = crear >= 0
            ? await servicio.CrearPrimerSuperusuarioAsync(nombreUsuario)
            : await servicio.RestablecerSuperusuarioAsync(nombreUsuario);

        return Informar(respuesta);
    }

    private static int Informar(RespuestaServicio<ContrasenaTemporalEntregada> respuesta)
    {
        if (!respuesta.Exito || respuesta.Datos is null)
        {
            Console.Error.WriteLine(respuesta.Mensaje);
            return 1;
        }

        Console.WriteLine(respuesta.Mensaje);
        Console.WriteLine($"Usuario: {respuesta.Datos.Usuario.NombreUsuario}");
        Console.WriteLine($"Contraseña temporal: {respuesta.Datos.ContrasenaTemporal}");
        Console.WriteLine("Debe cambiarla al entrar. No se volverá a mostrar.");
        return 0;
    }
}
