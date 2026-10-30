using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using NpgsqlTypes;
using TherapEase.Domain.Compartido.Reglas;
using TherapEase.Domain.Pacientes.Constantes;
using TherapEase.Domain.Pacientes.Entidades;
using TherapEase.Domain.Pacientes.Entidades.Enums;
using TherapEase.Domain.Pacientes.Reglas;
using TherapEase.Infrastructure.Data;

namespace TherapEase.IntegrationTests;

/// <summary>
/// Comprueba con datos ficticios anteriores la migración que reemplaza Contacto por teléfono y correo y agrega la clave de nombre:
/// la correspondencia aprobada, la clave normalizada igual a la de Domain, subir, bajar y volver a subir, que los demás datos y
/// permisos no cambien y que un nombre sin clave válida cancele todo antes de retirar Contacto.
/// </summary>
public class MigracionDeDatosDePacienteTests(PostgreSqlRevision postgres) : IClassFixture<PostgreSqlRevision>
{
    private const string Anterior = "20261017205258_NombreCompletoDeUsuario";
    private const string Nueva = "20261028032404_DatosDePacienteYTutor";

    private static readonly Guid Actor = Guid.Parse("10000000-0000-4000-8000-000000000001");

    // Contactos ficticios de la tabla aprobada (la columna anterior admitia hasta 200 caracteres): nulo, vacío, correo, teléfono, texto libre, mezclas y formatos casi válidos.
    private static readonly string?[] Contactos =
    [
        null,
        "",
        "   ",
        "Persona@Ficticio.TEST",
        "  persona@ficticio.test  ",
        "\tpersona@ficticio.test\n",
        "\u00A0persona@ficticio.test\u00A0",
        "MARÍA@Ficticio.TEST",
        "\u00D8SCAR@Ficticio.TEST",
        "\u039F\u03A3@Ficticio.TEST",
        "\u0130@Ficticio.TEST",
        "sin@dominio",
        "@ficticio.test",
        "persona@",
        "a@b..c",
        "a@.b.c",
        "a@b.c.",
        "a@@b.c",
        "a b@c.d",
        "persona\u200B@ficticio.test",
        "persona\u202E@ficticio.test",
        "persona@ficticio.test\u2028",
        "6441234567",
        " +52 (644) 123-4567 ",
        "644-123-4567",
        "644.123.4567",
        "6441234567\n",
        "1234567",
        "123456789012345",
        "123456",
        "1234567890123456",
        "+",
        "++526441234567",
        "644+1234567",
        "644\t1234567",
        "llamar al 6441234567",
        "644 123 4567 ext 5",
        "correo a@b.c o tel 6441234567",
        "٤٤١٢٣٤٥",
        "texto libre sin datos",
        "x",
        CorreoDeLongitud(200),
        new string('9', 200)
    ];

    // Nombres ficticios: acentos, mayúsculas, formas compuesta y descompuesta, espacios raros y escrituras no latinas.
    private static readonly string[] Nombres =
    [
        "María",
        "MARÍA",
        "maria",
        "Muñoz",
        "MUÑOZ",
        "Mun\u0303oz",
        "María Muñoz",
        "Mari\u0301a Mun\u0303oz",
        "  José   Luis \t Ramírez\n",
        "A\u000BB\u000CC\rD",
        "Ana-María O'Brien",
        "Ñandú",
        "Güemes Pérez",
        "ß straße",
        "Жук",
        "İstanbul",
        "ﬁnal",
        "한글",
        "  Ana  ",
        "Ångström",
        "Renée Çelik",
        "\u0301abc",
        "😀 Ana",
        "\u00D8scar",
        "\u00D8SCAR",
        "\u0152UVRE",
        "\u039F\u0394\u03A5\u03A3\u03A3\u0395\u03A5\u03A3",
        "\u03BF\u03B4\u03C5\u03C3\u03C3\u03B5\u03C5\u03C2",
        "\u03A3\u039F\u03A6\u038A\u0391",
        "I\u0307STANBUL",
        new string('Z', 200)
    ];

    private static string CorreoDeLongitud(int longitud) => new string('a', longitud - "@ficticio.test".Length) + "@ficticio.test";

    // ──── PREPARACIÓN ────────────────────────────────────────────────────────────────────────

