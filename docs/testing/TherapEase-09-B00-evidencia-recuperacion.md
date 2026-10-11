# TherapEase — B00 · Evidencia local de recuperación Q10–Q11

**Actualizado el 10-oct-2026 · Correcciones aplicadas y 50 pruebas repetidas: todas correctas. Revisión humana y aprobación pendientes.** Propuesta de validación, sin respaldo diario configurado, datos reales o nube. B00/LCA/R04 abiertos.

## Decisión ADR-05 registrada — 10-oct-2026

La operación real usará age con clave pública en GitHub; Lucía custodia la privada fuera de GitHub en su gestor y Miguel conserva copia. En cada trabajo diario se restaura y compara el volcado en PostgreSQL desechable antes de cifrarlo; el volcado sin cifrar nunca sale del ejecutor. Retención de 14 días como artefacto privado en el repo de respaldos. Lucía descifra/restaura/mide en un simulacro mensual. Copia de más de 24 h: restaurar igual, reportar Q10 y abrir o actualizar incidencia a Lucía. No se implementa ni configura nada en esta entrega.

| Pruebas existentes | Tratamiento decidido |
| --- | --- |
| Cifrar / Abrir | Mantienen el formato exclusivo de QA AES-256-GCM; no son implementación ni validación de age. |
| Copia alterada, clave incorrecta, aleatorio nuevo y fecha protegida | Se reemplazarán por pruebas con age al implementar el respaldo real; no se modifican ahora. |
| Volcado, restauración, comparación, fallos y antigüedad | Se conservan. |
| Resultados actuales | Los 13 casos de recuperación y la ejecución conjunta de 50 pertenecen al formato de QA. No se repitieron pruebas por esta actualización documental. |

## Correcciones aplicadas y verificadas — 08-oct-2026

- Comentarios conservados, Arrange/Act/Assert pegados al código y término «copia cifrada».
- `RestaurarAsync` ya no bloquea por antigüedad; `Antiguedad` mide y la prueba informa cumplimiento Q10. Nota propuesta ADR-05 actualizada. Integridad/clave correcta siguen siendo obligatorias.
- **50 pruebas repetidas: 50 correctas, 0 fallos y 0 omitidas**, Release sin advertencias, 19 s. Dentro del recorrido principal se restauró además una copia íntegra de 25 h en otra base vacía, se compararon sus 13 tablas y se informó «Q10 incumplido» sin bloquear la recuperación. No aumenta el número de casos.
- Opcionales conservados sin aplicar: verificar el evento HTTP, no solo el 200, y documentar `crear-roles.sql` para servidor nuevo.

## 1. Alcance e identificación

