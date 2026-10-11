using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using TherapEase.Application.Identidad.Interfaces.Repositorios;
using TherapEase.Application.Identidad.Interfaces.Servicios;
using TherapEase.Application.Identidad.Modelos;
using TherapEase.Domain.Pacientes.Entidades.Enums;
using TherapEase.Domain.Pacientes.Reglas;
using Xunit.Abstractions;

namespace TherapEase.IntegrationTests;

/// <summary>
/// Comprueba recuperación Q10–Q11 con datos ficticios y PostgreSQL desechable.
/// Revisa restauración completa, rechazo de respaldos alterados y fallos sin éxito aparente.
/// </summary>
public class RecuperacionTests(PostgreSqlRevision postgres, ITestOutputHelper salida) : IClassFixture<PostgreSqlRevision>
{


    // ──── PÉRDIDA Y RESTAURACIÓN COMPLETA ────


    /// Recupera todas las tablas en otro contenedor después de perder acceso al origen; compara y mide hasta una consulta autorizada real.
    [Fact]
    public async Task Respaldo_Cifrado_Restaura_En_Otro_Contenedor_Datos_Permisos_Identidad_Y_Tiempos()
    {

        // Arrange: origen con migraciones, pacientes, citas, auditoría, usuario y claves de sesión.
        var origen = new PostgreSqlRevision();
        var destino = new PostgreSqlRevision();
        var clave = RandomNumberGenerator.GetBytes(32);
        var carpeta = Path.Combine(Path.GetTempPath(), "therapease-recuperacion-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(carpeta);
        try
        {
            await origen.InitializeAsync();
            var anterior = await origen.Crear();
            var idPaciente = await PrepararAsync(anterior);
            string cookie;
            using (var proveedor = anterior.Servicios())
            using (var solicitud = new SolicitudIdentidad(proveedor))
            {
                Assert.True((await solicitud.Obtener<IServicioAcceso>().IniciarSesionAsync("super-recuperacion-ficticio", "contrasenaficticiarecuperacion")).Exito);
                cookie = solicitud.CookieEmitida();
            }
            var esperado = await RecuperacionRevision.CompararDatosAsync(anterior.Migrador);
            Assert.Equal(13, esperado.Count);
            var fechaCopia = DateTimeOffset.UtcNow;
            var duracionRespaldo = Stopwatch.StartNew();
            var volcado = await RecuperacionRevision.VolcarAsync(origen.Contenedor, anterior);
            RespaldoRevision respaldo;
            try { respaldo = RecuperacionRevision.Cifrar(volcado, clave, fechaCopia); }
            finally { CryptographicOperations.ZeroMemory(volcado); }
            var archivo = Path.Combine(carpeta, "respaldo.aes");
            await File.WriteAllBytesAsync(archivo, respaldo.Contenido);
            await File.WriteAllTextAsync(Path.Combine(carpeta, "respaldo.sha256"), Convert.ToHexString(respaldo.Huella));
            duracionRespaldo.Stop();

            // Un cambio posterior no existe en el respaldo: permite medir la pérdida simulada.

            await PostgreSqlRevision.Sql(anterior.App, $"UPDATE \"Paciente\" SET \"Nombre\"='Cambio posterior ficticio' WHERE \"IdPaciente\"='{idPaciente}'");
            var detectadoEn = DateTimeOffset.UtcNow;
            var recuperacion = Stopwatch.StartNew();

            // Act: el origen queda inaccesible y un contenedor nuevo recibe solo el respaldo cifrado.
            await origen.Contenedor.StopAsync();
            var sinOrigen = new NpgsqlConnectionStringBuilder(anterior.App) { Timeout = 1, Pooling = false }.ConnectionString;
            await Assert.ThrowsAnyAsync<NpgsqlException>(() => PostgreSqlRevision.Valor<long>(sinOrigen, "SELECT 1"));
            await destino.InitializeAsync();
            Assert.NotEqual(origen.Contenedor.Id, destino.Contenedor.Id);
            var nuevo = await RecuperacionRevision.CrearVaciaAsync(destino);
            Assert.Equal(0L, await PostgreSqlRevision.Valor<long>(nuevo.Migrador, "SELECT count(*) FROM pg_tables WHERE schemaname='public'"));
            var leido = new RespaldoRevision(await File.ReadAllBytesAsync(archivo), Convert.FromHexString(await File.ReadAllTextAsync(Path.Combine(carpeta, "respaldo.sha256"))));
            await RecuperacionRevision.RestaurarAsync(destino.Contenedor, nuevo, leido, clave);

            // Assert: todas las filas coinciden, incluidos permisos de Identity, auditoría y claves.
            Assert.Equal(esperado.ToArray(), (await RecuperacionRevision.CompararDatosAsync(nuevo.Migrador)).ToArray());
            Assert.Equal("Paciente ficticio recuperable", await PostgreSqlRevision.Valor<string>(nuevo.App, $"SELECT \"Nombre\" FROM \"Paciente\" WHERE \"IdPaciente\"='{idPaciente}'"));
            using (var proveedor = nuevo.Servicios())
            using (var solicitud = new SolicitudIdentidad(proveedor, cookie))
                Assert.True(await solicitud.AutenticarAsync());
            await using (var servidor = await ServidorRevision.IniciarAsync(nuevo))
                Assert.Equal(200, await servidor.EstadoAsync(cookie, "/api/auditoria/eventos?desde=2026-10-07T00:00:00Z&hasta=2026-10-08T00:00:00Z"));
            recuperacion.Stop();
            var perdidaSimulada = RecuperacionRevision.Antiguedad(leido, detectadoEn);
            Assert.InRange(perdidaSimulada, TimeSpan.Zero, TimeSpan.FromHours(24));
            Assert.InRange(recuperacion.Elapsed, TimeSpan.Zero, TimeSpan.FromHours(24));

            // Arrange: la misma copia íntegra con una antigüedad simulada de 25 horas.
            var datosAnteriores = RecuperacionRevision.Abrir(leido, clave);
            RespaldoRevision copiaAnterior;
            try { copiaAnterior = RecuperacionRevision.Cifrar(datosAnteriores, clave, DateTimeOffset.UtcNow.AddHours(-25)); }
            finally { CryptographicOperations.ZeroMemory(datosAnteriores); }
            var destinoAnterior = await RecuperacionRevision.CrearVaciaAsync(destino);

            // Act: recuperar también la copia antigua en otra base vacía.
            await RecuperacionRevision.RestaurarAsync(destino.Contenedor, destinoAnterior, copiaAnterior, clave);
            var antiguedadAnterior = RecuperacionRevision.Antiguedad(copiaAnterior, DateTimeOffset.UtcNow);
            salida.WriteLine($"Copia de {antiguedadAnterior.TotalHours:F2} horas restaurada; Q10 incumplido. No se bloqueó la recuperación.");

            // Assert: los datos vuelven aunque se incumpla Q10; esta fase queda fuera del tiempo de la copia reciente.
            Assert.True(antiguedadAnterior > TimeSpan.FromHours(24));
            Assert.Equal(esperado.ToArray(), (await RecuperacionRevision.CompararDatosAsync(destinoAnterior.Migrador)).ToArray());

            // Las restricciones siguen activas después de restaurar, sin volver a migrar la base.

            foreach (var sql in new[] { "UPDATE \"EventoAuditoria\" SET \"Hecho\"='No permitido'", "DELETE FROM \"EventoAuditoria\"", "CREATE TABLE public.no_permitida(id int)" })
                Assert.Equal("42501", (await Assert.ThrowsAsync<PostgresException>(() => PostgreSqlRevision.Sql(nuevo.App, sql))).SqlState);
            var nuevoEvento = Guid.NewGuid();
            await PostgreSqlRevision.Sql(nuevo.App, $"INSERT INTO \"EventoAuditoria\" SELECT '{nuevoEvento}',\"FechaEvento\",\"IdUsuarioActor\",\"TipoRegistro\",\"IdRegistro\",\"Accion\",\"CamposAfectados\",\"Hecho\" FROM \"EventoAuditoria\" LIMIT 1");
            Assert.Equal(2L, await PostgreSqlRevision.Valor<long>(nuevo.App, "SELECT count(*) FROM \"EventoAuditoria\""));
            Assert.Equal(2L, await PostgreSqlRevision.Valor<long>(nuevo.Migrador, "SELECT count(*) FROM \"__EFMigrationsHistory\""));
            Assert.Equal("42501", (await Assert.ThrowsAsync<PostgresException>(() => PostgreSqlRevision.Valor<long>(nuevo.App, "SELECT count(*) FROM \"__EFMigrationsHistory\""))).SqlState);
            var version = await destino.Contenedor.ExecAsync(["pg_dump", "--version"]);
            var medicion = JsonSerializer.Serialize(new {
                tablas_comparadas = esperado.Count, filas_iguales = true,
                respaldo_ms = duracionRespaldo.ElapsedMilliseconds,
                recuperacion_desde_deteccion_ms = recuperacion.ElapsedMilliseconds,
                perdida_simulada_ms = perdidaSimulada.TotalMilliseconds,
                detectado_en_utc = detectadoEn, copia_en_utc = fechaCopia,
                bytes_cifrados = respaldo.Contenido.Length,
                huella_sha256 = Convert.ToHexString(respaldo.Huella),
                herramienta = version.Stdout.Trim(), consulta_autorizada_http = 200,
                copia_25h_restaurada = true, copia_25h_cumple_q10 = false,
                limite = "Demostración local pequeña; no acredita cadencia diaria, retención ni operación de nube."
            });
            salida.WriteLine(medicion);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(clave);
            await origen.DisposeAsync();
            await destino.DisposeAsync();
            File.Delete(Path.Combine(carpeta, "respaldo.aes"));
            File.Delete(Path.Combine(carpeta, "respaldo.sha256"));
            Directory.Delete(carpeta);
        }
    }


    // ──── COPIAS ALTERADAS Y CLAVE INCORRECTA ────


    /// Rechaza alteraciones y clave incorrecta antes de crear tablas en el destino, incluso si se recalcula la huella externa.
    [Theory]
    [InlineData("huella")]
    [InlineData("contenido")]
    [InlineData("fecha")]
    [InlineData("clave")]
    [InlineData("truncado")]
    public async Task Copia_Alterada_O_Clave_Incorrecta_No_Toca_El_Destino(string cambio)
    {

        // Arrange: copia cifrada de prueba y una base realmente vacía.
        var destino = await RecuperacionRevision.CrearVaciaAsync(postgres);
        var clave = RandomNumberGenerator.GetBytes(32);
        var otraClave = RandomNumberGenerator.GetBytes(32);
        try
        {
            var copia = RecuperacionRevision.Cifrar("Contenido ficticio de prueba"u8.ToArray(), clave, DateTimeOffset.UtcNow);
            if (cambio == "huella") copia.Huella[0] ^= 1;
            if (cambio == "contenido") copia.Contenido[^1] ^= 1;
            if (cambio == "fecha") copia.Contenido[8] ^= 1;
            if (cambio == "truncado") copia = copia with { Contenido = copia.Contenido[..10] };
            if (cambio is "contenido" or "fecha" or "truncado") copia = copia with { Huella = SHA256.HashData(copia.Contenido) };

            // Act + Assert: ni una huella recalculada sustituye la autenticación del cifrado.
            if (cambio is "huella" or "truncado")
                await Assert.ThrowsAsync<InvalidDataException>(() => RecuperacionRevision.RestaurarAsync(postgres.Contenedor, destino, copia, clave));
            else
                await Assert.ThrowsAsync<AuthenticationTagMismatchException>(() => RecuperacionRevision.RestaurarAsync(postgres.Contenedor, destino, copia, cambio == "clave" ? otraClave : clave));
            Assert.Equal(0L, await PostgreSqlRevision.Valor<long>(destino.Migrador, "SELECT count(*) FROM pg_tables WHERE schemaname='public'"));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(clave);
            CryptographicOperations.ZeroMemory(otraClave);
        }
    }


    // ──── FALLOS DE HERRAMIENTAS ────


    /// Si pg_restore falla después de empezar, su única transacción deshace los objetos nuevos.
    [Fact]
    public async Task Error_De_Restauracion_Revierte_Todo_Y_No_Confirma_Exito()
    {

        // Arrange: respaldo real y una tabla incompatible solo en la base de destino.
        var origen = await postgres.Crear();
        await PrepararAsync(origen);
        var destino = await RecuperacionRevision.CrearVaciaAsync(postgres);
        await PostgreSqlRevision.Sql(destino.Migrador, "CREATE TABLE \"Cita\"(id int); INSERT INTO \"Cita\" VALUES (7)");
        var antes = await RecuperacionRevision.CompararDatosAsync(destino.Migrador);
        var clave = RandomNumberGenerator.GetBytes(32);
        var volcado = await RecuperacionRevision.VolcarAsync(postgres.Contenedor, origen);
        try
        {
            var copia = RecuperacionRevision.Cifrar(volcado, clave, DateTimeOffset.UtcNow);

            // Act + Assert: el error no deja una recuperación parcial ni elimina la tabla previa.
            await Assert.ThrowsAsync<InvalidOperationException>(() => RecuperacionRevision.RestaurarAsync(postgres.Contenedor, destino, copia, clave));
            Assert.Equal(antes.ToArray(), (await RecuperacionRevision.CompararDatosAsync(destino.Migrador)).ToArray());
            Assert.Equal(1L, await PostgreSqlRevision.Valor<long>(destino.Migrador, "SELECT count(*) FROM pg_tables WHERE schemaname='public'"));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(clave);
            CryptographicOperations.ZeroMemory(volcado);
        }
    }

    /// Un pg_dump fallido avisa y retira el archivo temporal; no entrega un respaldo vacío como válido.
    [Fact]
    public async Task Error_De_PgDump_No_Entrega_Respaldo_Y_Limpia_Archivo_Temporal()
    {

        // Arrange: nombre de base que no existe en el contenedor de prueba.
        var inexistente = new NpgsqlConnectionStringBuilder(postgres.Contenedor.GetConnectionString()) { Database = "qa_inexistente_" + Guid.NewGuid().ToString("N") }.ConnectionString;
        var antes = await postgres.Contenedor.ExecAsync(["sh", "-c", "find /tmp -maxdepth 1 -name 'qa_respaldo_*.dump' -print"]);

        // Act + Assert: el fallo se propaga y no quedan archivos nuevos.
        await Assert.ThrowsAsync<InvalidOperationException>(() => RecuperacionRevision.VolcarAsync(postgres.Contenedor, new Escenario(inexistente, inexistente)));
        var despues = await postgres.Contenedor.ExecAsync(["sh", "-c", "find /tmp -maxdepth 1 -name 'qa_respaldo_*.dump' -print"]);
        Assert.Equal(antes.Stdout, despues.Stdout);
    }


    // ──── ANTIGÜEDAD Y CIFRADO ────


    /// Mide la antigüedad y comprueba si cumple Q10; una copia antigua o fecha futura se informa sin impedir restaurar.
    [Theory]
    [InlineData(23, true)]
    [InlineData(24, true)]
    [InlineData(25, false)]
    [InlineData(-1, false)]
    public void Antiguedad_De_Copia_Mide_Cumplimiento_De_Q10(int horas, bool cumpleQ10)
    {

        // Arrange: fechas simuladas; no se espera un día real.
        var detectado = DateTimeOffset.Parse("2026-10-07T12:00:00Z");
        var clave = RandomNumberGenerator.GetBytes(32);
        try
        {
            var copia = RecuperacionRevision.Cifrar("Datos ficticios"u8.ToArray(), clave, detectado.AddHours(-horas));

            // Act: autenticar la fecha antes de medir, incluso si la copia tiene más de 24 horas.
            var abierto = RecuperacionRevision.Abrir(copia, clave);
            CryptographicOperations.ZeroMemory(abierto);
            var antiguedad = RecuperacionRevision.Antiguedad(copia, detectado);
            salida.WriteLine($"Antigüedad simulada: {antiguedad.TotalHours} horas; cumple Q10: {antiguedad >= TimeSpan.Zero && antiguedad <= TimeSpan.FromHours(24)}. La medición no impide restaurar.");

            // Assert: la medición informa el incumplimiento sin lanzar una excepción.
            Assert.Equal(TimeSpan.FromHours(horas), antiguedad);
            Assert.Equal(cumpleQ10, antiguedad >= TimeSpan.Zero && antiguedad <= TimeSpan.FromHours(24));
        }
        finally { CryptographicOperations.ZeroMemory(clave); }
    }

    /// El mismo contenido usa valores aleatorios distintos; ambos archivos se descifran y no guardan la clave.
    [Fact]
    public void Cifrado_Usa_Aleatorio_Nuevo_Y_Recupera_El_Contenido()
    {

        // Arrange: datos ficticios y clave aleatoria solo en memoria.
        var clave = RandomNumberGenerator.GetBytes(32);
        var datos = Encoding.UTF8.GetBytes("Dato rastreable ficticio de recuperación");
        try
        {

            // Act: dos copias del mismo contenido.
            var primera = RecuperacionRevision.Cifrar(datos, clave, DateTimeOffset.UtcNow);
            var segunda = RecuperacionRevision.Cifrar(datos, clave, DateTimeOffset.UtcNow);

            // Assert: aleatorios distintos, huella correcta y contenido recuperable.
            Assert.False(primera.Contenido.AsSpan(16, 12).SequenceEqual(segunda.Contenido.AsSpan(16, 12)));
            Assert.Equal(SHA256.HashData(primera.Contenido), primera.Huella);
            Assert.Equal(datos, RecuperacionRevision.Abrir(primera, clave));
            Assert.Equal(datos, RecuperacionRevision.Abrir(segunda, clave));
        }
        finally { CryptographicOperations.ZeroMemory(clave); }
    }


    // ──── DATOS FICTICIOS ────


    /// Llena las tablas principales y crea un superusuario; no usa datos de personas reales.
    private static async Task<Guid> PrepararAsync(Escenario escenario)
    {
        var actor = Guid.NewGuid();
        using (var proveedor = escenario.Servicios())
        using (var alcance = proveedor.CreateScope())
            Assert.Equal(ResultadoCreacionDeUsuario.Creado, await alcance.ServiceProvider.GetRequiredService<IGestorIdentidad>()
                .CrearAsync(actor, "super-recuperacion-ficticio", "Superusuario", "contrasenaficticiarecuperacion", false));
        var paciente = Guid.NewGuid();
        await using (var contexto = escenario.Contexto())
        {
            contexto.Pacientes.Add(ReglasDePaciente.Registrar(paciente, "Paciente ficticio recuperable", "contacto-rastreable@ficticio.test", [AmbitoAtencion.Independiente], actor, DateTimeOffset.UtcNow));
            await contexto.SaveChangesAsync();
        }
        await PostgreSqlRevision.Sql(escenario.App, $"""
            INSERT INTO "Cita" ("IdCita","IdPaciente","Ambito","Inicio","Fin","Estado","EstadoPago","Condicion","Version","FechaAlta","IdUsuarioAlta")
            VALUES ('{Guid.NewGuid()}','{paciente}','Independiente','2026-10-08T10:00:00Z','2026-10-08T11:00:00Z','Agendada','Pendiente','Vigente',1,'2026-10-07T12:00:00Z','{actor}');
            INSERT INTO "EventoAuditoria" VALUES ('{Guid.NewGuid()}','2026-10-07T12:00:00Z','{actor}','Paciente','{paciente}','Alta',ARRAY['Nombre'],'Alta ficticia');
            """);
        return paciente;
    }
}
