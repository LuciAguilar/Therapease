namespace TherapEase.Domain.Citas.Constantes;

// Mensajes fijos de las reglas del estado de pago: sin fechas, nombres ni datos de la cita. Las páginas los comparan con las constantes de Application, no con el texto.
public static class MensajesDePagoDeCita
{
    public const string PagoNoValido = "El estado de pago no es válido.";
    public const string CitaDeBajaNoSeActualiza = "La cita está dada de baja y su pago no puede cambiarse.";
}
