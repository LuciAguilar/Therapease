namespace TherapEase.Web.Seguridad;

public static class PoliticasDeAutorizacion
{
    // Sesión válida y sin contraseña temporal pendiente: es la política por defecto de todo lo que no declare otra.
    public const string AccesoOperativo = nameof(AccesoOperativo);

    // Sesión válida, incluso con contraseña temporal: solo para CU11.
    public const string CambiarContrasenaPropia = nameof(CambiarContrasenaPropia);

    public const string AdministrarUsuarios = nameof(AdministrarUsuarios);
    public const string ConsultarAuditoria = nameof(ConsultarAuditoria);
}
