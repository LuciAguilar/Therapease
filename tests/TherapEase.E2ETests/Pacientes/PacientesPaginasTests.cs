using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using TherapEase.Domain.Auditoria.Entidades;
using TherapEase.Domain.Compartido.Entidades.Enums;
using TherapEase.Domain.Pacientes.Entidades;
using TherapEase.Domain.Pacientes.Entidades.Enums;
using TherapEase.Domain.Pacientes.Reglas;

namespace TherapEase.E2ETests;

/// <summary>
/// Comprueba las páginas reales de pacientes con cuentas y datos ficticios.
/// Reutiliza HTTPS, PostgreSQL y las comprobaciones que protegen la evidencia de navegador.
/// </summary>
public sealed partial class AccesoSesionesYPermisosTests
{


    // ──── PREPARACIÓN COMPARTIDA ───────────────────────────────────────────


    /// Guarda una ficha ficticia para comprobar lectura y edición desde las páginas.
    private async Task<Guid> PrepararPaciente(string nombre, bool sinNacimiento = false, bool baja = false)
    {
        await using var db = entorno.Servidor.Escenario.Contexto();
        var id = Guid.NewGuid();
        db.Pacientes.Add(ReglasDePaciente.Registrar(id,
            new DatosDePaciente(nombre, new DateOnly(1990, 5, 8)),
            [AmbitoAtencion.Escolar], Guid.NewGuid(), DateTimeOffset.UtcNow));
        await db.SaveChangesAsync();
        if (sinNacimiento)
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE \"Paciente\" SET \"FechaNacimiento\"=NULL WHERE \"IdPaciente\"={id}");
        if (baja)
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE \"Paciente\" SET \"Condicion\"='Baja', \"FechaBaja\"={DateTimeOffset.UtcNow}, \"IdUsuarioBaja\"={Guid.NewGuid()} WHERE \"IdPaciente\"={id}");
        return id;
    }

    /// Espera la respuesta de la búsqueda para no confundir resultados anteriores con los nuevos.
    private async Task FiltrarPacientes(IPage p, string texto)
    {
        _paso = "filtrar pacientes";
        await p.RunAndWaitForResponseAsync(
            () => p.Locator("[name='Texto']").First.FillAsync(texto),
            r => new Uri(r.Url).AbsolutePath == "/Pacientes" && r.Request.Method == "POST");
        await Assertions.Expect(p.Locator("#estado-busqueda-pacientes")).ToContainTextAsync("Resultados actualizados.");
    }

    /// Completa una solicitud válida sin enviar actor, estado, edad ni versión del alta.
    private static Dictionary<string, string> AltaPaciente(Guid id, string nombre) => new()
    {
        ["IdSolicitud"] = id.ToString(), ["Entrada.Nombre"] = nombre,
        ["Entrada.FechaNacimiento"] = "1990-05-08", ["Entrada.Escolar"] = "true"
    };


    // ──── ACCESO Y LISTADO ─────────────────────────────────────────────────


    /// Las cuatro páginas de pacientes exigen iniciar sesión antes de mostrar información.
    [Theory]
    [InlineData("/Pacientes")]
    [InlineData("/Pacientes/Crear")]
    [InlineData("/Pacientes/Detalle/00000000-0000-0000-0000-000000000001")]
    [InlineData("/Pacientes/Editar/00000000-0000-0000-0000-000000000001")]
    public Task PacientesSinSesionRedirigeAlAcceso(string ruta) => Verificar(async () =>
    {
        // Arrange
        var p = await Pagina();
        // Act
        await Abrir(p, ruta);
        // Assert
        Exigir(Ruta(p) == "/Acceso");
    });

    /// Una contraseña temporal no permite abrir las páginas de pacientes ni recuperar su contenido.
    [Theory]
    [InlineData("/Pacientes")]
    [InlineData("/Pacientes/Crear")]
    [InlineData("/Pacientes/Detalle/00000000-0000-0000-0000-000000000001")]
    [InlineData("/Pacientes/Editar/00000000-0000-0000-0000-000000000001")]
    public Task PacientesConTemporalExigeCambiarLaContrasena(string ruta) => Verificar(async () =>
    {
        // Arrange
        var p = await Pagina();
        // Act
        await Entrar(p, await entorno.CrearCuentaAsync(temporal: true));
        await Abrir(p, ruta);
        // Assert
        Exigir(Ruta(p) == "/Cuenta/Contrasena");
    });

    /// Ambos roles pueden listar seis fichas por página, filtrar y abrir una ficha real.
    [Theory]
    [InlineData("Usuaria")]
    [InlineData("Superusuario")]
    public Task PacientesPaginadosConFiltroYPanelRespetanLosDosRoles(string rol) => Verificar(async () =>
    {
        // Arrange
        var p = await Pagina();
        var prefijo = "Listado " + Guid.NewGuid().ToString("N")[..8];
        for (var i = 1; i <= 14; i++) await PrepararPaciente($"{prefijo} {i:00}");
        // Act
        await Entrar(p, await entorno.CrearCuentaAsync(rol));
        await Abrir(p, "/Pacientes");
        // Assert
        await Assertions.Expect(p.Locator(".tarjeta-paciente")).ToHaveCountAsync(6);
        await FiltrarPacientes(p, prefijo);
        await Assertions.Expect(p.Locator(".paginacion-resumen")).ToContainTextAsync("Página 1 de 3");
        await p.RunAndWaitForResponseAsync(() => Boton(p, "Siguiente").ClickAsync(), r => r.Request.Method == "POST");
        await Assertions.Expect(p.Locator(".paginacion-resumen")).ToContainTextAsync("Página 2 de 3");
        await Assertions.Expect(p.Locator(".tarjeta-paciente").First).ToContainTextAsync(prefijo + " 07");
        await FiltrarPacientes(p, prefijo.ToUpperInvariant());
        await Assertions.Expect(p.Locator(".paginacion-resumen")).ToContainTextAsync("Página 1 de 3");
        await FiltrarPacientes(p, prefijo + " 01");
        await Assertions.Expect(p.Locator(".tarjeta-paciente")).ToHaveCountAsync(1);
        await p.Locator("a[data-abrir-ficha]").ClickAsync();
        await Assertions.Expect(p.Locator("#panel-contenido-ficha")).ToContainTextAsync(prefijo + " 01");
        await Boton(p, "Cerrar ficha").ClickAsync();
        Exigir(!await p.Locator("#panel-ficha-paciente").IsVisibleAsync());
    });


    // ──── REGISTRO Y GUARDADO ──────────────────────────────────────────────


    /// El rechazo por tutor conserva la captura; al completarlo guarda una ficha y abre su panel.
    [Fact]
    public Task AltaDeMenorConservaCapturaYConfirmaSoloAlCompletarTutor() => Verificar(async () =>
    {
        // Arrange
        var p = await Pagina();
        await Entrar(p, await entorno.CrearCuentaAsync());
        await Abrir(p, "/Pacientes/Crear");
        var nombre = "Menor " + Guid.NewGuid().ToString("N");
        await p.GetByLabel("Nombre completo", new() { Exact = true }).FillAsync(nombre);
        await p.GetByLabel("Fecha de nacimiento", new() { Exact = true }).FillAsync("2015-05-08");
        await p.GetByLabel("Escolar", new() { Exact = true }).CheckAsync();
        var id = await p.Locator("[name='IdSolicitud']").InputValueAsync();
        // Act
        await Boton(p, "Registrar paciente").ClickAsync();
        // Assert
        await Assertions.Expect(p.GetByRole(AriaRole.Alert)).ToContainTextAsync("se necesita un tutor");
        Exigir(await p.Locator("[name='IdSolicitud']").InputValueAsync() == id);
        Exigir(await p.GetByLabel("Nombre completo", new() { Exact = true }).InputValueAsync() == nombre);
        await p.GetByLabel("Nombre del tutor", new() { Exact = true }).FillAsync("Tutor ficticio");
        await p.GetByLabel("Parentesco", new() { Exact = true }).FillAsync("Madre");
        await p.GetByLabel("Teléfono del tutor", new() { Exact = true }).FillAsync("6620000000");
        await Task.WhenAll(p.WaitForURLAsync(u => new Uri(u).AbsolutePath == "/Pacientes"), Boton(p, "Registrar paciente").ClickAsync());
        await Assertions.Expect(p.Locator("#panel-contenido-ficha")).ToContainTextAsync("Paciente registrado.");
        await Assertions.Expect(p.Locator("#panel-contenido-ficha")).ToContainTextAsync("Tutor ficticio");
        await using var db = entorno.Servidor.Escenario.Contexto();
        Exigir(await db.Pacientes.CountAsync(x => x.IdPaciente == Guid.Parse(id)) == 1);
        Exigir(await db.Set<EventoAuditoria>().CountAsync(x => x.IdRegistro == Guid.Parse(id)) == 1);
    });

    /// El reenvío tras una respuesta no seguida conserva una sola ficha y un solo evento.
    [Fact]
    public Task ReenvioHttpDeAltaNoDuplicaLaFichaNiSuAuditoria() => Verificar(async () =>
    {
        // Arrange
        var p = await Pagina();
        var cuenta = await entorno.CrearCuentaAsync();
        await Entrar(p, cuenta);
        await Abrir(p, "/Pacientes/Crear");
        var id = Guid.Parse(await p.Locator("[name='IdSolicitud']").InputValueAsync());
        var datos = AltaPaciente(id, "Reenvio " + Guid.NewGuid().ToString("N"));
        datos["Entrada.Edad"] = "99";
        datos["Entrada.Version"] = "999";
        datos["Entrada.Condicion"] = "Baja";
        datos["Entrada.IdUsuarioAlta"] = Guid.NewGuid().ToString();
        // Act
        var primera = await Enviar(p, "/Pacientes/Crear", datos);
        var repetida = await Enviar(p, "/Pacientes/Crear", datos);
        // Assert
        Exigir(primera.Status == 302 && repetida.Status == 302);
        await using var db = entorno.Servidor.Escenario.Contexto();
        Exigir(await db.Pacientes.CountAsync(x => x.IdPaciente == id) == 1);
        Exigir(await db.Set<EventoAuditoria>().CountAsync(x => x.IdRegistro == id) == 1);
        var ficha = await db.Pacientes.SingleAsync(x => x.IdPaciente == id);
        Exigir(ficha.Version == 1 && ficha.Condicion == CondicionRegistro.Vigente);
        Exigir((await db.Set<EventoAuditoria>().SingleAsync(x => x.IdRegistro == id)).IdUsuarioActor == cuenta.Id);
    });

    /// La página no guarda fechas mal formadas, solicitudes sin número ni cambios sin versión.
    [Theory]
    [InlineData("fecha")]
    [InlineData("solicitud")]
    [InlineData("version")]
    public Task FormulariosManipuladosNoGuardanUnaFicha(string defecto) => Verificar(async () =>
    {
        // Arrange
        var p = await Pagina();
        await Entrar(p, await entorno.CrearCuentaAsync());
        await Abrir(p, "/Pacientes/Crear");
        var nombreOriginal = "Entrada " + Guid.NewGuid().ToString("N");
        var id = defecto == "version" ? await PrepararPaciente(nombreOriginal) : Guid.NewGuid();
        var datos = AltaPaciente(id, "Cambio " + Guid.NewGuid().ToString("N"));
        if (defecto == "fecha") datos["Entrada.FechaNacimiento"] = "no-es-fecha";
        if (defecto == "solicitud") datos.Remove("IdSolicitud");
        // Act
        var r = await Enviar(p, defecto == "version" ? "/Pacientes/Editar/" + id : "/Pacientes/Crear", datos);
        // Assert
        Exigir(r.Status == 200 && System.Net.WebUtility.HtmlDecode(await r.TextAsync()).Contains("La solicitud no es válida."));
        await using var db = entorno.Servidor.Escenario.Contexto();
        if (defecto == "version")
        {
            var ficha = await db.Pacientes.SingleAsync(x => x.IdPaciente == id);
            Exigir(ficha.Nombre == nombreOriginal && ficha.Version == 1);
            Exigir(!await db.Set<EventoAuditoria>().AnyAsync(x => x.IdRegistro == id));
        }
        else
        {
            Exigir(!await db.Pacientes.AnyAsync(x => x.IdPaciente == id));
        }
    });

    /// El servidor rechaza un alta sin la protección del formulario, aunque tenga sesión válida.
    [Fact]
    public Task AltaSinAntifalsificacionNoLlegaAlGuardado() => Verificar(async () =>
    {
        // Arrange
        var p = await Pagina();
        await Entrar(p, await entorno.CrearCuentaAsync());
        await Abrir(p, "/Pacientes/Crear");
        var id = Guid.NewGuid();
        // Act
        var r = await Enviar(p, "/Pacientes/Crear", AltaPaciente(id, "Proteccion ficticia"), protegido: false);
        // Assert
        Exigir(r.Status == 400);
        await using var db = entorno.Servidor.Escenario.Contexto();
        Exigir(!await db.Pacientes.AnyAsync(x => x.IdPaciente == id));
    });


    // ──── CONSULTA, EDICIÓN Y SALIDA SEGURA ─────────────────────────────────


    /// Una baja queda fuera del filtro y su apertura solo informa la condición, sin datos de la ficha.
    [Fact]
    public Task UnaBajaNoApareceYNoRevelaSuNombreAlAbrirla() => Verificar(async () =>
    {
        // Arrange
        var nombre = "Baja " + Guid.NewGuid().ToString("N");
        var id = await PrepararPaciente(nombre, baja: true);
        var p = await Pagina();
        await Entrar(p, await entorno.CrearCuentaAsync());
        // Act
        await Abrir(p, "/Pacientes");
        await FiltrarPacientes(p, nombre);
        // Assert
        await Assertions.Expect(p.Locator("#resultados-pacientes")).ToContainTextAsync("No se encontraron pacientes");
        await Abrir(p, "/Pacientes/Detalle/" + id);
        var contenido = await p.Locator("#contenido-ficha-paciente").TextContentAsync();
        Exigir(contenido!.Contains("Está dada de baja.") && !contenido.Contains(nombre));
    });

    /// Dos fichas homónimas muestran el nacimiento conocido y avisan si la otra fecha es desconocida.
    [Fact]
    public Task HomonimosConNacimientoAnteriorDesconocidoSeDistinguenEnElListado() => Verificar(async () =>
    {
        // Arrange
        var nombre = "Homonimo " + Guid.NewGuid().ToString("N");
        await PrepararPaciente(nombre);
        await PrepararPaciente(nombre, sinNacimiento: true);
        var p = await Pagina();
        await Entrar(p, await entorno.CrearCuentaAsync());
        await Abrir(p, "/Pacientes");
        // Act
        await FiltrarPacientes(p, nombre);
        // Assert
        await Assertions.Expect(p.Locator(".tarjeta-paciente")).ToHaveCountAsync(2);
        await Assertions.Expect(p.Locator("#resultados-pacientes")).ToContainTextAsync("Nacimiento: 08/05/1990");
        await Assertions.Expect(p.Locator("#resultados-pacientes")).ToContainTextAsync("Fecha de nacimiento desconocida.");
    });

    /// El conflicto conserva la captura y la versión anteriores, sin sobrescribir la edición ya confirmada.
    [Fact]
    public Task EdicionConVersionAnteriorConservaLaCapturaYLaFichaVigente() => Verificar(async () =>
    {
        // Arrange
        var id = await PrepararPaciente("Edicion " + Guid.NewGuid().ToString("N"));
        var p = await Pagina();
        await Entrar(p, await entorno.CrearCuentaAsync());
        await Abrir(p, "/Pacientes/Editar/" + id);
        var datos = AltaPaciente(id, "Cambio confirmado");
        datos["VersionEsperada"] = "1";
        // Act
        Exigir((await Enviar(p, "/Pacientes/Editar/" + id, datos)).Status == 302);
        await p.GetByLabel("Nombre completo", new() { Exact = true }).FillAsync("Mi captura pendiente");
        await Boton(p, "Guardar cambios").ClickAsync();
        // Assert
        await Assertions.Expect(p.GetByRole(AriaRole.Alert)).ToContainTextAsync("El registro cambió");
        Exigir(await p.GetByLabel("Nombre completo", new() { Exact = true }).InputValueAsync() == "Mi captura pendiente");
        Exigir(await p.Locator("[name='VersionEsperada']").InputValueAsync() == "1");
        await using var db = entorno.Servidor.Escenario.Contexto();
        var actual = await db.Pacientes.SingleAsync(x => x.IdPaciente == id);
        Exigir(actual.Nombre == "Cambio confirmado" && actual.Version == 2);
        Exigir(await db.Set<EventoAuditoria>().CountAsync(x => x.IdRegistro == id) == 1);
    });

    /// El contenido de escuela se muestra como texto y no ejecuta código en el panel de la ficha.
    [Fact]
    public Task FichaCodificaLosDatosAlMostrarlosEnElPanel() => Verificar(async () =>
    {
        // Arrange
        var p = await Pagina();
        await Entrar(p, await entorno.CrearCuentaAsync());
        await Abrir(p, "/Pacientes/Crear");
        var id = Guid.NewGuid();
        var datos = AltaPaciente(id, "Salida " + Guid.NewGuid().ToString("N"));
        var valor = "<script>globalThis.__qaXss=1</script>";
        datos["Entrada.Escuela"] = valor;
        // Act
        Exigir((await Enviar(p, "/Pacientes/Crear", datos)).Status == 302);
        await Abrir(p, "/Pacientes?ficha=" + id);
        // Assert
        await Assertions.Expect(p.Locator("#panel-contenido-ficha")).ToContainTextAsync(valor);
        Exigir(await p.EvaluateAsync<int>("() => globalThis.__qaXss || 0") == 0);
        Exigir(await p.Locator("#panel-contenido-ficha script").CountAsync() == 0);
    });

    /// Un homónimo requiere confirmación expresa y conserva el número del alta hasta guardar.
    [Fact]
    public Task ConfirmarHomonimoConservaElNumeroYRegistraOtraPersona() => Verificar(async () =>
    {
        // Arrange
        var nombre = "Confirmacion " + Guid.NewGuid().ToString("N");
        await PrepararPaciente(nombre);
        var p = await Pagina();
        await Entrar(p, await entorno.CrearCuentaAsync());
        await Abrir(p, "/Pacientes/Crear");
        var id = Guid.Parse(await p.Locator("[name='IdSolicitud']").InputValueAsync());
        await p.GetByLabel("Nombre completo", new() { Exact = true }).FillAsync(nombre);
        await p.GetByLabel("Fecha de nacimiento", new() { Exact = true }).FillAsync("1991-05-08");
        await p.GetByLabel("Escolar", new() { Exact = true }).CheckAsync();
        // Act
        await Boton(p, "Registrar paciente").ClickAsync();
        // Assert
        await Assertions.Expect(p.GetByRole(AriaRole.Heading, new() { Name = "Fichas con el mismo nombre" })).ToBeVisibleAsync();
        Exigir(await p.Locator("[name='IdSolicitud']").InputValueAsync() == id.ToString());
        await using (var antes = entorno.Servidor.Escenario.Contexto())
            Exigir(!await antes.Pacientes.AnyAsync(x => x.IdPaciente == id));
        await Task.WhenAll(p.WaitForURLAsync(u => new Uri(u).AbsolutePath == "/Pacientes"), Boton(p, "Confirmar que es otra persona").ClickAsync());
        await Assertions.Expect(p.Locator("#panel-contenido-ficha")).ToContainTextAsync("Paciente registrado.");
        await using var db = entorno.Servidor.Escenario.Contexto();
        Exigir(await db.Pacientes.CountAsync(x => x.Nombre == nombre) == 2);
        Exigir(await db.Set<EventoAuditoria>().CountAsync(x => x.IdRegistro == id) == 1);
    });

    /// Guardar una ficha sin cambios muestra la confirmación correspondiente y no crea otro evento.
    [Fact]
    public Task EdicionSinCambiosNoEscribeNiIncrementaVersion() => Verificar(async () =>
    {
        // Arrange
        var id = await PrepararPaciente("Sin cambios " + Guid.NewGuid().ToString("N"));
        var p = await Pagina();
        await Entrar(p, await entorno.CrearCuentaAsync());
        await Abrir(p, "/Pacientes/Editar/" + id);
        // Act
        await Task.WhenAll(p.WaitForURLAsync(u => new Uri(u).AbsolutePath == "/Pacientes"), Boton(p, "Guardar cambios").ClickAsync());
        // Assert
        await Assertions.Expect(p.Locator("#panel-contenido-ficha")).ToContainTextAsync("No hubo cambios.");
        await using var db = entorno.Servidor.Escenario.Contexto();
        Exigir((await db.Pacientes.SingleAsync(x => x.IdPaciente == id)).Version == 1);
        Exigir(!await db.Set<EventoAuditoria>().AnyAsync(x => x.IdRegistro == id));
    });

    /// Un fallo real de auditoría devuelve el error general sin guardar; el mismo envío puede recuperarse después.
    [Fact]
    public Task ErrorDeAuditoriaHttpNoConfirmaYElReenvioPosteriorEsSeguro() => Verificar(async () =>
    {
        // Arrange
        var p = await Pagina();
        await Entrar(p, await entorno.CrearCuentaAsync());
        await Abrir(p, "/Pacientes/Crear");
        var id = Guid.NewGuid();
        var datos = AltaPaciente(id, "Fallo controlado " + Guid.NewGuid().ToString("N"));
        await TherapEase.IntegrationTests.PostgreSqlRevision.Sql(entorno.Servidor.Escenario.Migrador,
            $"CREATE FUNCTION rechazar_alta_paciente_revision() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN IF NEW.\"IdRegistro\"='{id}'::uuid THEN RAISE EXCEPTION 'Fallo controlado de auditoria'; END IF; RETURN NEW; END; $$; CREATE TRIGGER rechazar_alta_paciente_revision BEFORE INSERT ON \"EventoAuditoria\" FOR EACH ROW EXECUTE FUNCTION rechazar_alta_paciente_revision();");
        // Act
        try
        {
            var respuesta = await Enviar(p, "/Pacientes/Crear", datos);

            // Assert
            Exigir(respuesta.Status == 500);
            Exigir(System.Net.WebUtility.HtmlDecode(await respuesta.TextAsync()).Contains("Ocurrió un error."));
            await using var antes = entorno.Servidor.Escenario.Contexto();
            Exigir(!await antes.Pacientes.AnyAsync(x => x.IdPaciente == id));
            Exigir(!await antes.Set<EventoAuditoria>().AnyAsync(x => x.IdRegistro == id));
        }
        finally
        {
            await TherapEase.IntegrationTests.PostgreSqlRevision.Sql(entorno.Servidor.Escenario.Migrador,
                "DROP TRIGGER rechazar_alta_paciente_revision ON \"EventoAuditoria\"; DROP FUNCTION rechazar_alta_paciente_revision();");
        }
        Exigir((await Enviar(p, "/Pacientes/Crear", datos)).Status == 302);
        await using var db = entorno.Servidor.Escenario.Contexto();
        Exigir(await db.Pacientes.CountAsync(x => x.IdPaciente == id) == 1);
        Exigir(await db.Set<EventoAuditoria>().CountAsync(x => x.IdRegistro == id) == 1);
    });
}
