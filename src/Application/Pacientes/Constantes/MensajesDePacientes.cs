namespace TherapEase.Application.Pacientes.Constantes;

// Mensajes fijos de los servicios de pacientes: sin nombres, fechas ni contactos. Las páginas los comparan con estas constantes, no con el texto.
public static class MensajesDePacientes
{
    public const string SinPermiso = "No tienes permiso para realizar esta operación.";
    public const string PacienteNoEncontrado = "No se encontró el paciente.";
    public const string FichaDeBaja = "Está dada de baja.";
    public const string FichaDeBajaNoSeActualiza = "La ficha está dada de baja y no puede actualizarse.";
    public const string ConflictoDeVersion = "El registro cambió mientras se editaba. Recarga la información antes de decidir.";
    public const string SolicitudInvalida = "La solicitud no es válida.";
    public const string SolicitudYaUsada = "No fue posible registrar la ficha con esa solicitud. Abre de nuevo el formulario.";
    public const string BusquedaInvalida = "Escribe de 2 a 100 caracteres, con hasta 6 palabras.";
    public const string CoincidenciasPorConfirmar = "Hay fichas con el mismo nombre. Abre una existente o confirma que es otra persona.";

    public const string PacienteRegistrado = "Paciente registrado.";
    public const string PacienteYaRegistrado = "Esa ficha ya estaba registrada.";
    public const string DatosActualizados = "Datos actualizados.";
    public const string SinCambios = "No hubo cambios.";
}
