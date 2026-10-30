using Microsoft.EntityFrameworkCore;
using TherapEase.Application.Identidad.Interfaces.Servicios;
using TherapEase.Application.Pacientes.Constantes;
using TherapEase.Application.Pacientes.Interfaces.Servicios;
using TherapEase.Application.Pacientes.Modelos;
using static TherapEase.IntegrationTests.PacientesRevision;
using static TherapEase.IntegrationTests.ServidorIdentidadRevision;

namespace TherapEase.IntegrationTests;

/// <summary>
/// Pruebas de seguridad de las fichas de pacientes con usuarios y sesiones reales:
/// sin sesión vigente o sin permiso no se puede hacer nada, y nadie aprovecha el número de ficha de otra persona.
/// También revisa que la búsqueda no entregue la lista completa, no se altere con caracteres especiales
/// y no distinga mayúsculas ni acentos, y que los mensajes y registros no muestren datos privados.
/// No crea páginas ni rutas especiales para las pruebas.
/// </summary>
public class PacientesProteccionTests(PostgreSqlRevision postgres) : IClassFixture<PostgreSqlRevision>
{


    // ──── SESIÓN Y PERMISOS ────────────────────────────────────────────────────────────────


    /// Sin sesión, con la sesión vencida o cerrada, con el usuario desactivado, con contraseña temporal pendiente
    /// o sin rol, se rechazan registrar, buscar, consultar y editar, para Usuaria y para Superusuario.
    /// El rechazo ocurre antes de leer fichas y no revela si una ficha existe.
    [Theory]
    [InlineData("Usuaria", "ausente")]
    [InlineData("Superusuario", "ausente")]
    [InlineData("Usuaria", "vencida")]
    [InlineData("Superusuario", "vencida")]
    [InlineData("Usuaria", "revocada")]
    [InlineData("Superusuario", "revocada")]
    [InlineData("Usuaria", "inactiva")]
    [InlineData("Superusuario", "inactiva")]
    [InlineData("Usuaria", "temporal")]
    [InlineData("Superusuario", "temporal")]
    [InlineData("Usuaria", "sin-rol")]
    [InlineData("Superusuario", "sin-rol")]
    public async Task SinAccesoVigenteLasCuatroOperacionesSeDenieganAntesDeLeerPacientes(string rol, string estado)
    {
        // Arrange: primero se crea una ficha y luego se retira el acceso de esa sesión.
        using var e = await PacientesRevision.CrearAsync(postgres, rol);
        var id = await e.RegistrarAsync();

        // Se crea otro superusuario porque el sistema no deja desactivar ni quitar el rol al último.
        // Esta prueba revisa el acceso retirado, no esa protección.

        if (rol == "Superusuario" && estado is "inactiva" or "sin-rol")
            await CrearAsync(e.Proveedor, "superusuario-reserva-ficticio", "Superusuario");
        var cookie = e.Cookie;
        switch (estado)
        {
            case "ausente": cookie = string.Empty; break;
            case "vencida": e.Reloj.Avanzar(TimeSpan.FromMinutes(31)); break;
            case "revocada":
                await e.EnSolicitudAsync(async s => { await s.Obtener<IServicioAcceso>().CerrarSesionAsync(); return true; });
                break;
            case "inactiva":
                await e.SqlAsync("UPDATE \"AspNetUsers\" SET \"Activo\"=false WHERE \"Id\"=@id", ("id", e.Actor));
                break;
            case "temporal":
                await e.SqlAsync("UPDATE \"AspNetUsers\" SET \"DebeCambiarContrasena\"=true WHERE \"Id\"=@id", ("id", e.Actor));
                break;
            case "sin-rol":
                await e.SqlAsync("DELETE FROM \"AspNetUserRoles\" WHERE \"UserId\"=@id", ("id", e.Actor));
                break;
            default: throw new ArgumentOutOfRangeException(nameof(estado));
        }
        var antes = await e.HuellaAsync();
        e.Comandos.Limpiar();

        // Act: todas las operaciones usan la misma cookie, revisada por la validación real de sesiones.
        await e.EnSolicitudAsync(async solicitud =>
        {
            var servicio = solicitud.Obtener<IServicioPacientes>();
            var alta = await servicio.RegistrarAsync(Guid.NewGuid(), Adulto(), true);
            var reenvio = await servicio.RegistrarAsync(id, Adulto());
            var lista = await servicio.BuscarAsync("ejemplo");
            var ficha = await servicio.ObtenerAsync(id);
            var inexistente = await servicio.ObtenerAsync(Guid.NewGuid());
            var edicion = await servicio.ActualizarAsync(id, 1, Adulto("Cambio Prohibido"));

            // Assert: el mismo rechazo para todas; no dice si la ficha existe ni muestra fichas parecidas.
            Assert.False(alta.Exito); Assert.Null(alta.Datos); Assert.Equal(MensajesDePacientes.SinPermiso, alta.Mensaje);
            Assert.False(reenvio.Exito); Assert.Null(reenvio.Datos); Assert.Equal(MensajesDePacientes.SinPermiso, reenvio.Mensaje);
            Assert.False(lista.Exito); Assert.Null(lista.Datos); Assert.Equal(MensajesDePacientes.SinPermiso, lista.Mensaje);
            Assert.False(ficha.Exito); Assert.Null(ficha.Datos); Assert.Equal(MensajesDePacientes.SinPermiso, ficha.Mensaje);
            Assert.Equal(ficha.Mensaje, inexistente.Mensaje); Assert.Null(inexistente.Datos);
            Assert.False(edicion.Exito); Assert.Null(edicion.Datos); Assert.Equal(MensajesDePacientes.SinPermiso, edicion.Mensaje);
            return true;
        }, cookie, exigirSesion: false);
        Assert.DoesNotContain(e.Comandos.Textos, t => t.Contains("FROM \"Paciente\"", StringComparison.Ordinal));
        Assert.Equal(antes, await e.HuellaAsync());
    }

