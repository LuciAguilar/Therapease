using TherapEase.Domain.Compartido;
using TherapEase.Domain.Pacientes;

namespace TherapEase.UnitTests.Domain;

public class PacienteTests
{
    private static readonly Guid Actor = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OtroActor = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly DateTimeOffset Ahora = new(2026, 10, 5, 15, 0, 0, TimeSpan.Zero);

    private static Paciente NuevoPaciente(params AmbitoAtencion[] ambitos) =>
        Paciente.Registrar(Guid.NewGuid(), "Paciente Ficticio", "canario@ficticio.test", ambitos.Length == 0 ? [AmbitoAtencion.Escolar] : ambitos, Actor, Ahora);

    [Theory]
    [InlineData(new[] { AmbitoAtencion.Escolar })]
    [InlineData(new[] { AmbitoAtencion.Independiente })]
    [InlineData(new[] { AmbitoAtencion.Escolar, AmbitoAtencion.Independiente })]
    public void Registrar_ConUnoODosAmbitos_EsValidoYQuedaVigente(AmbitoAtencion[] ambitos)
    {
        var paciente = NuevoPaciente(ambitos);

        Assert.Equal(CondicionRegistro.Vigente, paciente.Condicion);
        Assert.Equal(1, paciente.Version);
        Assert.Equal(ambitos.Length, paciente.Ambitos.Count);
    }

    [Fact]
    public void Registrar_ConAmbitoRepetido_Rechaza()
    {
        Assert.Throws<ReglaDeNegocioException>(() =>
            NuevoPaciente(AmbitoAtencion.Escolar, AmbitoAtencion.Escolar));
    }

    [Fact]
    public void Registrar_SinAmbito_Rechaza()
    {
        Assert.Throws<ReglaDeNegocioException>(() =>
            Paciente.Registrar(Guid.NewGuid(), "Paciente Ficticio", null, [], Actor, Ahora));
    }

