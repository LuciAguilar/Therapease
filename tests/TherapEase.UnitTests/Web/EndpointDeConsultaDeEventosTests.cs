using TherapEase.Domain.Auditoria.Entidades.Enums;
using TherapEase.Web.Endpoints;

namespace TherapEase.UnitTests.Web;

public class EndpointDeConsultaDeEventosTests
{
    public static TheoryData<TipoRegistroAuditoria> TiposDefinidos() => new(Enum.GetValues<TipoRegistroAuditoria>());

    [Theory]
    [MemberData(nameof(TiposDefinidos))]
    public void TryParsearTipoDeRegistro_ConCadaNombreCompleto_DevuelveEseTipo(TipoRegistroAuditoria esperado)
    {
        var aceptado = EndpointDeConsultaDeEventos.TryParsearTipoDeRegistro(esperado.ToString(), out var tipo);

        Assert.True(aceptado);
        Assert.Equal(esperado, tipo);
    }

    [Theory]
    [InlineData("paciente", TipoRegistroAuditoria.Paciente)]
    [InlineData("CITA", TipoRegistroAuditoria.Cita)]
    [InlineData("uSuArIo", TipoRegistroAuditoria.Usuario)]
    public void TryParsearTipoDeRegistro_SinDistinguirMayusculas_DevuelveElTipo(string valor, TipoRegistroAuditoria esperado)
    {
        var aceptado = EndpointDeConsultaDeEventos.TryParsearTipoDeRegistro(valor, out var tipo);

        Assert.True(aceptado);
        Assert.Equal(esperado, tipo);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("1")]
    [InlineData("2")]
    [InlineData("3")]
    [InlineData("-1")]
    [InlineData("Paciente,Cita")]
    [InlineData("Paciente, Cita")]
    [InlineData("Cita,Usuario,Paciente")]
    [InlineData("Paciente,")]
    [InlineData(",Paciente")]
    [InlineData(" Paciente")]
    [InlineData("Paciente ")]
    [InlineData("Pacient")]
    [InlineData("Inexistente")]
    [InlineData("")]
    public void TryParsearTipoDeRegistro_ConNumeroListaOEntradaNoExacta_Rechaza(string valor)
    {
        var aceptado = EndpointDeConsultaDeEventos.TryParsearTipoDeRegistro(valor, out _);

        Assert.False(aceptado);
    }
}
