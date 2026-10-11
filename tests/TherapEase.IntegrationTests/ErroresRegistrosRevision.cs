using System.Net;
using System.Text.Json;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TherapEase.Domain.Pacientes.Entidades.Enums;
using TherapEase.Domain.Pacientes.Reglas;

namespace TherapEase.IntegrationTests;

/// <summary>
/// Prepara datos sensibles ficticios y provoca errores en la API real por HTTPS.
/// Busca esos valores en respuestas y registros; la evidencia conserva solo resultados y categorías.
/// </summary>
public sealed class ErroresRegistrosRevision : IAsyncDisposable
{


    // ──── DATOS Y SERVIDOR DE CADA CASO ────────────────────────────────────────────────────


    private readonly HttpsLocalRevision _https;
    private readonly Dictionary<string, List<string>> _privados = new();
    private int _cerrado;
    public const string Contacto = "contacto-errores-rastreable@ficticio.test";
    public const string TextoClinico = "texto-clinico-ficticio-solo-entrada";
    private const string Nombre = "Paciente ficticio rastreable errores";
    public string Evidencia { get; }
    public string Periodo => "desde=2026-10-10T00:00:00Z&hasta=2026-10-11T00:00:00Z";

    private ErroresRegistrosRevision(string caso)
    {
        var carpeta = Environment.GetEnvironmentVariable("THERAPEASE_EVIDENCIA_ERRORES")
            ?? Path.Combine(ServidorRevision.RaizRepositorio(), "TestResults", "errores-registros", Guid.NewGuid().ToString("N"));
        Evidencia = Path.Combine(carpeta, caso);
        _https = new HttpsLocalRevision(Evidencia);
    }

    /// Crea un servidor y PostgreSQL exclusivos del caso; los retira si falla la preparación.
    public static async Task<ErroresRegistrosRevision> CrearAsync(string caso)
    {
        var entorno = new ErroresRegistrosRevision(caso);
        try
        {
            await entorno._https.InitializeAsync();
            await entorno.PrepararPrivadosAsync();
            return entorno;
        }
        catch
        {
            await entorno.DisposeAsync();
            throw;
        }
    }

    /// Guarda paciente/contacto y obtiene hashes, sellos y claves reales del entorno ficticio.
    private async Task PrepararPrivadosAsync()
    {
        await using var contexto = _https.Escenario.Contexto();
        contexto.Pacientes.Add(ReglasDePaciente.Registrar(Guid.NewGuid(), Nombre, Contacto,
            [AmbitoAtencion.Independiente], _https.Registro, DateTimeOffset.UtcNow));
        await contexto.SaveChangesAsync();
        var paciente = await contexto.Pacientes.AsNoTracking().SingleAsync();
        if (paciente.Nombre != Nombre || paciente.Contacto != Contacto)
            throw new InvalidOperationException("No se guardó la preparación ficticia de errores.");

        Agregar("Paciente", Nombre);
        Agregar("Contacto", Contacto);
        Agregar("TextoClinicoSoloEntrada", TextoClinico);
        Agregar("Cuenta", "https-ficticio");
        Agregar("ContrasenaSesion", "contrasenaficticiahttpslocal");
        var usuario = await contexto.Users.AsNoTracking().SingleAsync();
        Agregar("Hash", usuario.PasswordHash!);
        Agregar("Sello", usuario.SecurityStamp!);
        var claves = await contexto.DataProtectionKeys.AsNoTracking().Select(c => c.Xml!).ToListAsync();
        if (claves.Count == 0) throw new InvalidOperationException("Falta la clave de sesión ficticia.");
        foreach (var clave in claves)
        {
            Agregar("ClaveDeSesion", clave);
            foreach (var valor in XDocument.Parse(clave).Descendants("value"))
                Agregar("ClaveDeSesion", valor.Value);
        }
        Agregar("Cookie", _https.Cookie);
        Agregar("Cookie", _https.Cookie.Split('=', 2)[1]);
        foreach (var cadena in new[] { _https.Escenario.App, _https.Escenario.Migrador })
        {
            Agregar("Conexion", cadena);
            Agregar("ClaveBD", new NpgsqlConnectionStringBuilder(cadena).Password!);
        }

        // Comprobamos que la búsqueda sí detecta los datos de muestra, también si vienen codificados.

        foreach (var valor in new[] { Contacto, Nombre, usuario.PasswordHash! })
            foreach (var variante in Variantes(valor))
                if (CategoriasEncontradas(variante).Length == 0)
                    throw new InvalidOperationException("La búsqueda no detectó un dato ficticio de control.");
    }

    /// Conserva cada valor solo en memoria, agrupado para informar la categoría sin revelar su contenido.
    private void Agregar(string categoria, string valor)
    {
        if (string.IsNullOrEmpty(valor)) throw new InvalidOperationException("Falta un dato ficticio de control.");
        if (!_privados.TryGetValue(categoria, out var valores)) _privados[categoria] = valores = [];
        valores.Add(valor);
    }


    // ──── SOLICITUDES Y FALLOS REALES ──────────────────────────────────────────────────────


    /// Envía la consulta por HTTPS; permite omitir la sesión o enviar una cookie inválida.
    public async Task<HttpResponseMessage> ConsultarAsync(string parametros, string sesion = "valida")
    {
        using var solicitud = new HttpRequestMessage(HttpMethod.Get,
            new Uri(_https.Https, "/api/auditoria/eventos?" + parametros));
        if (sesion != "ausente")
            solicitud.Headers.Add("Cookie", sesion == "valida" ? _https.Cookie : "TherapEase.Sesion=cookieficticiainvalidaerrores");
        return await _https.Cliente.SendAsync(solicitud);
    }

