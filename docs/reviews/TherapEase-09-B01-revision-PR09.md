# TherapEase — B01 · Revisión del PR #9

**25-oct-2026 · PR #9 revisado y fusionado.** Claude emitió APROBADO v3 sobre `28f52df`, sin correcciones obligatorias. Lucía aprobó en GitHub y fusionó con squash en **`6526607fbd59ffda966093d65f04467da1a959ad`**. Los cuatro trabajos de CI de `28f52df` fueron correctos. Este informe conserva abajo las comprobaciones de cada versión.

## Cierre de esta revisión y siguiente entrega

Claude informó sobre `28f52df`: compilación sin advertencias; doce casos de horas correctos con una entrada de control a las 17:30 UTC; sin esa entrada, 422 unitarias y 319 de integración con dos hilos. Son resultados de su auditoría, no nuevas ejecuciones de Codex. No pidió correcciones obligatorias.

PR #10 se conserva como borrador para incorporar las diez pruebas propias de eventos. Lucía aprobó combinar las reglas actuales de main, el avance de su QA, los pasos de HTTPS de Miguel y la redacción sencilla del plan. Preparación local, sin commit ni push hasta auditoría y su aprobación. Revisión humana de Miguel o Dulce antes de fusionar #10 con Create a merge commit. La mejora de separar identificadores en `HttpsLocalRevision` sigue opcional y sin aplicar.

Para Miguel: PR #9 ya está fusionado. Leer AGENTS §3 y la descripción del PR #9 en `28f52df`. No quedan correcciones obligatorias de esta revisión. Se propone retirar `b00/correcciones-pr4` y usar una rama nueva desde main para la propuesta B02; mientras conserve la anterior, hacer `git pull` antes de modificarla porque contiene un commit de Lucía. El QA de #10 sigue local, aún no disponible para su revisión.

## Antecedente: revisión final de 28f52df

## 1. Versión revisada y qué cambió

