using Microsoft.EntityFrameworkCore;
using Npgsql;
using TherapEase.Application.Pacientes.Constantes;
using TherapEase.Application.Pacientes.Interfaces.Servicios;
using TherapEase.Application.Pacientes.Modelos;
using TherapEase.Domain.Auditoria.Entidades.Enums;
using static TherapEase.IntegrationTests.PacientesRevision;
using static TherapEase.IntegrationTests.ServidorIdentidadRevision;

namespace TherapEase.IntegrationTests;

/// <summary>
/// Pruebas de qué queda guardado cuando algo falla o llegan cambios al mismo tiempo:
/// un corte antes o después de confirmar el guardado, un fallo al guardar la auditoría,
/// dos envíos iguales del mismo registro y dos personas editando la misma ficha.
/// Usa PostgreSQL real y datos inventados.
/// </summary>
public class PacientesGuardadoTests(PostgreSqlRevision postgres) : IClassFixture<PostgreSqlRevision>
{


    // ──── CORTES Y FALLOS AL GUARDAR ───────────────────────────────────────────────────────


    /// Si el registro se corta antes de guardar, al perder la conexión o después de guardar sin recibir respuesta,
    /// reenviar el mismo número de ficha termina con una sola ficha y un solo evento.
    /// Una respuesta perdida no significa que no se guardó.
    [Theory]
    [InlineData("antes")]
    [InlineData("conexion")]
    [InlineData("despues")]
    public async Task UnAltaInterrumpidaSeCompruebaAntesDeReenviarElMismoIdentificador(string modo)
    {
        // Arrange: el corte interrumpe el guardado en el punto elegido. Se activa dentro de la solicitud,
        // cuando la sesión ya se validó, para cortar solo el guardado de la ficha.
        var corte = new CorteGuardadoRevision(postgres.Contenedor.GetConnectionString());
        using var e = await PacientesRevision.CrearAsync(postgres, interceptores: [corte]);
        var id = Guid.NewGuid();
        var datos = Menor();
        var antes = await e.HuellaAsync();

        // Act
        await Assert.ThrowsAnyAsync<Exception>(() => e.EnSolicitudAsync(async solicitud =>
        {
            corte.Activar(modo);
            return await solicitud.Obtener<IServicioPacientes>().RegistrarAsync(id, datos);
        }));

        // Assert: una respuesta perdida no equivale a un guardado fallido.
        Assert.Equal(1, corte.AntesDeConfirmar);
        var confirmada = modo == "despues";
        Assert.Equal(confirmada ? 1 : 0, (await e.FichasAsync()).Count);
        Assert.Equal(confirmada ? 1 : 0, (await e.EventosAsync()).Count);
        if (!confirmada) Assert.Equal(antes, await e.HuellaAsync());
        if (modo == "conexion") Assert.True(corte.ConexionTerminada);
        var reenvio = await e.ServicioAsync(s => s.RegistrarAsync(id, datos));
        Assert.True(reenvio.Exito);
        Assert.Equal(confirmada ? EstadoDeAlta.YaRegistrado : EstadoDeAlta.Registrado, reenvio.Datos!.Estado);
        Assert.Equal(id, (await e.FichaUnicaAsync()).IdPaciente);
        Assert.Single(await e.EventosAsync());
    }

