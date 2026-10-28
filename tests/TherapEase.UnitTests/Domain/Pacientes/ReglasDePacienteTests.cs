using TherapEase.Domain.Compartido.Entidades.Enums;
using TherapEase.Domain.Compartido.Excepciones;
using TherapEase.Domain.Pacientes.Constantes;
using TherapEase.Domain.Pacientes.Entidades;
using TherapEase.Domain.Pacientes.Entidades.Enums;
using TherapEase.Domain.Pacientes.Reglas;

namespace TherapEase.UnitTests.Domain.Pacientes;

public class ReglasDePacienteTests
{
    private static readonly Guid Actor = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OtroActor = Guid.Parse("22222222-2222-2222-2222-222222222222");

    // 5-oct-2026, 08:00 en Hermosillo (UTC-7, sin horario de verano).
    private static readonly DateTimeOffset Ahora = new(2026, 10, 5, 15, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Hoy = new(2026, 10, 5);
    private static readonly DateOnly NacimientoAdulto = new(1990, 1, 15);
    private static readonly DateOnly NacimientoMenor = new(2015, 3, 20);
    private static readonly DatosDeTutor TutorValido = new("Tutora Ficticia", "Madre", "6441234567", null);

    private static DatosDePaciente Adulto(string nombre = "Paciente Ficticio", string? correo = "canario@ficticio.test") =>
        new(nombre, NacimientoAdulto, Correo: correo);

    private static Paciente NuevoPaciente(params AmbitoAtencion[] ambitos) =>
        ReglasDePaciente.Registrar(Guid.NewGuid(), Adulto(), ambitos.Length == 0 ? [AmbitoAtencion.Escolar] : ambitos, Actor, Ahora);

    private static Paciente NuevoMenor() =>
        ReglasDePaciente.Registrar(Guid.NewGuid(), new DatosDePaciente("Menor Ficticio", NacimientoMenor, Tutor: TutorValido), [AmbitoAtencion.Escolar], Actor, Ahora);

    // Una ficha anterior a B02: sin nacimiento. El setter es interno de Domain, por eso se usa reflexión solo en la prueba.
    private static Paciente FichaAnterior()
    {
        var paciente = NuevoPaciente();
        typeof(Paciente).GetProperty(nameof(Paciente.FechaNacimiento))!.SetValue(paciente, null);
        return paciente;
    }

    private static string Mensaje(Action accion) => Assert.Throws<ReglaDeNegocioException>(accion).Message;

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
    public void Ambitos_SeExponenSoloLectura_NoSePuedenModificarDesdeFuera()
    {
        var paciente = NuevoPaciente(AmbitoAtencion.Escolar, AmbitoAtencion.Independiente);

        var ambitos = paciente.Ambitos;

        Assert.IsNotType<List<PacienteAmbito>>(ambitos);
        var comoColeccion = Assert.IsAssignableFrom<ICollection<PacienteAmbito>>(ambitos);
        Assert.True(comoColeccion.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => comoColeccion.Clear());
        Assert.Throws<NotSupportedException>(() => comoColeccion.Remove(ambitos.First()));
        Assert.Equal(2, paciente.Ambitos.Count);
    }

    [Fact]
    public void Ambitos_ReflejanLosRegistradosPorLasReglas()
    {
        var paciente = NuevoPaciente(AmbitoAtencion.Escolar, AmbitoAtencion.Independiente);

        Assert.Equal(
            [AmbitoAtencion.Escolar, AmbitoAtencion.Independiente],
            paciente.Ambitos.Select(a => a.Ambito).Order());
        Assert.All(paciente.Ambitos, a => Assert.Equal(paciente.IdPaciente, a.IdPaciente));
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
            ReglasDePaciente.Registrar(Guid.NewGuid(), Adulto(), [], Actor, Ahora));
    }

    [Fact]
    public void Registrar_ConAmbitoInexistente_Rechaza()
    {
        Assert.Throws<ReglaDeNegocioException>(() =>
            ReglasDePaciente.Registrar(Guid.NewGuid(), Adulto(), [(AmbitoAtencion)99], Actor, Ahora));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("A")]
    public void Registrar_SinNombreOConNombreMuyCorto_Rechaza(string nombre)
    {
        Assert.Equal(
            MensajesDeDatosDePaciente.NombreInvalido,
            Mensaje(() => ReglasDePaciente.Registrar(Guid.NewGuid(), Adulto(nombre), [AmbitoAtencion.Escolar], Actor, Ahora)));
    }

