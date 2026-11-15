using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TherapEase.Application.Citas.Constantes;
using TherapEase.Application.Citas.Interfaces.Servicios;
using TherapEase.Application.Citas.Modelos;
using TherapEase.Application.Compartido.Modelos;
using TherapEase.Application.Pacientes.Constantes;
using TherapEase.Application.Pacientes.Interfaces.Servicios;
using TherapEase.Application.Pacientes.Modelos;
using TherapEase.Domain.Citas.Entidades.Enums;
using TherapEase.Domain.Pacientes.Entidades.Enums;
using TherapEase.Web.Pages.Agenda;

namespace TherapEase.UnitTests.Web;

public class PaginasDeAgendaTests
{
    private static readonly DateOnly Dia = new(2026, 11, 15);
    private static readonly Guid IdPaciente = Guid.NewGuid();

    [Theory]
    [InlineData(VistaDeAgenda.Activas, false)]
    [InlineData(VistaDeAgenda.Canceladas, true)]
    public async Task AgendaEnviaElFiltroYConservaElPeriodoDevuelto(VistaDeAgenda vista, bool hayMas)
    {
        var agenda = new ResultadoDeAgenda([], Dia, Dia.AddDays(6), Dia, vista, hayMas);
        var servicio = new Citas { Agenda = RespuestaServicio<ResultadoDeAgenda>.Correcta(agenda) };
        var pagina = new IndexModel(servicio) { PageContext = Contexto(), Vista = vista };
        if (vista == VistaDeAgenda.Canceladas)
        {
            pagina.Desde = Dia;
            pagina.Hasta = Dia.AddDays(6);
        }

        var filtro = (pagina.Desde, pagina.Hasta, vista);
        Assert.IsType<PageResult>(await pagina.OnGetAsync());

        Assert.Equal(filtro, servicio.Filtro);
        Assert.Same(agenda, pagina.Agenda);
        Assert.Equal(agenda.Desde, pagina.Desde);
        Assert.Equal(agenda.Hasta, pagina.Hasta);
        Assert.Null(pagina.Error);
    }

    [Fact]
    public async Task AgendaRechazaUnFiltroMalFormadoSinConsultar()
    {
        var servicio = new Citas();
        var pagina = new IndexModel(servicio) { PageContext = Contexto() };
        pagina.ModelState.AddModelError(nameof(pagina.Desde), "Fecha inválida");

        Assert.IsType<PageResult>(await pagina.OnGetAsync());
        Assert.Null(servicio.Filtro);
        Assert.Equal(MensajesDeCitas.PeriodoInvalido, pagina.Error);
    }

    [Fact]
    public async Task DetalleRespondeConElMensajeFijoY404CuandoNoEncuentraLaCita()
    {
        var pagina = new DetalleModel(new Citas()) { PageContext = Contexto() };

        Assert.IsType<PageResult>(await pagina.OnGetAsync(Guid.NewGuid()));
        Assert.Null(pagina.Cita);
        Assert.Equal(StatusCodes.Status404NotFound, pagina.Response.StatusCode);
        Assert.Equal(MensajesDeCitas.CitaNoEncontrada, pagina.Error);
    }

    [Fact]
    public async Task BuscarYElegirPacienteConservaElNumeroYLasHorasCapturadas()
    {
        var servicio = new Citas();
        var pacientes = new Pacientes
        {
            Busqueda = RespuestaServicio<ResultadoDeBusqueda>.Correcta(
                new ResultadoDeBusqueda([new ResumenDePaciente(IdPaciente, "Paciente ficticio")], false))
        };
        var pagina = new CrearModel(servicio, pacientes) { PageContext = Contexto(), Texto = "Paciente" };
        await pagina.OnGetAsync(null);
        var idSolicitud = pagina.IdSolicitud;
        pagina.Entrada.Fecha = Dia;
        pagina.Entrada.HoraInicio = new TimeOnly(14, 30);
        pagina.Entrada.HoraFin = new TimeOnly(15, 30);

        Assert.IsType<PageResult>(await pagina.OnPostBuscarAsync());
        Assert.Equal("Paciente", pacientes.Texto);
        Assert.Equal(IdPaciente, Assert.Single(pagina.Busqueda!.Pacientes).IdPaciente);
        Assert.IsType<PageResult>(await pagina.OnPostElegirAsync(IdPaciente));
        Assert.NotEqual(Guid.Empty, idSolicitud);
        Assert.Equal(idSolicitud, pagina.IdSolicitud);
        Assert.Equal(IdPaciente, pagina.Entrada.IdPaciente);
        Assert.Equal(AmbitoAtencion.Escolar, Assert.Single(pagina.Ambitos));
        Assert.Equal(Dia, pagina.Entrada.Fecha);
        Assert.Equal(new TimeOnly(14, 30), pagina.Entrada.HoraInicio);
        Assert.Equal(new TimeOnly(15, 30), pagina.Entrada.HoraFin);
        Assert.Empty(servicio.Envios);
    }

