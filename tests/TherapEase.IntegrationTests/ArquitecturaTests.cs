using System.Reflection;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TherapEase.Application.Pacientes.Interfaces.Repositorios;
using TherapEase.Domain.Auditoria.Entidades.Enums;
using TherapEase.Domain.Citas.Entidades;
using TherapEase.Domain.Compartido.Entidades.Enums;
using TherapEase.Domain.Identidad.Constantes;
using TherapEase.Domain.Identidad.Reglas;
using TherapEase.Domain.Pacientes.Entidades;
using TherapEase.Infrastructure.Data;
using Xunit.Abstractions;

namespace TherapEase.IntegrationTests;

/// <summary>
/// Comprueba que cada capa y cada módulo usen solo lo que tienen permitido.
/// Las últimas pruebas usan ejemplos con errores a propósito para confirmar
/// que la revisión sí los detecta. No usa base de datos.
/// </summary>
public class ArquitecturaTests(ITestOutputHelper salida)
{


    // ──── REFERENCIAS Y DEPENDENCIAS REALES ──────────────────────────────────────────────────


    /// Cada proyecto solo está enlazado con las capas que tiene permitidas.
    [Theory]
    [InlineData("Domain")]
    [InlineData("Application")]
    [InlineData("Infrastructure")]
    [InlineData("Web")]
    public void Proyectos_Referencian_Solo_Capas_Permitidas(string capa)
    {
        // Arrange
        var permitidas = ArquitecturaRevision.CapasPermitidas(capa).Select(otra => $"TherapEase.{otra}");

        // Act
        var referencias = ArquitecturaRevision.ReferenciasDeProyecto(capa);
        var indebidas = referencias.Except(permitidas).ToArray();

        // Assert
        salida.WriteLine($"{capa}: {string.Join(", ", referencias)}");
        Assert.True(indebidas.Length == 0, $"{capa} referencia proyectos prohibidos: {string.Join(", ", indebidas)}.");
    }

    /// Ninguna clase de una capa usa clases de otra capa que no le corresponde, aunque lleguen de forma indirecta.
    [Theory]
    [InlineData("Domain")]
    [InlineData("Application")]
    [InlineData("Infrastructure")]
    [InlineData("Web")]
    public void Tipos_Compilados_Respetan_Limites_De_Capas(string capa)
    {
        // Arrange
        var tipos = ArquitecturaRevision.Ensamblado(capa).GetTypes();

        // Act
        var indebidas = tipos.SelectMany(origen => ArquitecturaRevision.Dependencias(origen)
            .Where(destino => !ArquitecturaRevision.DependenciaPermitida(capa, origen, destino))
            .Select(destino => $"{origen.FullName} -> {destino.FullName}"))
            .Distinct().Order().ToArray();

        // Assert
        salida.WriteLine($"{capa}: {tipos.Length} tipos inspeccionados.");
        Assert.True(indebidas.Length == 0, "Dependencias fuera del límite acordado:\n" + string.Join("\n", indebidas));
    }

    /// Domain y Application no usan bibliotecas de base de datos ni de páginas web.
    [Theory]
    [InlineData("Domain")]
    [InlineData("Application")]
    public void Capas_Internas_No_Referencian_Persistencia_Ni_Web(string capa)
    {
        // Arrange
        var prefijos = new[] { "Microsoft.EntityFrameworkCore", "Npgsql", "Microsoft.AspNetCore" };

        // Act
        var indebidas = ArquitecturaRevision.Ensamblado(capa).GetReferencedAssemblies()
            .Select(referencia => referencia.Name!)
            .Where(nombre => prefijos.Any(prefijo => nombre.StartsWith(prefijo, StringComparison.Ordinal))).ToArray();

        // Assert
        Assert.Empty(indebidas);
    }

    /// Los proyectos no se usan en círculo (A usa B y B usa A).
    [Fact]
    public void Referencias_De_Proyectos_No_Forman_Ciclos()
    {
        // Arrange
        var grafo = ArquitecturaRevision.Capas.ToDictionary(capa => $"TherapEase.{capa}",
            ArquitecturaRevision.ReferenciasDeProyecto);

        // Act
        var ciclos = ArquitecturaRevision.Ciclos(grafo);

        // Assert
        Assert.Empty(ciclos);
    }


    // ──── MÓDULOS, PÁGINAS Y CONTRATOS ───────────────────────────────────────────────────────


