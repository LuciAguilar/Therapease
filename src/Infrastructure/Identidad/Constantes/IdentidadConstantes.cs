namespace TherapEase.Infrastructure.Identidad.Constantes;

internal static class IdentidadConstantes
{
    // Identificadores fijos de los roles sembrados por la migración; los usan también los disparadores de la base.
    public static readonly Guid IdRolUsuaria = Guid.Parse("a1b2c3d4-0001-4000-8000-000000000001");
    public static readonly Guid IdRolSuperusuario = Guid.Parse("a1b2c3d4-0001-4000-8000-000000000002");

    public const string CodigoUltimoSuperusuario = "TE002";
}
