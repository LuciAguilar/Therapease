using TherapEase.Domain.Compartido;

namespace TherapEase.UnitTests.Domain;

public class HoraHermosilloTests
{
    [Fact]
    public void AInstante_DiezDeLaManana_EsLasDiecisieteUtc()
    {
        var instante = HoraHermosillo.AInstante(new DateOnly(2026, 11, 2), new TimeOnly(10, 0));

        Assert.Equal(new DateTimeOffset(2026, 11, 2, 17, 0, 0, TimeSpan.Zero), instante);
        Assert.Equal(TimeSpan.Zero, instante.Offset);
    }

    [Fact]
    public void ALocal_ViajaDeIdaYVueltaSinCambiarLaHora()
    {
        var fecha = new DateOnly(2026, 11, 2);
        var hora = new TimeOnly(10, 30);

        var local = HoraHermosillo.ALocal(HoraHermosillo.AInstante(fecha, hora));

        Assert.Equal(fecha.ToDateTime(hora), local);
    }

    [Theory]
    [InlineData(3, 15)]
    [InlineData(7, 1)]
    [InlineData(10, 25)]
    [InlineData(11, 1)]
    public void AInstante_NoCambiaConElHorarioDeVeranoDeOtrasZonas(int mes, int dia)
    {
        var instante = HoraHermosillo.AInstante(new DateOnly(2026, mes, dia), new TimeOnly(10, 0));

        Assert.Equal(17, instante.UtcDateTime.Hour);
    }

    [Fact]
    public void ALocal_ConvierteCualquierInstanteALaHoraDeHermosillo()
    {
        var instante = new DateTimeOffset(2026, 11, 2, 17, 0, 0, TimeSpan.Zero);

        Assert.Equal(new DateTime(2026, 11, 2, 10, 0, 0), HoraHermosillo.ALocal(instante));
        Assert.Equal(new DateTime(2026, 11, 2, 10, 0, 0), HoraHermosillo.ALocal(instante.ToOffset(TimeSpan.FromHours(1))));
    }
}
