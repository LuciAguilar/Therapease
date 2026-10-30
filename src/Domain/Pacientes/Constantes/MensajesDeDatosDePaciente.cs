namespace TherapEase.Domain.Pacientes.Constantes;

// Mensajes fijos de las reglas de la ficha: nunca incluyen el valor escrito (Q04).
public static class MensajesDeDatosDePaciente
{
    public const string NombreInvalido = "El nombre es obligatorio y admite de 2 a 200 caracteres, sin caracteres de control.";
    public const string NacimientoObligatorio = "La fecha de nacimiento es obligatoria.";
    public const string NacimientoInvalido = "La fecha de nacimiento no es válida.";
    public const string TelefonoInvalido = "El teléfono no es válido: use de 7 a 15 dígitos.";
    public const string CorreoInvalido = "El correo no es válido.";
    public const string EscuelaInvalida = "La escuela admite hasta 150 caracteres, sin caracteres de control.";
    public const string GrupoInvalido = "El grupo admite hasta 30 caracteres, sin caracteres de control.";
    public const string TutorObligatorioParaMenores = "Para una persona menor de 18 años se necesita un tutor: nombre, parentesco y teléfono o correo.";
    public const string TutorIncompleto = "Los datos del tutor están incompletos: se necesita nombre, parentesco y teléfono o correo.";
    public const string TutorInvalido = "Los datos del tutor no son válidos.";
    public const string AmbitosInvalidos = "El paciente debe tener uno o dos ámbitos distintos.";
    public const string FichaDeBajaNoSeActualiza = "La ficha está dada de baja y no puede actualizarse.";
    public const string IdentificadorNecesario = "El paciente necesita un identificador.";
}
