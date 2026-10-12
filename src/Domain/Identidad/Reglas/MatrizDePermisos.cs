using TherapEase.Domain.Compartido.Entidades.Enums;
using TherapEase.Domain.Identidad.Constantes;

namespace TherapEase.Domain.Identidad.Reglas;

public static class MatrizDePermisos
{
    // 08B §1.2: administrar usuarios y la consulta técnica de auditoría son solo del superusuario; la baja de pacientes también es de la usuaria (decisión provisional de Lucía, 11-oct-2026).
    public static bool RolTienePermiso(string? rol, Permiso permiso)
    {
        if (!Enum.IsDefined(permiso))
        {
            return false;
        }

        return rol switch
        {
            NombresDeRol.Superusuario => true,
            NombresDeRol.Usuaria => permiso == Permiso.DarDeBajaPacientes,
            _ => false
        };
    }

    public static IReadOnlyList<Permiso> PermisosDelRol(string? rol) =>
        Enum.GetValues<Permiso>().Where(p => RolTienePermiso(rol, p)).ToList();
}
