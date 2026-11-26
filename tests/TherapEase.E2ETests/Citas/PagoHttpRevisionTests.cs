using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using TherapEase.Domain.Citas.Entidades.Enums;
using TherapEase.Domain.Citas.Reglas;
using TherapEase.IntegrationTests;

namespace TherapEase.E2ETests;

/// <summary>
/// Comprueba permisos, formularios manipulados y auditoría de pago por HTTP real.
/// Conserva el servidor aprobado y usa PostgreSQL desechable con datos ficticios.
/// </summary>
public sealed partial class AccesoSesionesYPermisosTests
{


    // ──── SESIÓN Y PERMISOS EFECTIVOS ──────────────────────────────────


    /// Sin sesión no se obtiene el detalle ni se cambia el pago, exista o no la cita.
    [Theory]
    [InlineData("detalle", false)]
    [InlineData("detalle", true)]
    [InlineData("pago", false)]
    [InlineData("pago", true)]
    [InlineData("post", false)]
    [InlineData("post", true)]
    public Task PagoHttpAnonimoNoRevelaNiModifica(string acceso, bool inexistente) => Verificar(async () =>
    {
        // Arrange
        var propietario = await Pagina();
        var cita = await PrepararPagoRevision(propietario);
        var p = await Pagina();
        var id = inexistente ? Guid.NewGuid() : cita.Id;
        // Act
        var respuesta = acceso == "post"
            ? await Enviar(p, "/Agenda/Pago/" + id, DatosPagoRevision(), protegido: false)
            : await p.APIRequest.GetAsync(entorno.Url + (acceso == "pago" ? "/Agenda/Pago/" : "/Agenda/Detalle/") + id, new() { MaxRedirects = 0 });
        // Assert
        Exigir(DenegadoPagoRevision(respuesta));
        await ComprobarPagoRevision(cita.Id, EstadoPago.Pendiente, 1, 0);
    });

    /// Una cuenta con temporal no consulta ni escribe pago aunque use URL o POST directos.
    [Theory]
    [InlineData("detalle")]
    [InlineData("pago")]
    [InlineData("post")]
    public Task PagoHttpConTemporalSeDeniega(string acceso) => Verificar(async () =>
    {
        // Arrange
        var propietario = await Pagina();
        var cita = await PrepararPagoRevision(propietario);
        var p = await Pagina();
        await Entrar(p, await entorno.CrearCuentaAsync(temporal: true));
        // Act
        var respuesta = acceso == "post" ? await Enviar(p, "/Agenda/Pago/" + cita.Id, DatosPagoRevision())
            : await p.APIRequest.GetAsync(entorno.Url + (acceso == "pago" ? "/Agenda/Pago/" : "/Agenda/Detalle/") + cita.Id,
                new() { MaxRedirects = 0 });
        // Assert
        Exigir(DenegadoPagoRevision(respuesta));
        await ComprobarPagoRevision(cita.Id, EstadoPago.Pendiente, 1, 0);
    });

    /// Una sesión válida sin decisiones de rol no obtiene ni cambia el pago.
    [Theory]
    [InlineData("detalle")]
    [InlineData("pago")]
    [InlineData("post")]
    public Task PagoHttpSinRolSeDeniegaAntesDeLeer(string acceso) => Verificar(async () =>
    {
        // Arrange
        var p = await Pagina();
        var cita = await PrepararPagoRevision(p);
        await using (var db = entorno.Servidor.Escenario.Contexto())
            await db.UserRoles.Where(r => r.UserId == cita.Cuenta.Id).ExecuteDeleteAsync();
        // Act
        var respuesta = acceso == "post" ? await Enviar(p, "/Agenda/Pago/" + cita.Id, DatosPagoRevision())
            : await p.APIRequest.GetAsync(entorno.Url + (acceso == "pago" ? "/Agenda/Pago/" : "/Agenda/Detalle/") + cita.Id,
                new() { MaxRedirects = 0 });
        // Assert
        Exigir(DenegadoPagoRevision(respuesta));
        await ComprobarPagoRevision(cita.Id, EstadoPago.Pendiente, 1, 0);
    });


    // ──── ANTIFALSIFICACIÓN Y ENTRADAS ─────────────────────────────────


