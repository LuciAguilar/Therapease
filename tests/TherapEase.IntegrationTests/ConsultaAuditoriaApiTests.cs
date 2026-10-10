using System.Net;
using System.Text.Json;

namespace TherapEase.IntegrationTests;

/// <summary>
/// Comprueba filtros, límites, permisos y contenido de la consulta técnica de auditoría.
/// Envía solicitudes HTTPS al servidor real con datos ficticios en PostgreSQL desechable.
/// </summary>
public sealed class ConsultaAuditoriaApiTests(ConsultaAuditoriaRevision entorno) : IClassFixture<ConsultaAuditoriaRevision>
{


    // ──── FILTROS Y ORDEN ──────────────────────────────────────────────────────────────────


    /// El periodo incluye su inicio, excluye su fin y entrega primero el evento más reciente.
    [Fact]
    public async Task Periodo_Incluye_Inicio_Excluye_Fin_Y_Ordena()
    {
        // Arrange
        var periodo = ConsultaAuditoriaRevision.Periodo(ConsultaAuditoriaRevision.Desde, ConsultaAuditoriaRevision.Hasta);

        // Act
        using var respuesta = await entorno.ConsultarAsync(periodo);
        using var eventos = await ConsultaAuditoriaRevision.LeerEventosAsync(respuesta);

        // Assert
        ComprobarEventos(eventos.RootElement, entorno.Eventos);
        Assert.Contains(eventos.RootElement.EnumerateArray(), e => e.GetProperty("fechaEvento").GetDateTimeOffset() == ConsultaAuditoriaRevision.Desde);
    }

    /// Cada tipo devuelve solo sus eventos; escribir el nombre con otras mayúsculas conserva el resultado.
    [Theory]
    [InlineData("Paciente")]
    [InlineData("Cita")]
    [InlineData("Usuario")]
    [InlineData("pAcIeNtE")]
    public async Task Tipo_De_Registro_Selecciona_Solo_Los_Correspondientes(string tipo)
    {
        // Arrange
        var periodo = ConsultaAuditoriaRevision.Periodo(ConsultaAuditoriaRevision.Desde, ConsultaAuditoriaRevision.Hasta);
        var esperados = entorno.Eventos.Where(e => string.Equals(e.Tipo.ToString(), tipo, StringComparison.OrdinalIgnoreCase));

        // Act
        using var respuesta = await entorno.ConsultarAsync(periodo + "&tipoRegistro=" + Uri.EscapeDataString(tipo));
        using var eventos = await ConsultaAuditoriaRevision.LeerEventosAsync(respuesta);

        // Assert
        ComprobarEventos(eventos.RootElement, esperados);
    }

    /// Filtrar por identificador no entrega cambios de otro paciente, cita o usuario.
    [Theory]
    [InlineData("Paciente")]
    [InlineData("Cita")]
    [InlineData("Usuario")]
    public async Task Identificador_Selecciona_Solo_El_Registro_Pedido(string clase)
    {
        // Arrange
        var id = clase switch { "Paciente" => entorno.Paciente, "Cita" => entorno.Cita, _ => entorno.Usuario };
        var periodo = ConsultaAuditoriaRevision.Periodo(ConsultaAuditoriaRevision.Desde, ConsultaAuditoriaRevision.Hasta);

        // Act
        using var respuesta = await entorno.ConsultarAsync(periodo + "&idRegistro=" + id);
        using var eventos = await ConsultaAuditoriaRevision.LeerEventosAsync(respuesta);

        // Assert
        ComprobarEventos(eventos.RootElement, entorno.Eventos.Where(e => e.Registro == id));
    }

