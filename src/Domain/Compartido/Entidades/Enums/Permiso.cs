namespace TherapEase.Domain.Compartido.Entidades.Enums;

// Solo las operaciones que existen hoy; las de pacientes, citas y pago se agregan con sus incrementos.
public enum Permiso
{
    AdministrarUsuarios,
    ConsultarAuditoria,

    // Usuaria y superusuario, provisionalmente (decisión de Lucía, 11-oct-2026); Dulce valida la política operativa con la usuaria.
    DarDeBajaPacientes,

    // Decisión de Lucía, 26-oct-2026: usuaria y superusuario; los del superusuario valen solo con datos de prueba, el acceso real se decide con R04.
    // ConsultarPacientes cubre la búsqueda, la ficha y la lista de parecidos al registrar.
    RegistrarPacientes,
    ConsultarPacientes,
    ActualizarPacientes
}
