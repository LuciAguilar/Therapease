using TherapEase.Domain.Identidad.Constantes;

namespace TherapEase.Domain.Identidad.Reglas;

public static class ReglasDeRol
{
    public static bool EsRolConocido(string? rol) => rol is NombresDeRol.Usuaria or NombresDeRol.Superusuario;
}
