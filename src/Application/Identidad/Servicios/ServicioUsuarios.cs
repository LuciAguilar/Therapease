using TherapEase.Application.Auditoria.Interfaces.Servicios;
using TherapEase.Application.Compartido.Interfaces.Repositorios;
using TherapEase.Application.Compartido.Interfaces.Servicios;
using TherapEase.Application.Compartido.Modelos;
using TherapEase.Application.Identidad.Constantes;
using TherapEase.Application.Identidad.Interfaces.Repositorios;
using TherapEase.Application.Identidad.Interfaces.Servicios;
using TherapEase.Application.Identidad.Modelos;
using TherapEase.Domain.Auditoria.Entidades.Enums;
using TherapEase.Domain.Compartido.Entidades.Enums;
using TherapEase.Domain.Compartido.Excepciones;
using TherapEase.Domain.Identidad.Constantes;
using TherapEase.Domain.Identidad.Reglas;

namespace TherapEase.Application.Identidad.Servicios;

public class ServicioUsuarios : IServicioUsuarios
{
    private readonly IGestorIdentidad _gestor;
    private readonly IUnidadDeTrabajo _unidad;
    private readonly IServicioAuditoria _auditoria;
    private readonly IAutorizacion _autorizacion;
    private readonly IUsuarioActual _usuarioActual;

    public ServicioUsuarios(
        IGestorIdentidad gestor,
        IUnidadDeTrabajo unidad,
        IServicioAuditoria auditoria,
        IAutorizacion autorizacion,
        IUsuarioActual usuarioActual)
    {
        _gestor = gestor;
        _unidad = unidad;
        _auditoria = auditoria;
        _autorizacion = autorizacion;
        _usuarioActual = usuarioActual;
    }

