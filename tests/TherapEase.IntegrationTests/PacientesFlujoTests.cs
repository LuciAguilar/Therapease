using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TherapEase.Application.Coordinadores.Interfaces.Servicios;
using TherapEase.Application.Pacientes.Constantes;
using TherapEase.Application.Pacientes.Interfaces.Servicios;
using TherapEase.Application.Pacientes.Modelos;
using TherapEase.Domain.Auditoria.Entidades.Enums;
using TherapEase.Domain.Citas.Reglas;
using TherapEase.Domain.Compartido.Entidades.Enums;
using TherapEase.Domain.Pacientes.Constantes;
using TherapEase.Domain.Pacientes.Entidades.Enums;
using TherapEase.Infrastructure.Data;
using static TherapEase.IntegrationTests.PacientesRevision;

namespace TherapEase.IntegrationTests;

/// <summary>
/// Pruebas del uso normal de las fichas de pacientes, con el servidor real y una base de datos desechable:
/// registrar, buscar, consultar y editar; datos permitidos y rechazados; edad y tutor;
/// personas con el mismo nombre; fichas dadas de baja; fichas anteriores sin fecha de nacimiento;
/// y tipos de atención sin tocar las citas.
/// Todos los datos son inventados. No usa páginas ni navegador.
/// </summary>
public class PacientesFlujoTests(PostgreSqlRevision postgres) : IClassFixture<PostgreSqlRevision>
{


    // ──── REGISTRAR, CONSULTAR Y EDITAR ────────────────────────────────────────────────────


    /// Usuaria y Superusuario registran, consultan, buscan y editan la ficha de un menor.
    /// Se guarda quién y cuándo lo hizo, y la auditoría y los mensajes del sistema no muestran los datos del paciente.
    [Theory]
    [InlineData("Usuaria")]
    [InlineData("Superusuario")]
    public async Task CadaRolRegistraConsultaYEditaUnMenorConAuditoriaSinValoresPrivados(string rol)
    {
        // Arrange
        using var e = await PacientesRevision.CrearAsync(postgres, rol);
        var datos = Menor();
        var id = await e.RegistrarAsync(datos);

        // Act
        var consulta = await e.ServicioAsync(s => s.ObtenerAsync(id));
        var cambio = await e.ServicioAsync(s => s.ActualizarAsync(id, 1, datos with { Grupo = "Grupo Nuevo Ficticio" }));
        var busqueda = await e.ServicioAsync(s => s.BuscarAsync("menor ejemplo"));

        // Assert: lo guardado se lee con una conexión aparte, no del objeto que se editó.
        Assert.True(consulta.Exito);
        Assert.Equal(10, consulta.Datos!.Edad);
        Assert.Equal("6442345678", consulta.Datos.Tutor!.Telefono);
        Assert.True(cambio.Exito);
        Assert.Equal(id, Assert.Single(busqueda.Datos!.Pacientes).IdPaciente);
        var ficha = (await e.FichaUnicaAsync());
        Assert.Equal(2, ficha.Version);
        Assert.Equal("Grupo Nuevo Ficticio", ficha.Grupo);
        Assert.Equal(e.Actor, ficha.IdUsuarioAlta);
        Assert.Equal(e.Actor, ficha.IdUsuarioActualizacion);
        Assert.Equal(e.Reloj.GetUtcNow(), ficha.FechaAlta);
        Assert.Equal(e.Reloj.GetUtcNow(), ficha.FechaActualizacion);
        var eventos = await e.EventosAsync();
        Assert.Equal(2, eventos.Count);
        Assert.All(eventos, evento => { Assert.Equal(e.Actor, evento.IdUsuarioActor); Assert.Equal(id, evento.IdRegistro); });
        var edicion = Assert.Single(eventos, evento => evento.Accion == AccionAuditoria.Actualizacion);
        Assert.Equal([CamposDePaciente.Grupo], edicion.CamposAfectados);
        var texto = JsonSerializer.Serialize(eventos) + e.Registro.Texto;
        foreach (var privado in new[] { datos.Nombre!, datos.Correo!, datos.Tutor!.Nombre!, datos.Tutor.Correo!,
            "6441234567", "6442345678", "Grupo Nuevo Ficticio", e.Cookie, e.Escenario.App })
            Assert.False(texto.Contains(privado, StringComparison.OrdinalIgnoreCase), "Un registro contiene un valor privado de prueba.");
    }

