using System.Diagnostics;
using TherapEase.Infrastructure.Configuracion;

namespace TherapEase.IntegrationTests;

/// <summary>
/// Arranca la aplicación compilada y sus comandos de operador como procesos, sin la cadena de conexión de la aplicación: deben detenerse con el mensaje fijo
/// y el código de salida 1, sin traza, sin ninguna contraseña y sin mostrar ningún valor de configuración. No necesitan PostgreSQL.
/// </summary>
public class ArranqueSinCadenaDeConexionTests
{
    private const string SecretoFicticio = "SecretoFicticioQueNoDebeAparecer";

    // Ejecuta la aplicación compilada como lo haría el operador. «conexion» nulo quita la variable; vacío o con espacios la deja sin valor útil.
    // Se fuerza Production para que ningún archivo de desarrollo ni secreto de usuario aporte una cadena, y se deja otra configuración con un secreto
    // para comprobar que ninguna salida muestra valores de configuración.
    private static async Task<(int Codigo, string Salida, string Error)> EjecutarAsync(string? conexion, params string[] argumentos)
    {
        var configuracion = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
        var dll = Path.Combine(ServidorRevision.RaizRepositorio(), "src", "Web", "bin", configuracion, "net10.0", "TherapEase.Web.dll");
        var inicio = new ProcessStartInfo("dotnet")
        {
            UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true,
            CreateNoWindow = true, WorkingDirectory = Path.GetDirectoryName(dll)!
        };
        inicio.ArgumentList.Add(dll);
        foreach (var argumento in argumentos)
        {
            inicio.ArgumentList.Add(argumento);
        }

        inicio.Environment.Remove("ConnectionStrings__TherapEase");
        if (conexion is not null)
        {
            inicio.Environment["ConnectionStrings__TherapEase"] = conexion;
        }

        inicio.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";
        inicio.Environment["DOTNET_ENVIRONMENT"] = "Production";
        inicio.Environment["ConnectionStrings__Migraciones"] = $"Host=ficticio;Database=ficticia;Username=ficticio;Password={SecretoFicticio}";

        using var proceso = Process.Start(inicio)!;
        var salida = proceso.StandardOutput.ReadToEndAsync();
        var error = proceso.StandardError.ReadToEndAsync();
        try { await proceso.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(40)); }
        finally { if (!proceso.HasExited) proceso.Kill(entireProcessTree: true); }
        return (proceso.ExitCode, await salida, await error);
    }

    private static void ComprobarQueSoloSeMuestraElMensajeFijo((int Codigo, string Salida, string Error) resultado)
    {
        Assert.Equal(1, resultado.Codigo);
        Assert.Equal(InyeccionDeDependencias.MensajeSinCadenaDeConexion, resultado.Error.Trim());
        var todo = resultado.Salida + resultado.Error;
        Assert.DoesNotContain(SecretoFicticio, todo, StringComparison.Ordinal);
        Assert.DoesNotContain("Password", todo, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Unhandled exception", todo, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("   at ", todo, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task LaAplicacion_SinCadenaDeConexion_SeDetieneConElMensajeFijoSinEscuchar(string? conexion)
    {
        var resultado = await EjecutarAsync(conexion, "--urls", "http://127.0.0.1:0");

        ComprobarQueSoloSeMuestraElMensajeFijo(resultado);
        Assert.DoesNotContain("Now listening", resultado.Salida, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("--crear-superusuario", null)]
    [InlineData("--crear-superusuario", "")]
    [InlineData("--crear-superusuario", "   ")]
    [InlineData("--restablecer-superusuario", null)]
    [InlineData("--restablecer-superusuario", "   ")]
    public async Task ElComandoDelOperador_SinCadenaDeConexion_SeDetieneConElMensajeFijoYSinContrasenaTemporal(string comando, string? conexion)
    {
        var resultado = await EjecutarAsync(conexion, comando, "operador-ficticio");

        ComprobarQueSoloSeMuestraElMensajeFijo(resultado);
        Assert.DoesNotContain("Contraseña temporal", resultado.Salida, StringComparison.Ordinal);
        Assert.Empty(resultado.Salida.Trim());
    }

    // Las comprobaciones previas del comando no cambian: sin nombre de usuario sigue siendo un error de uso (código 2), antes de mirar la conexión.
    [Fact]
    public async Task ElComandoSinNombreDeUsuario_SigueSiendoUnErrorDeUsoConCodigoDos()
    {
        var resultado = await EjecutarAsync(null, "--crear-superusuario");

        Assert.Equal(2, resultado.Codigo);
        Assert.Contains("Uso:", resultado.Error, StringComparison.Ordinal);
        Assert.DoesNotContain(InyeccionDeDependencias.MensajeSinCadenaDeConexion, resultado.Error, StringComparison.Ordinal);
    }

    // La sonda de salud del contenedor corre antes de configurar la persistencia y no necesita la conexión.
    [Fact]
    public async Task LaSondaDeSalud_NoExigeLaCadenaDeConexion()
    {
        var resultado = await EjecutarAsync(null, "--salud", "http://127.0.0.1:1/salud");

        Assert.DoesNotContain(InyeccionDeDependencias.MensajeSinCadenaDeConexion, resultado.Salida + resultado.Error, StringComparison.Ordinal);
    }
}
