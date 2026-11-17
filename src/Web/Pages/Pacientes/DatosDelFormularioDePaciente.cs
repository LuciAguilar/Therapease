using TherapEase.Application.Pacientes.Modelos;
using TherapEase.Domain.Pacientes.Entidades.Enums;

namespace TherapEase.Web.Pages.Pacientes;

// Solo campos editables; la validación de negocio y la edad pertenecen a Application.
public sealed class DatosDelFormularioDePaciente
{
    public string? Nombre { get; set; }
    public DateOnly? FechaNacimiento { get; set; }
    public string? Telefono { get; set; }
    public string? Correo { get; set; }
    public string? Escuela { get; set; }
    public string? Grupo { get; set; }
    public bool Escolar { get; set; }
    public bool Independiente { get; set; }
    public DatosDelFormularioDeTutor Tutor { get; set; } = new();

    public DatosDePacienteEntrada ParaServicio()
    {
        List<AmbitoAtencion> ambitos = [];
        if (Escolar) ambitos.Add(AmbitoAtencion.Escolar);
        if (Independiente) ambitos.Add(AmbitoAtencion.Independiente);
        return new(Nombre, FechaNacimiento, Telefono, Correo, Escuela, Grupo, ambitos, Tutor.ParaServicio());
    }

    public static DatosDelFormularioDePaciente DesdeFicha(FichaDePaciente ficha) => new()
    {
        Nombre = ficha.Nombre,
        FechaNacimiento = ficha.FechaNacimiento,
        Telefono = ficha.Telefono,
        Correo = ficha.Correo,
        Escuela = ficha.Escuela,
        Grupo = ficha.Grupo,
        Escolar = ficha.Ambitos.Contains(AmbitoAtencion.Escolar),
        Independiente = ficha.Ambitos.Contains(AmbitoAtencion.Independiente),
        Tutor = new()
        {
            Nombre = ficha.Tutor?.Nombre,
            Parentesco = ficha.Tutor?.Parentesco,
            Telefono = ficha.Tutor?.Telefono,
            Correo = ficha.Tutor?.Correo
        }
    };

    public override string ToString() => nameof(DatosDelFormularioDePaciente);
}

public sealed class DatosDelFormularioDeTutor
{
    public string? Nombre { get; set; }
    public string? Parentesco { get; set; }
    public string? Telefono { get; set; }
    public string? Correo { get; set; }

    public DatosDeTutorEntrada? ParaServicio() =>
        string.IsNullOrWhiteSpace(Nombre) && string.IsNullOrWhiteSpace(Parentesco)
        && string.IsNullOrWhiteSpace(Telefono) && string.IsNullOrWhiteSpace(Correo)
            ? null : new(Nombre, Parentesco, Telefono, Correo);

    public override string ToString() => nameof(DatosDelFormularioDeTutor);
}
