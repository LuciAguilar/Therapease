namespace TherapEase.Application.Compartido.Interfaces.Servicios;

// Registro técnico para fallos que no deben impedir una protección ya aplicada (AGENTS §2.3). Cada aviso es un texto fijo:
// no recibe parámetros, así que no puede llevar nombres de usuario, contraseñas, IP ni datos de la solicitud.
public interface IRegistroTecnico
{
    // La sesión ya quedó cerrada, pero no se pudo guardar el evento CierreSesion.
    void EventoDeSalidaNoGuardado();

    // La cuenta ya quedó bloqueada, pero no se pudo guardar el evento BloqueoAcceso.
    void EventoDeBloqueoNoGuardado();
}
