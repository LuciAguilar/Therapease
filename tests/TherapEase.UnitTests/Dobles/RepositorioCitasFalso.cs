using TherapEase.Application.Citas.Interfaces.Repositorios;
using TherapEase.Application.Citas.Modelos;
using TherapEase.Domain.Citas.Entidades;
using TherapEase.Domain.Citas.Entidades.Enums;
using TherapEase.Domain.Compartido.Entidades.Enums;

namespace TherapEase.UnitTests.Dobles;

internal sealed class RepositorioCitasFalso : IRepositorioCitas
{
    public Dictionary<Guid, Cita> Citas { get; } = [];

    // Para simular una carrera: se ejecuta justo antes de guardar el alta y puede insertar a la gemela o lanzar la excepción de la base.
    public Action<Cita>? AlAgregar { get; set; }

    // Se ejecuta antes de responder cada lectura por número, con su ordinal (1 para la primera): permite confirmar a la gemela justo antes de la lectura que se quiera.
    public Action<int>? AlLeerPorNumero { get; set; }

    public int LecturasParaComparacion { get; private set; }

    public int LecturasParaEscribir { get; private set; }

    public int ConsultasDeAgenda { get; private set; }

    public (DateTimeOffset Desde, DateTimeOffset Hasta, VistaDeAgenda Vista, int Limite)? UltimaConsultaDeAgenda { get; private set; }

    public int Lecturas => LecturasParaComparacion + LecturasParaEscribir + ConsultasDeAgenda;

    public Task AgregarAsync(Cita cita, CancellationToken cancelacion = default)
    {
        AlAgregar?.Invoke(cita);
        Citas[cita.IdCita] = cita;
        return Task.CompletedTask;
    }

    public Task<Cita?> ObtenerAsync(Guid idCita, CancellationToken cancelacion = default)
    {
        LecturasParaEscribir++;
        return Task.FromResult(Citas.GetValueOrDefault(idCita));
    }

    public Task<Cita?> ObtenerParaComparacionAsync(Guid idCita, CancellationToken cancelacion = default)
    {
        LecturasParaComparacion++;
        AlLeerPorNumero?.Invoke(LecturasParaComparacion);
        return Task.FromResult(Citas.GetValueOrDefault(idCita));
    }

    public Task<IReadOnlyList<FilaDeCitaDeAgenda>> ConsultarAgendaAsync(
        DateTimeOffset desde, DateTimeOffset hasta, VistaDeAgenda vista, int limite, CancellationToken cancelacion = default)
    {
        ConsultasDeAgenda++;
        UltimaConsultaDeAgenda = (desde, hasta, vista, limite);
        var estado = vista == VistaDeAgenda.Activas ? EstadoCita.Agendada : EstadoCita.Cancelada;
        IReadOnlyList<FilaDeCitaDeAgenda> filas = Citas.Values
            .Where(c => c.Condicion == CondicionRegistro.Vigente && c.Estado == estado && c.Fin > desde && c.Inicio < hasta)
            .OrderBy(c => c.Inicio)
            .ThenBy(c => c.IdCita)
            .Take(limite)
            .Select(c => new FilaDeCitaDeAgenda(c.IdCita, c.IdPaciente, c.Ambito, c.Estado, c.Inicio, c.Fin, c.FechaAlta > c.Inicio))
            .ToList();
        return Task.FromResult(filas);
    }

    public Task<bool> ExistenActivasDePacienteAsync(Guid idPaciente, CancellationToken cancelacion = default) =>
        Task.FromResult(Citas.Values.Any(c => c.IdPaciente == idPaciente && c.EsActiva));
}
