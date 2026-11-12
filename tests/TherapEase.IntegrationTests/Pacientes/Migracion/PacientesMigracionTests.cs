using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using TherapEase.Domain.Compartido.Reglas;
using TherapEase.Domain.Pacientes.Constantes;
using TherapEase.Infrastructure.Data;

namespace TherapEase.IntegrationTests;

/// <summary>
/// Pruebas del cambio de estructura de la base de datos (migración) con fichas que ya existían.
/// El contacto anterior pasa a teléfono o correo solo cuando se reconoce; el texto que no se puede separar
/// se pierde, como se aprobó. Los nombres anteriores quedan listos para buscarse con las mismas reglas.
/// Usa datos inventados.
/// </summary>
public class PacientesMigracionTests(PostgreSqlRevision postgres) : IClassFixture<PostgreSqlRevision>
{


    // ──── PREPARACIÓN DE FICHAS ANTERIORES ─────────────────────────────────────────────────


    /// Última migración antes de agregar los datos de paciente y tutor.
    private const string Anterior = "20261017205258_NombreCompletoDeUsuario";

    /// Usuario inventado que figura como quien registró las fichas anteriores.
    private static readonly Guid ActorFicticio = Guid.Parse("20000000-0000-4000-8000-000000000002");

    /// Conexión con permiso para cambiar la estructura de la base de datos.
    private static ContextoDeDatos Migrador(Escenario e) =>
        new(new DbContextOptionsBuilder<ContextoDeDatos>().UseNpgsql(e.Migrador).Options);

    /// Inserta una ficha con la estructura anterior (un solo campo de contacto),
    /// usando parámetros y sin saltar ni desactivar las reglas de la base de datos.
    private static async Task<Guid> InsertarAnteriorAsync(Escenario e, string nombre, string? contacto)
    {
        var id = Guid.NewGuid();
        await using var conexion = new NpgsqlConnection(e.Migrador);
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand("""
            INSERT INTO "Paciente" ("IdPaciente","Nombre","Contacto","Condicion","Version","FechaAlta","IdUsuarioAlta")
            VALUES (@id,@nombre,@contacto,'Vigente',3,@alta,@actor);
            INSERT INTO "PacienteAmbito" ("IdPaciente","Ambito") VALUES (@id,'Independiente');
            """, conexion);
        comando.Parameters.AddWithValue("id", id);
        comando.Parameters.AddWithValue("nombre", nombre);
        comando.Parameters.AddWithValue("contacto", NpgsqlTypes.NpgsqlDbType.Varchar, (object?)contacto ?? DBNull.Value);
        comando.Parameters.AddWithValue("alta", new DateTimeOffset(2026, 10, 3, 12, 0, 0, TimeSpan.Zero));
        comando.Parameters.AddWithValue("actor", ActorFicticio);
        await comando.ExecuteNonQueryAsync();
        return id;
    }


    // ──── CONTACTOS Y NOMBRES ANTERIORES ───────────────────────────────────────────────────


    /// Al migrar, un contacto que claramente es un teléfono o un correo pasa a su campo;
    /// el texto libre o mezclado se pierde, como se aprobó.
    /// Se conservan número de ficha, versión y quién la registró, sin inventar nacimiento ni tutor.
    /// Volver a la estructura anterior no recupera el texto perdido.
    [Fact]
    public async Task LaMigracionSeparaSoloContactosReconociblesYNoPrometeRecuperarElTextoPerdido()
    {
        // Arrange
        var e = await postgres.Crear();
        await using var migrador = Migrador(e);
        var migraciones = migrador.GetService<IMigrator>();
        await migraciones.MigrateAsync(Anterior);
        var telefono = await InsertarAnteriorAsync(e, "Telefono Legado Ficticio", "644-123-4567");
        var correo = await InsertarAnteriorAsync(e, "Correo Legado Ficticio", "PERSONA@FICTICIO.TEST");
        var libre = await InsertarAnteriorAsync(e, "Contacto Libre Ficticio", "Llamar solo por la tarde");
        var mezcla = await InsertarAnteriorAsync(e, "Contacto Mixto Ficticio", "correo a@b.c o tel 6441234567");

        // Act
        await migraciones.MigrateAsync();

        // Assert: conserva número de ficha, versión y quién la registró; no inventa nacimiento ni tutor.
        await using (var lectura = e.Contexto())
        {
            var fichas = await lectura.Pacientes.AsNoTracking().Include(p => p.Ambitos).ToDictionaryAsync(p => p.IdPaciente);
            Assert.Equal(4, fichas.Count);
            Assert.Equal("6441234567", fichas[telefono].Telefono);
            Assert.Null(fichas[telefono].Correo);
            Assert.Equal("persona@ficticio.test", fichas[correo].Correo);
            Assert.Null(fichas[correo].Telefono);
            Assert.Null(fichas[libre].Telefono); Assert.Null(fichas[libre].Correo);
            Assert.Null(fichas[mezcla].Telefono); Assert.Null(fichas[mezcla].Correo);
            Assert.All(fichas.Values, p =>
            {
                Assert.Equal(3, p.Version); Assert.Equal(ActorFicticio, p.IdUsuarioAlta);
                Assert.Null(p.FechaNacimiento); Assert.Null(p.TutorNombre); Assert.Single(p.Ambitos.Select(a => a.Ambito));
            });
        }

        // Act + Assert: al volver a la estructura anterior, el texto libre no regresa.
        await migraciones.MigrateAsync(Anterior);
        await using (var conexion = new NpgsqlConnection(e.Migrador))
        {
            await conexion.OpenAsync();
            await using var comando = new NpgsqlCommand("SELECT \"Contacto\" FROM \"Paciente\" WHERE \"IdPaciente\"=@id", conexion);
            comando.Parameters.AddWithValue("id", libre);
            Assert.Equal(DBNull.Value, await comando.ExecuteScalarAsync());
        }

        // Act + Assert: migrar otra vez conserva las cuatro fichas.
        await migraciones.MigrateAsync();
        await using var final = e.Contexto();
        Assert.Equal(4, await final.Pacientes.CountAsync());
    }