    [Fact]
    public void Registrar_ConNombreDeLaLongitudMaximaEsValidoYUnCaracterMasSeRechaza()
    {
        var limite = new string('a', Paciente.LongitudMaximaNombre);

        var paciente = ReglasDePaciente.Registrar(Guid.NewGuid(), Adulto(limite), [AmbitoAtencion.Escolar], Actor, Ahora);

        Assert.Equal(limite, paciente.Nombre);
        Assert.Throws<ReglaDeNegocioException>(() =>
            ReglasDePaciente.Registrar(Guid.NewGuid(), Adulto(limite + "a"), [AmbitoAtencion.Escolar], Actor, Ahora));
    }

    [Fact]
    public void Registrar_SinActor_Rechaza()
    {
        Assert.Throws<ReglaDeNegocioException>(() =>
            ReglasDePaciente.Registrar(Guid.NewGuid(), Adulto(), [AmbitoAtencion.Escolar], Guid.Empty, Ahora));
    }

    [Fact]
    public void Registrar_SinIdentificador_Rechaza()
    {
        Assert.Equal(
            MensajesDeDatosDePaciente.IdentificadorNecesario,
            Mensaje(() => ReglasDePaciente.Registrar(Guid.Empty, Adulto(), [AmbitoAtencion.Escolar], Actor, Ahora)));
    }

    [Fact]
    public void Registrar_LlenaFechaYUsuarioDeAltaEnUtc()
    {
        var local = new DateTimeOffset(2026, 10, 5, 8, 0, 0, TimeSpan.FromHours(-7));

        var paciente = ReglasDePaciente.Registrar(Guid.NewGuid(), Adulto(), [AmbitoAtencion.Escolar], Actor, local);

        Assert.Equal(Actor, paciente.IdUsuarioAlta);
        Assert.Equal(TimeSpan.Zero, paciente.FechaAlta.Offset);
        Assert.Equal(local, paciente.FechaAlta);
        Assert.Null(paciente.FechaActualizacion);
        Assert.Null(paciente.FechaBaja);
    }

    [Fact]
    public void Registrar_GuardaLosDatosNormalizadosYLaClaveDeBusqueda()
    {
        var datos = new DatosDePaciente(
            "  María  José Muñoz  ", NacimientoAdulto, " +52 (644) 123-4567 ", "  Canario@Ficticio.TEST ", " Escuela Ficticia ", " 3A ", TutorValido);

        var paciente = ReglasDePaciente.Registrar(Guid.NewGuid(), datos, [AmbitoAtencion.Escolar], Actor, Ahora);

        Assert.Equal("María  José Muñoz", paciente.Nombre);
        Assert.Equal("maria jose munoz", paciente.NombreNormalizado);
        Assert.Equal(NacimientoAdulto, paciente.FechaNacimiento);
        Assert.Equal("+526441234567", paciente.Telefono);
        Assert.Equal("canario@ficticio.test", paciente.Correo);
        Assert.Equal("Escuela Ficticia", paciente.Escuela);
        Assert.Equal("3A", paciente.Grupo);
        Assert.Equal("Tutora Ficticia", paciente.TutorNombre);
        Assert.Equal("Madre", paciente.TutorParentesco);
        Assert.Equal("6441234567", paciente.TutorTelefono);
        Assert.Null(paciente.TutorCorreo);
    }

    [Fact]
    public void Registrar_ConDatosOpcionalesVaciosLosTrataComoAusentes()
    {
        var datos = new DatosDePaciente("  Paciente Ficticio  ", NacimientoAdulto, "   ", "   ", "", " ", new DatosDeTutor(" ", null, "", " "));

        var paciente = ReglasDePaciente.Registrar(Guid.NewGuid(), datos, [AmbitoAtencion.Escolar], Actor, Ahora);

        Assert.Equal("Paciente Ficticio", paciente.Nombre);
        Assert.Null(paciente.Telefono);
        Assert.Null(paciente.Correo);
        Assert.Null(paciente.Escuela);
        Assert.Null(paciente.Grupo);
        Assert.Null(paciente.TutorNombre);
        Assert.Null(paciente.TutorParentesco);
    }

