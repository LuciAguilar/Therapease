using TherapEase.Domain.Compartido.Reglas;
using TherapEase.Domain.Identidad.Constantes;

namespace TherapEase.Domain.Identidad.Reglas;

// Nombre completo y correo de una cuenta. El correo es un dato de contacto interno: no sirve para entrar ni se exige que sea único.
// La validación en sí es la común con pacientes; aquí solo se fijan los límites propios de las cuentas.
public static class ReglasDeDatosDeCuenta
{
    public static bool EsNombreCompletoValido(string? entrada, out string nombre) =>
        ReglasDeNombreYCorreo.EsNombreValido(
            entrada, LimitesDeCuenta.LongitudMinimaNombreCompleto, LimitesDeCuenta.LongitudMaximaNombreCompleto, out nombre);

    public static bool EsCorreoValido(string? entrada, out string? correo) =>
        ReglasDeNombreYCorreo.EsCorreoValido(entrada, LimitesDeCuenta.LongitudMaximaCorreo, out correo);
}
