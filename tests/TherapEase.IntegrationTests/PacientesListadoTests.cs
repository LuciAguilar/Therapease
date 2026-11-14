using TherapEase.Application.Coordinadores.Interfaces.Servicios;
using TherapEase.Application.Identidad.Interfaces.Servicios;
using TherapEase.Application.Pacientes.Constantes;
using TherapEase.Application.Pacientes.Interfaces.Servicios;
using TherapEase.Domain.Pacientes.Entidades;
using TherapEase.Domain.Pacientes.Entidades.Enums;
using TherapEase.Domain.Pacientes.Reglas;
using static TherapEase.IntegrationTests.PacientesRevision;

namespace TherapEase.IntegrationTests;

/// <summary>
/// Pruebas del listado paginado de pacientes con el servidor real y una base de datos desechable:
/// páginas de seis en orden estable, totales, filtro, fichas dadas de baja, página inexistente,
/// que la base entregue solo las filas de la página y que sin acceso vigente no se vea nada.
/// Todos los datos son inventados. Van en su propia clase para que el contenedor de PostgreSQL no se quede sin conexiones.
/// </summary>
public class PacientesListadoTests(PostgreSqlRevision postgres) : IClassFixture<PostgreSqlRevision>
{


    // ──── PREPARACIÓN ──────────────────────────────────────────────────────────────────────


    /// Guarda fichas inventadas numeradas directo en la base; el número va en el nombre para comprobar el orden.
    private static async Task GuardarNumeradasAsync(PacientesRevision e, int cantidad, string prefijo = "Persona Listada")
    {
        await using var contexto = e.Escenario.Contexto();
        for (var i = 1; i <= cantidad; i++)
        {
            contexto.Pacientes.Add(ReglasDePaciente.Registrar(
                Guid.NewGuid(), new DatosDePaciente($"{prefijo} {i:00}", new DateOnly(1990, 5, 8)),
                [AmbitoAtencion.Escolar], e.Actor, e.Reloj.GetUtcNow()));
        }

        await contexto.SaveChangesAsync();
    }


    // ──── PÁGINAS Y TOTALES ────────────────────────────────────────────────────────────────


    /// Catorce fichas se muestran en tres páginas de seis, seis y dos, siempre en el mismo orden,
    /// con el total de páginas, el total de pacientes y la posición de cada página.
    [Fact]
    public async Task CatorceFichasSeMuestranEnTresPaginasDeSeisEnOrdenEstable()
    {
        // Arrange
        using var e = await PacientesRevision.CrearAsync(postgres);
        await GuardarNumeradasAsync(e, 14);
        var nombres = Enumerable.Range(1, 14).Select(i => $"Persona Listada {i:00}").ToList();

        // Act
        var primera = (await e.ServicioAsync(s => s.ListarAsync(null, null, 1))).Datos!;
        var segunda = (await e.ServicioAsync(s => s.ListarAsync(null, null, 2))).Datos!;
        var tercera = (await e.ServicioAsync(s => s.ListarAsync(null, null, 3))).Datos!;
        var repetida = (await e.ServicioAsync(s => s.ListarAsync(null, null, 2))).Datos!;

        // Assert
        Assert.Equal(nombres.Take(6), primera.Pacientes.Select(p => p.Nombre));
        Assert.Equal(nombres.Skip(6).Take(6), segunda.Pacientes.Select(p => p.Nombre));
        Assert.Equal(nombres.Skip(12), tercera.Pacientes.Select(p => p.Nombre));
        Assert.Equal(segunda.Pacientes.Select(p => p.IdPaciente), repetida.Pacientes.Select(p => p.IdPaciente));
        Assert.All(new[] { primera, segunda, tercera }, p =>
        {
            Assert.Equal(14, p.TotalDePacientes);
            Assert.Equal(3, p.TotalDePaginas);
        });
        Assert.Equal((7, 12), (segunda.Desde, segunda.Hasta));
        Assert.Equal((13, 14), (tercera.Desde, tercera.Hasta));
    }

    /// Para mostrar una página la base recibe un conteo aparte y una consulta con LIMIT y OFFSET:
    /// nunca se leen todas las fichas ni se escribe nada.
    [Fact]
    public async Task LaBaseEntregaSoloLasFilasDeLaPaginaYUnConteoAparte()
    {
        // Arrange
        using var e = await PacientesRevision.CrearAsync(postgres);
        await GuardarNumeradasAsync(e, 14);
        e.Comandos.Limpiar();

        // Act
        var pagina = (await e.ServicioAsync(s => s.ListarAsync(null, null, 2))).Datos!;
        var consultasAPaciente = e.Comandos.Textos.Where(t => t.Contains("FROM \"Paciente\"", StringComparison.Ordinal)).ToList();

        // Assert
        Assert.Equal(6, pagina.Pacientes.Count);
        Assert.Equal(2, consultasAPaciente.Count);
        Assert.Single(consultasAPaciente, t => t.Contains("count(*)", StringComparison.OrdinalIgnoreCase));
        var filas = Assert.Single(consultasAPaciente, t => t.Contains("LIMIT", StringComparison.Ordinal));
        Assert.Contains("OFFSET", filas);
        Assert.DoesNotContain(e.Comandos.Textos, t => t.TrimStart().StartsWith("INSERT", StringComparison.OrdinalIgnoreCase));
    }

