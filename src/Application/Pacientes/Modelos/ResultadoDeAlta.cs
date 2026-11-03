namespace TherapEase.Application.Pacientes.Modelos;

public enum EstadoDeAlta
{
    // Se guardó ahora: el cambio y su evento se confirmaron.
    Registrado,

    // Era el mismo envío de antes, ya confirmado: sin segunda ficha, evento ni versión.
    YaRegistrado,

    // Hay fichas vigentes con el mismo nombre y no se guardó nada: esperar la decisión de abrir una o registrar a otra persona.
    CoincidenciasPorConfirmar
}

// No decidir la navegación solo por Exito: con CoincidenciasPorConfirmar es falso y trae la lista permitida.
public sealed record ResultadoDeAlta(
    EstadoDeAlta Estado,
    Guid? IdPaciente = null,
    IReadOnlyList<ResumenDePaciente>? Coincidencias = null,
    bool HayMasCoincidencias = false)
{
    public override string ToString() => nameof(ResultadoDeAlta);
}

public enum EstadoDeEdicion
{
    Actualizado,

    // Con la versión vigente y datos iguales a los guardados: no se escribió nada ni se registró evento.
    SinCambios
}

public sealed record ResultadoDeEdicion(EstadoDeEdicion Estado, FichaDePaciente Ficha)
{
    public override string ToString() => nameof(ResultadoDeEdicion);
}
