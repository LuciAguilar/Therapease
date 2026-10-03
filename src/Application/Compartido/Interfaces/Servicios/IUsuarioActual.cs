namespace TherapEase.Application.Compartido.Interfaces.Servicios;

// Identidad de la sesión en curso; el actor de un cambio nunca se toma de un parámetro enviado por el cliente.
public interface IUsuarioActual
{
    Guid? IdUsuario { get; }
}
