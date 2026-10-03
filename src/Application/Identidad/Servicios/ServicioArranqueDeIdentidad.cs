using TherapEase.Application.Auditoria.Interfaces.Servicios;
using TherapEase.Application.Compartido.Interfaces.Repositorios;
using TherapEase.Application.Compartido.Modelos;
using TherapEase.Application.Identidad.Constantes;
using TherapEase.Application.Identidad.Interfaces.Repositorios;
using TherapEase.Application.Identidad.Interfaces.Servicios;
using TherapEase.Application.Identidad.Modelos;
using TherapEase.Domain.Auditoria.Entidades.Enums;
using TherapEase.Domain.Compartido.Excepciones;
using TherapEase.Domain.Identidad.Constantes;

namespace TherapEase.Application.Identidad.Servicios;

public class ServicioArranqueDeIdentidad : IServicioArranqueDeIdentidad
{
    private readonly IGestorIdentidad _gestor;
    private readonly IUnidadDeTrabajo _unidad;
    private readonly IServicioAuditoria _auditoria;

    public ServicioArranqueDeIdentidad(IGestorIdentidad gestor, IUnidadDeTrabajo unidad, IServicioAuditoria auditoria)
    {
        _gestor = gestor;
        _unidad = unidad;
        _auditoria = auditoria;
    }

    public async Task<RespuestaServicio<ContrasenaTemporalEntregada>> CrearPrimerSuperusuarioAsync(
        string nombreUsuario, CancellationToken cancelacion = default)
    {
        var nombre = nombreUsuario?.Trim();
        if (string.IsNullOrEmpty(nombre) || nombre.Length > LimitesDeIdentidad.LongitudMaximaNombreUsuario)
        {
            return RespuestaServicio<ContrasenaTemporalEntregada>.Fallida(MensajesDeIdentidad.DatosDeUsuarioInvalidos);
        }

        if (await _gestor.ContarSuperusuariosActivosAsync(cancelacion) > 0)
        {
            return RespuestaServicio<ContrasenaTemporalEntregada>.Fallida(
                "Ya existe un superusuario activo. Si ninguno puede entrar, usa el restablecimiento de un superusuario existente.");
        }

        var contrasena = GeneradorDeContrasenaTemporal.Generar();
        var idNuevo = Guid.CreateVersion7();
        ResultadoCreacionDeUsuario? resultado = null;

        try
        {
            await _unidad.EjecutarAsync(async ct =>
            {
                resultado = await _gestor.CrearAsync(idNuevo, nombre, NombresDeRol.Superusuario, contrasena, debeCambiarContrasena: true, ct);
                if (resultado == ResultadoCreacionDeUsuario.Creado)
                {
                    // Sin sesión previa, el propio usuario creado figura como actor del alta inicial.
                    await _auditoria.RegistrarHechoDeCambioAsync(
                        idNuevo, TipoRegistroAuditoria.Usuario, idNuevo, AccionAuditoria.Alta,
                        ["NombreUsuario", "Rol", "DebeCambiarContrasena"],
                        "Alta inicial del primer superusuario por procedimiento de arranque del servidor.", ct);
                }
            }, cancelacion);
        }
        catch (ReglaDeNegocioException excepcion)
        {
            return RespuestaServicio<ContrasenaTemporalEntregada>.Fallida(excepcion.Message);
        }

        return resultado switch
        {
            ResultadoCreacionDeUsuario.Creado => RespuestaServicio<ContrasenaTemporalEntregada>.Correcta(
                new ContrasenaTemporalEntregada(new DatosUsuario(idNuevo, nombre, NombresDeRol.Superusuario, true, true), contrasena),
                "Superusuario creado con contraseña temporal."),
            ResultadoCreacionDeUsuario.NombreDuplicado => RespuestaServicio<ContrasenaTemporalEntregada>.Fallida(MensajesDeIdentidad.NombreDeUsuarioDuplicado),
            _ => RespuestaServicio<ContrasenaTemporalEntregada>.Fallida(MensajesDeIdentidad.DatosDeUsuarioInvalidos)
        };
    }

    public async Task<RespuestaServicio<ContrasenaTemporalEntregada>> RestablecerSuperusuarioAsync(
        string nombreUsuario, CancellationToken cancelacion = default)
    {
        var nombre = nombreUsuario?.Trim();
        var objetivo = string.IsNullOrEmpty(nombre) ? null : await _gestor.ObtenerPorNombreAsync(nombre, cancelacion);
        if (objetivo is not { Rol: NombresDeRol.Superusuario, Activo: true })
        {
            return RespuestaServicio<ContrasenaTemporalEntregada>.Fallida(MensajesDeIdentidad.UsuarioNoEncontrado);
        }

        var contrasena = GeneradorDeContrasenaTemporal.Generar();
        ResultadoDeContrasena? resultado = null;

        try
        {
            await _unidad.EjecutarAsync(async ct =>
            {
                resultado = await _gestor.EstablecerContrasenaAsync(objetivo.IdUsuario, contrasena, debeCambiarContrasena: true, ct);
                if (resultado == ResultadoDeContrasena.Establecida)
                {
                    await _auditoria.RegistrarHechoDeCambioAsync(
                        objetivo.IdUsuario, TipoRegistroAuditoria.Usuario, objetivo.IdUsuario, AccionAuditoria.ReinicioContrasena,
                        ["Contrasena"], "Se restableció la contraseña de un superusuario por procedimiento de recuperación del servidor.", ct);
                }
            }, cancelacion);
        }
        catch (ReglaDeNegocioException excepcion)
        {
            return RespuestaServicio<ContrasenaTemporalEntregada>.Fallida(excepcion.Message);
        }

        return resultado == ResultadoDeContrasena.Establecida
            ? RespuestaServicio<ContrasenaTemporalEntregada>.Correcta(
                new ContrasenaTemporalEntregada(objetivo with { DebeCambiarContrasena = true }, contrasena),
                "Contraseña del superusuario restablecida. Un bloqueo vigente no se levanta.")
            : RespuestaServicio<ContrasenaTemporalEntregada>.Fallida(MensajesDeIdentidad.DatosDeUsuarioInvalidos);
    }
}