    /// Si otra persona usa el número de una ficha ya registrada, o se reenvía con datos distintos,
    /// se rechaza sin mostrar ni cambiar la ficha.
    /// Solo quien la registró, con los mismos datos, recibe «ya registrada».
    [Fact]
    public async Task UnIdentificadorDeAltaNoPermiteQueOtraPersonaLoReutiliceNiCambieLosDatos()
    {
        // Arrange
        using var e = await PacientesRevision.CrearAsync(postgres);
        var datos = Adulto();
        var id = await e.RegistrarAsync(datos);
        await CrearAsync(e.Proveedor, "otro-operador-ficticio", "Superusuario");
        var otraCookie = await EntrarAsync(e.Proveedor, "otro-operador-ficticio");
        e.Reloj.Avanzar(TimeSpan.FromMilliseconds(1));
        var antes = await e.HuellaAsync();

        // Act
        var ajena = await e.EnSolicitudAsync(s => s.Obtener<IServicioPacientes>().RegistrarAsync(id, datos), otraCookie);
        var distinta = await e.ServicioAsync(s => s.RegistrarAsync(id, datos with { Grupo = "Otro Grupo" }));
        var propia = await e.ServicioAsync(s => s.RegistrarAsync(id, datos));

        // Assert
        Assert.False(ajena.Exito); Assert.Null(ajena.Datos);
        Assert.False(distinta.Exito); Assert.Null(distinta.Datos);
        Assert.True(propia.Exito);
        Assert.Equal(EstadoDeAlta.YaRegistrado, propia.Datos!.Estado);
        Assert.Equal(antes, await e.HuellaAsync());
    }


    // ──── BÚSQUEDA ─────────────────────────────────────────────────────────────────────────