    [Fact]
    public void Registrar_SinNacimiento_Rechaza()
    {
        Assert.Equal(
            MensajesDeDatosDePaciente.NacimientoObligatorio,
            Mensaje(() => ReglasDePaciente.Registrar(Guid.NewGuid(), new DatosDePaciente("Paciente Ficticio"), [AmbitoAtencion.Escolar], Actor, Ahora)));
    }

    [Theory]
    [InlineData(2026, 10, 6)]
    [InlineData(2030, 1, 1)]
    [InlineData(1899, 12, 31)]
    public void Registrar_ConNacimientoFuturoOAnteriorA1900_Rechaza(int anio, int mes, int dia)
    {
        var datos = new DatosDePaciente("Paciente Ficticio", new DateOnly(anio, mes, dia));

        Assert.Equal(
            MensajesDeDatosDePaciente.NacimientoInvalido,
            Mensaje(() => ReglasDePaciente.Registrar(Guid.NewGuid(), datos, [AmbitoAtencion.Escolar], Actor, Ahora)));
    }

    [Fact]
    public void Registrar_ConNacimientoHoyOEl1DeEnero1900_EsValido()
    {
        var recienNacido = ReglasDePaciente.Registrar(
            Guid.NewGuid(), new DatosDePaciente("Bebe Ficticio", Hoy, Tutor: TutorValido), [AmbitoAtencion.Escolar], Actor, Ahora);
        var anciano = ReglasDePaciente.Registrar(
            Guid.NewGuid(), new DatosDePaciente("Persona Ficticia", new DateOnly(1900, 1, 1)), [AmbitoAtencion.Escolar], Actor, Ahora);

        Assert.Equal(Hoy, recienNacido.FechaNacimiento);
        Assert.Equal(new DateOnly(1900, 1, 1), anciano.FechaNacimiento);
    }

    [Fact]
    public void Registrar_DeUnaPersonaMenorSinTutor_Rechaza()
    {
        Assert.Equal(
            MensajesDeDatosDePaciente.TutorObligatorioParaMenores,
            Mensaje(() => ReglasDePaciente.Registrar(
                Guid.NewGuid(), new DatosDePaciente("Menor Ficticio", NacimientoMenor), [AmbitoAtencion.Escolar], Actor, Ahora)));
    }

    [Fact]
    public void Registrar_DeUnaPersonaMenorConTutorCompleto_Guarda()
    {
        var paciente = NuevoMenor();

        Assert.Equal("Tutora Ficticia", paciente.TutorNombre);
        Assert.Equal(1, paciente.Version);
    }

    [Fact]
    public void Registrar_DeUnAdultoPermiteUnTutorOpcionalYNoLoExige()
    {
        var sinTutor = NuevoPaciente();
        var conTutor = ReglasDePaciente.Registrar(
            Guid.NewGuid(), new DatosDePaciente("Adulto Ficticio", NacimientoAdulto, Tutor: new DatosDeTutor("Apoyo Ficticio", "Hermano", null, "apoyo@ficticio.test")),
            [AmbitoAtencion.Escolar], Actor, Ahora);

        Assert.Null(sinTutor.TutorNombre);
        Assert.Equal("apoyo@ficticio.test", conTutor.TutorCorreo);
    }

    [Theory]
    [InlineData(null, "Madre", "6441234567", null)]
    [InlineData("Tutora Ficticia", null, "6441234567", null)]
    [InlineData("Tutora Ficticia", "Madre", null, null)]
    [InlineData("T", "Madre", "6441234567", null)]
    [InlineData("Tutora Ficticia", "M", null, "tutora@ficticio.test")]
    public void Registrar_ConTutorIncompleto_RechazaAunSiLaPersonaEsAdulta(string? nombre, string? parentesco, string? telefono, string? correo)
    {
        var datos = new DatosDePaciente("Adulto Ficticio", NacimientoAdulto, Tutor: new DatosDeTutor(nombre, parentesco, telefono, correo));

        Assert.Equal(
            MensajesDeDatosDePaciente.TutorIncompleto,
            Mensaje(() => ReglasDePaciente.Registrar(Guid.NewGuid(), datos, [AmbitoAtencion.Escolar], Actor, Ahora)));
    }

