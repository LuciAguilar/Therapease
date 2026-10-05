namespace TherapEase.Domain.Auditoria.Entidades.Enums;

// Los nombres se guardan en una columna de 20 caracteres.
public enum AccionAuditoria
{
    Alta,
    Actualizacion,
    Baja,
    Recuperacion,
    Cancelacion,
    Desactivacion,
    CambioContrasena,
    ReinicioContrasena
}
