using TherapEase.Application.Citas.Modelos;
using TherapEase.Domain.Pacientes.Entidades.Enums;

namespace TherapEase.Web.Pages.Agenda;

public sealed class DatosDelFormularioDeCita
{
    public Guid? IdPaciente { get; set; }
    public AmbitoAtencion? Ambito { get; set; }
    public DateOnly? Fecha { get; set; }
    public TimeOnly? HoraInicio { get; set; }
    public TimeOnly? HoraFin { get; set; }

    public DatosDeCitaEntrada ParaServicio() => new(IdPaciente, Ambito, Fecha, HoraInicio, HoraFin);
}
