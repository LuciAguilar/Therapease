using TherapEase.Domain.Compartido.Excepciones;

namespace TherapEase.Domain.Citas.Excepciones;

// La base rechazó un alta porque ya existe una cita con ese identificador (la clave primaria de Cita): puede ser el reenvío de una solicitud ya guardada.
public class IdentificadorDeCitaRepetidoException(Exception? interna = null)
    : ReglaDeNegocioException("Ya existe una cita con ese identificador.", interna)
{
}
