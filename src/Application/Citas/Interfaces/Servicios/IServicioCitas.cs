using TherapEase.Application.Citas.Modelos;
using TherapEase.Application.Compartido.Modelos;

namespace TherapEase.Application.Citas.Interfaces.Servicios;

// Consultar la agenda y agendar citas ficticias en la hora de Hermosillo. Todas las operaciones toman a la persona de la sesión (nunca de un parámetro),
// comprueban sus permisos en el servidor antes de leer o cambiar nada y devuelven mensajes fijos sin datos privados.
// Un fallo inesperado (base de datos o auditoría) no vuelve como resultado: sube como excepción y la aplicación responde su error general.
public interface IServicioCitas
{
    // Requiere ConsultarAgenda y también ConsultarPacientes, porque la agenda muestra nombres de pacientes.
    // Periodo de 1 a 35 días con ambas fechas incluidas; sin fechas, hoy y los seis días siguientes. Una sola fecha, un orden invertido, un periodo mayor o una
    // vista desconocida dan Exito falso sin consultar datos. Hasta 500 filas; con más, HayMas. Sin citas es un resultado correcto con la lista vacía.
    Task<RespuestaServicio<ResultadoDeAgenda>> ConsultarAgendaAsync(
        DateOnly? desde, DateOnly? hasta, VistaDeAgenda vista = VistaDeAgenda.Activas, CancellationToken cancelacion = default);

    // Requiere ConsultarAgenda y ConsultarPacientes. Una cita de baja o inexistente responde lo mismo: no se encontró.
    Task<RespuestaServicio<DetalleDeCita>> ObtenerCitaAsync(Guid idCita, CancellationToken cancelacion = default);

    // Requiere AgendarCitas y también ConsultarPacientes, porque lee la condición y los ámbitos del paciente.
    // idSolicitud lo crea la página al abrir el formulario y lo conserva al reenviar: es el identificador de la cita. No concede acceso ni se trata como secreto.
    // Un reenvío de la misma reserva (mismo actor y mismos datos) ya confirmada devuelve YaAgendada sin segunda cita ni evento.
    // La misma solicitud con otros datos, otro actor o una cita cancelada o de baja se rechaza sin revelar la otra cita.
    Task<RespuestaServicio<ResultadoDeAgendar>> AgendarAsync(
        Guid idSolicitud, DatosDeCitaEntrada datos, CancellationToken cancelacion = default);
}
