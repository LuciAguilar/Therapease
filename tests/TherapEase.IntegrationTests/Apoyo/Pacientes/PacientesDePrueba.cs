using TherapEase.Domain.Pacientes.Entidades;

namespace TherapEase.IntegrationTests;

// Datos ficticios válidos para registrar fichas en pruebas que no tratan sobre los datos del paciente: una persona adulta, con nacimiento fijo
// para que no cambie de edad con el reloj de la prueba, y sin tutor.
internal static class PacientesDePrueba
{
    public static readonly DateOnly NacimientoDeAdulto = new(1990, 1, 15);

    public static DatosDePaciente Adulto(string nombre, string? correo = null) =>
        new(nombre, NacimientoDeAdulto, Correo: correo);
}
