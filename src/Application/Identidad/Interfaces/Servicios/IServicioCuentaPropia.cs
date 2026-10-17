using TherapEase.Application.Compartido.Modelos;
using TherapEase.Application.Identidad.Modelos;

namespace TherapEase.Application.Identidad.Interfaces.Servicios;

// Datos de la cuenta de quien tiene la sesión abierta. Solo lectura y sin evento: cambiarlos le toca al superusuario (ActualizarDatosDeCuentaAsync).
public interface IServicioCuentaPropia
{
    // No recibe identificador: toma la cuenta de la sesión en curso, así nadie consulta la de otra persona.
    // Funciona también con la contraseña temporal pendiente, porque la pantalla de cambio de contraseña (CU11) muestra el nombre.
    Task<RespuestaServicio<FichaDeUsuario>> ObtenerMisDatosAsync(CancellationToken cancelacion = default);
}