    /// Retira SELECT solo de la tabla elegida en esta base ficticia; no modifica el código del servidor.
    public Task RetirarLecturaAsync(string tabla) => PermisoLecturaAsync(tabla, retirar: true);

    /// Devuelve SELECT para comprobar que el servidor se recupera después del fallo provocado.
    public Task DevolverLecturaAsync(string tabla) => PermisoLecturaAsync(tabla, retirar: false);

    /// Acepta únicamente las dos tablas de estos casos, para acotar el cambio de permisos.
    private Task PermisoLecturaAsync(string tabla, bool retirar)
    {
        var nombre = tabla switch
        {
            "auditoria" => "EventoAuditoria",
            "identidad" => "AspNetUsers",
            _ => throw new ArgumentOutOfRangeException(nameof(tabla))
        };
        var sql = retirar ? $"REVOKE SELECT ON \"{nombre}\" FROM therapease_app"
            : $"GRANT SELECT ON \"{nombre}\" TO therapease_app";
        return PostgreSqlRevision.Sql(_https.Escenario.Migrador, sql);
    }


    // ──── BÚSQUEDA Y EVIDENCIA SIN VALORES PRIVADOS ─────────────────────────────────────────


    /// Busca también representaciones de URL, HTML y JSON para que la codificación no oculte un valor.
    private static IEnumerable<string> Variantes(string valor) =>
        new[] { valor, Uri.EscapeDataString(valor), WebUtility.HtmlEncode(valor), JsonSerializer.Serialize(valor)[1..^1] }.Distinct();

    /// Devuelve solo las categorías encontradas, sin imprimir el valor que coincidió.
    private string[] CategoriasEncontradas(string texto) => _privados
        .Where(c => c.Value.SelectMany(Variantes).Any(v => texto.Contains(v, StringComparison.Ordinal)))
        .Select(c => c.Key).ToArray();

    /// Revisa cuerpo y cabeceras de respuesta; Set-Cookie se permite como entrega normal de la sesión.
    public async Task<RespuestaRevisada> RevisarRespuestaAsync(HttpResponseMessage respuesta, string nombre)
    {
        if (respuesta.Headers.TryGetValues("Set-Cookie", out var cookies))
            foreach (var cookie in cookies.Where(c => c.StartsWith("TherapEase.Sesion=", StringComparison.Ordinal)))
            {
                var par = cookie.Split(';', 2)[0];
                Agregar("Cookie", par);
                Agregar("Cookie", par.Split('=', 2)[1]);
            }
        var cuerpo = await respuesta.Content.ReadAsStringAsync();
        var cabeceras = respuesta.Headers.Where(c => !c.Key.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase))
            .Concat(respuesta.Content.Headers).SelectMany(c => c.Value);
        var categorias = CategoriasEncontradas(cuerpo + "\n" + string.Join('\n', cabeceras));
        var detalles = new[] { "PostgresException", "Npgsql.", "Microsoft.EntityFrameworkCore", "SELECT ",
            "PasswordHash", "SecurityStamp", "StackTrace", "\\src\\", "/src/" };
        var resultado = new RespuestaRevisada((int)respuesta.StatusCode, categorias.Length == 0,
            !detalles.Any(d => cuerpo.Contains(d, StringComparison.Ordinal)), cuerpo.Length == 0,
            cuerpo.Contains("Ocurrió un error", StringComparison.Ordinal), respuesta.Headers.CacheControl?.NoStore == true);
        await File.WriteAllTextAsync(Path.Combine(Evidencia, nombre + ".json"), JsonSerializer.Serialize(new
        { Resultado = resultado, CategoriasEncontradas = categorias, Cookies = "Valores permitidos solo en Set-Cookie; buscados en cuerpo y registros" },
            new JsonSerializerOptions { WriteIndented = true }));
        return resultado;
    }

    /// Primero termina y drena el proceso; después busca en todos sus registros, sin guardar los mensajes crudos.
    public async Task<RegistrosRevisados> CerrarYRevisarRegistrosAsync()
    {
        await DisposeAsync();
        var categorias = _privados.Where(c => !_https.RegistrosSinValores(c.Value.SelectMany(Variantes))).Select(c => c.Key).ToArray();
        var resultado = new RegistrosRevisados(_https.CantidadDeRegistros,
            categorias.Length == 0 && _https.RegistrosSinSecretos(), _https.RegistrosContienen("42501"));
        await File.WriteAllTextAsync(Path.Combine(Evidencia, "registros.json"), JsonSerializer.Serialize(new
        { Resultado = resultado, CategoriasEncontradas = categorias, Lectura = "Salida y errores completos tras detener el proceso; sin contenido crudo" },
            new JsonSerializerOptions { WriteIndented = true }));
        return resultado;
    }

    /// La limpieza puede pedirse antes de comprobar los registros y repetirse al salir de la prueba.
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _cerrado, 1) == 0) await _https.DisposeAsync();
    }
}

/// <summary>
/// Resume una respuesta HTTP sin conservar su cuerpo ni valores privados.
/// Distingue el rechazo, la ausencia de detalles técnicos y la página genérica de error.
/// </summary>
public sealed record RespuestaRevisada(int Estado, bool SinValoresPrivados, bool SinDetallesTecnicos,
    bool CuerpoVacio, bool PaginaGenerica, bool NoStore);

/// <summary>
/// Resume la búsqueda en los registros completos de un proceso ya detenido.
/// El código 42501 confirma que PostgreSQL rechazó realmente la lectura por permisos.
/// </summary>
public sealed record RegistrosRevisados(int Cantidad, bool SinValoresPrivados, bool FalloPostgreSqlRegistrado);