    private static async Task MigrarAsync(Escenario e, string destino)
    {
        await using var contexto = new ContextoDeDatos(new DbContextOptionsBuilder<ContextoDeDatos>().UseNpgsql(e.Migrador).Options);
        await contexto.GetService<IMigrator>().MigrateAsync(destino);
    }

    private static async Task InsertarAnteriorAsync(Escenario e, Guid id, string nombre, string? contacto, bool baja = false)
    {
        await using var conexion = new NpgsqlConnection(e.Migrador);
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand(
            """
            INSERT INTO "Paciente" ("IdPaciente","Nombre","Contacto","Condicion","Version","FechaAlta","IdUsuarioAlta","FechaBaja","IdUsuarioBaja")
            VALUES (@id,@nombre,@contacto,@condicion,3,@alta,@actor,@fechaBaja,@usuarioBaja)
            """, conexion);
        comando.Parameters.Add(new NpgsqlParameter("id", NpgsqlDbType.Uuid) { Value = id });
        comando.Parameters.Add(new NpgsqlParameter("nombre", NpgsqlDbType.Varchar) { Value = nombre });
        comando.Parameters.Add(new NpgsqlParameter("contacto", NpgsqlDbType.Varchar) { Value = (object?)contacto ?? DBNull.Value });
        comando.Parameters.Add(new NpgsqlParameter("condicion", NpgsqlDbType.Varchar) { Value = baja ? "Baja" : "Vigente" });
        comando.Parameters.Add(new NpgsqlParameter("alta", NpgsqlDbType.TimestampTz) { Value = new DateTimeOffset(2026, 10, 3, 12, 0, 0, TimeSpan.Zero) });
        comando.Parameters.Add(new NpgsqlParameter("actor", NpgsqlDbType.Uuid) { Value = Actor });
        comando.Parameters.Add(new NpgsqlParameter("fechaBaja", NpgsqlDbType.TimestampTz) { Value = baja ? new DateTimeOffset(2026, 10, 4, 12, 0, 0, TimeSpan.Zero) : DBNull.Value });
        comando.Parameters.Add(new NpgsqlParameter("usuarioBaja", NpgsqlDbType.Uuid) { Value = baja ? Actor : DBNull.Value });
        await comando.ExecuteNonQueryAsync();
        await PostgreSqlRevision.Sql(e.Migrador, $"INSERT INTO \"PacienteAmbito\" (\"IdPaciente\",\"Ambito\") VALUES ('{id}','Escolar')");
    }

    // Contacto con cada muestra y nombre con cada nombre; una ficha de baja para comprobar que su condición no cambia.
    private static async Task<Dictionary<Guid, (string Nombre, string? Contacto)>> PrepararDatosAnterioresAsync(Escenario e)
    {
        var filas = new Dictionary<Guid, (string, string?)>();
        foreach (var contacto in Contactos)
        {
            var id = Guid.NewGuid();
            filas[id] = ("Paciente Ficticio", contacto);
            await InsertarAnteriorAsync(e, id, "Paciente Ficticio", contacto);
        }

        foreach (var nombre in Nombres)
        {
            var id = Guid.NewGuid();
            filas[id] = (nombre, null);
            await InsertarAnteriorAsync(e, id, nombre, null);
        }

        var baja = Guid.NewGuid();
        filas[baja] = ("Paciente De Baja", "baja@ficticio.test");
        await InsertarAnteriorAsync(e, baja, "Paciente De Baja", "baja@ficticio.test", baja: true);
        return filas;
    }

    // Lo que decide Domain al escribir un contacto con las reglas nuevas, para comparar con lo que hace el SQL de la migración.
    private static (string? Correo, string? Telefono) Esperado(string? contacto)
    {
        if (string.IsNullOrWhiteSpace(contacto))
        {
            return (null, null);
        }

        if (ReglasDeNombreYCorreo.EsCorreoValido(contacto, LimitesDePaciente.LongitudMaximaCorreo, out var correo) && correo is not null)
        {
            return (correo, null);
        }

        if (ReglasDeTelefono.EsTelefonoValido(contacto, out var telefono) && telefono is not null)
        {
            return (null, telefono);
        }

        return (null, null);
    }

