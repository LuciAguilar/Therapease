using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using TherapEase.Application.Compartido.Interfaces.Servicios;
using TherapEase.Application.Identidad.Interfaces.Servicios;
using TherapEase.Infrastructure.Identidad.Entidades;
using TherapEase.Web.Configuracion;
using TherapEase.Web.Seguridad;
using Npgsql;
using Testcontainers.PostgreSql;
using TherapEase.Application.Identidad.Interfaces.Repositorios;
using TherapEase.Domain.Pacientes.Entidades.Enums;
using TherapEase.Domain.Pacientes.Reglas;
using TherapEase.Infrastructure.Configuracion;
using TherapEase.Infrastructure.Data;
using Xunit;


namespace TherapEase.IntegrationTests;

/// <summary>
/// Levanta un PostgreSQL desechable en Docker para las pruebas de integración.
/// Crea los dos usuarios de la base (migrador y aplicación) igual que en producción
/// y entrega a cada prueba su propia base, ya migrada y sin datos de otras pruebas.
/// </summary>
public class PostgreSqlRevision : IAsyncLifetime
{


    // ──── CONTENEDOR ─────────────────────────────────────────────────────────────────────────


    /// Contenedor de PostgreSQL 17; las contraseñas son ficticias y solo existen mientras corre.
    public PostgreSqlContainer Contenedor { get; } = new PostgreSqlBuilder("postgres:17")
        .WithDatabase("revision_ficticia")
        .WithUsername("postgres")
        .WithPassword("Ficticio-Solo-Contenedor-Revision")
        .Build();


    /// Arranca el contenedor y crea los usuarios separados: el migrador cambia el esquema
    /// y la aplicación solo trabaja con datos, sin privilegios de administrador.
    public async Task InitializeAsync()
    {
        await Contenedor.StartAsync();
        await Sql(Contenedor.GetConnectionString(), """
            CREATE ROLE therapease_migrador LOGIN PASSWORD 'Ficticio-Migrador-Revision' NOSUPERUSER NOCREATEDB NOCREATEROLE;
            CREATE ROLE therapease_app LOGIN PASSWORD 'Ficticio-App-Revision' NOSUPERUSER NOCREATEDB NOCREATEROLE;
            """);
    }


    /// Elimina el contenedor al terminar; no queda ninguna base de prueba.
    public async Task DisposeAsync() => await Contenedor.DisposeAsync();


    // ──── BASE POR PRUEBA ────────────────────────────────────────────────────────────────────


    /// Crea una base nueva con nombre único, aplica las migraciones reales con el migrador
    /// y devuelve las conexiones de ambos usuarios. Así cada prueba empieza desde cero.
    public async Task<Escenario> Crear()
    {
        var nombre = "qa_" + Guid.NewGuid().ToString("N");
        await Sql(Contenedor.GetConnectionString(), $"CREATE DATABASE {nombre} OWNER therapease_migrador");
        var migrador = new NpgsqlConnectionStringBuilder(Contenedor.GetConnectionString())
        {
            Database = nombre, Username = "therapease_migrador", Password = "Ficticio-Migrador-Revision"
        }.ConnectionString;

        // Igual que el script de roles: nadie más que la aplicación puede conectarse.

        await Sql(migrador, $"REVOKE ALL ON DATABASE {nombre} FROM PUBLIC; GRANT CONNECT ON DATABASE {nombre} TO therapease_app;");
        await using var contexto = new ContextoDeDatos(new DbContextOptionsBuilder<ContextoDeDatos>().UseNpgsql(migrador).Options);
        await contexto.Database.MigrateAsync();
        var app = new NpgsqlConnectionStringBuilder(migrador)
        {
            Username = "therapease_app", Password = "Ficticio-App-Revision"
        }.ConnectionString;
        return new Escenario(app, migrador);
    }


    // ──── CONSULTAS DIRECTAS ─────────────────────────────────────────────────────────────────


    /// Ejecuta SQL sin pasar por la aplicación; sirve para preparar datos o intentar
    /// operaciones que la base debe rechazar. Devuelve las filas afectadas.
    public static async Task<int> Sql(string conexion, string sql)
    {
        await using var c = new NpgsqlConnection(conexion);
        await c.OpenAsync();
        await using var cmd = new NpgsqlCommand(sql, c);
        cmd.CommandTimeout = 15;
        return await cmd.ExecuteNonQueryAsync();
    }


    /// Lee un solo valor de la base para comprobar el resultado de una prueba.
    public static async Task<T> Valor<T>(string conexion, string sql)
    {
        await using var c = new NpgsqlConnection(conexion);
        await c.OpenAsync();
        await using var cmd = new NpgsqlCommand(sql, c);
        return (T)(await cmd.ExecuteScalarAsync())!;
    }
}



/// <summary>
/// Base de una prueba con sus dos conexiones: la de la aplicación y la del migrador.
/// Permite construir los servicios reales del sistema conectados a esa base.
/// </summary>
public sealed record Escenario(string App, string Migrador)
{
    /// Contexto de datos con el usuario de la aplicación, el mismo que usa el servidor.
    public ContextoDeDatos Contexto() => new(new DbContextOptionsBuilder<ContextoDeDatos>().UseNpgsql(App).Options);


    /// Registra los mismos servicios que el arranque de Web: persistencia, Identity,
    /// servicios de aplicación y seguridad. Cada prueba puede ajustar algo con «configurar».
    public ServiceProvider Servicios(Action<IServiceCollection>? configurar = null)
    {
        var servicios = new ServiceCollection();
        servicios.AddLogging();
        servicios.AgregarPersistencia(App);
        servicios.AgregarIdentidad();
        servicios.AgregarServiciosDeAplicacion();
        servicios.AgregarSeguridadWeb();
        configurar?.Invoke(servicios);
        return servicios.BuildServiceProvider();
    }
}