    [Theory]
    [InlineData("Tutora Ficticia", "Madre", "abc", null)]
    [InlineData("Tutora Ficticia", "Madre", null, "sin-arroba")]
    [InlineData("Tutora Ficticia", "Madre", "6441234567", "otro@@ficticio.test")]
    public void Registrar_ConDatosDeTutorInvalidos_Rechaza(string? nombre, string? parentesco, string? telefono, string? correo)
    {
        var datos = new DatosDePaciente("Adulto Ficticio", NacimientoAdulto, Tutor: new DatosDeTutor(nombre, parentesco, telefono, correo));

        Assert.Equal(
            MensajesDeDatosDePaciente.TutorInvalido,
            Mensaje(() => ReglasDePaciente.Registrar(Guid.NewGuid(), datos, [AmbitoAtencion.Escolar], Actor, Ahora)));
    }

    [Fact]
    public void Registrar_ElDiaQueCumpleDieciochoYaEsAdultoYAnteriormenteEsMenor()
    {
        // Hoy es 5-oct-2026: quien nació el 5-oct-2008 cumple 18 hoy; quien nació el 6-oct-2008 cumple mañana.
        var cumpleHoy = new DatosDePaciente("Persona Ficticia", new DateOnly(2008, 10, 5));
        var cumpleManana = new DatosDePaciente("Persona Ficticia", new DateOnly(2008, 10, 6));

        var adulto = ReglasDePaciente.Registrar(Guid.NewGuid(), cumpleHoy, [AmbitoAtencion.Escolar], Actor, Ahora);

        Assert.Null(adulto.TutorNombre);
        Assert.Equal(
            MensajesDeDatosDePaciente.TutorObligatorioParaMenores,
            Mensaje(() => ReglasDePaciente.Registrar(Guid.NewGuid(), cumpleManana, [AmbitoAtencion.Escolar], Actor, Ahora)));
    }

    [Fact]
    public void Registrar_CalculaLaEdadConLaFechaDeHermosilloYNoLaDeUtc()
    {
        // A las 03:00 UTC del 6-oct ya es 6-oct en UTC, pero en Hermosillo (UTC-7) todavía es el 5-oct a las 20:00.
        var tardeEnHermosillo = new DateTimeOffset(2026, 10, 6, 3, 0, 0, TimeSpan.Zero);
        var cumpleDeMananaEnHermosillo = new DatosDePaciente("Persona Ficticia", new DateOnly(2008, 10, 6));

        Assert.Equal(
            MensajesDeDatosDePaciente.TutorObligatorioParaMenores,
            Mensaje(() => ReglasDePaciente.Registrar(Guid.NewGuid(), cumpleDeMananaEnHermosillo, [AmbitoAtencion.Escolar], Actor, tardeEnHermosillo)));
    }

    [Fact]
    public void Registrar_ConDatosInvalidosDeContacto_RechazaConMensajesFijosSinElValor()
    {
        var telefono = Mensaje(() => ReglasDePaciente.Registrar(
            Guid.NewGuid(), new DatosDePaciente("Paciente Ficticio", NacimientoAdulto, Telefono: "valor-privado-123"), [AmbitoAtencion.Escolar], Actor, Ahora));
        var correo = Mensaje(() => ReglasDePaciente.Registrar(
            Guid.NewGuid(), new DatosDePaciente("Paciente Ficticio", NacimientoAdulto, Correo: "valor-privado@"), [AmbitoAtencion.Escolar], Actor, Ahora));

        Assert.Equal(MensajesDeDatosDePaciente.TelefonoInvalido, telefono);
        Assert.Equal(MensajesDeDatosDePaciente.CorreoInvalido, correo);
        Assert.DoesNotContain("privado", telefono + correo);
    }

    [Fact]
    public void CamposDeAlta_IncluyeLosObligatoriosYSoloLosOpcionalesEscritos()
    {
        var minimo = NuevoPaciente();
        var completo = ReglasDePaciente.Registrar(
            Guid.NewGuid(), new DatosDePaciente("Menor Ficticio", NacimientoMenor, "6441234567", "m@ficticio.test", "Escuela", "3A", TutorValido),
            [AmbitoAtencion.Escolar], Actor, Ahora);

        Assert.Equal(
            [CamposDePaciente.Nombre, CamposDePaciente.FechaNacimiento, CamposDePaciente.Correo, CamposDePaciente.Ambitos],
            ReglasDePaciente.CamposDeAlta(minimo));
        Assert.Equal(
            [
                CamposDePaciente.Nombre, CamposDePaciente.FechaNacimiento, CamposDePaciente.Telefono, CamposDePaciente.Correo, CamposDePaciente.Escuela,
                CamposDePaciente.Grupo, CamposDePaciente.TutorNombre, CamposDePaciente.TutorParentesco, CamposDePaciente.TutorTelefono, CamposDePaciente.Ambitos
            ],
            ReglasDePaciente.CamposDeAlta(completo));
    }

