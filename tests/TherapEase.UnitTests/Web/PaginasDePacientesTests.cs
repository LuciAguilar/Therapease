using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TherapEase.Application.Compartido.Modelos;
using TherapEase.Application.Pacientes.Constantes;
using TherapEase.Application.Pacientes.Interfaces.Servicios;
using TherapEase.Application.Pacientes.Modelos;
using TherapEase.Domain.Pacientes.Entidades.Enums;
using TherapEase.Web.Pages.Pacientes;

namespace TherapEase.UnitTests.Web;

public class PaginasDePacientesTests
{
    private static readonly Guid IdPaciente = Guid.NewGuid();
    private static readonly DateOnly Nacimiento = new(2015, 2, 15);
    private static readonly DatosDeTutorEntrada Tutor = new("Tutor ficticio", "Madre", "6620000000", null);
    private static readonly FichaDePaciente Ficha = new(
        IdPaciente, 3, "Paciente ficticio", Nacimiento, 11, null, null, null, null,
        [AmbitoAtencion.Escolar, AmbitoAtencion.Independiente], Tutor);

    [Fact]
    public async Task BusquedaConservaFiltrosYPaginaDeSeisEnSeis()
    {
        var filas = Enumerable.Range(1, 7).Select(i => new ResumenDePaciente(Guid.NewGuid(), $"Paciente {i}")).ToList();
        var resultado = new ResultadoDeBusqueda(filas, false);
        var servicio = new Pacientes { Busqueda = RespuestaServicio<ResultadoDeBusqueda>.Correcta(resultado) };
        var pagina = new IndexModel(servicio) { PageContext = Contexto(), Texto = "Paciente", NacimientoExacto = Nacimiento };

        pagina.OnGet();
        Assert.Null(servicio.Filtro);
        Assert.IsType<PageResult>(await pagina.OnPostAsync());
        Assert.Equal(6, pagina.Tarjetas.Count);
        Assert.True(pagina.HaySiguiente);

        Assert.IsType<PageResult>(await pagina.OnPostAsync(2));
        Assert.Equal((pagina.Texto, pagina.NacimientoExacto), servicio.Filtro);
        Assert.Equal(filas[6], Assert.Single(pagina.Tarjetas));
        Assert.False(pagina.HaySiguiente);
    }

    [Fact]
    public async Task BusquedaConFechaMalFormadaNoConsulta()
    {
        var servicio = new Pacientes();
        var pagina = new IndexModel(servicio) { PageContext = Contexto() };
        pagina.ModelState.AddModelError(nameof(IndexModel.NacimientoExacto), "Fecha inválida");

        Assert.IsType<PageResult>(await pagina.OnPostAsync());
        Assert.Null(servicio.Filtro);
        Assert.Equal(MensajesDePacientes.BusquedaInvalida, pagina.Error);
    }

    [Fact]
    public async Task AltaRechazaUnaSolicitudSinIdentificador()
    {
        var servicio = new Pacientes();
        var pagina = new CrearModel(servicio) { PageContext = Contexto() };

        Assert.IsType<PageResult>(await pagina.OnPostAsync());
        Assert.Empty(servicio.Altas);
        Assert.Equal(MensajesDePacientes.SolicitudInvalida, pagina.Error);
    }

    [Theory]
    [InlineData(EstadoDeAlta.Registrado, MensajesDePacientes.PacienteRegistrado)]
    [InlineData(EstadoDeAlta.YaRegistrado, MensajesDePacientes.PacienteYaRegistrado)]
    public async Task AltaConParecidosConservaCapturaYReenviaElMismoIdentificador(EstadoDeAlta estado, string aviso)
    {
        var coincidencias = new ResultadoDeAlta(EstadoDeAlta.CoincidenciasPorConfirmar,
            Coincidencias: [new(IdPaciente, Ficha.Nombre, Nacimiento)], HayMasCoincidencias: true);
        var servicio = new Pacientes
        {
            Alta = new() { Mensaje = MensajesDePacientes.CoincidenciasPorConfirmar, Datos = coincidencias }
        };
        var entrada = DatosDelFormularioDePaciente.DesdeFicha(Ficha);
        var pagina = new CrearModel(servicio) { PageContext = Contexto(), Entrada = entrada };
        pagina.OnGet();
        var idSolicitud = pagina.IdSolicitud;
        Assert.NotEqual(Guid.Empty, idSolicitud);

        Assert.IsType<PageResult>(await pagina.OnPostAsync());
        Assert.Same(coincidencias, pagina.Resultado);
        Assert.Same(entrada, pagina.Entrada);
        Assert.Null(pagina.Confirmacion);

        servicio.Alta = RespuestaServicio<ResultadoDeAlta>.Correcta(new(estado, idSolicitud));
        pagina.ConfirmaQueEsOtraPersona = true;
        var redireccion = Assert.IsType<RedirectToPageResult>(await pagina.OnPostAsync());

        Assert.Collection(servicio.Altas,
            primera =>
            {
                Assert.Equal(idSolicitud, primera.Id);
                Assert.False(primera.Confirmada);
                Assert.Equal(Tutor, primera.Datos.Tutor);
            },
            segunda =>
            {
                Assert.Equal(idSolicitud, segunda.Id);
                Assert.True(segunda.Confirmada);
                Assert.Equal(Ficha.Ambitos, segunda.Datos.Ambitos);
            });
        Assert.Equal("Index", redireccion.PageName);
        Assert.Equal(idSolicitud, redireccion.RouteValues!["ficha"]);
        Assert.Equal(aviso, pagina.Confirmacion);
    }

