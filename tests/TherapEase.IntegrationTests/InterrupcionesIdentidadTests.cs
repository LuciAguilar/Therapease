using Npgsql;

namespace TherapEase.IntegrationTests;

/// <summary>
/// Comprueba guardados interrumpidos y reintentos de Identity con PostgreSQL desechable.
/// Conserva separados los fallos anteriores al guardado y la respuesta perdida después.
/// </summary>
public class InterrupcionesIdentidadTests(PostgreSqlRevision postgres) : IClassFixture<PostgreSqlRevision>
{


    // ──── INTERRUPCIÓN ANTES DE CONFIRMAR ─────────────────────────────────────────────────


    /// Si se interrumpe antes de confirmar, usuario, roles y auditoría quedan iguales; un nuevo intento puede guardarse.
    [Theory]
    [InlineData("crear")]
    [InlineData("desactivar")]
    [InlineData("cambiar-rol")]
    [InlineData("restablecer")]
    [InlineData("cambiar-propia-temporal")]
    [InlineData("cambiar-propia")]
    public async Task Antes_De_Confirmar_No_Guarda_Y_Permite_Reintento(string operacion)
    {
        // Arrange
        using var entorno = await InterrupcionesIdentidadRevision.CrearAsync(postgres, operacion);
        var anterior = await entorno.EstadoAsync();

        // Act + Assert
        await Assert.ThrowsAsync<IOException>(() => entorno.EjecutarAsync(operacion, "antes"));

        // Assert
        var interrumpido = await entorno.EstadoAsync();
        Assert.Equal(1, entorno.Corte.AntesDeConfirmar);
        Assert.Equal(0, entorno.Corte.Confirmadas);
        if (operacion == "cambiar-propia") Assert.Equal(1, entorno.Corte.ConfirmacionesPrevias);
        Assert.Equal(anterior, interrumpido);
        if (operacion != "crear") Assert.True(await entorno.SesionObjetivoValidaAsync());

        // Act
        var repetido = await entorno.EjecutarAsync(operacion);

        // Assert
        Assert.True(repetido.Exito);
        var final = await entorno.EstadoAsync();
        ComprobarCambio(operacion, final);
        if (operacion is "crear" or "restablecer") Assert.True(repetido.TemporalVerificada);
        await entorno.GuardarAsync("antes-" + operacion, new { anterior, interrumpido, final, ExitoEntregadoAntesDelCommit = false });
    }


    // ──── RESPUESTA PERDIDA DESPUÉS DE CONFIRMAR ───────────────────────────────────────────


    /// Una respuesta perdida deja cambio y evento guardados; repetir alta, rol o baja no duplica el efecto.
    [Theory]
    [InlineData("crear")]
    [InlineData("desactivar")]
    [InlineData("cambiar-rol")]
    [InlineData("restablecer")]
    [InlineData("cambiar-propia-temporal")]
    [InlineData("cambiar-propia")]
    public async Task Despues_De_Confirmar_Consulta_El_Cambio_Aunque_No_Haya_Respuesta(string operacion)
    {
        // Arrange
        using var entorno = await InterrupcionesIdentidadRevision.CrearAsync(postgres, operacion);
        var anterior = await entorno.EstadoAsync();

        // Act + Assert
        await Assert.ThrowsAsync<IOException>(() => entorno.EjecutarAsync(operacion, "despues"));

        // Assert
        var guardado = await entorno.EstadoAsync();
        Assert.Equal(1, entorno.Corte.Confirmadas);
        if (operacion == "cambiar-propia") Assert.Equal(1, entorno.Corte.ConfirmacionesPrevias);
        Assert.NotEqual(anterior.Huella, guardado.Huella);
        ComprobarCambio(operacion, guardado);
        if (operacion != "crear") Assert.False(await entorno.SesionObjetivoValidaAsync());

        // Un restablecimiento genera otra temporal al repetirlo; se comprueba aparte como nueva entrega explícita.

        if (operacion is not ("restablecer" or "cambiar-propia-temporal" or "cambiar-propia"))
        {
            // Act
            var repetido = await entorno.EjecutarAsync(operacion);

            // Assert
            Assert.False(repetido.Exito);
            Assert.Equal(guardado, await entorno.EstadoAsync());
        }
        if (operacion is "cambiar-propia-temporal" or "cambiar-propia") Assert.True(await entorno.NuevaClavePropiaValidaAsync());
        await entorno.GuardarAsync("despues-" + operacion, new { anterior, guardado, RespuestaEntregada = false,
            ReintentoSinDuplicarComprobado = operacion is not ("restablecer" or "cambiar-propia-temporal" or "cambiar-propia"),
            NuevaClavePropiaComprobada = operacion is "cambiar-propia-temporal" or "cambiar-propia" });
    }

