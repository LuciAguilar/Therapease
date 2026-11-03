using System.Text;
using TherapEase.Domain.Pacientes.Constantes;

namespace TherapEase.Domain.Pacientes.Reglas;

public static class ReglasDeTelefono
{
    // Opcional: sin texto es válido y no se guarda ninguno. Al capturar se admiten espacios, guiones, puntos, paréntesis y un + inicial; de 7 a 15 dígitos.
    // Se guardan solo los dígitos y, si existía, ese signo inicial. Un teléfono nunca lleva letras.
    public static bool EsTelefonoValido(string? entrada, out string? telefono)
    {
        telefono = null;
        var texto = entrada?.Trim();
        if (string.IsNullOrEmpty(texto))
        {
            return true;
        }

        var empiezaConMas = texto[0] == '+';
        var digitos = new StringBuilder();
        foreach (var c in texto.AsSpan(empiezaConMas ? 1 : 0))
        {
            if (c is >= '0' and <= '9')
            {
                digitos.Append(c);
            }
            else if (c is not (' ' or '-' or '.' or '(' or ')'))
            {
                return false;
            }
        }

        if (digitos.Length < LimitesDePaciente.MinimoDeDigitosDeTelefono || digitos.Length > LimitesDePaciente.MaximoDeDigitosDeTelefono)
        {
            return false;
        }

        telefono = (empiezaConMas ? "+" : string.Empty) + digitos;
        return true;
    }
}
