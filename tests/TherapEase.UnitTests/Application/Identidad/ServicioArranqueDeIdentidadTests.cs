using TherapEase.Application.Auditoria.Servicios;
using TherapEase.Application.Identidad.Servicios;
using TherapEase.Domain.Auditoria.Entidades.Enums;
using TherapEase.Domain.Identidad.Constantes;
using TherapEase.UnitTests.Dobles;

namespace TherapEase.UnitTests.Application.Identidad;

public class ServicioArranqueDeIdentidadTests
{
    private readonly GestorIdentidadFalso _gestor = new();
    private readonly UnidadDeTrabajoFalsa _unidad = new();
    private readonly RepositorioAuditoriaFalso _repositorioAuditoria = new();
    private readonly ServicioArranqueDeIdentidad _servicio;

    public ServicioArranqueDeIdentidadTests()
    {
        var auditoria = new ServicioAuditoria(_repositorioAuditoria, new AutorizacionFalsa(), new RelojFijo(new DateTimeOffset(2026, 10, 5, 15, 0, 0, TimeSpan.Zero)));
        _servicio = new ServicioArranqueDeIdentidad(_gestor, _unidad, auditoria);
    }

    [Fact]
    public async Task CrearPrimerSuperusuario_SinNingunSuperusuarioActivo_LoCreaConTemporalQueDebeCambiar()
    {
        var respuesta = await _servicio.CrearPrimerSuperusuarioAsync("super1");

        Assert.True(respuesta.Exito);
        var creacion = Assert.Single(_gestor.Creaciones);
        Assert.Equal(NombresDeRol.Superusuario, creacion.Rol);
        Assert.True(creacion.DebeCambiar);
        Assert.Equal(GeneradorDeContrasenaTemporal.Longitud, respuesta.Datos!.ContrasenaTemporal.Length);
    }

    [Fact]
    public async Task CrearPrimerSuperusuario_RegistraUnEventoSinContrasenaConElPropioUsuarioComoActor()
    {
        var respuesta = await _servicio.CrearPrimerSuperusuarioAsync("super_canario");

        var evento = Assert.Single(_repositorioAuditoria.Eventos);
        Assert.Equal(respuesta.Datos!.Usuario.IdUsuario, evento.IdUsuarioActor);
        Assert.Equal(AccionAuditoria.Alta, evento.Accion);
        var texto = $"{evento.Hecho}|{string.Join(',', evento.CamposAfectados)}";
        Assert.DoesNotContain(respuesta.Datos.ContrasenaTemporal, texto);
        Assert.DoesNotContain("super_canario", texto);
    }

    [Fact]
    public async Task CrearPrimerSuperusuario_SiYaExisteUnoActivo_Rechaza()
    {
        _gestor.Agregar("super1", NombresDeRol.Superusuario);

        var respuesta = await _servicio.CrearPrimerSuperusuarioAsync("super2");

        Assert.False(respuesta.Exito);
        Assert.Empty(_gestor.Creaciones);
    }

    [Fact]
    public async Task CrearPrimerSuperusuario_ConSoloUnoDesactivado_PermiteElAlta()
    {
        _gestor.Agregar("viejo", NombresDeRol.Superusuario, activo: false);

        var respuesta = await _servicio.CrearPrimerSuperusuarioAsync("super1");

        Assert.True(respuesta.Exito);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CrearPrimerSuperusuario_SinNombre_Rechaza(string nombre)
    {
        Assert.False((await _servicio.CrearPrimerSuperusuarioAsync(nombre)).Exito);
        Assert.Empty(_gestor.Creaciones);
    }

    [Fact]
    public async Task RestablecerSuperusuario_DeUnSuperusuarioActivo_EntregaTemporalYAudita()
    {
        var super1 = _gestor.Agregar("super1", NombresDeRol.Superusuario);

        var respuesta = await _servicio.RestablecerSuperusuarioAsync("super1");

        Assert.True(respuesta.Exito);
        var establecida = Assert.Single(_gestor.ContrasenasEstablecidas);
        Assert.Equal(super1.IdUsuario, establecida.Id);
        Assert.True(establecida.DebeCambiar);
        Assert.Equal(AccionAuditoria.ReinicioContrasena, Assert.Single(_repositorioAuditoria.Eventos).Accion);
    }

    [Fact]
    public async Task RestablecerSuperusuario_DeAlguienQueNoEsSuperusuarioActivo_Rechaza()
    {
        _gestor.Agregar("usuaria1", NombresDeRol.Usuaria);
        _gestor.Agregar("super_inactivo", NombresDeRol.Superusuario, activo: false);

        Assert.False((await _servicio.RestablecerSuperusuarioAsync("usuaria1")).Exito);
        Assert.False((await _servicio.RestablecerSuperusuarioAsync("super_inactivo")).Exito);
        Assert.False((await _servicio.RestablecerSuperusuarioAsync("no-existe")).Exito);
        Assert.Empty(_gestor.ContrasenasEstablecidas);
    }

    [Fact]
    public async Task RestablecerSuperusuario_NoLevantaUnBloqueoVigente()
    {
        var super1 = _gestor.Agregar("super1", NombresDeRol.Superusuario);
        _gestor.Bloqueados.Add(super1.IdUsuario);

        await _servicio.RestablecerSuperusuarioAsync("super1");

        Assert.Contains(super1.IdUsuario, _gestor.Bloqueados);
    }
}
