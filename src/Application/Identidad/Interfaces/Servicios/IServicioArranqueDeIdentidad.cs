using TherapEase.Application.Compartido.Modelos;
using TherapEase.Application.Identidad.Modelos;

namespace TherapEase.Application.Identidad.Interfaces.Servicios;

// Propuesta de Miguel, pendiente de decisión de Lucía: procedimiento de operador, sin correo ni contraseña fija en el código.
public interface IServicioArranqueDeIdentidad
{
    // Solo mientras no exista ningún superusuario activo.
    Task<RespuestaServicio<ContrasenaTemporalEntregada>> CrearPrimerSuperusuarioAsync(
        string nombreUsuario, 
        CancellationToken cancelacion = default
        );

    // Recuperación cuando ningún superusuario puede entrar; no levanta un bloqueo vigente.
    Task<RespuestaServicio<ContrasenaTemporalEntregada>> RestablecerSuperusuarioAsync(
        string nombreUsuario, 
        CancellationToken cancelacion = default
        );
}
