using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using TherapEase.Application.Identidad.Interfaces.Repositorios;
using TherapEase.Application.Identidad.Interfaces.Servicios;
using TherapEase.Application.Identidad.Modelos;
using TherapEase.Domain.Auditoria.Entidades.Enums;
using TherapEase.Domain.Auditoria.Reglas;
using TherapEase.Domain.Citas.Reglas;
using TherapEase.Domain.Pacientes.Entidades.Enums;
using TherapEase.Domain.Pacientes.Reglas;

namespace TherapEase.IntegrationTests;

/// <summary>
/// Prepara eventos y datos privados ficticios para consultar auditoría por HTTPS real.
/// Usa PostgreSQL desechable y conserva separados los casos de filtros, volumen y permisos.
/// </summary>
public sealed class ConsultaAuditoriaRevision : IAsyncLifetime
{


    // ──── DATOS CONOCIDOS DE LA REVISIÓN ───────────────────────────────────────────────────


    private readonly HttpsLocalRevision _https;
    private readonly Dictionary<string, string> _cookies = new();
    private readonly List<string> _datosPrivados = [];
    private const string Contrasena = "datosensiblerastreableq03";
    private const string NombrePaciente = "Paciente ficticio dato rastreable Q03";
    private const string Contacto = "contacto-rastreable-q03@ficticio.test";
    public string Evidencia { get; } = Environment.GetEnvironmentVariable("THERAPEASE_EVIDENCIA_Q03")
        ?? Path.Combine(ServidorRevision.RaizRepositorio(), "TestResults", "auditoria-api", Guid.NewGuid().ToString("N"));
    public static DateTimeOffset Desde { get; } = new(2026, 10, 10, 9, 0, 0, TimeSpan.Zero);
    public static DateTimeOffset Hasta => Desde.AddHours(1);
    public Guid Paciente { get; } = Guid.NewGuid();
    public Guid Cita { get; } = Guid.NewGuid();
    public Guid Usuario { get; private set; }
    public Guid Actor => _https.Registro;
    public Guid RegistroVolumen { get; } = Guid.NewGuid();
    public List<EventoEsperado> Eventos { get; } = [];
    public List<EventoEsperado> Volumen { get; } = [];

    public ConsultaAuditoriaRevision() => _https = new HttpsLocalRevision(Evidencia);

    /// Inicia HTTPS, crea cuentas y carga filas reales, sin datos de ninguna persona.
    public async Task InitializeAsync()
    {
        try
        {
            await _https.InitializeAsync();
            await PrepararUsuariosAsync();
            await PrepararEventosAsync();
        }
        catch
        {
            await DisposeAsync();
            throw;
        }
    }


    // ──── CUENTAS Y VALORES QUE NO DEBEN SALIR ─────────────────────────────────────────────


    /// Obtiene sesiones válidas y luego revoca algunas con servicios reales para comprobar el permiso actual.
    private async Task PrepararUsuariosAsync()
    {
        var reloj = new RelojRevision();
        reloj.Avanzar(Hasta.AddDays(2) - reloj.GetUtcNow());

        // Solo los eventos usan la fecha fija; las sesiones conservan la hora real para no vencer al preparar datos.

        using var proveedor = _https.Escenario.Servicios(servicios =>
        {
            servicios.AddSingleton<TimeProvider>(reloj);
            servicios.Configure<CookieAuthenticationOptions>(IdentityConstants.ApplicationScheme, opciones => opciones.TimeProvider = TimeProvider.System);
            servicios.Configure<SecurityStampValidatorOptions>(opciones => opciones.TimeProvider = TimeProvider.System);
        });
        var ids = new Dictionary<string, Guid>();
        foreach (var caso in new[] { "Usuaria", "Temporal", "Desactivada", "RolCambiado" })
        {
            var id = Guid.NewGuid();
            var nombre = "dato-rastreable-q03-" + caso.ToLowerInvariant();
            using (var alcance = proveedor.CreateScope())
            {
                var creado = await alcance.ServiceProvider.GetRequiredService<IGestorIdentidad>()
                    .CrearAsync(id, nombre, caso == "Usuaria" ? "Usuaria" : "Superusuario", Contrasena, caso == "Temporal");
                if (creado != ResultadoCreacionDeUsuario.Creado)
                    throw new InvalidOperationException("No se creó una cuenta ficticia para Q03.");
            }
            using (var entrada = new SolicitudIdentidad(proveedor))
            {
                if (!(await entrada.Obtener<IServicioAcceso>().IniciarSesionAsync(nombre, Contrasena)).Exito)
                    throw new InvalidOperationException("No se obtuvo la sesión ficticia para Q03.");
                _cookies.Add(caso, entrada.CookieEmitida());
            }
            ids.Add(caso, id);
            _datosPrivados.Add(nombre);
        }
        Usuario = ids["Usuaria"];
        using (var solicitud = new SolicitudIdentidad(proveedor, _https.Cookie))
        {
            if (!await solicitud.AutenticarAsync()) throw new InvalidOperationException("La sesión de preparación no es válida.");
            var servicio = solicitud.Obtener<IServicioUsuarios>();
            if (!(await servicio.DesactivarUsuarioAsync(ids["Desactivada"])).Exito
                || !(await servicio.CambiarRolAsync(ids["RolCambiado"], "Usuaria")).Exito)
                throw new InvalidOperationException("No se revocaron las sesiones ficticias de preparación.");
        }
        await using var contexto = _https.Escenario.Contexto();
        var cuentas = await contexto.Users.AsNoTracking().ToListAsync();
        if (cuentas.Any(c => string.IsNullOrEmpty(c.PasswordHash) || string.IsNullOrEmpty(c.SecurityStamp)))
            throw new InvalidOperationException("Faltan hashes o sellos reales en la preparación.");
        _datosPrivados.AddRange(cuentas.SelectMany(c => new[] { c.UserName!, c.PasswordHash!, c.SecurityStamp! }));
        var claves = await contexto.DataProtectionKeys.AsNoTracking().Select(c => c.Xml!).ToListAsync();
        if (claves.Count == 0) throw new InvalidOperationException("No se guardaron claves de sesión en la base de prueba.");
        _datosPrivados.AddRange(claves);
        _datosPrivados.AddRange(_cookies.Values);
        _datosPrivados.AddRange([NombrePaciente, Contacto, Contrasena, _https.Cookie, _https.Escenario.App]);
    }