    // Lo que la bajada deja en Contacto: el correo si cabe en 200 caracteres; si no, el teléfono; si no, nulo.
    private static string? ContactoTrasBajar(string? correo, string? telefono) =>
        correo is not null && correo.Length <= 200 ? correo : telefono;

    private sealed record FilaNueva(Guid Id, string Clave, string? Correo, string? Telefono, DateOnly? Nacimiento, string?[] Otros);

    private static async Task<Dictionary<Guid, FilaNueva>> LeerNuevoAsync(Escenario e)
    {
        await using var conexion = new NpgsqlConnection(e.Migrador);
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand(
            """
            SELECT "IdPaciente","NombreNormalizado","Correo","Telefono","FechaNacimiento","Escuela","Grupo","TutorNombre","TutorParentesco","TutorTelefono","TutorCorreo"
            FROM "Paciente"
            """, conexion);
        await using var lector = await comando.ExecuteReaderAsync();
        var filas = new Dictionary<Guid, FilaNueva>();
        while (await lector.ReadAsync())
        {
            var id = lector.GetGuid(0);
            filas[id] = new FilaNueva(
                id, lector.GetString(1),
                lector.IsDBNull(2) ? null : lector.GetString(2),
                lector.IsDBNull(3) ? null : lector.GetString(3),
                lector.IsDBNull(4) ? null : DateOnly.FromDateTime(lector.GetDateTime(4)),
                Enumerable.Range(5, 6).Select(i => lector.IsDBNull(i) ? null : lector.GetString(i)).ToArray());
        }

        return filas;
    }

    private static async Task<Dictionary<Guid, string?>> LeerContactoAsync(Escenario e)
    {
        await using var conexion = new NpgsqlConnection(e.Migrador);
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand("""SELECT "IdPaciente","Contacto" FROM "Paciente" """, conexion);
        await using var lector = await comando.ExecuteReaderAsync();
        var filas = new Dictionary<Guid, string?>();
        while (await lector.ReadAsync())
        {
            filas[lector.GetGuid(0)] = lector.IsDBNull(1) ? null : lector.GetString(1);
        }

        return filas;
    }

    // Huella de lo que la migración no debe tocar: identificadores, nombre, condición, versión, metadatos y ámbitos.
    private static Task<string> HuellaIntactaAsync(Escenario e) => PostgreSqlRevision.Valor<string>(e.Migrador,
        """
        SELECT md5(
          (SELECT COALESCE(string_agg(concat_ws('|', "IdPaciente"::text, "Nombre", "Condicion", "Version"::text, "FechaAlta"::text, "IdUsuarioAlta"::text,
             COALESCE("FechaActualizacion"::text,''), COALESCE("IdUsuarioActualizacion"::text,''), COALESCE("FechaBaja"::text,''), COALESCE("IdUsuarioBaja"::text,'')),
             E'\n' ORDER BY "IdPaciente"), '') FROM "Paciente")
          || '#' ||
          (SELECT COALESCE(string_agg("IdPaciente"::text || "Ambito", ',' ORDER BY "IdPaciente", "Ambito"), '') FROM "PacienteAmbito"))
        """);

    private static Task<string> PermisosDeLaAplicacionAsync(Escenario e) => PostgreSqlRevision.Valor<string>(e.Migrador,
        """
        SELECT COALESCE(string_agg(table_name || ':' || privilege_type, ',' ORDER BY table_name, privilege_type), '')
        FROM information_schema.role_table_grants WHERE grantee = 'therapease_app'
        """);

    private static void ComprobarCorrespondencia(Dictionary<Guid, (string Nombre, string? Contacto)> anteriores, Dictionary<Guid, FilaNueva> nuevas, Func<string?, string?> contactoVigente)
    {
        Assert.Equal(anteriores.Count, nuevas.Count);
        foreach (var (id, (nombre, contacto)) in anteriores)
        {
            var fila = nuevas[id];
            var (correo, telefono) = Esperado(contactoVigente(contacto));
            Assert.True(
                NormalizacionDeNombres.Normalizar(nombre) == fila.Clave,
                $"Clave distinta para el nombre de muestra {anteriores.Keys.ToList().IndexOf(id)}: SQL '{fila.Clave}' frente a Domain '{NormalizacionDeNombres.Normalizar(nombre)}'.");
            Assert.True(correo == fila.Correo, $"Correo distinto para la muestra de contacto {anteriores.Keys.ToList().IndexOf(id)}: esperado '{correo}', SQL '{fila.Correo}'.");
            Assert.True(telefono == fila.Telefono, $"Telefono distinto para la muestra de contacto {anteriores.Keys.ToList().IndexOf(id)}: esperado '{telefono}', SQL '{fila.Telefono}'.");
            Assert.Null(fila.Nacimiento);
            Assert.All(fila.Otros, otro => Assert.Null(otro));
        }
    }