    [Fact]
    public void Actualizar_ConVersionVigente_CambiaDatosIncrementaVersionYLlenaMetadatos()
    {
        var paciente = NuevoPaciente();
        var despues = Ahora.AddHours(1);

        var cambios = ReglasDePaciente.Actualizar(paciente, Adulto("Nombre Nuevo"), [AmbitoAtencion.Escolar], 1, OtroActor, despues);

        Assert.Equal("Nombre Nuevo", paciente.Nombre);
        Assert.Equal("nombre nuevo", paciente.NombreNormalizado);
        Assert.Equal(2, paciente.Version);
        Assert.Equal(OtroActor, paciente.IdUsuarioActualizacion);
        Assert.Equal(despues, paciente.FechaActualizacion);
        Assert.True(cambios.Hubo);
        Assert.Equal([CamposDePaciente.Nombre], cambios.Campos);
    }

    [Fact]
    public void Actualizar_InformaSoloLosNombresDeLosCamposQueCambiaron()
    {
        var paciente = NuevoPaciente();
        var datos = new DatosDePaciente("Paciente Ficticio", NacimientoAdulto, "6441234567", "canario@ficticio.test", "Escuela Nueva", Tutor: null);

        var cambios = ReglasDePaciente.Actualizar(paciente, datos, [AmbitoAtencion.Escolar], 1, OtroActor, Ahora);

        Assert.Equal([CamposDePaciente.Telefono, CamposDePaciente.Escuela], cambios.Campos);
        Assert.Equal("6441234567", paciente.Telefono);
        Assert.Equal("Escuela Nueva", paciente.Escuela);
    }

    [Fact]
    public void Actualizar_SinCambiosConLaVersionVigente_NoEscribeNiSubeVersionNiTocaMetadatos()
    {
        var paciente = NuevoPaciente();

        // Mismos datos escritos con otro formato: espacios, mayúsculas del correo.
        var cambios = ReglasDePaciente.Actualizar(
            paciente, Adulto("  Paciente Ficticio  ", " CANARIO@ficticio.test "), [AmbitoAtencion.Escolar], 1, OtroActor, Ahora.AddHours(1));

        Assert.False(cambios.Hubo);
        Assert.Same(CambiosDePaciente.Ninguno, cambios);
        Assert.Equal(1, paciente.Version);
        Assert.Null(paciente.FechaActualizacion);
        Assert.Null(paciente.IdUsuarioActualizacion);
    }

    [Fact]
    public void Actualizar_ConVersionAnteriorYDatosIguales_LanzaConflictoAunSinCambios()
    {
        var paciente = NuevoPaciente();
        ReglasDePaciente.Actualizar(paciente, Adulto("Primera Edicion"), [AmbitoAtencion.Escolar], 1, Actor, Ahora);

        // Versión 1 ya no es la vigente: el conflicto se da aunque los datos enviados coincidan con los guardados.
        Assert.Throws<ConflictoDeVersionException>(() =>
            ReglasDePaciente.Actualizar(paciente, Adulto("Primera Edicion"), [AmbitoAtencion.Escolar], 1, OtroActor, Ahora));
        Assert.Equal(2, paciente.Version);
    }

    [Fact]
    public void Actualizar_ConVersionAnterior_LanzaConflictoYNoCambiaNada()
    {
        var paciente = NuevoPaciente();
        ReglasDePaciente.Actualizar(paciente, Adulto("Primera Edicion"), [AmbitoAtencion.Escolar], 1, Actor, Ahora);

        Assert.Throws<ConflictoDeVersionException>(() =>
            ReglasDePaciente.Actualizar(paciente, Adulto("Segunda Edicion"), [AmbitoAtencion.Escolar], 1, OtroActor, Ahora));

        Assert.Equal("Primera Edicion", paciente.Nombre);
        Assert.Equal(2, paciente.Version);
    }