    /// Una ficha dada de baja no aparece ni cuenta en el total; pedir una página que no existe devuelve la última, aun con un número enorme.
    [Fact]
    public async Task LasBajasNoAparecenNiCuentanYUnaPaginaInexistenteDevuelveLaUltima()
    {
        // Arrange
        using var e = await PacientesRevision.CrearAsync(postgres);
        await GuardarNumeradasAsync(e, 7);
        var baja = await e.RegistrarAsync(Adulto("Persona Listada 00"));
        var dadaDeBaja = await e.EnSolicitudAsync(s => s.Obtener<ICoordinadorDeBajaDePaciente>().DarDeBajaAsync(baja, 1));
        Assert.True(dadaDeBaja.Exito, dadaDeBaja.Mensaje);

        // Act
        var primera = (await e.ServicioAsync(s => s.ListarAsync(null, null, 1))).Datos!;
        var lejana = (await e.ServicioAsync(s => s.ListarAsync(null, null, int.MaxValue))).Datos!;

        // Assert
        Assert.Equal(7, primera.TotalDePacientes);
        Assert.Equal(2, primera.TotalDePaginas);
        Assert.DoesNotContain(primera.Pacientes, p => p.IdPaciente == baja);
        Assert.Equal(2, lejana.PaginaActual);
        Assert.Equal("Persona Listada 07", Assert.Single(lejana.Pacientes).Nombre);
    }

    /// Un filtro por nombre, sin distinguir mayúsculas, cuenta y pagina solo las coincidencias.
    [Fact]
    public async Task ElFiltroPorNombreCuentaYPaginaSoloLasCoincidencias()
    {
        // Arrange
        using var e = await PacientesRevision.CrearAsync(postgres);
        await GuardarNumeradasAsync(e, 8, "Ana Listada");
        await GuardarNumeradasAsync(e, 3, "Luis Listado");

        // Act
        var primera = (await e.ServicioAsync(s => s.ListarAsync("ANA listada", null, 1))).Datos!;
        var segunda = (await e.ServicioAsync(s => s.ListarAsync("ana", null, 2))).Datos!;
        var sinResultados = (await e.ServicioAsync(s => s.ListarAsync("inexistente", null, 1))).Datos!;

        // Assert
        Assert.Equal((8, 2, 6), (primera.TotalDePacientes, primera.TotalDePaginas, primera.Pacientes.Count));
        Assert.All(primera.Pacientes, p => Assert.StartsWith("Ana Listada", p.Nombre));
        Assert.Equal(["Ana Listada 07", "Ana Listada 08"], segunda.Pacientes.Select(p => p.Nombre));
        Assert.Empty(sinResultados.Pacientes);
        Assert.Equal(0, sinResultados.TotalDePacientes);
    }


    // ──── PERMISOS Y SESIÓN ────────────────────────────────────────────────────────────────


    /// Usuaria y Superusuario ven el listado con los permisos reales.
    [Theory]
    [InlineData("Usuaria")]
    [InlineData("Superusuario")]
    public async Task LosDosRolesVenElListadoConLosPermisosReales(string rol)
    {
        using var e = await PacientesRevision.CrearAsync(postgres, rol);
        await GuardarNumeradasAsync(e, 7);

        var respuesta = await e.ServicioAsync(s => s.ListarAsync(null));

        Assert.True(respuesta.Exito, respuesta.Mensaje);
        Assert.Equal(7, respuesta.Datos!.TotalDePacientes);
        Assert.Equal(6, respuesta.Datos.Pacientes.Count);
    }

    /// Sin sesión, con la sesión vencida, cerrada, con el usuario desactivado, con contraseña temporal o sin rol,
    /// el listado se rechaza antes de leer fichas y no entrega ni totales.
    [Theory]
    [InlineData("ausente")]
    [InlineData("vencida")]
    [InlineData("revocada")]
    [InlineData("inactiva")]
    [InlineData("temporal")]
    [InlineData("sin-rol")]
    public async Task SinAccesoVigenteElListadoSeDeniegaAntesDeLeerPacientes(string estado)
    {
        // Arrange
        using var e = await PacientesRevision.CrearAsync(postgres);
        await GuardarNumeradasAsync(e, 7);
        var cookie = e.Cookie;
        switch (estado)
        {
            case "ausente": cookie = string.Empty; break;
            case "vencida": e.Reloj.Avanzar(TimeSpan.FromMinutes(31)); break;
            case "revocada":
                await e.EnSolicitudAsync(async s => { await s.Obtener<IServicioAcceso>().CerrarSesionAsync(); return true; });
                break;
            case "inactiva":
                await e.SqlAsync("UPDATE \"AspNetUsers\" SET \"Activo\"=false WHERE \"Id\"=@id", ("id", e.Actor));
                break;
            case "temporal":
                await e.SqlAsync("UPDATE \"AspNetUsers\" SET \"DebeCambiarContrasena\"=true WHERE \"Id\"=@id", ("id", e.Actor));
                break;
            case "sin-rol":
                await e.SqlAsync("DELETE FROM \"AspNetUserRoles\" WHERE \"UserId\"=@id", ("id", e.Actor));
                break;
            default: throw new ArgumentOutOfRangeException(nameof(estado));
        }

        e.Comandos.Limpiar();

        // Act
        var respuesta = await e.EnSolicitudAsync(
            s => s.Obtener<IServicioPacientes>().ListarAsync(null, null, 1), cookie, exigirSesion: false);

        // Assert
        Assert.False(respuesta.Exito);
        Assert.Null(respuesta.Datos);
        Assert.Equal(MensajesDePacientes.SinPermiso, respuesta.Mensaje);
        Assert.DoesNotContain(e.Comandos.Textos, t => t.Contains("FROM \"Paciente\"", StringComparison.Ordinal));
    }
}
