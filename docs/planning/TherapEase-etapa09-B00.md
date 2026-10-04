# TherapEase — 09/B00 · Plan y estado de validación

**Actualización documental: 04-oct-2026.** B00 está iniciado; Elaboración/LCA y R04 continúan abiertos. No hay autorización de B01, Construcción, datos reales o despliegue.

## 1. Estado y documentación

- T1 fue aprobado y publicado el 01-oct-2026; su commit base fue `174afe0b6764f2415427f28b04d4627ce4c33b16`.
- PR [#1](https://github.com/LuciAguilar/Therapease/pull/1): B00 pasos 1–3. Revisión original en `e418010`; correcciones comprobadas en `b60eba42edeb2fa8cdbea08ad1a4e51b041c21b0`. Lucía revisó/aprobó y fusionó con Squash and merge; commit en main: `41123f2d2c115a05ff1df944457195a7f671cc17`.
- [Informe de ajustes](../reviews/TherapEase-09-B00-revision-PR01.md): alcance, cambios que deben hacer Miguel/Dulce y mejoras opcionales. No incluye explicaciones personales ni el plan de siguientes pasos.
- [Guía de equipo](../../README.md): responsabilidades, documentación y preparación del entorno. [AGENTS.md](../../AGENTS.md) es la fuente única de reglas.
- PR [#3](https://github.com/LuciAguilar/Therapease/pull/3), rama `revision/etapa09`, reúne el informe de ajustes, guía/README, B00 y plan de pruebas; actualizado sobre main tras #1 y pendiente de revisión/fusión por Lucía. La integración documental no cierra B00 ni LCA.

## 2. Responsabilidades y forma de trabajo

| Persona | Responsabilidad |
| --- | --- |
| Miguel | Solución, proyectos, carpetas internas, backend, persistencia, migraciones e Identity. Unitarias propias en cada PR. PR #1 corregido y fusionado; documenta comandos/resultados de los siguientes cambios de B00. |
| Dulce | Frontend, PageModel y unitarias propias; valida las políticas pendientes y prioridades con la usuaria. |
| Lucía | Arquitectura, seguridad, estrategia de QA, integración crítica, recorrido y evidencia final. Revisa los cambios críticos y toma la decisión de LCA. |

Cada integrante organiza sus herramientas y revisiones a su modo; la documentación aporta contexto y soporte. Las reglas de seguridad, datos, pruebas y revisión son obligatorias para todo el equipo y están en AGENTS. Las pruebas de Lucía requieren revisión de Miguel o Dulce; ninguna revisión asistida sustituye a la revisión humana.

## 3. Condiciones resueltas y propuestas de producto

- Condiciones de identidad resueltas: longitud mínima de contraseña, caducidad de sesión, comando local de primer superusuario y conservación del bloqueo al restablecer; incorporadas al AGENTS del repositorio por el PR #1 fusionado. PageModel a cargo de Dulce.
- El bloqueo PostgreSQL se conserva: nunca puede quedar una cita activa de un paciente de baja. Solo queda provisional la política para las citas existentes al dar de baja: impedir la baja o cancelar primero.
- Pago inicial «Pendiente» y citas contiguas siguen provisionales, pendientes de Dulce con la usuaria. Aprobar el PR parcial no decide esas políticas.
- No se implementa pantalla de auditoría. Q03 registra eventos y permite consulta técnica autorizada. Directorio de pacientes: vigentes por defecto y filtro de bajas; campos visibles pendientes.
- Corte B00–B04 al 04-dic-2026: propuesta que Dulce valida con la usuaria. B05–B07 dependen de prioridad y capacidad; B08 reúne evidencia realmente obtenida.

## 4. Evidencia parcial del PR #1

**Fecha:** revisión del 03–04-oct-2026. **Commit:** `e418010882f9827ece2a1500d28f27490b45c585`. Entorno local: Windows, SDK .NET 10.0.401, Docker 29.8.1 con contenedores Linux; PostgreSQL 17 desechable mediante Testcontainers 4.15.0, xUnit 2.9.3. Solo datos ficticios.

| Comprobación | Resultado registrado | Límite |
| --- | --- | --- |
| Build de siete proyectos | 0 errores y 0 advertencias | Corresponde al SHA indicado, no a futuras correcciones. |
| Unitarias del PR | 149 pasan, sin fallos ni omitidas | Integración y E2E del PR están vacíos; no cuentan como pruebas ejecutadas. |
| QA adicional con PostgreSQL/Testcontainers | 11 pasan, sin fallos ni omitidas | Borrador local fuera del repositorio, pendiente de incorporación y revisión por otra persona. No lo ejecuta actualmente la CI del repo. |
| Docker | Imagen construida, proceso UID 1654 y salud observados | Salud comprueba el proceso, no disponibilidad de base o acceso funcional. |
| HTTP | `/salud` 200, auditoría sin sesión 401, inicio público 200 sin datos privados | Comprobación local; no acredita TLS o navegación completa. |
| Límites | Inspección parcial sin infracciones encontradas | Falta prueba automática A-01 y código real de CU04. |
| NuGet directo y transitivo | Consulta sin vulnerabilidades reportadas en PR y borrador QA | Resultado de esa consulta; no equivale a auditoría completa de seguridad. |

Las 11 comprobaciones cubrieron migraciones y usuario separado; auditoría de solo inserción; cruces y dos reservas concurrentes; versiones de edición; baja/recuperación de paciente sin citas activas; último superusuario y dos desactivaciones concurrentes; bloqueo tras cinco fallos; permisos/revocación en API; y reversión del alta de usuario si falla su auditoría.

Las cookies de QA se generaron y enviaron manualmente por HTTP local: no prueban el formulario de acceso, navegador, atributos recibidos de cookie, antifalsificación o TLS. Los 15 minutos se comprobaron en configuración sin esperar su expiración. Las carreras cubren dos transacciones coordinadas, no carga ni todos los órdenes posibles. La reversión de un alta no prueba todas las operaciones. Los resultados adicionales requieren revisión/adopción de Lucía y revisión de Miguel o Dulce antes de incorporarse a su PR de pruebas.

Los comandos y salidas completos siguen conservados localmente. El PR de pruebas publicará el código y la evidencia reproducible necesarios, con datos ficticios y sin secretos. Esta actualización documental no ejecutó nuevas pruebas de código.

**Comprobación de las correcciones:** el nuevo SHA `b60eba4` cambió solo README y AGENTS; build Release de siete proyectos correcto, 0 errores/advertencias. Miguel declara 149 unitarias correctas para ese SHA; no se repitieron las pruebas de esta tabla en la actualización documental. La fusión en `41123f2` incorpora ese código, sin convertir la evidencia parcial en cierre de B00.

## 5. Pendientes ejecutables de B00

| Tema | Comprobación que falta |
| --- | --- |
| A-01 y CU04 | Prueba automática de capas/módulos, ausencia de ciclos y coordinador real fuera de Pacientes/Citas. Una carpeta vacía no demuestra el límite. |
| A-04, Docker | America/Hermosillo disponible dentro de la imagen y reproducción por otra persona. |
| Q03/Q20 | Comando autenticado sin pantalla, filtros/contenido seguro y cambio/evento atómicos en las otras operaciones. |
| Q06 | Recuperar cita agendada, carrera cita/baja y estados alternos mediante servicios, con política de producto resuelta. |
| Q07 | Aviso de conflicto sin sobrescribir y cobertura de otros registros. |
| Q08 | Interrupciones antes/después del commit, resultado incierto, reintentos y duplicados. |
| Q09 | Conversión explícita a Hermosillo desde otras zonas; distinguir fecha sin hora de instante. |
| Q10–Q11 | Respaldo cifrado, huella, restauración desechable, comparación de datos y recuperación cronometrada desde detección. |
| Identity/Q19 | Acceso mínimo con PageModel, cookies reales en navegador, salida/cambio/restablecimiento, roles/sello siguiente solicitud, expiración, antifalsificación, TLS y revisión de hashes/configuración/secretos. |
| A-06 y protección | Medir minutos, runner y duración cuando exista CI. Main con una revisión comprobado el 03-oct; protección contra secretos aún sin verificar. |
| QA y decisión | Incorporar las 11 pruebas en IntegrationTests en PR de Lucía, revisar con Miguel/Dulce y decidir LCA con evidencia completa. |

Estos pendientes no bloquean por sí solos el PR parcial #1; sí impiden declarar terminado B00. CI se exige desde B01 y el recorrido Q16 completo desde B04, conforme a AGENTS.

## 6. Siguientes pasos compartidos

1. **Completado:** Miguel corrigió descripción, AGENTS y README del PR #1; correcciones y build comprobados. Lucía revisó/aprobó y fusionó #1 en main (`41123f2`). Las políticas de producto pendientes no se resolvieron por esa aprobación.
2. **Integración documental:** PR #3 actualizado sobre ese main, con la guía y la sección técnica de Miguel íntegra. Lucía revisa y decide su fusión; no sustituye la decisión de LCA.
3. Incorporar las 11 pruebas adicionales en el PR propio de Lucía, revisado por Miguel o Dulce, y reproducir su evidencia. No acreditar una revisión aún no recibida.
4. Dulce registra las respuestas de producto obtenidas; si falta alguna, conserva el pendiente. Continuar las tareas independientes autorizadas de B00.
5. Completar las comprobaciones pendientes de §5. LCA solo se decide con resultados completos y aprobación expresa de Lucía.

## 7. Plan de validación autorizado

Los estados se indican por tarea: tener implementación o evidencia parcial no completa todas las pruebas de un paso. Las comprobaciones históricas de §4 mantienen sus límites; no se acredita una revisión aún no recibida.

1. **Base y límites — validación pendiente.**
   - **Completado:** solución, siete proyectos y aplicación Razor Pages/Docker inicial de Miguel, incorporados al PR #1 fusionado. Build correcto; arranque Docker observado en la revisión original (§4).
   - **Pendiente:** Lucía escribe y ejecuta la prueba automática de límites, incluido CU04; Miguel la revisa. Miguel implementa el coordinador CU04. Comprobar Hermosillo dentro de la imagen y reproducción por otra persona. Medir minutos A-06 cuando opere CI.
2. **Datos — validación pendiente.**
   - **Completado:** persistencia, migraciones, usuarios separados de PostgreSQL y unitarias iniciales de Miguel, incorporados al PR #1 fusionado.
   - **Pendiente; se puede adelantar sin PageModel:** Lucía incorpora y amplía las comprobaciones Q03/Q06/Q07/Q20 con PostgreSQL desechable/Testcontainers: auditoría solo de inserción, migrador separado, cruces, versiones, bajas, cambio/evento atómicos, carreras y commit. Ya hay evidencia parcial (§4); falta incorporarla al repo y la revisión de Miguel o Dulce. Las pruebas de políticas funcionales aún abiertas requieren la respuesta de producto correspondiente.
3. **Identidad y seguridad — validación pendiente.**
   - **Completado:** mecanismos iniciales de Identity de Miguel y condiciones documentales aprobadas, incorporados al PR #1 fusionado. No equivale a validación completa de Q19.
   - **Pendiente; se puede adelantar sin PageModel:** Lucía prueba cuentas ficticias y servicios de servidor: bloqueo y su expiración, contraseña temporal y restricción de acceso, revocación, cambio de rol y último superusuario concurrente; revisa secretos, hashes y configuración. Completa las comprobaciones parciales de §4 con Testcontainers y datos ficticios; Miguel o Dulce revisa sus pruebas.
   - **Pendiente de Dulce:** PageModel mínimo de acceso con sus unitarias. Después Lucía prueba el formulario real, cookies emitidas/recibidas en navegador, antifalsificación y los recorridos de salida/cambio/restablecimiento. Los atributos Secure y el transporte Q19 requieren también un entorno local HTTPS verificable; no autorizan nube. Las cookies generadas manualmente no sustituyen estas pruebas.
4. **Recuperación y decisión — pendiente.**
   - **Pendiente; se puede adelantar sin PageModel:** Lucía prueba Q10–Q11: pg_dump cifrado, huella SHA-256 y restauración en otro PostgreSQL desechable, comparación de datos y tiempos desde detección; registra resultados y fallos. Solo datos ficticios y entorno local/Testcontainers, sin repo de respaldo o conexiones de nube. Miguel o Dulce revisa sus pruebas.
   - **Pendiente de Lucía tras la evidencia completa:** decidir el cierre de B00/LCA. Completar las pruebas de recuperación no cierra LCA por sí solo ni autoriza B01 o Construcción.

## 8. Consulta Q03 y respaldo

`ConsultarEventosAutorizados` es consulta técnica de solo lectura mediante comando documentado que llama al servidor. En cada solicitud se valida sesión Identity y rol de superusuario activo; no hay acceso directo desde cliente a PostgreSQL. Filtros de periodo/registro parametrizados; salida solo actor, momento, registro/campo y hecho, sin valores clínicos, contactos o credenciales. No permite editar o borrar. Aprobada documentalmente el 01-oct; validación completa pendiente.

Respaldo: opción A resuelta, repositorio privado aparte para el respaldo diario. Conservar pg_dump cifrado, SHA-256, retención de 14 días, restauración desechable e incidencia asignada a Lucía ante fallo. La nota ADR-05 sigue como propuesta de implementación pendiente de revisión y decisión. No crear ese repo, conectar Neon ni desplegar. B00 prueba recuperación desechable sin nube.

## 9. Publicación y límites

El informe de ajustes está en `docs/reviews/`; B00 en `docs/planning/`; el plan de pruebas en `docs/testing/`; la guía es la entrada del README. Las reglas están en AGENTS y los ADR en `docs/architecture/adr/`. El StarUML aprobado de 08 antecede al cambio de pantalla; no autoriza construir la pantalla de auditoría retirada. Las skills adaptadas siguen para un PR posterior.

R04 permanece abierto y se usan únicamente datos ficticios. No iniciar B01, cerrar LCA o desplegar sin resultados y decisión expresa. Publicar documentos, aprobar el PR parcial y cerrar LCA son decisiones distintas.
