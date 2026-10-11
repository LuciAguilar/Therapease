using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using TherapEase.Application.Identidad.Interfaces.Repositorios;
using TherapEase.Application.Identidad.Interfaces.Servicios;
using TherapEase.Application.Identidad.Modelos;

namespace TherapEase.IntegrationTests;

/// <summary>
/// Construye la imagen real y prepara un servidor con PostgreSQL desechable.
/// Permite comprobar horas y reinicios sin agregar rutas o proyectos de producción.
/// </summary>
public sealed class HorasDockerRevision : IAsyncLifetime
{


    // ──── ENTORNO FICTICIO ──────────────────────────────────────────────────────────────────


    private readonly PostgreSqlRevision _postgres = new();
    private readonly string _nombre = "therapease-horas-" + Guid.NewGuid().ToString("N");
    private HttpClient? _cliente;
    private bool _contenedorCreado;
    private bool _imagenCreada;
    public string Imagen { get; } = "therapease-b00-horas:" + Guid.NewGuid().ToString("N");
    public string Evidencia { get; } = Environment.GetEnvironmentVariable("THERAPEASE_EVIDENCIA_HORAS")
        ?? Path.Combine(ServidorRevision.RaizRepositorio(), "TestResults", "horas-docker", Guid.NewGuid().ToString("N"));
    public Escenario Escenario { get; private set; } = null!;
    public string Cookie { get; private set; } = "";
    public Guid Registro { get; } = Guid.NewGuid();
    public Guid EventoIncluido { get; } = Guid.NewGuid();

