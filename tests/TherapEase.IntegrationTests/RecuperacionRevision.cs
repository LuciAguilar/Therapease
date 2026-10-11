using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Npgsql;
using Testcontainers.PostgreSql;

namespace TherapEase.IntegrationTests;

/// <summary>
/// Prepara respaldos y restauraciones locales para QA con PostgreSQL desechable.
/// El cifrado es candidato de prueba; no configura respaldos del sistema en producción.
/// </summary>
public static class RecuperacionRevision
{


    // ──── FORMATO DE PRUEBA ────


    private static readonly byte[] Version = "TEQA0001"u8.ToArray();
    private const int TamanoCabecera = 16;
    private const int TamanoAleatorio = 12;
    private const int TamanoAutenticacion = 16;

    /// Cifra el volcado con AES-256-GCM; la fecha y versión también quedan protegidas.
    public static RespaldoRevision Cifrar(byte[] volcado, byte[] clave, DateTimeOffset fecha)
    {
        var contenido = new byte[TamanoCabecera + TamanoAleatorio + TamanoAutenticacion + volcado.Length];
        Version.CopyTo(contenido, 0);
        BinaryPrimitives.WriteInt64LittleEndian(contenido.AsSpan(8, 8), fecha.ToUnixTimeMilliseconds());
        var aleatorio = contenido.AsSpan(TamanoCabecera, TamanoAleatorio);
        RandomNumberGenerator.Fill(aleatorio);
        using var cifrador = new AesGcm(clave, TamanoAutenticacion);
        cifrador.Encrypt(aleatorio, volcado, contenido.AsSpan(44), contenido.AsSpan(28, 16), contenido.AsSpan(0, TamanoCabecera));
        return new RespaldoRevision(contenido, SHA256.HashData(contenido));
    }

    /// Verifica huella e integridad antes de entregar bytes para pg_restore; una clave incorrecta falla.
    public static byte[] Abrir(RespaldoRevision respaldo, byte[] clave)
    {
        var contenido = respaldo.Contenido;
        if (!CryptographicOperations.FixedTimeEquals(SHA256.HashData(contenido), respaldo.Huella))
            throw new InvalidDataException("La huella del respaldo no coincide.");
        if (contenido.Length <= 44 || !contenido.AsSpan(0, 8).SequenceEqual(Version))
            throw new InvalidDataException("El respaldo está incompleto o su formato no es válido.");
        var volcado = new byte[contenido.Length - 44];
        try
        {
            using var cifrador = new AesGcm(clave, TamanoAutenticacion);
            cifrador.Decrypt(contenido.AsSpan(16, 12), contenido.AsSpan(44), contenido.AsSpan(28, 16), volcado, contenido.AsSpan(0, 16));
            return volcado;
        }
        catch
        {
            CryptographicOperations.ZeroMemory(volcado);
            throw;
        }
    }

    /// Mide la antigüedad para informar Q10, sin impedir la restauración; usar solo después de autenticar la fecha.
    public static TimeSpan Antiguedad(RespaldoRevision respaldo, DateTimeOffset deteccion)
    {
        var fecha = DateTimeOffset.FromUnixTimeMilliseconds(BinaryPrimitives.ReadInt64LittleEndian(respaldo.Contenido.AsSpan(8, 8)));
        return deteccion - fecha;
    }


    // ──── BASE DE DESTINO ────


    /// Crea una base vacía, sin migrarla: pg_restore debe recuperar por sí mismo todo el esquema.
    public static async Task<Escenario> CrearVaciaAsync(PostgreSqlRevision postgres)
    {
        var nombre = "qa_restauracion_" + Guid.NewGuid().ToString("N");
        await PostgreSqlRevision.Sql(postgres.Contenedor.GetConnectionString(), $"CREATE DATABASE {nombre} OWNER therapease_migrador");
        var migrador = new NpgsqlConnectionStringBuilder(postgres.Contenedor.GetConnectionString())
        {
            Database = nombre, Username = "therapease_migrador", Password = "Ficticio-Migrador-Revision"
        }.ConnectionString;
        await PostgreSqlRevision.Sql(migrador, $"REVOKE ALL ON DATABASE {nombre} FROM PUBLIC; GRANT CONNECT ON DATABASE {nombre} TO therapease_app");
        return new Escenario(new NpgsqlConnectionStringBuilder(migrador)
        {
            Username = "therapease_app", Password = "Ficticio-App-Revision"
        }.ConnectionString, migrador);
    }