    public async Task<RespuestaServicio<ContrasenaTemporalEntregada>> CrearUsuarioAsync(
        string nombreUsuario, string rol, CancellationToken cancelacion = default)
    {
        if (await ObtenerActorAutorizadoAsync(cancelacion) is not { } idActor)
        {
            return RespuestaServicio<ContrasenaTemporalEntregada>.Fallida(MensajesDeIdentidad.SinPermiso);
        }

        var nombre = nombreUsuario?.Trim();
        if (string.IsNullOrEmpty(nombre) || nombre.Length > LimitesDeIdentidad.LongitudMaximaNombreUsuario || !ReglasDeRol.EsRolConocido(rol))
        {
            return RespuestaServicio<ContrasenaTemporalEntregada>.Fallida(MensajesDeIdentidad.DatosDeUsuarioInvalidos);
        }

        var contrasena = GeneradorDeContrasenaTemporal.Generar();
        var idNuevo = Guid.CreateVersion7();
        ResultadoCreacionDeUsuario? resultado = null;

        try
        {
            await _unidad.EjecutarAsync(async ct =>
            {
                resultado = await _gestor.CrearAsync(idNuevo, nombre, rol, contrasena, debeCambiarContrasena: true, ct);
                if (resultado == ResultadoCreacionDeUsuario.Creado)
                {
                    await _auditoria.RegistrarHechoDeCambioAsync(
                        idActor, TipoRegistroAuditoria.Usuario, idNuevo, AccionAuditoria.Alta,
                        ["NombreUsuario", "Rol", "DebeCambiarContrasena"], "Se creó un usuario con contraseña temporal.", ct);
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
                new ContrasenaTemporalEntregada(new DatosUsuario(idNuevo, nombre, rol, true, true), contrasena),
                "Usuario creado. Entrega la contraseña temporal por un canal seguro."),
            ResultadoCreacionDeUsuario.NombreDuplicado => RespuestaServicio<ContrasenaTemporalEntregada>.Fallida(MensajesDeIdentidad.NombreDeUsuarioDuplicado),
            _ => RespuestaServicio<ContrasenaTemporalEntregada>.Fallida(MensajesDeIdentidad.DatosDeUsuarioInvalidos)
        };
    }

    public async Task<RespuestaServicio<DatosUsuario>> CambiarRolAsync(
        Guid idUsuario, string nuevoRol, CancellationToken cancelacion = default)
    {
        if (await ObtenerActorAutorizadoAsync(cancelacion) is not { } idActor)
        {
            return RespuestaServicio<DatosUsuario>.Fallida(MensajesDeIdentidad.SinPermiso);
        }

        if (!ReglasDeRol.EsRolConocido(nuevoRol))
        {
            return RespuestaServicio<DatosUsuario>.Fallida(MensajesDeIdentidad.DatosDeUsuarioInvalidos);
        }

        var objetivo = await _gestor.ObtenerAsync(idUsuario, cancelacion);
        if (objetivo is null)
        {
            return RespuestaServicio<DatosUsuario>.Fallida(MensajesDeIdentidad.UsuarioNoEncontrado);
        }

        if (objetivo.Rol == nuevoRol)
        {
            return RespuestaServicio<DatosUsuario>.Fallida("El usuario ya tiene ese rol.");
        }

        if (await DejariaSinSuperusuarioAsync(objetivo, perderiaElRol: nuevoRol != NombresDeRol.Superusuario, cancelacion))
        {
            return RespuestaServicio<DatosUsuario>.Fallida(MensajesDeIdentidad.UltimoSuperusuario);
        }

        try
        {
            await _unidad.EjecutarAsync(async ct =>
            {
                await _gestor.CambiarRolAsync(idUsuario, nuevoRol, ct);
                await _auditoria.RegistrarHechoDeCambioAsync(
                    idActor, TipoRegistroAuditoria.Usuario, idUsuario, AccionAuditoria.Actualizacion,
                    ["Rol"], "Se cambió el rol de un usuario.", ct);
            }, cancelacion);
        }
        catch (ReglaDeNegocioException excepcion)
        {
            return RespuestaServicio<DatosUsuario>.Fallida(excepcion.Message);
        }

        return RespuestaServicio<DatosUsuario>.Correcta(objetivo with { Rol = nuevoRol }, "Rol actualizado.");
    }

    public async Task<RespuestaServicio<DatosUsuario>> DesactivarUsuarioAsync(Guid idUsuario, CancellationToken cancelacion = default)
    {
        if (await ObtenerActorAutorizadoAsync(cancelacion) is not { } idActor)
        {
            return RespuestaServicio<DatosUsuario>.Fallida(MensajesDeIdentidad.SinPermiso);
        }

        var objetivo = await _gestor.ObtenerAsync(idUsuario, cancelacion);
        if (objetivo is null)
        {
            return RespuestaServicio<DatosUsuario>.Fallida(MensajesDeIdentidad.UsuarioNoEncontrado);
        }

        if (!objetivo.Activo)
        {
            return RespuestaServicio<DatosUsuario>.Fallida("El usuario ya está desactivado.");
        }

        if (await DejariaSinSuperusuarioAsync(objetivo, perderiaElRol: true, cancelacion))
        {
            return RespuestaServicio<DatosUsuario>.Fallida(MensajesDeIdentidad.UltimoSuperusuario);
        }

        try
        {
            await _unidad.EjecutarAsync(async ct =>
            {
                await _gestor.DesactivarAsync(idUsuario, ct);
                await _auditoria.RegistrarHechoDeCambioAsync(
                    idActor, TipoRegistroAuditoria.Usuario, idUsuario, AccionAuditoria.Desactivacion,
                    ["Activo"], "Se desactivó un usuario.", ct);
            }, cancelacion);
        }
        catch (ReglaDeNegocioException excepcion)
        {
            return RespuestaServicio<DatosUsuario>.Fallida(excepcion.Message);
        }

        return RespuestaServicio<DatosUsuario>.Correcta(objetivo with { Activo = false }, "Usuario desactivado.");
    }

    public async Task<RespuestaServicio<ContrasenaTemporalEntregada>> RestablecerContrasenaAsync(
        Guid idUsuario, CancellationToken cancelacion = default)
    {
        if (await ObtenerActorAutorizadoAsync(cancelacion) is not { } idActor)
        {
            return RespuestaServicio<ContrasenaTemporalEntregada>.Fallida(MensajesDeIdentidad.SinPermiso);
        }

        var objetivo = await _gestor.ObtenerAsync(idUsuario, cancelacion);
        if (objetivo is not { Activo: true })
        {
            return RespuestaServicio<ContrasenaTemporalEntregada>.Fallida(MensajesDeIdentidad.UsuarioNoEncontrado);
        }

        var contrasena = GeneradorDeContrasenaTemporal.Generar();
        ResultadoDeContrasena? resultado = null;

        try
        {
            await _unidad.EjecutarAsync(async ct =>
            {
                resultado = await _gestor.EstablecerContrasenaAsync(idUsuario, contrasena, debeCambiarContrasena: true, ct);
                if (resultado == ResultadoDeContrasena.Establecida)
                {
                    await _auditoria.RegistrarHechoDeCambioAsync(
                        idActor, TipoRegistroAuditoria.Usuario, idUsuario, AccionAuditoria.ReinicioContrasena,
                        ["Contrasena"], "Se restableció la contraseña de un usuario.", ct);
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
                "Contraseña restablecida. Entrega la contraseña temporal por un canal seguro.")
            : RespuestaServicio<ContrasenaTemporalEntregada>.Fallida(MensajesDeIdentidad.DatosDeUsuarioInvalidos);
    }

    private async Task<Guid?> ObtenerActorAutorizadoAsync(CancellationToken cancelacion) =>
        await _autorizacion.TienePermisoAsync(Permiso.AdministrarUsuarios, cancelacion) ? _usuarioActual.IdUsuario : null;

    // La base lo impone aun con cambios simultáneos; esta comprobación evita viajes inútiles y da el mensaje claro.
    private async Task<bool> DejariaSinSuperusuarioAsync(DatosUsuario objetivo, bool perderiaElRol, CancellationToken cancelacion) =>
        perderiaElRol
        && objetivo is { Rol: NombresDeRol.Superusuario, Activo: true }
        && await _gestor.ContarSuperusuariosActivosAsync(cancelacion) <= 1;
}
