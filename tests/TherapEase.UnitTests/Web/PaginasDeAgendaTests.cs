using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
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
using TherapEase.Web.Seguridad;

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

    [Theory]
    [InlineData(nameof(DatosDelFormularioDeCita.IdPaciente))]
    [InlineData(nameof(DatosDelFormularioDeCita.Ambito))]
    [InlineData(nameof(DatosDelFormularioDeCita.Fecha))]
    [InlineData(nameof(DatosDelFormularioDeCita.HoraInicio))]
    [InlineData(nameof(DatosDelFormularioDeCita.HoraFin))]
    public async Task UnCampoMalFormadoConservaLaCapturaSinIntentarGuardar(string campo)
    {
        var servicio = new Citas();
        var pagina = PaginaDeAlta(servicio);
        var captura = pagina.Entrada.ParaServicio();
        var solicitud = pagina.IdSolicitud;
        pagina.ModelState.AddModelError("Entrada." + campo, "Valor mal formado");

        Assert.IsType<PageResult>(await pagina.OnPostAsync());
        Assert.Empty(servicio.Envios);
        Assert.Equal(captura, pagina.Entrada.ParaServicio());
        Assert.Equal(solicitud, pagina.IdSolicitud);
        Assert.Null(pagina.Resultado);
        Assert.Null(pagina.Confirmacion);
        Assert.Equal(MensajesDeCitas.SolicitudInvalida, pagina.Error);
    }

    [Fact]
    public async Task AbrirDesdeLaFichaCargaSoloElPacienteElegidoSinAgendar()
    {
        var servicio = new Citas();
        var pacientes = new Pacientes();
        var pagina = new CrearModel(servicio, pacientes) { PageContext = Contexto() };

        Assert.IsType<PageResult>(await pagina.OnGetAsync(IdPaciente));

        Assert.Equal(IdPaciente, Assert.Single(pacientes.Lecturas));
        Assert.Equal(IdPaciente, pagina.Entrada.IdPaciente);
        Assert.NotEqual(Guid.Empty, pagina.IdSolicitud);
        Assert.Equal("Paciente ficticio", pagina.NombreDelPaciente);
        Assert.Equal(AmbitoAtencion.Escolar, Assert.Single(pagina.Ambitos));
        Assert.Null(pagina.Entrada.Ambito);
        Assert.Empty(servicio.Envios);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LaBusquedaVaciaOTruncadaConservaLaReserva(bool hayMas)
    {
        var pacientes = new Pacientes
        {
            Busqueda = RespuestaServicio<ResultadoDeBusqueda>.Correcta(new([], hayMas))
        };
        var servicio = new Citas();
        var pagina = new CrearModel(servicio, pacientes)
        {
            PageContext = Contexto(), IdSolicitud = Guid.NewGuid(), Texto = "Paciente",
            Entrada = new() { Fecha = Dia, HoraInicio = new(14, 59), HoraFin = new(15, 59) }
        };
        var solicitud = pagina.IdSolicitud;
        var captura = pagina.Entrada.ParaServicio();

        Assert.IsType<PageResult>(await pagina.OnPostBuscarAsync());

        Assert.Empty(pagina.Busqueda!.Pacientes);
        Assert.Equal(hayMas, pagina.Busqueda.HayMas);
        Assert.Equal(captura, pagina.Entrada.ParaServicio());
        Assert.Equal(solicitud, pagina.IdSolicitud);
        Assert.Empty(servicio.Envios);
        Assert.Null(pagina.Confirmacion);
    }

    [Theory]
    [InlineData(MensajesDePacientes.BusquedaInvalida)]
    [InlineData(MensajesDePacientes.SinPermiso)]
    public async Task LaBusquedaRechazadaNoExponeResultadosNiGuarda(string mensaje)
    {
        var servicio = new Citas();
        var pacientes = new Pacientes { Busqueda = RespuestaServicio<ResultadoDeBusqueda>.Fallida(mensaje) };
        var pagina = new CrearModel(servicio, pacientes) { PageContext = Contexto(), Texto = "Paciente" };

        var resultado = await pagina.OnPostBuscarAsync();

        if (mensaje == MensajesDePacientes.SinPermiso) Assert.IsType<ForbidResult>(resultado);
        else
        {
            Assert.IsType<PageResult>(resultado);
            Assert.Equal(mensaje, pagina.Error);
        }
        Assert.Null(pagina.Busqueda);
        Assert.Empty(servicio.Envios);
    }

    [Fact]
    public async Task CambiarPacienteRetiraElAmbitoAnteriorYElValorLigadoDelFormulario()
    {
        var pagina = PaginaDeAlta(new Citas());
        var nuevo = Guid.NewGuid();
        var captura = pagina.Entrada.ParaServicio();
        var solicitud = pagina.IdSolicitud;
        pagina.ModelState.SetModelValue("Entrada.IdPaciente", IdPaciente.ToString(), IdPaciente.ToString());
        pagina.ModelState.SetModelValue("Entrada.Ambito", "Escolar", "Escolar");

        Assert.IsType<PageResult>(await pagina.OnPostElegirAsync(nuevo));

        Assert.Equal(nuevo, pagina.Entrada.IdPaciente);
        Assert.Null(pagina.Entrada.Ambito);
        Assert.False(pagina.ModelState.ContainsKey("Entrada.IdPaciente"));
        Assert.False(pagina.ModelState.ContainsKey("Entrada.Ambito"));
        Assert.Equal(solicitud, pagina.IdSolicitud);
        Assert.Equal(captura.Fecha, pagina.Entrada.Fecha);
        Assert.Equal(captura.HoraInicio, pagina.Entrada.HoraInicio);
        Assert.Equal(captura.HoraFin, pagina.Entrada.HoraFin);
    }

    [Fact]
    public async Task ElegirDeNuevoAlMismoPacienteConservaElAmbito()
    {
        var pagina = PaginaDeAlta(new Citas());
        Assert.IsType<PageResult>(await pagina.OnPostElegirAsync(IdPaciente));
        Assert.Equal(AmbitoAtencion.Escolar, pagina.Entrada.Ambito);
    }

    [Fact]
    public async Task UnaSeleccionVaciaNoReemplazaAlPacienteCapturado()
    {
        var pagina = PaginaDeAlta(new Citas());
        var captura = pagina.Entrada.ParaServicio();

        Assert.IsType<PageResult>(await pagina.OnPostElegirAsync(Guid.Empty));

        Assert.Equal(captura, pagina.Entrada.ParaServicio());
        Assert.Equal(MensajesDeCitas.SolicitudInvalida, pagina.Error);
    }

    [Theory]
    [InlineData(MensajesDePacientes.FichaDeBaja)]
    [InlineData(MensajesDePacientes.PacienteNoEncontrado)]
    [InlineData(MensajesDePacientes.SinPermiso)]
    public async Task UnaFichaNoAutorizadaOInaccesibleNoEntregaNombreNiAmbitos(string mensaje)
    {
        var pacientes = new Pacientes { Lectura = RespuestaServicio<FichaDePaciente>.Fallida(mensaje) };
        var pagina = new CrearModel(new Citas(), pacientes) { PageContext = Contexto() };

        var resultado = await pagina.OnGetAsync(IdPaciente);

        if (mensaje == MensajesDePacientes.SinPermiso) Assert.IsType<ForbidResult>(resultado);
        else Assert.Equal(mensaje, pagina.Error);
        Assert.Null(pagina.NombreDelPaciente);
        Assert.Empty(pagina.Ambitos);
    }

    [Theory]
    [InlineData(EstadoDeAgendar.Agendada)]
    [InlineData(EstadoDeAgendar.YaAgendada)]
    public async Task UnaReservaConfirmadaSinNombreNoReconsultaAlPacienteNiInventaUnaBaja(EstadoDeAgendar estado)
    {
        var cita = new DetalleDeCita(Guid.NewGuid(), Dia.ToDateTime(new(14, 59)),
            Dia.ToDateTime(new(15, 59)), AmbitoAtencion.Escolar, EstadoCita.Agendada,
            IdPaciente, null, false, true, EstadoPago.Pendiente, 1);
        var servicio = new Citas { Alta = RespuestaServicio<ResultadoDeAgendar>.Correcta(new(estado, cita)) };
        var pacientes = new Pacientes { Lectura = RespuestaServicio<FichaDePaciente>.Fallida(MensajesDePacientes.SinPermiso) };
        var pagina = new CrearModel(servicio, pacientes)
        {
            PageContext = Contexto(), IdSolicitud = cita.IdCita,
            Entrada = PaginaDeAlta(servicio).Entrada
        };

        Assert.IsType<PageResult>(await pagina.OnPostAsync());

        Assert.Empty(pacientes.Lecturas);
        Assert.Same(cita, pagina.Resultado!.Cita);
        Assert.False(pagina.Resultado.Cita.PacienteDeBaja);
        Assert.True(pagina.Resultado.Cita.RegistradaDespuesDeSuHorario);
        Assert.Equal(estado == EstadoDeAgendar.Agendada ? MensajesDeCitas.CitaAgendada : MensajesDeCitas.CitaYaAgendada,
            pagina.Confirmacion);
    }

    [Fact]
    public async Task UnFalloInesperadoDeGuardadoSePropagaSinReintentarNiAnunciarExito()
    {
        var fallo = new InvalidOperationException("Fallo ficticio");
        var servicio = new Citas { FalloAlta = fallo };
        var pagina = PaginaDeAlta(servicio);
        var solicitud = pagina.IdSolicitud;
        var captura = pagina.Entrada.ParaServicio();

        Assert.Same(fallo, await Assert.ThrowsAsync<InvalidOperationException>(() => pagina.OnPostAsync()));
        Assert.Single(servicio.Envios);
        Assert.Null(pagina.Confirmacion);
        Assert.Null(pagina.Resultado);
        Assert.Null(pagina.Error);
        Assert.Equal(solicitud, pagina.IdSolicitud);
        Assert.Equal(captura, pagina.Entrada.ParaServicio());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(28)]
    [InlineData(31)]
    [InlineData(35)]
    public async Task ConsultarUnPeriodoConservaLasFechasLocalesYElLimiteInformado(int dias)
    {
        var hasta = Dia.AddDays(dias - 1);
        var agenda = new ResultadoDeAgenda([], Dia, hasta, Dia, VistaDeAgenda.Activas, true);
        var servicio = new Citas { Agenda = RespuestaServicio<ResultadoDeAgenda>.Correcta(agenda) };
        var pagina = new IndexModel(servicio) { PageContext = Contexto(), Desde = Dia, Hasta = hasta };

        Assert.IsType<PageResult>(await pagina.OnGetAsync());
        Assert.Equal((Dia, hasta, VistaDeAgenda.Activas), servicio.Filtro);
        Assert.Same(agenda, pagina.Agenda);
        Assert.True(pagina.Agenda!.HayMas);
    }

    [Fact]
    public async Task ElPeriodoRechazadoConservaLosFiltrosSinMostrarAgenda()
    {
        var pagina = new IndexModel(new Citas())
        {
            PageContext = Contexto(), Desde = Dia, Hasta = Dia.AddDays(40), Vista = VistaDeAgenda.Canceladas
        };

        Assert.IsType<PageResult>(await pagina.OnGetAsync());
        Assert.Equal(MensajesDeCitas.PeriodoInvalido, pagina.Error);
        Assert.Null(pagina.Agenda);
        Assert.Equal(Dia, pagina.Desde);
        Assert.Equal(Dia.AddDays(40), pagina.Hasta);
        Assert.Equal(VistaDeAgenda.Canceladas, pagina.Vista);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ElDetalleConservaLasHorasYLaPrivacidadDevueltasPorElServicio(bool pacienteDeBaja)
    {
        var cita = new DetalleDeCita(Guid.NewGuid(), Dia.ToDateTime(new(14, 59)),
            Dia.ToDateTime(new(15, 59)), AmbitoAtencion.Escolar, EstadoCita.Cancelada,
            pacienteDeBaja ? null : IdPaciente, pacienteDeBaja ? null : "Paciente ficticio",
            pacienteDeBaja, true, EstadoPago.Pendiente, 1);
        var servicio = new Citas { Detalle = RespuestaServicio<DetalleDeCita>.Correcta(cita) };
        var pagina = new DetalleModel(servicio) { PageContext = Contexto() };

        Assert.IsType<PageResult>(await pagina.OnGetAsync(cita.IdCita));
        Assert.Same(cita, pagina.Cita);
        Assert.Equal(cita.IdCita, servicio.IdConsultado);
        Assert.Null(pagina.Error);
        Assert.Equal(StatusCodes.Status200OK, pagina.Response.StatusCode);
    }

    [Theory]
    [InlineData(typeof(IndexModel), PoliticasDeAutorizacion.ConsultarAgenda)]
    [InlineData(typeof(DetalleModel), PoliticasDeAutorizacion.ConsultarAgenda)]
    [InlineData(typeof(CrearModel), PoliticasDeAutorizacion.AgendarCitas)]
    public void LasPaginasExigenAmbosPermisosYEvitanCachePrivada(Type pagina, string permiso)
    {
        var politicas = pagina.GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>().Select(atributo => atributo.Policy).ToArray();
        Assert.Contains(permiso, politicas);
        Assert.Contains(PoliticasDeAutorizacion.ConsultarPacientes, politicas);
        var cache = Assert.Single(pagina.GetCustomAttributes(typeof(ResponseCacheAttribute), true)
            .Cast<ResponseCacheAttribute>());
        Assert.True(cache.NoStore);
        Assert.Equal(ResponseCacheLocation.None, cache.Location);
    }

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
        public Exception? FalloAlta { get; init; }
        public Guid? IdConsultado { get; private set; }
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

        public Task<RespuestaServicio<DetalleDeCita>> ObtenerCitaAsync(Guid idCita, CancellationToken cancelacion)
        {
            IdConsultado = idCita;
            return Task.FromResult(Detalle);
        }

        public Task<RespuestaServicio<ResultadoDeAgendar>> AgendarAsync(
            Guid idSolicitud, DatosDeCitaEntrada datos, CancellationToken cancelacion)
        {
            Envios.Add((idSolicitud, datos));
            if (FalloAlta is not null) throw FalloAlta;
            return Task.FromResult(Alta);
        }
    }

    private sealed class Pacientes : IServicioPacientes
    {
        public string? Texto { get; private set; }
        public List<Guid> Lecturas { get; } = [];
        public RespuestaServicio<FichaDePaciente>? Lectura { get; init; }
        public RespuestaServicio<ResultadoDeBusqueda> Busqueda { get; init; } =
            RespuestaServicio<ResultadoDeBusqueda>.Fallida(MensajesDePacientes.BusquedaInvalida);

        public Task<RespuestaServicio<ResultadoDeBusqueda>> BuscarAsync(
            string? texto, DateOnly? nacimientoExacto, CancellationToken cancelacion)
        {
            Texto = texto;
            return Task.FromResult(Busqueda);
        }

        public Task<RespuestaServicio<FichaDePaciente>> ObtenerAsync(Guid idPaciente, CancellationToken cancelacion)
        {
            Lecturas.Add(idPaciente);
            return Task.FromResult(Lectura ?? RespuestaServicio<FichaDePaciente>.Correcta(
                new FichaDePaciente(idPaciente, 1, "Paciente ficticio", null, null, null, null, null, null,
                    [AmbitoAtencion.Escolar], null)));
        }

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
