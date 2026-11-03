using TherapEase.Application.Compartido.Modelos;
using TherapEase.Application.Pacientes.Modelos;

namespace TherapEase.Application.Pacientes.Interfaces.Servicios;

// Registrar, buscar, consultar y editar fichas de pacientes ficticios. Todas las operaciones toman a la persona de la sesión (nunca de un parámetro),
// comprueban su permiso en el servidor antes de leer o cambiar nada y devuelven mensajes fijos sin datos privados.
// Un fallo inesperado (base de datos o auditoría) no vuelve como resultado: sube como excepción y la aplicación responde su error general.
public interface IServicioPacientes
{
    // Requiere RegistrarPacientes y también ConsultarPacientes, porque comprueba si ya hay fichas parecidas.
    // idSolicitud lo crea la página al abrir el formulario y lo conserva al reenviar: es el identificador de la ficha. No concede acceso ni se trata como secreto.
    // Con fichas vigentes del mismo nombre normalizado y sin confirmar, no guarda nada y devuelve CoincidenciasPorConfirmar con Exito falso.
    Task<RespuestaServicio<ResultadoDeAlta>> RegistrarAsync(
        Guid idSolicitud, DatosDePacienteEntrada datos, bool confirmaQueEsOtraPersona = false, CancellationToken cancelacion = default);

    // Requiere ConsultarPacientes. Solo fichas vigentes; texto de 2 a 100 caracteres con hasta 6 palabras. El texto buscado no se guarda ni se audita.
    Task<RespuestaServicio<ResultadoDeBusqueda>> BuscarAsync(
        string? texto, DateOnly? nacimientoExacto = null, CancellationToken cancelacion = default);

    // Requiere ConsultarPacientes. Una sola consulta: ficha vigente con contenido, o, si está de baja, solo MensajesDePacientes.FichaDeBaja.
    Task<RespuestaServicio<FichaDePaciente>> ObtenerAsync(Guid idPaciente, CancellationToken cancelacion = default);

    // Requiere ActualizarPacientes. versionEsperada es la de la ficha al abrirla: una versión anterior da siempre conflicto, aunque los datos coincidan.
    // Con la versión vigente y datos iguales a los guardados devuelve SinCambios sin escribir ni registrar evento.
    Task<RespuestaServicio<ResultadoDeEdicion>> ActualizarAsync(
        Guid idPaciente, int versionEsperada, DatosDePacienteEntrada datos, CancellationToken cancelacion = default);
}
