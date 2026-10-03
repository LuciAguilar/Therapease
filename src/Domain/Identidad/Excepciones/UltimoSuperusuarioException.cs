using TherapEase.Domain.Compartido.Excepciones;

namespace TherapEase.Domain.Identidad.Excepciones;

public class UltimoSuperusuarioException(Exception? interna = null)
    : ReglaDeNegocioException("Debe quedar al menos un superusuario activo.", interna)
{
}
