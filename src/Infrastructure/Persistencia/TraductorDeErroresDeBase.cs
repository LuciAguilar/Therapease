using Microsoft.EntityFrameworkCore;
using Npgsql;
using TherapEase.Domain.Citas;
using TherapEase.Domain.Compartido;

namespace TherapEase.Infrastructure.Persistencia;

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
        _ => null
    };
}