    [Fact]
    public void Actualizar_UnaFichaDeBaja_RechazaSinCambioSilencioso()
    {
        var paciente = NuevoPaciente();
        ReglasDePaciente.DarDeBaja(paciente, 1, Actor, Ahora);

        var error = Assert.Throws<ReglaDeNegocioException>(() =>
            ReglasDePaciente.Actualizar(paciente, Adulto("Otro Nombre"), [AmbitoAtencion.Escolar], 2, Actor, Ahora));

        Assert.Equal(MensajesDeDatosDePaciente.FichaDeBajaNoSeActualiza, error.Message);
        Assert.Equal("Paciente Ficticio", paciente.Nombre);
    }

    [Fact]
    public void Actualizar_ConDatosInvalidos_NoModificaNada()
    {
        var paciente = NuevoPaciente();

        Assert.Throws<ReglaDeNegocioException>(() =>
            ReglasDePaciente.Actualizar(paciente, Adulto("   ", "otro@ficticio.test"), [AmbitoAtencion.Escolar], 1, Actor, Ahora));

        Assert.Equal("Paciente Ficticio", paciente.Nombre);
        Assert.Equal("canario@ficticio.test", paciente.Correo);
        Assert.Equal(1, paciente.Version);
    }

    [Fact]
    public void Actualizar_DeAdultoAMenorSinTutor_RechazaYNoModificaNada()
    {
        var paciente = NuevoPaciente();

        var mensaje = Mensaje(() => ReglasDePaciente.Actualizar(
            paciente, new DatosDePaciente("Paciente Ficticio", NacimientoMenor), [AmbitoAtencion.Escolar], 1, Actor, Ahora));

        Assert.Equal(MensajesDeDatosDePaciente.TutorObligatorioParaMenores, mensaje);
        Assert.Equal(NacimientoAdulto, paciente.FechaNacimiento);
        Assert.Equal(1, paciente.Version);
    }

    [Fact]
    public void Actualizar_DeAdultoAMenorConTutorEnLaMismaEdicion_GuardaAmbosConUnaVersion()
    {
        var paciente = NuevoPaciente();

        var cambios = ReglasDePaciente.Actualizar(
            paciente, new DatosDePaciente("Paciente Ficticio", NacimientoMenor, Tutor: TutorValido), [AmbitoAtencion.Escolar], 1, Actor, Ahora);

        Assert.Equal(NacimientoMenor, paciente.FechaNacimiento);
        Assert.Equal("Tutora Ficticia", paciente.TutorNombre);
        Assert.Equal(2, paciente.Version);
        Assert.Contains(CamposDePaciente.FechaNacimiento, cambios.Campos);
        Assert.Contains(CamposDePaciente.TutorNombre, cambios.Campos);
    }

    [Fact]
    public void Actualizar_DeMenorAAdulto_ConservaElTutorSiSeEnvia()
    {
        var paciente = NuevoMenor();

        ReglasDePaciente.Actualizar(
            paciente, new DatosDePaciente("Menor Ficticio", NacimientoAdulto, Tutor: TutorValido), [AmbitoAtencion.Escolar], 1, Actor, Ahora);

        Assert.Equal(NacimientoAdulto, paciente.FechaNacimiento);
        Assert.Equal("Tutora Ficticia", paciente.TutorNombre);
    }

    [Fact]
    public void Actualizar_DeUnMenorQuitandoSuTutor_Rechaza()
    {
        var paciente = NuevoMenor();

        Assert.Equal(
            MensajesDeDatosDePaciente.TutorObligatorioParaMenores,
            Mensaje(() => ReglasDePaciente.Actualizar(
                paciente, new DatosDePaciente("Menor Ficticio", NacimientoMenor), [AmbitoAtencion.Escolar], 1, Actor, Ahora)));
        Assert.Equal("Tutora Ficticia", paciente.TutorNombre);
    }

    [Fact]
    public void Actualizar_SoloElTutorConDatosIncompletos_RechazaYConservaElAnterior()
    {
        var paciente = NuevoMenor();
        var datos = new DatosDePaciente("Menor Ficticio", NacimientoMenor, Tutor: new DatosDeTutor("Otra Tutora", null, "6441234567", null));

        Assert.Equal(
            MensajesDeDatosDePaciente.TutorIncompleto,
            Mensaje(() => ReglasDePaciente.Actualizar(paciente, datos, [AmbitoAtencion.Escolar], 1, Actor, Ahora)));
        Assert.Equal("Tutora Ficticia", paciente.TutorNombre);
        Assert.Equal(1, paciente.Version);
    }