    /// Un correo anterior conserva las mismas letras que al escribirlo en la aplicación,
    /// también con la İ turca, la sigma griega y ambas juntas.
    /// Volver a la estructura anterior y migrar otra vez conserva el mismo correo.
    [Theory]
    [InlineData("İ@ficticio.test", "İ@ficticio.test")]
    [InlineData("ΟΣ@ficticio.test", "οσ@ficticio.test")]
    [InlineData("aİbΣ@X.TEST", "aİbσ@x.test")]
    public async Task LosCorreosAnterioresCoincidenConLaAplicacionAlMigrarYVolverAMigrar(string anterior, string esperado)
    {
        // Arrange: se comprueba la regla de la aplicación y un resultado escrito a mano.
        Assert.True(ReglasDeNombreYCorreo.EsCorreoValido(anterior, LimitesDePaciente.LongitudMaximaCorreo, out var normalizado));
        Assert.Equal(esperado, normalizado);
        var e = await postgres.Crear();
        await using var migrador = Migrador(e);
        var migraciones = migrador.GetService<IMigrator>();
        await migraciones.MigrateAsync(Anterior);
        var id = await InsertarAnteriorAsync(e, "Correo Anterior Ficticio", anterior);

        // Act
        await migraciones.MigrateAsync();

        // Assert
        await ComprobarCorreoGuardadoAsync(e, id, normalizado!);

        // Act
        await migraciones.MigrateAsync(Anterior);

        // Assert: la vuelta conserva el correo reconocido, sin inventar otro dato.
        await using (var conexion = new NpgsqlConnection(e.Migrador))
        {
            await conexion.OpenAsync();
            await using var comando = new NpgsqlCommand("SELECT \"Contacto\" FROM \"Paciente\" WHERE \"IdPaciente\"=@id", conexion);
            comando.Parameters.AddWithValue("id", id);
            Assert.Equal(normalizado, await comando.ExecuteScalarAsync());
        }

        // Act
        await migraciones.MigrateAsync();

        // Assert
        await ComprobarCorreoGuardadoAsync(e, id, normalizado!);
    }

    /// Lee el correo con otra conexión y comprueba que no se haya convertido en teléfono.
    private static async Task ComprobarCorreoGuardadoAsync(Escenario e, Guid id, string esperado)
    {
        await using var lectura = e.Contexto();
        var ficha = await lectura.Pacientes.AsNoTracking().SingleAsync(p => p.IdPaciente == id);
        Assert.Equal(esperado, ficha.Correo);
        Assert.Null(ficha.Telefono);
        Assert.Equal(3, ficha.Version);
    }

    /// Al migrar, los nombres anteriores quedan preparados para buscarse con las mismas reglas que los nuevos:
    /// sin mayúsculas ni acentos, también en otros alfabetos (Ø, Ж, İ, Σ).
    /// Lo esperado se escribió a mano a partir de la regla aprobada, sin copiar el código de la migración.
    [Theory]
    [InlineData("Øscar Ficticio", "øscar ficticio")]
    [InlineData("Жук Ficticio", "жук ficticio")]
    [InlineData("İstanbul Ficticio", "istanbul ficticio")]
    [InlineData("Σς Ficticio", "σσ ficticio")]
    public async Task LaMigracionNormalizaLasMismasMayusculasQueLaBusquedaAprobada(string nombre, string claveEsperada)
    {
        // Arrange
        var e = await postgres.Crear();
        await using var migrador = Migrador(e);
        var migraciones = migrador.GetService<IMigrator>();
        await migraciones.MigrateAsync(Anterior);
        var id = await InsertarAnteriorAsync(e, nombre, null);

        // Act
        await migraciones.MigrateAsync();

        // Assert
        await using var lectura = e.Contexto();
        Assert.Equal(claveEsperada, await lectura.Pacientes.Where(p => p.IdPaciente == id).Select(p => p.NombreNormalizado).SingleAsync());
    }
}