    /// Guardar sin cambiar nada no escribe en la base de datos.
    /// Reenviar una edición con una versión anterior siempre avisa que la ficha cambió, aunque los datos sean iguales.
    [Fact]
    public async Task SinCambiosNoEscribeYUnaVersionAntiguaSiempreDaConflicto()
    {
        // Arrange
        using var e = await PacientesRevision.CrearAsync(postgres);
        var datos = Adulto();
        var id = await e.RegistrarAsync(datos);
        var antes = await e.HuellaAsync();
        e.Comandos.Limpiar();

        // Act
        var igual = await e.ServicioAsync(s => s.ActualizarAsync(id, 1, datos));

        // Assert
        Assert.True(igual.Exito);
        Assert.Equal(EstadoDeEdicion.SinCambios, igual.Datos!.Estado);
        Assert.Equal(antes, await e.HuellaAsync());
        Assert.DoesNotContain(e.Comandos.Textos, t => t.Contains("UPDATE ", StringComparison.Ordinal) || t.Contains("INSERT ", StringComparison.Ordinal));

        // Act + Assert: se guarda un cambio real y la ficha pasa a la versión 2.
        var cambio = datos with { Grupo = "Grupo Corregido" };
        Assert.True((await e.ServicioAsync(s => s.ActualizarAsync(id, 1, cambio))).Exito);
        antes = await e.HuellaAsync();

        // Act: se reenvía el mismo cambio con la versión 1.
        var antigua = await e.ServicioAsync(s => s.ActualizarAsync(id, 1, cambio));

        // Assert
        Assert.False(antigua.Exito);
        Assert.Equal(MensajesDePacientes.ConflictoDeVersion, antigua.Mensaje);
        Assert.Equal(antes, await e.HuellaAsync());
    }

    /// Consultar o editar un número de ficha vacío o que no existe solo responde «no se encontró»,
    /// sin crear, mostrar ni cambiar ninguna ficha.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task UnIdentificadorVacioOInexistenteNoEntregaFichaNiCambiaDatos(bool vacio)
    {
        // Arrange
        using var e = await PacientesRevision.CrearAsync(postgres);
        await e.RegistrarAsync();
        var antes = await e.HuellaAsync();
        var id = vacio ? Guid.Empty : Guid.NewGuid();

        // Act
        var consulta = await e.ServicioAsync(s => s.ObtenerAsync(id));
        var edicion = await e.ServicioAsync(s => s.ActualizarAsync(id, 1, Adulto()));

        // Assert
        Assert.False(consulta.Exito); Assert.Null(consulta.Datos);
        Assert.Equal(MensajesDePacientes.PacienteNoEncontrado, consulta.Mensaje);
        Assert.False(edicion.Exito); Assert.Null(edicion.Datos);
        Assert.Equal(antes, await e.HuellaAsync());
    }

    /// Quitar un tipo de atención del paciente (por ejemplo, Escolar) no modifica la cita que ya tenía con ese tipo.
    [Fact]
    public async Task RetirarUnAmbitoNoCambiaLaCitaQueYaTeniaEseAmbito()
    {
        // Arrange: la cita se crea directo en la base solo para preparar el caso; esta prueba no revisa la agenda.
        using var e = await PacientesRevision.CrearAsync(postgres);
        var datos = Adulto();
        var id = await e.RegistrarAsync(datos);
        await using (var c = e.Escenario.Contexto())
        {
            var inicio = e.Reloj.GetUtcNow().AddDays(1);
            c.Citas.Add(ReglasDeCita.Agendar(Guid.NewGuid(), id, AmbitoAtencion.Escolar, inicio, inicio.AddHours(1), e.Actor, inicio));
            await c.SaveChangesAsync();
        }
        string anterior;
        await using (var c = e.Escenario.Contexto()) anterior = JsonSerializer.Serialize(await c.Citas.AsNoTracking().ToListAsync());

        // Act
        var edicion = await e.ServicioAsync(s => s.ActualizarAsync(id, 1, datos with { Ambitos = [AmbitoAtencion.Independiente] }));

        // Assert
        Assert.True(edicion.Exito);
        Assert.Equal(AmbitoAtencion.Independiente, Assert.Single((await e.FichaUnicaAsync()).Ambitos).Ambito);
        await using var lectura = e.Escenario.Contexto();
        Assert.Equal(anterior, JsonSerializer.Serialize(await lectura.Citas.AsNoTracking().ToListAsync()));
    }


