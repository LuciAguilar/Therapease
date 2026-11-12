# TherapEase — B01 · QA de páginas de Lucía

**Preparación local del 10-nov-2026 sobre main 32e7bca, después de #13.** Sin commit ni PR nuevo. Autoría de las pruebas de esta entrega, Lucía con Codex. Claude APROBADO v1, 11-nov, reauditoría. Publicación, CI nueva y revisión humana de Miguel pendientes. B00/B01/LCA/R04 siguen abiertos.

## 1. Alcance y trazabilidad

| Estado | Parte | Dónde se comprueba |
| --- | --- | --- |
| ✅ ejecutado | Acceso anónimo, menús, roles, retorno externo y rechazo genérico | AccesoSesionesYPermisosTests, casos de acceso y permisos. |
| ✅ ejecutado | Bloqueo, cookies, caducidad, renovación, salida y revocaciones | Misma clase, casos de sesiones y contraseñas. Reloj controlado y solicitudes reales. |
| ✅ ejecutado | Temporal, cambio propio, campos extra y antifalsificación | Misma clase. Cada POST recibe token ausente y de otra cookie. |
| ✅ ejecutado | Alta, edición, búsqueda, último Superusuario y auditoría desde la misma ventana | Misma clase, cuentas y resultados inciertos. |
| ✅ ejecutado | Historial sin temporal, una sola respuesta al POST y respuesta perdida antes/después de guardar | Casos de alta e interrupción con Route.Fetch/Abort, sin reintentos. |
| ✅ ejecutado | Fallo del evento de entrada, reversión de alta y POST complementarios | FallosDeFormularioTests, tres pruebas HTTP procedentes del QA local auditado. |
| ✅ ejecutado | Cabeceras y /Acceso/Denegado anónimo con 403 y no-store | CabecerasSeguridadTests. |
| ✅ Claude / ⌛ publicación y revisión humana | Auditoría, revisión humana, CI publicada y decisión | Claude APROBADO v1, 11-nov, reauditoría. Miguel revisa las pruebas y Lucía decide. |

Se reutilizan los identificadores del [plan de pruebas](TherapEase-09-plan-de-pruebas.md), CU01/CU02/CU11, Q03/Q08 y los controles S aplicables. No se duplican sus tablas de casos. Las 57 comprobaciones anteriores de Node se reorganizan en 54 casos xUnit, con varias comprobaciones por caso. No sumar cifras ni presentarlas como 54 funciones nuevas.

Los casos de acceso denegado por Usuaria usan solicitudes HTTP desde el contexto autenticado del navegador. La consulta autorizada Q03 abre la URL en la misma ventana y lee su JSON. Las pruebas de PageModel de Dulce siguen siendo suyas.

### Organización de archivos — 11-nov-2026

Las pruebas de Lucía se clasifican por función. [Integración](../../tests/TherapEase.IntegrationTests/README.md) separa cabeceras, formularios HTTP, HTTPS, identidad, auditoría, pacientes, datos, entorno y ayudas. [E2E](../../tests/TherapEase.E2ETests/README.md) separa acceso, permisos, sesiones, contraseñas, formularios, seguridad y apoyo.

Se reorganizaron los archivos de Lucía y, con su autorización posterior del 11-nov, también los tres archivos de pacientes de Miguel. MigracionDeDatosDePacienteTests queda en Pacientes/Migracion; PacientesServidorTests en Pacientes/Fichas; PacientesDePrueba en Apoyo/Pacientes. Las 48 clases/ayudas de UnitTests quedan sin cambios. Tras mover estos tres archivos, compilación sin errores/advertencias y 47/47 casos dirigidos correctos; catálogo conservado, sin repetir la batería completa. La ejecución de 710 unitarias es regresión de código de Miguel y Dulce, no autoría de Lucía.

La clase de navegador se reparte mediante partial, manteniendo los mismos 24 métodos y 54 casos xUnit. Namespaces y filtros conservados. Comprobado tras reorganizar: 54/54 navegador, 710/710 unitarias y 469/469 integraciones, sin omitidas. Los catálogos de casos coinciden con la entrega anterior. Los 24 métodos E2E conservan el contenido y los archivos movidos de integración conservan sus huellas.

## 2. Cómo repetir

