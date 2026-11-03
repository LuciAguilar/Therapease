using TherapEase.Domain.Pacientes.Reglas;

namespace TherapEase.UnitTests.Domain.Pacientes;

public class ReglasDeEdadTests
{
    [Theory]
    [InlineData(2008, 10, 5, 2026, 10, 5, 18)]
    [InlineData(2008, 10, 6, 2026, 10, 5, 17)]
    [InlineData(2008, 10, 4, 2026, 10, 5, 18)]
    [InlineData(2026, 10, 5, 2026, 10, 5, 0)]
    [InlineData(2025, 10, 6, 2026, 10, 5, 0)]
    [InlineData(1990, 1, 15, 2026, 10, 5, 36)]
    [InlineData(1900, 1, 1, 2026, 10, 5, 126)]
    public void Calcular_CuentaLosAniosCumplidosALaFechaDeHoy(int an, int mn, int dn, int ah, int mh, int dh, int esperada)
    {
        Assert.Equal(esperada, ReglasDeEdad.Calcular(new DateOnly(an, mn, dn), new DateOnly(ah, mh, dh)));
    }

    [Fact]
    public void Calcular_QuienNacioUn29DeFebreroCumpleElUnoDeMarzoEnAnioNoBisiesto()
    {
        var nacido = new DateOnly(2000, 2, 29);

        Assert.Equal(17, ReglasDeEdad.Calcular(nacido, new DateOnly(2018, 2, 28)));
        Assert.Equal(18, ReglasDeEdad.Calcular(nacido, new DateOnly(2018, 3, 1)));
        Assert.Equal(20, ReglasDeEdad.Calcular(nacido, new DateOnly(2020, 2, 29)));
    }

    [Fact]
    public void EsMenorDeEdad_SeDejaDeSerElDiaDelCumpleanosDieciocho()
    {
        var hoy = new DateOnly(2026, 10, 5);

        Assert.True(ReglasDeEdad.EsMenorDeEdad(new DateOnly(2008, 10, 6), hoy));
        Assert.False(ReglasDeEdad.EsMenorDeEdad(new DateOnly(2008, 10, 5), hoy));
        Assert.False(ReglasDeEdad.EsMenorDeEdad(new DateOnly(2000, 1, 1), hoy));
    }

    [Fact]
    public void EsMenorDeEdad_QuienNacioUn29DeFebreroEsMenorHastaElUnoDeMarzoDeLosDieciocho()
    {
        var nacido = new DateOnly(2008, 2, 29);

        Assert.True(ReglasDeEdad.EsMenorDeEdad(nacido, new DateOnly(2026, 2, 28)));
        Assert.False(ReglasDeEdad.EsMenorDeEdad(nacido, new DateOnly(2026, 3, 1)));
    }

    [Theory]
    [InlineData(2026, 10, 5, 15, 0, 2026, 10, 5)]
    [InlineData(2026, 10, 6, 3, 0, 2026, 10, 5)]
    [InlineData(2026, 10, 6, 6, 59, 2026, 10, 5)]
    [InlineData(2026, 10, 6, 7, 0, 2026, 10, 6)]
    [InlineData(2026, 7, 1, 0, 0, 2026, 6, 30)]
    public void HoyEnHermosillo_UsaUtcMenosSieteTodoElAnio(int a, int m, int d, int h, int min, int ea, int em, int ed)
    {
        var ahora = new DateTimeOffset(a, m, d, h, min, 0, TimeSpan.Zero);

        Assert.Equal(new DateOnly(ea, em, ed), ReglasDeEdad.HoyEnHermosillo(ahora));
    }
}