    // ──── DATOS PERMITIDOS Y RECHAZADOS ────────────────────────────────────────────────────


    /// Datos inválidos de ejemplo: cada caso tiene un nombre para reconocerlo y un solo dato mal.
    public static IEnumerable<object[]> EntradasInvalidas()
    {
        var d = Adulto();
        yield return ["nombre vacío", d with { Nombre = " " }];
        yield return ["nombre corto", d with { Nombre = "A" }];
        yield return ["nombre largo", d with { Nombre = new string('A', 201) }];
        yield return ["nombre con control", d with { Nombre = "Paciente\u0001Ficticio" }];
        yield return ["nacimiento ausente", d with { FechaNacimiento = null }];
        yield return ["nacimiento anterior", d with { FechaNacimiento = new DateOnly(1899, 12, 31) }];
        yield return ["nacimiento futuro", d with { FechaNacimiento = new DateOnly(2026, 10, 29) }];
        yield return ["teléfono corto", d with { Telefono = "123456" }];
        yield return ["teléfono largo", d with { Telefono = "1234567890123456" }];
        yield return ["teléfono con extensión", d with { Telefono = "6441234567 ext 2" }];
        yield return ["correo sin dominio", d with { Correo = "ejemplo@local" }];
        yield return ["correo con punto inicial", d with { Correo = "ejemplo@.ficticio.test" }];
        yield return ["correo con puntos repetidos", d with { Correo = "ejemplo@ficticio..test" }];
        yield return ["correo con punto final", d with { Correo = "ejemplo@ficticio.test." }];
        yield return ["escuela larga", d with { Escuela = new string('E', 151) }];
        yield return ["grupo largo", d with { Grupo = new string('G', 31) }];
        yield return ["sin ámbitos", d with { Ambitos = [] }];
        yield return ["ámbitos repetidos", d with { Ambitos = [AmbitoAtencion.Escolar, AmbitoAtencion.Escolar] }];
        yield return ["ámbito desconocido", d with { Ambitos = [(AmbitoAtencion)87] }];
        yield return ["menor sin tutor", Menor() with { Tutor = null }];
        yield return ["tutor sin nombre", Menor() with { Tutor = new(null, "Madre", "6441234567", null) }];
        yield return ["tutor sin parentesco", Menor() with { Tutor = new("Tutora Ficticia", null, "6441234567", null) }];
        yield return ["tutor sin contacto", Menor() with { Tutor = new("Tutora Ficticia", "Madre", null, null) }];
    }

    /// Un dato inválido se rechaza al registrar y al editar, sin guardar nada a medias
    /// (ni la ficha, ni el cambio, ni su evento de auditoría).
    [Theory]
    [MemberData(nameof(EntradasInvalidas))]
    public async Task UnDatoInvalidoNoDejaAltaNiEdicionParcial(string caso, DatosDePacienteEntrada invalido)
    {
        // Arrange
        Assert.False(string.IsNullOrEmpty(caso)); // El nombre del caso solo sirve para reconocerlo en el resultado.
        using var e = await PacientesRevision.CrearAsync(postgres);
        var id = await e.RegistrarAsync();
        var antes = await e.HuellaAsync();

        // Act
        var alta = await e.ServicioAsync(s => s.RegistrarAsync(Guid.NewGuid(), invalido, true));
        var edicion = await e.ServicioAsync(s => s.ActualizarAsync(id, 1, invalido));

        // Assert
        Assert.False(alta.Exito);
        Assert.Null(alta.Datos);
        Assert.False(edicion.Exito);
        Assert.Null(edicion.Datos);
        Assert.Equal(antes, await e.HuellaAsync());
    }

