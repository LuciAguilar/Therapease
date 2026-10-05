namespace TherapEase.Application.Compartido.Modelos;

// Resultado uniforme de los servicios de Application 
public class RespuestaServicio<T>
{
    public T? Datos { get; init; }

    public bool Exito { get; init; }

    public string Mensaje { get; init; } = string.Empty;

    public static RespuestaServicio<T> Correcta(T datos, string mensaje = "") =>
        new() { 
            Exito = true, 
            Datos = datos, 
            Mensaje = mensaje 
        };

    public static RespuestaServicio<T> Fallida(string mensaje) =>
        new() { 
            Exito = false, 
            Mensaje = mensaje 
        };
}
