using TherapEase.Domain.Compartido.Excepciones;
using TherapEase.Domain.Compartido.Reglas;
using TherapEase.Domain.Pacientes.Constantes;
using TherapEase.Domain.Pacientes.Entidades;

namespace TherapEase.Domain.Pacientes.Reglas;

// Validación de los datos de la ficha (valores aprobados por Lucía el 26-oct-2026, provisionales para datos de prueba).
// Todos los mensajes son fijos: nunca repiten el valor escrito.
public static class ReglasDeDatosDePaciente
{
    // Valida el formato de cada dato y devuelve los datos normalizados: sin espacios exteriores, correo en minúsculas, teléfono solo con dígitos y tutor completo o ausente.
    // No exige nacimiento ni tutor: una edición sin cambios de una ficha anterior no debe obligar a inventarlos; eso lo decide ExigirCompletos.
    public static DatosDePaciente Normalizar(DatosDePaciente? datos, DateOnly hoy)
    {
        ArgumentNullException.ThrowIfNull(datos);

        if (TieneSurrogadosInvalidos(datos.Nombre)
            || !ReglasDeNombreYCorreo.EsNombreValido(datos.Nombre, LimitesDePaciente.LongitudMinimaNombre, LimitesDePaciente.LongitudMaximaNombre, out var nombre)
            || !NormalizacionDeNombres.TryNormalizar(nombre, out var clave)
            || clave.Length == 0)
        {
            throw new ReglaDeNegocioException(MensajesDeDatosDePaciente.NombreInvalido);
        }

        if (datos.FechaNacimiento is { } nacimiento && (nacimiento > hoy || nacimiento.Year < LimitesDePaciente.AnioMinimoDeNacimiento))
        {
            throw new ReglaDeNegocioException(MensajesDeDatosDePaciente.NacimientoInvalido);
        }

        if (!EsTelefonoOpcionalValido(datos.Telefono, out var telefono))
        {
            throw new ReglaDeNegocioException(MensajesDeDatosDePaciente.TelefonoInvalido);
        }

        if (!EsCorreoOpcionalValido(datos.Correo, out var correo))
        {
            throw new ReglaDeNegocioException(MensajesDeDatosDePaciente.CorreoInvalido);
        }

        if (!EsTextoOpcionalValido(datos.Escuela, LimitesDePaciente.LongitudMaximaEscuela, out var escuela))
        {
            throw new ReglaDeNegocioException(MensajesDeDatosDePaciente.EscuelaInvalida);
        }

        if (!EsTextoOpcionalValido(datos.Grupo, LimitesDePaciente.LongitudMaximaGrupo, out var grupo))
        {
            throw new ReglaDeNegocioException(MensajesDeDatosDePaciente.GrupoInvalido);
        }

        return new DatosDePaciente(nombre, datos.FechaNacimiento, telefono, correo, escuela, grupo, NormalizarTutor(datos.Tutor));
    }

    // Reglas que dependen de que haya datos que guardar: nacimiento obligatorio y, si la persona es menor de 18 años, un tutor completo.
    // Se comprueba con el nacimiento resultante de la misma operación, no con el anterior.
    public static void ExigirCompletos(DatosDePaciente datosNormalizados, DateOnly hoy)
    {
        if (datosNormalizados.FechaNacimiento is not { } nacimiento)
        {
            throw new ReglaDeNegocioException(MensajesDeDatosDePaciente.NacimientoObligatorio);
        }

        if (ReglasDeEdad.EsMenorDeEdad(nacimiento, hoy) && datosNormalizados.Tutor is null)
        {
            throw new ReglaDeNegocioException(MensajesDeDatosDePaciente.TutorObligatorioParaMenores);
        }
    }

    // Si se da algún dato del tutor, sus datos requeridos se validan juntos: nombre, parentesco y teléfono o correo. Sin ningún dato, no hay tutor.
    private static DatosDeTutor? NormalizarTutor(DatosDeTutor? tutor)
    {
        if (tutor is null)
        {
            return null;
        }

        var hayAlgo = new[] { tutor.Nombre, tutor.Parentesco, tutor.Telefono, tutor.Correo }.Any(valor => !string.IsNullOrWhiteSpace(valor));
        if (!hayAlgo)
        {
            return null;
        }

        // Se evalúan todos (sin cortocircuito) para que cada variable quede asignada.
        var nombreValido = EsTextoOpcionalValido(tutor.Nombre, LimitesDePaciente.LongitudMaximaNombreDeTutor, out var nombre);
        var parentescoValido = EsTextoOpcionalValido(tutor.Parentesco, LimitesDePaciente.LongitudMaximaParentesco, out var parentesco);
        var telefonoValido = EsTelefonoOpcionalValido(tutor.Telefono, out var telefono);
        var correoValido = EsCorreoOpcionalValido(tutor.Correo, out var correo);
        if (!(nombreValido && parentescoValido && telefonoValido && correoValido))
        {
            throw new ReglaDeNegocioException(MensajesDeDatosDePaciente.TutorInvalido);
        }

        nombre = nombre?.Length >= LimitesDePaciente.LongitudMinimaNombre ? nombre : null;
        parentesco = parentesco?.Length >= LimitesDePaciente.LongitudMinimaParentesco ? parentesco : null;
        if (nombre is null || parentesco is null || (telefono is null && correo is null))
        {
            throw new ReglaDeNegocioException(MensajesDeDatosDePaciente.TutorIncompleto);
        }

        return new DatosDeTutor(nombre, parentesco, telefono, correo);
    }

    private static bool EsTelefonoOpcionalValido(string? entrada, out string? telefono)
    {
        telefono = null;
        return !TieneSurrogadosInvalidos(entrada) && ReglasDeTelefono.EsTelefonoValido(entrada, out telefono);
    }

    private static bool EsCorreoOpcionalValido(string? entrada, out string? correo)
    {
        correo = null;
        return !TieneSurrogadosInvalidos(entrada)
            && ReglasDeNombreYCorreo.EsCorreoValido(entrada, LimitesDePaciente.LongitudMaximaCorreo, out correo);
    }

    // Texto opcional sin límite inferior: vacío es ausente. Se rechazan el exceso de caracteres y los caracteres de control o de formato.
    private static bool EsTextoOpcionalValido(string? entrada, int longitudMaxima, out string? texto)
    {
        texto = null;
        var recortado = entrada?.Trim();
        if (string.IsNullOrEmpty(recortado))
        {
            return true;
        }

        if (TieneSurrogadosInvalidos(recortado) || recortado.Length > longitudMaxima || ReglasDeNombreYCorreo.TieneCaracteresInvisibles(recortado))
        {
            return false;
        }

        texto = recortado;
        return true;
    }

    // Un texto con sustitutos Unicode sueltos no se puede guardar en la base ni descomponer.
    internal static bool TieneSurrogadosInvalidos(string? texto)
    {
        if (texto is null)
        {
            return false;
        }

        for (var i = 0; i < texto.Length; i++)
        {
            if (char.IsHighSurrogate(texto[i]))
            {
                if (i + 1 >= texto.Length || !char.IsLowSurrogate(texto[i + 1]))
                {
                    return true;
                }

                i++;
            }
            else if (char.IsLowSurrogate(texto[i]))
            {
                return true;
            }
        }

        return false;
    }
}
