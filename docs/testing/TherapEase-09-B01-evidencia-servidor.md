# TherapEase — B01 · Pruebas del servidor

**15-oct-2026 · Ejecución local correcta; revisión humana y CI pendientes.**

Se comprueba el código existente de usuarios y acceso, sin esperar las páginas. Son **35 casos nuevos** y **42 casos existentes repetidos**: **77 correctos, 0 fallidos y 0 omitidos** en la versión de entrega. No se modificaron servicios del sistema, páginas, migraciones, paquetes ni permisos.

## 1. Versión y entorno

| Dato | Registro |
| --- | --- |
| Base | `main`, `ffb2494ad9da026f24a57826c45f6f530afb467b`, squash de PR #6. |
| Rama | `revision/etapa09`. La versión probada añade los cuatro archivos de pruebas B01 a esa base. |
| Entorno | Windows; SDK .NET 10.0.402; Docker Desktop Linux 29.8.1; imagen `postgres:17`; Testcontainers 4.15.0; xUnit 2.9.3. |
| Base de datos | Desechable, nueva por caso y con migraciones reales; usuarios separados de aplicación y migrador. Solo datos ficticios. |
| Fecha | 15-oct-2026, America/Hermosillo, UTC−07:00. Los TRX conservan inicio y fin exactos. |
| Evidencia local | Carpeta `tmp/b01-servidor-15oct/evidencia/` del espacio documental de Lucía: TRX, JSON de interrupciones y `manifest.json`. No se publican secretos ni estos archivos locales. |

El manifest identifica el SHA de la base, las huellas de los cuatro archivos nuevos y los resultados. El SHA del commit de entrega se registra en el PR. Las versiones preliminares fallidas se conservan como antecedentes; sus archivos de código fueron corregidos y no se presentan como la versión final.

## 2. Qué se añadió

| Estado | Casos nuevos | Qué comprueban | Referencias del plan |
| --- | --- | --- | --- |
| ✅ | 11 altas inválidas | Nombres vacíos, de más de 50 caracteres, no admitidos o con entradas manipuladas; roles desconocidos. Respuesta de datos inválidos, sin usuario, rol ni evento nuevos. | M-02; CU02 A2–A4; S-18, solo esta entrada de usuario. |
| ✅ | 2 altas válidas | Ambos roles actuales, nombre de 50 caracteres y espacios exteriores. Cuenta activa, rol correcto y temporal válida que debe cambiarse. | M-02; L-CU02-01, parte del servidor. |
| ✅ | 2 nombres duplicados | Mayúsculas y espacios no permiten repetir un nombre, aunque la cuenta anterior esté inactiva. No se modifica la anterior. | L-CU02-02; CU02 A2. |
| ✅ | 8 operaciones rechazadas | Objetivo inexistente en tres operaciones, tres cambios de rol inválidos/repetidos y dos operaciones sobre cuenta inactiva. Sin cambios guardados. | CU02 A2–A4; L-CU02-07, parte de rechazos. |
| ✅ | 1 fallo de auditoría en alta | Si PostgreSQL rechaza el evento, el usuario y su rol también se deshacen. Se lee desde otra conexión. | M-03; L-CU02-07; Q03/Q08. |
| ✅ | 3 cambios simultáneos | Los dos últimos Superusuarios modifican al otro: desactivar/desactivar, rol/rol y desactivar/rol. Ambos ven inicialmente dos activos; solo un cambio y su evento se guardan. El rechazado conserva sus datos y rol. | M-01; L-CU02-05. |
| ✅ | 2 comprobaciones de bloqueo | Veinte fallos simultáneos bloquean solo la cuenta afectada; un acceso correcto reinicia cuatro fallos y se necesitan cinco nuevos. | S-01/S-03; CU01 A2–A3. |
| ✅ | 6 eventos confirmados | Alta, rol, desactivación, restablecimiento y cambios propios ordinario/temporal. Actor, cuenta, hora, acción y campos correctos; sin contraseñas, valores de comprobación de contraseña, cookie ni conexión en evento o registros capturados. | M-05; L-CU02-07; L-CU11-03; L-Q03-01/02. |

Archivos en `tests/TherapEase.IntegrationTests/`: `UsuariosServidorB01Tests.cs`, `ConcurrenciaServidorB01Tests.cs`, `AuditoriaServidorB01Tests.cs` y la ayuda `ServidorIdentidadB01Revision.cs`. Las ayudas preparan solicitudes y controlan la concurrencia únicamente dentro de pruebas. No añaden rutas ni métodos para evitar la autenticación del sistema.