    /// Los valores en el límite permitido sí se guardan: nombre de 2 y de 200 letras, teléfono de 7 y de 15 dígitos,
    /// nacimiento en 1900, escuela de 150 y grupo de 30. El nombre de paciente admite 200, más que las cuentas (150).
    [Theory]
    [InlineData(2, "1234567")]
    [InlineData(200, "123456789012345")]
    public async Task LosLimitesPermitidosDePacienteSeGuardanSinUsarElLimiteMenorDeCuentas(int longitud, string telefono)
    {
        // Arrange
        using var e = await PacientesRevision.CrearAsync(postgres);
        var datos = Adulto(new string('A', longitud)) with
        {
            FechaNacimiento = new(1900, 1, 1), Telefono = telefono,
            Escuela = new string('E', 150), Grupo = new string('G', 30)
        };

        // Act
        var id = await e.RegistrarAsync(datos);

        // Assert
        var ficha = (await e.ServicioAsync(s => s.ObtenerAsync(id))).Datos!;
        Assert.Equal(longitud, ficha.Nombre.Length);
        Assert.Equal(telefono, ficha.Telefono);
        Assert.Single(await e.EventosAsync());
    }


    // ──── EDAD Y TUTOR ─────────────────────────────────────────────────────────────────────


    /// Se pide tutor hasta el día en que el paciente cumple 18 años; desde ese día ya no.
    /// Quien nació un 29 de febrero cumple el 1 de marzo en los años no bisiestos (regla aprobada).
    [Theory]
    [InlineData(2026, 10, 27, 2008, 10, 28, false)]
    [InlineData(2026, 10, 28, 2008, 10, 28, true)]
    [InlineData(2026, 10, 29, 2008, 10, 28, true)]
    [InlineData(2026, 2, 28, 2008, 2, 29, false)]
    [InlineData(2026, 3, 1, 2008, 2, 29, true)]
    public async Task ElTutorSeExigeHastaCumplirDieciochoIncluidoElNacimientoEnFebrero(
        int anio, int mes, int dia, int anioNacimiento, int mesNacimiento, int diaNacimiento, bool esAdulto)
    {
        // Arrange
        using var e = await PacientesRevision.CrearAsync(postgres, hoy: new(anio, mes, dia));
        var datos = Adulto() with { FechaNacimiento = new(anioNacimiento, mesNacimiento, diaNacimiento) };

        // Act
        var alta = await e.ServicioAsync(s => s.RegistrarAsync(Guid.NewGuid(), datos));

        // Assert
        Assert.Equal(esAdulto, alta.Exito);
        Assert.Equal(esAdulto ? 1 : 0, (await e.FichasAsync()).Count);
        Assert.Equal(esAdulto ? 1 : 0, (await e.EventosAsync()).Count);
    }

    /// Al editar, la fecha de nacimiento y el tutor se revisan juntos: a un menor no se le puede quitar el tutor.
    /// Si la nueva fecha lo hace adulto, el tutor se conserva.
    [Fact]
    public async Task EditarNacimientoOTutorSeValidaJuntoYSuperarLaMayoriaNoBorraAlTutor()
    {
        // Arrange
        using var e = await PacientesRevision.CrearAsync(postgres);
        var datos = Menor();
        var id = await e.RegistrarAsync(datos);
        var antes = await e.HuellaAsync();

        // Act + Assert
        Assert.False((await e.ServicioAsync(s => s.ActualizarAsync(id, 1, datos with { Tutor = null }))).Exito);
        Assert.Equal(antes, await e.HuellaAsync());
        var adulto = await e.ServicioAsync(s => s.ActualizarAsync(id, 1, datos with { FechaNacimiento = new(1990, 5, 8) }));
        Assert.True(adulto.Exito);
        Assert.Equal(datos.Tutor!.Nombre, adulto.Datos!.Ficha.Tutor!.Nombre);
        Assert.Equal(datos.Tutor.Nombre, (await e.FichaUnicaAsync()).TutorNombre);
    }