    /// Construye la imagen actual, carga datos de ejemplo y la pone en marcha.
    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(Evidencia);
        try
        {
            await _postgres.InitializeAsync();
            Escenario = await _postgres.Crear();
            await PrepararDatosAsync();
            var construccion = await EjecutarAsync("docker", ["build", "--tag", Imagen,
                "--label", "therapease.revision=b00-horas-docker", "."], ServidorRevision.RaizRepositorio());
            await File.WriteAllTextAsync(Path.Combine(Evidencia, "construccion.log"), construccion.Salida + construccion.Error);
            ExigirExito(construccion, "construcción de la imagen; consultar construccion.log");
            _imagenCreada = true;
            var id = await DockerAsync("image", "inspect", "--format", "{{.Id}}", Imagen);
            ExigirExito(id, "identificación de la imagen");
            await File.WriteAllTextAsync(Path.Combine(Evidencia, "imagen.json"), JsonSerializer.Serialize(new
            { Imagen, Id = id.Salida.Trim(), ZonaServidor = "Europe/Madrid", Conexion = "PostgreSQL desechable; usuario de aplicación", FechaUtc = DateTimeOffset.UtcNow }));
            var conexion = new NpgsqlConnectionStringBuilder(Escenario.App) { Host = "host.docker.internal" }.ConnectionString;
            var reserva = new TcpListener(IPAddress.Loopback, 0);
            reserva.Start();
            var numero = ((IPEndPoint)reserva.LocalEndpoint).Port;
            reserva.Stop();

            // Un puerto asignado explícitamente se conserva cuando Docker reinicia el contenedor.

            var arranque = await DockerAsync("run", "--detach", "--name", _nombre,
                "--label", "therapease.revision=b00-horas-docker", "--publish", $"127.0.0.1:{numero}:8080",
                "--env", "ConnectionStrings__TherapEase=" + conexion, "--env", "TZ=Europe/Madrid", Imagen);
            ExigirExito(arranque, "arranque del contenedor");
            _contenedorCreado = true;
            await PrepararSondaAsync();
            _cliente = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false })
            { BaseAddress = new Uri($"http://127.0.0.1:{numero}"), Timeout = TimeSpan.FromSeconds(4) };
            await EsperarSaludAsync();
        }
        catch
        {
            await DisposeAsync();
            throw;
        }
    }

    /// Crea una sesión válida y tres eventos justo en los bordes del rango de tiempo que se va a consultar.
    private async Task PrepararDatosAsync()
    {
        using var proveedor = Escenario.Servicios();
        using (var alcance = proveedor.CreateScope())
        {
            var resultado = await alcance.ServiceProvider.GetRequiredService<IGestorIdentidad>()
                .CrearAsync(Registro, "horas-ficticias", "Superusuario", "ficticiacontrasenahoras", false);
            if (resultado != ResultadoCreacionDeUsuario.Creado) throw new InvalidOperationException("No se creó el usuario ficticio.");
        }
        using (var solicitud = new SolicitudIdentidad(proveedor))
        {
            var entrada = await solicitud.Obtener<IServicioAcceso>().IniciarSesionAsync("horas-ficticias", "ficticiacontrasenahoras");
            if (!entrada.Exito) throw new InvalidOperationException("No se obtuvo la sesión ficticia.");
            Cookie = solicitud.CookieEmitida();
        }
        await using var conexion = new NpgsqlConnection(Escenario.App);
        await conexion.OpenAsync();
        foreach (var (id, instante) in new[] { (Guid.NewGuid(), "2026-10-25T16:59:59Z"),
            (EventoIncluido, "2026-10-25T17:00:00Z"), (Guid.NewGuid(), "2026-10-25T18:00:00Z") })
        {
            await using var comando = new NpgsqlCommand("""
                INSERT INTO "EventoAuditoria" ("IdEvento","FechaEvento","IdUsuarioActor","TipoRegistro","IdRegistro","Accion","CamposAfectados","Hecho")
                VALUES (@id,@fecha,@actor,'Paciente',@registro,'Alta',ARRAY['Nombre'],'Evento ficticio de horas')
                """, conexion);
            comando.Parameters.AddWithValue("id", id);
            comando.Parameters.AddWithValue("fecha", DateTimeOffset.Parse(instante).ToUniversalTime());
            comando.Parameters.AddWithValue("actor", Registro);
            comando.Parameters.AddWithValue("registro", Registro);
            await comando.ExecuteNonQueryAsync();
        }
    }


    // ──── SONDA DE LA HORA EN LA IMAGEN FINAL ───────────────────────────────────────────────


    /// Arma un pequeño programa temporal de prueba que usa el código ya compilado de Domain, sin agregar paquetes nuevos.
    private async Task PrepararSondaAsync()
    {
        var carpeta = Path.Combine(Evidencia, "sonda");
        Directory.CreateDirectory(carpeta);
        var dominio = Path.Combine(carpeta, "TherapEase.Domain.dll");
        ExigirExito(await DockerAsync("cp", _nombre + ":/app/TherapEase.Domain.dll", dominio), "lectura de Domain desde la imagen final");
        var referencia = System.Security.SecurityElement.Escape(dominio);
        await File.WriteAllTextAsync(Path.Combine(carpeta, "SondaHora.csproj"), $"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings></PropertyGroup>
              <ItemGroup><Reference Include="TherapEase.Domain"><HintPath>{referencia}</HintPath></Reference></ItemGroup>
            </Project>
            """);
        await File.WriteAllTextAsync(Path.Combine(carpeta, "Program.cs"), """
            using System.Globalization;
            using System.Text.Json;
            using TherapEase.Domain.Compartido.Reglas;

            /// <summary>
            /// Consulta la regla real de Hermosillo desde la imagen final para compararla entre zonas.
            /// </summary>
            public static class SondaHora
            {


                // ──── CONVERSIONES FICTICIAS ────────────────────────────────────────────────────


                /// Devuelve horas conocidas, incluyendo cambios horarios externos y cambios de fecha.
                public static void Main()
                {
                    var ejemplos = new[] { "2026-01-01T00:15:00", "2026-03-08T02:30:00", "2026-03-29T02:30:00",
                        "2026-10-25T02:30:00", "2026-11-01T01:30:00", "2026-12-31T23:45:00" };
                    var conversiones = ejemplos.Select(valor =>
                    {
                        var local = DateTime.ParseExact(valor, "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
                        var instante = HoraHermosillo.AInstante(DateOnly.FromDateTime(local), TimeOnly.FromDateTime(local));
                        var regreso = HoraHermosillo.ALocal(instante);
                        return new { Entrada = valor, Instante = instante.ToString("O"), Regreso = regreso.ToString("yyyy-MM-ddTHH:mm:ss"), TipoFecha = regreso.Kind.ToString() };
                    });
                    Console.WriteLine(JsonSerializer.Serialize(new { ZonaProceso = TimeZoneInfo.Local.Id,
                        ZonaHermosillo = TimeZoneInfo.FindSystemTimeZoneById(HoraHermosillo.IdZona).Id, Conversiones = conversiones }));
                }
            }
            """);
        var resultado = await EjecutarAsync("dotnet", ["build", Path.Combine(carpeta, "SondaHora.csproj"),
            "--configuration", "Release", "--output", Path.Combine(carpeta, "salida"), "--ignore-failed-sources"], carpeta);
        ExigirExito(resultado, "compilación de la consola temporal de QA");
    }

    /// Ejecuta esa misma regla ya compilada bajo otra zona horaria; aquí no interviene la base de datos.
    public async Task<JsonDocument> ConsultarHoraAsync(string zona)
    {
        var salida = Path.Combine(Evidencia, "sonda", "salida");
        var resultado = await DockerAsync("run", "--rm", "--label", "therapease.revision=b00-horas-docker",
            "--entrypoint", "dotnet", "--mount", $"type=bind,source={salida},target=/revision,readonly",
            "--env", "TZ=" + zona, Imagen, "/revision/SondaHora.dll");
        ExigirExito(resultado, "consulta de Hermosillo dentro de la imagen final");
        await File.WriteAllTextAsync(Path.Combine(Evidencia, "hora-" + zona.Replace('/', '-') + ".json"), resultado.Salida);
        return JsonDocument.Parse(resultado.Salida);
    }


    // ──── SOLICITUDES Y REINICIO ────────────────────────────────────────────────────────────


    /// Entrega otra base de datos lista para guardar citas, sin mezclarlas con los eventos del servidor.
    public Task<Escenario> CrearEscenarioAsync() => _postgres.Crear();

    /// Llama a la aplicación real; usar la cookie a mano aquí no demuestra que funcionen HTTPS ni un navegador real.
    public async Task<(HttpStatusCode Estado, string Cuerpo)> ConsultarAsync(string ruta, bool conSesion = true)
    {
        using var solicitud = new HttpRequestMessage(HttpMethod.Get, ruta);
        if (conSesion) solicitud.Headers.Add("Cookie", Cookie);
        using var respuesta = await _cliente!.SendAsync(solicitud);
        return (respuesta.StatusCode, await respuesta.Content.ReadAsStringAsync());
    }

    /// Espera a que la aplicación arranque; /salud solo indica que el proceso responde.
    public async Task EsperarSaludAsync()
    {
        var espera = Stopwatch.StartNew();
        while (espera.Elapsed < TimeSpan.FromSeconds(40))
        {
            try { if ((await ConsultarAsync("/salud", false)).Estado == HttpStatusCode.OK) return; }
            catch (HttpRequestException) { }
            catch (TaskCanceledException) { }
            await Task.Delay(200);
        }
        throw new InvalidOperationException("El contenedor no respondió a /salud en el plazo de QA.");
    }

    /// Reinicia el proceso del contenedor y espera a que vuelva a responder.
    public async Task ReiniciarAsync()
    {
        ExigirExito(await DockerAsync("restart", _nombre), "reinicio del contenedor");
        await EsperarSaludAsync();
    }

    /// Lee solo el dato pedido, sin mostrar las variables que contienen la conexión.
    public async Task<string> InspeccionarAsync(string formato)
    {
        var resultado = await DockerAsync("inspect", "--format", formato, _nombre);
        ExigirExito(resultado, "consulta acotada del contenedor");
        return resultado.Salida.Trim();
    }

    /// Revisa dentro de Linux qué usuario se usa y los lugares donde suelen quedar las claves de sesión.
    public async Task<string> DentroAsync(params string[] argumentos)
    {
        var resultado = await DockerAsync(["exec", _nombre, .. argumentos]);
        ExigirExito(resultado, "comprobación dentro del contenedor");
        return resultado.Salida.Trim();
    }


    // ──── PROCESOS Y CIERRE ─────────────────────────────────────────────────────────────────


    private static Task<SalidaProceso> DockerAsync(params string[] argumentos) => EjecutarAsync("docker", argumentos);

    /// Pasa los argumentos por separado y guarda la salida normal y la de errores; nunca escribe comandos con contraseñas.
    private static async Task<SalidaProceso> EjecutarAsync(string ejecutable, string[] argumentos, string? carpeta = null)
    {
        var inicio = new ProcessStartInfo(ejecutable) { UseShellExecute = false, RedirectStandardOutput = true,
            RedirectStandardError = true, CreateNoWindow = true, WorkingDirectory = carpeta ?? ServidorRevision.RaizRepositorio() };
        foreach (var argumento in argumentos) inicio.ArgumentList.Add(argumento);
        using var proceso = Process.Start(inicio) ?? throw new InvalidOperationException("No arrancó " + ejecutable);
        var salida = proceso.StandardOutput.ReadToEndAsync();
        var error = proceso.StandardError.ReadToEndAsync();
        using var limite = new CancellationTokenSource(TimeSpan.FromMinutes(12));
        try { await proceso.WaitForExitAsync(limite.Token); }
        catch { if (!proceso.HasExited) proceso.Kill(entireProcessTree: true); throw; }
        return new SalidaProceso(proceso.ExitCode, await salida, await error);
    }

    /// Indica qué paso falló, sin mostrar cookies ni datos de conexión.
    private static void ExigirExito(SalidaProceso resultado, string fase)
    {
        if (resultado.Codigo != 0) throw new InvalidOperationException($"Falló {fase} (código {resultado.Codigo}).");
    }

    /// Elimina los contenedores y la imagen de esta ejecución; conserva su identificador y la evidencia para revisar.
    public async Task DisposeAsync()
    {
        _cliente?.Dispose();
        try
        {
            if (_contenedorCreado)
                ExigirExito(await DockerAsync("rm", "--force", _nombre), "retiro del contenedor ficticio");
        }
        finally
        {
            _contenedorCreado = false;
            try
            {
                if (_imagenCreada)
                {
                    ExigirExito(await DockerAsync("rmi", Imagen), "retiro de la imagen de prueba");
                    _imagenCreada = false;
                }
            }
            finally
            {
                await _postgres.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Resultado privado de un comando de QA, sin imprimir argumentos sensibles.
    /// </summary>
    private sealed record SalidaProceso(int Codigo, string Salida, string Error);
}
