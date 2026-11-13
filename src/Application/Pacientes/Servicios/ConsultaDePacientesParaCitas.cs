using TherapEase.Application.Compartido.Interfaces.Servicios;
using TherapEase.Application.Compartido.Modelos;
using TherapEase.Application.Pacientes.Constantes;
using TherapEase.Application.Pacientes.Interfaces.Repositorios;
using TherapEase.Application.Pacientes.Interfaces.Servicios;
using TherapEase.Application.Pacientes.Modelos;
using TherapEase.Domain.Compartido.Entidades.Enums;
using TherapEase.Domain.Pacientes.Entidades.Enums;

namespace TherapEase.Application.Pacientes.Servicios;

public class ConsultaDePacientesParaCitas : IConsultaDePacientesParaCitas
{
    private readonly IRepositorioPacientes _pacientes;
    private readonly IAutorizacion _autorizacion;
    private readonly IUsuarioActual _usuarioActual;

    public ConsultaDePacientesParaCitas(IRepositorioPacientes pacientes, IAutorizacion autorizacion, IUsuarioActual usuarioActual)
    {
        _pacientes = pacientes;
        _autorizacion = autorizacion;
        _usuarioActual = usuarioActual;
    }

    public async Task<RespuestaServicio<CondicionYAmbitosDePaciente>> ConsultarCondicionYAmbitosAsync(
        Guid idPaciente, CancellationToken cancelacion = default)
    {
        if (!await PuedeConsultarAsync(cancelacion))
        {
            return RespuestaServicio<CondicionYAmbitosDePaciente>.Fallida(MensajesDePacientes.SinPermiso);
        }

        var fila = idPaciente == Guid.Empty ? null : await _pacientes.ObtenerCondicionYAmbitosAsync(idPaciente, cancelacion);
        if (fila is null)
        {
            return RespuestaServicio<CondicionYAmbitosDePaciente>.Correcta(new CondicionYAmbitosDePaciente(null, []));
        }

        var ambitos = new List<AmbitoAtencion>();
        if (fila.TieneAmbitoEscolar)
        {
            ambitos.Add(AmbitoAtencion.Escolar);
        }

        if (fila.TieneAmbitoIndependiente)
        {
            ambitos.Add(AmbitoAtencion.Independiente);
        }

        return RespuestaServicio<CondicionYAmbitosDePaciente>.Correcta(new CondicionYAmbitosDePaciente(fila.Condicion, ambitos));
    }

    public async Task<RespuestaServicio<IReadOnlyDictionary<Guid, string>>> ConsultarNombresParaAgendaAsync(
        IReadOnlyCollection<Guid> idsPacientes, CancellationToken cancelacion = default)
    {
        if (!await PuedeConsultarAsync(cancelacion))
        {
            return RespuestaServicio<IReadOnlyDictionary<Guid, string>>.Fallida(MensajesDePacientes.SinPermiso);
        }

        var ids = (idsPacientes ?? []).Where(id => id != Guid.Empty).Distinct().ToArray();
        IReadOnlyList<NombreDePacienteVigente> filas = ids.Length == 0 ? [] : await _pacientes.ObtenerNombresDeVigentesAsync(ids, cancelacion);
        IReadOnlyDictionary<Guid, string> nombres = filas.ToDictionary(f => f.IdPaciente, f => f.Nombre);
        return RespuestaServicio<IReadOnlyDictionary<Guid, string>>.Correcta(nombres);
    }

    // El permiso se comprueba en el servidor contra el estado vigente del usuario y antes de cualquier lectura.
    private async Task<bool> PuedeConsultarAsync(CancellationToken cancelacion) =>
        _usuarioActual.IdUsuario is not null && await _autorizacion.TienePermisoAsync(Permiso.ConsultarPacientes, cancelacion);
}
