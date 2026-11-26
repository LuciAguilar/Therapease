using Microsoft.EntityFrameworkCore;
using TherapEase.Application.Citas.Constantes;
using TherapEase.Application.Citas.Interfaces.Servicios;
using TherapEase.Application.Citas.Modelos;
using TherapEase.Application.Compartido.Modelos;
using TherapEase.Application.Compartido.Interfaces.Servicios;
using TherapEase.Application.Identidad.Interfaces.Servicios;
using TherapEase.Domain.Citas.Entidades.Enums;

namespace TherapEase.IntegrationTests;

/// <summary>
/// Comprueba las cuatro revocaciones reales antes de autenticar y durante una solicitud de pago.
/// Usa cookies solo en memoria y exige que la siguiente solicitud quede rechazada.
/// </summary>
public sealed partial class PagoDeCitasRevisionTests
{


    // ──── REVOCACIONES CON SERVICIOS REALES ────────────────────────────────────────────────


    /// Ejecuta la revocación desde otra solicitud; el usuario de pago no se sustituye por el administrador.
    private static async Task RevocarAsync(PacientesRevision e, string administrador, string tipo)
    {
        if (tipo == "salir")
        {
            await e.EnSolicitudAsync(async s =>
            {
                await s.Obtener<IServicioAcceso>().CerrarSesionAsync();
                return true;
            });
            return;
        }

        await e.EnSolicitudAsync(async s =>
        {
            var usuarios = s.Obtener<IServicioUsuarios>();
            var exito = tipo switch
            {
                "desactivar" => (await usuarios.DesactivarUsuarioAsync(e.Actor)).Exito,
                "restablecer" => (await usuarios.RestablecerContrasenaAsync(e.Actor)).Exito,
                "rol" => (await usuarios.CambiarRolAsync(e.Actor, "Superusuario")).Exito,
                _ => throw new InvalidOperationException("Revocación de prueba desconocida.")
            };
            Assert.True(exito, "Debe confirmarse la revocación ficticia.");
            return true;
        }, cookie: administrador);
    }

    /// Separa los contextos de dos solicitudes reales: la revocación no debe borrar el usuario de la solicitud pausada en esta simulación.
    private static async Task RevocarEnOtraSolicitudAsync(PacientesRevision e, string administrador, string tipo)
    {
        Task revocacion;
        using (ExecutionContext.SuppressFlow())
            revocacion = Task.Run(() => RevocarAsync(e, administrador, tipo));
        await revocacion;
    }

    /// Una revocación anterior niega el pago; una solicitud ya autenticada conserva el límite aprobado, también antes del commit.
    [Theory]
    [InlineData("salir", "antes")]
    [InlineData("desactivar", "antes")]
    [InlineData("restablecer", "antes")]
    [InlineData("rol", "antes")]
    [InlineData("salir", "autenticada")]
    [InlineData("desactivar", "autenticada")]
    [InlineData("restablecer", "autenticada")]
    [InlineData("rol", "autenticada")]
    [InlineData("salir", "commit")]
    [InlineData("desactivar", "commit")]
    [InlineData("restablecer", "commit")]
    [InlineData("rol", "commit")]
    public async Task RevocacionRespetaElLimiteDeLaSolicitudYNiegaLaSiguiente(string tipo, string momento)
    {
        // Arrange
        var pausa = new PausaDePago();
        using var e = await PacientesRevision.CrearAsync(postgres, interceptores: [pausa]);
        var (paciente, cita) = await PrepararAsync(e);
        var administrador = await ServidorIdentidadRevision.PrepararSuperusuarioAsync(e.Proveedor);
        e.Reloj.Avanzar(TimeSpan.FromMilliseconds(1));
        RespuestaServicio<ResultadoDeCambioDePago> respuesta;

        // Act
        if (momento == "antes")
        {
            await RevocarAsync(e, administrador, tipo);
            respuesta = await e.EnSolicitudAsync(s => s.Obtener<IServicioCitas>().CambiarPagoAsync(cita, 1, EstadoPago.Pagado), exigirSesion: false);
        }
        else if (momento == "autenticada")
        {
            using var solicitud = new SolicitudIdentidad(e.Proveedor, e.Cookie);
            Assert.True(await solicitud.AutenticarAsync());
            await RevocarEnOtraSolicitudAsync(e, administrador, tipo);
            Assert.Equal(e.Actor, solicitud.Obtener<IUsuarioActual>().IdUsuario);
            respuesta = await solicitud.Obtener<IServicioCitas>().CambiarPagoAsync(cita, 1, EstadoPago.Pagado);
        }
        else
        {
            pausa.Activar();
            var enCurso = CambiarAsync(e, cita, 1, EstadoPago.Pagado);
            try
            {
                await pausa.Alcanzada.Task.WaitAsync(TimeSpan.FromSeconds(20));
                await RevocarAsync(e, administrador, tipo);
            }
            finally
            {
                pausa.Continuar.TrySetResult();
                await enCurso.WaitAsync(TimeSpan.FromSeconds(20));
            }
            respuesta = await enCurso;
        }

        // Assert
        var confirmada = momento != "antes";
        Assert.Equal(confirmada, respuesta.Exito);
        if (confirmada)
        {
            Assert.Equal(EstadoDeCambioDePago.Actualizado, respuesta.Datos!.Estado);
            ExigirRespuestaMinima(respuesta, paciente);
        }
        else
        {
            Assert.Equal(MensajesDeCitas.SinPermiso, respuesta.Mensaje);
            Assert.Null(respuesta.Datos);
        }
        var guardada = await LeerAsync(e, cita);
        Assert.Equal(confirmada ? EstadoPago.Pagado : EstadoPago.Pendiente, guardada.EstadoPago);
        Assert.Equal(confirmada ? 2 : 1, guardada.Version);
        Assert.Equal(confirmada ? 1 : 0, await EventosAsync(e, cita));

        // Act
        e.Reloj.Avanzar(TimeSpan.FromMilliseconds(1));
        var siguiente = await e.EnSolicitudAsync(async s =>
        {
            var servicio = s.Obtener<IServicioCitas>();
            return (Cambio: await servicio.CambiarPagoAsync(cita, guardada.Version, EstadoPago.Pendiente),
                Consulta: await servicio.ObtenerCitaAsync(cita));
        }, exigirSesion: false);

        // Assert
        Assert.Equal(MensajesDeCitas.SinPermiso, siguiente.Cambio.Mensaje);
        Assert.Equal(MensajesDeCitas.SinPermiso, siguiente.Consulta.Mensaje);
        Assert.Null(siguiente.Cambio.Datos);
        Assert.Null(siguiente.Consulta.Datos);
        Assert.Equal(confirmada ? 1 : 0, await EventosAsync(e, cita));
    }
}
