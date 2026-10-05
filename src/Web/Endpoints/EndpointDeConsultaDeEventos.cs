using System.Globalization;
using System.Text.RegularExpressions;
using TherapEase.Application.Auditoria.Constantes;
using TherapEase.Application.Auditoria.Interfaces.Servicios;
using TherapEase.Application.Auditoria.Modelos;
using TherapEase.Domain.Auditoria.Entidades.Enums;
using TherapEase.Web.Seguridad;

namespace TherapEase.Web.Endpoints;

// Consulta técnica de solo lectura de B00: sin pantalla, solo para superusuario activo con sesión vigente.
public static partial class EndpointDeConsultaDeEventos
{
    public const string Ruta = "/api/auditoria/eventos";

    public static IEndpointRouteBuilder MapearConsultaTecnicaDeAuditoria(this IEndpointRouteBuilder rutas)
    {
        rutas.MapGet(Ruta, ConsultarAsync).RequireAuthorization(PoliticasDeAutorizacion.ConsultarAuditoria);
        return rutas;
    }

    private static async Task<IResult> ConsultarAsync(
        string? desde,
        string? hasta,
        string? tipoRegistro,
        Guid? idRegistro,
        int? limite,
        IServicioAuditoria servicio,
        CancellationToken cancelacion)
    {
        if (!TryParsearInstante(desde, out var inicio) || !TryParsearInstante(hasta, out var fin))
        {
            return Results.BadRequest(new { error = "Indica desde y hasta como instantes con zona, por ejemplo 2026-10-01T00:00:00Z." });
        }

        TipoRegistroAuditoria? tipo = null;
        if (!string.IsNullOrEmpty(tipoRegistro))
        {
            if (!Enum.TryParse<TipoRegistroAuditoria>(tipoRegistro, ignoreCase: true, out var tipoValido) || !Enum.IsDefined(tipoValido))
            {
                return Results.BadRequest(new { error = "El tipo de registro no es válido." });
            }

            tipo = tipoValido;
        }

        var respuesta = await servicio.ConsultarEventosAutorizadosAsync(
            new FiltroDeEventos(inicio, fin, tipo, idRegistro, limite ?? FiltroDeEventos.LimitePorDefecto), cancelacion);

        if (respuesta.Exito)
        {
            return Results.Ok(respuesta.Datos);
        }

        return respuesta.Mensaje == MensajesDeAuditoria.SinPermiso
            ? Results.StatusCode(StatusCodes.Status403Forbidden)
            : Results.BadRequest(new { error = respuesta.Mensaje });
    }

    // Los instantes deben llevar zona (Q09): sin esta comprobación DateTimeOffset asumiría la zona del servidor.
    private static bool TryParsearInstante(string? valor, out DateTimeOffset instante)
    {
        instante = default;
        return valor is not null
            && InstanteConZona().IsMatch(valor)
            && DateTimeOffset.TryParse(valor, CultureInfo.InvariantCulture, DateTimeStyles.None, out instante);
    }

    [GeneratedRegex(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d{1,7})?(Z|[+-]\d{2}:\d{2})$")]
    private static partial Regex InstanteConZona();
}
