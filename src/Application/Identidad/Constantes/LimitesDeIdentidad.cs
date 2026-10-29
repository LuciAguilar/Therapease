namespace TherapEase.Application.Identidad.Constantes;

public static class LimitesDeIdentidad
{
    public const int LongitudMaximaNombreUsuario = 50;

    // Tope de la lista de usuarios (sin paginación: hay muy pocas cuentas) y del texto de búsqueda, que no tiene por qué ser más largo que un nombre.
    public const int MaximoDeUsuariosListados = 100;

    public const int LongitudMaximaBusquedaDeUsuarios = 150;
}
