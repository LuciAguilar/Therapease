using Microsoft.EntityFrameworkCore;
using TherapEase.Application.Citas.Constantes;
using TherapEase.Application.Citas.Interfaces.Servicios;
using TherapEase.Application.Citas.Modelos;
using TherapEase.Application.Coordinadores.Interfaces.Servicios;

namespace TherapEase.IntegrationTests;

/// <summary>
/// Comprueba límites de la agenda, privacidad de un paciente de baja y permisos de sesiones reales.
/// Los datos preparados fuera del horario representan citas anteriores que deben conservarse.
/// </summary>
public sealed partial class AgendaYReservasTests
{


    // ──── CONSULTA Y PRIVACIDAD ────────────────────────────────────────────────────────────


    /// Una agenda con 501 citas devuelve 500 ordenadas y avisa que hay más, sin alterar las citas antiguas.
    [Fact]
    public async Task AgendaLimitaAQuinientasYConservaCitasAnterioresFueraDeHorario()
    {

        // Arrange
        using var e = await PacientesRevision.CrearAsync(postgres);
        var paciente = await e.RegistrarAsync();
        var numeros = Enumerable.Range(0, 501).Select(_ => Guid.NewGuid()).ToArray();
        await e.SqlAsync("""
            INSERT INTO "Cita" ("IdCita","IdPaciente","Ambito","Inicio","Fin","Estado","EstadoPago","Condicion","Version","FechaAlta","IdUsuarioAlta")
            SELECT x.id, @paciente, 'Escolar', @inicio + (x.n-1)*INTERVAL '2 minutes',
                @inicio + (x.n-1)*INTERVAL '2 minutes' + INTERVAL '1 minute',
                'Agendada','Pendiente','Vigente',1,@alta,@actor
            FROM unnest(@numeros::uuid[]) WITH ORDINALITY AS x(id,n)
            """, ("paciente", paciente), ("inicio", new DateTimeOffset(2026, 12, 1, 16, 0, 0, TimeSpan.Zero)),
            ("alta", new DateTimeOffset(2026, 10, 1, 18, 0, 0, TimeSpan.Zero)), ("actor", e.Actor), ("numeros", numeros));

        // Act
        var agenda = await e.EnSolicitudAsync(s => s.Obtener<IServicioCitas>().ConsultarAgendaAsync(new DateOnly(2026, 12, 1), new DateOnly(2026, 12, 2)));
        var diaSiguiente = await e.EnSolicitudAsync(s => s.Obtener<IServicioCitas>().ConsultarAgendaAsync(new DateOnly(2026, 12, 2), new DateOnly(2026, 12, 2)));

        // Assert
        Assert.True(agenda.Exito, agenda.Mensaje);
        Assert.True(agenda.Datos!.HayMas);
        Assert.Equal(500, agenda.Datos.Citas.Count);
        Assert.Equal(numeros.Take(500), agenda.Datos.Citas.Select(c => c.IdCita));
        Assert.Equal(51, diaSiguiente.Datos!.Citas.Count);
        Assert.False(diaSiguiente.Datos.HayMas);
        await using var db = e.Escenario.Contexto();
        Assert.Equal(501, await db.Citas.CountAsync());
    }