    /// Tipo e identificador se cumplen juntos; si no corresponden al mismo registro, devuelve una lista vacía.
    [Theory]
    [InlineData("Paciente", true)]
    [InlineData("Cita", false)]
    public async Task Tipo_E_Identificador_Se_Combinan(string tipo, bool coincide)
    {
        // Arrange
        var periodo = ConsultaAuditoriaRevision.Periodo(ConsultaAuditoriaRevision.Desde, ConsultaAuditoriaRevision.Hasta);
        var esperados = coincide ? entorno.Eventos.Where(e => e.Registro == entorno.Paciente) : [];

        // Act
        using var respuesta = await entorno.ConsultarAsync(periodo + $"&tipoRegistro={tipo}&idRegistro={entorno.Paciente}");
        using var eventos = await ConsultaAuditoriaRevision.LeerEventosAsync(respuesta);

        // Assert
        ComprobarEventos(eventos.RootElement, esperados);
    }

    /// Un identificador sin eventos devuelve una lista vacía, sin entregar otros registros.
    [Fact]
    public async Task Registro_Sin_Eventos_Devuelve_Lista_Vacia()
    {
        // Arrange
        var periodo = ConsultaAuditoriaRevision.Periodo(ConsultaAuditoriaRevision.Desde, ConsultaAuditoriaRevision.Hasta);

        // Act
        using var respuesta = await entorno.ConsultarAsync(periodo + "&idRegistro=" + Guid.NewGuid());
        using var eventos = await ConsultaAuditoriaRevision.LeerEventosAsync(respuesta);

        // Assert
        Assert.Empty(eventos.RootElement.EnumerateArray());
    }


    // ──── LÍMITES DE RESULTADOS ─────────────────────────────────────────────────────────────