    /// Inserta cinco eventos dentro del periodo, dos en sus bordes y 501 para probar límites sin resultados incompletos.
    private async Task PrepararEventosAsync()
    {
        var actor = _https.Registro;
        await using var contexto = _https.Escenario.Contexto();
        contexto.Pacientes.Add(ReglasDePaciente.Registrar(Paciente, NombrePaciente, Contacto,
            [AmbitoAtencion.Independiente], actor, Desde));
        contexto.Citas.Add(ReglasDeCita.Agendar(Cita, Paciente, AmbitoAtencion.Independiente,
            Desde, Desde.AddMinutes(30), actor, Desde));
        foreach (var (tipo, registro, fecha) in new[]
        {
            (TipoRegistroAuditoria.Paciente, Paciente, Desde),
            (TipoRegistroAuditoria.Paciente, Paciente, Desde.AddMinutes(15)),
            (TipoRegistroAuditoria.Cita, Cita, Desde.AddMinutes(30)),
            (TipoRegistroAuditoria.Usuario, Usuario, Desde.AddMinutes(45)),
            (TipoRegistroAuditoria.Paciente, Guid.NewGuid(), Desde.AddMinutes(50)),
            (TipoRegistroAuditoria.Paciente, Paciente, Desde.AddTicks(-10)),
            (TipoRegistroAuditoria.Paciente, Paciente, Hasta)
        })
        {
            var id = Guid.NewGuid();
            contexto.EventosAuditoria.Add(ReglasDeEventoAuditoria.Registrar(id, fecha, actor, tipo,
                registro, AccionAuditoria.Actualizacion, ["Nombre"], "Cambio ficticio sin valores privados"));
            if (fecha >= Desde && fecha < Hasta) Eventos.Add(new(id, fecha, tipo, registro));
        }
        for (var i = 0; i < 501; i++)
        {
            var fecha = Hasta.AddHours(1).AddSeconds(i);
            var id = Guid.NewGuid();
            contexto.EventosAuditoria.Add(ReglasDeEventoAuditoria.Registrar(id, fecha, actor,
                TipoRegistroAuditoria.Paciente, RegistroVolumen, AccionAuditoria.Actualizacion,
                ["Nombre"], "Cambio ficticio para comprobar el límite"));
            Volumen.Add(new(id, fecha, TipoRegistroAuditoria.Paciente, RegistroVolumen));
        }
        await contexto.SaveChangesAsync();
        var paciente = await contexto.Pacientes.AsNoTracking().SingleAsync(p => p.IdPaciente == Paciente);
        if (paciente.Nombre != NombrePaciente || paciente.Contacto != Contacto)
            throw new InvalidOperationException("No quedaron guardados los datos privados ficticios.");
        await File.WriteAllTextAsync(Path.Combine(Evidencia, "preparacion.json"), JsonSerializer.Serialize(new
        {
            EventosEnPeriodo = Eventos.Count, EventosEnBordes = 2, EventosDeVolumen = Volumen.Count,
            PrivadosGuardados = true, Sesiones = "Superusuario, ausente, inválida, usuaria, temporal, desactivada y rol cambiado",
            Captura = "Eventos de pacientes/cita preparados por QA; desactivación y cambio de rol usan servicios reales"
        }, new JsonSerializerOptions { WriteIndented = true }));
    }