    [Fact]
    public async Task SinNumeroDeSolicitudNoSeIntentaAgendar()
    {
        var servicio = new Citas();
        var pagina = new CrearModel(servicio, new Pacientes()) { PageContext = Contexto() };

        Assert.IsType<PageResult>(await pagina.OnPostAsync());
        Assert.Empty(servicio.Envios);
        Assert.Equal(MensajesDeCitas.SolicitudInvalida, pagina.Error);
        Assert.Null(pagina.Confirmacion);
    }

    [Fact]
    public async Task LosCamposObligatoriosAusentesLleganComoNulosAlServicio()
    {
        var servicio = new Citas
        {
            Alta = RespuestaServicio<ResultadoDeAgendar>.Fallida(MensajesDeCitas.SolicitudInvalida)
        };
        var pagina = new CrearModel(servicio, new Pacientes()) { PageContext = Contexto(), IdSolicitud = Guid.NewGuid() };

        Assert.IsType<PageResult>(await pagina.OnPostAsync());
        Assert.Equal(new DatosDeCitaEntrada(null, null, null, null, null), Assert.Single(servicio.Envios).Datos);
        Assert.Equal(MensajesDeCitas.SolicitudInvalida, pagina.Error);
        Assert.Null(pagina.Confirmacion);
    }

    [Theory]
    [InlineData(MensajesDeCitas.HorarioOcupado)]
    [InlineData(MensajesDeCitas.AmbitoNoPermitido)]
    [InlineData(MensajesDeCitas.SolicitudYaUsada)]
    public async Task UnRechazoConservaLaCapturaYElIdentificador(string mensaje)
    {
        var servicio = new Citas { Alta = RespuestaServicio<ResultadoDeAgendar>.Fallida(mensaje) };
        var pagina = PaginaDeAlta(servicio);
        var envio = (pagina.IdSolicitud, pagina.Entrada.ParaServicio());

        Assert.IsType<PageResult>(await pagina.OnPostAsync());
        Assert.Equal(envio, Assert.Single(servicio.Envios));
        Assert.Equal(envio, (pagina.IdSolicitud, pagina.Entrada.ParaServicio()));
        Assert.Equal(mensaje, pagina.Error);
        Assert.Null(pagina.Confirmacion);
    }

    [Fact]
    public async Task ReenviarLaMismaReservaConservaSusDatosYDistingueLaConfirmacion()
    {
        var servicio = new Citas();
        var primera = PaginaDeAlta(servicio);
        var cita = new DetalleDeCita(primera.IdSolicitud, Dia.ToDateTime(new TimeOnly(14, 30)),
            Dia.ToDateTime(new TimeOnly(15, 30)), AmbitoAtencion.Escolar, EstadoCita.Agendada,
            IdPaciente, null, false, true, EstadoPago.Pendiente, 1);
        servicio.Alta = RespuestaServicio<ResultadoDeAgendar>.Correcta(new(EstadoDeAgendar.Agendada, cita));

        Assert.IsType<PageResult>(await primera.OnPostAsync());
        Assert.Equal(MensajesDeCitas.CitaAgendada, primera.Confirmacion);
        Assert.Same(cita, primera.Resultado?.Cita);

        var reenvio = PaginaDeAlta(servicio);
        reenvio.IdSolicitud = primera.IdSolicitud;
        servicio.Alta = RespuestaServicio<ResultadoDeAgendar>.Correcta(new(EstadoDeAgendar.YaAgendada, cita));

        Assert.IsType<PageResult>(await reenvio.OnPostAsync());
        Assert.Equal(MensajesDeCitas.CitaYaAgendada, reenvio.Confirmacion);
        Assert.Same(cita, reenvio.Resultado?.Cita);
        Assert.Equal(2, servicio.Envios.Count);
        Assert.Equal(servicio.Envios[0], servicio.Envios[1]);
        Assert.Equal(new TimeOnly(14, 30), servicio.Envios[0].Datos.HoraInicio);
        Assert.Equal(new TimeOnly(15, 30), servicio.Envios[0].Datos.HoraFin);
    }

