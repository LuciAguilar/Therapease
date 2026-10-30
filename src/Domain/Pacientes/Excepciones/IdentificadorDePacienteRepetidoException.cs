using TherapEase.Domain.Compartido.Excepciones;

namespace TherapEase.Domain.Pacientes.Excepciones;

// La base rechazó un alta porque ya existe una ficha con ese identificador (la clave primaria de Paciente): puede ser el reenvío de una solicitud ya guardada.
public class IdentificadorDePacienteRepetidoException(Exception? interna = null)
    : ReglaDeNegocioException("Ya existe una ficha con ese identificador.", interna)
{
}
