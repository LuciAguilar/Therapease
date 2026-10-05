using Microsoft.EntityFrameworkCore;
using Npgsql;
using TherapEase.Domain.Citas.Excepciones;
using TherapEase.Domain.Compartido.Excepciones;
using TherapEase.Domain.Identidad.Excepciones;
using TherapEase.Infrastructure.Identidad.Constantes;

namespace TherapEase.Infrastructure.Data;

internal static class TraductorDeErroresDeBase
{
    public const string RestriccionCruce = "EX_Cita_SinCruceActivo";
    public const string CodigoPacienteDeBaja = "TE001";

    // Devuelve null cuando el error no es una regla conocida: debe propagarse tal cual.
    public static ReglaDeNegocioException? Traducir(Exception excepcion) => excepcion switch
    {
        DbUpdateConcurrencyException => new ConflictoDeVersionException(excepcion),
        DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.ExclusionViolation, ConstraintName: RestriccionCruce } }
            => new CruceDeHorarioException(excepcion),
        DbUpdateException { InnerException: PostgresException { SqlState: CodigoPacienteDeBaja } }
            => new CitaActivaConPacienteDeBajaException(excepcion),
        DbUpdateException { InnerException: PostgresException { SqlState: IdentidadConstantes.CodigoUltimoSuperusuario } }
            => new UltimoSuperusuarioException(excepcion),
        _ => null
    };
}
