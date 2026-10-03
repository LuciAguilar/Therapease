using TherapEase.Domain.Compartido.Entidades.Enums;

namespace TherapEase.Application.Identidad.Modelos;

public sealed record IdentidadYPermisos(DatosUsuario Usuario, IReadOnlyList<Permiso> Permisos);
