using TherapEase.Domain.Citas.Constantes;
using TherapEase.Domain.Citas.Reglas;
using TherapEase.Domain.Compartido.Excepciones;

namespace TherapEase.UnitTests.Domain.Citas;

public class ReglasDeHorarioDeCitaTests
{
    private static readonly DateOnly Lunes = new(2026, 11, 2);

    private static TimeOnly Hora(int minutosDesdeMedianoche) => new(minutosDesdeMedianoche / 60, minutosDesdeMedianoche % 60);

    private static TimeOnly Hora(int hora, int minuto) => new(hora, minuto);

    // Lista escrita aparte de la regla, con números literales del horario decidido el 01-nov-2026:
    // una hora para inicios de 9:00 a 18:00 (541 minutos) y 30 minutos para inicios de 15:00 a 18:30 (211 minutos).
    private static HashSet<(int Inicio, int Fin)> CombinacionesValidasIndependientes()
    {
        var validas = new HashSet<(int, int)>();
        for (var inicio = 540; inicio <= 1080; inicio++)
        {
            validas.Add((inicio, inicio + 60));
        }

        for (var inicio = 900; inicio <= 1110; inicio++)
        {
            validas.Add((inicio, inicio + 30));
        }

        return validas;
    }

    [Fact]
    public void ListaIndependiente_TieneLas752CombinacionesValidas()
    {
        Assert.Equal(752, CombinacionesValidasIndependientes().Count);
    }

    // Compara los 1 440 x 1 440 pares de minutos del día: ninguna combinación válida se rechaza ni ninguna inválida pasa.
    [Fact]
    public void ValidarHoras_AceptaExactamenteLasCombinacionesDeLaListaIndependiente()
    {
        var validas = CombinacionesValidasIndependientes();
        var horas = Enumerable.Range(0, 1440).Select(minuto => Hora(minuto)).ToArray();
        var diferencias = new List<string>();

        for (var inicio = 0; inicio < 1440; inicio++)
        {
            for (var fin = 0; fin < 1440; fin++)
            {
                var aceptada = ReglasDeHorarioDeCita.ValidarHoras(horas[inicio], horas[fin]) is null;
                if (aceptada != validas.Contains((inicio, fin)))
                {
                    diferencias.Add($"{inicio}-{fin}: regla {(aceptada ? "acepta" : "rechaza")}");
                }
            }
        }

        Assert.Empty(diferencias.Take(10));
        Assert.Empty(diferencias);
    }

    [Theory]
    [InlineData(9, 0, 10, 0)]
    [InlineData(10, 15, 11, 15)]
    [InlineData(14, 30, 15, 30)]
    [InlineData(14, 59, 15, 59)]
    [InlineData(15, 0, 15, 30)]
    [InlineData(15, 0, 16, 0)]
    [InlineData(15, 17, 15, 47)]
    [InlineData(18, 0, 19, 0)]
    [InlineData(18, 30, 19, 0)]
    public void ValidarHoras_AceptaLosEjemplosDelPlan(int horaInicio, int minutoInicio, int horaFin, int minutoFin)
    {
        Assert.Null(ReglasDeHorarioDeCita.ValidarHoras(Hora(horaInicio, minutoInicio), Hora(horaFin, minutoFin)));
    }

    [Theory]
    [InlineData(10, 0, 10, 30, MensajesDeHorarioDeCita.DuracionAntesDeLas15)]
    [InlineData(10, 0, 12, 0, MensajesDeHorarioDeCita.DuracionAntesDeLas15)]
    [InlineData(14, 30, 15, 0, MensajesDeHorarioDeCita.DuracionAntesDeLas15)]
    [InlineData(14, 59, 15, 29, MensajesDeHorarioDeCita.DuracionAntesDeLas15)]
    [InlineData(15, 0, 15, 45, MensajesDeHorarioDeCita.DuracionDesdeLas15)]
    [InlineData(15, 0, 17, 0, MensajesDeHorarioDeCita.DuracionDesdeLas15)]
    [InlineData(8, 59, 9, 59, MensajesDeHorarioDeCita.FueraDelHorarioDeAtencion)]
    [InlineData(0, 0, 1, 0, MensajesDeHorarioDeCita.FueraDelHorarioDeAtencion)]
    [InlineData(18, 30, 19, 30, MensajesDeHorarioDeCita.FueraDelHorarioDeAtencion)]
    [InlineData(19, 0, 20, 0, MensajesDeHorarioDeCita.FueraDelHorarioDeAtencion)]
    [InlineData(10, 0, 10, 0, MensajesDeHorarioDeCita.FinNoPosteriorAlInicio)]
    [InlineData(10, 0, 9, 0, MensajesDeHorarioDeCita.FinNoPosteriorAlInicio)]
    public void ValidarHoras_RechazaConElMensajeFijoQueCorresponde(int horaInicio, int minutoInicio, int horaFin, int minutoFin, string esperado)
    {
        Assert.Equal(esperado, ReglasDeHorarioDeCita.ValidarHoras(Hora(horaInicio, minutoInicio), Hora(horaFin, minutoFin)));
    }

