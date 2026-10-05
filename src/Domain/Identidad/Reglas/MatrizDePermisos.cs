using TherapEase.Domain.Compartido.Entidades.Enums;
using TherapEase.Domain.Identidad.Constantes;

namespace TherapEase.Domain.Identidad.Reglas;

public static class MatrizDePermisos
{
    // 08B §1.2: administrar usuarios y la consulta técnica de auditoría son solo del superusuario.
    public static bool RolTienePermiso(string? rol, Permiso permiso) => rol == NombresDeRol.Superusuario && Enum.IsDefined(permiso);

    public static IReadOnlyList<Permiso> PermisosDelRol(string? rol) =>
        Enum.GetValues<Permiso>().Where(p => RolTienePermiso(rol, p)).ToList();
}
