
using System.Globalization;
using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TherapEase.Domain.Citas.Reglas;
using TherapEase.Domain.Compartido.Reglas;
using TherapEase.Domain.Pacientes.Entidades.Enums;
using TherapEase.Domain.Pacientes.Reglas;

namespace TherapEase.IntegrationTests;

/// <summary>
/// Comprueba horas reales en Linux, PostgreSQL y la API, además del arranque sin privilegios.
/// Verifica la continuidad de claves y sesión al reiniciar; no sustituye pruebas de navegador o TLS.
/// </summary>
public sealed class HorasDockerTests(HorasDockerRevision entorno) : IClassFixture<HorasDockerRevision>
{


    // ──── HERMOSILLO DENTRO DE DOCKER ───────────────────────────────────────────────────────


    /// La imagen muestra siempre la hora de Hermosillo, aunque el contenedor use otra zona horaria.
    [Theory]
    [InlineData("UTC")]
    [InlineData("America/New_York")]
    [InlineData("Europe/Madrid")]
    public async Task Hermosillo_No_Depende_De_Zona_Del_Contenedor(string zona)
    {
        // Arrange
        var fechas = new[] { "2026-01-01T00:15:00", "2026-03-08T02:30:00", "2026-03-29T02:30:00",
            "2026-10-25T02:30:00", "2026-11-01T01:30:00", "2026-12-31T23:45:00" };

        // Act
        using var respuesta = await entorno.ConsultarHoraAsync(zona);

        // Assert
        Assert.Equal(zona, respuesta.RootElement.GetProperty("ZonaProceso").GetString());
        Assert.Equal("America/Hermosillo", respuesta.RootElement.GetProperty("ZonaHermosillo").GetString());
        var conversiones = respuesta.RootElement.GetProperty("Conversiones").EnumerateArray().ToArray();
        Assert.Equal(fechas.Length, conversiones.Length);

        for (var i = 0; i < fechas.Length; i++)
        {
            var fecha = DateTime.ParseExact(fechas[i], "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
            var esperado = new DateTimeOffset(fecha, TimeSpan.FromHours(-7)).ToUniversalTime();
            Assert.Equal(fechas[i], conversiones[i].GetProperty("Entrada").GetString());
            var instante = DateTimeOffset.Parse(conversiones[i].GetProperty("Instante").GetString()!, CultureInfo.InvariantCulture);
            Assert.Equal(esperado, instante);
            Assert.Equal(TimeSpan.Zero, instante.Offset);
            Assert.Equal(fechas[i], conversiones[i].GetProperty("Regreso").GetString());
            Assert.Equal("Unspecified", conversiones[i].GetProperty("TipoFecha").GetString());
        }
    }


    // ──── API Y PERSISTENCIA DE INSTANTES ────────────────────────────────────────────────────


    /// La API rechaza una fecha sin hora, una hora sin zona y una fecha que no existe, antes de buscar eventos.
    [Theory]
    [InlineData("2026-10-25")]
    [InlineData("2026-10-25T10:00:00")]
    [InlineData("2026-02-30T10:00:00Z")]
    public async Task Api_No_Confunde_Fecha_Sin_Hora_Con_Instante(string desde)
    {
        // Arrange
        var ruta = "/api/auditoria/eventos?desde=" + Uri.EscapeDataString(desde) + "&hasta=2026-10-25T18:00:00Z";

        // Act
        var respuesta = await entorno.ConsultarAsync(ruta);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, respuesta.Estado);
        Assert.Contains("instantes con zona", respuesta.Cuerpo);
    }

    /// La misma hora escrita de tres maneras (en UTC, en Hermosillo y con otra zona) encuentra el mismo evento; incluye el inicio y deja fuera el fin.
    [Theory]
    [InlineData("2026-10-25T17:00:00Z", "2026-10-25T18:00:00Z")]
    [InlineData("2026-10-25T10:00:00-07:00", "2026-10-25T11:00:00-07:00")]
    [InlineData("2026-10-25T19:00:00+02:00", "2026-10-25T20:00:00+02:00")]
    public async Task Api_Offsets_Equivalentes_Devuelven_El_Mismo_Evento(string desde, string hasta)
    {
        // Arrange
        var ruta = $"/api/auditoria/eventos?desde={Uri.EscapeDataString(desde)}&hasta={Uri.EscapeDataString(hasta)}&idRegistro={entorno.Registro}";

        // Act
        var respuesta = await entorno.ConsultarAsync(ruta);

        // Assert
        Assert.Equal(HttpStatusCode.OK, respuesta.Estado);
        using var documento = JsonDocument.Parse(respuesta.Cuerpo);
        var evento = Assert.Single(documento.RootElement.EnumerateArray());
        Assert.Equal(entorno.EventoIncluido, evento.GetProperty("idEvento").GetGuid());
        Assert.Equal(DateTimeOffset.Parse("2026-10-25T17:00:00Z"), evento.GetProperty("fechaEvento").GetDateTimeOffset());
    }