    [Theory]
    [InlineData(MensajesDePacientes.FichaDeBaja, StatusCodes.Status200OK)]
    [InlineData(MensajesDePacientes.PacienteNoEncontrado, StatusCodes.Status404NotFound)]
    public async Task FichaSinContenidoConservaElMensajeFijo(string mensaje, int estadoHttp)
    {
        var servicio = new Pacientes { Lectura = RespuestaServicio<FichaDePaciente>.Fallida(mensaje) };
        var pagina = new DetalleModel(servicio) { PageContext = Contexto() };

        Assert.IsType<PageResult>(await pagina.OnGetAsync(IdPaciente));
        Assert.Equal(IdPaciente, Assert.Single(servicio.Lecturas));
        Assert.Null(pagina.Ficha);
        Assert.Equal(mensaje, pagina.Error);
        Assert.Equal(estadoHttp, pagina.Response.StatusCode);
    }

    [Fact]
    public async Task DenegacionDelServicioNoExponeDatosEnNingunaOperacion()
    {
        var servicio = new Pacientes();
        var buscar = new IndexModel(servicio) { PageContext = Contexto() };
        var crear = new CrearModel(servicio) { PageContext = Contexto(), IdSolicitud = Guid.NewGuid() };
        var detalle = new DetalleModel(servicio) { PageContext = Contexto() };
        var editar = new EditarModel(servicio) { PageContext = Contexto(), VersionEsperada = 1 };

        Assert.IsType<ForbidResult>(await buscar.OnPostAsync());
        Assert.IsType<ForbidResult>(await crear.OnPostAsync());
        Assert.IsType<ForbidResult>(await detalle.OnGetAsync(IdPaciente));
        Assert.IsType<ForbidResult>(await editar.OnGetAsync(IdPaciente));
        Assert.IsType<ForbidResult>(await editar.OnPostAsync(IdPaciente));
        Assert.Empty(buscar.Tarjetas);
        Assert.Null(crear.Resultado);
        Assert.Null(detalle.Ficha);
        Assert.Null(editar.Confirmacion);
    }

    [Fact]
    public async Task EdicionDeFichaAnteriorConservaElNacimientoDesconocido()
    {
        var servicio = new Pacientes
        {
            Lectura = RespuestaServicio<FichaDePaciente>.Correcta(Ficha with { FechaNacimiento = null, Edad = null })
        };
        var pagina = new EditarModel(servicio) { PageContext = Contexto() };

        Assert.IsType<PageResult>(await pagina.OnGetAsync(IdPaciente));
        Assert.True(pagina.MostrarFormulario);
        Assert.Null(pagina.Entrada.FechaNacimiento);
        Assert.Equal(Ficha.Version, pagina.VersionEsperada);
        Assert.Equal(Tutor, pagina.Entrada.Tutor.ParaServicio());
        Assert.True(pagina.Entrada.Escolar);
        Assert.True(pagina.Entrada.Independiente);
    }

    [Theory]
    [InlineData(EstadoDeEdicion.Actualizado, 4, MensajesDePacientes.DatosActualizados)]
    [InlineData(EstadoDeEdicion.SinCambios, 3, MensajesDePacientes.SinCambios)]
    public async Task EdicionConfirmadaVuelveAlListadoConLaFicha(EstadoDeEdicion estado, int version, string aviso)
    {
        var servicio = new Pacientes
        {
            Edicion = RespuestaServicio<ResultadoDeEdicion>.Correcta(new(estado, Ficha with { Version = version }))
        };
        var pagina = new EditarModel(servicio) { PageContext = Contexto(), VersionEsperada = Ficha.Version };
        pagina.ModelState.SetModelValue(nameof(EditarModel.VersionEsperada), "3", "3");
        pagina.ModelState.MarkFieldValid(nameof(EditarModel.VersionEsperada));

        var destino = Assert.IsType<RedirectToPageResult>(await pagina.OnPostAsync(IdPaciente));
        Assert.Equal(Ficha.Version, servicio.Cambio!.Value.Version);
        Assert.Equal(IdPaciente, servicio.Cambio.Value.Id);
        Assert.Equal("Index", destino.PageName);
        Assert.Equal(IdPaciente, destino.RouteValues!["ficha"]);
        Assert.Equal(aviso, pagina.Confirmacion);
    }