    /// Lo compartido no depende de ningún módulo (Pacientes, Citas…), para que no se forme un círculo escondido.
    [Theory]
    [InlineData("Domain")]
    [InlineData("Application")]
    public void Compartido_No_Depende_De_Modulos_De_Negocio(string capa)
    {
        // Arrange
        var tipos = ArquitecturaRevision.Ensamblado(capa).GetTypes()
            .Where(tipo => tipo.Namespace?.StartsWith($"TherapEase.{capa}.Compartido.", StringComparison.Ordinal) == true);

        // Act
        var indebidas = tipos.SelectMany(origen => ArquitecturaRevision.Dependencias(origen)
            .Where(destino => ArquitecturaRevision.Modulo(destino) is not null)
            .Select(destino => $"{origen.FullName} -> {destino.FullName}")).ToArray();

        // Assert
        Assert.True(indebidas.Length == 0, "Dependencias de negocio dentro de Compartido:\n" + string.Join("\n", indebidas));
    }

    /// Los módulos no se usan en círculo: Citas puede usar Pacientes, pero Pacientes no usa Citas, y solo a través de lo público.
    [Fact]
    public void Modulos_No_Forman_Ciclos_Y_Pacientes_No_Depende_De_Citas()
    {
        // Arrange
        var tipos = TiposDeProduccion();
        var relaciones = tipos.SelectMany(origen => ArquitecturaRevision.Dependencias(origen)
            .Select(destino => (Origen: origen, Destino: destino)))
            .Where(par => ArquitecturaRevision.Modulo(par.Origen) is not null
                && ArquitecturaRevision.Modulo(par.Destino) is not null
                && ArquitecturaRevision.Modulo(par.Origen) != ArquitecturaRevision.Modulo(par.Destino)).ToArray();

        // Act
        var grafo = ArquitecturaRevision.Modulos.ToDictionary(modulo => modulo, modulo => relaciones
            .Where(par => ArquitecturaRevision.Modulo(par.Origen) == modulo)
            .Select(par => ArquitecturaRevision.Modulo(par.Destino)!).Distinct().ToArray());
        var accesosPrivados = relaciones.Where(par => ArquitecturaRevision.Modulo(par.Origen) == "Citas"
            && ArquitecturaRevision.Modulo(par.Destino) == "Pacientes" && !par.Destino.IsVisible).ToArray();

        // Assert
        foreach (var (modulo, destinos) in grafo) salida.WriteLine($"{modulo}: {string.Join(", ", destinos)}");
        Assert.DoesNotContain("Citas", grafo["Pacientes"]);
        Assert.Empty(accesosPrivados);
        Assert.Empty(ArquitecturaRevision.Ciclos(grafo));
    }

    /// Ninguna página (PageModel) habla directo con la base de datos; siempre pasa por Application.
    [Fact]
    public void PageModel_No_Accede_Directamente_A_Persistencia()
    {
        // Arrange
        var paginas = ArquitecturaRevision.Ensamblado("Web").GetTypes()
            .Where(tipo => typeof(PageModel).IsAssignableFrom(tipo) && !tipo.IsAbstract).ToArray();

        // Act
        var indebidas = paginas.SelectMany(pagina => ArquitecturaRevision.TipoYAnidados(pagina)
            .SelectMany(ArquitecturaRevision.Dependencias)
            .Where(ArquitecturaRevision.EsPersistencia).Select(destino => $"{pagina.FullName} -> {destino.FullName}"))
            .Distinct().ToArray();

        // Assert
        Assert.NotEmpty(paginas);
        salida.WriteLine("PageModel disponibles: " + string.Join(", ", paginas.Select(pagina => pagina.FullName)));
        Assert.True(indebidas.Length == 0, "Acceso directo a datos desde PageModel:\n" + string.Join("\n", indebidas));
    }

