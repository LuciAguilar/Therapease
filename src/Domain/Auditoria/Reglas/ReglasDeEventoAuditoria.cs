using System.Text.RegularExpressions;
using TherapEase.Domain.Auditoria.Entidades;
using TherapEase.Domain.Auditoria.Entidades.Enums;
using TherapEase.Domain.Compartido.Excepciones;

namespace TherapEase.Domain.Auditoria.Reglas;

public static partial class ReglasDeEventoAuditoria
{
    // CamposAfectados lleva nombres de campos, nunca valores (Q03/Q04); por eso se exige forma de identificador.
    public static EventoAuditoria Registrar(
        Guid idEvento,
        DateTimeOffset ahora,
        Guid idActor,
        TipoRegistroAuditoria tipoRegistro,
        Guid idRegistro,
        AccionAuditoria accion,
        IEnumerable<string> camposAfectados,
        string hecho)
    {
        ArgumentNullException.ThrowIfNull(camposAfectados);

        if (idEvento == Guid.Empty || idActor == Guid.Empty || idRegistro == Guid.Empty)
        {
            throw new ReglaDeNegocioException("El evento necesita identificador, actor y registro afectado.");
        }

        if (!Enum.IsDefined(tipoRegistro) || !Enum.IsDefined(accion))
        {
            throw new ReglaDeNegocioException("El tipo de registro o la acción del evento no son válidos.");
        }

        var campos = camposAfectados.ToList();
        if (campos.Any(c => c is null || !NombreDeCampo().IsMatch(c)))
        {
            throw new ReglaDeNegocioException("Los campos afectados deben ser nombres de campo, no valores.");
        }

        var hechoLimpio = hecho?.Trim();
        if (string.IsNullOrEmpty(hechoLimpio) || hechoLimpio.Length > EventoAuditoria.LongitudMaximaHecho)
        {
            throw new ReglaDeNegocioException($"El hecho es obligatorio y admite hasta {EventoAuditoria.LongitudMaximaHecho} caracteres.");
        }

        return new EventoAuditoria
        {
            IdEvento = idEvento,
            FechaEvento = ahora.ToUniversalTime(),
            IdUsuarioActor = idActor,
            TipoRegistro = tipoRegistro,
            IdRegistro = idRegistro,
            Accion = accion,
            CamposAfectados = campos,
            Hecho = hechoLimpio
        };
    }

    [GeneratedRegex("^[A-Za-z][A-Za-z0-9]{0,49}$")]
    private static partial Regex NombreDeCampo();
}
