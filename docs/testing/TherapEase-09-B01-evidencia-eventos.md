# TherapEase — B01 · Pruebas de eventos de acceso

**Actualizado el 26-oct-2026 · QA revisado y fusionado en PR #10.** Auditoría y precisión de texto aprobadas; Miguel (`GalloNav`) aprobó `3a44332` y Lucía fusionó con Create a merge commit en `a7c0c52`, a las 13:23:52 de Hermosillo. Última CI de ese SHA: 40393607611, cuatro trabajos correctos. Este resultado no cierra B01; faltan páginas y navegador.

## 1. Qué se comprueba

`EventosAccesoServidorTests` contiene **diez casos** con PostgreSQL desechable. Usa los servicios de la aplicación, sin páginas nuevas y solo con datos ficticios.

| Casos | Qué debe pasar |
| --- | --- |
| 1 | Entrar guarda su evento y reinicia los cuatro fallos anteriores antes de entregar una sesión válida. |
| 1 | Si falla guardar la entrada, conserva los fallos anteriores y no entrega cookie. |
| 1 | Salir guarda su evento y la cookie anterior deja de servir. |
| 1 | Si falla el evento de salida, la cookie sigue revocada y queda un aviso técnico fijo. |
| 3 | Catorce intentos simultáneos generan un solo bloqueo y un solo evento; se comprueba en tres bases nuevas. |
| 1 | Si falla el evento del bloqueo, este sigue vigente y queda un único aviso técnico. |
| 2 | Cinco fallos de la contraseña actual bloquean nuevos accesos y conservan la sesión abierta y la contraseña anterior, tanto si se guarda el evento como si falla. |

Se comprueban actor, registro, tipo, texto fijo y ausencia de campos privados en los eventos; nivel, identificador y ausencia de excepción adjunta en los avisos técnicos. Estos avisos no guardan los mensajes recibidos.

## 2. Versión y cambios

Base integrada: main **`6526607fbd59ffda966093d65f04467da1a959ad`**, squash del PR #9. Rama global `revision/etapa09`, sincronizada mediante `160aa72345e0f923993d3adcef1c1fa26cdb1d05`. El SHA de esta entrega y su CI se identifican en los commits del [PR #10](https://github.com/LuciAguilar/Therapease/pull/10). Se incorporan los diez casos auditados con los ajustes aprobados en la auditoría (nombre de clase, comentarios y señal de arranque simultáneo); sus comprobaciones no cambian. Las ayudas existentes de `ServidorIdentidadRevision` son suficientes.

Frente a main, esta entrega no modifica código del servidor ni pruebas existentes. Combina documentación aprobada de ambas ramas. `HttpsLocalRevision` sigue sin modificar; separar sus identificadores es una mejora opcional pendiente de decisión.

## 3. Resultado de esta ejecución

| Estado | Comprobación | Resultado |
| --- | --- | --- |
| ✅ | Compilación Release, advertencias como errores | 0 advertencias y 0 errores. |
| ✅ | Unitarias | 422 correctas, 0 fallidas y 0 omitidas. |
| ✅ | Eventos y concurrencia, tres ejecuciones nuevas | 15 correctas, 0 fallidas y 0 omitidas en cada una: diez casos de eventos y cinco de concurrencia. |
| ✅ | Integración completa con dos hilos | 329 correctas, 0 fallidas y 0 omitidas; incluye los diez casos incorporados. Duración informada: 2 min 25 s. |

Pruebas comentadas según el skill del proyecto. Las repeticiones no se suman como casos distintos. No se ejecutaron pruebas de navegador ni se probaron formularios de Dulce. La CI de GitHub se comprobará después de la publicación autorizada.

```powershell
dotnet build TherapEase.sln -c Release --no-restore -warnaserror -m:1 -p:UseSharedCompilation=false
dotnet test tests/TherapEase.UnitTests -c Release --no-build --no-restore
dotnet test tests/TherapEase.IntegrationTests -c Release --no-build --no-restore --filter "FullyQualifiedName~EventosAccesoServidorTests|FullyQualifiedName~ConcurrenciaServidorTests" -- xUnit.MaxParallelThreads=2
dotnet test tests/TherapEase.IntegrationTests -c Release --no-build --no-restore -- xUnit.MaxParallelThreads=2
```

## 4. Para terminar esta entrega

Claude auditó la combinación y pidió la precisión de texto ya aplicada. Lucía aprobó y autorizó la publicación; Miguel revisó `3a44332` y dejó Approve. Lucía fusionó el mismo PR #10 el 26-oct mediante **Create a merge commit** en `a7c0c52`. CI 40393607611 correcta en cuatro trabajos. Las ejecuciones de la sección 3 conservan su fecha/versión; no son pruebas nuevas por esta actualización. Páginas, formularios, navegador y cierre completo de B01 siguen pendientes.