    /// Con 501 eventos disponibles, los límites válidos entregan exactamente la cantidad pedida y los más recientes.
    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(499)]
    [InlineData(500)]
    public async Task Limite_Valido_Entrega_Cantidad_Y_Orden_Exactos(int limite)
    {
        // Arrange
        var periodo = ConsultaAuditoriaRevision.Periodo(ConsultaAuditoriaRevision.Hasta, ConsultaAuditoriaRevision.Hasta.AddDays(1));
        var esperados = entorno.Volumen.OrderByDescending(e => e.Fecha).Take(limite);

        // Act
        using var respuesta = await entorno.ConsultarAsync(periodo + $"&idRegistro={entorno.RegistroVolumen}&limite={limite}");
        using var eventos = await ConsultaAuditoriaRevision.LeerEventosAsync(respuesta);

        // Assert
        ComprobarEventos(eventos.RootElement, esperados);
    }

    /// Si no se indica límite, entrega los 100 eventos más recientes de los 501 disponibles.
    [Fact]
    public async Task Limite_Omitido_Usa_Cien()
    {
        // Arrange
        var periodo = ConsultaAuditoriaRevision.Periodo(ConsultaAuditoriaRevision.Hasta, ConsultaAuditoriaRevision.Hasta.AddDays(1));

        // Act
        using var respuesta = await entorno.ConsultarAsync(periodo + "&idRegistro=" + entorno.RegistroVolumen);
        using var eventos = await ConsultaAuditoriaRevision.LeerEventosAsync(respuesta);

        // Assert
        ComprobarEventos(eventos.RootElement, entorno.Volumen.OrderByDescending(e => e.Fecha).Take(100));
    }

    /// Cero, negativos, más de 500 o un límite que no sea entero se rechazan con 400.
    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("501")]
    [InlineData("abc")]
    [InlineData("1.5")]
    [InlineData("2147483648")]
    public async Task Limite_Invalido_Se_Rechaza(string limite)
    {
        // Arrange
        var periodo = ConsultaAuditoriaRevision.Periodo(ConsultaAuditoriaRevision.Desde, ConsultaAuditoriaRevision.Hasta);

        // Act
        using var respuesta = await entorno.ConsultarAsync(periodo + "&limite=" + Uri.EscapeDataString(limite));

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }


    // ──── ENTRADAS INVÁLIDAS ────────────────────────────────────────────────────────────────


    /// Un tipo inexistente, una lista de tipos o texto de SQL no se interpreta como un tipo válido.
    [Theory]
    [InlineData("Inexistente")]
    [InlineData("999")]
    [InlineData("-1")]
    [InlineData("Cita,Usuario")]
    [InlineData("Paciente,Cita")]
    [InlineData("Paciente' OR 1=1--")]
    public async Task Tipo_Invalido_O_Combinado_Se_Rechaza(string tipo)
    {
        // Arrange
        var periodo = ConsultaAuditoriaRevision.Periodo(ConsultaAuditoriaRevision.Desde, ConsultaAuditoriaRevision.Hasta);

        // Act
        using var respuesta = await entorno.ConsultarAsync(periodo + "&tipoRegistro=" + Uri.EscapeDataString(tipo));
        if (tipo == "Paciente,Cita") await entorno.GuardarTipoCombinadoAsync(respuesta);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    /// Un identificador mal formado, incluido texto de SQL, no se acepta ni amplía la consulta.
    [Theory]
    [InlineData("no-es-identificador")]
    [InlineData("' OR 1=1--")]
    public async Task Identificador_Mal_Formado_Se_Rechaza(string id)
    {
        // Arrange
        var periodo = ConsultaAuditoriaRevision.Periodo(ConsultaAuditoriaRevision.Desde, ConsultaAuditoriaRevision.Hasta);

        // Act
        using var respuesta = await entorno.ConsultarAsync(periodo + "&idRegistro=" + Uri.EscapeDataString(id));

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    /// Faltan las fechas o el periodo no avanza: se rechaza sin entregar eventos.
    [Theory]
    [InlineData("")]
    [InlineData("desde=2026-10-10T09:00:00Z")]
    [InlineData("hasta=2026-10-10T10:00:00Z")]
    [InlineData("desde=2026-10-10T09:00:00Z&hasta=2026-10-10T09:00:00Z")]
    [InlineData("desde=2026-10-10T10:00:00Z&hasta=2026-10-10T09:00:00Z")]
    public async Task Periodo_Incompleto_O_Sin_Avance_Se_Rechaza(string parametros)
    {
        // Arrange
        var consulta = parametros;

        // Act
        using var respuesta = await entorno.ConsultarAsync(consulta);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }


    // ──── PERMISOS Y SOLO LECTURA ───────────────────────────────────────────────────────────


    /// Sin sesión válida o sin permiso actual no se devuelve ningún evento, aunque la cookie haya servido antes.
    [Theory]
    [InlineData("Ausente", 401)]
    [InlineData("Invalida", 401)]
    [InlineData("Usuaria", 403)]
    [InlineData("Temporal", 403)]
    [InlineData("Desactivada", 401)]
    [InlineData("RolCambiado", 401)]
    public async Task Sesion_Y_Permiso_Actual_Se_Exigen(string sesion, int estado)
    {
        // Arrange
        var periodo = ConsultaAuditoriaRevision.Periodo(ConsultaAuditoriaRevision.Desde, ConsultaAuditoriaRevision.Hasta);

        // Act
        using var respuesta = await entorno.ConsultarAsync(periodo, sesion);
        var cuerpo = await respuesta.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal((HttpStatusCode)estado, respuesta.StatusCode);
        Assert.Null(respuesta.Headers.Location);
        Assert.Empty(cuerpo);
    }

    /// La ruta de lectura rechaza métodos de escritura y todas las filas de auditoría permanecen iguales.
    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    [InlineData("DELETE")]
    public async Task Ruta_No_Ofrece_Escritura(string metodo)
    {
        // Arrange
        var periodo = ConsultaAuditoriaRevision.Periodo(ConsultaAuditoriaRevision.Desde, ConsultaAuditoriaRevision.Hasta);
        var antes = await entorno.HuellaAuditoriaAsync();

        // Act
        using var respuesta = await entorno.ConsultarAsync(periodo, metodo: metodo);
        var despues = await entorno.HuellaAuditoriaAsync();

        // Assert
        Assert.Equal(HttpStatusCode.MethodNotAllowed, respuesta.StatusCode);
        Assert.Equal(antes, despues);
    }

    /// Consultar devuelve los eventos esperados sin insertar, modificar ni borrar ninguna fila de auditoría.
    [Fact]
    public async Task Consulta_Conserva_Todas_Las_Filas_De_Auditoria()
    {
        // Arrange
        var periodo = ConsultaAuditoriaRevision.Periodo(ConsultaAuditoriaRevision.Desde, ConsultaAuditoriaRevision.Hasta);
        var antes = await entorno.HuellaAuditoriaAsync();

        // Act
        using var respuesta = await entorno.ConsultarAsync(periodo);
        using var eventos = await ConsultaAuditoriaRevision.LeerEventosAsync(respuesta);
        var despues = await entorno.HuellaAuditoriaAsync();

        // Assert
        ComprobarEventos(eventos.RootElement, entorno.Eventos);
        Assert.Equal(antes, despues);
    }


    // ──── CONTENIDO SIN VALORES PRIVADOS ────────────────────────────────────────────────────


    /// Solo salen los ocho campos de metadatos; contacto, contraseña, hash, claves y sesión no aparecen.
    [Fact]
    public async Task Respuesta_Contiene_Metadatos_Sin_Valores_Privados()
    {
        // Arrange
        var periodo = ConsultaAuditoriaRevision.Periodo(ConsultaAuditoriaRevision.Desde, ConsultaAuditoriaRevision.Hasta);
        var permitidos = new[] { "idEvento", "fechaEvento", "idUsuarioActor", "tipoRegistro", "idRegistro", "accion", "camposAfectados", "hecho" }.Order().ToArray();

        // Act
        using var respuesta = await entorno.ConsultarAsync(periodo);
        var cuerpo = await respuesta.Content.ReadAsStringAsync();
        using var eventos = JsonDocument.Parse(cuerpo);

        // Assert
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        ComprobarEventos(eventos.RootElement, entorno.Eventos);
        foreach (var evento in eventos.RootElement.EnumerateArray())
        {
            Assert.True(permitidos.SequenceEqual(evento.EnumerateObject().Select(p => p.Name).Order()), "Solo deben aparecer los ocho campos de metadatos.");
            Assert.Equal(entorno.Actor, evento.GetProperty("idUsuarioActor").GetGuid());
            Assert.Equal("Actualizacion", evento.GetProperty("accion").GetString());
            Assert.Equal("Cambio ficticio sin valores privados", evento.GetProperty("hecho").GetString());
            Assert.Equal(new[] { "Nombre" }, evento.GetProperty("camposAfectados").EnumerateArray().Select(c => c.GetString()).ToArray());
        }
        Assert.True(entorno.SinValoresPrivados(cuerpo, eventos.RootElement), "La respuesta no debe revelar ninguno de los valores privados ficticios guardados.");
    }

    /// Compara eventos completos de prueba: cantidad, identificador, fecha, tipo y registro en el orden esperado.
    private static void ComprobarEventos(JsonElement respuesta, IEnumerable<EventoEsperado> esperados)
    {
        var lista = esperados.OrderByDescending(e => e.Fecha).ToArray();
        var recibidos = respuesta.EnumerateArray().ToArray();
        Assert.Equal(lista.Length, recibidos.Length);
        for (var i = 0; i < lista.Length; i++)
        {
            Assert.Equal(lista[i].Id, recibidos[i].GetProperty("idEvento").GetGuid());
            Assert.Equal(lista[i].Fecha, recibidos[i].GetProperty("fechaEvento").GetDateTimeOffset());
            Assert.Equal(lista[i].Tipo.ToString(), recibidos[i].GetProperty("tipoRegistro").GetString());
            Assert.Equal(lista[i].Registro, recibidos[i].GetProperty("idRegistro").GetGuid());
        }
    }
}
