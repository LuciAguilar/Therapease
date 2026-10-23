namespace TherapEase.Application.Identidad.Modelos;

// BloqueoNuevo es verdadero solo si este intento fallido fue el que inició el bloqueo de la cuenta.
public sealed record ResultadoDeContrasenaActual(bool Correcta, bool BloqueoNuevo = false);
