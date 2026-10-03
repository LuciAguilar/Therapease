using TherapEase.Application.Auditoria.Interfaces.Servicios;
using TherapEase.Application.Compartido.Interfaces.Repositorios;
using TherapEase.Application.Compartido.Interfaces.Servicios;
using TherapEase.Application.Compartido.Modelos;
using TherapEase.Application.Identidad.Constantes;
using TherapEase.Application.Identidad.Interfaces.Repositorios;
using TherapEase.Application.Identidad.Interfaces.Servicios;
using TherapEase.Application.Identidad.Modelos;
using TherapEase.Domain.Auditoria.Entidades.Enums;
using TherapEase.Domain.Compartido.Excepciones;

namespace TherapEase.Application.Identidad.Servicios;

public class ServicioContrasena : IServicioContrasena
{
    private readonly IGestorIdentidad _gestor;
    private readonly IEmisorDeSesion _emisor;
    private readonly IUnidadDeTrabajo _unidad;
    private readonly IServicioAuditoria _auditoria;
    private readonly IUsuarioActual _usuarioActual;

    public ServicioContrasena(
        IGestorIdentidad gestor,
        IEmisorDeSesion emisor,
        IUnidadDeTrabajo unidad,
        IServicioAuditoria auditoria,
        IUsuarioActual usuarioActual)
    {
        _gestor = gestor;
        _emisor = emisor;
        _unidad = unidad;
        _auditoria = auditoria;
        _usuarioActual = usuarioActual;
    }

    public async Task<RespuestaServicio<DatosUsuario>> CambiarContrasenaPropiaAsync(
        string? contrasenaActual, string contrasenaNueva, CancellationToken cancelacion = default)
    {
        if (_usuarioActual.IdUsuario is not { } idUsuario || string.IsNullOrEmpty(contrasenaNueva))
        {
            return RespuestaServicio<DatosUsuario>.Fallida(MensajesDeIdentidad.CambioDeContrasenaNoRealizado);
        }

        var usuario = await _gestor.ObtenerAsync(idUsuario, cancelacion);
        if (usuario is not { Activo: true })
        {
            return RespuestaServicio<DatosUsuario>.Fallida(MensajesDeIdentidad.CambioDeContrasenaNoRealizado);
        }

        // En el cambio ordinario se exige la actual; un intento fallido cuenta para el bloqueo (CU11 A1).
        if (!usuario.DebeCambiarContrasena)
        {
            var actualCorrecta = false;
            if (!string.IsNullOrEmpty(contrasenaActual))
            {
                await _unidad.EjecutarAsync(
                    async ct => actualCorrecta = await _gestor.VerificarContrasenaActualAsync(idUsuario, contrasenaActual, ct), cancelacion);
            }

            if (!actualCorrecta)
            {
                return RespuestaServicio<DatosUsuario>.Fallida(MensajesDeIdentidad.CambioDeContrasenaNoRealizado);
            }
        }

        ResultadoDeContrasena? resultado = null;
        try
        {
            await _unidad.EjecutarAsync(async ct =>
            {
                resultado = await _gestor.EstablecerContrasenaAsync(idUsuario, contrasenaNueva, debeCambiarContrasena: false, ct);
                if (resultado == ResultadoDeContrasena.Establecida)
                {
                    await _auditoria.RegistrarHechoDeCambioAsync(
                        idUsuario, TipoRegistroAuditoria.Usuario, idUsuario, AccionAuditoria.CambioContrasena,
                        ["Contrasena"], "El usuario cambió su propia contraseña.", ct);
                }
            }, cancelacion);
        }
        catch (ReglaDeNegocioException excepcion)
        {
            return RespuestaServicio<DatosUsuario>.Fallida(excepcion.Message);
        }

        if (resultado != ResultadoDeContrasena.Establecida)
        {
            return RespuestaServicio<DatosUsuario>.Fallida(MensajesDeIdentidad.ContrasenaNuevaNoCumpleReglas);
        }

        // El cambio renovó el sello: se reemite la cookie de esta sesión; las demás dejan de valer.
        await _emisor.RefrescarAsync(idUsuario, cancelacion);
        return RespuestaServicio<DatosUsuario>.Correcta(usuario with { DebeCambiarContrasena = false }, "Contraseña actualizada.");
    }
}
