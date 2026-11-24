using TherapEase.Domain.Citas.Entidades.Enums;
namespace TherapEase.Web.Pages.Agenda;
public sealed record DatosDelFormularioDePago(Guid IdCita, int? VersionEsperada, EstadoPago? NuevoPago);