**Pruebas comentadas según el skill del proyecto:** español sencillo, descripción de clases y pruebas, secciones y Arrange/Act/Assert.

## 3. Resultados conservados

| Estado | Archivo local | Correctas / fallidas / omitidas | Interpretación |
| --- | --- | --- | --- |
| ⏳ | `b01-servidor-15oct.trx` | 20 / 15 / 0 | Preparación inicial incorrecta: una ayuda perdía el usuario autenticado; también se usó incorrectamente una comprobación de cabeceras. No se atribuye al backend. |
| ⏳ | `b01-servidor-corregido-15oct.trx` | 30 / 5 / 0 | Faltaba cerrar una comilla en una consulta de las pruebas. Se corrigió; no cambió el sistema. |
| ✅ | `b01-servidor-final-15oct.trx` | 35 / 0 / 0 | Primera ejecución correcta de los casos nuevos, antes de precisar el caso de cambios mutuos. |
| ✅ | `b01-servidor-conjunto-15oct.trx` | 77 / 0 / 0 | Primera ejecución conjunta; los 42 existentes son una repetición nueva, no resultados copiados. |
| ✅ | `b01-servidor-entrega-15oct.trx` | **77 / 0 / 0** | Versión de entrega: cambios mutuos y búsqueda de valores de comprobación de contraseña anteriores y posteriores. Duración informada por el ejecutor: **15 s**; compilación Release sin advertencias informadas. |

Los dos resultados fallidos conservan el antecedente de correcciones ya aplicadas a las pruebas. No representan defectos pendientes del servidor ni se cuentan como aprobados. Los casos correctos iniciales tampoco sustituyen la ejecución final.

## 4. Cómo repetirlo

Desde la raíz del repositorio, con Docker disponible y el SDK indicado por `global.json`. Se usan las mismas dependencias existentes. En PowerShell:

```powershell
$env:THERAPEASE_EVIDENCIA_Q08 = Join-Path (Get-Location) 'TestResults/B01/servidor/interrupciones'
dotnet test tests/TherapEase.IntegrationTests/TherapEase.IntegrationTests.csproj -c Release --filter 'FullyQualifiedName~ServidorB01Tests|FullyQualifiedName~IdentidadServidorTests|FullyQualifiedName~IdentidadComplementariaTests|FullyQualifiedName~InterrupcionesIdentidadTests' --logger 'trx;LogFileName=b01-servidor.trx' --results-directory TestResults/B01/servidor
```

La ejecución registrada usó además `--no-restore`, porque las dependencias ya estaban descargadas. Para ejecutar únicamente los 35 nuevos, usar el filtro `FullyQualifiedName~ServidorB01Tests`.

## 5. Qué sigue pendiente

| Estado | Pendiente | Responsable / dependencia |
| --- | --- | --- |
| ⏳ | Revisar estas pruebas y repetirlas en otra computadora. | Miguel o Dulce, persona distinta de la autora; aprobación final de Lucía. |
| ⏳ | Comprobaciones automáticas en GitHub. | Miguel. Esta ejecución local no acredita CI. |
| ⏳ | Consulta mínima CU02, nombre/correo, eventos nuevos de acceso/salida/bloqueo/cierre forzado y permisos explícitos del Superusuario. | Miguel presenta el detalle; Lucía aprueba antes de programar. No se implementan aquí. |
| ⏳ | Ampliaciones propias de permisos, sesiones y contraseñas que no cubra lo existente. | Siguientes entregas de QA según el checklist de Lucía; no se dan por completas por esta entrega de servidor. |
| ⏳ | Formularios, antifalsificación, temporales en respuestas web y recorrido de Q03 autenticado en navegador. | Páginas y PageModel de Dulce; pruebas posteriores de Lucía. |
| ⏳ | CSS con sesión y tiempos de espera del primer arranque observados en PR #6. | Seguimiento conservado; estas pruebas no vuelven a comprobar esos casos HTTP. |

Esta muestra de concurrencia cubre los escenarios indicados; no demuestra cualquier volumen o combinación posible. No hay pruebas de navegador, carga Q12, revisión nueva de paquetes ni despliegue. Temporal de 24 horas sigue exigida antes de datos reales, sin implementarse aquí. **B01 y B00/LCA/R04 continúan abiertos.**
