namespace TherapEase.Application.Identidad.Modelos;

// Se entrega una sola vez a quien la comunica; ToString oculta el secreto para que no llegue a registros (Q04).
public sealed class ContrasenaTemporalEntregada
{
    public ContrasenaTemporalEntregada(DatosUsuario usuario, string contrasenaTemporal)
    {
        Usuario = usuario;
        ContrasenaTemporal = contrasenaTemporal;
    }

    public DatosUsuario Usuario { get; }

    public string ContrasenaTemporal { get; }

    public override string ToString() => $"ContrasenaTemporalEntregada {{ Usuario = {Usuario.IdUsuario}, ContrasenaTemporal = [oculta] }}";
}