    // ──── PRUEBAS ────────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Subida_ConvierteContactoYNombreIgualQueDomain_ConservaElRestoYLosPermisos()
    {
        var e = await postgres.Crear();
        await MigrarAsync(e, Anterior);
        var anteriores = await PrepararDatosAnterioresAsync(e);
        var huella = await HuellaIntactaAsync(e);
        var permisos = await PermisosDeLaAplicacionAsync(e);

        await MigrarAsync(e, Nueva);

        var nuevas = await LeerNuevoAsync(e);
        ComprobarCorrespondencia(anteriores, nuevas, contacto => contacto);
        Assert.Equal(huella, await HuellaIntactaAsync(e));
        Assert.Equal(permisos, await PermisosDeLaAplicacionAsync(e));
        Assert.Equal(0L, await PostgreSqlRevision.Valor<long>(e.Migrador,
            "SELECT count(*) FROM information_schema.columns WHERE table_name='Paciente' AND column_name='Contacto'"));
        Assert.Equal(1L, await PostgreSqlRevision.Valor<long>(e.Migrador, "SELECT count(*) FROM \"Paciente\" WHERE \"Condicion\"='Baja'"));
    }

    [Fact]
    public async Task Subida_ConservaLaCorrespondenciaAprobadaEnLosCasosRepresentativos()
    {
        var e = await postgres.Crear();
        await MigrarAsync(e, Anterior);
        var anteriores = await PrepararDatosAnterioresAsync(e);

        await MigrarAsync(e, Nueva);

        var nuevas = await LeerNuevoAsync(e);
        (string? Correo, string? Telefono) De(string? contacto) =>
            anteriores.Where(a => a.Value.Contacto == contacto && a.Value.Nombre == "Paciente Ficticio").Select(a => (nuevas[a.Key].Correo, nuevas[a.Key].Telefono)).First();

        Assert.Equal((null, null), De(null));
        Assert.Equal((null, null), De(""));
        Assert.Equal(("persona@ficticio.test", null), De("Persona@Ficticio.TEST"));
        Assert.Equal(("persona@ficticio.test", null), De("  persona@ficticio.test  "));
        Assert.Equal(("persona@ficticio.test", null), De("\u00A0persona@ficticio.test\u00A0"));
        Assert.Equal(("\u00F8scar@ficticio.test", null), De("\u00D8SCAR@Ficticio.TEST"));
        Assert.Equal(("\u03BF\u03C3@ficticio.test", null), De("\u039F\u03A3@Ficticio.TEST"));
        Assert.Equal(("\u0130@ficticio.test", null), De("\u0130@Ficticio.TEST"));
        Assert.Equal((null, "+526441234567"), De(" +52 (644) 123-4567 "));
        Assert.Equal((null, "6441234567"), De("644-123-4567"));
        Assert.Equal((null, null), De("sin@dominio"));
        Assert.Equal((null, null), De("persona\u200B@ficticio.test"));
        Assert.Equal((null, null), De("llamar al 6441234567"));
        Assert.Equal((null, null), De("correo a@b.c o tel 6441234567"));
        Assert.Equal((null, null), De("123456"));
        Assert.Equal((null, null), De("1234567890123456"));
        Assert.Equal((CorreoDeLongitud(200), null), De(CorreoDeLongitud(200)));
        Assert.Equal((null, null), De(new string('9', 200)));
    }

