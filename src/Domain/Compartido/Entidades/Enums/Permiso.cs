namespace TherapEase.Domain.Compartido.Entidades.Enums;

// Solo las operaciones que existen hoy; las de pacientes, citas y pago se agregan con sus incrementos.
public enum Permiso
{
    AdministrarUsuarios,
    ConsultarAuditoria,

    // Usuaria y superusuario, provisionalmente (decisión de Lucía, 11-oct-2026); Dulce valida la política operativa con la usuaria.
    DarDeBajaPacientes
}
