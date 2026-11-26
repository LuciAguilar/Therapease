using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using TherapEase.Domain.Auditoria.Entidades;
using TherapEase.Domain.Auditoria.Entidades.Enums;
using TherapEase.Domain.Citas.Entidades.Enums;

namespace TherapEase.E2ETests;

/// <summary>
/// Comparte preparación y comprobaciones del QA de pago con las páginas reales.
/// Solo usa datos ficticios y evita imprimir formularios o secretos.
/// </summary>
public sealed partial class AccesoSesionesYPermisosTests
{


    // ──── PREPARACIÓN Y RESULTADOS DE PAGO ──────────────────────────────


    /// Agenda mediante HTTP con la sesión real y deja abierto el detalle con su token.
    private async Task<(Guid Id, Guid Paciente, CuentaFicticia Cuenta)> PrepararPagoRevision(
        IPage p, string rol = "Usuaria", string? nombre = null)
    {
        var cuenta = await entorno.CrearCuentaAsync(rol);
        await Entrar(p, cuenta);
        var paciente = await PrepararPaciente(nombre ?? "Paciente de pago ficticio " + Guid.NewGuid().ToString("N"));
        var id = Guid.NewGuid();
        await Abrir(p, "/Agenda/Crear?paciente=" + paciente);
        var alta = await Enviar(p, "/Agenda/Crear", DatosDeReserva(id, paciente, FechaDeAgenda()));
        Exigir(alta.Status == 200);
        await Abrir(p, "/Agenda/Detalle/" + id);
        return (id, paciente, cuenta);
    }

    /// Conserva explícitos el destino y la versión; nunca renueva la versión para repetir.
    private static Dictionary<string, string> DatosPagoRevision(int version = 1, string pago = "Pagado") =>
        new() { ["VersionEsperada"] = version.ToString(), ["NuevoPago"] = pago };

    /// Consulta desde otro contexto para comprobar el pago confirmado y sus eventos reales.
    private async Task ComprobarPagoRevision(Guid id, EstadoPago pago, int version, int eventos, Guid? actor = null)
    {
        await using var db = entorno.Servidor.Escenario.Contexto();
        var cita = await db.Citas.AsNoTracking().SingleAsync(c => c.IdCita == id);
        var cambios = await db.Set<EventoAuditoria>().AsNoTracking()
            .Where(e => e.IdRegistro == id && e.Accion == AccionAuditoria.Actualizacion).ToListAsync();
        Exigir(cita.EstadoPago == pago && cita.Version == version && cambios.Count == eventos);
        foreach (var evento in cambios)
        {
            Exigir(evento.CamposAfectados.SequenceEqual(new[] { "EstadoPago" }));
            Exigir(evento.Hecho == "Se cambió el estado de pago de una cita.");
            if (actor is { } esperado) Exigir(evento.IdUsuarioActor == esperado);
        }
        if (eventos > 0 && actor is { } autor) Exigir(cita.IdUsuarioActualizacion == autor);
    }

    /// Acepta solo la denegación prevista sin seguir la redirección ni leer contenido privado.
    private static bool DenegadoPagoRevision(IAPIResponse respuesta) => respuesta.Status == 403 ||
        (respuesta.Status == 302 && respuesta.Headers.TryGetValue("location", out var ruta) &&
            (ruta.Contains("/Acceso") || ruta.Contains("/Cuenta/Contrasena")));

    /// Comprueba la confirmación visible sin copiar su contenido a los resultados de la prueba.
    private static async Task AvisoPagoRevision(IPage p, string texto) =>
        await Assertions.Expect(p.Locator(".aviso-confirmacion:visible strong")).ToHaveTextAsync(texto);
}