    /// Sin token o con el de otra sesión, el servidor rechaza antes de cambiar pago o auditoría.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task PagoHttpExigeTokenDeLaMismaSesion(bool ajeno) => Verificar(async () =>
    {
        // Arrange
        var p = await Pagina();
        var cita = await PrepararPagoRevision(p);
        var datos = DatosPagoRevision();
        if (ajeno)
        {
            var otra = await Pagina();
            await Entrar(otra, await entorno.CrearCuentaAsync());
            await Abrir(otra, "/Cuenta/Contrasena");
            datos["__RequestVerificationToken"] = await Token(otra);
        }
        // Act
        var respuesta = await Enviar(p, "/Agenda/Pago/" + cita.Id, datos, protegido: false);
        // Assert
        Exigir(respuesta.Status == 400);
        await ComprobarPagoRevision(cita.Id, EstadoPago.Pendiente, 1, 0);
    });

    /// Saltarse la validación del navegador no convierte entradas ausentes o mal formadas en un pago.
    [Theory]
    [InlineData("sin-version")]
    [InlineData("version-cero")]
    [InlineData("version-texto")]
    [InlineData("version-enorme")]
    [InlineData("sin-pago")]
    [InlineData("pago-enum")]
    [InlineData("pago-texto")]
    public Task PagoHttpRechazaDatosManipulados(string defecto) => Verificar(async () =>
    {
        // Arrange
        var p = await Pagina();
        var cita = await PrepararPagoRevision(p);
        var datos = DatosPagoRevision();
        switch (defecto)
        {
            case "sin-version": datos.Remove("VersionEsperada"); break;
            case "version-cero": datos["VersionEsperada"] = "0"; break;
            case "version-texto": datos["VersionEsperada"] = "no-es-version"; break;
            case "version-enorme": datos["VersionEsperada"] = "99999999999999999999"; break;
            case "sin-pago": datos.Remove("NuevoPago"); break;
            case "pago-enum": datos["NuevoPago"] = "99"; break;
            case "pago-texto": datos["NuevoPago"] = "<script>qaPago=1</script>"; break;
        }
        // Act
        var respuesta = await Enviar(p, "/Agenda/Pago/" + cita.Id, datos);
        // Assert
        Exigir(respuesta.Status == 200 && WebUtility.HtmlDecode(await respuesta.TextAsync()).Contains("La solicitud no es válida."));
        await ComprobarPagoRevision(cita.Id, EstadoPago.Pendiente, 1, 0);
    });

    /// Los campos extra no cambian otra parte de la cita ni el actor real de ambos roles.
    [Theory]
    [InlineData("Usuaria")]
    [InlineData("Superusuario")]
    public Task PagoHttpIgnoraCamposExtraYConservaActor(string rol) => Verificar(async () =>
    {
        // Arrange
        var p = await Pagina();
        var cita = await PrepararPagoRevision(p, rol);
        var datos = DatosPagoRevision();
        foreach (var campo in new[] { "IdUsuarioActualizacion", "IdUsuarioAlta", "IdPaciente", "Inicio", "Fin", "Estado", "EstadoPago", "Condicion", "Rol", "Monto" })
            datos[campo] = campo.StartsWith("Id") ? Guid.NewGuid().ToString() : "valor-ignorado";
        await using var antes = entorno.Servidor.Escenario.Contexto();
        var original = await antes.Citas.AsNoTracking().SingleAsync(c => c.IdCita == cita.Id);
        // Act
        var respuesta = await Enviar(p, "/Agenda/Pago/" + cita.Id, datos);
        // Assert
        Exigir(respuesta.Status == 302);
        await ComprobarPagoRevision(cita.Id, EstadoPago.Pagado, 2, 1, cita.Cuenta.Id);
        await using var despues = entorno.Servidor.Escenario.Contexto();
        var guardada = await despues.Citas.AsNoTracking().SingleAsync(c => c.IdCita == cita.Id);
        Exigir(guardada.IdPaciente == original.IdPaciente && guardada.Inicio == original.Inicio && guardada.Fin == original.Fin &&
            guardada.Ambito == original.Ambito && guardada.Estado == original.Estado && guardada.Condicion == original.Condicion);
    });


    // ──── ESTADOS, VERSIONES Y SALIDA SEGURA ───────────────────────────


    /// Una cancelada admite corregir el pago; una de baja nunca, sin crear funciones de B05/B06.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task PagoHttpRespetaCanceladaYBaja(bool baja) => Verificar(async () =>
    {
        // Arrange
        var p = await Pagina();
        var cita = await PrepararPagoRevision(p);
        await using (var db = entorno.Servidor.Escenario.Contexto())
        {
            var fila = await db.Citas.SingleAsync(c => c.IdCita == cita.Id);
            if (baja) ReglasDeCita.DarDeBaja(fila, 1, cita.Cuenta.Id, DateTimeOffset.UtcNow);
            else ReglasDeCita.Cancelar(fila, 1, cita.Cuenta.Id, DateTimeOffset.UtcNow);
            await db.SaveChangesAsync();
        }
        // Act
        var respuesta = await Enviar(p, "/Agenda/Pago/" + cita.Id, DatosPagoRevision(2));
        // Assert
        Exigir(respuesta.Status == (baja ? 200 : 302));
        if (baja)
        {
            Exigir(WebUtility.HtmlDecode(await respuesta.TextAsync()).Contains("su pago no puede cambiarse"));
            var consulta = await p.APIRequest.GetAsync(entorno.Url + "/Agenda/Detalle/" + cita.Id);
            Exigir(consulta.Status == 404 && !(await consulta.TextAsync()).Contains("data-formulario-pago"));
        }
        await ComprobarPagoRevision(cita.Id, baja ? EstadoPago.Pendiente : EstadoPago.Pagado, baja ? 2 : 3, baja ? 0 : 1);
    });

    /// Sin cambios no escribe; la versión antigua se rechaza incluso cuando el pago coincide.
    [Fact]
    public Task PagoHttpSinCambiosYVersionAntiguaNoCreanEventos() => Verificar(async () =>
    {
        // Arrange
        var p = await Pagina();
        var cita = await PrepararPagoRevision(p);
        // Act
        Exigir((await Enviar(p, "/Agenda/Pago/" + cita.Id, DatosPagoRevision(1, "Pendiente"))).Status == 302);
        await Abrir(p, "/Agenda/Detalle/" + cita.Id);
        // Assert
        await AvisoPagoRevision(p, "No hubo cambios.");
        await ComprobarPagoRevision(cita.Id, EstadoPago.Pendiente, 1, 0);
        // Act
        Exigir((await Enviar(p, "/Agenda/Pago/" + cita.Id, DatosPagoRevision())).Status == 302);
        var repetida = await Enviar(p, "/Agenda/Pago/" + cita.Id, DatosPagoRevision());
        // Assert
        Exigir(repetida.Status == 200 && WebUtility.HtmlDecode(await repetida.TextAsync()).Contains("El registro cambió mientras se editaba"));
        await ComprobarPagoRevision(cita.Id, EstadoPago.Pagado, 2, 1);
    });

    /// La consulta privada mantiene no-store y muestra un nombre malicioso como texto.
    [Fact]
    public Task PagoHttpDetalleCodificaDatosYEvitaCache() => Verificar(async () =>
    {
        // Arrange
        var p = await Pagina();
        var cita = await PrepararPagoRevision(p, nombre: "Paciente <script>globalThis.qaPago=1</script>");
        // Act
        var respuesta = await Abrir(p, "/Agenda/Detalle/" + cita.Id);
        // Assert
        Exigir(respuesta.Headers["cache-control"].Contains("no-store"));
        Exigir(await p.EvaluateAsync<bool>("globalThis.qaPago === undefined"));
        await Assertions.Expect(p.Locator(".cita-enlace-paciente")).ToContainTextAsync("<script>");
        var ausente = await p.APIRequest.GetAsync(entorno.Url + "/Agenda/Detalle/" + Guid.NewGuid());
        Exigir(ausente.Status == 404 && !(await ausente.TextAsync()).Contains("data-formulario-pago"));
        await ComprobarPagoRevision(cita.Id, EstadoPago.Pendiente, 1, 0);
    });
    /// Un fallo real al insertar la auditoría devuelve 500 y revierte el pago, sin éxito ni conflicto falsos.
    [Fact]
    public Task PagoHttpFalloRealDeAuditoriaRevierteTodo() => Verificar(async () =>
    {
        // Arrange
        var p = await Pagina();
        var cita = await PrepararPagoRevision(p);
        await PostgreSqlRevision.Sql(entorno.Servidor.Escenario.Migrador,
            "ALTER TABLE \"EventoAuditoria\" ADD CONSTRAINT \"CK_PruebaPago_Evento\" CHECK (\"Accion\" <> 'Actualizacion') NOT VALID");
        try
        {
            // Act
            var respuesta = await Enviar(p, "/Agenda/Pago/" + cita.Id, DatosPagoRevision());
            // Assert
            Exigir(respuesta.Status == 500);
            Exigir(!(await respuesta.TextAsync()).Contains("Estado de pago actualizado."));
            await ComprobarPagoRevision(cita.Id, EstadoPago.Pendiente, 1, 0);
        }
        finally
        {
            await PostgreSqlRevision.Sql(entorno.Servidor.Escenario.Migrador,
                "ALTER TABLE \"EventoAuditoria\" DROP CONSTRAINT \"CK_PruebaPago_Evento\"");
        }
        // Act: una acción nueva con la versión original solo guarda ahora, no repite un cambio pendiente del fallo.
        var nueva = await Enviar(p, "/Agenda/Pago/" + cita.Id, DatosPagoRevision());
        // Assert
        Exigir(nueva.Status == 302);
        await ComprobarPagoRevision(cita.Id, EstadoPago.Pagado, 2, 1);
    });
}