    /// Cada módulo tiene una sola interfaz para guardar datos, y Infrastructure la implementa.
    [Fact]
    public void Persistencia_Tiene_Un_Contrato_Por_Modulo_Con_Implementacion()
    {
        // Arrange
        var contratos = ArquitecturaRevision.Ensamblado("Application").GetTypes()
            .Where(tipo => tipo.IsInterface && ArquitecturaRevision.EsPersistencia(tipo)).ToArray();
        var implementaciones = ArquitecturaRevision.Ensamblado("Infrastructure").GetTypes()
            .Where(tipo => tipo.IsClass && !tipo.IsAbstract).ToArray();

        // Act
        var porModulo = ArquitecturaRevision.Modulos.ToDictionary(modulo => modulo,
            modulo => contratos.Where(tipo => ArquitecturaRevision.Modulo(tipo) == modulo).ToArray());

        // Assert
        foreach (var (modulo, propios) in porModulo)
        {
            var contrato = Assert.Single(propios);
            Assert.True(contrato.IsVisible, $"El contrato de {modulo} debe ser público.");
            Assert.Contains(implementaciones, contrato.IsAssignableFrom);
            salida.WriteLine($"{modulo}: {contrato.FullName}");
        }
    }

    /// Existe en Application una clase que coordina Pacientes y Citas (CU04: dar de baja con citas) sin estar dentro de ninguno de los dos.
    [Fact]
    public void Existe_Candidato_A_Coordinador_CU04_Fuera_De_Pacientes_Y_Citas()
    {
        // Arrange
        var candidatos = ArquitecturaRevision.Ensamblado("Application").GetTypes()
            .Where(tipo => tipo.IsClass && tipo.IsVisible && (!tipo.IsAbstract || tipo.IsSealed)
                && ArquitecturaRevision.Modulo(tipo) is not ("Pacientes" or "Citas")).ToArray();

        // Act
        var coordinadores = candidatos.Where(tipo =>
        {
            var modulos = ArquitecturaRevision.TipoYAnidados(tipo).SelectMany(ArquitecturaRevision.Dependencias)
                .Select(ArquitecturaRevision.Modulo).ToHashSet();
            return modulos.Contains("Pacientes") && modulos.Contains("Citas");
        }).ToArray();

        // Assert
        Assert.True(coordinadores.Length > 0,
            "No existe candidato a coordinador CU04 que use Pacientes y Citas fuera de ambos módulos. Miguel debe implementarlo; después comprobar la operación real, no solo esta ubicación.");
        salida.WriteLine(string.Join(", ", coordinadores.Select(tipo => tipo.FullName)));
    }


    // ──── CONTROLES NEGATIVOS DEL INSPECTOR ──────────────────────────────────────────────────


    /// La excepción aprobada permite enumeraciones y clases que solo contienen constantes de Domain.
    [Theory]
    [InlineData(typeof(Permiso))]
    [InlineData(typeof(TipoRegistroAuditoria))]
    [InlineData(typeof(NombresDeRol))]
    [InlineData(typeof(Reclamaciones))]
    public void Web_Permite_Enumeraciones_Y_Constantes_De_Domain(Type destino)
    {
        // Arrange
        var origen = typeof(PaginaConPersistenciaDeEjemplo);

        // Act
        var permitida = ArquitecturaRevision.DependenciaPermitida("Web", origen, destino);

        // Assert
        Assert.True(permitida);
    }

    /// Un arreglo o un parámetro por referencia no convierte una enumeración permitida en una regla de negocio.
    [Fact]
    public void Web_Permite_Enumeracion_En_Arreglo_Y_Parametro_Por_Referencia()
    {
        // Arrange
        var destinos = new[] { typeof(Permiso).MakeArrayType(), typeof(Permiso).MakeByRefType() };

        // Act
        var permitidas = destinos.Select(destino => ArquitecturaRevision.DependenciaPermitida(
            "Web", typeof(PaginaConPersistenciaDeEjemplo), destino)).ToArray();

        // Assert
        Assert.All(permitidas, permitida => Assert.True(permitida));
    }

    /// La excepción nunca permite que Web use entidades o reglas de Domain directamente.
    [Theory]
    [InlineData(typeof(Paciente))]
    [InlineData(typeof(Cita))]
    [InlineData(typeof(MatrizDePermisos))]
    public void Web_Rechaza_Entidades_Y_Reglas_De_Domain(Type destino)
    {
        // Arrange
        var origen = typeof(PaginaConPersistenciaDeEjemplo);

        // Act
        var permitida = ArquitecturaRevision.DependenciaPermitida("Web", origen, destino);

        // Assert
        Assert.False(permitida);
    }

