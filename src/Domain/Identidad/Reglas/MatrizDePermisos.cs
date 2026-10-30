using System.Collections.Frozen;
using TherapEase.Domain.Compartido.Entidades.Enums;
using TherapEase.Domain.Identidad.Constantes;

namespace TherapEase.Domain.Identidad.Reglas;

public static class MatrizDePermisos
{
    // 08B §1.2: administrar usuarios y auditoría técnica, solo superusuario; la baja de pacientes también la usuaria (provisional, Lucía 11-oct-2026).
    // Cada rol decide por escrito cada permiso. Un permiso nuevo sin decisión se niega, y una prueba falla hasta que se decida.
    private static readonly FrozenDictionary<Permiso, bool> DecisionesDelSuperusuario = new Dictionary<Permiso, bool>
    {
        [Permiso.AdministrarUsuarios] = true,
        [Permiso.ConsultarAuditoria] = true,
        [Permiso.DarDeBajaPacientes] = true,

        // Solo con datos de prueba: antes de datos reales se vuelve a decidir con R04 (el nombre del rol no da acceso clínico real).
        [Permiso.RegistrarPacientes] = true,
        [Permiso.ConsultarPacientes] = true,
        [Permiso.ActualizarPacientes] = true
    }.ToFrozenDictionary();

    private static readonly FrozenDictionary<Permiso, bool> DecisionesDeLaUsuaria = new Dictionary<Permiso, bool>
    {
        [Permiso.AdministrarUsuarios] = false,
        [Permiso.ConsultarAuditoria] = false,
        [Permiso.DarDeBajaPacientes] = true,
        [Permiso.RegistrarPacientes] = true,
        [Permiso.ConsultarPacientes] = true,
        [Permiso.ActualizarPacientes] = true
    }.ToFrozenDictionary();

    private static readonly FrozenDictionary<Permiso, bool> SinDecisiones = FrozenDictionary<Permiso, bool>.Empty;

    // Un rol desconocido o mal escrito no tiene decisiones.
    public static IReadOnlyDictionary<Permiso, bool> DecisionesDelRol(string? rol) =>
        rol switch
        {
            NombresDeRol.Superusuario => DecisionesDelSuperusuario,
            NombresDeRol.Usuaria => DecisionesDeLaUsuaria,
            _ => SinDecisiones
        };

    public static bool RolTienePermiso(string? rol, Permiso permiso) =>
        DecisionesDelRol(rol).TryGetValue(permiso, out var permitido) && permitido;

    public static IReadOnlyList<Permiso> PermisosDelRol(string? rol) =>
        Enum.GetValues<Permiso>().Where(p => RolTienePermiso(rol, p)).ToList();
}
