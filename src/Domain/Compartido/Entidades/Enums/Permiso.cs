namespace TherapEase.Domain.Compartido.Entidades.Enums;

// Solo las operaciones que existen hoy; las de pacientes, citas y pago se agregan con sus incrementos.
public enum Permiso
{
    AdministrarUsuarios,
    ConsultarAuditoria,

    // Solo superusuario por ahora: concederla a la usuaria (08B §1.2) queda pendiente de validar con Dulce.
    DarDeBajaPacientes
}
