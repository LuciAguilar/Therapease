using System.Text;

namespace TherapEase.Domain.Pacientes.Reglas;

// Clave de búsqueda de un nombre: mismo contrato para el alta, la edición, la búsqueda y la conversión de datos anteriores en la migración.
// No depende del idioma del equipo ni del servidor. Pasos, en este orden:
//   1. descomposición canónica Unicode (forma D);
//   2. quitar las marcas combinantes U+0300 a U+036F (acentos, diéresis, la tilde de la ñ...);
//   3. pasar a minúscula con ToLowerInvariant (Ø, Ж, Σ... incluidas), ya sin las marcas: así la İ turca, que .NET no baja de mayúscula al estar sola,
//      llega como I y queda como i, igual que con la minúscula completa de ICU;
//   4. igualar la sigma final (ς, U+03C2) con la sigma (σ, U+03C3): ICU escribe una u otra según la posición y .NET siempre la segunda;
//   5. tratar espacio, tabulador, salto de línea, retorno, tabulador vertical y avance de página como un espacio; colapsar repetidos y recortar los extremos.
// La migración calcula esta misma clave con normalize(..., NFD), lower(... COLLATE "und-x-icu") y translate de PostgreSQL, y sus pruebas demuestran que coincide con esta.
public static class NormalizacionDeNombres
{
    public static string Normalizar(string? texto)
    {
        if (string.IsNullOrEmpty(texto))
        {
            return string.Empty;
        }

        var descompuesto = texto.Normalize(NormalizationForm.FormD);
        var resultado = new StringBuilder(descompuesto.Length);
        var hayEspacioPendiente = false;

        foreach (var c in descompuesto)
        {
            if (c is >= (char)0x0300 and <= (char)0x036F)
            {
                continue;
            }

            if (c is ' ' or '\t' or '\n' or '\r' or '\v' or '\f')
            {
                hayEspacioPendiente = resultado.Length > 0;
                continue;
            }

            if (hayEspacioPendiente)
            {
                resultado.Append(' ');
                hayEspacioPendiente = false;
            }

            resultado.Append(c);
        }

        return resultado.ToString().ToLowerInvariant().Replace((char)0x03C2, (char)0x03C3);
    }

    // Un texto con sustitutos Unicode mal formados no se puede descomponer: se informa en lugar de fallar.
    public static bool TryNormalizar(string? texto, out string normalizado)
    {
        try
        {
            normalizado = Normalizar(texto);
            return true;
        }
        catch (ArgumentException)
        {
            normalizado = string.Empty;
            return false;
        }
    }
}