    /// Una búsqueda vacía, de una sola letra o con más de seis palabras se rechaza sin consultar fichas.
    /// Nunca entrega la lista completa de pacientes.
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("a")]
    [InlineData("uno dos tres cuatro cinco seis siete")]
    public async Task UnaBusquedaInvalidaNoSeConvierteEnListaCompleta(string? texto)
    {
        // Arrange
        using var e = await PacientesRevision.CrearAsync(postgres);
        await e.RegistrarAsync();
        var antes = await e.HuellaAsync();
        e.Comandos.Limpiar();

        // Act
        var resultado = await e.ServicioAsync(s => s.BuscarAsync(texto));

        // Assert
        Assert.False(resultado.Exito);
        Assert.Equal(MensajesDePacientes.BusquedaInvalida, resultado.Mensaje);
        Assert.Null(resultado.Datos);
        Assert.DoesNotContain(e.Comandos.Textos, t => t.Contains("FROM \"Paciente\"", StringComparison.Ordinal));
        Assert.Equal(antes, await e.HuellaAsync());
    }

    /// Los caracteres especiales de búsqueda (%, _ y \) se buscan como texto normal y el orden de las palabras no importa.
    /// Un intento de alterar la consulta no devuelve fichas, y el texto buscado no queda en los registros.
    [Fact]
    public async Task LaBusquedaTrataLosComodinesComoTextoYNoRegistraElTextoBuscado()
    {
        // Arrange
        using var e = await PacientesRevision.CrearAsync(postgres);
        var especial = await e.RegistrarAsync(Adulto("Paciente %_\\ Ejemplo Ficticio"));
        await e.RegistrarAsync(Adulto("Otra Persona Ficticia"));
        var antes = await e.HuellaAsync();

        // Act + Assert
        foreach (var texto in new[] { "%_\\", "EJEMPLO PACIENTE" })
        {
            var lista = await e.ServicioAsync(s => s.BuscarAsync(texto));
            Assert.Equal(especial, Assert.Single(lista.Datos!.Pacientes).IdPaciente);
        }
        const string intento = "ejemplo' OR 1=1 --";
        var vacia = await e.ServicioAsync(s => s.BuscarAsync(intento));
        Assert.True(vacia.Exito);
        Assert.Empty(vacia.Datos!.Pacientes);
        Assert.False(e.Registro.Texto.Contains(intento, StringComparison.Ordinal), "Se registró el texto buscado.");
        Assert.Equal(antes, await e.HuellaAsync());
    }

    /// Buscar en minúsculas encuentra nombres de otros alfabetos escritos con mayúsculas o acentos
    /// (por ejemplo, «øscar» encuentra «Øscar»; la sigma final «ς» cuenta igual que «σ»).
    /// Lo esperado sale de la regla aprobada «sin distinguir mayúsculas ni acentos», no del código actual.
    [Theory]
    [InlineData("Øscar Ficticio", "øscar")]
    [InlineData("Жук Ficticio", "жук")]
    [InlineData("Σωτηρία Ficticia", "σωτηρια")]
    [InlineData("ςταυρος Ficticio", "σταυροσ")]
    [InlineData("İstanbul Ficticio", "istanbul")]
    public async Task BuscarIgnoraMayusculasYAcentosTambienEnNombresNoAscii(string nombre, string buscado)
    {
        // Arrange
        using var e = await PacientesRevision.CrearAsync(postgres);
        var id = await e.RegistrarAsync(Adulto(nombre));

        // Act
        var respuesta = await e.ServicioAsync(s => s.BuscarAsync(buscado));

        // Assert
        Assert.True(respuesta.Exito);
        Assert.Contains(respuesta.Datos!.Pacientes, p => p.IdPaciente == id);
    }


    // ──── DATOS PRIVADOS ───────────────────────────────────────────────────────────────────


    /// Ni un dato rechazado ni un fallo al guardar la auditoría dejan datos privados
    /// (nombre, contactos, escuela, grupo, texto buscado, cookie o conexión a la base)
    /// en mensajes, errores, eventos o registros.
    [Fact]
    public async Task LosRechazosYErroresNoDejanDatosPrivadosEnMensajesEventosORegistros()
    {
        // Arrange
        using var e = await PacientesRevision.CrearAsync(postgres);
        var datos = Menor();
        var id = await e.RegistrarAsync(datos);
        const string correoInvalido = "rastreo-rechazado@dominio";
        const string busqueda = "texto-privado-rastreo";

        // Act
        var rechazo = await e.ServicioAsync(s => s.ActualizarAsync(id, 1, datos with { Correo = correoInvalido }));
        var lista = await e.ServicioAsync(s => s.BuscarAsync(busqueda));
        await SimularFalloDeAuditoriaAsync(e.Escenario);
        var error = await Assert.ThrowsAsync<DbUpdateException>(() => e.ServicioAsync(s => s.ActualizarAsync(id, 1,
            datos with { Grupo = "Dato Privado Nuevo" })));

        // Assert: si aparece un dato privado, el aviso de la prueba no lo muestra.
        Assert.False(rechazo.Exito);
        Assert.True(lista.Exito);
        var texto = rechazo.Mensaje + error + System.Text.Json.JsonSerializer.Serialize(await e.EventosAsync()) + e.Registro.Texto;
        foreach (var privado in new[] { datos.Nombre!, datos.Correo!, datos.Tutor!.Nombre!, datos.Tutor.Correo!,
            datos.Escuela!, datos.Grupo!, "6441234567", "6442345678", "Dato Privado Nuevo", correoInvalido, busqueda, e.Cookie, e.Escenario.App })
            Assert.False(texto.Contains(privado, StringComparison.OrdinalIgnoreCase), "Un mensaje o registro contiene un valor privado de prueba.");
    }
}
