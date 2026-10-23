using TherapEase.Application.Auditoria.Interfaces.Servicios;
using TherapEase.Application.Compartido.Interfaces.Repositorios;
using TherapEase.Application.Compartido.Interfaces.Servicios;
using TherapEase.Domain.Auditoria.Entidades.Enums;

namespace TherapEase.Application.Identidad.Servicios;

// Eventos de entrada, salida y bloqueo. Actor y registro son siempre la propia cuenta. Los hechos son textos fijos y los campos van vacíos:
// nunca se guardan el nombre escrito, la contraseña, la IP ni el navegador.
internal static class EventosDeAcceso
{
    public const string HechoInicioSesion = "Inició sesión.";
    public const string HechoCierreSesion = "Cerró sesión.";
    public const string HechoBloqueoAcceso = "Se bloqueó el acceso por cinco intentos fallidos.";

    // Sin transacción propia: debe llamarse dentro de IUnidadDeTrabajo, para que el evento y la verificación se confirmen juntos.
    public static Task RegistrarEntradaAsync(IServicioAuditoria auditoria, Guid idUsuario, CancellationToken cancelacion) =>
        auditoria.RegistrarHechoDeCambioAsync(
            idUsuario, TipoRegistroAuditoria.Usuario, idUsuario, AccionAuditoria.InicioSesion, [], HechoInicioSesion, cancelacion);

    // La sesión ya quedó cerrada: el evento va en otra transacción y, si no se guarda, solo queda el aviso técnico fijo (AGENTS §2.3).
    public static Task RegistrarSalidaAsync(
        IUnidadDeTrabajo unidad, IServicioAuditoria auditoria, IRegistroTecnico registro, Guid idUsuario, CancellationToken cancelacion) =>
        RegistrarDespuesDeLaProteccionAsync(
            unidad, auditoria, idUsuario, AccionAuditoria.CierreSesion, HechoCierreSesion, registro.EventoDeSalidaNoGuardado, cancelacion);

    // El bloqueo ya quedó guardado: igual que la salida, un fallo del evento no lo deshace.
    public static Task RegistrarBloqueoAsync(
        IUnidadDeTrabajo unidad, IServicioAuditoria auditoria, IRegistroTecnico registro, Guid idUsuario, CancellationToken cancelacion) =>
        RegistrarDespuesDeLaProteccionAsync(
            unidad, auditoria, idUsuario, AccionAuditoria.BloqueoAcceso, HechoBloqueoAcceso, registro.EventoDeBloqueoNoGuardado, cancelacion);

    private static async Task RegistrarDespuesDeLaProteccionAsync(
        IUnidadDeTrabajo unidad, IServicioAuditoria auditoria, Guid idUsuario, AccionAuditoria accion, string hecho, Action avisoTecnico, CancellationToken cancelacion)
    {
        try
        {
            await unidad.EjecutarAsync(
                ct => auditoria.RegistrarHechoDeCambioAsync(idUsuario, TipoRegistroAuditoria.Usuario, idUsuario, accion, [], hecho, ct),
                cancelacion);
        }
        catch (Exception)
        {
            // La excepción no se registra ni se propaga: su texto podría traer datos de la base. El aviso es fijo.
            avisoTecnico();
        }
    }
}