    [Fact]
    public void Actualizar_UnaFichaAnteriorSinNacimientoYSinCambios_NoExigeInventarDatos()
    {
        var paciente = FichaAnterior();

        var cambios = ReglasDePaciente.Actualizar(
            paciente, new DatosDePaciente("Paciente Ficticio", null, Correo: "canario@ficticio.test"), [AmbitoAtencion.Escolar], 1, Actor, Ahora);

        Assert.False(cambios.Hubo);
        Assert.Null(paciente.FechaNacimiento);
        Assert.Equal(1, paciente.Version);
    }

    [Fact]
    public void Actualizar_UnaFichaAnteriorConUnCambioRealSinNacimiento_ExigeCompletarloYNoGuarda()
    {
        var paciente = FichaAnterior();

        Assert.Equal(
            MensajesDeDatosDePaciente.NacimientoObligatorio,
            Mensaje(() => ReglasDePaciente.Actualizar(
                paciente, new DatosDePaciente("Nombre Nuevo", null, Correo: "canario@ficticio.test"), [AmbitoAtencion.Escolar], 1, Actor, Ahora)));
        Assert.Equal("Paciente Ficticio", paciente.Nombre);
        Assert.Equal(1, paciente.Version);
    }

    [Fact]
    public void Actualizar_UnaFichaAnteriorAlCompletarElNacimiento_GuardaYRegistraElCampo()
    {
        var paciente = FichaAnterior();

        var cambios = ReglasDePaciente.Actualizar(paciente, Adulto(), [AmbitoAtencion.Escolar], 1, Actor, Ahora);

        Assert.Equal([CamposDePaciente.FechaNacimiento], cambios.Campos);
        Assert.Equal(NacimientoAdulto, paciente.FechaNacimiento);
    }

    [Fact]
    public void Actualizar_AgregaYQuitaAmbitosYDevuelveLosQuitados()
    {
        var paciente = NuevoPaciente(AmbitoAtencion.Escolar);
        var escolar = paciente.Ambitos.Single();

        var cambios = ReglasDePaciente.Actualizar(paciente, Adulto(), [AmbitoAtencion.Independiente], 1, Actor, Ahora);

        Assert.Equal([CamposDePaciente.Ambitos], cambios.Campos);
        Assert.Equal(AmbitoAtencion.Independiente, Assert.Single(paciente.Ambitos).Ambito);
        Assert.Same(escolar, Assert.Single(cambios.AmbitosQuitados));
        Assert.Equal(2, paciente.Version);
    }

    [Fact]
    public void Actualizar_AgregarUnAmbitoConservaElExistenteSinQuitarNada()
    {
        var paciente = NuevoPaciente(AmbitoAtencion.Escolar);

        var cambios = ReglasDePaciente.Actualizar(
            paciente, Adulto(), [AmbitoAtencion.Independiente, AmbitoAtencion.Escolar], 1, Actor, Ahora);

        Assert.Empty(cambios.AmbitosQuitados);
        Assert.Equal(
            [AmbitoAtencion.Escolar, AmbitoAtencion.Independiente],
            paciente.Ambitos.Select(a => a.Ambito).Order());
        Assert.All(paciente.Ambitos, a => Assert.Equal(paciente.IdPaciente, a.IdPaciente));
    }