    [Fact]
    public async Task SubirBajarYVolverASubir_DaElMismoResultadoYLaBajadaDejaContactoSegunLoAprobado()
    {
        var e = await postgres.Crear();
        await MigrarAsync(e, Anterior);
        var anteriores = await PrepararDatosAnterioresAsync(e);
        var huella = await HuellaIntactaAsync(e);

        await MigrarAsync(e, Nueva);
        var primera = await LeerNuevoAsync(e);
        await MigrarAsync(e, Anterior);
        var contactosTrasBajar = await LeerContactoAsync(e);
        var huellaTrasBajar = await HuellaIntactaAsync(e);
        await MigrarAsync(e, Nueva);
        var segunda = await LeerNuevoAsync(e);

        foreach (var (id, fila) in primera)
        {
            Assert.Equal(ContactoTrasBajar(fila.Correo, fila.Telefono), contactosTrasBajar[id]);
        }

        Assert.Equal(huella, huellaTrasBajar);
        Assert.Equal(huella, await HuellaIntactaAsync(e));
        ComprobarCorrespondencia(anteriores, segunda, contacto => ContactoTrasBajar(Esperado(contacto).Correo, Esperado(contacto).Telefono));
        foreach (var (id, fila) in primera)
        {
            Assert.Equal(fila.Clave, segunda[id].Clave);
        }
    }

    [Fact]
    public async Task Bajada_ConCorreoYTelefonoSoloConservaElCorreoYUnCorreoDeMasDe200CaracteresNoSeTrunca()
    {
        var e = await postgres.Crear();
        await MigrarAsync(e, Nueva);
        await using (var contexto = e.Contexto())
        {
            var largo = CorreoDeLongitud(254);
            contexto.Pacientes.AddRange(
                ReglasDePaciente.Registrar(Guid.NewGuid(), new DatosDePaciente("Con Ambos", PacientesDePrueba.NacimientoDeAdulto, "6441234567", "ambos@ficticio.test"), [AmbitoAtencion.Escolar], Actor, DateTimeOffset.UtcNow),
                ReglasDePaciente.Registrar(Guid.NewGuid(), new DatosDePaciente("Solo Telefono", PacientesDePrueba.NacimientoDeAdulto, "6441234567"), [AmbitoAtencion.Escolar], Actor, DateTimeOffset.UtcNow),
                ReglasDePaciente.Registrar(Guid.NewGuid(), new DatosDePaciente("Correo Largo Y Telefono", PacientesDePrueba.NacimientoDeAdulto, "6441234567", largo), [AmbitoAtencion.Escolar], Actor, DateTimeOffset.UtcNow),
                ReglasDePaciente.Registrar(Guid.NewGuid(), new DatosDePaciente("Correo Largo Sin Telefono", PacientesDePrueba.NacimientoDeAdulto, null, largo), [AmbitoAtencion.Escolar], Actor, DateTimeOffset.UtcNow),
                ReglasDePaciente.Registrar(Guid.NewGuid(), new DatosDePaciente("Sin Contacto", PacientesDePrueba.NacimientoDeAdulto), [AmbitoAtencion.Escolar], Actor, DateTimeOffset.UtcNow));
            await contexto.SaveChangesAsync();
        }

        await MigrarAsync(e, Anterior);

        var contactos = await PostgreSqlRevision.Valor<string>(e.Migrador,
            """SELECT string_agg("Nombre" || '=' || COALESCE("Contacto", '<nulo>'), ';' ORDER BY "Nombre") FROM "Paciente" """);
        Assert.Equal(
            "Con Ambos=ambos@ficticio.test;Correo Largo Sin Telefono=<nulo>;Correo Largo Y Telefono=6441234567;Sin Contacto=<nulo>;Solo Telefono=6441234567",
            contactos);
    }

    [Fact]
    public async Task ClaveDeNombre_DeLasNuevasEscrituras_EsIgualALaQueCalculaElSqlDeLaMigracion()
    {
        var e = await postgres.Crear();
        // Domain rechaza nombres con caracteres de control: solo se escriben los que acepta.
        var nombres = Nombres.Where(n => !n.Any(char.IsControl) && n.Trim().Length >= 2).ToArray();
        var clavesDeDomain = new Dictionary<Guid, string>();
        await using (var contexto = e.Contexto())
        {
            foreach (var nombre in nombres)
            {
                var paciente = ReglasDePaciente.Registrar(
                    Guid.NewGuid(), new DatosDePaciente(nombre, PacientesDePrueba.NacimientoDeAdulto), [AmbitoAtencion.Escolar], Actor, DateTimeOffset.UtcNow);
                clavesDeDomain[paciente.IdPaciente] = paciente.NombreNormalizado;
                contexto.Pacientes.Add(paciente);
            }

            await contexto.SaveChangesAsync();
        }

        // Bajar y subir recalcula la clave de todas las filas con el SQL de la migración.
        await MigrarAsync(e, Anterior);
        await MigrarAsync(e, Nueva);

        var recalculadas = await LeerNuevoAsync(e);
        Assert.Equal(clavesDeDomain.Count, recalculadas.Count);
        foreach (var (id, clave) in clavesDeDomain)
        {
            Assert.Equal(clave, recalculadas[id].Clave);
        }
    }

