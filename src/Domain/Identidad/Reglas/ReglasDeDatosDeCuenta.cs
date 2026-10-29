using System.Globalization;
using TherapEase.Domain.Identidad.Constantes;

namespace TherapEase.Domain.Identidad.Reglas;

// Nombre completo y correo de una cuenta. El correo es un dato de contacto interno: no sirve para entrar ni se exige que sea único.
public static class ReglasDeDatosDeCuenta
{
    // Recorta los espacios exteriores. Se rechazan los caracteres de control y los de formato (por ejemplo, los que invierten el sentido del texto).
    public static bool EsNombreCompletoValido(string? entrada, out string nombre)
    {
        nombre = entrada?.Trim() ?? string.Empty;
        return nombre.Length is >= LimitesDeCuenta.LongitudMinimaNombreCompleto and <= LimitesDeCuenta.LongitudMaximaNombreCompleto
            && !nombre.Any(EsCaracterInvisible);
    }

    // El correo es opcional: sin texto es válido y no se guarda ninguno. Si hay texto se guarda recortado y en minúsculas.
    // Formato: una sola @, texto antes y después, al menos un punto en el dominio que no esté al inicio ni al final ni repetido, y sin espacios.
    public static bool EsCorreoValido(string? entrada, out string? correo)
    {
        var texto = entrada?.Trim();
        if (string.IsNullOrEmpty(texto))
        {
            correo = null;
            return true;
        }

        texto = texto.ToLowerInvariant();
        correo = null;

        if (texto.Length > LimitesDeCuenta.LongitudMaximaCorreo
            || texto.Any(c => char.IsWhiteSpace(c) || EsCaracterInvisible(c)))
        {
            return false;
        }

        var partes = texto.Split('@');
        if (partes.Length != 2 || partes[0].Length == 0)
        {
            return false;
        }

        var dominio = partes[1];
        if (!dominio.Contains('.') || dominio.StartsWith('.') || dominio.EndsWith('.') || dominio.Contains(".."))
        {
            return false;
        }

        correo = texto;
        return true;
    }

    private static bool EsCaracterInvisible(char c) =>
        char.IsControl(c)
        || char.GetUnicodeCategory(c) is UnicodeCategory.Format or UnicodeCategory.LineSeparator or UnicodeCategory.ParagraphSeparator;
}
