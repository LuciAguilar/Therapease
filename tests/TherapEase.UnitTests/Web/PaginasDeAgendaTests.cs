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
        // Arrange
        var agenda = new ResultadoDeAgenda([], Dia, Dia.AddDays(6), Dia, vista, hayMas);
        var servicio = new Citas { Agenda = RespuestaServicio<ResultadoDeAgenda>.Correcta(agenda) };
        var pagina = new IndexModel(servicio) { PageContext = Contexto(), Vista = vista };
        if (vista == VistaDeAgenda.Canceladas)
        {
            pagina.Desde = Dia;
            pagina.Hasta = Dia.AddDays(6);
        }

        var filtro = (pagina.Desde, pagina.Hasta, vista);

        // Act
        var resultado = await pagina.OnGetAsync();

        // Assert
        Assert.IsType<PageResult>(resultado);

        Assert.Equal(filtro, servicio.Filtro);
        Assert.Same(agenda, pagina.Agenda);
        Assert.Equal(agenda.Desde, pagina.Desde);
        Assert.Equal(agenda.Hasta, pagina.Hasta);
        Assert.Null(pagina.Error);
    }

    [Fact]
    public async Task AgendaRechazaUnFiltroMalFormadoSinConsultar()
    {
        // Arrange
        var servicio = new Citas();
        var pagina = new IndexModel(servicio) { PageContext = Contexto() };
        pagina.ModelState.AddModelError(nameof(pagina.Desde), "Fecha inválida");

        // Act
        var resultado = await pagina.OnGetAsync();

        // Assert
        Assert.IsType<PageResult>(resultado);
        Assert.Null(servicio.Filtro);
        Assert.Equal(MensajesDeCitas.PeriodoInvalido, pagina.Error);
    }

    [Fact]
    public async Task DetalleRespondeConElMensajeFijoY404CuandoNoEncuentraLaCita()
    {
        // Arrange
        var pagina = new DetalleModel(new Citas()) { PageContext = Contexto() };

        // Act
        var resultado = await pagina.OnGetAsync(Guid.NewGuid());

        // Assert
        Assert.IsType<PageResult>(resultado);
        Assert.Null(pagina.Cita);
        Assert.Equal(StatusCodes.Status404NotFound, pagina.Response.StatusCode);
        Assert.Equal(MensajesDeCitas.CitaNoEncontrada, pagina.Error);
    }

    [Fact]
    public async Task BuscarYElegirPacienteConservaElNumeroYLasHorasCapturadas()
    {
        // Arrange
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

        // Act
        var resultadoBusqueda = await pagina.OnPostBuscarAsync();

        // Assert
        Assert.IsType<PageResult>(resultadoBusqueda);
        Assert.Equal("Paciente", pacientes.Texto);
        Assert.Equal(IdPaciente, Assert.Single(pagina.Busqueda!.Pacientes).IdPaciente);

        // Act
        var resultado = await pagina.OnPostElegirAsync(IdPaciente);

        // Assert
        Assert.IsType<PageResult>(resultado);
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
        // Arrange
        var servicio = new Citas();
        var pagina = new CrearModel(servicio, new Pacientes()) { PageContext = Contexto() };

        // Act
        var resultado = await pagina.OnPostAsync();

        // Assert
        Assert.IsType<PageResult>(resultado);
        Assert.Empty(servicio.Envios);
        Assert.Equal(MensajesDeCitas.SolicitudInvalida, pagina.Error);
        Assert.Null(pagina.Confirmacion);
    }

    [Fact]
    public async Task LosCamposObligatoriosAusentesLleganComoNulosAlServicio()
    {
        // Arrange
        var servicio = new Citas
        {
            Alta = RespuestaServicio<ResultadoDeAgendar>.Fallida(MensajesDeCitas.SolicitudInvalida)
        };
        var pagina = new CrearModel(servicio, new Pacientes()) { PageContext = Contexto(), IdSolicitud = Guid.NewGuid() };

        // Act
        var resultado = await pagina.OnPostAsync();

        // Assert
        Assert.IsType<PageResult>(resultado);
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
        // Arrange
        var servicio = new Citas { Alta = RespuestaServicio<ResultadoDeAgendar>.Fallida(mensaje) };
        var pagina = PaginaDeAlta(servicio);
        var envio = (pagina.IdSolicitud, pagina.Entrada.ParaServicio());

        // Act
        var resultado = await pagina.OnPostAsync();

        // Assert
        Assert.IsType<PageResult>(resultado);
        Assert.Equal(envio, Assert.Single(servicio.Envios));
        Assert.Equal(envio, (pagina.IdSolicitud, pagina.Entrada.ParaServicio()));
        Assert.Equal(mensaje, pagina.Error);
        Assert.Null(pagina.Confirmacion);
    }

    [Fact]
    public async Task ReenviarLaMismaReservaConservaSusDatosYDistingueLaConfirmacion()
    {
        // Arrange
        var servicio = new Citas();
        var primera = PaginaDeAlta(servicio);
        var cita = new DetalleDeCita(primera.IdSolicitud, Dia.ToDateTime(new TimeOnly(14, 30)),
            Dia.ToDateTime(new TimeOnly(15, 30)), AmbitoAtencion.Escolar, EstadoCita.Agendada,
            IdPaciente, null, false, true, EstadoPago.Pendiente, 1);
        servicio.Alta = RespuestaServicio<ResultadoDeAgendar>.Correcta(new(EstadoDeAgendar.Agendada, cita));

        // Act
        var resultadoInicial = await primera.OnPostAsync();

        // Assert
        Assert.IsType<PageResult>(resultadoInicial);
        Assert.Equal(MensajesDeCitas.CitaAgendada, primera.Confirmacion);
        Assert.Same(cita, primera.Resultado?.Cita);

        // Arrange
        // Una respuesta perdida se reenvía con la misma solicitud y captura.
        var reenvio = PaginaDeAlta(servicio);
        reenvio.IdSolicitud = primera.IdSolicitud;
        servicio.Alta = RespuestaServicio<ResultadoDeAgendar>.Correcta(new(EstadoDeAgendar.YaAgendada, cita));

        // Act
        var resultadoReenvio = await reenvio.OnPostAsync();

        // Assert
        Assert.IsType<PageResult>(resultadoReenvio);
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
        // Arrange
        var servicio = new Citas
        {
            Agenda = RespuestaServicio<ResultadoDeAgenda>.Fallida(MensajesDeCitas.SinPermiso),
            Detalle = RespuestaServicio<DetalleDeCita>.Fallida(MensajesDeCitas.SinPermiso),
            Alta = RespuestaServicio<ResultadoDeAgendar>.Fallida(MensajesDeCitas.SinPermiso)
        };
        var agenda = new IndexModel(servicio) { PageContext = Contexto() };
        var detalle = new DetalleModel(servicio) { PageContext = Contexto() };
        var alta = PaginaDeAlta(servicio);

        // Act
        var resultadoAgenda = await agenda.OnGetAsync();
        var resultadoDetalle = await detalle.OnGetAsync(Guid.NewGuid());
        var resultadoAlta = await alta.OnPostAsync();

        // Assert
        Assert.IsType<ForbidResult>(resultadoAgenda);
        Assert.IsType<ForbidResult>(resultadoDetalle);
        Assert.IsType<ForbidResult>(resultadoAlta);
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
        // Arrange
        var servicio = new Citas();
        var pagina = PaginaDeAlta(servicio);
        var captura = pagina.Entrada.ParaServicio();
        var solicitud = pagina.IdSolicitud;
        pagina.ModelState.AddModelError("Entrada." + campo, "Valor mal formado");

        // Act
        var resultado = await pagina.OnPostAsync();

        // Assert
        Assert.IsType<PageResult>(resultado);
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
        // Arrange
        var servicio = new Citas();
        var pacientes = new Pacientes();
        var pagina = new CrearModel(servicio, pacientes) { PageContext = Contexto() };

        // Act
        var resultado = await pagina.OnGetAsync(IdPaciente);

        // Assert
        Assert.IsType<PageResult>(resultado);

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
        // Arrange
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

        // Act
        var resultado = await pagina.OnPostBuscarAsync();

        // Assert
        Assert.IsType<PageResult>(resultado);

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
        // Arrange
        var servicio = new Citas();
        var pacientes = new Pacientes { Busqueda = RespuestaServicio<ResultadoDeBusqueda>.Fallida(mensaje) };
        var pagina = new CrearModel(servicio, pacientes) { PageContext = Contexto(), Texto = "Paciente" };

        // Act
        var resultado = await pagina.OnPostBuscarAsync();

        // Assert
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
        // Arrange
        var pagina = PaginaDeAlta(new Citas());
        var nuevo = Guid.NewGuid();
        var captura = pagina.Entrada.ParaServicio();
        var solicitud = pagina.IdSolicitud;
        pagina.ModelState.SetModelValue("Entrada.IdPaciente", IdPaciente.ToString(), IdPaciente.ToString());
        pagina.ModelState.SetModelValue("Entrada.Ambito", "Escolar", "Escolar");

        // El valor ligado anterior no debe reaparecer en el formulario.
        // Act
        var resultado = await pagina.OnPostElegirAsync(nuevo);

        // Assert
        Assert.IsType<PageResult>(resultado);

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
        // Arrange
        var pagina = PaginaDeAlta(new Citas());
        // Act
        var resultado = await pagina.OnPostElegirAsync(IdPaciente);

        // Assert
        Assert.IsType<PageResult>(resultado);
        Assert.Equal(AmbitoAtencion.Escolar, pagina.Entrada.Ambito);
    }

    [Fact]
    public async Task UnaSeleccionVaciaNoReemplazaAlPacienteCapturado()
    {
        // Arrange
        var pagina = PaginaDeAlta(new Citas());
        var captura = pagina.Entrada.ParaServicio();

        // Act
        var resultado = await pagina.OnPostElegirAsync(Guid.Empty);

        // Assert
        Assert.IsType<PageResult>(resultado);

        Assert.Equal(captura, pagina.Entrada.ParaServicio());
        Assert.Equal(MensajesDeCitas.SolicitudInvalida, pagina.Error);
    }

    [Theory]
    [InlineData(MensajesDePacientes.FichaDeBaja)]
    [InlineData(MensajesDePacientes.PacienteNoEncontrado)]
    [InlineData(MensajesDePacientes.SinPermiso)]
    public async Task UnaFichaNoAutorizadaOInaccesibleNoEntregaNombreNiAmbitos(string mensaje)
    {
        // Arrange
        var pacientes = new Pacientes { Lectura = RespuestaServicio<FichaDePaciente>.Fallida(mensaje) };
        var pagina = new CrearModel(new Citas(), pacientes) { PageContext = Contexto() };

        // Act
        var resultado = await pagina.OnGetAsync(IdPaciente);

        // Assert
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
        // Arrange
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

        // Act
        var resultado = await pagina.OnPostAsync();

        // Assert
        Assert.IsType<PageResult>(resultado);

        // La respuesta confirmada prevalece aunque una lectura posterior se deniegue.
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
        // Arrange
        var fallo = new InvalidOperationException("Fallo ficticio");
        var servicio = new Citas { FalloAlta = fallo };
        var pagina = PaginaDeAlta(servicio);
        var solicitud = pagina.IdSolicitud;
        var captura = pagina.Entrada.ParaServicio();

        // Act
        var excepcion = await Assert.ThrowsAsync<InvalidOperationException>(() => pagina.OnPostAsync());

        // Assert
        Assert.Same(fallo, excepcion);
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
        // Arrange
        var hasta = Dia.AddDays(dias - 1);
        var agenda = new ResultadoDeAgenda([], Dia, hasta, Dia, VistaDeAgenda.Activas, true);
        var servicio = new Citas { Agenda = RespuestaServicio<ResultadoDeAgenda>.Correcta(agenda) };
        var pagina = new IndexModel(servicio) { PageContext = Contexto(), Desde = Dia, Hasta = hasta };

        // Act
        var resultado = await pagina.OnGetAsync();

        // Assert
        Assert.IsType<PageResult>(resultado);
        Assert.Equal((Dia, hasta, VistaDeAgenda.Activas), servicio.Filtro);
        Assert.Same(agenda, pagina.Agenda);
        Assert.True(pagina.Agenda!.HayMas);
    }

    [Fact]
    public async Task ElPeriodoRechazadoConservaLosFiltrosSinMostrarAgenda()
    {
        // Arrange
        var pagina = new IndexModel(new Citas())
        {
            PageContext = Contexto(), Desde = Dia, Hasta = Dia.AddDays(40), Vista = VistaDeAgenda.Canceladas
        };

        // Act
        var resultado = await pagina.OnGetAsync();

        // Assert
        Assert.IsType<PageResult>(resultado);
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
        // Arrange
        var cita = new DetalleDeCita(Guid.NewGuid(), Dia.ToDateTime(new(14, 59)),
            Dia.ToDateTime(new(15, 59)), AmbitoAtencion.Escolar, EstadoCita.Cancelada,
            pacienteDeBaja ? null : IdPaciente, pacienteDeBaja ? null : "Paciente ficticio",
            pacienteDeBaja, true, EstadoPago.Pendiente, 1);
        var servicio = new Citas { Detalle = RespuestaServicio<DetalleDeCita>.Correcta(cita) };
        var pagina = new DetalleModel(servicio) { PageContext = Contexto() };

        // Act
        var resultado = await pagina.OnGetAsync(cita.IdCita);

        // Assert
        Assert.IsType<PageResult>(resultado);
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
        // Arrange
        // La teoría proporciona la página y el permiso de la operación.
        // Act
        var politicas = pagina.GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>().Select(atributo => atributo.Policy).ToArray();
        var atributosCache = pagina.GetCustomAttributes(typeof(ResponseCacheAttribute), true)
            .Cast<ResponseCacheAttribute>().ToArray();

        // Assert
        Assert.Contains(permiso, politicas);
        Assert.Contains(PoliticasDeAutorizacion.ConsultarPacientes, politicas);
        var cache = Assert.Single(atributosCache);
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


    [Theory]
    [InlineData(null, EstadoPago.Pagado)]
    [InlineData(0, EstadoPago.Pagado)]
    [InlineData(-1, EstadoPago.Pagado)]
    [InlineData(1, null)]
    [InlineData(1, (EstadoPago)99)]
    [InlineData(null, null)]
    [InlineData(1, (EstadoPago)(-1))]
    public async Task PagoInvalidoConservaEntradaSinGuardar(int? version, EstadoPago? pago)
    {
        var servicio = new Citas();
        var pagina = new PagoModel(servicio) { PageContext = Contexto(), VersionEsperada = version, NuevoPago = pago };
        Assert.IsType<PageResult>(await pagina.OnPostAsync(Guid.NewGuid()));
        Assert.Empty(servicio.Pagos);
        Assert.Equal(version, pagina.VersionEsperada);
        Assert.Equal(pago, pagina.NuevoPago);
        Assert.Equal(MensajesDeCitas.SolicitudInvalida, pagina.Error);
    }
    [Theory]
    [InlineData(EstadoDeCambioDePago.Actualizado, EstadoPago.Pagado)]
    [InlineData(EstadoDeCambioDePago.Actualizado, EstadoPago.Pendiente)]
    [InlineData(EstadoDeCambioDePago.SinCambios, EstadoPago.Pagado)]
    [InlineData(EstadoDeCambioDePago.SinCambios, EstadoPago.Pendiente)]
    public async Task PagoConfirmadoRedirigeSinReleerYConAvisoCorrespondiente(EstadoDeCambioDePago estado, EstadoPago pago)
    {
        var id = Guid.NewGuid();
        var servicio = new Citas { Pago = RespuestaServicio<ResultadoDeCambioDePago>.Correcta(new(estado, new(id, pago, EstadoCita.Cancelada, 8))) };
        var pagina = new PagoModel(servicio) { PageContext = Contexto(), VersionEsperada = 7, NuevoPago = pago };
        var resultado = Assert.IsType<RedirectToPageResult>(await pagina.OnPostAsync(id));
        Assert.Equal("Detalle", resultado.PageName);
        Assert.Equal(id, resultado.RouteValues!["id"]);
        Assert.Equal((id, 7, (EstadoPago?)pago), Assert.Single(servicio.Pagos));
        Assert.Null(servicio.IdConsultado);
        Assert.Single(resultado.RouteValues);
        Assert.Null(pagina.Error);
        Assert.Equal(7, pagina.VersionEsperada);
        Assert.Equal(pago, pagina.NuevoPago);
        Assert.Equal(estado == EstadoDeCambioDePago.Actualizado ? MensajesDeCitas.PagoActualizado : MensajesDeCitas.SinCambios, pagina.Confirmacion);
    }
    [Theory]
    [InlineData(MensajesDeCitas.ConflictoDeVersion, true, 200)]
    [InlineData(MensajesDeCitas.SolicitudInvalida, true, 200)]
    [InlineData(MensajesDeCitas.PagoDeCitaDeBajaNoSeActualiza, false, 200)]
    [InlineData(MensajesDeCitas.CitaNoEncontrada, false, 404)]
    public async Task PagoRechazadoConservaVersionSinReconsulta(string mensaje, bool formulario, int codigo)
    {
        var servicio = new Citas { Pago = RespuestaServicio<ResultadoDeCambioDePago>.Fallida(mensaje) };
        var pagina = new PagoModel(servicio) { PageContext = Contexto(), VersionEsperada = 2, NuevoPago = EstadoPago.Pagado };
        Assert.IsType<PageResult>(await pagina.OnPostAsync(Guid.NewGuid()));
        Assert.Equal(mensaje, pagina.Error);
        Assert.Equal(formulario, pagina.MostrarFormulario);
        Assert.Equal(codigo, pagina.Response.StatusCode);
        Assert.Equal(2, pagina.VersionEsperada);
        Assert.Equal(EstadoPago.Pagado, pagina.NuevoPago);
        Assert.Single(servicio.Pagos);
        Assert.Null(servicio.IdConsultado);
        Assert.Null(pagina.Confirmacion);
        Assert.Equal(mensaje == MensajesDeCitas.ConflictoDeVersion, pagina.HayConflicto);
    }
    [Fact]
    public async Task PagoSinPermisoSeDeniegaSinEntregarDatos()
    {
        var servicio = new Citas { Pago = RespuestaServicio<ResultadoDeCambioDePago>.Fallida(MensajesDeCitas.SinPermiso) };
        var pagina = new PagoModel(servicio) { PageContext = Contexto(), VersionEsperada = 1, NuevoPago = EstadoPago.Pagado };
        Assert.IsType<ForbidResult>(await pagina.OnPostAsync(Guid.NewGuid()));
        Assert.Null(pagina.Error);
        Assert.Null(pagina.Confirmacion);
        Assert.Null(servicio.IdConsultado);
    }
    [Fact]
    public async Task PagoConFalloInesperadoNoReintentaNiConfirma()
    {
        var servicio = new Citas { FalloPago = new InvalidOperationException("Fallo ficticio") };
        var pagina = new PagoModel(servicio) { PageContext = Contexto(), VersionEsperada = 1, NuevoPago = EstadoPago.Pagado };
        await Assert.ThrowsAsync<InvalidOperationException>(() => pagina.OnPostAsync(Guid.NewGuid()));
        Assert.Single(servicio.Pagos);
        Assert.Null(pagina.Confirmacion);
    }
    [Fact]
    public void PagoExigeTresPermisosYDetalleConservaSoloConsulta()
    {
        var politicas = typeof(PagoModel).GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>().Select(a => a.Policy).ToArray();
        Assert.Equal(3, politicas.Length);
        Assert.Contains(PoliticasDeAutorizacion.CambiarPagoDeCitas, politicas);
        Assert.Contains(PoliticasDeAutorizacion.ConsultarAgenda, politicas);
        Assert.Contains(PoliticasDeAutorizacion.ConsultarPacientes, politicas);
        var consulta = typeof(DetalleModel).GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>().Select(a => a.Policy).ToArray();
        Assert.Equal(2, consulta.Length);
        Assert.Contains(PoliticasDeAutorizacion.ConsultarAgenda, consulta);
        Assert.Contains(PoliticasDeAutorizacion.ConsultarPacientes, consulta);
        Assert.DoesNotContain(PoliticasDeAutorizacion.CambiarPagoDeCitas, consulta);
        var cache = Assert.Single(typeof(PagoModel).GetCustomAttributes(typeof(ResponseCacheAttribute), true).Cast<ResponseCacheAttribute>());
        Assert.True(cache.NoStore);
        Assert.Equal(ResponseCacheLocation.None, cache.Location);
    }

    [Theory]
    [InlineData("NuevoPago")]
    [InlineData("VersionEsperada")]
    public async Task PagoMalFormadoNoLlamaAlServicio(string campo)
    {
        var servicio = new Citas();
        var pagina = new PagoModel(servicio) { PageContext = Contexto(), VersionEsperada = 1, NuevoPago = EstadoPago.Pagado };
        pagina.ModelState.AddModelError(campo, "Valor inválido");
        Assert.IsType<PageResult>(await pagina.OnPostAsync(Guid.NewGuid()));
        Assert.Empty(servicio.Pagos);
        Assert.Equal(MensajesDeCitas.SolicitudInvalida, pagina.Error);
    }
    [Fact]
    public async Task PagoConIdentificadorVacioNoConsultaNiGuarda()
    {
        var servicio = new Citas();
        var pagina = new PagoModel(servicio) { PageContext = Contexto(), VersionEsperada = 1, NuevoPago = EstadoPago.Pagado };
        Assert.IsType<PageResult>(await pagina.OnPostAsync(Guid.Empty));
        Assert.Equal(404, pagina.Response.StatusCode);
        Assert.False(pagina.MostrarFormulario);
        Assert.Empty(servicio.Pagos);
    }

    [Fact]
    public void AbrirPagoRedirigeAlDetalleSinConsultarNiGuardar()
    {
        var id = Guid.NewGuid();
        var servicio = new Citas();
        var pagina = new PagoModel(servicio) { PageContext = Contexto() };

        var resultado = Assert.IsType<RedirectToPageResult>(pagina.OnGet(id));

        Assert.Equal("Detalle", resultado.PageName);
        Assert.Equal(id, resultado.RouteValues!["id"]);
        Assert.Null(servicio.IdConsultado);
        Assert.Empty(servicio.Pagos);
        Assert.Null(pagina.Confirmacion);
    }

    [Theory]
    [InlineData(EstadoPago.Pagado)]
    [InlineData(EstadoPago.Pendiente)]
    public async Task TrasGuardarElDetalleConsultaElPagoYLaVersionVigentes(EstadoPago pago)
    {
        var id = Guid.NewGuid();
        var cita = new DetalleDeCita(id, Dia.ToDateTime(new(14, 30)), Dia.ToDateTime(new(15, 30)),
            AmbitoAtencion.Escolar, EstadoCita.Cancelada, null, null, true, false, pago, 12);
        var servicio = new Citas
        {
            Pago = RespuestaServicio<ResultadoDeCambioDePago>.Correcta(new(EstadoDeCambioDePago.Actualizado,
                new(id, pago, EstadoCita.Cancelada, 11))),
            Detalle = RespuestaServicio<DetalleDeCita>.Correcta(cita)
        };
        var edicion = new PagoModel(servicio) { PageContext = Contexto(), VersionEsperada = 10, NuevoPago = pago };
        var redireccion = Assert.IsType<RedirectToPageResult>(await edicion.OnPostAsync(id));
        var detalle = new DetalleModel(servicio) { PageContext = Contexto() };

        Assert.IsType<PageResult>(await detalle.OnGetAsync((Guid)redireccion.RouteValues!["id"]!));

        Assert.Equal(id, servicio.IdConsultado);
        Assert.Same(cita, detalle.Cita);
        Assert.Equal(12, detalle.Cita!.Version);
        Assert.Equal(pago, detalle.Cita.EstadoPago);
        Assert.Equal(EstadoCita.Cancelada, detalle.Cita.Estado);
        Assert.Null(detalle.Cita.NombreDelPaciente);
        Assert.Null(detalle.Cita.IdPaciente);
        Assert.Single(servicio.Pagos);
    }

    [Fact]
    public async Task ConflictoDePagoConservaLosValoresLigadosSinReenviar()
    {
        var servicio = new Citas { Pago = RespuestaServicio<ResultadoDeCambioDePago>.Fallida(MensajesDeCitas.ConflictoDeVersion) };
        var pagina = new PagoModel(servicio) { PageContext = Contexto(), VersionEsperada = 3, NuevoPago = EstadoPago.Pendiente };
        pagina.ModelState.SetModelValue("VersionEsperada", "3", "3");
        pagina.ModelState.SetModelValue("NuevoPago", "Pendiente", "Pendiente");
        pagina.ModelState.MarkFieldValid("VersionEsperada");
        pagina.ModelState.MarkFieldValid("NuevoPago");

        Assert.IsType<PageResult>(await pagina.OnPostAsync(Guid.NewGuid()));

        Assert.True(pagina.HayConflicto);
        Assert.Equal("3", pagina.ModelState["VersionEsperada"]!.AttemptedValue);
        Assert.Equal("Pendiente", pagina.ModelState["NuevoPago"]!.AttemptedValue);
        Assert.Equal(3, pagina.VersionEsperada);
        Assert.Equal(EstadoPago.Pendiente, pagina.NuevoPago);
        Assert.Single(servicio.Pagos);
        Assert.Null(servicio.IdConsultado);
        Assert.Null(pagina.Confirmacion);
    }

    [Fact]
    public async Task DetalleConFalloDeLecturaSePropagaSinMostrarDatosNiReintentar()
    {
        var fallo = new InvalidOperationException("Fallo ficticio de lectura");
        var servicio = new Citas { FalloDetalle = fallo };
        var pagina = new DetalleModel(servicio) { PageContext = Contexto() };

        Assert.Same(fallo, await Assert.ThrowsAsync<InvalidOperationException>(() => pagina.OnGetAsync(Guid.NewGuid())));

        Assert.Null(pagina.Cita);
        Assert.Null(pagina.Error);
        Assert.Null(pagina.Confirmacion);
        Assert.Equal(1, servicio.Lecturas);
        Assert.Empty(servicio.Pagos);
    }


    [Theory]
    [InlineData(EstadoDeCambioDePago.Actualizado)]
    [InlineData(EstadoDeCambioDePago.SinCambios)]
    public async Task PagoEsperaLaRespuestaDelServicioAntesDeConfirmar(EstadoDeCambioDePago estado)
    {
        var id = Guid.NewGuid();
        var respuesta = new TaskCompletionSource<RespuestaServicio<ResultadoDeCambioDePago>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var servicio = new Citas { PagoPendiente = respuesta.Task };
        var pagina = new PagoModel(servicio) { PageContext = Contexto(), VersionEsperada = 1, NuevoPago = EstadoPago.Pagado };

        var envio = pagina.OnPostAsync(id);

        Assert.Single(servicio.Pagos);
        Assert.False(envio.IsCompleted);
        Assert.Null(pagina.Confirmacion);
        Assert.Null(servicio.IdConsultado);
        respuesta.SetResult(RespuestaServicio<ResultadoDeCambioDePago>.Correcta(new(estado,
            new(id, EstadoPago.Pagado, EstadoCita.Agendada, 2))));

        Assert.IsType<RedirectToPageResult>(await envio);
        Assert.Equal(estado == EstadoDeCambioDePago.Actualizado ? MensajesDeCitas.PagoActualizado : MensajesDeCitas.SinCambios,
            pagina.Confirmacion);
    }

    private sealed class Citas : IServicioCitas
    {
        public (DateOnly? Desde, DateOnly? Hasta, VistaDeAgenda Vista)? Filtro { get; private set; }
        public List<(Guid Id, DatosDeCitaEntrada Datos)> Envios { get; } = [];
        public Exception? FalloAlta { get; init; }
        public Guid? IdConsultado { get; private set; }
        public Exception? FalloDetalle { get; init; }
        public int Lecturas { get; private set; }
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
            Lecturas++;
            if (FalloDetalle is not null) throw FalloDetalle;
            return Task.FromResult(Detalle);
        }

        public Task<RespuestaServicio<ResultadoDeAgendar>> AgendarAsync(
            Guid idSolicitud, DatosDeCitaEntrada datos, CancellationToken cancelacion)
        {
            Envios.Add((idSolicitud, datos));
            if (FalloAlta is not null) throw FalloAlta;
            return Task.FromResult(Alta);
        }

        public RespuestaServicio<ResultadoDeCambioDePago> Pago { get; set; } = RespuestaServicio<ResultadoDeCambioDePago>.Fallida(MensajesDeCitas.SolicitudInvalida);
        public Exception? FalloPago { get; set; }
        public Task<RespuestaServicio<ResultadoDeCambioDePago>>? PagoPendiente { get; init; }
        public List<(Guid Id, int Version, EstadoPago? Pago)> Pagos { get; } = [];
        public Task<RespuestaServicio<ResultadoDeCambioDePago>> CambiarPagoAsync(Guid idCita, int versionEsperada, EstadoPago? nuevoPago, CancellationToken cancelacion)
        {
            Pagos.Add((idCita, versionEsperada, nuevoPago));
            if (FalloPago is not null) throw FalloPago;
            return PagoPendiente ?? Task.FromResult(Pago);
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