    [Fact]
    public async Task Subida_ConUnNombreQueNoDaClaveValida_CancelaTodoAntesDeRetirarContacto()
    {
        var e = await postgres.Crear();
        await MigrarAsync(e, Anterior);
        await InsertarAnteriorAsync(e, Guid.NewGuid(), "Paciente Ficticio", "contacto@ficticio.test");
        // Solo marcas combinantes: no es vacío para la restricción anterior, pero su clave de búsqueda sí lo es.
        await InsertarAnteriorAsync(e, Guid.NewGuid(), "\u0301\u0302", "otro@ficticio.test");
        var huella = await HuellaIntactaAsync(e);

        var error = await Assert.ThrowsAsync<PostgresException>(() => MigrarAsync(e, Nueva));

        Assert.Contains("clave de busqueda", error.MessageText);
        Assert.DoesNotContain("ficticio", error.MessageText);
        Assert.Equal(1L, await PostgreSqlRevision.Valor<long>(e.Migrador,
            "SELECT count(*) FROM information_schema.columns WHERE table_name='Paciente' AND column_name='Contacto'"));
        Assert.Equal(0L, await PostgreSqlRevision.Valor<long>(e.Migrador,
            "SELECT count(*) FROM information_schema.columns WHERE table_name='Paciente' AND column_name='NombreNormalizado'"));
        Assert.Equal(0L, await PostgreSqlRevision.Valor<long>(e.Migrador,
            $"SELECT count(*) FROM \"__EFMigrationsHistory\" WHERE \"MigrationId\"='{Nueva}'"));
        Assert.Equal(huella, await HuellaIntactaAsync(e));
        var contactos = await LeerContactoAsync(e);
        Assert.Equal(["contacto@ficticio.test", "otro@ficticio.test"], contactos.Values.Order(StringComparer.Ordinal));
    }

    [Fact]
    public async Task BaseNueva_ConLaMigracionCompleta_NoTieneContactoYTieneLasColumnasIndiceYRestricciones()
    {
        var e = await postgres.Crear();

        var columnas = await PostgreSqlRevision.Valor<string>(e.Migrador,
            """
            SELECT string_agg(column_name || ':' || data_type || ':' || is_nullable, ',' ORDER BY column_name)
            FROM information_schema.columns WHERE table_name = 'Paciente'
              AND column_name IN ('Contacto','FechaNacimiento','Telefono','Correo','Escuela','Grupo','TutorNombre','TutorParentesco','TutorTelefono','TutorCorreo','NombreNormalizado')
            """);
        var indice = await PostgreSqlRevision.Valor<long>(e.Migrador,
            "SELECT count(*) FROM pg_indexes WHERE tablename='Paciente' AND indexname='IX_Paciente_NombreNormalizado'");
        var restricciones = await PostgreSqlRevision.Valor<string>(e.Migrador,
            "SELECT string_agg(conname, ',' ORDER BY conname) FROM pg_constraint WHERE conrelid = '\"Paciente\"'::regclass AND contype = 'c'");

        Assert.Equal(
            "Correo:character varying:YES,Escuela:character varying:YES,FechaNacimiento:date:YES,Grupo:character varying:YES,NombreNormalizado:character varying:NO,"
            + "Telefono:character varying:YES,TutorCorreo:character varying:YES,TutorNombre:character varying:YES,TutorParentesco:character varying:YES,TutorTelefono:character varying:YES",
            columnas);
        Assert.Equal(1L, indice);
        Assert.Equal(
            "CK_Paciente_BajaCoherente,CK_Paciente_FechaNacimientoMinima,CK_Paciente_NombreNoVacio,CK_Paciente_NombreNormalizadoFormato,CK_Paciente_TutorCompleto",
            restricciones);
    }
}
