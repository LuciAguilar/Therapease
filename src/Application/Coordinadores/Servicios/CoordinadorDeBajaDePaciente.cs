using TherapEase.Application.Auditoria.Interfaces.Servicios;
using TherapEase.Application.Citas.Interfaces.Repositorios;
using TherapEase.Application.Compartido.Interfaces.Repositorios;
using TherapEase.Application.Compartido.Interfaces.Servicios;
using TherapEase.Application.Compartido.Modelos;
using TherapEase.Application.Coordinadores.Constantes;
using TherapEase.Application.Coordinadores.Interfaces.Servicios;
using TherapEase.Application.Coordinadores.Modelos;
using TherapEase.Application.Pacientes.Interfaces.Repositorios;
using TherapEase.Domain.Auditoria.Entidades.Enums;
using TherapEase.Domain.Compartido.Entidades.Enums;
using TherapEase.Domain.Compartido.Excepciones;
using TherapEase.Domain.Pacientes.Reglas;

namespace TherapEase.Application.Coordinadores.Servicios;

public class CoordinadorDeBajaDePaciente : ICoordinadorDeBajaDePaciente
{
    private readonly IRepositorioPacientes _pacientes;
    private readonly IRepositorioCitas _citas;
    private readonly IUnidadDeTrabajo _unidad;
    private readonly IServicioAuditoria _auditoria;
    private readonly IAutorizacion _autorizacion;
    private readonly IUsuarioActual _usuarioActual;
    private readonly TimeProvider _tiempo;

    public CoordinadorDeBajaDePaciente(
        IRepositorioPacientes pacientes,
        IRepositorioCitas citas,
        IUnidadDeTrabajo unidad,
        IServicioAuditoria auditoria,
        IAutorizacion autorizacion,
        IUsuarioActual usuarioActual,
        TimeProvider tiempo)
    {
        _pacientes = pacientes;
        _citas = citas;
        _unidad = unidad;
        _auditoria = auditoria;
        _autorizacion = autorizacion;
        _usuarioActual = usuarioActual;
        _tiempo = tiempo;
    }

    public async Task<RespuestaServicio<ResultadoDeBajaDePaciente>> DarDeBajaAsync(
        Guid idPaciente, int versionEsperada, CancellationToken cancelacion = default)
    {
        if (_usuarioActual.IdUsuario is not { } idActor || !await _autorizacion.TienePermisoAsync(Permiso.DarDeBajaPacientes, cancelacion))
        {
            return RespuestaServicio<ResultadoDeBajaDePaciente>.Fallida(MensajesDeCoordinacion.SinPermiso);
        }

        var paciente = await _pacientes.ObtenerAsync(idPaciente, cancelacion);
        if (paciente is null)
        {
            return RespuestaServicio<ResultadoDeBajaDePaciente>.Fallida(MensajesDeCoordinacion.PacienteNoEncontrado);
        }

        if (await _citas.ExistenActivasDePacienteAsync(idPaciente, cancelacion))
        {
            return RespuestaServicio<ResultadoDeBajaDePaciente>.Fallida(MensajesDeCoordinacion.PacienteConCitasActivas);
        }

        // Una cita agendada entre la consulta y el guardado la rechaza la base al confirmar (TE001), sea cual sea el orden.
        try
        {
            await _unidad.EjecutarAsync(async ct =>
            {
                ReglasDePaciente.DarDeBaja(paciente, versionEsperada, idActor, _tiempo.GetUtcNow());
                await _auditoria.RegistrarHechoDeCambioAsync(
                    idActor, TipoRegistroAuditoria.Paciente, idPaciente, AccionAuditoria.Baja,
                    ["Condicion", "FechaBaja", "IdUsuarioBaja"], "Se dio de baja a un paciente.", ct);
            }, cancelacion);
        }
        catch (ReglaDeNegocioException excepcion)
        {
            return RespuestaServicio<ResultadoDeBajaDePaciente>.Fallida(excepcion.Message);
        }

        return RespuestaServicio<ResultadoDeBajaDePaciente>.Correcta(
            new ResultadoDeBajaDePaciente(idPaciente, paciente.Version), MensajesDeCoordinacion.BajaRealizada);
    }
}
