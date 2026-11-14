using TherapEase.Domain.Compartido.Reglas;

namespace TherapEase.UnitTests.Domain.Compartido;

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

    [Theory]
    [InlineData(2026, 11, 2, 10, 0, true)]
    [InlineData(1, 1, 1, 0, 0, true)]
    [InlineData(9999, 12, 31, 16, 59, true)]
    [InlineData(9999, 12, 31, 17, 0, false)]
    [InlineData(9999, 12, 31, 23, 59, false)]
    public void EsRepresentable_DiceSiElInstanteUtcCabeEnElRango(int anio, int mes, int dia, int hora, int minuto, bool esperado)
    {
        Assert.Equal(esperado, HoraHermosillo.EsRepresentable(new DateOnly(anio, mes, dia), new TimeOnly(hora, minuto)));
    }

    [Fact]
    public void EsRepresentable_CoincideConCuandoAInstanteNoFalla()
    {
        var fecha = new DateOnly(9999, 12, 31);

        Assert.True(HoraHermosillo.EsRepresentable(fecha, new TimeOnly(16, 59)));
        Assert.Equal(new DateTimeOffset(9999, 12, 31, 23, 59, 0, TimeSpan.Zero), HoraHermosillo.AInstante(fecha, new TimeOnly(16, 59)));
        Assert.False(HoraHermosillo.EsRepresentable(fecha, new TimeOnly(17, 0)));
        Assert.Throws<ArgumentOutOfRangeException>(() => HoraHermosillo.AInstante(fecha, new TimeOnly(17, 0)));
    }

    [Fact]
    public void ALocal_ConvierteCualquierInstanteALaHoraDeHermosillo()
    {
        var instante = new DateTimeOffset(2026, 11, 2, 17, 0, 0, TimeSpan.Zero);

        Assert.Equal(new DateTime(2026, 11, 2, 10, 0, 0), HoraHermosillo.ALocal(instante));
        Assert.Equal(new DateTime(2026, 11, 2, 10, 0, 0), HoraHermosillo.ALocal(instante.ToOffset(TimeSpan.FromHours(1))));
    }
}