En la revisión previa a la fusión, [PR #9](https://github.com/LuciAguilar/Therapease/pull/9) estaba en **`26555176f05d936eeab2cb8c08ddc037dcb80741`**, frente a main `b862d99`. Desde `a9bb313` cambiaban solo **tres archivos**: la preparación `HorasDockerRevision.cs`, la propuesta y el plan de pruebas. El arreglo es funcionalmente igual al parche auditado; solo cambian su ubicación y comentario. No cambia servidor, `HorasDockerTests`, comprobaciones, fechas, paquetes, migraciones ni CI.

Actualización autorizada por Lucía: el PR llega ahora a **`28f52dfc4f600827f5fce3e3486d8fe220665947`**. Frente a `26555176` cambia únicamente una fila del plan de pruebas; también se corrigió el título real en GitHub. Código y pruebas idénticos. [Nueva CI 40244671671](https://github.com/LuciAguilar/Therapease/actions/runs/40244671671): cuatro trabajos correctos, verificados por API; los conteos detallados de abajo corresponden a los registros de `26555176`.

✅ Corrigió las fechas del 17/21-oct, las decisiones sobre actor y error de entrada, y los principales estados de sus tablas. La aprobación del código del 24-oct se conserva como antecedente; no aprueba esta actualización ni permite ignorar la CI actual.

## 2. Antecedente: por qué fallaron cuatro pruebas

La [CI anterior 40217174330](https://github.com/LuciAguilar/Therapease/actions/runs/40217174330), sobre `a9bb313`, compiló y superó unitarias, controles e imagen. Integración: **315 correctas, 4 fallidas, 0 omitidas; 319 en total**. Este resultado es histórico; no describe la versión nueva.

`HorasDockerRevision` usa el mismo identificador para el usuario de prueba y el paciente ficticio. Las consultas buscan ese identificador entre **2026-10-25 17:00 y 18:00 UTC**. El login real de la preparación ocurrió a las **17:36:55 UTC** y dejó `Usuario / InicioSesion`; la semilla ya tenía `Paciente / Alta` a las 17:00.

Son **dos eventos distintos**, con identificadores de evento diferentes. La API cumple el filtro pedido; la prueba esperaba uno y recibe ambos. Antes pasaba porque se ejecutaba fuera de esa franja. El análisis no demuestra duplicación de auditoría ni un defecto de la consulta.

| Estado | Ajuste necesario | Qué conservar |
| --- | --- | --- |
| ✅ | En `HorasDockerRevision`, identificador propio para el usuario; se usa en `CrearAsync` y como actor de la semilla. | Incorporado en `26555176`. Conserva los doce casos, las variantes horarias, los bordes, el reinicio y `Assert.Single`; API intacta. |
| ✅ | CI nueva y explicación de la reproducción | [40238140968](https://github.com/LuciAguilar/Therapease/actions/runs/40238140968): cuatro trabajos correctos sobre `26555176`. Descripción del PR distingue fallo anterior y ejecución final; aclara que salir verde fuera de la franja no demuestra por sí solo el arreglo. |

## 3. Qué se comprobó y a qué versión corresponde

| Estado | Comprobación | Resultado / alcance |
| --- | --- | --- |
| ✅ | Reproducción controlada | Se sitúa una entrada ficticia a las 17:30 en la base de prueba: **8 correctas y los mismos 4 fallos** entre los doce casos de horas. No se cambió el reloj del equipo. |
| ✅ | Propuesta con la misma entrada dentro de la franja | Separando los identificadores: **12 correctas, 0 fallidas y 0 omitidas**. Las comprobaciones de `HorasDockerTests` permanecen iguales. |
| ✅ | Integración completa con el ajuste mínimo, sin la entrada añadida para reproducir | **319 correctas, 0 fallidas y 0 omitidas**; 2 min 32 s informados. Se excluyen solo los diez casos privados que no pertenecen al PR. |

Las tres filas anteriores son la comprobación local anterior del parche sobre la base `a9bb313`; no se cuentan como ejecuciones nuevas de `26555176`.

✅ Claude confirmó por separado sobre una exportación de `a9bb313`: reproducción **8/4**, parche con la misma entrada **12/12**, integración **319/319** con dos hilos y unitarias **422/422**. Confirmó que la copia de Codex es el PR más el parche y que la API está bien. Son resultados de su veredicto, no ejecuciones nuevas de Codex.

✅ **CI de `26555176`, registros leídos:** 422 unitarias, 30 de arquitectura y 319 de integración correctas, sin fallidas ni omitidas. Las 30 de arquitectura también están incluidas en las 319; no se suman dos veces. Compilaciones sin advertencias ni errores.

✅ **Comprobación local nueva de `26555176`: 12 correctas, 0 fallidas y 0 omitidas**, 10 s informados. Se usó su archivo exacto, verificado por su huella de Git; no se reinsertó la entrada temporal. Esta repetición fuera de la franja complementa la reproducción controlada auditada; no la sustituye.

## 4. Documentos y título corregidos

| Estado | Dónde | Corrección |
| --- | --- | --- |
| ✅ | Título real del PR | «B01: eventos de entrada, salida y bloqueo con registro técnico», corregido por Codex con autorización puntual de Lucía y verificado en GitHub. |
| ✅ | Plan de pruebas §0.8 | Fila actualizada en `28f52df`: ejecución 40238140968 correcta sobre `26555176`, con enlace, cantidades y auditoría/fusión pendientes. §0.8 significa apartado 0.8 de `docs/testing/TherapEase-09-plan-de-pruebas.md`, en la rama de este PR. |
| ✅ | Descripción del PR | Incluye 40217174330 fallida, 40238140968 correcta y explicación de reproducción. Conserva el fallo local por tiempo de espera y su repetición de 36 casos como resultados propios de Miguel. No los presenta como una ejecución local completa de 319 correctas. |
| ✅ | Inventario de la propuesta, sección 1 | Permisos explícitos entregados en #7; main ya tiene flujo; fila CI con ✅. También aclaró los pendientes históricos de §1.3. |

## 5. Para seguir

Los dos textos y la CI de `28f52df` quedaron comprobados; Miguel no necesita repetirlos. Claude aprobó v3 y Lucía completó la aprobación y fusión con squash. El siguiente paso es la entrega propia del mismo #10, como se indica al inicio.

**Opcional para #10:** `HttpsLocalRevision` comparte el mismo patrón de identificador; hoy su franja del 10-oct evita este fallo. Proponer separarlos junto a los diez casos; no aplicarlo como cambio aprobado por recibir este veredicto.

Con la autorización puntual se publicó solo esa fila en la rama existente de #9, mediante push normal, y se editó el título. No se publicó código, pruebas ni notas personales; no se comentó, aprobó ni fusionó el PR. La rama global y main quedaron intactos. Los diez casos propios de Lucía siguen destinados al #10 después de #9, con revisión humana y Create a merge commit. Esta revisión no cierra B01/B00/LCA/R04 ni cambia los límites de B02.