    /// Una cita cancelada de paciente de baja conserva su horario, pero agenda y detalle ocultan su nombre y número de paciente.
    [Fact]
    public async Task AgendaYDetalleOcultanLosDatosDelPacienteDeBaja()
    {

        // Arrange
        using var e = await PacientesRevision.CrearAsync(postgres);
        var paciente = await e.RegistrarAsync();
        var id = Guid.NewGuid();
        var alta = await e.EnSolicitudAsync(s => s.Obtener<IServicioCitas>().AgendarAsync(id, Datos(paciente)));
        Assert.True(alta.Exito, alta.Mensaje);
        await e.SqlAsync("UPDATE \"Cita\" SET \"Estado\"='Cancelada' WHERE \"IdCita\"=@id", ("id", id));
        var baja = await e.EnSolicitudAsync(s => s.Obtener<ICoordinadorDeBajaDePaciente>().DarDeBajaAsync(paciente, 1));
        Assert.True(baja.Exito, baja.Mensaje);
        await using var db = e.Escenario.Contexto();
        var eventosAntes = await db.EventosAuditoria.CountAsync();

        // Act
        var activas = await e.EnSolicitudAsync(s => s.Obtener<IServicioCitas>().ConsultarAgendaAsync(new DateOnly(2026, 12, 1), new DateOnly(2026, 12, 1)));
        var canceladas = await e.EnSolicitudAsync(s => s.Obtener<IServicioCitas>().ConsultarAgendaAsync(new DateOnly(2026, 12, 1), new DateOnly(2026, 12, 1), VistaDeAgenda.Canceladas));
        var detalle = await e.EnSolicitudAsync(s => s.Obtener<IServicioCitas>().ObtenerCitaAsync(id));

        // Assert
        Assert.Empty(activas.Datos!.Citas);
        var fila = Assert.Single(canceladas.Datos!.Citas);
        Assert.True(fila.PacienteDeBaja);
        Assert.Null(fila.IdPaciente);
        Assert.Null(fila.NombreDelPaciente);
        Assert.True(detalle.Datos!.PacienteDeBaja);
        Assert.Null(detalle.Datos.IdPaciente);
        Assert.Null(detalle.Datos.NombreDelPaciente);
        Assert.Equal(eventosAntes, await db.EventosAuditoria.CountAsync());
    }


    // ──── PERMISOS, FECHAS Y VISTAS ────────────────────────────────────────────────────────


    /// Sin sesión o con contraseña temporal no se lee la agenda ni se agenda, aunque las entradas sean válidas o inválidas.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SesionInvalidaOTemporalNoLeeCitasNiPacientes(bool temporal)
    {

        // Arrange
        using var e = await PacientesRevision.CrearAsync(postgres);
        var paciente = await e.RegistrarAsync();
        var cookie = "TherapEase.Sesion=invalida";
        if (temporal)
        {
            await ServidorIdentidadRevision.CrearAsync(e.Proveedor, "temporal-de-agenda", "Usuaria", true);
            cookie = await ServidorIdentidadRevision.EntrarAsync(e.Proveedor, "temporal-de-agenda");
        }

        e.Comandos.Limpiar();

        // Act
        var agenda = await e.EnSolicitudAsync(s => s.Obtener<IServicioCitas>().ConsultarAgendaAsync(DateOnly.MaxValue, DateOnly.MaxValue, (VistaDeAgenda)999), cookie, false);
        var alta = await e.EnSolicitudAsync(s => s.Obtener<IServicioCitas>().AgendarAsync(Guid.NewGuid(), Datos(paciente)), cookie, false);
        var detalle = await e.EnSolicitudAsync(s => s.Obtener<IServicioCitas>().ObtenerCitaAsync(Guid.NewGuid()), cookie, false);

        // Assert
        Assert.Equal(MensajesDeCitas.SinPermiso, agenda.Mensaje);
        Assert.Equal(MensajesDeCitas.SinPermiso, alta.Mensaje);
        Assert.Equal(MensajesDeCitas.SinPermiso, detalle.Mensaje);
        Assert.Null(agenda.Datos);
        Assert.Null(alta.Datos);
        Assert.Null(detalle.Datos);
        Assert.DoesNotContain(e.Comandos.Textos, t => t.Contains("FROM \"Cita\"", StringComparison.Ordinal) || t.Contains("FROM \"Paciente\"", StringComparison.Ordinal));
    }

    /// El periodo y la vista se rechazan antes de consultar: fechas máximas, más de 35 días, fecha incompleta o vista inexistente.
    [Theory]
    [InlineData("maxima")]
    [InlineData("larga")]
    [InlineData("incompleta")]
    [InlineData("vista")]
    public async Task PeriodosYVistasInvalidosNoLleganALaConsultaDeCitas(string caso)
    {

        // Arrange
        using var e = await PacientesRevision.CrearAsync(postgres);
        DateOnly? desde = caso == "maxima" ? DateOnly.MaxValue : new DateOnly(2026, 12, 1);
        DateOnly? hasta = caso switch
        {
            "maxima" => DateOnly.MaxValue,
            "larga" => new DateOnly(2027, 1, 5),
            "incompleta" => null,
            _ => desde
        };
        var vista = caso == "vista" ? (VistaDeAgenda)999 : VistaDeAgenda.Activas;
        e.Comandos.Limpiar();

        // Act
        var resultado = await e.EnSolicitudAsync(s => s.Obtener<IServicioCitas>().ConsultarAgendaAsync(desde, hasta, vista));

        // Assert
        Assert.False(resultado.Exito);
        Assert.Equal(MensajesDeCitas.PeriodoInvalido, resultado.Mensaje);
        Assert.DoesNotContain(e.Comandos.Textos, t => t.Contains("FROM \"Cita\"", StringComparison.Ordinal));
    }