    /// La edad se calcula cada vez que se consulta: al pasar un año sube en uno, sin editar la ficha ni guardar nada.
    [Fact]
    public async Task LaEdadSeRecalculaAlCambiarDeAnioSinGuardarOtraVersionNiEventoDePaciente()
    {
        // Arrange
        using var e = await PacientesRevision.CrearAsync(postgres);
        var id = await e.RegistrarAsync(Menor());
        var inicial = (await e.ServicioAsync(s => s.ObtenerAsync(id))).Datos!;
        var antes = await e.HuellaAsync();

        // Act: un año después; se abre otra sesión porque la anterior ya caducó.
        e.Reloj.Avanzar(TimeSpan.FromDays(365));
        var cookie = await ServidorIdentidadRevision.EntrarAsync(e.Proveedor, "operador-qa-ficticio");
        e.Reloj.Avanzar(TimeSpan.FromMilliseconds(1));
        var posterior = await e.EnSolicitudAsync(s => s.Obtener<IServicioPacientes>().ObtenerAsync(id), cookie);

        // Assert
        Assert.Equal(inicial.Edad + 1, posterior.Datos!.Edad);
        Assert.Equal(inicial.Version, posterior.Datos.Version);
        Assert.Equal(antes, await e.HuellaAsync());
    }

    /// Una ficha creada antes de pedir la fecha de nacimiento se consulta sin inventar la edad,
    /// y guardarla sin cambios no obliga a completarla.
    /// Un cambio real sí exige poner la fecha de nacimiento y, si resulta menor, el tutor.
    [Fact]
    public async Task LaFichaAnteriorNoInventaEdadPeroUnaEdicionRealExigeCompletarNacimientoYTutor()
    {
        // Arrange: simula una ficha que existía antes de pedir la fecha de nacimiento.
        using var e = await PacientesRevision.CrearAsync(postgres);
        var datos = Adulto();
        var id = await e.RegistrarAsync(datos);
        await e.SqlAsync("UPDATE \"Paciente\" SET \"FechaNacimiento\"=NULL WHERE \"IdPaciente\"=@id", ("id", id));
        datos = datos with { FechaNacimiento = null, Telefono = "6441234567" };
        var antes = await e.HuellaAsync();

        // Act + Assert
        Assert.Null((await e.ServicioAsync(s => s.ObtenerAsync(id))).Datos!.Edad);
        Assert.Equal(EstadoDeEdicion.SinCambios, (await e.ServicioAsync(s => s.ActualizarAsync(id, 1, datos))).Datos!.Estado);
        Assert.False((await e.ServicioAsync(s => s.ActualizarAsync(id, 1, datos with { Grupo = "Otro Grupo" }))).Exito);
        Assert.False((await e.ServicioAsync(s => s.ActualizarAsync(id, 1, datos with { FechaNacimiento = new(2016, 4, 9) }))).Exito);
        Assert.Equal(antes, await e.HuellaAsync());
        Assert.True((await e.ServicioAsync(s => s.ActualizarAsync(id, 1, Menor()))).Exito);
        Assert.Equal(2, (await e.EventosAsync()).Count);
    }


    // ──── PERSONAS CON EL MISMO NOMBRE Y BÚSQUEDA ──────────────────────────────────────────


    /// Dos o más personas pueden llamarse igual, incluso con la misma fecha de nacimiento.
    /// El sistema avisa antes de registrar y, al buscar, muestra la fecha de nacimiento para distinguirlas.
    [Fact]
    public async Task LosHomonimosSeConfirmanYSeEligenPorIdentificadorSinUnicidadPorNacimiento()
    {
        // Arrange
        using var e = await PacientesRevision.CrearAsync(postgres);
        var datos = Adulto("María Muñoz Ficticia");
        var primero = await e.RegistrarAsync(datos);
        var segundo = Guid.NewGuid();
        var antes = await e.HuellaAsync();

        // Act + Assert: el aviso todavía no registra a la segunda persona.
        var aviso = await e.ServicioAsync(s => s.RegistrarAsync(segundo, datos with { FechaNacimiento = new(1991, 6, 9) }));
        Assert.False(aviso.Exito);
        Assert.Equal(EstadoDeAlta.CoincidenciasPorConfirmar, aviso.Datos!.Estado);
        Assert.Equal(primero, Assert.Single(aviso.Datos.Coincidencias!).IdPaciente);
        Assert.Equal(antes, await e.HuellaAsync());
        Assert.True((await e.ServicioAsync(s => s.RegistrarAsync(segundo, datos with { FechaNacimiento = new(1991, 6, 9) }, true))).Exito);
        var tercero = await e.RegistrarAsync(datos, confirmar: true);
        var busqueda = await e.ServicioAsync(s => s.BuscarAsync("MUNOZ MARIA"));
        Assert.Equal(3, busqueda.Datos!.Pacientes.Count);
        Assert.All(busqueda.Datos.Pacientes, p => Assert.NotNull(p.FechaNacimiento));
        var exacta = await e.ServicioAsync(s => s.BuscarAsync("maria", datos.FechaNacimiento));
        Assert.Equal(2, exacta.Datos!.Pacientes.Count);
        Assert.Equal(tercero, (await e.ServicioAsync(s => s.ObtenerAsync(tercero))).Datos!.IdPaciente);
        Assert.Equal(3, (await e.EventosAsync()).Count);
    }

