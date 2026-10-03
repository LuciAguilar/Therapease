namespace TherapEase.Domain.Compartido;

// El mensaje es apto para mostrarse a la usuaria: nunca debe incluir datos de pacientes (Q04).
public class ReglaDeNegocioException(string mensaje, Exception? interna = null) : Exception(mensaje, interna)
{
}