    /// Una cita cancelada o de baja no bloquea una nueva reserva; los estados antiguos se preparan solo como datos ficticios.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CitaAnteriorInactivaPermiteReservarElMismoHorario(bool deBaja)
    {

        // Arrange
        using var e = await PacientesRevision.CrearAsync(postgres);
        var paciente = await e.RegistrarAsync();
        var anterior = Guid.NewGuid();
        var primera = await e.EnSolicitudAsync(s => s.Obtener<IServicioCitas>().AgendarAsync(anterior, Datos(paciente)));
        Assert.True(primera.Exito, primera.Mensaje);
        if (deBaja)
        {
            await e.SqlAsync("UPDATE \"Cita\" SET \"Condicion\"='Baja', \"FechaBaja\"=@fecha, \"IdUsuarioBaja\"=@actor WHERE \"IdCita\"=@id",
                ("fecha", e.Reloj.GetUtcNow()), ("actor", e.Actor), ("id", anterior));
        }
        else
        {
            await e.SqlAsync("UPDATE \"Cita\" SET \"Estado\"='Cancelada' WHERE \"IdCita\"=@id", ("id", anterior));
        }

        // Act
        var nueva = await e.EnSolicitudAsync(s => s.Obtener<IServicioCitas>().AgendarAsync(Guid.NewGuid(), Datos(paciente)));

        // Assert
        Assert.True(nueva.Exito, nueva.Mensaje);
        await using var db = e.Escenario.Contexto();
        Assert.Equal(2, await db.Citas.CountAsync());
        Assert.Equal(1, await db.Citas.CountAsync(c => c.Estado == TherapEase.Domain.Citas.Entidades.Enums.EstadoCita.Agendada
            && c.Condicion == TherapEase.Domain.Compartido.Entidades.Enums.CondicionRegistro.Vigente));
        Assert.Equal(2, await db.EventosAuditoria.CountAsync(v => v.TipoRegistro == TherapEase.Domain.Auditoria.Entidades.Enums.TipoRegistroAuditoria.Cita));
    }

    /// Una cita de fecha pasada se guarda y la marca y el evento indican que fue registrada después de su horario.
    [Fact]
    public async Task CitaPasadaConservaHorarioYMarcaDeRegistroTardio()
    {

        // Arrange
        using var e = await PacientesRevision.CrearAsync(postgres);
        var paciente = await e.RegistrarAsync();
        var fecha = new DateOnly(2021, 4, 4);

        // Act
        var alta = await e.EnSolicitudAsync(s => s.Obtener<IServicioCitas>().AgendarAsync(Guid.NewGuid(), Datos(paciente) with { Fecha = fecha }));
        var agenda = await e.EnSolicitudAsync(s => s.Obtener<IServicioCitas>().ConsultarAgendaAsync(fecha, fecha));

        // Assert
        Assert.True(alta.Exito, alta.Mensaje);
        Assert.True(alta.Datos!.Cita.RegistradaDespuesDeSuHorario);
        Assert.True(Assert.Single(agenda.Datos!.Citas).RegistradaDespuesDeSuHorario);
        await using var db = e.Escenario.Contexto();
        var cita = await db.Citas.SingleAsync();
        Assert.Equal(new DateTimeOffset(2021, 4, 4, 17, 0, 0, TimeSpan.Zero), cita.Inicio);
        var evento = await db.EventosAuditoria.SingleAsync(v => v.TipoRegistro == TherapEase.Domain.Auditoria.Entidades.Enums.TipoRegistroAuditoria.Cita);
        Assert.Equal("Se agendó una cita registrada después de su horario.", evento.Hecho);
    }
}
