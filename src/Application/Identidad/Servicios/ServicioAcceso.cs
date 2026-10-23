using TherapEase.Application.Auditoria.Interfaces.Servicios;
using TherapEase.Application.Compartido.Interfaces.Repositorios;
using TherapEase.Application.Compartido.Interfaces.Servicios;
using TherapEase.Application.Compartido.Modelos;
using TherapEase.Application.Identidad.Constantes;
using TherapEase.Application.Identidad.Interfaces.Repositorios;
using TherapEase.Application.Identidad.Interfaces.Servicios;
using TherapEase.Application.Identidad.Modelos;

namespace TherapEase.Application.Identidad.Servicios;

public class ServicioAcceso : IServicioAcceso
{
    private readonly IGestorIdentidad _gestor;
    private readonly IEmisorDeSesion _emisor;
    private readonly IUnidadDeTrabajo _unidad;
    private readonly IServicioAuditoria _auditoria;
    private readonly IRegistroTecnico _registroTecnico;
    private readonly IUsuarioActual _usuarioActual;

    public ServicioAcceso(
        IGestorIdentidad gestor,
        IEmisorDeSesion emisor,
        IUnidadDeTrabajo unidad,
        IServicioAuditoria auditoria,
        IRegistroTecnico registroTecnico,
        IUsuarioActual usuarioActual)
    {
        _gestor = gestor;
        _emisor = emisor;
        _unidad = unidad;
        _auditoria = auditoria;
        _registroTecnico = registroTecnico;
        _usuarioActual = usuarioActual;
    }

    public async Task<RespuestaServicio<DatosUsuario>> IniciarSesionAsync(
        string nombreUsuario, string contrasena, CancellationToken cancelacion = default)
    {
        var nombre = nombreUsuario?.Trim();
        if (string.IsNullOrEmpty(nombre) || string.IsNullOrEmpty(contrasena))
        {
            return RespuestaServicio<DatosUsuario>.Fallida(MensajesDeIdentidad.CredencialesInvalidas);
        }

        // La verificación y el conteo de fallos se confirman siempre, incluso cuando el acceso se niega.
        // Si la entrada es correcta, su evento se guarda en la misma transacción: si falla, nada se confirma, no se abre sesión
        // y la excepción llega a la respuesta de error general (CU01 A7); no se presenta como credenciales incorrectas.
        ResultadoDeVerificacion? verificacion = null;
        await _unidad.EjecutarAsync(async ct =>
        {
            verificacion = await _gestor.VerificarCredencialesAsync(nombre, contrasena, ct);
            if (verificacion.Usuario is { } entrante)
            {
                await EventosDeAcceso.RegistrarEntradaAsync(_auditoria, entrante.IdUsuario, ct);
            }
        }, cancelacion);

        // El bloqueo ya está confirmado; su evento va aparte y no cambia lo que ve la persona (AGENTS §2.3).
        if (verificacion!.IdUsuarioBloqueado is { } idBloqueado)
        {
            await EventosDeAcceso.RegistrarBloqueoAsync(_unidad, _auditoria, _registroTecnico, idBloqueado, cancelacion);
        }

        if (verificacion.Usuario is not { } usuario)
        {
            return RespuestaServicio<DatosUsuario>.Fallida(MensajesDeIdentidad.CredencialesInvalidas);
        }

        // La sesión solo se abre después del commit (HU01 CA5).
        await _emisor.AbrirAsync(usuario.IdUsuario, cancelacion);
        return RespuestaServicio<DatosUsuario>.Correcta(usuario);
    }

    public async Task CerrarSesionAsync(CancellationToken cancelacion = default)
    {
        if (_usuarioActual.IdUsuario is not { } idUsuario)
        {
            // Sin sesión o ya vencida no hay cuenta a la que atribuir el evento; solo se retira la cookie.
            await _emisor.CerrarAsync(cancelacion);
            return;
        }

        await _gestor.RevocarSesionesAsync(idUsuario, cancelacion);
        await _emisor.CerrarAsync(cancelacion);

        // Primero se protege y después se registra, en otra transacción: un fallo del evento no reabre la sesión (AGENTS §2.3).
        await EventosDeAcceso.RegistrarSalidaAsync(_unidad, _auditoria, _registroTecnico, idUsuario, cancelacion);
    }
}