Docker disponible, .NET 10 y PowerShell. Preparar Playwright como indica el [README](../../README.md#pruebas-de-acceso-sesiones-y-permisos-en-navegador).

~~~powershell
pwsh scripts/qa/probar-paginas.ps1 -Canal chrome
~~~

Omitir -Canal chrome para usar Chromium instalado con Playwright. El script compila y prepara Web local antes de probar. Una variable de entorno permite a las ayudas HTTPS usar esa salida; no se publica el programa en un servicio externo.

~~~powershell
dotnet test tests/TherapEase.UnitTests -c Release
dotnet test tests/TherapEase.IntegrationTests -c Release --filter "FullyQualifiedName~CabecerasSeguridadTests|FullyQualifiedName~FallosDeFormularioTests"
~~~

Los casos HTTP se pueden ejecutar con la salida normal de compilación. Para comprobar todos los recursos de páginas en Production, utilizar la salida publicada configurada por el script o por CI.

## 3. Herramientas y evidencia

| Herramienta | Uso |
| --- | --- |
| PowerShell y .NET/xUnit | Preparan y ejecutan las pruebas; TRX con resultados por caso. |
| Playwright .NET 1.58.0 | Maneja Chrome local. CI prepara Chromium con la misma biblioteca. |
| Docker y Testcontainers | PostgreSQL ficticio desechable, con migrador y usuario de aplicación separados. |
| HTTPS local | Certificado temporal, TLS y nombre comprobados por el cliente .NET. El contexto del navegador acepta ese certificado ficticio. |
| Datos y registros | Cuentas únicas por recorrido, cookies y contraseñas solo en memoria; errores de prueba con paso y línea seguros. |

Conexión de la prueba: xUnit → Playwright → navegador → HTTPS → ASP.NET Core en Production local → PostgreSQL en Docker. Los formularios HTTP complementarios usan HttpClient sin Chrome. Las claves temporales del certificado se retiran al finalizar.

La ejecución de servidor comprueba caducidad con cookies firmadas 29, 31 y 16 minutos antes. No espera treinta minutos físicamente. La integración nueva no acredita rendimiento, pruebas de uso con otra persona ni Q16 completo de B04. Por decisión de Lucía registrada el 11-nov en AGENTS sección 2.7, no se graban trazas de identidad hasta Q16/B04. Entonces se decidirá el uso de trazas enmascaradas. Tampoco se guardan capturas de formularios de identidad. CI conserva únicamente TRX durante siete días.

## 4. Entrega y revisión

La primera pasada de adaptación tuvo 52/54 correctas; la segunda, 53/54. Los fallos correspondieron a preparar la expectativa de redirección al cambiar rol y a leer el documento durante el cambio al error de caché de Chrome. Se conservaron los resultados anteriores y se corrigieron las pruebas. La versión final dio 54/54 desde el script repetible en Chrome 157.0.8103.50. Compilación sin errores ni advertencias, 710/710 unitarias y 469/469 integraciones, incluidas 10 de cabeceras y 3 HTTP. Sin fallidas ni omitidas. Los controles locales de fijaciones y archivos sensibles pasaron. NuGet no reportó vulnerabilidades en E2ETests ni sus transitivos en la consulta de esta entrega. Estos resultados son locales; la CI remota del nuevo PR está pendiente.

El mismo PR retirará tres notas de docs/reviews. Sus copias personales se conservan fuera del repositorio y sus enlaces compartidos apuntan a los PR originales. No se modifica el historial de Git.

Antes de publicar se revisan la evidencia final, este documento y la [nota de dependencias de QA](../architecture/adr/ADR-06-monorepo.md#nota-posterior--qa-de-páginas-b01-10-nov-2026). Los documentos personales, evidencias locales, modelo y PDF quedan fuera.

### Corrección de comentarios, 11-nov

Los seis grupos explican su alcance y las 24 pruebas describen el resultado esperado. Se separan fases e instrucciones sin cambiar la lógica. Pruebas comentadas según el skill del proyecto. Comprobado con el analizador de C#: solo comentarios y espacios, sin cambios en los tokens del código. Repetición final de 54/54 en Chrome, sin omitidas. Claude APROBADO v1, 11-nov, reauditoría. La guía B02 conserva su publicación documental separada y queda fuera de este PR de QA.

**Opcionales tras la reauditoría.** Rótulos antes del if y llaves en los dos casos señalados. Compilación correcta y 6/6 casos afectados de Chrome correctos, sin omitidas. Forman parte de los 54; no se suman como pruebas nuevas.