    [Fact]
    public void Registrar_ConAmbitoInexistente_Rechaza()
    {
        Assert.Throws<ReglaDeNegocioException>(() =>
            Paciente.Registrar(Guid.NewGuid(), "Paciente Ficticio", null, [(AmbitoAtencion)99], Actor, Ahora));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Registrar_SinNombre_Rechaza(string nombre)
    {
        Assert.Throws<ReglaDeNegocioException>(() =>
            Paciente.Registrar(Guid.NewGuid(), nombre, null, [AmbitoAtencion.Escolar], Actor, Ahora));
    }

    [Fact]
    public void Registrar_ConNombreDemasiadoLargo_Rechaza()
    {
        var nombre = new string('a', Paciente.LongitudMaximaNombre + 1);

        Assert.Throws<ReglaDeNegocioException>(() =>
            Paciente.Registrar(Guid.NewGuid(), nombre, null, [AmbitoAtencion.Escolar], Actor, Ahora));
    }

    [Fact]
    public void Registrar_SinActor_Rechaza()
    {
        Assert.Throws<ReglaDeNegocioException>(() =>
            Paciente.Registrar(Guid.NewGuid(), "Paciente Ficticio", null, [AmbitoAtencion.Escolar], Guid.Empty, Ahora));
    }

    [Fact]
    public void Registrar_LlenaFechaYUsuarioDeAltaEnUtc()
    {
        var local = new DateTimeOffset(2026, 10, 5, 8, 0, 0, TimeSpan.FromHours(-7));

        var paciente = Paciente.Registrar(Guid.NewGuid(), "Paciente Ficticio", null, [AmbitoAtencion.Escolar], Actor, local);

        Assert.Equal(Actor, paciente.IdUsuarioAlta);
        Assert.Equal(TimeSpan.Zero, paciente.FechaAlta.Offset);
        Assert.Equal(local, paciente.FechaAlta);
        Assert.Null(paciente.FechaActualizacion);
        Assert.Null(paciente.FechaBaja);
    }

    [Fact]
    public void Registrar_RecortaNombreYTratarContactoVacioComoAusente()
    {
        var paciente = Paciente.Registrar(Guid.NewGuid(), "  Paciente Ficticio  ", "   ", [AmbitoAtencion.Escolar], Actor, Ahora);

        Assert.Equal("Paciente Ficticio", paciente.Nombre);
        Assert.Null(paciente.Contacto);
    }

    [Fact]
    public void Actualizar_ConVersionVigente_CambiaDatosIncrementaVersionYLlenaMetadatos()
    {
        var paciente = NuevoPaciente();
        var despues = Ahora.AddHours(1);

        paciente.Actualizar("Nombre Nuevo", null, 1, OtroActor, despues);

        Assert.Equal("Nombre Nuevo", paciente.Nombre);
        Assert.Equal(2, paciente.Version);
        Assert.Equal(OtroActor, paciente.IdUsuarioActualizacion);
        Assert.Equal(despues, paciente.FechaActualizacion);
    }

    [Fact]
    public void Actualizar_ConVersionAnterior_LanzaConflictoYNoCambiaNada()
    {
        var paciente = NuevoPaciente();
        paciente.Actualizar("Primera Edicion", null, 1, Actor, Ahora);

        Assert.Throws<ConflictoDeVersionException>(() =>
            paciente.Actualizar("Segunda Edicion", null, 1, OtroActor, Ahora));

        Assert.Equal("Primera Edicion", paciente.Nombre);
        Assert.Equal(2, paciente.Version);
    }

    [Fact]
    public void Actualizar_UnaFichaDeBaja_RechazaSinCambioSilencioso()
    {
        var paciente = NuevoPaciente();
        paciente.DarDeBaja(1, Actor, Ahora);

        var error = Assert.Throws<ReglaDeNegocioException>(() =>
            paciente.Actualizar("Otro Nombre", null, 2, Actor, Ahora));

        Assert.Contains("baja", error.Message);
        Assert.Equal("Paciente Ficticio", paciente.Nombre);
    }

    [Fact]
    public void Actualizar_ConDatosInvalidos_NoModificaNada()
    {
        var paciente = NuevoPaciente();

        Assert.Throws<ReglaDeNegocioException>(() => paciente.Actualizar("   ", "contacto nuevo", 1, Actor, Ahora));

        Assert.Equal("Paciente Ficticio", paciente.Nombre);
        Assert.Equal("canario@ficticio.test", paciente.Contacto);
        Assert.Equal(1, paciente.Version);
    }

    [Fact]
    public void DarDeBaja_MarcaBajaConFechaYUsuarioSinBorrarDatos()
    {
        var paciente = NuevoPaciente(AmbitoAtencion.Escolar, AmbitoAtencion.Independiente);

        paciente.DarDeBaja(1, OtroActor, Ahora);

        Assert.Equal(CondicionRegistro.Baja, paciente.Condicion);
        Assert.Equal(Ahora, paciente.FechaBaja);
        Assert.Equal(OtroActor, paciente.IdUsuarioBaja);
        Assert.Equal(2, paciente.Version);
        Assert.Equal("Paciente Ficticio", paciente.Nombre);
        Assert.Equal(2, paciente.Ambitos.Count);
    }

    [Fact]
    public void DarDeBaja_UnaFichaYaDeBaja_InformaSinDuplicarElCambio()
    {
        var paciente = NuevoPaciente();
        paciente.DarDeBaja(1, Actor, Ahora);

        Assert.Throws<ReglaDeNegocioException>(() => paciente.DarDeBaja(2, Actor, Ahora));
        Assert.Equal(2, paciente.Version);
    }

    [Fact]
    public void Recuperar_DevuelveAVigenteYLimpiaDatosDeBaja()
    {
        var paciente = NuevoPaciente();
        paciente.DarDeBaja(1, Actor, Ahora);

        paciente.Recuperar(2, OtroActor, Ahora.AddDays(1));

        Assert.Equal(CondicionRegistro.Vigente, paciente.Condicion);
        Assert.Null(paciente.FechaBaja);
        Assert.Null(paciente.IdUsuarioBaja);
        Assert.Equal(3, paciente.Version);
    }

    [Fact]
    public void Recuperar_UnaFichaYaVigente_Informa()
    {
        var paciente = NuevoPaciente();

        Assert.Throws<ReglaDeNegocioException>(() => paciente.Recuperar(1, Actor, Ahora));
    }

    [Fact]
    public void DarDeBajaYRecuperar_ConVersionAnterior_LanzanConflicto()
    {
        var paciente = NuevoPaciente();

        Assert.Throws<ConflictoDeVersionException>(() => paciente.DarDeBaja(7, Actor, Ahora));
        paciente.DarDeBaja(1, Actor, Ahora);
        Assert.Throws<ConflictoDeVersionException>(() => paciente.Recuperar(1, Actor, Ahora));
    }
}
