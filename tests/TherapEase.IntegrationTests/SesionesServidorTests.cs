using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using static TherapEase.IntegrationTests.ServidorIdentidadRevision;
using TherapEase.Application.Identidad.Interfaces.Servicios;

namespace TherapEase.IntegrationTests;

/// <summary>
/// Comprueba el vencimiento y la separación de sesiones en una base de datos de prueba.
/// Usa un reloj de prueba para revisar minutos de inactividad sin esperar en tiempo real.
/// </summary>
public class SesionesServidorTests(PostgreSqlRevision postgres) : IClassFixture<PostgreSqlRevision>
{


    // ──── VENCIMIENTO ──────────────────────────────────────────────────────────────────────


    /// Sin recibir una cookie renovada, la sesión vale antes de treinta minutos y vence después.
    [Theory]
    [InlineData(1799, true)]
    [InlineData(1801, false)]
    public async Task Inactividad_Respeta_El_Limite_De_Treinta_Minutos(int segundos, bool valida)
    {
        // Arrange
        var escenario = await postgres.Crear();
        var reloj = new RelojRevision();
        using var proveedor = escenario.Servicios(reloj.Configurar);
        await CrearAsync(proveedor, "sesion-ficticia");
        var cookie = await EntrarAsync(proveedor, "sesion-ficticia");

        // Act
        reloj.Avanzar(TimeSpan.FromSeconds(segundos));
        using var solicitud = new SolicitudIdentidad(proveedor, cookie);
        var autenticada = await solicitud.AutenticarAsync();

        // Assert
        Assert.Equal(valida, autenticada);
        Assert.Equal(valida, solicitud.Contexto.User.Identity?.IsAuthenticated == true);
    }


    // ──── SEPARACIÓN ENTRE USUARIOS Y APLICACIONES ─────────────────────────────────────────


    /// Salir quita acceso a las dos sesiones propias, pero conserva la sesión de otra persona.
    [Fact]
    public async Task Salida_Revoca_Sesiones_Propias_Y_Conserva_La_De_Otro_Usuario()
    {
        // Arrange
        var escenario = await postgres.Crear();
        using var proveedor = escenario.Servicios();
        await CrearAsync(proveedor, "primera-ficticia");
        await CrearAsync(proveedor, "segunda-ficticia");
        var primera = await EntrarAsync(proveedor, "primera-ficticia");
        var otraPropia = await EntrarAsync(proveedor, "primera-ficticia");
        var ajena = await EntrarAsync(proveedor, "segunda-ficticia");

        // Act + Assert: la salida parte de una sesión válida.
        using (var salida = new SolicitudIdentidad(proveedor, primera))
        {
            Assert.True(await salida.AutenticarAsync());
            await salida.Obtener<IServicioAcceso>().CerrarSesionAsync();
        }

        // Assert
        Assert.False(await CookieSigueSirviendoAsync(proveedor, primera));
        Assert.False(await CookieSigueSirviendoAsync(proveedor, otraPropia));
        Assert.True(await CookieSigueSirviendoAsync(proveedor, ajena));
    }

    /// Otra aplicación no puede aceptar la cookie, aunque tenga acceso a la misma base de prueba.
    /// Otra copia de la misma aplicación sí la acepta.
    [Fact]
    public async Task Cookie_Solo_Se_Acepta_En_La_Aplicacion_Que_La_Protegio()
    {
        // Arrange
        var escenario = await postgres.Crear();
        using var original = escenario.Servicios();
        await CrearAsync(original, "sesion-ficticia");
        var cookie = await EntrarAsync(original, "sesion-ficticia");
        using var otraAplicacion = escenario.Servicios(servicios =>
            servicios.AddDataProtection().SetApplicationName("AplicacionFicticiaAjena"));
        using var mismaAplicacion = escenario.Servicios();

        // Act
        var ajena = await CookieSigueSirviendoAsync(otraAplicacion, cookie);
        var misma = await CookieSigueSirviendoAsync(mismaAplicacion, cookie);

        // Assert
        Assert.False(ajena);
        Assert.True(misma);
    }
}
