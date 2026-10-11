using System.Net;

namespace TherapEase.IntegrationTests;

/// <summary>
/// Comprueba errores de la API y registros del servidor usando datos sensibles ficticios.
/// Cada caso termina su proceso antes de revisar todos los mensajes capturados.
/// </summary>
public sealed class ErroresRegistrosTests
{


    // ──── ENTRADAS INVÁLIDAS ───────────────────────────────────────────────────────────────


    /// Una entrada inválida devuelve 400 sin repetir el dato privado ni revelar detalles del servidor.
    [Theory]
    [InlineData("fecha")]
    [InlineData("tipo")]
    [InlineData("identificador")]
    [InlineData("limite-texto")]
    [InlineData("limite-desbordado")]
    [InlineData("periodo-invertido")]
    [InlineData("limite-fuera")]
    public async Task Entrada_Invalida_No_Revela_Datos_En_Respuesta_O_Registros(string caso)
    {
        // Arrange
        await using var entorno = await ErroresRegistrosRevision.CrearAsync(caso);
        var privado = Uri.EscapeDataString(ErroresRegistrosRevision.Contacto);
        var extra = "&dato=" + Uri.EscapeDataString(ErroresRegistrosRevision.TextoClinico);
        var parametros = caso switch
        {
            "fecha" => "desde=" + privado + "&hasta=2026-10-11T00:00:00Z",
            "tipo" => entorno.Periodo + "&tipoRegistro=" + privado,
            "identificador" => entorno.Periodo + "&idRegistro=" + privado,
            "limite-texto" => entorno.Periodo + "&limite=" + privado,
            "limite-desbordado" => entorno.Periodo + "&limite=99999999999999999999&contacto=" + privado,
            "periodo-invertido" => "desde=2026-10-11T00:00:00Z&hasta=2026-10-10T00:00:00Z&contacto=" + privado,
            "limite-fuera" => entorno.Periodo + "&limite=501&contacto=" + privado,
            _ => throw new ArgumentOutOfRangeException(nameof(caso))
        };

        // Act
        using var respuesta = await entorno.ConsultarAsync(parametros + extra);
        var revision = await entorno.RevisarRespuestaAsync(respuesta, "respuesta");
        var registros = await entorno.CerrarYRevisarRegistrosAsync();

        // Assert
        Assert.Equal((int)HttpStatusCode.BadRequest, revision.Estado);
        Assert.True(revision.SinValoresPrivados, "La respuesta no debe repetir los valores ficticios rastreables.");
        Assert.True(revision.SinDetallesTecnicos, "El error no debe revelar consultas, clases internas o rutas.");
        Assert.True(registros.Cantidad > 0, "Debe haberse capturado la salida real del proceso.");
        Assert.True(registros.SinValoresPrivados, "Los registros completos deben omitir los valores ficticios de control.");
    }


    // ──── ACCESO SIN SESIÓN VÁLIDA ─────────────────────────────────────────────────────────


    /// Sin sesión válida se devuelve 401; el dato privado enviado no sale en el error ni en registros.
    [Theory]
    [InlineData("ausente")]
    [InlineData("invalida")]
    public async Task Acceso_Rechazado_No_Revela_Datos_En_Respuesta_O_Registros(string sesion)
    {
        // Arrange
        await using var entorno = await ErroresRegistrosRevision.CrearAsync("sesion-" + sesion);
        var parametros = entorno.Periodo + "&contacto=" + Uri.EscapeDataString(ErroresRegistrosRevision.Contacto);

        // Act
        using var respuesta = await entorno.ConsultarAsync(parametros, sesion);
        var revision = await entorno.RevisarRespuestaAsync(respuesta, "respuesta");
        var registros = await entorno.CerrarYRevisarRegistrosAsync();

        // Assert
        Assert.Equal((int)HttpStatusCode.Unauthorized, revision.Estado);
        Assert.True(revision.SinValoresPrivados);
        Assert.True(revision.SinDetallesTecnicos);
        Assert.Null(respuesta.Headers.Location);
        Assert.True(registros.Cantidad > 0);
        Assert.True(registros.SinValoresPrivados, "Los registros no deben repetir datos recibidos en la solicitud.");
    }


    // ──── FALLOS DE BASE Y RECUPERACIÓN DEL SERVIDOR ────────────────────────────────────────


    /// Un rechazo real de PostgreSQL devuelve 500 genérico; los registros explican el fallo sin valores privados.
    [Theory]
    [InlineData("auditoria")]
    [InlineData("identidad")]
    public async Task Fallo_De_Lectura_No_Revela_Datos_Y_Permite_Recuperarse(string tabla)
    {
        // Arrange
        await using var entorno = await ErroresRegistrosRevision.CrearAsync("fallo-" + tabla);
        using var inicial = await entorno.ConsultarAsync(entorno.Periodo);
        Assert.Equal(HttpStatusCode.OK, inicial.StatusCode);
        await entorno.RetirarLecturaAsync(tabla);

        // Act
        RespuestaRevisada revision;
        try
        {
            using var fallo = await entorno.ConsultarAsync(entorno.Periodo
                + "&contacto=" + Uri.EscapeDataString(ErroresRegistrosRevision.Contacto));
            revision = await entorno.RevisarRespuestaAsync(fallo, "fallo");
        }
        finally
        {
            await entorno.DevolverLecturaAsync(tabla);
        }
        using var recuperada = await entorno.ConsultarAsync(entorno.Periodo);
        var recuperacion = await entorno.RevisarRespuestaAsync(recuperada, "recuperacion");
        var registros = await entorno.CerrarYRevisarRegistrosAsync();

        // Assert
        Assert.Equal((int)HttpStatusCode.InternalServerError, revision.Estado);
        Assert.True(revision.SinValoresPrivados);
        Assert.True(revision.SinDetallesTecnicos);
        Assert.True(revision.PaginaGenerica || revision.CuerpoVacio, "El error debe ser genérico o carecer de detalles.");
        if (tabla == "auditoria")
        {
            Assert.True(revision.PaginaGenerica, "La consulta fallida debe llegar a la página genérica de error.");
            Assert.True(revision.NoStore, "La página de error debe impedir el almacenamiento en caché.");
        }
        Assert.Equal((int)HttpStatusCode.OK, recuperacion.Estado);
        Assert.True(recuperacion.SinValoresPrivados);
        Assert.True(registros.FalloPostgreSqlRegistrado, "Debe existir el código real 42501 en los registros.");
        Assert.True(registros.SinValoresPrivados, "Incluso al registrar la excepción deben omitirse los valores ficticios privados.");
    }
}