    [Fact]
    public async Task UnPermisoRetiradoImpideMostrarElResultado()
    {
        var servicio = new Citas
        {
            Agenda = RespuestaServicio<ResultadoDeAgenda>.Fallida(MensajesDeCitas.SinPermiso),
            Detalle = RespuestaServicio<DetalleDeCita>.Fallida(MensajesDeCitas.SinPermiso),
            Alta = RespuestaServicio<ResultadoDeAgendar>.Fallida(MensajesDeCitas.SinPermiso)
        };
        var agenda = new IndexModel(servicio) { PageContext = Contexto() };
        var detalle = new DetalleModel(servicio) { PageContext = Contexto() };
        var alta = PaginaDeAlta(servicio);

        Assert.IsType<ForbidResult>(await agenda.OnGetAsync());
        Assert.IsType<ForbidResult>(await detalle.OnGetAsync(Guid.NewGuid()));
        Assert.IsType<ForbidResult>(await alta.OnPostAsync());
        Assert.Null(agenda.Agenda);
        Assert.Null(detalle.Cita);
        Assert.Null(alta.Resultado);
    }

    private static PageContext Contexto() => new() { HttpContext = new DefaultHttpContext() };

    private static CrearModel PaginaDeAlta(Citas citas) => new(citas, new Pacientes())
    {
        PageContext = Contexto(),
        IdSolicitud = Guid.NewGuid(),
        Entrada = new()
        {
            IdPaciente = IdPaciente,
            Ambito = AmbitoAtencion.Escolar,
            Fecha = Dia,
            HoraInicio = new TimeOnly(14, 30),
            HoraFin = new TimeOnly(15, 30)
        }
    };

    private sealed class Citas : IServicioCitas
    {
        public (DateOnly? Desde, DateOnly? Hasta, VistaDeAgenda Vista)? Filtro { get; private set; }
        public List<(Guid Id, DatosDeCitaEntrada Datos)> Envios { get; } = [];
        public RespuestaServicio<ResultadoDeAgenda> Agenda { get; init; } =
            RespuestaServicio<ResultadoDeAgenda>.Fallida(MensajesDeCitas.PeriodoInvalido);
        public RespuestaServicio<DetalleDeCita> Detalle { get; init; } =
            RespuestaServicio<DetalleDeCita>.Fallida(MensajesDeCitas.CitaNoEncontrada);
        public RespuestaServicio<ResultadoDeAgendar> Alta { get; set; } =
            RespuestaServicio<ResultadoDeAgendar>.Fallida(MensajesDeCitas.SolicitudInvalida);

        public Task<RespuestaServicio<ResultadoDeAgenda>> ConsultarAgendaAsync(
            DateOnly? desde, DateOnly? hasta, VistaDeAgenda vista, CancellationToken cancelacion)
        {
            Filtro = (desde, hasta, vista);
            return Task.FromResult(Agenda);
        }

        public Task<RespuestaServicio<DetalleDeCita>> ObtenerCitaAsync(Guid idCita, CancellationToken cancelacion) =>
            Task.FromResult(Detalle);

        public Task<RespuestaServicio<ResultadoDeAgendar>> AgendarAsync(
            Guid idSolicitud, DatosDeCitaEntrada datos, CancellationToken cancelacion)
        {
            Envios.Add((idSolicitud, datos));
            return Task.FromResult(Alta);
        }
    }

    private sealed class Pacientes : IServicioPacientes
    {
        public string? Texto { get; private set; }
        public RespuestaServicio<ResultadoDeBusqueda> Busqueda { get; init; } =
            RespuestaServicio<ResultadoDeBusqueda>.Fallida(MensajesDePacientes.BusquedaInvalida);

        public Task<RespuestaServicio<ResultadoDeBusqueda>> BuscarAsync(
            string? texto, DateOnly? nacimientoExacto, CancellationToken cancelacion)
        {
            Texto = texto;
            return Task.FromResult(Busqueda);
        }

        public Task<RespuestaServicio<FichaDePaciente>> ObtenerAsync(Guid idPaciente, CancellationToken cancelacion) =>
            Task.FromResult(RespuestaServicio<FichaDePaciente>.Correcta(
                new FichaDePaciente(idPaciente, 1, "Paciente ficticio", null, null, null, null, null, null,
                    [AmbitoAtencion.Escolar], null)));

        public Task<RespuestaServicio<ResultadoDeAlta>> RegistrarAsync(
            Guid idSolicitud, DatosDePacienteEntrada datos, bool confirmaQueEsOtraPersona, CancellationToken cancelacion) =>
            throw new NotSupportedException();

        public Task<RespuestaServicio<PaginaDePacientes>> ListarAsync(
            string? texto, DateOnly? nacimientoExacto, int pagina, CancellationToken cancelacion) =>
            throw new NotSupportedException();

        public Task<RespuestaServicio<ResultadoDeEdicion>> ActualizarAsync(
            Guid idPaciente, int versionEsperada, DatosDePacienteEntrada datos, CancellationToken cancelacion) =>
            throw new NotSupportedException();
    }
}
