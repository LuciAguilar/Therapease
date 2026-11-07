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
    // Un interbloqueo no es una regla de negocio: se traduce a una excepción de dominio que NO es ReglaDeNegocioException, de modo que ningún servicio la
    // confunde con un rechazo y todos siguen respondiendo su error general, salvo el alta de citas, que la trata como conflicto de horario.
    public static Exception? Traducir(Exception excepcion) => excepcion switch
    {
        _ when TieneInterbloqueo(excepcion) => new InterbloqueoDeBaseException(excepcion),
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

    // EF ofrece el interbloqueo como fallo transitorio dentro de otra excepción: se busca en toda la cadena, sin leer mensajes ni datos.
    private static bool TieneInterbloqueo(Exception excepcion)
    {
        for (var actual = excepcion; actual is not null; actual = actual.InnerException)
        {
            if (actual is PostgresException { SqlState: PostgresErrorCodes.DeadlockDetected })
            {
                return true;
            }
        }

        return false;
    }
}
