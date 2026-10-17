namespace TherapEase.Application.Identidad.Modelos;

// Qué datos de la cuenta cambiaron de verdad al actualizarla: solo esos entran al evento de auditoría.
public sealed record CamposActualizados(
    bool NombreCompleto,
    bool Correo
    )
{
    public bool Hubo => NombreCompleto || Correo;
}
