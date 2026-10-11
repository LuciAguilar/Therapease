using System.Reflection;
using System.Reflection.Emit;
using System.Xml.Linq;

namespace TherapEase.IntegrationTests;

/// <summary>
/// Revisa el código ya compilado para comprobar que cada capa solo usa las partes permitidas
/// (por ejemplo, que las páginas no lleguen directo a la base de datos).
/// Solo lee el código: no lo ejecuta ni agrega paquetes.
/// </summary>
internal static class ArquitecturaRevision
{


    // ──── REFERENCIAS DE PROYECTOS ───────────────────────────────────────────────────────────


    internal static readonly string[] Capas = ["Domain", "Application", "Infrastructure", "Web"];
    internal static readonly string[] Modulos = ["Pacientes", "Citas", "Identidad", "Auditoria"];


    /// Tabla de todas las instrucciones que existen en código compilado, para poder leerlas por su número.
    private static readonly IReadOnlyDictionary<short, OpCode> Instrucciones = typeof(OpCodes)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(campo => campo.FieldType == typeof(OpCode))
        .Select(campo => (OpCode)campo.GetValue(null)!)
        .ToDictionary(instruccion => instruccion.Value);


    /// Encuentra la carpeta principal del proyecto a partir de donde corren las pruebas (clon), sin usar rutas fijas de ninguna computadora.
    internal static string Raiz()
    {
        for (var carpeta = new DirectoryInfo(AppContext.BaseDirectory); carpeta is not null; carpeta = carpeta.Parent)
        {
            if (File.Exists(Path.Combine(carpeta.FullName, "TherapEase.sln")))
                return carpeta.FullName;
        }

        throw new DirectoryNotFoundException("No se encontró TherapEase.sln al recorrer la carpeta de pruebas.");
    }


    /// Lee en el archivo .csproj qué otros proyectos tiene enlazados una capa, aunque todavía no los use.
    internal static string[] ReferenciasDeProyecto(string capa)
    {
        var ruta = Path.Combine(Raiz(), "src", capa, $"TherapEase.{capa}.csproj");
        return XDocument.Load(ruta).Descendants()
            .Where(elemento => elemento.Name.LocalName == "ProjectReference")
            .Select(elemento => (string?)elemento.Attribute("Include")
                ?? throw new InvalidDataException($"ProjectReference sin Include en {ruta}."))
            .Select(referencia => Path.GetFileNameWithoutExtension(referencia.Replace('\\', '/')))
            .ToArray();
    }


    /// Lista de qué capas puede usar cada capa. Web usa Domain solo para enumeraciones y constantes, e Infrastructure solo para arrancar.
    internal static string[] CapasPermitidas(string capa) => capa switch
    {
        "Domain" => [],
        "Application" => ["Domain"],
        "Infrastructure" => ["Domain", "Application"],
        "Web" => ["Application", "Domain", "Infrastructure"],
        _ => throw new ArgumentOutOfRangeException(nameof(capa))
    };


    /// Abre el código compilado de una capa (Domain, Application, Infrastructure o Web) para revisarlo.
    internal static Assembly Ensamblado(string capa) => Assembly.Load($"TherapEase.{capa}");



    // ──── TIPOS, FIRMAS Y CÓDIGO COMPILADO ──────────────────────────────────────────────────


    /// Dice a qué capa del proyecto pertenece una clase; las de .NET no cuentan.
    internal static string? Capa(Type tipo) => Capas.FirstOrDefault(capa =>
        tipo.Namespace == $"TherapEase.{capa}" || tipo.Namespace?.StartsWith($"TherapEase.{capa}.", StringComparison.Ordinal) == true);


    /// Dice a qué módulo pertenece una clase (por ejemplo Pacientes o Citas); lo compartido no es un módulo.
    internal static string? Modulo(Type tipo) => Modulos.FirstOrDefault(modulo =>
        Capa(tipo) is { } capa && (tipo.Namespace == $"TherapEase.{capa}.{modulo}"
            || tipo.Namespace?.StartsWith($"TherapEase.{capa}.{modulo}.", StringComparison.Ordinal) == true));


    /// Junta todas las clases que usa una clase: en sus datos, en sus métodos y dentro del código de esos métodos.
    internal static HashSet<Type> Dependencias(Type tipo)
    {
        var tipos = new HashSet<Type>();
        AgregarTipo(tipos, tipo.BaseType);
        foreach (var contrato in tipo.GetInterfaces()) AgregarTipo(tipos, contrato);
        foreach (var argumento in tipo.GetGenericArguments()) AgregarTipo(tipos, argumento);
        AgregarAtributos(tipos, tipo.GetCustomAttributesData());

        const BindingFlags propios = BindingFlags.Public | BindingFlags.NonPublic
            | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
        foreach (var campo in tipo.GetFields(propios))
        {
            AgregarTipo(tipos, campo.FieldType);
            AgregarAtributos(tipos, campo.GetCustomAttributesData());
        }

        var metodos = tipo.GetMethods(propios).Cast<MethodBase>().Concat(tipo.GetConstructors(propios));
        if (tipo.TypeInitializer is { } inicializador) metodos = metodos.Append(inicializador);
        foreach (var metodo in metodos.Distinct())
        {
            AgregarFirma(tipos, metodo);
            AgregarAtributos(tipos, metodo.GetCustomAttributesData());
            var cuerpo = metodo.GetMethodBody();
            if (cuerpo is null) continue;
            foreach (var variable in cuerpo.LocalVariables) AgregarTipo(tipos, variable.LocalType);
            foreach (var captura in cuerpo.ExceptionHandlingClauses.Where(captura => captura.Flags == ExceptionHandlingClauseOptions.Clause))
                AgregarTipo(tipos, captura.CatchType);
            LeerInstrucciones(tipos, metodo, cuerpo.GetILAsByteArray() ?? []);
        }

        tipos.Remove(tipo);
        return tipos;
    }


