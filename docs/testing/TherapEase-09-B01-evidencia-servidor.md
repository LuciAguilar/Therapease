# TherapEase — B01 · Pruebas del servidor

**Estado al 20-oct-2026:** entrega aprobada por Lucía, revisada por Miguel y fusionada en PR #8 (`7a73e0a`); CI correcta. Las pruebas finales siguen fechadas el 19-oct; no se ejecutaron de nuevo por actualizar el informe.

Se comprueban los servicios existentes de usuarios, acceso y contraseñas con PostgreSQL desechable, sin páginas. La ejecución final reúne **44 casos propios y 42 existentes: 86 correctos, 0 fallidos y 0 omitidos**. Se usaron los nombres finales y se conservaron intactos los comentarios corregidos por Claude, incluido `// Act + Assert` en el bloque que ejecuta cambios y comprueba sus respuestas.

✅ significa comprobación local ejecutada para el alcance indicado; Lucía aprobó esta entrega y autorizó su commit local el 18-oct. La revisión humana y CI de PR #8 están completas; no equivale al cierre de B01.

## 1. Versión y entorno

| Dato | Registro |
| --- | --- |
| Aplicación | `922a4e32705c50fa7b559bd95ad19f86800bcba2`, squash de PR #7 aprobado por Lucía. |
| Referencia de trabajo | `revision/etapa09` sobre `bbf7aed`, refactorización aprobada, con ajustes autorizados de Contraseñas. |
| Entorno | Windows; SDK .NET 10.0.402; Docker Desktop Linux 29.8.1; imagen `postgres:17`; Testcontainers 4.15.0; xUnit 2.9.3. |
| Base de datos | Nueva por caso, con migraciones reales y usuarios separados de aplicación y migrador. Solo datos ficticios. |
| Inicio y fin del resultado | `2026-10-19T20:32:36.8924245-07:00` → `2026-10-19T20:33:13.4220040-07:00`. |
| Archivos del resultado | `servidor.trx` y `manifest-final.json`; el manifest identifica fuentes, huellas y contadores. |

