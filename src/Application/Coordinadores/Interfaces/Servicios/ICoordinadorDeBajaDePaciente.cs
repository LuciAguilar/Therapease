using TherapEase.Application.Compartido.Modelos;
using TherapEase.Application.Coordinadores.Modelos;

namespace TherapEase.Application.Coordinadores.Interfaces.Servicios;

// CU04: coordina Pacientes y Citas fuera de ambos módulos para no crear un ciclo entre ellos.
public interface ICoordinadorDeBajaDePaciente
{
    // La baja solo se efectúa si el paciente no tiene citas activas; nunca cancela citas por su cuenta.
    Task<RespuestaServicio<ResultadoDeBajaDePaciente>> DarDeBajaAsync(
        Guid idPaciente, int versionEsperada, CancellationToken cancelacion = default);
}