    [Fact]
    public void Actualizar_ConLosMismosAmbitosEnOtroOrden_NoEsUnCambio()
    {
        var paciente = NuevoPaciente(AmbitoAtencion.Escolar, AmbitoAtencion.Independiente);

        var cambios = ReglasDePaciente.Actualizar(
            paciente, Adulto(), [AmbitoAtencion.Independiente, AmbitoAtencion.Escolar], 1, Actor, Ahora);

        Assert.False(cambios.Hubo);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    public void Actualizar_ConCantidadInvalidaDeAmbitos_RechazaYConservaLosActuales(int cantidad)
    {
        var paciente = NuevoPaciente(AmbitoAtencion.Escolar);
        AmbitoAtencion[] ambitos = cantidad == 0
            ? []
            : [AmbitoAtencion.Escolar, AmbitoAtencion.Independiente, AmbitoAtencion.Escolar];

        Assert.Equal(
            MensajesDeDatosDePaciente.AmbitosInvalidos,
            Mensaje(() => ReglasDePaciente.Actualizar(paciente, Adulto(), ambitos, 1, Actor, Ahora)));
        Assert.Equal(AmbitoAtencion.Escolar, Assert.Single(paciente.Ambitos).Ambito);
        Assert.Equal(1, paciente.Version);
    }

    [Fact]
    public void Actualizar_ConAmbitoRepetidoOInexistente_Rechaza()
    {
        var paciente = NuevoPaciente();

        Assert.Throws<ReglaDeNegocioException>(() =>
            ReglasDePaciente.Actualizar(paciente, Adulto(), [AmbitoAtencion.Escolar, AmbitoAtencion.Escolar], 1, Actor, Ahora));
        Assert.Throws<ReglaDeNegocioException>(() =>
            ReglasDePaciente.Actualizar(paciente, Adulto(), [(AmbitoAtencion)99], 1, Actor, Ahora));
    }

    [Fact]
    public void Actualizar_SinActor_RechazaAntesDeCambiarDatos()
    {
        var paciente = NuevoPaciente();

        Assert.Throws<ReglaDeNegocioException>(() =>
            ReglasDePaciente.Actualizar(paciente, Adulto("Nombre Nuevo"), [AmbitoAtencion.Escolar], 1, Guid.Empty, Ahora));
        Assert.Equal("Paciente Ficticio", paciente.Nombre);
    }

    [Fact]
    public void TienenLosMismosDatos_ComparaContenidoYAmbitosPeroNoIdentificadores()
    {
        var uno = ReglasDePaciente.Registrar(Guid.NewGuid(), Adulto(), [AmbitoAtencion.Escolar], Actor, Ahora);
        var igual = ReglasDePaciente.Registrar(Guid.NewGuid(), Adulto(), [AmbitoAtencion.Escolar], OtroActor, Ahora.AddDays(1));
        var otroAmbito = ReglasDePaciente.Registrar(Guid.NewGuid(), Adulto(), [AmbitoAtencion.Independiente], Actor, Ahora);
        var otroNombre = ReglasDePaciente.Registrar(Guid.NewGuid(), Adulto("Otra Persona"), [AmbitoAtencion.Escolar], Actor, Ahora);

        Assert.True(ReglasDePaciente.TienenLosMismosDatos(uno, igual));
        Assert.False(ReglasDePaciente.TienenLosMismosDatos(uno, otroAmbito));
        Assert.False(ReglasDePaciente.TienenLosMismosDatos(uno, otroNombre));
    }

    [Fact]
    public void DarDeBaja_MarcaBajaConFechaYUsuarioSinBorrarDatos()
    {
        var paciente = NuevoPaciente(AmbitoAtencion.Escolar, AmbitoAtencion.Independiente);

        ReglasDePaciente.DarDeBaja(paciente, 1, OtroActor, Ahora);

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
        ReglasDePaciente.DarDeBaja(paciente, 1, Actor, Ahora);

        Assert.Throws<ReglaDeNegocioException>(() => ReglasDePaciente.DarDeBaja(paciente, 2, Actor, Ahora));
        Assert.Equal(2, paciente.Version);
    }

    [Fact]
    public void Recuperar_DevuelveAVigenteYLimpiaDatosDeBaja()
    {
        var paciente = NuevoPaciente();
        ReglasDePaciente.DarDeBaja(paciente, 1, Actor, Ahora);

        ReglasDePaciente.Recuperar(paciente, 2, OtroActor, Ahora.AddDays(1));

        Assert.Equal(CondicionRegistro.Vigente, paciente.Condicion);
        Assert.Null(paciente.FechaBaja);
        Assert.Null(paciente.IdUsuarioBaja);
        Assert.Equal(3, paciente.Version);
    }

    [Fact]
    public void Recuperar_UnaFichaYaVigente_Informa()
    {
        var paciente = NuevoPaciente();

        Assert.Throws<ReglaDeNegocioException>(() => ReglasDePaciente.Recuperar(paciente, 1, Actor, Ahora));
    }

    [Fact]
    public void DarDeBajaYRecuperar_ConVersionAnterior_LanzanConflicto()
    {
        var paciente = NuevoPaciente();

        Assert.Throws<ConflictoDeVersionException>(() => ReglasDePaciente.DarDeBaja(paciente, 7, Actor, Ahora));
        ReglasDePaciente.DarDeBaja(paciente, 1, Actor, Ahora);
        Assert.Throws<ConflictoDeVersionException>(() => ReglasDePaciente.Recuperar(paciente, 1, Actor, Ahora));
    }
}