    [Fact]
    public async Task ConflictoConservaCapturaYVersionSinVolverALeer()
    {
        var servicio = new Pacientes
        {
            Edicion = RespuestaServicio<ResultadoDeEdicion>.Fallida(MensajesDePacientes.ConflictoDeVersion)
        };
        var entrada = new DatosDelFormularioDePaciente { Nombre = "Captura ficticia", Escolar = true };
        var pagina = new EditarModel(servicio) { PageContext = Contexto(), Entrada = entrada, VersionEsperada = 2 };

        Assert.IsType<PageResult>(await pagina.OnPostAsync(IdPaciente));
        Assert.True(pagina.HayConflicto);
        Assert.True(pagina.MostrarFormulario);
        Assert.Same(entrada, pagina.Entrada);
        Assert.Equal(2, pagina.VersionEsperada);
        Assert.Equal(entrada.Nombre, servicio.Cambio!.Value.Datos.Nombre);
        Assert.Empty(servicio.Lecturas);
        Assert.Null(pagina.Confirmacion);
    }

    [Fact]
    public async Task EdicionDeBajaNoVuelveAMostrarElFormulario()
    {
        var servicio = new Pacientes
        {
            Edicion = RespuestaServicio<ResultadoDeEdicion>.Fallida(MensajesDePacientes.FichaDeBajaNoSeActualiza)
        };
        var pagina = new EditarModel(servicio) { PageContext = Contexto(), VersionEsperada = 2 };

        Assert.IsType<PageResult>(await pagina.OnPostAsync(IdPaciente));
        Assert.False(pagina.MostrarFormulario);
        Assert.Equal(MensajesDePacientes.FichaDeBajaNoSeActualiza, pagina.Error);
        Assert.Empty(servicio.Lecturas);
    }

    private static PageContext Contexto() => new() { HttpContext = new DefaultHttpContext() };

    private sealed class Pacientes : IServicioPacientes
    {
        public RespuestaServicio<ResultadoDeAlta> Alta { get; set; } = SinPermiso<ResultadoDeAlta>();
        public RespuestaServicio<ResultadoDeBusqueda> Busqueda { get; init; } = SinPermiso<ResultadoDeBusqueda>();
        public RespuestaServicio<FichaDePaciente> Lectura { get; init; } = SinPermiso<FichaDePaciente>();
        public RespuestaServicio<ResultadoDeEdicion> Edicion { get; init; } = SinPermiso<ResultadoDeEdicion>();

        public List<(Guid Id, DatosDePacienteEntrada Datos, bool Confirmada)> Altas { get; } = [];
        public List<Guid> Lecturas { get; } = [];
        public (string? Texto, DateOnly? Nacimiento)? Filtro { get; private set; }
        public (Guid Id, int Version, DatosDePacienteEntrada Datos)? Cambio { get; private set; }

        public Task<RespuestaServicio<ResultadoDeAlta>> RegistrarAsync(
            Guid idSolicitud, DatosDePacienteEntrada datos, bool confirmaQueEsOtraPersona, CancellationToken cancelacion)
        {
            Altas.Add((idSolicitud, datos, confirmaQueEsOtraPersona));
            return Task.FromResult(Alta);
        }

        public Task<RespuestaServicio<ResultadoDeBusqueda>> BuscarAsync(
            string? texto, DateOnly? nacimientoExacto, CancellationToken cancelacion)
        {
            Filtro = (texto, nacimientoExacto);
            return Task.FromResult(Busqueda);
        }

        public Task<RespuestaServicio<FichaDePaciente>> ObtenerAsync(Guid idPaciente, CancellationToken cancelacion)
        {
            Lecturas.Add(idPaciente);
            return Task.FromResult(Lectura);
        }

        public Task<RespuestaServicio<ResultadoDeEdicion>> ActualizarAsync(
            Guid idPaciente, int versionEsperada, DatosDePacienteEntrada datos, CancellationToken cancelacion)
        {
            Cambio = (idPaciente, versionEsperada, datos);
            return Task.FromResult(Edicion);
        }

        private static RespuestaServicio<T> SinPermiso<T>() =>
            RespuestaServicio<T>.Fallida(MensajesDePacientes.SinPermiso);
    }
}
