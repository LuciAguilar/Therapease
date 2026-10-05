using TherapEase.Application.Auditoria.Constantes;
using TherapEase.Application.Auditoria.Interfaces.Repositorios;
using TherapEase.Application.Auditoria.Interfaces.Servicios;
using TherapEase.Application.Auditoria.Modelos;
using TherapEase.Application.Compartido.Interfaces.Servicios;
using TherapEase.Application.Compartido.Modelos;
using TherapEase.Domain.Auditoria.Entidades.Enums;
using TherapEase.Domain.Auditoria.Reglas;
using TherapEase.Domain.Compartido.Entidades.Enums;

namespace TherapEase.Application.Auditoria.Servicios;

public class ServicioAuditoria : IServicioAuditoria
{
    private readonly IRepositorioAuditoria _repositorio;
    private readonly IAutorizacion _autorizacion;
    private readonly TimeProvider _tiempo;

    public ServicioAuditoria(IRepositorioAuditoria repositorio, IAutorizacion autorizacion, TimeProvider tiempo)
    {
        _repositorio = repositorio;
        _autorizacion = autorizacion;
        _tiempo = tiempo;
    }

    public async Task RegistrarHechoDeCambioAsync(
        Guid idActor,
        TipoRegistroAuditoria tipoRegistro,
        Guid idRegistro,
        AccionAuditoria accion,
        IEnumerable<string> camposAfectados,
        string hecho,
        CancellationToken cancelacion = default)
    {
        var evento = ReglasDeEventoAuditoria.Registrar(
            Guid.CreateVersion7(), _tiempo.GetUtcNow(), idActor, tipoRegistro, idRegistro, accion, camposAfectados, hecho);
        await _repositorio.AgregarAsync(evento, cancelacion);
    }

    public async Task<RespuestaServicio<IReadOnlyList<EventoConsultado>>> ConsultarEventosAutorizadosAsync(
        FiltroDeEventos filtro,
        CancellationToken cancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(filtro);

        if (!await _autorizacion.TienePermisoAsync(Permiso.ConsultarAuditoria, cancelacion))
        {
            return RespuestaServicio<IReadOnlyList<EventoConsultado>>.Fallida(MensajesDeAuditoria.SinPermiso);
        }

        if (filtro.Desde.ToUniversalTime() >= filtro.Hasta.ToUniversalTime()
            || filtro.Limite is < 1 or > FiltroDeEventos.LimiteMaximo
            || (filtro.TipoRegistro is { } tipo && !Enum.IsDefined(tipo)))
        {
            return RespuestaServicio<IReadOnlyList<EventoConsultado>>.Fallida(MensajesDeAuditoria.FiltroInvalido);
        }

        var eventos = await _repositorio.ConsultarAsync(
            filtro.Desde.ToUniversalTime(), filtro.Hasta.ToUniversalTime(), filtro.TipoRegistro, filtro.IdRegistro, filtro.Limite, cancelacion);

        IReadOnlyList<EventoConsultado> resultado = eventos
            .Select(e => new EventoConsultado(
                e.IdEvento, e.FechaEvento, e.IdUsuarioActor, e.TipoRegistro, e.IdRegistro, e.Accion, e.CamposAfectados, e.Hecho))
            .ToList();
        return RespuestaServicio<IReadOnlyList<EventoConsultado>>.Correcta(resultado);
    }
}