- Rama existente `revision/etapa09`, sobre main `8baf8fbc34890d4710c2b78e920f10b8d3e92cb3`. Identity quedó inicialmente guardado en `4dbee3a614e04c4086adc068151543fe81f6ab90`; recuperación corregida y ejecución conjunta están en `83328783b2ea4f7487d28e2935e3c81acb8e521b`, publicado en [PR #4](https://github.com/LuciAguilar/Therapease/pull/4), en borrador. Esta entrega reúne Identity y recuperación; la descripción del PR identifica el commit y la evidencia local conserva las huellas.
- **13 casos nuevos; 50 pasan en conjunto** con los 37 anteriores, 0 fallos/omitidas. Release sin advertencias; duración informada: 19 s. Solo pruebas/documentación, sin cambios de backend, migraciones, PageModel, dependencias de ejecución o reglas compartidas.
- SDK 10.0.401, Docker 29.8.1 Linux, PostgreSQL 17.11, Testcontainers 4.15.0, xUnit 2.9.3. Contenedores desechables, bases aisladas y datos ficticios.
- Cifrado **candidato de QA**: AES-256-GCM de .NET, clave aleatoria de 32 bytes en memoria, aleatorio nuevo de 12 bytes y autenticación de 16 bytes; fecha/versión autenticadas, SHA-256 del archivo cifrado. Formato exclusivo de QA documentado en ADR-05; el respaldo real aprobado usará age, con custodia decidida el 10-oct y todavía sin implementar.

## 2. Casos ejecutados

| Casos | Comprobación y resultado |
| --- | --- |
| 1 | `pg_dump` real, cifrado y escritura del archivo cifrado/huella; detener origen, constatar que no responde, iniciar otro contenedor y restaurar en base vacía sin migrarla. Todas las filas de las 13 tablas coinciden por huella: Identity, auditoría, claves, historial EF y demás datos. Se restaura también una copia íntegra de 25 h sin bloquearla; esa fase adicional queda fuera del cronómetro de la copia reciente. |
| 5 | Huella incorrecta, contenido cambiado, fecha cambiada, clave incorrecta y copia truncada rechazados antes de crear tablas. Recalcular SHA-256 al alterar contenido/fecha no evita el rechazo de GCM. Estos casos usan contenido ficticio pequeño, no un volcado real. |
| 1 | Conflicto al restaurar un volcado real: `pg_restore --single-transaction --exit-on-error` falla y conserva exactamente el destino previo, sin tablas parcialmente restauradas. |
| 1 | `pg_dump` de base inexistente falla, no entrega una copia válida y retira el archivo temporal. |
| 4 | Fechas controladas: se mide antigüedad en 23/24/25 horas y fecha futura; 23/24 cumplen Q10, 25/futura se informan como incumplimiento/anomalía, sin usar la medición para bloquear la restauración. No se esperó un día real. |
| 1 | Dos cifrados del mismo contenido generan aleatorios distintos, huellas correctas y recuperan los bytes originales. |

También se comprueba después de restaurar: app sin UPDATE/DELETE en auditoría ni CREATE de tablas; INSERT permitido; historial EF legible por migrador y denegado a la app; cookie anterior autenticada y consulta autorizada real de auditoría por HTTP local, 200. Los roles PostgreSQL se aprovisionan separadamente: el volcado de una base no incluye roles globales. Sus permisos de tablas sí se recuperan del volcado.

**Pruebas comentadas según el skill del proyecto.** Ajuste de espaciado y término solicitado aplicado el 08-oct; los archivos corregidos están incluidos en la nueva ejecución correcta. Descripciones de clases/pruebas, secciones, preparación/operación/comprobación y espaciado solicitado; sin jerga en comentarios propios.

## 3. Medición de esta ejecución

| Medida | Resultado |
| --- | --- |
| Respaldo, cifrado y escritura local | 458 ms |
| Detección simulada → consulta autorizada tras restauración | **7.375 ms** |
| Antigüedad de copia al detectar el fallo | 460,8682 ms |
| Archivo cifrado | 36.194 bytes |
| Tablas comparadas | 13, todas sus filas iguales al estado respaldado |

El cronómetro comienza al registrar la detección simulada, antes de detener el origen. Incluye comprobar su inaccesibilidad, iniciar destino, descifrar/restaurar, comparar y consultar con autorización. Un cambio ficticio posterior al respaldo no se recupera: se vuelve al estado respaldado. La ventana cumple 24 horas **en esta muestra**, sin acreditar un programador diario durante un día real ni tiempos a carga Q12.

## 4. Reproducción y evidencia

Desde la raíz de la solución, con .NET 10 y Docker Desktop Linux disponible:

```powershell
dotnet test tests/TherapEase.IntegrationTests/TherapEase.IntegrationTests.csproj -c Release --filter FullyQualifiedName~RecuperacionTests --logger "trx;LogFileName=recuperacion.trx" --results-directory TestResults/B00
```

Los 50 casos corresponden a la ejecución conjunta de Identity/recuperación del 08-oct. Hoy, sin filtro se incluyen también los bloques nuevos y los fallos conocidos de arquitectura/Q03; el comando anterior selecciona solo recuperación. TRX corregido del 08-oct: `b00-conjunto-50-corregido-08oct.trx`, con mediciones sin datos ni claves. Dos intentos anteriores fallaron en comprobaciones nuevas: consulta HTTP sin fechas y consulta del historial EF con usuario de app; se corrigieron las pruebas sin cambiar la aplicación ni omitir casos. Evidencia anterior conservada localmente.

Código: [casos](../../tests/TherapEase.IntegrationTests/RecuperacionTests.cs), [apoyo de cifrado/volcado/restauración](../../tests/TherapEase.IntegrationTests/RecuperacionRevision.cs).

Volcados sin cifrar: solo temporales dentro del contenedor, modo 0600 comprobado; el descifrado de restauración usa 0600. Se retiran en `finally`. En el equipo solo se escriben copia cifrada/huella en una carpeta temporal del caso, también eliminada al finalizar. La clave no se guarda/imprime y se limpia de memoria. Comandos sin contraseñas, mediante conexión local del contenedor ficticio; no acredita TLS/autenticación de producción. Volcado/restauración cancelables a 30 s.

## 5. Pendientes y límites

- Claude emitió CORREGIR menor; correcciones aplicadas y pruebas repetidas correctamente. Miguel o Dulce revisa las pruebas; Lucía aprueba y decide publicación.
- Clave efímera solo de QA. Custodia de la privada age decidida el 10-oct; generación, entrega, comprobación de custodia y rotación pendientes cuando se autorice. SHA-256 por sí solo no autentica frente a quien cambie archivo y huella.
- Repositorio privado, ejecución diaria con validación del volcado antes de cifrar, retención 14 días, incidencia automática, acceso privado y simulacro mensual pendientes de implementación/ejecución; no se crean servicios o conexiones de nube.
- Se restaura también el estado de Identity de la copia. La cookie anterior se conserva deliberadamente para verificar las claves; revisar la política de revocación de sesiones tras un incidente real antes de usar datos reales. No acredita S-31 (reinicio del contenedor de app), navegador, antifalsificación o HTTPS/Q19.
- Muestra pequeña: un paciente, una cita, un superusuario, un evento y tablas auxiliares. No acredita carga Q12, caída de proveedor, detección automática, transporte seguro o pérdida máxima diaria en operación real.
- Q10–Q11 conservan revisión, reproducción y operación pendientes. Sin cierre B00/LCA ni autorización B01/Construcción.

Referencias primarias: [pg_dump 17](https://www.postgresql.org/docs/17/app-pgdump.html), [pg_restore 17](https://www.postgresql.org/docs/17/app-pgrestore.html), [Testcontainers](https://dotnet.testcontainers.org/api/create_docker_container/), [AES-GCM .NET](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.aesgcm?view=net-10.0).
