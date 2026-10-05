using System.Security.Cryptography;

namespace TherapEase.Application.Identidad.Servicios;

public static class GeneradorDeContrasenaTemporal
{
    public const int Longitud = 16;

    // Sin caracteres ambiguos (0/O, 1/l/I) para poder dictarla o copiarla sin errores.
    public const string Alfabeto = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789";

    public static string Generar()
    {
        return string.Create(Longitud, 0, static (destino, _) =>
        {
            for (var i = 0; i < destino.Length; i++)
            {
                destino[i] = Alfabeto[RandomNumberGenerator.GetInt32(Alfabeto.Length)];
            }
        });
    }
}
