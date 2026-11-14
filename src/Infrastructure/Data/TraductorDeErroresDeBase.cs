using Microsoft.EntityFrameworkCore;
using Npgsql;
using TherapEase.Domain.Citas.Excepciones;
using TherapEase.Domain.Compartido.Excepciones;
using TherapEase.Domain.Identidad.Excepciones;
using TherapEase.Domain.Pacientes.Excepciones;
using TherapEase.Infrastructure.Identidad.Constantes;

namespace TherapEase.Infrastructure.Data;

internal static class TraductorDeErroresDeBase
{
    public const string RestriccionCruce = "EX_Cita_SinCruceActivo";
    public const string CodigoPacienteDeBaja = "TE001";
    public const string ClavePrimariaDePaciente = "PK_Paciente";
    public const string ClavePrimariaDeCita = "PK_Cita";

    // Devuelve null cuando el error no es una regla conocida: debe propagarse tal cual.
    // Un interbloqueo (40P01) u otro fallo de la base no se traduce: sube como error general y no se confunde con un rechazo de negocio.
    public static ReglaDeNegocioException? Traducir(Exception excepcion) => excepcion switch
    {
        DbUpdateConcurrencyException => new ConflictoDeVersionException(excepcion),
        DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.ExclusionViolation, ConstraintName: RestriccionCruce } }
            => new CruceDeHorarioException(excepcion),
        DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: ClavePrimariaDePaciente } }
            => new IdentificadorDePacienteRepetidoException(excepcion),
        DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: ClavePrimariaDeCita } }
            => new IdentificadorDeCitaRepetidoException(excepcion),
        DbUpdateException { InnerException: PostgresException { SqlState: CodigoPacienteDeBaja } }
            => new CitaActivaConPacienteDeBajaException(excepcion),
        DbUpdateException { InnerException: PostgresException { SqlState: IdentidadConstantes.CodigoUltimoSuperusuario } }
            => new UltimoSuperusuarioException(excepcion),
        _ => null
    };
}
