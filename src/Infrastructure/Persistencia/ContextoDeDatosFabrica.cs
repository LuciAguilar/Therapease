using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TherapEase.Infrastructure.Persistencia;

// Usado solo por `dotnet ef`: las migraciones corren con el usuario migrador, nunca con el de la aplicación.
public class ContextoDeDatosFabrica : IDesignTimeDbContextFactory<ContextoDeDatos>
{
    public const string VariableConexionMigraciones = "ConnectionStrings__Migraciones";

    public ContextoDeDatos CreateDbContext(string[] args)
    {
        var cadenaConexion = Environment.GetEnvironmentVariable(VariableConexionMigraciones);
        if (string.IsNullOrWhiteSpace(cadenaConexion))
        {
            throw new InvalidOperationException(
                $"Define la variable de entorno {VariableConexionMigraciones} con la conexión del usuario migrador.");
        }

        var opciones = new DbContextOptionsBuilder<ContextoDeDatos>()
            .UseNpgsql(cadenaConexion)
            .Options;
        return new ContextoDeDatos(opciones);
    }
}
