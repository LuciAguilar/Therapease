using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TherapEase.Application.Auditoria.Interfaces.Repositorios;
using TherapEase.Application.Citas.Interfaces.Repositorios;
using TherapEase.Application.Compartido.Interfaces.Repositorios;
using TherapEase.Application.Pacientes.Interfaces.Repositorios;
using TherapEase.Infrastructure.Configuracion;
using TherapEase.Infrastructure.Data;

namespace TherapEase.UnitTests.Infrastructure.Configuracion;

public class InyeccionDeDependenciasTests
{
    // Valores ficticios: ninguna prueba abre una conexión; solo se comprueba qué se registra y qué se configura.
    private const string ConexionFicticia = "Host=localhost;Port=1;Database=ficticia;Username=ficticio;Password=ClaveFicticiaQueNoDebeVerse";

    public static TheoryData<string?> CadenasQueFaltan() => new() { null, "", " ", "   ", "\t", "\r\n" };

    // Falta la conexión: la aplicación y el comando del operador se detienen con un mensaje fijo, y eso se decide solo aquí.
    [Theory]
    [MemberData(nameof(CadenasQueFaltan))]
    public void AgregarPersistencia_SinCadenaDeConexion_SeDetieneConElMensajeFijo(string? cadena)
    {
        var servicios = new ServiceCollection();

        var excepcion = Assert.Throws<InvalidOperationException>(() => servicios.AgregarPersistencia(cadena));

        Assert.Equal(InyeccionDeDependencias.MensajeSinCadenaDeConexion, excepcion.Message);
    }

    [Fact]
    public void AgregarPersistencia_SinCadenaDeConexion_NoRegistraNada()
    {
        var servicios = new ServiceCollection();

        Assert.Throws<InvalidOperationException>(() => servicios.AgregarPersistencia(null));

        Assert.Empty(servicios);
    }

    // El mensaje nombra la clave de configuración y la variable de entorno, pero nunca una cadena de conexión ni un valor.
    [Fact]
    public void ElMensajeFijo_NombraLaClaveYLaVariable_YNoLlevaNingunaCadenaNiSecreto()
    {
        var mensaje = InyeccionDeDependencias.MensajeSinCadenaDeConexion;

        Assert.Contains("ConnectionStrings:TherapEase", mensaje, StringComparison.Ordinal);
        Assert.Contains("ConnectionStrings__TherapEase", mensaje, StringComparison.Ordinal);
        foreach (var fragmento in new[] { "Host=", "Username=", "Password=", "Database=", "Port=" })
        {
            Assert.DoesNotContain(fragmento, mensaje, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void AgregarPersistencia_ConCadena_RegistraLaUnidadDeTrabajoYLosRepositorios()
    {
        var servicios = new ServiceCollection();

        var devuelto = servicios.AgregarPersistencia(ConexionFicticia);

        Assert.Same(servicios, devuelto);
        using var proveedor = servicios.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using var alcance = proveedor.CreateScope();
        Assert.NotNull(alcance.ServiceProvider.GetRequiredService<IUnidadDeTrabajo>());
        Assert.NotNull(alcance.ServiceProvider.GetRequiredService<IRepositorioPacientes>());
        Assert.NotNull(alcance.ServiceProvider.GetRequiredService<IRepositorioCitas>());
        Assert.NotNull(alcance.ServiceProvider.GetRequiredService<IRepositorioAuditoria>());
    }

    [Fact]
    public void AgregarPersistencia_ConCadena_ConfiguraElContextoConEsaConexion()
    {
        var servicios = new ServiceCollection();
        servicios.AgregarPersistencia(ConexionFicticia);
        using var proveedor = servicios.BuildServiceProvider();
        using var alcance = proveedor.CreateScope();

        var contexto = alcance.ServiceProvider.GetRequiredService<ContextoDeDatos>();

        Assert.Contains("Database=ficticia", contexto.Database.GetConnectionString(), StringComparison.Ordinal);
    }

    // Las pruebas de integración sustituyen la conexión (o agregan interceptores) con otra configuración del contexto después de AgregarPersistencia:
    // la nueva validación no debe impedirlo ni cambiar cuál conexión gana.
    [Fact]
    public void UnaConfiguracionPosteriorDelContexto_SustituyeLaConexion()
    {
        var servicios = new ServiceCollection();
        servicios.AgregarPersistencia(ConexionFicticia);
        servicios.AddDbContext<ContextoDeDatos>(opciones =>
            opciones.UseNpgsql("Host=localhost;Port=2;Database=sustituta;Username=ficticio;Password=ClaveFicticia"));
        using var proveedor = servicios.BuildServiceProvider();
        using var alcance = proveedor.CreateScope();

        var contexto = alcance.ServiceProvider.GetRequiredService<ContextoDeDatos>();

        Assert.Contains("Database=sustituta", contexto.Database.GetConnectionString(), StringComparison.Ordinal);
    }
}