El [PR #7](https://github.com/LuciAguilar/Therapease/pull/7) se fusionó el 18-oct sin los cuatro archivos antiguos de Lucía ni su informe. Se actualizó su rama conservando los cambios locales y se adaptaron cinco llamadas de alta al nombre completo, además de los campos del evento. Se incorporaron al mismo proyecto las ocho comprobaciones de consultas y edición que se usaron durante la revisión del PR. Se agregó el caso de cambio solo de correo solicitado en la auditoría. No se cambiaron servicios de producción, migraciones, permisos ni paquetes.

## 2. Qué se añadió

| Estado | Casos nuevos | Qué comprueban | Referencias del plan |
| --- | --- | --- | --- |
| ✅ | 11 altas inválidas | Nombres vacíos, de más de 50 caracteres, no admitidos o con entradas manipuladas; roles desconocidos. Respuesta de datos inválidos, sin usuario, rol ni evento nuevos. | M-02; CU02 A2–A4; S-18, solo esta entrada de usuario. |
| ✅ | 2 altas válidas | Ambos roles actuales, nombre de 50 caracteres y espacios exteriores. Usuario activo, rol correcto y temporal válida que debe cambiarse. | M-02; L-CU02-01, parte del servidor. |
| ✅ | 2 nombres duplicados | Mayúsculas y espacios no permiten repetir un nombre, aunque el usuario anterior esté inactivo. No se modifica al usuario existente. | L-CU02-02; CU02 A2. |
| ✅ | 8 operaciones rechazadas | Objetivo inexistente en tres operaciones, tres cambios de rol inválidos/repetidos y dos operaciones sobre usuario inactivo. Sin cambios guardados. | CU02 A2–A4; L-CU02-07, parte de rechazos. |
| ✅ | 1 fallo de auditoría en alta | Si PostgreSQL rechaza el evento, el usuario y su rol también se deshacen. Se lee desde otra conexión. | M-03; L-CU02-07; Q03/Q08. |
| ✅ | 3 cambios simultáneos | Los dos últimos Superusuarios modifican al otro: desactivar/desactivar, rol/rol y desactivar/rol. Ambos ven inicialmente dos activos; solo un cambio y su evento se guardan. El rechazado conserva sus datos y rol. | M-01; L-CU02-05. |
| ✅ | 2 comprobaciones de bloqueo | Veinte fallos simultáneos bloquean solo al usuario afectado; un acceso correcto reinicia cuatro fallos y se necesitan cinco nuevos. | S-01/S-03; CU01 A2–A3. |
| ✅ | 6 eventos confirmados | Alta, rol, desactivación, restablecimiento y cambios propios ordinario/temporal. Actor, usuario, hora, acción y campos correctos; sin contraseñas, valores de comprobación de contraseña, cookie ni conexión en evento o registros capturados. | M-05; L-CU02-07; L-CU11-03; L-Q03-01/02. |
| ✅ | 9 casos de consultas y edición | Lista ordenada y límite de cien, búsqueda de texto, alta con nombre/correo, datos propios, edición que conserva sesiones, operación repetida sin evento nuevo, cambio solo de correo que registra únicamente `Correo`, fallo de evento que revierte datos y reversión/reaplicación de la migración. PostgreSQL e Identity reales, datos ficticios. | CU02/CU11; M-02/M-03/M-05; Q03/Q08, solo los casos indicados. |

Archivos en `tests/TherapEase.IntegrationTests/`: `UsuariosServidorTests.cs`, `ConcurrenciaServidorTests.cs`, `AuditoriaServidorTests.cs`, `ConsultaYEdicionUsuariosTests.cs` y la ayuda `ServidorIdentidadRevision.cs`. Las ayudas preparan solicitudes y controlan la concurrencia únicamente dentro de pruebas. No añaden rutas ni métodos para evitar la autenticación del sistema.

**Pruebas comentadas según el skill del proyecto:** español sencillo, descripción de clases y pruebas, secciones y Arrange/Act/Assert.

## 3. Resultado final

| Estado | Correctas | Fallidas | Omitidas | Duración informada |
| --- | --- | --- | --- | --- |
| ✅ | **86** | **0** | **0** | **19 s** |

Compilación Release sin advertencias informadas. Resultado: `servidor.trx`. SHA-256: `572024f228a981af0db5bea1806bdda57b364728a2ebec54f0be83c4af0d62de`.

## 4. Cómo repetirlo

Desde la raíz del repositorio, con Docker disponible y el SDK indicado por `global.json`. Se usan las mismas dependencias existentes. En PowerShell:

```powershell
$env:THERAPEASE_EVIDENCIA_Q08 = Join-Path (Get-Location) 'TestResults/servidor/interrupciones'
dotnet test tests/TherapEase.IntegrationTests/TherapEase.IntegrationTests.csproj -c Release --filter 'FullyQualifiedName~UsuariosServidorTests|FullyQualifiedName~ConcurrenciaServidorTests|FullyQualifiedName~AuditoriaServidorTests|FullyQualifiedName~IdentidadServidorTests|FullyQualifiedName~IdentidadComplementariaTests|FullyQualifiedName~InterrupcionesIdentidadTests|FullyQualifiedName~ConsultaYEdicionUsuariosTests' --logger 'trx;LogFileName=servidor.trx' --results-directory TestResults/servidor
```

La ejecución registrada usó `--no-restore` porque las dependencias ya estaban descargadas y la variable `THERAPEASE_EVIDENCIA_Q08` para guardar los resúmenes de interrupciones. No se incorporan dependencias nuevas. Para ejecutar únicamente los 44 propios de esta entrega, usar el filtro `FullyQualifiedName~UsuariosServidorTests|FullyQualifiedName~ConcurrenciaServidorTests|FullyQualifiedName~AuditoriaServidorTests|FullyQualifiedName~ConsultaYEdicionUsuariosTests`.

## 5. Qué sigue pendiente

| Estado | Pendiente | Responsable / dependencia |
| --- | --- | --- |
| ✅ | Aprobar la entrega y revisar sus pruebas. | ✅ Lucía aprobó Entrega 1 con 86 correctas y su commit local. ✅ Miguel aprobó `95c9425`; Lucía fusionó PR #8 el 20-oct. |
| ✅ | Comprobaciones automáticas en GitHub. | ✅ CI de Miguel revisada en PR #7. ✅ Los cuatro trabajos de CI de PR #8 terminaron correctamente. |
| ✅ | Adaptación al PR #7 fusionado. | ✅ Rama actualizada y 86 pruebas correctas. ✅ Correcciones de auditoría resueltas y aprobación de Lucía. ✅ Revisión humana de Miguel completada en PR #8. |
| ✅ / ⏳ | QA de permisos, sesiones y contraseñas. | ✅ Cuatro entregas revisadas e incorporadas en PR #8. ⏳ Páginas/navegador y nuevos eventos no quedan cubiertos por estas pruebas. |
| ⏳ | Formularios, antifalsificación, temporales en respuestas web y recorrido de Q03 autenticado en navegador. | Páginas y PageModel de Dulce; pruebas posteriores de Lucía. |
| ⏳ | CSS con sesión y tiempos de espera del primer arranque observados en PR #6. | Seguimiento conservado; estas pruebas no vuelven a comprobar esos casos HTTP. |

Esta muestra de concurrencia cubre los escenarios indicados; no demuestra cualquier volumen o combinación posible. No hay pruebas de navegador, carga Q12, revisión nueva de paquetes ni despliegue. Temporal de 24 horas sigue exigida antes de datos reales, sin implementarse aquí. **B01 y B00/LCA/R04 continúan abiertos.**

## 6. Revisión de Miguel y observaciones — 20-oct-2026

✅ [PR #8](https://github.com/LuciAguilar/Therapease/pull/8) revisado y aprobado por Miguel (`GalloNav`) sobre `95c9425`, y fusionado por Lucía el 20-oct-2026, 17:29 de Hermosillo, mediante **Create a merge commit**, en `7a73e0a2282155273cd7d29dd9fea1c7e30635fd`. Los cinco commits se conservan. [CI del PR](https://github.com/LuciAguilar/Therapease/actions/runs/39512430706): cuatro trabajos correctos.

⏳ Mejora no bloqueante: `Migracion_Revierte_Y_Reaplica_Sin_Perder_Usuario` usa `20261003202126_IdentidadYAcceso` como destino. Revisar la referencia si cambia la migración; código sin cambios.