    /// Si se perdió la temporal después de guardar, una nueva solicitud de restablecimiento crea otra y otro evento.
    [Fact]
    public async Task Restablecimiento_Perdido_Nueva_Entrega_Genera_Otra_Clave_Y_Evento()
    {
        // Arrange
        using var entorno = await InterrupcionesIdentidadRevision.CrearAsync(postgres, "restablecer");

        // Act + Assert
        await Assert.ThrowsAsync<IOException>(() => entorno.EjecutarAsync("restablecer", "despues"));

        // Assert
        var perdido = await entorno.EstadoAsync();
        ComprobarCambio("restablecer", perdido);
        Assert.False(await entorno.SesionObjetivoValidaAsync());

        // Act
        var nuevaEntrega = await entorno.EjecutarAsync("restablecer");

        // Assert
        var final = await entorno.EstadoAsync();
        Assert.True(nuevaEntrega.Exito && nuevaEntrega.TemporalVerificada);
        Assert.Equal(2, final.Eventos);
        Assert.Equal(1, final.UsuariosObjetivo);
        Assert.NotEqual(perdido.HuellaUsuario, final.HuellaUsuario);
        Assert.True(final.Temporal);
        await entorno.GuardarAsync("restablecer-nueva-entrega", new { perdido, final, NuevaTemporalVerificada = true,
            ReintentoAutomaticoSeguro = false, ProcedimientoDeEntregaPendiente = true });
    }


    // ──── ESPERA, CANCELACIÓN Y CONEXIÓN REAL ─────────────────────────────────────────────


    /// Mientras PostgreSQL no confirma, el servicio no termina y otra conexión todavía ve los datos anteriores.
    [Fact]
    public async Task Exito_Solo_Despues_De_Confirmar_Y_Visible_Desde_Otra_Conexion()
    {
        // Arrange
        using var entorno = await InterrupcionesIdentidadRevision.CrearAsync(postgres, "crear");
        var anterior = await entorno.EstadoAsync();

        // Act
        var operacion = entorno.EjecutarAsync("crear", "esperar");
        try
        {
            await entorno.Corte.Alcanzado.Task.WaitAsync(TimeSpan.FromSeconds(15));

            // Assert
            Assert.False(operacion.IsCompleted);
            Assert.Equal(anterior, await entorno.EstadoAsync());
        }
        finally
        {
            entorno.Corte.Continuar.TrySetResult();
            await operacion;
        }

        // Act
        var respuesta = await operacion;

        // Assert
        Assert.True(respuesta.Exito && respuesta.TemporalVerificada);
        Assert.Equal(1, entorno.Corte.Confirmadas);
        var final = await entorno.EstadoAsync();
        ComprobarCambio("crear", final);
        await entorno.GuardarAsync("exito-tras-confirmacion", new { anterior, final, ExitoAntesDeConfirmar = false });
    }

    /// Cancelar después de escribir y antes de confirmar revierte todo; no devuelve éxito y permite intentar de nuevo.
    [Fact]
    public async Task Cancelacion_Antes_De_Confirmar_Revierte_Y_Permite_Reintento()
    {
        // Arrange
        using var entorno = await InterrupcionesIdentidadRevision.CrearAsync(postgres, "crear");
        using var cancelacion = new CancellationTokenSource();
        var anterior = await entorno.EstadoAsync();

        // Act
        var operacion = entorno.EjecutarAsync("crear", "esperar", cancelacion.Token);
        try
        {
            await entorno.Corte.Alcanzado.Task.WaitAsync(TimeSpan.FromSeconds(15));
            cancelacion.Cancel();

            // Assert
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operacion);
        }
        finally
        {
            cancelacion.Cancel();
            entorno.Corte.Continuar.TrySetResult();
            try { await operacion; }
            catch (OperationCanceledException) { }
        }

        // Assert
        Assert.Equal(anterior, await entorno.EstadoAsync());
        Assert.Equal(0, entorno.Corte.Confirmadas);

        // Act
        var repetido = await entorno.EjecutarAsync("crear");

        // Assert
        Assert.True(repetido.Exito);
        var final = await entorno.EstadoAsync();
        ComprobarCambio("crear", final);
        await entorno.GuardarAsync("cancelacion", new { anterior, final, ExitoEnSolicitudCancelada = false });
    }

    /// Cortar de verdad la conexión PostgreSQL antes de confirmar revierte usuario y evento; otro intento funciona.
    [Fact]
    public async Task Conexion_PostgreSql_Terminada_Antes_De_Confirmar_No_Deja_Cambios_Parciales()
    {
        // Arrange
        using var entorno = await InterrupcionesIdentidadRevision.CrearAsync(postgres, "crear");
        var anterior = await entorno.EstadoAsync();

        // Act
        var error = await Record.ExceptionAsync(() => entorno.EjecutarAsync("crear", "conexion"));

        // Assert
        Assert.NotNull(error);
        Assert.IsAssignableFrom<NpgsqlException>(error);
        Assert.True(entorno.Corte.ConexionTerminada);
        Assert.Equal(anterior, await entorno.EstadoAsync());

        // Act
        var repetido = await entorno.EjecutarAsync("crear");

        // Assert
        Assert.True(repetido.Exito && repetido.TemporalVerificada);
        var final = await entorno.EstadoAsync();
        ComprobarCambio("crear", final);
        await entorno.GuardarAsync("conexion-cortada", new { anterior, final, entorno.Corte.ConexionTerminada,
            Error = error.GetType().Name });
    }

    /// Cada cambio confirmado deja una sola cuenta objetivo, su estado esperado y un evento.
    private static void ComprobarCambio(string operacion, EstadoGuardadoRevision estado)
    {
        Assert.Equal(1, estado.UsuariosObjetivo);
        Assert.Equal(1, estado.Eventos);
        Assert.Equal(operacion != "desactivar", estado.Activo);
        Assert.Equal(operacion is "crear" or "restablecer", estado.Temporal);
        Assert.Equal(operacion == "cambiar-rol" ? "Superusuario" : "Usuaria", estado.Rol);
    }
}