    /// La cita guardada conserva su momento exacto en la base de datos, aunque PostgreSQL cambie de zona horaria.
    [Fact]
    public async Task Cita_Conserva_Instante_En_PostgreSql_Con_Otras_Zonas()
    {
        // Arrange
        var escenario = await entorno.CrearEscenarioAsync();
        var actor = Guid.NewGuid();
        var fecha = new DateOnly(2026, 12, 31);
        var inicio = HoraHermosillo.AInstante(fecha, new TimeOnly(23, 45));
        var paciente = ReglasDePaciente.Registrar(Guid.NewGuid(), "Paciente ficticio horas", null,
            [AmbitoAtencion.Independiente], actor, DateTimeOffset.UtcNow);
        var cita = ReglasDeCita.Agendar(Guid.NewGuid(), paciente.IdPaciente, AmbitoAtencion.Independiente,
            inicio.ToOffset(TimeSpan.FromHours(2)), inicio.AddMinutes(30), actor, DateTimeOffset.UtcNow);
        await using (var contexto = escenario.Contexto())
        {
            contexto.Pacientes.Add(paciente);
            contexto.Citas.Add(cita);
            await contexto.SaveChangesAsync();
        }

        // Act + Assert
        foreach (var zona in new[] { "UTC", "Europe/Madrid", "America/New_York" })
        {
            await using var conexion = new NpgsqlConnection(escenario.App);
            await conexion.OpenAsync();
            await using (var configurar = new NpgsqlCommand("SELECT set_config('TimeZone',@zona,false)", conexion))
            {
                configurar.Parameters.AddWithValue("zona", zona);
                await configurar.ExecuteScalarAsync();
            }
            await using var comando = new NpgsqlCommand("SELECT \"Inicio\" FROM \"Cita\" WHERE \"IdCita\"=@id", conexion);
            comando.Parameters.AddWithValue("id", cita.IdCita);
            await using var lector = await comando.ExecuteReaderAsync();
            Assert.True(await lector.ReadAsync());
            var recuperado = lector.GetFieldValue<DateTimeOffset>(0);
            Assert.Equal(new DateTimeOffset(2027, 1, 1, 6, 45, 0, TimeSpan.Zero), recuperado);
            Assert.Equal(fecha, DateOnly.FromDateTime(HoraHermosillo.ALocal(recuperado)));
        }
        await using var lectura = escenario.Contexto();
        var guardada = await lectura.Citas.AsNoTracking().SingleAsync();
        Assert.Equal(inicio, guardada.Inicio);
        Assert.Equal(inicio.AddMinutes(30), guardada.Fin);
    }


    // ──── ARRANQUE Y CONTINUIDAD ────────────────────────────────────────────────────────────


    /// La imagen corre como usuario normal (sin ser administrador) y reporta su propio estado de salud.
    [Fact]
    public async Task Docker_Usuario_Sin_Privilegios_Salud_Y_Ruta_Protegida()
    {
        // Arrange
        var usuario = await entorno.InspeccionarAsync("{{.Config.User}}");

        // Act
        var uid = await entorno.DentroAsync("id", "-u");
        var salud = await entorno.ConsultarAsync("/salud", false);
        var protegida = await entorno.ConsultarAsync("/api/auditoria/eventos", false);
        string estado = "";
        for (var i = 0; i < 180; i++)
        {
            estado = await entorno.InspeccionarAsync("{{.State.Health.Status}}");
            if (estado != "starting") break;
            await Task.Delay(250);
        }

        // Assert
        Assert.NotEqual("0", uid);
        Assert.NotEqual("root", usuario);
        Assert.Equal(usuario, uid);
        Assert.Equal(HttpStatusCode.OK, salud.Estado);
        Assert.Equal(HttpStatusCode.Unauthorized, protegida.Estado);
        Assert.Equal("healthy", estado);
    }

    /// Tras reiniciar el contenedor, las claves siguen en la base de datos y la sesión abierta antes sigue sirviendo.
    [Fact]
    public async Task Reinicio_Docker_Conserva_Claves_Y_Sesion_Sin_Claves_En_Disco()
    {
        // Arrange
        var ruta = $"/api/auditoria/eventos?desde=2026-10-25T17:00:00Z&hasta=2026-10-25T18:00:00Z&idRegistro={entorno.Registro}";
        Assert.Equal(HttpStatusCode.OK, (await entorno.ConsultarAsync(ruta)).Estado);
        var clavesAntes = await PostgreSqlRevision.Valor<string>(entorno.Escenario.App,
            "SELECT string_agg(\"Id\"::text || ':' || \"Xml\", '|' ORDER BY \"Id\") FROM \"DataProtectionKeys\"");
        var iniciadoAntes = await entorno.InspeccionarAsync("{{.State.StartedAt}}");

        // Act
        await entorno.ReiniciarAsync();
        var respuesta = await entorno.ConsultarAsync(ruta);
        var clavesDespues = await PostgreSqlRevision.Valor<string>(entorno.Escenario.App,
            "SELECT string_agg(\"Id\"::text || ':' || \"Xml\", '|' ORDER BY \"Id\") FROM \"DataProtectionKeys\"");
        var archivos = await entorno.DentroAsync("sh", "-c", "find /app /home/app /tmp -type f -name 'key-*.xml' -print");

        // Assert
        Assert.NotEqual(iniciadoAntes, await entorno.InspeccionarAsync("{{.State.StartedAt}}"));
        Assert.False(string.IsNullOrWhiteSpace(clavesAntes));
        Assert.Equal(clavesAntes, clavesDespues);
        Assert.Equal(HttpStatusCode.OK, respuesta.Estado);
        using var documento = JsonDocument.Parse(respuesta.Cuerpo);
        Assert.Equal(entorno.EventoIncluido, Assert.Single(documento.RootElement.EnumerateArray()).GetProperty("idEvento").GetGuid());
        Assert.Empty(archivos);
    }
}