    // Hay una sola fecha: «18:00 a 10:00 del día siguiente» no se puede expresar y se rechaza como un fin que no es posterior al inicio.
    [Fact]
    public void ValidarHoras_UnFinDelDiaSiguiente_NoEsExpresableYSeRechazaComoFinAnterior()
    {
        Assert.Equal(MensajesDeHorarioDeCita.FinNoPosteriorAlInicio, ReglasDeHorarioDeCita.ValidarHoras(Hora(18, 0), Hora(10, 0)));
        Assert.Equal(MensajesDeHorarioDeCita.FinNoPosteriorAlInicio, ReglasDeHorarioDeCita.ValidarHoras(Hora(18, 0), Hora(0, 0)));
    }

    [Theory]
    [InlineData(10, 0, 30, 11, 0, 0)]
    [InlineData(10, 0, 0, 11, 0, 30)]
    [InlineData(10, 0, 59, 11, 0, 59)]
    public void ValidarHoras_ConSegundos_Rechaza(int hora, int minuto, int segundo, int horaFin, int minutoFin, int segundoFin)
    {
        var rechazo = ReglasDeHorarioDeCita.ValidarHoras(new TimeOnly(hora, minuto, segundo), new TimeOnly(horaFin, minutoFin, segundoFin));

        Assert.Equal(MensajesDeHorarioDeCita.HorasConSegundos, rechazo);
    }

    [Fact]
    public void ValidarHoras_ConUnaFraccionDeSegundo_Rechaza()
    {
        var inicioConFraccion = new TimeOnly(Hora(10, 0).Ticks + 1);
        var finConFraccion = new TimeOnly(Hora(11, 0).Ticks + TimeSpan.TicksPerMillisecond);

        Assert.Equal(MensajesDeHorarioDeCita.HorasConSegundos, ReglasDeHorarioDeCita.ValidarHoras(inicioConFraccion, Hora(11, 0)));
        Assert.Equal(MensajesDeHorarioDeCita.HorasConSegundos, ReglasDeHorarioDeCita.ValidarHoras(Hora(10, 0), finConFraccion));
    }

    // Los segundos se rechazan antes que cualquier otra regla: el mensaje no depende del orden de las demás comprobaciones.
    [Fact]
    public void ValidarHoras_ConSegundosYOtroDefecto_PrefiereElMensajeDeSegundos()
    {
        Assert.Equal(MensajesDeHorarioDeCita.HorasConSegundos, ReglasDeHorarioDeCita.ValidarHoras(new TimeOnly(8, 0, 30), Hora(7, 0)));
    }

    [Fact]
    public void Calcular_DiezDeLaMananaAOnce_DaLosInstantesUtcDeHermosillo()
    {
        var horario = ReglasDeHorarioDeCita.Calcular(Lunes, Hora(10, 0), Hora(11, 0));

        Assert.Equal(new DateTimeOffset(2026, 11, 2, 17, 0, 0, TimeSpan.Zero), horario.Inicio);
        Assert.Equal(new DateTimeOffset(2026, 11, 2, 18, 0, 0, TimeSpan.Zero), horario.Fin);
        Assert.Equal(TimeSpan.Zero, horario.Inicio.Offset);
        Assert.Equal(TimeSpan.Zero, horario.Fin.Offset);
    }

    [Fact]
    public void Calcular_CatorceTreintaAQuinceTreinta_SePermiteYConservaLaHoraLocal()
    {
        var horario = ReglasDeHorarioDeCita.Calcular(Lunes, Hora(14, 30), Hora(15, 30));

        Assert.Equal(new DateTimeOffset(2026, 11, 2, 21, 30, 0, TimeSpan.Zero), horario.Inicio);
        Assert.Equal(new DateTimeOffset(2026, 11, 2, 22, 30, 0, TimeSpan.Zero), horario.Fin);
    }