    /// Si una edición se corta, se consulta la ficha para saber si el cambio quedó.
    /// Si sí quedó, repetir la edición con la versión anterior avisa del conflicto y no agrega otro evento.
    [Theory]
    [InlineData("antes")]
    [InlineData("conexion")]
    [InlineData("despues")]
    public async Task UnaEdicionInterrumpidaSeConsultaSinRepetirAutomaticamenteLaVersionAnterior(string modo)
    {
        // Arrange
        var corte = new CorteGuardadoRevision(postgres.Contenedor.GetConnectionString());
        using var e = await PacientesRevision.CrearAsync(postgres, interceptores: [corte]);
        var datos = Adulto();
        var id = await e.RegistrarAsync(datos);
        var antes = await e.HuellaAsync();
        var nuevos = datos with { Grupo = "Grupo Nuevo Ficticio" };

        // Act
        await Assert.ThrowsAnyAsync<Exception>(() => e.EnSolicitudAsync(async solicitud =>
        {
            corte.Activar(modo);
            return await solicitud.Obtener<IServicioPacientes>().ActualizarAsync(id, 1, nuevos);
        }));

        // Assert: que la ficha exista no demuestra que la edición se guardó.
        Assert.Equal(1, corte.AntesDeConfirmar);
        var confirmada = modo == "despues";
        var leida = (await e.ServicioAsync(s => s.ObtenerAsync(id))).Datos!;
        Assert.Equal(confirmada ? 2 : 1, leida.Version);
        Assert.Equal(confirmada ? nuevos.Grupo : datos.Grupo, leida.Grupo);
        Assert.Equal(confirmada ? 2 : 1, (await e.EventosAsync()).Count);
        if (!confirmada) Assert.Equal(antes, await e.HuellaAsync());
        if (confirmada)
        {
            var repetida = await e.ServicioAsync(s => s.ActualizarAsync(id, 1, nuevos));
            Assert.False(repetida.Exito);
            Assert.Equal(MensajesDePacientes.ConflictoDeVersion, repetida.Mensaje);
            Assert.Equal(2, (await e.EventosAsync()).Count);
        }
    }

    /// Si la base de datos rechaza el evento de auditoría, no se guarda el registro ni la edición:
    /// el cambio y su evento se guardan juntos o no se guarda ninguno.
    [Fact]
    public async Task SiLaAuditoriaRechazaElAltaOLaEdicionNoQuedaNingunaParteDelCambio()
    {
        // Arrange
        using var e = await PacientesRevision.CrearAsync(postgres);
        var id = await e.RegistrarAsync();
        await SimularFalloDeAuditoriaAsync(e.Escenario);
        var antes = await e.HuellaAsync();

        // Act + Assert: el error debe venir del rechazo real de la base de datos.
        var alta = await Assert.ThrowsAsync<DbUpdateException>(() => e.ServicioAsync(s => s.RegistrarAsync(Guid.NewGuid(), Menor())));
        Assert.IsType<PostgresException>(alta.InnerException);
        Assert.Equal(antes, await e.HuellaAsync());
        var edicion = await Assert.ThrowsAsync<DbUpdateException>(() => e.ServicioAsync(s => s.ActualizarAsync(id, 1, Adulto("Edicion Rechazada"))));
        Assert.IsType<PostgresException>(edicion.InnerException);
        Assert.Equal(antes, await e.HuellaAsync());
    }

    /// Después de un registro que falló por la auditoría, guardar otra ficha en la misma solicitud
    /// guarda solo la nueva: el registro fallido no debe reaparecer.
    [Fact]
    public async Task UnGuardadoPosteriorEnLaMismaSolicitudNoRescataElAltaQueFalloEnAuditoria()
    {
        // Arrange
        using var e = await PacientesRevision.CrearAsync(postgres);
        await SimularFalloDeAuditoriaAsync(e.Escenario);
        var fallida = Guid.NewGuid();
        var nueva = Guid.NewGuid();

        // Act
        await e.EnSolicitudAsync(async solicitud =>
        {
            var servicio = solicitud.Obtener<IServicioPacientes>();
            await Assert.ThrowsAsync<DbUpdateException>(() => servicio.RegistrarAsync(fallida, Menor()));
            Assert.Empty(await e.FichasAsync());

            // Se quita el rechazo de auditoría para que el segundo registro sí pueda guardarse.

            await e.QuitarFalloDeAuditoriaAsync();
            Assert.True((await servicio.RegistrarAsync(nueva, Adulto())).Exito);
            return true;
        });

        // Assert
        Assert.Equal(nueva, (await e.FichaUnicaAsync()).IdPaciente);
        Assert.Equal(nueva, Assert.Single(await e.EventosAsync()).IdRegistro);
    }


    // ──── CAMBIOS SIMULTÁNEOS ──────────────────────────────────────────────────────────────