    /// Devuelve la clase y las clases internas que .NET crea por su cuenta (por ejemplo, para los métodos async).
    internal static IEnumerable<Type> TipoYAnidados(Type tipo) => new[] { tipo }.Concat(
        tipo.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic).SelectMany(TipoYAnidados));


    /// Agrega una clase a la lista; si es una lista o un arreglo de otra clase, agrega también esa otra.
    private static void AgregarTipo(HashSet<Type> tipos, Type? tipo)
    {
        if (tipo is null || !tipos.Add(tipo)) return;
        if (tipo.HasElementType) AgregarTipo(tipos, tipo.GetElementType());
        if (tipo.IsGenericType)
        {
            tipos.Add(tipo.GetGenericTypeDefinition());
            foreach (var argumento in tipo.GetGenericArguments()) AgregarTipo(tipos, argumento);
        }
        if (tipo.IsGenericParameter)
            foreach (var restriccion in tipo.GetGenericParameterConstraints()) AgregarTipo(tipos, restriccion);
    }


    /// Agrega las clases que aparecen en lo que un método recibe y devuelve.
    private static void AgregarFirma(HashSet<Type> tipos, MethodBase metodo)
    {
        AgregarTipo(tipos, metodo.DeclaringType);
        if (metodo is MethodInfo informacion)
        {
            AgregarTipo(tipos, informacion.ReturnType);
            foreach (var argumento in informacion.GetGenericArguments()) AgregarTipo(tipos, argumento);
        }
        foreach (var parametro in metodo.GetParameters())
        {
            AgregarTipo(tipos, parametro.ParameterType);
            AgregarAtributos(tipos, parametro.GetCustomAttributesData());
        }
    }


    /// Agrega las clases mencionadas en los atributos (las etiquetas entre corchetes, como [Fact]).
    private static void AgregarAtributos(HashSet<Type> tipos, IList<CustomAttributeData> atributos)
    {
        foreach (var atributo in atributos)
        {
            AgregarTipo(tipos, atributo.AttributeType);
            foreach (var argumento in atributo.ConstructorArguments) AgregarArgumento(tipos, argumento);
            foreach (var argumento in atributo.NamedArguments) AgregarArgumento(tipos, argumento.TypedValue);
        }
    }


    /// Revisa cada valor de un atributo, porque también puede nombrar una clase.
    private static void AgregarArgumento(HashSet<Type> tipos, CustomAttributeTypedArgument argumento)
    {
        AgregarTipo(tipos, argumento.ArgumentType);
        if (argumento.Value is Type tipo) AgregarTipo(tipos, tipo);
        if (argumento.Value is IEnumerable<CustomAttributeTypedArgument> elementos)
            foreach (var elemento in elementos) AgregarArgumento(tipos, elemento);
    }


    /// Recorre el código compilado de un método y anota cada clase que usa. Si encuentra algo que no reconoce, falla en vez de saltárselo.
    private static void LeerInstrucciones(HashSet<Type> tipos, MethodBase metodo, byte[] codigo)
    {
        var posicion = 0;
        while (posicion < codigo.Length)
        {
            short valor = codigo[posicion++];
            if (valor == 0xfe)
            {
                ExigirBytes(codigo, posicion, 1);
                valor = unchecked((short)(0xfe00 | codigo[posicion++]));
            }
            if (!Instrucciones.TryGetValue(valor, out var instruccion))
                throw new InvalidDataException($"Instrucción desconocida en {metodo.DeclaringType}.{metodo.Name}.");

            var tamano = instruccion.OperandType switch
            {
                OperandType.InlineNone => 0,
                OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar => 1,
                OperandType.InlineVar => 2,
                OperandType.InlineI8 or OperandType.InlineR => 8,
                OperandType.InlineSwitch => TamanoDeSwitch(codigo, posicion),
                OperandType.InlineSig => throw new NotSupportedException($"Firma indirecta no cubierta en {metodo.Name}; ampliar el inspector antes de aprobar."),
                OperandType.InlineBrTarget or OperandType.InlineI or OperandType.InlineString
                    or OperandType.ShortInlineR or OperandType.InlineField or OperandType.InlineMethod
                    or OperandType.InlineTok or OperandType.InlineType => 4,
                _ => throw new NotSupportedException($"Operando no cubierto: {instruccion.OperandType}.")
            };
            ExigirBytes(codigo, posicion, tamano);
            if (instruccion.OperandType is OperandType.InlineField or OperandType.InlineMethod
                or OperandType.InlineTok or OperandType.InlineType)
            {
                var referencia = metodo.Module.ResolveMember(BitConverter.ToInt32(codigo, posicion),
                    metodo.DeclaringType?.GetGenericArguments(), metodo.IsGenericMethod ? metodo.GetGenericArguments() : null);
                switch (referencia)
                {
                    case Type tipo: AgregarTipo(tipos, tipo); break;
                    case MethodBase llamado: AgregarFirma(tipos, llamado); break;
                    case FieldInfo campo:
                        AgregarTipo(tipos, campo.DeclaringType);
                        AgregarTipo(tipos, campo.FieldType);
                        break;
                    default: throw new InvalidDataException($"Referencia sin resolver en {metodo.Name}.");
                }
            }
            posicion += tamano;
        }
    }


    /// Calcula cuánto ocupa una instrucción «switch», que puede tener varios destinos.
    private static int TamanoDeSwitch(byte[] codigo, int posicion)
    {
        ExigirBytes(codigo, posicion, 4);
        var cantidad = BitConverter.ToInt32(codigo, posicion);
        if (cantidad < 0) throw new InvalidDataException("Cantidad de saltos inválida.");
        return checked(4 + cantidad * 4);
    }


    /// Comprueba que el código no termine a la mitad; si falta algo, la revisión falla.
    private static void ExigirBytes(byte[] codigo, int posicion, int cantidad)
    {
        if (cantidad < 0 || posicion > codigo.Length - cantidad)
            throw new InvalidDataException("Código compilado truncado durante la revisión de arquitectura.");
    }


    // ──── REGLAS DE CAPAS, PERSISTENCIA Y CICLOS ─────────────────────────────────────────────


    /// Decide si una capa puede usar un tipo de otra. Web admite enumeraciones y constantes de Domain, pero sus reglas pasan por Application.
    internal static bool DependenciaPermitida(string capa, Type origen, Type destino)
    {
        while (destino.HasElementType) destino = destino.GetElementType()!;
        var otra = Capa(destino);
        if (otra is null || otra == capa) return true;
        if (capa != "Web") return CapasPermitidas(capa).Contains(otra);
        if (otra == "Application") return !EsPersistencia(destino);
        if (otra == "Domain") return destino.IsEnum || EsContenedorDeConstantes(destino);
        while (origen.DeclaringType is { } contenedor) origen = contenedor;
        return otra == "Infrastructure"
            && destino.FullName == "TherapEase.Infrastructure.Configuracion.InyeccionDeDependencias"
            && (origen.FullName == "Program" || origen.FullName == "TherapEase.Web.Comandos.ComandosDeIdentidad");
    }


    /// Acepta una clase estática que solo contiene constantes; tener una constante no permite agregar reglas o servicios.
    internal static bool EsContenedorDeConstantes(Type tipo)
    {
        const BindingFlags propios = BindingFlags.Public | BindingFlags.NonPublic
            | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;
        var campos = tipo.GetFields(propios);
        return tipo.IsClass && tipo.IsAbstract && tipo.IsSealed && campos.Length > 0
            && campos.All(campo => campo.IsStatic && campo.IsLiteral)
            && tipo.GetMethods(propios).Length == 0 && tipo.GetConstructors(propios).Length == 0
            && tipo.TypeInitializer is null
            && tipo.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic).Length == 0;
    }


    /// Dice si una clase sirve para leer o guardar en la base de datos (repositorios, EF Core o Npgsql).
    internal static bool EsPersistencia(Type tipo) =>
        tipo.Namespace?.Contains(".Interfaces.Repositorios", StringComparison.Ordinal) == true
        || tipo.Namespace?.StartsWith("TherapEase.Infrastructure", StringComparison.Ordinal) == true
        || tipo.Namespace?.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal) == true
        || tipo.Namespace?.StartsWith("Npgsql", StringComparison.Ordinal) == true;


    /// Busca círculos de dependencias (A usa B, B usa C y C usa A) y los muestra completos para saber qué corregir.
    internal static string[] Ciclos(IReadOnlyDictionary<string, string[]> grafo)
    {
        var terminados = new HashSet<string>();
        var camino = new List<string>();
        var ciclos = new HashSet<string>();
        foreach (var nodo in grafo.Keys) Visitar(nodo);
        return ciclos.Order().ToArray();

        void Visitar(string nodo)
        {
            var indice = camino.IndexOf(nodo);
            if (indice >= 0)
            {
                ciclos.Add(string.Join(" -> ", camino.Skip(indice).Append(nodo)));
                return;
            }
            if (!terminados.Add(nodo)) return;
            camino.Add(nodo);
            foreach (var destino in grafo.GetValueOrDefault(nodo) ?? []) Visitar(destino);
            camino.RemoveAt(camino.Count - 1);
        }
    }
}