    // Los siete días de la semana comparten el mismo horario (decisión del 01-nov-2026): ninguno se trata distinto.
    [Fact]
    public void Calcular_AceptaLosSieteDiasConElMismoHorario()
    {
        var dias = Enumerable.Range(0, 7).Select(Lunes.AddDays).ToList();

        Assert.Equal(7, dias.Select(d => d.DayOfWeek).Distinct().Count());
        foreach (var dia in dias)
        {
            var horario = ReglasDeHorarioDeCita.Calcular(dia, Hora(15, 0), Hora(15, 30));

            Assert.Equal(new DateTimeOffset(dia.Year, dia.Month, dia.Day, 22, 0, 0, TimeSpan.Zero), horario.Inicio);
            Assert.Equal(TimeSpan.FromMinutes(30), horario.Fin - horario.Inicio);
        }
    }

    // Una fecha pasada se permite con las mismas reglas: «Registrada después de su horario» no la rechaza.
    [Fact]
    public void Calcular_ConFechaPasada_SePermite()
    {
        var horario = ReglasDeHorarioDeCita.Calcular(new DateOnly(2020, 1, 15), Hora(9, 0), Hora(10, 0));

        Assert.Equal(new DateTimeOffset(2020, 1, 15, 16, 0, 0, TimeSpan.Zero), horario.Inicio);
    }

    [Theory]
    [InlineData(10, 0, 10, 30, MensajesDeHorarioDeCita.DuracionAntesDeLas15)]
    [InlineData(8, 0, 9, 0, MensajesDeHorarioDeCita.FueraDelHorarioDeAtencion)]
    [InlineData(11, 0, 10, 0, MensajesDeHorarioDeCita.FinNoPosteriorAlInicio)]
    public void Calcular_ConHorasInvalidas_LanzaReglaDeNegocioConMensajeFijo(int horaInicio, int minutoInicio, int horaFin, int minutoFin, string esperado)
    {
        var excepcion = Assert.Throws<ReglaDeNegocioException>(() =>
            ReglasDeHorarioDeCita.Calcular(Lunes, Hora(horaInicio, minutoInicio), Hora(horaFin, minutoFin)));

        Assert.Equal(esperado, excepcion.Message);
    }

    // El último día representable: con el fin desde las 17:00 el instante UTC pasaría del 31 de diciembre de 9999 y se rechaza de forma controlada.
    [Fact]
    public void Calcular_EnElUltimoDiaRepresentable_RechazaSinEscaparUnaExcepcionDeRango()
    {
        var excepcion = Assert.Throws<ReglaDeNegocioException>(() =>
            ReglasDeHorarioDeCita.Calcular(DateOnly.MaxValue, Hora(17, 0), Hora(18, 0)));

        Assert.Equal(MensajesDeHorarioDeCita.FechaNoRepresentable, excepcion.Message);
    }

    [Fact]
    public void Calcular_EnElUltimoDiaRepresentable_ConHorasQueSiCaben_Funciona()
    {
        var horario = ReglasDeHorarioDeCita.Calcular(DateOnly.MaxValue, Hora(9, 0), Hora(10, 0));

        Assert.Equal(new DateTimeOffset(9999, 12, 31, 16, 0, 0, TimeSpan.Zero), horario.Inicio);
    }

    [Fact]
    public void Calcular_ElInicioQueCabeYElFinQueNo_RechazaConElMensajeDeFecha()
    {
        var excepcion = Assert.Throws<ReglaDeNegocioException>(() =>
            ReglasDeHorarioDeCita.Calcular(DateOnly.MaxValue, Hora(16, 0), Hora(17, 0)));

        Assert.Equal(MensajesDeHorarioDeCita.FechaNoRepresentable, excepcion.Message);
    }

    [Fact]
    public void Calcular_EnElPrimerDiaRepresentable_Funciona()
    {
        var horario = ReglasDeHorarioDeCita.Calcular(DateOnly.MinValue, Hora(9, 0), Hora(10, 0));

        Assert.True(horario.Inicio < horario.Fin);
        Assert.Equal(TimeSpan.FromHours(1), horario.Fin - horario.Inicio);
    }

    // Los mensajes son fijos: ninguno repite la fecha ni las horas escritas.
    [Fact]
    public void LosMensajes_NoIncluyenValoresEscritos()
    {
        var excepcion = Assert.Throws<ReglaDeNegocioException>(() =>
            ReglasDeHorarioDeCita.Calcular(new DateOnly(2031, 7, 9), Hora(10, 17), Hora(10, 47)));

        Assert.DoesNotContain("2031", excepcion.Message);
        Assert.DoesNotContain("10:17", excepcion.Message);
        Assert.DoesNotContain("10:47", excepcion.Message);
    }
}