    /// Una búsqueda con más de 50 resultados muestra solo 50, avisa que hay más
    /// y da siempre el mismo orden, sin importar mayúsculas.
    /// Si los nombres no se repiten, no muestra fechas de nacimiento.
    [Fact]
    public async Task ElLimiteDeBusquedaEsEstableIndicaQueHayMasYNoMuestraNacimientosInnecesarios()
    {
        // Arrange
        using var e = await PacientesRevision.CrearAsync(postgres);
        for (var i = 0; i < 51; i++) await e.RegistrarAsync(Adulto($"Serie Ficticia {i:D2}"));
        var antes = await e.HuellaAsync();

        // Act
        var primera = (await e.ServicioAsync(s => s.BuscarAsync("serie ficticia"))).Datos!;
        var segunda = (await e.ServicioAsync(s => s.BuscarAsync("SERIE FICTICIA"))).Datos!;

        // Assert
        Assert.Equal(50, primera.Pacientes.Count);
        Assert.True(primera.HayMas);
        Assert.Equal(primera.Pacientes.Select(p => p.IdPaciente), segunda.Pacientes.Select(p => p.IdPaciente));
        Assert.All(primera.Pacientes, p => Assert.Null(p.FechaNacimiento));
        Assert.Equal(antes, await e.HuellaAsync());
    }


    // ──── FICHAS DADAS DE BAJA ─────────────────────────────────────────────────────────────


    /// Una ficha dada de baja no aparece al buscar y no se puede editar.
    /// Al abrirla solo se informa «Está dada de baja», con una sola consulta a la base de datos.
    /// Sin sesión, se rechaza antes de decir nada sobre la ficha.
    [Fact]
    public async Task LaBajaQuedaFueraDeBusquedaYAlAbrirlaSoloDevuelveSuCondicion()
    {
        // Arrange
        using var e = await PacientesRevision.CrearAsync(postgres);
        var id = await e.RegistrarAsync();
        var baja = await e.EnSolicitudAsync(s => s.Obtener<ICoordinadorDeBajaDePaciente>().DarDeBajaAsync(id, 1));
        Assert.True(baja.Exito);
        var antes = await e.HuellaAsync();
        e.Comandos.Limpiar();

        // Act
        var ficha = await e.ServicioAsync(s => s.ObtenerAsync(id));
        var consultas = e.Comandos.Textos.Count(t => t.Contains("FROM \"Paciente\"", StringComparison.Ordinal));
        var lista = await e.ServicioAsync(s => s.BuscarAsync("ejemplo"));
        var edicion = await e.ServicioAsync(s => s.ActualizarAsync(id, 2, Adulto("Cambio Rechazado")));

        // Assert
        Assert.False(ficha.Exito);
        Assert.Null(ficha.Datos);
        Assert.Equal(MensajesDePacientes.FichaDeBaja, ficha.Mensaje);
        Assert.Equal(1, consultas);
        Assert.Empty(lista.Datos!.Pacientes);
        Assert.False(edicion.Exito);
        var sinPermiso = await e.EnSolicitudAsync(s => s.Obtener<IServicioPacientes>().ObtenerAsync(id),
            cookie: string.Empty, exigirSesion: false);
        Assert.False(sinPermiso.Exito);
        Assert.Null(sinPermiso.Datos);
        Assert.Equal(MensajesDePacientes.SinPermiso, sinPermiso.Mensaje);
        Assert.Equal(antes, await e.HuellaAsync());
    }
}