    /// Agregar una constante a una clase con métodos no la vuelve un contenedor permitido de constantes.
    [Fact]
    public void Constantes_No_Permite_Clase_Con_Logica()
    {
        // Arrange
        var tipo = typeof(ConstantesConReglasDeEjemplo);

        // Act
        var soloConstantes = ArquitecturaRevision.EsContenedorDeConstantes(tipo);

        // Assert
        Assert.False(soloConstantes);
    }

    /// La revisión detecta un repositorio aunque esté escondido dentro de una lista.
    [Fact]
    public void Inspector_Detecta_Repositorio_Generico_En_Una_Pagina()
    {
        // Arrange
        var tipo = typeof(PaginaConPersistenciaDeEjemplo);

        // Act
        var dependencias = ArquitecturaRevision.Dependencias(tipo);

        // Assert
        Assert.Contains(typeof(IRepositorioPacientes), dependencias);
        Assert.Contains(dependencias, ArquitecturaRevision.EsPersistencia);
    }

    /// La revisión detecta una clase usada dentro de un método, no solo en lo que recibe o devuelve.
    [Fact]
    public void Inspector_Detecta_Tipo_Infrastructure_Dentro_De_Metodo()
    {
        // Arrange
        var tipo = typeof(LlamadasDeEjemplo);

        // Act
        var dependencias = ArquitecturaRevision.Dependencias(tipo);

        // Assert
        Assert.Contains(typeof(ContextoDeDatos), dependencias);
        Assert.False(ArquitecturaRevision.DependenciaPermitida("Web", tipo, typeof(ContextoDeDatos)));
    }

    /// La revisión también mira dentro de los métodos async, que .NET reorganiza al compilar.
    [Fact]
    public void Inspector_Detecta_Persistencia_Dentro_De_Metodo_Async()
    {
        // Arrange
        var tipos = ArquitecturaRevision.TipoYAnidados(typeof(PaginaConPersistenciaDeEjemplo));

        // Act
        var dependencias = tipos.SelectMany(ArquitecturaRevision.Dependencias).ToHashSet();

        // Assert
        Assert.Contains(typeof(ContextoDeDatos), dependencias);
    }

    /// La revisión detecta un círculo largo (A → B → C → A), no solo uno de dos.
    [Fact]
    public void Inspector_Detecta_Ciclo_Indirecto()
    {
        // Arrange
        var grafo = new Dictionary<string, string[]>
        {
            ["Pacientes"] = ["Citas"], ["Citas"] = ["Identidad"], ["Identidad"] = ["Pacientes"]
        };

        // Act
        var ciclos = ArquitecturaRevision.Ciclos(grafo);

        // Assert
        Assert.Contains("Pacientes -> Citas -> Identidad -> Pacientes", ciclos);
    }

    /// Junta las clases de las cuatro capas reales, sin incluir los ejemplos de estas pruebas.
    private static Type[] TiposDeProduccion() => ArquitecturaRevision.Capas
        .SelectMany(capa => ArquitecturaRevision.Ensamblado(capa).GetTypes()).ToArray();

    /// <summary>
    /// Página de ejemplo, con errores a propósito, para comprobar que la revisión los encuentra.
    /// Solo existe en las pruebas; nunca forma parte de la aplicación.
    /// </summary>
    private sealed class PaginaConPersistenciaDeEjemplo : PageModel
    {
        public List<IRepositorioPacientes> Repositorios { get; } = [];

        /// Usa la base de datos dentro de un método async, a propósito.
        public async Task<Type> OnGetAsync()
        {
            await Task.Yield();
            return typeof(ContextoDeDatos);
        }
    }

    /// <summary>
    /// Combina una constante y un método para comprobar que la excepción no permite lógica escondida.
    /// Solo es un ejemplo de prueba y no se incorpora a Domain.
    /// </summary>
    private static class ConstantesConReglasDeEjemplo
    {
        public const string Valor = "ejemplo ficticio";

        /// El método hace que esta clase no sea un contenedor exclusivo de constantes.
        public static string ObtenerValor() => Valor;
    }

    /// <summary>
    /// Clase de ejemplo que usa la base de datos dentro de un método, a propósito.
    /// No se conecta a nada ni ejecuta la aplicación.
    /// </summary>
    private static class LlamadasDeEjemplo
    {
        /// Menciona la base de datos solo dentro del método, no en lo que recibe o devuelve.
        public static Type TipoDeContexto() => typeof(ContextoDeDatos);
    }
}
