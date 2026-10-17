namespace TherapEase.Application.Identidad.Modelos;

// Datos de una cuenta para el superusuario. Nunca incluye hash, sello de seguridad ni contadores de fallos.
public sealed record FichaDeUsuario(
    DatosUsuario Datos,
    string? Correo
    );