    /// Dos personas editan al mismo tiempo la misma versión de una ficha: solo se guarda una edición,
    /// la otra recibe el aviso de conflicto y queda registrado quién hizo el cambio. Se repite tres veces.
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task DosPersonasEditanLaMismaVersionYSoloSeConfirmaUna(int ensayo)
    {
        // Arrange: dos usuarios con sesiones distintas; las dos solicitudes esperan a coincidir antes de guardar.
        Assert.InRange(ensayo, 1, 3); // El número de ensayo solo sirve para repetir la prueba.
        var espera = new EsperaGuardadoPacienteRevision(EntityState.Modified);
        using var e = await PacientesRevision.CrearAsync(postgres, interceptores: [espera]);
        var datos = Adulto();
        var id = await e.RegistrarAsync(datos);
        var otro = await CrearAsync(e.Proveedor, "segundo-editor-ficticio", "Superusuario");
        var otraCookie = await EntrarAsync(e.Proveedor, "segundo-editor-ficticio");
        e.Reloj.Avanzar(TimeSpan.FromMilliseconds(1));
        var primera = Task.Run(() => e.EnSolicitudAsync(s => s.Obtener<IServicioPacientes>()
            .ActualizarAsync(id, 1, datos with { Grupo = "Primer Grupo" })));
        var segunda = Task.Run(() => e.EnSolicitudAsync(s => s.Obtener<IServicioPacientes>()
            .ActualizarAsync(id, 1, datos with { Grupo = "Segundo Grupo" }), otraCookie));

        // Act
        var resultados = await Task.WhenAll(primera, segunda).WaitAsync(TimeSpan.FromSeconds(35));

        // Assert
        Assert.Equal(2, espera.Llegadas);
        Assert.Single(resultados, r => r.Exito);
        var rechazo = Assert.Single(resultados, r => !r.Exito);
        Assert.Equal(MensajesDePacientes.ConflictoDeVersion, rechazo.Mensaje);
        var ganadora = Assert.Single(resultados, r => r.Exito).Datos!.Ficha;
        var guardada = (await e.FichaUnicaAsync());
        Assert.Equal(2, guardada.Version);
        Assert.Equal(ganadora.Grupo, guardada.Grupo);
        Assert.Equal(guardada.Grupo == "Primer Grupo" ? e.Actor : otro, guardada.IdUsuarioActualizacion);
        Assert.Equal(2, (await e.EventosAsync()).Count);
    }

    /// Dos envíos del mismo registro al mismo tiempo (como un doble clic en Guardar) guardan una sola ficha
    /// y un solo evento; los dos reciben respuesta correcta. Se repite tres veces.
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task DosEnviosSimultaneosDelMismoAltaGuardanUnaFichaYUnEvento(int ensayo)
    {
        // Arrange: las dos solicitudes llegan al guardado; no basta con enviarlas una tras otra.
        Assert.InRange(ensayo, 1, 3); // El número de ensayo solo sirve para repetir la prueba.
        var espera = new EsperaGuardadoPacienteRevision(EntityState.Added);
        using var e = await PacientesRevision.CrearAsync(postgres, interceptores: [espera]);
        var id = Guid.NewGuid();
        var datos = Menor();

        // Act
        var resultados = await Task.WhenAll(
            Task.Run(() => e.ServicioAsync(s => s.RegistrarAsync(id, datos))),
            Task.Run(() => e.ServicioAsync(s => s.RegistrarAsync(id, datos)))).WaitAsync(TimeSpan.FromSeconds(35));

        // Assert
        Assert.Equal(2, espera.Llegadas);
        Assert.All(resultados, r => Assert.True(r.Exito, r.Mensaje));
        Assert.Single(resultados, r => r.Datos!.Estado == EstadoDeAlta.Registrado);
        Assert.Single(resultados, r => r.Datos!.Estado == EstadoDeAlta.YaRegistrado);
        Assert.Equal(id, (await e.FichaUnicaAsync()).IdPaciente);
        Assert.Equal(id, Assert.Single(await e.EventosAsync()).IdRegistro);
    }
}
