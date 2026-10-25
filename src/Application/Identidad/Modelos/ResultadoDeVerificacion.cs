namespace TherapEase.Application.Identidad.Modelos;

// Usuario es nulo ante cualquier fallo, sin distinguir la causa (S-02). IdUsuarioBloqueado solo lo conoce el servicio para auditar: nunca llega a la persona.
// Se informa únicamente cuando este intento fue el que inició el bloqueo, no cuando la cuenta ya estaba bloqueada.
public sealed record ResultadoDeVerificacion(DatosUsuario? Usuario, Guid? IdUsuarioBloqueado = null);