    // ──── COMANDOS REALES ────


    /// Ejecuta pg_dump con el migrador; el archivo sin cifrar solo existe temporalmente dentro del contenedor.
    public static async Task<byte[]> VolcarAsync(PostgreSqlContainer contenedor, Escenario escenario)
    {
        using var plazo = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var archivo = "/tmp/qa_respaldo_" + Guid.NewGuid().ToString("N") + ".dump";
        try
        {

            // Los argumentos no incluyen contraseñas; se usa la conexión local del contenedor ficticio.

            var resultado = await contenedor.ExecAsync(["sh", "-c", "umask 077; exec pg_dump \"$@\"", "pg_dump",
                "--username=therapease_migrador", "--no-password", "--format=custom",
                "--dbname=" + new NpgsqlConnectionStringBuilder(escenario.Migrador).Database, "--file=" + archivo], plazo.Token);
            if (resultado.ExitCode != 0) throw new InvalidOperationException("pg_dump falló; no se confirma un respaldo.");
            var permisos = await contenedor.ExecAsync(["stat", "-c", "%a", archivo], plazo.Token);
            if (permisos.ExitCode != 0 || permisos.Stdout.Trim() != "600") throw new InvalidOperationException("El volcado temporal no tiene permisos privados.");
            return await contenedor.ReadFileAsync(archivo, plazo.Token);
        }
        finally
        {
            var limpieza = await contenedor.ExecAsync(["rm", "-f", "--", archivo]);
            if (limpieza.ExitCode != 0) throw new InvalidOperationException("No se pudo retirar el volcado temporal.");
        }
    }

    /// Autentica y descifra antes de tocar el destino; restaura en una única transacción, conservando permisos.
    public static async Task RestaurarAsync(PostgreSqlContainer contenedor, Escenario destino, RespaldoRevision respaldo, byte[] clave)
    {
        using var plazo = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var volcado = Abrir(respaldo, clave);
        var archivo = "/tmp/qa_restauracion_" + Guid.NewGuid().ToString("N") + ".dump";
        try
        {
            await contenedor.CopyAsync(volcado, archivo, fileMode: DotNet.Testcontainers.Configurations.UnixFileModes.UserRead | DotNet.Testcontainers.Configurations.UnixFileModes.UserWrite, ct: plazo.Token);
            var resultado = await contenedor.ExecAsync(["pg_restore", "--username=therapease_migrador", "--no-password",
                "--single-transaction", "--exit-on-error", "--dbname=" + new NpgsqlConnectionStringBuilder(destino.Migrador).Database, archivo], plazo.Token);
            if (resultado.ExitCode != 0) throw new InvalidOperationException("pg_restore falló; no se confirma recuperación.");
        }
        finally
        {
            CryptographicOperations.ZeroMemory(volcado);
            var limpieza = await contenedor.ExecAsync(["rm", "-f", "--", archivo]);
            if (limpieza.ExitCode != 0) throw new InvalidOperationException("No se pudo retirar el volcado temporal.");
        }
    }


    // ──── COMPARACIÓN SIN MOSTRAR DATOS ────


    /// Lee todas las filas de cada tabla en orden estable y compara huellas; no imprime su contenido.
    public static async Task<SortedDictionary<string, string>> CompararDatosAsync(string conexion)
    {
        await using var baseDatos = new NpgsqlConnection(conexion);
        await baseDatos.OpenAsync();
        var tablas = new List<string>();
        await using (var comando = new NpgsqlCommand("SELECT tablename FROM pg_tables WHERE schemaname='public' ORDER BY tablename", baseDatos))
        await using (var lector = await comando.ExecuteReaderAsync())
            while (await lector.ReadAsync()) tablas.Add(lector.GetString(0));
        var huellas = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var tabla in tablas)
        {
            var identificador = new NpgsqlCommandBuilder().QuoteIdentifier(tabla);
            await using var comando = new NpgsqlCommand($"SELECT COALESCE(jsonb_agg(to_jsonb(f) ORDER BY to_jsonb(f)::text)::text,'[]') FROM {identificador} f", baseDatos);
            var datos = (string)(await comando.ExecuteScalarAsync())!;
            huellas.Add(tabla, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(datos))));
        }
        return huellas;
    }
}


/// <summary>
/// Conserva el archivo cifrado y su huella SHA-256; nunca incluye la clave de descifrado.
/// </summary>
public sealed record RespaldoRevision(byte[] Contenido, byte[] Huella);