    // ──── CONSULTAS Y COMPROBACIONES ───────────────────────────────────────────────────────


    /// Arma un periodo con zona explícita, sin depender de la hora o zona de la computadora.
    public static string Periodo(DateTimeOffset desde, DateTimeOffset hasta) =>
        "desde=" + Uri.EscapeDataString(desde.ToString("O", CultureInfo.InvariantCulture))
        + "&hasta=" + Uri.EscapeDataString(hasta.ToString("O", CultureInfo.InvariantCulture));

    /// Consulta la API real por HTTPS, con la sesión que corresponda al caso.
    public async Task<HttpResponseMessage> ConsultarAsync(string parametros, string sesion = "Superusuario", string metodo = "GET")
    {
        var cookie = sesion switch
        {
            "Superusuario" => _https.Cookie,
            "Ausente" => null,
            "Invalida" => "TherapEase.Sesion=sesionficticiainvalida",
            _ => _cookies[sesion]
        };
        using var solicitud = new HttpRequestMessage(new HttpMethod(metodo),
            new Uri(_https.Https, "/api/auditoria/eventos?" + parametros));
        if (cookie is not null) solicitud.Headers.Add("Cookie", cookie);
        return await _https.Cliente.SendAsync(solicitud);
    }

    /// Lee los eventos de una respuesta correcta, conservando orden e identificadores reales.
    public static async Task<JsonDocument> LeerEventosAsync(HttpResponseMessage respuesta)
    {
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        return JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
    }

    /// Calcula una huella de todas las filas para detectar cualquier cambio en auditoría, sin imprimir sus valores.
    public async Task<string> HuellaAuditoriaAsync()
    {
        await using var conexion = new NpgsqlConnection(_https.Escenario.App);
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand("""
            SELECT coalesce(jsonb_agg(to_jsonb(e) ORDER BY e."IdEvento"), '[]'::jsonb)::text
            FROM "EventoAuditoria" AS e
            """, conexion);
        var filas = (string)(await comando.ExecuteScalarAsync())!;
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(filas)));
    }

    /// Guarda el rechazo esperado y el tipo realmente devuelto, sin incluir valores privados ni cookies.
    public async Task GuardarTipoCombinadoAsync(HttpResponseMessage respuesta)
    {
        var tipos = new List<string>();
        var cantidad = 0;
        if (respuesta.StatusCode == HttpStatusCode.OK)
        {
            using var cuerpo = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
            cantidad = cuerpo.RootElement.GetArrayLength();
            foreach (var evento in cuerpo.RootElement.EnumerateArray())
            {
                var tipo = evento.GetProperty("tipoRegistro").GetString();
                tipos.Add(tipo is "Paciente" or "Cita" or "Usuario" ? tipo : "No reconocido");
            }
        }
        await File.WriteAllTextAsync(Path.Combine(Evidencia, "tipo-combinado.json"), JsonSerializer.Serialize(new
        { Entrada = "Paciente,Cita", EstadoEsperado = 400, EstadoReal = (int)respuesta.StatusCode, Cantidad = cantidad, TiposDevueltos = tipos.Distinct().ToArray() },
            new JsonSerializerOptions { WriteIndented = true }));
    }

    /// Busca los valores privados guardados, también después de descodificar el texto JSON.
    public bool SinValoresPrivados(string cuerpo, JsonElement eventos)
    {
        var textos = cuerpo + "\n" + string.Join('\n', TextosJson(eventos));
        return _datosPrivados.All(valor => !textos.Contains(valor, StringComparison.Ordinal));
    }

    /// Recorre los textos del JSON para que los caracteres escapados no oculten un dato rastreable.
    private static IEnumerable<string> TextosJson(JsonElement elemento)
    {
        if (elemento.ValueKind == JsonValueKind.String) yield return elemento.GetString()!;
        if (elemento.ValueKind == JsonValueKind.Object)
            foreach (var campo in elemento.EnumerateObject())
                foreach (var texto in TextosJson(campo.Value)) yield return texto;
        if (elemento.ValueKind == JsonValueKind.Array)
            foreach (var hijo in elemento.EnumerateArray())
                foreach (var texto in TextosJson(hijo)) yield return texto;
    }

    /// Retira servidor, certificado privado y PostgreSQL por medio del entorno HTTPS reutilizado.
    public Task DisposeAsync() => _https.DisposeAsync();
}

/// <summary>
/// Describe un evento ficticio esperado, para comparar su fecha, clase de registro e identificador.
/// No contiene contacto, contraseña ni otros valores privados.
/// </summary>
public sealed record EventoEsperado(Guid Id, DateTimeOffset Fecha, TipoRegistroAuditoria Tipo, Guid Registro);
