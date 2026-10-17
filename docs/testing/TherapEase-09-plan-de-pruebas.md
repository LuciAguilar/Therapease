# TherapEase — Plan de pruebas por caso de uso

**Publicación documental autorizada, 17-oct-2026:** este plan incorpora las decisiones aprobadas y el cierre de revisión del PR #6. La ampliación de pruebas de servidor B01 sigue pendiente de auditoría y aprobación de Lucía; sus resultados no se añaden al avance compartido por esta publicación. Las casillas se actualizan al cerrar las revisiones y aprobaciones, conservando la evidencia original.

> **Entrevista recibida y revisada el 14-oct-2026:** ✅ respuestas de la psicóloga registradas en [respuestas y ajustes](../requirements/TherapEase-09-entrevista-requisitos.md). ⏳ Montos, simplificación a solo particular y cancelación automática requieren decisión de alcance/mecanismo de Lucía; no están implementados por esta actualización. B00/LCA/R04 abiertos, preparación documental sin Construcción ni pruebas nuevas. Los resultados y aprobaciones anteriores se conservan como historia.

**Actualización: 11-oct-2026.** Referencia de pruebas de 09 basada en HU/CU y alternos de 08B, modelos de 08C, trazabilidad de 08D, reglas 07B–07C y Q01–Q20. Usar únicamente datos ficticios. Las reglas compartidas están en [AGENTS.md](../../AGENTS.md); las herramientas y organización de trabajo de cada integrante son libres, las reglas de seguridad/datos/pruebas/revisión son obligatorias.

PageModel a cargo de Dulce con sus unitarias. Las cuatro condiciones de Identity están resueltas e indicadas en el informe de ajustes; Miguel las incorporó a AGENTS mediante el PR #1 ya fusionado. Las 11 comprobaciones anteriores están incorporadas y ampliadas a 37 casos de Identity, repetidos el 07-oct. Con 13 casos de recuperación, la ejecución anterior registró **50 correctos**, sin fallos/omitidas. Claude emitió **CORREGIR menor** para recuperación; ajustes de código aplicados y los 50 casos repetidos correctamente el 08-oct, 19 s, sin fallos/omitidas. Entrega conjunta publicada en [PR #4](https://github.com/LuciAguilar/Therapease/pull/4), con ampliaciones en el mismo PR; revisado por Miguel y fusionado el 11-oct; el alcance nuevo del PR #5 se registra debajo. [Recuperación y límites](TherapEase-09-B00-evidencia-recuperacion.md). Alcance y límites en [evidencia Identity](TherapEase-09-B00-evidencia-identidad.md). Sus resultados y límites están en [B00](../planning/TherapEase-etapa09-B00.md). Este plan no acredita por sí mismo ejecución, revisión ni cierre B00/LCA; la evidencia identifica lo realmente ejecutado.

**Estado de revisión actualizado el 15-oct:** PR #6 ya revisado por Miguel y fusionado por Lucía; main `ffb2494ad9da026f24a57826c45f6f530afb467b`. Véase §0.6. Las tablas de ejecuciones anteriores conservan sus fechas/resultados; no se vuelven a encargar revisiones ya completadas. Páginas/navegador y B00/LCA/R04 siguen pendientes.

No se implementa pantalla de auditoría. Q03 conserva los eventos y consulta técnica autorizada. **Decisión de Dulce del 08-oct:** la búsqueda desde Pacientes permanece en el primer avance de diciembre. El directorio sale de CU03 y pasa a pantalla aparte posterior (B07), con la lista completa de vigentes por defecto y filtro autorizado de bajas; campos visibles y las demás políticas marcadas al final siguen pendientes de Dulce con la usuaria. [Guía del equipo](../../README.md). B00/LCA y R04 permanecen abiertos.

## 0. Cómo se usa

| Quién | Qué prueba | Proyecto | Herramientas |
| --- | --- | --- | --- |
| **Miguel** | Unitarias de backend: reglas de Domain y Application, sin base de datos (con dobles de los puertos). | `TherapEase.UnitTests` | xUnit |
| **Dulce** | Unitarias de la capa Web (PageModel) y checklist visual de cada pantalla. | `TherapEase.UnitTests` + checklist en el PR | xUnit |
| **Lucía (QA)** | Arquitectura, integración con PostgreSQL desechable, recorrido completo, seguridad y evidencia final. | `TherapEase.IntegrationTests`, `TherapEase.E2ETests`, CI | xUnit + Testcontainers, Playwright, k6, Astra |

- **Terminado:** el PR lleva las unitarias de su autor; Lucía agrega o actualiza las suyas del cambio; CI en verde; revisión cruzada. Las pruebas de Lucía las revisa Miguel o Dulce (autor ≠ revisor, 07C).
- **★ = corte mínimo B00–B04.** Cada prueba entra cuando se construye su bloque (B01–B08).
- **Pendiente** = depende de una decisión aún abierta (sección 6); se escribe cuando se decida.
- Nombres de prueba en español sin tildes (07A), p. ej. `Agendar_ConCruceConCitaActiva_Rechaza`.
- **Datos rastreables:** usar una contraseña y un contacto ficticios fáciles de buscar (p. ej. `DatoRastreable#Prueba1`, `datoRastreable@ficticio.test`) para comprobar después que no aparecen en registros (S-21).
- No se añaden herramientas nuevas sin ADR (07A). Si Dulce quiere probar JavaScript con otra herramienta, primero ADR.

## 0.1. Seguimiento de ejecución — 08–10-oct-2026

La columna **Estado** usa solo ✅ y ⏳. ✅ identifica ejecución local con evidencia para el alcance indicado; no equivale a revisión humana, aprobación, CI ni cierre. ⏳ señala que falta completar o evidenciar el caso; puede haber una parte ya probada. No se acreditan las unitarias de Miguel o Dulce a partir de las pruebas de integración de Lucía.

| Casos | Evidencia y alcance del estado |
| --- | --- |
| A-01 | Pruebas automáticas preparadas y ejecutadas el 08-oct: **30, 28 correctas y 2 fallidas, 0 omitidas** tras aprobar Lucía enumeraciones/constantes de Domain en Web. Hallazgos: Web llama a MatrizDePermisos y falta coordinador CU04. Controles negativos detectan infracciones; páginas funcionales y revisión de Miguel pendientes. Sigue ⏳; alcance y comando en B00 §4.3. Cambios aún locales para la actualización final del PR #4. |
| A-02, A-03, A-07, S-22, S-23 | Migraciones reales, usuario separado, cruce SQL y permisos de auditoría/esquema en `RevisionDatosTests`; repetidos dentro de los 50 casos del 07-oct. |
| L-CU02-02/03/04/06, L-CU11-01/02/03 | Duplicados, desactivación, rol, restablecimiento, cambio propio y auditoría en servicios reales de Identity. No acredita las páginas o formularios. |
| S-01, S-02, S-04, S-05, S-08 | Bloqueo, mensaje genérico, atributos de cookie, revocación y hash en componentes reales. Expiración con reloj/fecha controlados; navegador pendiente. HTTPS local se probó aparte el 10-oct (B00 §4.5). |
| S-04, S-29 (local) | ✅ HttpsLocalTests: 12 correctas el 10-oct, TLS 1.3, certificado/nombre válidos y rechazos, redirección 307, renovación real Secure/HttpOnly/Lax, cliente .NET excluye cookie en HTTP, HSTS local. No acredita navegador ni despliegue. B00 §4.5. |
| Consulta Q03 por API | 51 casos Q03 tras la auditoría: 46 correctos y 5 fallidos; la ejecución anterior repitió también 12 HTTPS correctamente. Filtros válidos, límites, permisos, solo lectura y metadatos en B00 §4.6. 0, 1, 2 y las listas Paciente,Cita / Paciente, Cita devuelven 200 en vez de 400; Miguel debe aceptar solo nombres completos. Comando final y revisión humana pendientes. |
| Q08 de Identity | ✅ 16 casos correctos en InterrupcionesIdentidadTests: antes/después del commit, cancelación, corte real de conexión, éxito posterior y reintentos de alta/baja/rol. Cambios propios temporal y ordinario probados; en el ordinario se deja pasar su primera confirmación. Restablecer otra vez genera otra clave/evento; procedimiento pendiente. No acredita páginas, red real de escrituras, Q08 completo en pacientes/citas/pago. B00 §4.7; auditoría de Claude APROBADO; revisión humana pendiente. |
| S-20, S-21 — API disponible | ✅ 11 casos correctos en ErroresRegistrosTests; B00 §4.8. 400/401/500 sin valores privados buscados, registros del proceso capturados y recuperación 200. ⏳ Alcance completo en otras rutas/páginas, mensaje del 500 vacío de Identity y revisión humana. Auditoría de errores/registros: APROBADO. |
| S-30 | ✅ Política aprobada e implementación PR #5 comprobada; nueve casos HTTPS de QA en B00 §4.10–§4.11. ✅ Revisión/fusión de PR #6 completadas el 15-oct. ⏳ Navegador, CSP de recursos y CSS con sesión. Este seguimiento antiguo se actualiza sin acreditar pruebas nuevas. |
| S-26, S-27 | Búsqueda acotada en historial/archivos e imagen histórica, y revisión de `.env.example` ficticio. No se detectaron credenciales de TherapEase dentro del alcance; las claves privadas de ejemplo de GnuTLS están clasificadas en la evidencia. No es garantía universal. |
| A-04, S-32 | ✅ Repetidos el 08 y 09-oct en HorasDockerTests: imagen actual construida, no root, /salud 200 y HEALTHCHECK healthy; ruta sin sesión 401. 12 casos correctos; el 09-oct se verificó también la eliminación automática de la imagen. B00 §4.4. |
| L-CU02-01/05/07, L-CU04-01/03/04, L-CU06-05, L-Q03-01/02/04 | Cobertura parcial: temporal/políticas, último superusuario concurrente, auditoría de Identity, versión EF, conservación del paciente, dos reservas SQL y consulta autorizada. Faltan los demás caminos/operaciones, formularios o recuentos previstos; no se da por completo el caso compuesto. |
| Q09 (servidor/imagen), S-31 | Horas en UTC/Nueva York/Madrid, entradas con/sin zona en API, cita EF/PostgreSQL y reinicio real con claves/sesión comprobados el 08-oct. Las 12 se repitieron correctamente el 09-oct tras ajustar limpieza. Q09 de navegador y operaciones de agenda sigue ⏳; S-31 local ✅. Detalle y reproducción en B00 §4.4. |
| S-03, S-06/07, S-11/12/13, S-21/24, S-28/33/34 | Comprobaciones parciales de servidor/configuración; faltan alcance completo, rutas, navegador, infraestructura o CI según el caso. Cinco fallos simultáneos no acreditan por sí solos cualquier volumen de intentos. |
| ADR-05 — decisión del 10-oct | age para operación real, restauración/comparación antes de cifrar y simulacro mensual de Lucía; claves/retención según AGENTS §2.8. No cambiar pruebas ahora: Cifrar/Abrir y sus casos son formato de QA; cifrado se reemplazará por age al implementar, conservando volcado/restauración/comparación/fallos/antigüedad. Sin implementación autorizada. |
| A-05 | Recuperación corregida y repetida el 08-oct: respaldo cifrado/huella, 13 tablas y 7,375 s desde detección en muestra pequeña; además copia de 25 h restaurada, con incumplimiento Q10 informado. Revisión humana/aprobación pendientes. |
| N-02, N-03 | Demostración local previa y corregida disponible; B08/operación real, carga, revisión humana y aprobación siguen pendientes. |
| Restantes | No hay evidencia suficiente para acreditar su alcance completo; permanecen ⏳. Las decisiones de producto abiertas se conservan en §6. |

Evidencia detallada: [Identity](TherapEase-09-B00-evidencia-identidad.md), [recuperación y correcciones](TherapEase-09-B00-evidencia-recuperacion.md), [avance B00](../planning/TherapEase-etapa09-B00.md). El TRX local `b00-conjunto-50-corregido-08oct.trx` identifica la nueva ejecución de los 50 casos tras el veredicto; `b00-conjunto-50.trx` conserva la anterior. La visibilidad pública de A-06 ya fue decidida; medir minutos de CI sigue pendiente. B00/LCA/R04 abiertos.

## 0.2. Estado vigente — 11-oct-2026

| Estado | Alcance | Evidencia / límite |
| --- | --- | --- |
| ✅ | Revisión humana de QA del PR #4 y fusión | Miguel APPROVED sobre f06dc7b; main 709ff615. No incluye QA nuevo local. |
| ✅ | Repetición del PR #5, 7e83c3c | 192 unitarias y 182 de integración correctas, en ejecuciones separadas; 0 fallidas/omitidas. Arquitectura 30/30 y Q03 51/51: siete fallos anteriores corregidos. B00 §4.9. |
| ✅ | CU04 mínimo, nueve adicionales locales | PostgreSQL real: cambios/eventos, conflictos, citas frente a baja y respuesta perdida. Comentadas según el skill del proyecto. Fuera del PR de Miguel. |
| ⏳ | Incorporación y revisión humana de las nueve adicionales | Miguel o Dulce revisa; no acreditar revisión por la de PR #4. |
| ⏳ | Implementar permiso aprobado, actualizar README/AGENTS y aprobar PR #5 | Lucía aprobó el 11-oct Usuaria y Superusuario como provisional. Miguel implementa y prueba; Lucía revisa el nuevo commit. |
| ✅ | Decisiones de Lucía del 11-oct, órdenes 1–3 de B00 §7 | Cabeceras y cuatro mejoras de backend incluidas en PR #5; tres mejoras de QA incluidas en el próximo PR conjunto de Lucía. Implementación, pruebas y revisión siguen ⏳. Reglas en AGENTS §2.5–§2.6. |
| ✅ | Secret Protection y Push protection de GitHub | Captura aportada por Lucía el 12-oct: ambos botones Disable. Configuración activada; no prueba de bloqueo, ni revisión actual de main, ni garantía de ausencia de secretos. B00 §7, orden 10. |
| ⏳ | Cobertura restante B00 | Páginas/navegador, temporales, Q03, cabeceras/secretos. B00 §7 ordena las propuestas, dependencias y entregas: mejoras de Miguel en PR #5 y QA de Lucía en PR #6, ambos ya revisados y fusionados; sin PR por cada punto. B00/LCA/R04 siguen abiertos. |

El seguimiento 0.1 conserva las ejecuciones del 08–10-oct como historia. Los fallos de arquitectura/Q03 allí citados ya no son el resultado vigente. No se marcan completas las pruebas de páginas u operaciones aún inexistentes.

## 0.6. PR #6 revisado y fusionado — 15-oct-2026

✅ Miguel aprobó la versión `0db344a`; Lucía fusionó mediante squash en `ffb2494ad9da026f24a57826c45f6f530afb467b`. No se repitieron pruebas por actualizar este plan. Se conservan los resultados de B00 y siguen pendientes páginas/navegador, producto y cierre B00/LCA/R04.

⏳ Seguimientos de revisión: tres tiempos de espera agotados en el primer arranque pasaron aislados y en la segunda ejecución completa; Miguel los atribuye a carga de arranque. S-30 debe añadir CSS con sesión, porque la prueba anterior solo cubre CSS sin sesión. Observación no bloqueante para PR #6; no se da por ejecutado ese caso.

## 0.5. Correcciones de auditoría del PR #6 — 14-oct-2026

| Estado | Comprobación | Resultado / pendiente |
| --- | --- | --- |
| ✅ | Dependencias de pruebas corregidas | Pkcs y Xml fijados/resueltos en 10.0.13 en UnitTests e IntegrationTests. Auditoría de siete proyectos con transitivos: cero paquetes vulnerables reportados. |
| ✅ | Encabezados y repetición completa | Nueve encabezados ampliados sin cambiar lógica. Build Release sin errores/advertencias; 258 unitarias y 205 integración correctas, 0 fallos/omitidas. Evidencia y comandos en B00 §4.11. |
| ✅ | Revisión humana y fusión del PR #6 | [Miguel aprobó](https://github.com/LuciAguilar/Therapease/pull/6#pullrequestreview-5528518803) `0db344a`; Lucía fusionó mediante squash el 15-oct en `ffb2494ad9da026f24a57826c45f6f530afb467b`. Páginas/navegador y resto de B00 conservan sus pendientes. |

Esta sección actualiza 0.4; no acredita cierre de B00/LCA ni cambia la autorización de avance a B01 con datos ficticios.

## 0.4. Actualización de QA — 13-oct-2026

| Estado | Alcance | Resultado / pendiente |
| --- | --- | --- |
| ✅ | Repetición final de QA | Código 4ca53c4: 258 unitarias y 205 integración correctas, 0 fallos/omitidas. Evidencia/huellas en B00 §4.10. Revisión humana posterior completada en PR #6 el 15-oct. |
| ✅ | PR #5 bce9409 | 258 unitarias y 182 integración, 0 fallos/omitidas; compilación Release sin errores/advertencias. Mensaje 500 corregido; PR #5 fusionado el 13-oct, main 40785d2. |
| ✅ | BajaPacienteTests: 14 casos | Nueve anteriores más Usuaria/Superusuario, con/sin temporal y sesión ausente. Guardado/evento reales y concurrencia conservados. L-CU04, Q03/Q07/Q08/Q20; cobertura de servidor, no página. |
| ✅ | CabecerasSeguridadTests: 9 casos | S-30 y apoyo Q04/Q08/Q19: cabeceras reales, caché privada, estático, fallo de Identity JSON/HTML y recuperación. No sustituye navegador/CSP de recursos. |
| ✅ | Tres mejoras de QA | Arranque con timeout tolerado, evento restaurado comprobado en HTTP, documentación de crear-roles.sql para un destino nuevo. Recuperación: 13 casos correctos. |
| ✅ | Revisión y fusión de QA nuevo | [PR #6](https://github.com/LuciAguilar/Therapease/pull/6) revisado por Miguel sobre `0db344a` y fusionado por Lucía el 15-oct en `ffb2494`. Esta ampliación tiene su propia revisión, distinta de PR #4. |
| ⏳ | Páginas, navegador y producto | Mantener los casos completos pendientes; detalles y responsables en B00 §7. |
| ✅ | Avance a Construcción/B01 | Autorizado por Lucía el 13-oct, con datos ficticios y pendientes B00/LCA visibles; no declara cierre de B00/LCA, datos reales ni nube. |

Esta sección prevalece sobre estados históricos 0.1–0.3. Pruebas comentadas según el skill del proyecto. Ejecuciones locales, no CI.

## 0.3. Correspondencia del QA adicional de CU04 — PR #5

**Archivo actual:** `BajaPacienteTests.cs`, preparado por Codex para QA de Lucía en la copia local de revisión. **Fuera del PR #5; incorporado a la entrega de QA de Lucía del 13-oct, con 14 casos.** Los nueve casos originales concretan requisitos ya contemplados; no crean una funcionalidad nueva ni sustituyen los recorridos completos siguientes.

| Estado de ejecución local | Casos del archivo | Caso/requisito ya previsto | Alcance que falta |
| --- | --- | --- | --- |
| ✅ | Baja sin citas, con cita cancelada y con cita de baja: 3 casos | L-CU04-03/05; Q03 y Q20 | Baja/evento guardados; falta salida de listas/búsquedas y recorrido autorizado desde página. Política operativa aún provisional. |
| ✅ | Cita activa previa y cita insertada entre consulta/guardado: 2 casos | L-CU04-05/06 | Rechazo real sin baja/evento. No acredita la política final ni todas las operaciones de reservas. |
| ✅ | Versión modificada por otra conexión: 1 caso | L-CU04-01; Q07 | Conflicto en baja, sin sobrescribir. Faltan dos ediciones de ficha y aviso en página. |
| ✅ | Fallo real al insertar auditoría: 1 caso | Q03; regla de cambio/evento atómicos, B00 §7 paso 2 | Se revierte la baja. No acredita atomicidad de todas las operaciones restantes. |
| ✅ | Respuesta perdida después de confirmar y nueva solicitud: 1 caso | L-CU04-02; Q08 | No duplica baja/evento. Interrupción simulada tras commit, no corte HTTP/navegador. |
| ✅ | Baja primero y cita simultánea que espera el bloqueo: 1 caso | L-CU04-06 | PostgreSQL rechaza la cita. Falta el recorrido por servicio de reservas y repetición ampliada del caso completo. |
| ✅ | Renombrar archivo y clase a `BajaPacienteTests` | Ajuste de nombre solicitado por Lucía el 12-oct | Comentarios y lógica conservados; el archivo sigue local, fuera del PR #5. |
| ✅ | Incorporar el archivo como `BajaPacienteTests.cs` | QA de Lucía, revisión humana de Miguel o Dulce | Aplicar/conservar comentarios y subir en un PR de Lucía, separado del #5 de Miguel. No se crea rama ni PR en este paso. |

Los identificadores de la sección 3 conservan ⏳ si su alcance completo aún falta, aunque una parte tenga ejecución ✅ aquí. La prueba automática de arquitectura verifica ubicación y dependencias; estas nueve comprueban además el comportamiento real del coordinador.

## 1. Miguel — unitarias de backend

### Identidad: CU01, CU02, CU11 (B01) ★

| ID | Estado | Qué hacer → qué debe pasar | Origen |
| --- | --- | --- | --- |
| M-01 | ⏳ | Desactivar o degradar al único superusuario activo → rechaza. Con dos activos → permite uno. | HU02, CU02 A1 |
| M-02 | ⏳ | Alta con nombre de usuario duplicado o rol inexistente → rechaza. Alta válida → `DebeCambiarContrasena = true`. | HU02, CU02 A2 |
| M-03 | ⏳ | Si el registro del evento de auditoría falla → el servicio no confirma el cambio. | HU02, CU02 A6, S4 |
| M-04 | ⏳ | Cambio propio ordinario exige la contraseña actual; primera entrada con temporal pide solo la nueva; nunca permite cambiar la de otro usuario. | HU15, CU11 |
| M-05 | ⏳ | Los eventos de usuario, rol y contraseña no contienen contraseña ni hash. | Q03, Q04 |

### Pacientes: CU03, CU04 (B02) ★

| ID | Estado | Qué hacer → qué debe pasar | Origen |
| --- | --- | --- | --- |
| M-06 | ⏳ | Registrar sin nombre o sin ámbito → rechaza. Con uno o dos ámbitos → válido. Ámbito repetido → rechaza. | HU04, CU03 A2 |
| M-07 | ⏳ | Buscar coincidencias por nombre → devuelve las posibles; un homónimo sigue pudiendo registrarse. | HU04, CU03 A1 |
| M-08 | ⏳ | Actualizar con una versión anterior → conflicto, sin sobrescribir. | HU06, Q07 |
| M-09 | ⏳ | Actualizar por el flujo ordinario una ficha dada de baja → rechaza e informa su condición. | CU04 A4 |
| M-10 | ⏳ | Baja → `Condicion = Baja`, `FechaBaja` e `IdUsuarioBaja` llenos. Recuperar → `Vigente` y ambos vacíos. | 08C regla 7, Q20 |
| M-11 | ⏳ | Base B00: rechazar baja con cita activa. Entrevista solicita cancelación automática: cambiar expectativa solo tras aprobación/implementación; baja/cancelaciones/eventos atómicos y pago conservado. | HU07, CU04 A2; entrevista 1 |
| M-12 | ⏳ | Alta y actualización llenan fecha y usuario de `EntidadAuditable`. | 08C |

### Citas y pago: CU05–CU09 (B03–B06)

| ID | Estado | Qué hacer → qué debe pasar | Origen |
| --- | --- | --- | --- |
| M-13 ★ | ⏳ | Cita activa solo si está agendada **y** vigente (probar las 4 combinaciones). | Q06 |
| M-14 ★ | ⏳ | Agendar con inicio ≥ fin, paciente inexistente o de baja, o ámbito que el paciente no tiene → rechaza. | HU09, CU06 A1 |
| M-15 ★ | ⏳ | Cruce: solapada con una activa → rechaza, aunque sea de otro ámbito. Cancelada o de baja → no bloquea. Contigua (10–11 y 11–12) → permite. ✅ Preferencia recibida: contiguas sí; ⏳ ejecución del caso restante. | HU09, CU06 A2 |
| M-16 ★ | ⏳ | Una cita nueva queda agendada, vigente y con pago pendiente. ✅ Respuesta recibida: inicial Pendiente; ⏳ pruebas. | 08C §2 |
| M-17 ★ | ⏳ | El pago solo acepta Pagado o Pendiente. | HU13, CU09 A6 |
| M-18 | ⏳ | Reprogramar conserva el pago. Cita cancelada, de baja o con paciente de baja → rechaza. | HU10, CU07 A3–A4 |
| M-19 | ⏳ | Cancelar → cancelada, conserva pago y condición. | HU11, Q20 |
| M-20 | ⏳ | Baja de cita → conserva estado y pago. | HU12 |
| M-21 | ⏳ | Recuperar: agendada con paciente de baja → rechaza; agendada con cruce → rechaza; cancelada → vigente pero sigue cancelada; ya vigente → informa. | HU12, CU08 |
| M-22 ★ | ⏳ | Si el guardado falla, el servicio devuelve error, nunca éxito. | Q08, S4 |

### Transversales

| ID | Estado | Qué hacer → qué debe pasar | Origen |
| --- | --- | --- | --- |
| M-23 ★ | ⏳ | «10:00 en Hermosillo» se convierte en el instante correcto y de vuelta. Una fecha sin hora no se convierte en instante. | Q09 |
| M-24 | ⏳ | El evento de auditoría lleva actor, momento, tipo e Id del registro, acción y campos; nunca valores de contacto. | Q03, Q04 |

## 2. Dulce — capa Web e interfaz

### Unitarias de PageModel (con servicios de aplicación falsos)

| ID | Estado | Qué hacer → qué debe pasar | Origen |
| --- | --- | --- | --- |
| D-01 ★ | ⏳ | Formularios de acceso, paciente, cita y contraseña con campos obligatorios vacíos → mensaje de validación y no se llama al servicio. | CU03 A2, CU06 A1 |
| D-02 ★ | ⏳ | El servicio responde «conflicto» → la página avisa y conserva lo capturado. | Q07, S3 |
| D-03 ★ | ⏳ | Rechazo conocido → «No se guardó» y motivo. Excepción inesperada → «No pudimos confirmar si se guardó; revisa el registro antes de repetir», sin reintento automático. Restablecer otra vez → avisar que la temporal anterior deja de servir. | Q08, S5 |
| D-04 ★ | ⏳ | Éxito → confirmación solo tras respuesta correcta. | Q08 |
| D-05 ★ | ⏳ | El PageModel solo usa servicios de Application, nunca el acceso a datos. (También lo vigila A-01.) | 07A |
| D-06 | ⏳ | Cambio de contraseña: sin campo «actual» en la primera entrada; con él en el cambio ordinario. | HU15 |

### Checklist visual por pantalla (según el PDF de pantallas)

| ID | Estado | Qué revisar | Origen |
| --- | --- | --- | --- |
| D-07 ★ | ⏳ | Login: mensaje de error genérico; nunca «el usuario no existe». | CU01 A1 |
| D-08 ★ | ⏳ | El menú muestra opciones según el rol (Usuarios solo para superusuario); no muestra Auditoría. Ocultar no protege: eso lo prueba Lucía (S-11 a S-13). | Decisión 09; 08B §1.2 |
| D-09 ★ | ⏳ | B02: búsqueda/lista mínima de pacientes vigentes, estado vacío y acceso a ficha autorizada. | HU05, Q20 |
| D-15 | ⏳ | **B07, posterior al primer avance de diciembre:** directorio separado de CU03, lista completa de vigentes por defecto, información relevante validada, filtro por ámbito y filtro autorizado de bajas. | Q20; decisión de Dulce 08-oct |
| D-10 ★ | ⏳ | Agenda: con la zona horaria del equipo cambiada, las horas siguen en Hermosillo. Estado vacío. Canceladas solo con su filtro. | HU08, Q09 |
| D-11 | ⏳ | Detalle de cita: interruptor Pagado/Pendiente; confirmación antes de cancelar y de dar de baja. | HU11–HU13 |
| D-12 | ⏳ | Mensajes emergentes: guardado, faltan datos, conflicto, incierto, confirmar baja. | S3–S5 |
| D-13 ★ | ⏳ | JavaScript nunca inserta datos con `innerHTML` (usar `textContent`); los formularios conservan el token antifalsificación de Razor. | 05B, 07B |
| D-14 | ⏳ | Se dice «paciente», nunca «cliente»; «Dar de baja», nunca «Eliminar». | §2 v0.4 |

## 3. Lucía — funcionales (integración y recorrido)

Integración: xUnit + Testcontainers, con el esquema creado por **las migraciones reales**. Recorrido: Playwright.

### B00 — Validación de arquitectura (LCA) ★

| ID | Estado | Qué hacer → qué debe pasar | Origen |
| --- | --- | --- | --- |
| A-01 | ⏳ | Prueba automática de límites: Web usa Application y solo enumeraciones/constantes sin lógica de Domain, salvo composición de Infrastructure; Domain sin dependencias de otras capas; Pacientes no depende de Citas; coordinador CU04 fuera de ambos; sin ciclos. PR #5: 30/30 correctas; Miguel corrigió Web e implementó CU04. Revisó las pruebas de PR #4; falta repetir sobre PageModel funcionales. | 07A, 07C, ADR-06; B00 §4.9 |
| A-02 | ✅ | Las migraciones crean el esquema desde cero sin errores con un usuario distinto del de la aplicación; las credenciales de migración no se usan en ejecución normal. | 07B |
| A-03 | ✅ | Insertar por SQL directo dos citas activas solapadas → **la base** las rechaza (no solo el código). | Q06, 05C |
| A-04 | ✅ | La imagen Docker arranca, responde la comprobación de salud y corre sin privilegios. | 05E |
| A-05 | ✅ | Respaldo cifrado, huella SHA-256 correcta y restauración en PostgreSQL desechable con los mismos datos. | Q10, 05E |
| A-06 | ⏳ | Lucía registra la decisión de visibilidad del repositorio antes de crearlo y mide minutos de CI por PR, indicando visibilidad, tipo de runner y duración por trabajo. | R01, 07C, ajuste B00 del 01-oct |
| A-07 | ✅ | Con PostgreSQL/Testcontainers y el usuario real de la aplicación: `INSERT` en `EventoAuditoria` permitido; `UPDATE`, `DELETE` y cambio de esquema denegados. | Q03, 07B; S-22–S-23 |

### CU01 — Acceder y cerrar sesión (B01) ★

| ID | Estado | Qué hacer → qué debe pasar | Origen |
| --- | --- | --- | --- |
| L-CU01-01 | ⏳ | Credenciales válidas → entra con su rol (la usuaria y superusuario). | HU01 |
| L-CU01-02 | ⏳ | Salir en el navegador A → la sesión del navegador B deja de funcionar en su siguiente solicitud. | Q02, 07B |
| L-CU01-03 | ⏳ | Salida repetida o con sesión vencida → sin error y sin acceso. | CU01 A8 |

### CU02 — Gestionar usuarios (B01) ★

| ID | Estado | Qué hacer → qué debe pasar | Origen |
| --- | --- | --- | --- |
| L-CU02-01 | ⏳ | Crear usuario → recibe contraseña temporal → al entrar, solo puede ir a cambiarla. | HU02 |
| L-CU02-02 | ✅ | Nombre de usuario duplicado → rechaza. | CU02 A2 |
| L-CU02-03 | ✅ | Desactivar → sus sesiones abiertas se cierran y ya no puede entrar. | HU02, CU01 A4 |
| L-CU02-04 | ✅ | Cambiar rol → en la siguiente solicitud ya no tiene los permisos anteriores. | CU02 A7 |
| L-CU02-05 | ⏳ | Los dos últimos superusuarios se desactivan o degradan mutuamente al mismo tiempo → queda al menos uno activo. | HU02, CU02 A1 |
| L-CU02-06 | ✅ | Restablecer contraseña → cierra sus sesiones y la temporal obliga a cambiarla; no levanta el bloqueo vigente, conforme a [AGENTS.md](../../AGENTS.md). | HU03; decisión de Lucía 04-oct |
| L-CU02-07 | ⏳ | Cada acción genera su evento sin contraseña; si el evento no se guarda, el cambio tampoco. | Q03, S4 |

### CU11 — Cambiar contraseña propia (B01) ★

| ID | Estado | Qué hacer → qué debe pasar | Origen |
| --- | --- | --- | --- |
| L-CU11-01 | ✅ | Cambio con la actual correcta → cambia y cierra las demás sesiones. | HU15 |
| L-CU11-02 | ✅ | Actual incorrecta o nueva que incumple la longitud aprobada → no cambia; no se exige composición adicional. Condiciones en [AGENTS.md](../../AGENTS.md). | CU11 A1; decisión de Lucía 04-oct |
| L-CU11-03 | ✅ | El evento se registra sin contraseña. | Q03 |

### CU03 — Registrar, buscar y consultar paciente (B02) ★

| ID | Estado | Qué hacer → qué debe pasar | Origen |
| --- | --- | --- | --- |
| L-CU03-01 | ⏳ | Registrar con uno y con dos ámbitos → consultable y auditado. | HU04 |
| L-CU03-02 | ⏳ | Nombre ya existente → muestra coincidencias; se puede abrir la existente o registrar un homónimo. | HU04, CU03 A1 |
| L-CU03-03 | ⏳ | La búsqueda ordinaria no muestra pacientes de baja. | HU05, Q20 |
| L-CU03-04 | ⏳ | Doble clic en Guardar o reenvío → no crea dos fichas. | S5 |

### B07 — Directorio como pantalla aparte, posterior al primer avance de diciembre

> Por decisión de Dulce del 08-oct, estas pruebas salen del alcance CU03/B02 y del primer avance. Se conservan sus identificadores originales únicamente para trazabilidad; no significan que el directorio siga dentro de CU03. La búsqueda de Pacientes se prueba en L-CU03-02/03.

| ID | Estado | Qué hacer → qué debe pasar | Origen |
| --- | --- | --- | --- |
| L-CU03-05 | ⏳ | Directorio posterior sin filtro de bajas → lista completa de vigentes; con filtro autorizado → bajas consultables sin alterar fichas. | Q20; decisión de Dulce 08-oct; ID histórico |
| L-CU03-06 | ⏳ | Sin sesión o sin permiso para bajas → el directorio posterior no revela fichas ni permite el filtro de bajas por URL/envío directo. | Q01, Q20; decisión de Dulce 08-oct; ID histórico |

### CU04 — Actualizar, dar de baja y recuperar paciente (B02, B06)

| ID | Estado | Qué hacer → qué debe pasar | Origen |
| --- | --- | --- | --- |
| L-CU04-01 ★ | ⏳ | Dos usuarios editan la misma ficha → el segundo recibe aviso de conflicto; nada se pierde en silencio. | Q07 |
| L-CU04-02 ★ | ⏳ | Cortar la respuesta después de guardar → al consultar, el dato es coherente y no se anunció éxito. | Q08 |
| L-CU04-03 | ⏳ | Baja → sale de listas y búsquedas, aparece en «Pacientes de baja» y sigue en la base. | Q20 |
| L-CU04-04 | ⏳ | Recuperar → vuelve a las listas; se limpian los datos de baja; el evento histórico se conserva. | Q20, 08C |
| L-CU04-05 | ⏳ | Base B00 probada: rechazo con cita activa. La entrevista pide cancelación automática al dar de baja paciente; cambiar expectativa solo tras decisión/implementación, con baja/cancelaciones/eventos atómicos, pago y concurrencia protegidos. | CU04 A2 |
| L-CU04-06 | ⏳ | Dar de baja al paciente mientras se le agenda una cita, al mismo tiempo y repetido muchas veces → nunca queda una cita activa de un paciente de baja. | CU06 A3 |
| L-CU04-07 | ⏳ | Retirar ámbito conserva el original de las citas según entrevista. Recuperar/reprogramar con ámbito retirado y el cambio a solo particular siguen pendientes; probar tras concretarlos. | CU04 A5 |

### CU05 — Consultar agenda (B03) ★

| ID | Estado | Qué hacer → qué debe pasar | Origen |
| --- | --- | --- | --- |
| L-CU05-01 | ⏳ | Muestra solo las activas del periodo; canceladas con filtro; de baja nunca. | HU08 |
| L-CU05-02 | ⏳ | Navegador con otra zona horaria (p. ej. Ciudad de México y Madrid) → mismas horas de Hermosillo. | Q09 |
| L-CU05-03 | ⏳ | Periodo sin citas → estado vacío. | CU05 A1 |

### CU06 — Agendar cita (B03) ★

| ID | Estado | Qué hacer → qué debe pasar | Origen |
| --- | --- | --- | --- |
| L-CU06-01 | ⏳ | Cita válida → guardada con pago pendiente y auditada. | HU09 |
| L-CU06-02 | ⏳ | Solapada con una activa, aunque sea de otro ámbito → rechaza. | Q06 |
| L-CU06-03 | ⏳ | Contigua (una termina 11:00, otra empieza 11:00) → permitida. **Pendiente.** | 08B §1.1 |
| L-CU06-04 | ⏳ | Solapada con una cancelada o de baja → permitida. | Q06 |
| L-CU06-05 | ⏳ | 2 y 10 solicitudes simultáneas por el mismo horario → se guarda exactamente una. | Q06, R02 |
| L-CU06-06 | ⏳ | Agendar desde un equipo en otra zona horaria → al consultarla, la hora de Hermosillo está intacta. | HU09 CA4, Q09 |
| L-CU06-07 | ⏳ | Servidor/imagen y persistencia probados (B00 §4.4); falta recorrido de agenda. **Extra:** citas en las fechas de cambio de horario de EE. UU. y del resto de México (Hermosillo no cambia) → la hora no se desplaza. | Q09 |
| L-CU06-08 | ⏳ | Paciente de baja o ámbito que no tiene → rechaza. | CU06 A1 |
| L-CU06-09 | ⏳ | Respuesta interrumpida y reintento → no existe una segunda cita. | S5 |

### CU07 — Reprogramar o cancelar cita (B05)

| ID | Estado | Qué hacer → qué debe pasar | Origen |
| --- | --- | --- | --- |
| L-CU07-01 | ⏳ | Reprogramar a un hueco libre → nuevo horario, mismo pago, auditado. | HU10 |
| L-CU07-02 | ⏳ | Reprogramar a un hueco ocupado → rechaza y conserva el horario anterior. | HU10, Q06 |
| L-CU07-03 | ⏳ | Dos reprogramaciones simultáneas de la misma cita → una recibe aviso de conflicto. | Q07 |
| L-CU07-04 | ⏳ | Cancelar → libera el horario (se puede agendar otra ahí), conserva el pago y no queda de baja. | HU11, Q20 |
| L-CU07-05 | ⏳ | Reprogramar una cita cancelada o de baja → rechaza. | CU07 A3 |
| L-CU07-06 | ⏳ | Reprogramar desde otra zona horaria → la nueva hora de Hermosillo queda intacta. | HU10 CA4 |

### CU08 — Dar de baja y recuperar cita (B06)

| ID | Estado | Qué hacer → qué debe pasar | Origen |
| --- | --- | --- | --- |
| L-CU08-01 | ⏳ | Baja → fuera de la agenda, visible en «Citas de baja», estado y pago intactos. | HU12 |
| L-CU08-02 | ⏳ | Recuperar una agendada con horario libre y paciente vigente → vuelve. | HU12 |
| L-CU08-03 | ⏳ | Recuperar una agendada con cruce → rechaza y sigue de baja. | HU12 CA2 |
| L-CU08-04 | ⏳ | Recuperar una agendada con paciente de baja → rechaza. | HU12 CA2 |
| L-CU08-05 | ⏳ | Recuperar una cancelada → vigente pero cancelada; no ocupa horario. | CU08 A2 |
| L-CU08-06 | ⏳ | Recuperar mientras otro agenda ese mismo horario, al mismo tiempo → nunca quedan dos activas solapadas. | Q06 |

### CU09 — Estado de pago (B04) ★

| ID | Estado | Qué hacer → qué debe pasar | Origen |
| --- | --- | --- | --- |
| L-CU09-01 | ⏳ | Cambiar Pendiente ↔ Pagado → guardado y auditado. | HU13 |
| L-CU09-02 | ⏳ | Uno cambia el pago y otro reprograma la misma cita a la vez → aviso de conflicto. | CU09 A1 |
| L-CU09-03 | ⏳ | Cancelar o dar de baja no cambia el pago (verificar en la base). | HU13 CA3 |
| L-CU09-04 | ⏳ | Enviar a mano un valor distinto de Pagado/Pendiente → rechaza. | CU09 A6 |
| L-CU09-05 | ⏳ | Respuesta recibida: permitir corrección autorizada del pago en cancelada y rechazarla en cita de baja; cancelar/baja conservan pago. Montos requieren decisión de alcance y reglas. | CU09 A3–A4 |

### Q03 — Auditoría en base (captura desde B01; sin pantalla CU10 en 09)

| ID | Estado | Qué hacer → qué debe pasar | Origen |
| --- | --- | --- | --- |
| L-Q03-01 ★ | ⏳ | Cada cambio de paciente, cita, pago y usuario genera exactamente un evento con actor, momento, registro/campo y hecho. | Q03 |
| L-Q03-02 ★ | ⏳ | Si el evento no se puede guardar → el cambio tampoco se guarda. | Q03, S4 |
| L-Q03-03 ★ | ✅ | El usuario de aplicación puede insertar eventos, pero `UPDATE`/`DELETE` se deniegan en PostgreSQL. La consulta técnica autorizada devuelve únicamente actor, momento, registro/campo y hecho. | Q03, 07B, B00 aprobado |
| L-Q03-04 ★ | ⏳ | Opción A aprobada el 12-oct: Superusuario activo sin temporal entra por CU01 y abre la URL de consulta en la misma ventana, con cookie/permisos vigentes. Sin sesión, revocada o sin permiso: denegación sin eventos. Sin pantalla de auditoría ni script B. Falta recorrido real con páginas y navegador. | Q03, Q01–Q02, AGENTS §2.3 |

Consulta Q03 sin pantalla: opción A de navegador autenticado aprobada el 12-oct, con cookie/permiso vigente. El filtro fue corregido en PR #5; 51/51 históricas según B00 §4.9. Falta recorrido final con acceso/cierre de sesión de Dulce y navegador; captura/atomicidad de todas las operaciones continúan con su cobertura parcial. Sin comando/script B ni ejecución nueva por esta actualización.

### Recorrido completo Q16 (desde B04) ★

| ID | Estado | Qué hacer → qué debe pasar | Origen |
| --- | --- | --- | --- |
| L-Q16-01 | ⏳ | Playwright en cada PR: entrar → registrar paciente → agendar cita → marcar pagada → salir. | Q16 |
| L-Q16-02 | ⏳ | El mismo recorrido con un superusuario. | Q16 |

## 4. Lucía — exclusivas de seguridad

Referencia: OWASP Top 10:2025, ASVS 5.0 aplicable y 07B. «Todas las rutas» = lista de páginas y formularios protegidos, que se actualiza en cada PR.

### Autenticación y sesiones (B01) ★

| ID | Estado | Qué hacer → qué debe pasar | Origen |
| --- | --- | --- | --- |
| S-01 | ✅ | 5 fallos → bloqueo de 15 min; la contraseña correcta durante el bloqueo no entra; al terminar, se vuelve a evaluar. | 07B, CU01 A2–A3 |
| S-02 | ✅ | Usuario inexistente, contraseña incorrecta y usuario bloqueado reciben exactamente el mismo mensaje. | CU01 A1 |
| S-03 | ⏳ | Muchos intentos fallidos en paralelo no permiten más de 5 antes del bloqueo. | CU01 A3 |
| S-04 | ✅ | La cookie de sesión tiene `HttpOnly`, `Secure` y `SameSite=Lax`. | 07B |
| S-05 | ✅ | Copiar la cookie, salir y reutilizarla → no funciona. | Q02, 07B |
| S-06 | ⏳ | Cookie manipulada o ausente → pide acceso sin mostrar datos. | CU01 A6 |
| S-07 | ⏳ | Con contraseña temporal, entrar por URL directa a pacientes o citas → redirige al cambio de contraseña. | CU01 A5 |
| S-08 | ✅ | En la base, las contraseñas solo existen como hash de Identity; nunca en texto ni en SHA-256 simple. | Q19 |
| S-09 | ⏳ | **Extra:** la redirección después del login (`returnUrl`) solo acepta rutas internas; un enlace a un sitio externo se ignora. | ASVS |
| S-10 | ⏳ | **Extra:** después de salir, el botón «atrás» no muestra páginas protegidas (sin caché). | Q02 |

### Autorización (todas las operaciones) ★

| ID | Estado | Qué hacer → qué debe pasar | Origen |
| --- | --- | --- | --- |
| S-11 | ⏳ | Sin sesión: cada página y cada envío protegido de «todas las rutas» → denegado, también por URL directa. | Q01, S1 |
| S-12 | ⏳ | la usuaria intenta usar Usuarios y roles por URL o envío directo → denegado. | 08B §1.2 |
| S-13 | ⏳ | Un rol de prueba sin permisos (luego, el capturista) intenta pago, bajas y auditoría por URL y envío directo → denegado. | Q17, HU13 CA4 |
| S-14 | ⏳ | **Extra:** enviar campos de más en un formulario (p. ej. `EstadoPago` al reprogramar, `IdUsuarioAlta`, `Condicion`, `Rol`) → se ignoran. | 07B |
| S-15 | ⏳ | Abrir o editar por URL un Id inexistente o de baja → no revela datos. | S2 |

### Entradas y salidas

| ID | Estado | Qué hacer → qué debe pasar | Origen |
| --- | --- | --- | --- |
| S-16 | ⏳ | Enviar un formulario sin token antifalsificación o con el de otra sesión → rechazado. | 07B |
| S-17 | ⏳ | Nombre de paciente con `<script>` o `"><img src=x onerror=...>` → se muestra como texto en la lista, el panel y la auditoría; no se ejecuta. | 07B |
| S-18 | ⏳ | Buscar con `' OR 1=1 --` y variantes → sin error ni resultados de más. Revisar que no haya SQL armado con texto del usuario. | 05E A05 |
| S-19 | ⏳ | Envío directo con datos inválidos, saltando la validación del navegador → el servidor rechaza. | 07B |
| S-20 | ⏳ | Provocar un error → mensaje genérico, sin detalles técnicos, rutas ni datos. | Q04 |

### Datos, auditoría y registros

| ID | Estado | Qué hacer → qué debe pasar | Origen |
| --- | --- | --- | --- |
| S-21 | ⏳ | Tras correr toda la suite, buscar los datos rastreables en registros técnicos, errores y eventos → no aparecen. | Q04 |
| S-22 | ✅ | Con el usuario de base de datos de la app, intentar modificar, borrar o alterar la tabla de auditoría → denegado. | Q03, 07B |
| S-23 | ✅ | El usuario de la app no puede cambiar el esquema; las migraciones usan otro usuario. | 07B |
| S-24 | ⏳ | Revisar filas de auditoría → sin contacto, contraseña ni texto clínico. | Q03 |
| S-25 | ⏳ | El conjunto de datos de la demo es completamente ficticio. | Q05, R04 |

### Secretos, configuración e infraestructura

| ID | Estado | Qué hacer → qué debe pasar | Origen |
| --- | --- | --- | --- |
| S-26 | ✅ | Buscar secretos en el repositorio (incluido el historial) y en la imagen Docker → ninguno. | Q19, 07B |
| S-27 | ✅ | `.env.example` solo tiene nombres y valores ficticios. | 07B |
| S-28 | ⏳ | La conexión a Neon usa `SSL Mode=VerifyFull` (revisión de configuración, sin conectar desde CI). | 07B, 07C |
| S-29 | ✅ | Validación local: HTTP redirige a HTTPS y HSTS se emite en modo Production con nombre .test; localhost excluido. HttpsLocalTests, B00 §4.5. Despliegue y navegador siguen pendientes; duración HSTS final por decidir al desplegar. | Q19 |
| S-30 | ✅ / ⏳ | Cabeceras aprobadas el 11-oct, implementadas en PR #5 bce9409 y comprobadas con nueve casos HTTPS de CabecerasSeguridadTests (B00 §4.10–§4.11, PR #6). ⏳ Navegador, CSP de recursos y caso CSS con sesión; revisión humana/fusión de PR #6 completadas el 15-oct; HSTS final al despliegue autorizado. No atribuir ejecución nueva a esta corrección. | ASVS, AGENTS §2.5 |
| S-31 | ✅ | Reiniciar el contenedor → las sesiones siguen siendo válidas (claves de Data Protection en PostgreSQL) y no hay claves en disco. | 05D, 07B |
| S-32 | ✅ | El proceso del contenedor no corre como administrador. | 05E |

### Dependencias y revisión de vulnerabilidades

| ID | Estado | Qué hacer → qué debe pasar | Origen |
| --- | --- | --- | --- |
| S-33 | ⏳ | La CI audita paquetes NuGet y falla ante una vulnerabilidad alta. | 05E A03 |
| S-34 | ⏳ | Acciones de GitHub fijadas por SHA; imagen base fijada por digest. | 07B |
| S-35 | ⏳ | Revisión de vulnerabilidades con Astra sobre código y configuración; los hallazgos altos se corrigen antes de la demo. Si se revisa la app publicada, antes se necesita la autorización de Lucía para publicar. | Prompt maestro §10, §12 |

### Antes de datos reales (futuro)

| ID | Estado | Qué hacer → qué debe pasar | Origen |
| --- | --- | --- | --- |
| S-36 | ⏳ | TOTP obligatorio, código inválido o reutilizado → no entra; recuperación por pérdida de la app probada. | HU-F06, CU-F14 |
| S-37 | ⏳ | Revisión completa de R04 antes de cualquier dato real. | R04, 08D D04 |

## 5. Lucía — evidencia final (B08)

| ID | Estado | Qué hacer → qué debe pasar | Origen |
| --- | --- | --- | --- |
| N-01 | ⏳ | k6 con 4 usuarios, 100 pacientes y 1,000 citas ficticias → al menos 95 % en ≤3 s, incluido un acceso tras suspensión. | Q12 |
| N-02 | ⏳ | Restaurar un respaldo y comparar → pérdida máxima de 24 h de cambios. | Q10 |
| N-03 | ⏳ | Recuperación cronometrada desde la detección del fallo → máximo 24 h. | Q11 |
| N-04 | ⏳ | Persona ajena al equipo: registra paciente, agenda y marca pagada sin ayuda; se anotan sus dificultades. | Q13 |
| N-05 | ⏳ | Demo de 30 min sin interrupciones (calentamiento documentado). | Q14 |
| N-06 | ⏳ | Otro integrante prepara el entorno solo con la documentación. | Q15 |
| N-07 | ⏳ | Si falla el respaldo diario, se abre o actualiza una incidencia asignada a Lucía. | 05E |

## 6. Decisiones pendientes que cambian pruebas

| Decisión | Pruebas afectadas | Quién decide |
| --- | --- | --- |
| ✅ Respuesta: citas contiguas sí. | M-15, L-CU06-03 | Preferencia de la usuaria recibida; ⏳ implementación/QA del alcance restante. |
| ✅ Respuesta: estado inicial Pendiente. | M-16 | Respuesta recibida; ⏳ monto inicial si se aprueba ampliación, pruebas pendientes. |
| ✅ Respuesta: cancelar automáticamente al dar de baja paciente; ⏳ cambio de regla por aprobar. | M-11, L-CU04-05 | Lucía decide impacto; Miguel propone operación atómica/eventos/concurrencia. Base B00 sigue rechazando con activa. |
| ✅ Respuesta: retirarlo conservando original en citas; ⏳ recuperación con ámbito retirado y solo particular. | L-CU04-07 | Dulce concreta alternos; Lucía decide cambio de alcance. |
| ✅ Respuesta: cancelada sí, de baja no; ⏳ montos solicitados por aprobar. | L-CU09-05, HU13/CU09 | Lucía decide alcance de montos, Dulce detalla, Miguel implementa y Lucía prueba. |
| Resuelto el 04-oct: restablecimiento no levanta el bloqueo vigente | L-CU02-06 | Lucía aprobó; Miguel implementa; Lucía prueba |
| Resuelto el 04-oct: contraseña mínima de 12 caracteres sin composición | L-CU11-02 | Lucía aprobó; Miguel implementa; Lucía prueba |
| Resuelto el 11-oct: cabeceras según AGENTS §2.5, implementadas/probadas en PR #5 y QA posterior | S-30 | ✅ Implementación y evidencia histórica; ✅ revisión/fusión QA nuevo completadas el 15-oct; ⏳ navegador, CSP de recursos y CSS con sesión. |
| Resuelto el 04-oct: Dulce escribe los PageModel y sus unitarias | Sección 2 | Lucía confirmó; revisión distinta y aprobación final de Lucía |
| ✅ Respuesta: nombre en lista; nacimiento/apellidos para distinguir homónimos; directorio prioridad posterior 3. | D-15, L-CU03-05–06 | ⏳ Concretar selección/formatos y capacidad; no volver a pedir preferencia ya recibida. |
| Resuelto el 01-oct: consulta técnica sin pantalla de B00 para superusuario activo aprobada | L-Q03-03–04, S-22–S-24 | Lucía aprobó y prueba; Miguel implementa y revisa las pruebas de Lucía |
| Resuelto el 11-oct: baja provisional de pacientes para Usuaria y Superusuario; otros permisos no se amplían | CU04, Q01; pruebas de autorización del permiso nuevo | Lucía aprobó; Miguel implementa y prueba en PR #5. Dulce valida la política operativa con la usuaria. |
| Resuelto el 12-oct: temporales, orden 7 puntos 1–4; A para B00 ficticio, B de 24 h antes de datos reales | CU02, CU11, Q08/Q19 | Lucía aprobó; entrega/interfaz y prueba final pendientes; B no se implementa ahora |
| Resuelto el 12-oct: Q03 por navegador autenticado, orden 8 opción A; script B no incluido | L-Q03-03–04, S-22–S-24 | Lucía aprobó; acceso/cierre de sesión de Dulce y prueba final pendientes |

## Ajuste de cobertura por entrevista del 14-oct-2026

### Respuestas recibidas que sustituyen esperas de producto

| Estado | Respuesta recibida | Pendiente que permanece |
| --- | --- | --- |
| ✅ | Paciente: nombre completo, teléfono, correo, nacimiento, escuela/grupo; anotación de edad. Tutor siempre en menores; homónimos por nacimiento y apellidos; lista solo nombre. | ⏳ Campos/formatos/obligatoriedad, datos del tutor y edad calculada o guardada; escuela/grupo ante «solo particular». |
| ✅ | Citas contiguas permitidas; pago inicial Pendiente; corregir pago cancelada sí, de baja no. | ⏳ Implementación/QA restante; horario aclarado por Lucía (9–15: 1 h; desde 15: posibilidad de 30 min, hasta 19); límite de 19 por precisar. |
| ✅ | Solicita solo particular, montos y cancelación automática de citas al dar de baja paciente. | ⏳ Aprobación de cambios frente a la base y mecanismo/modelo/pruebas; conservar invariantes y pago. |
| ✅ | Solo la usuaria operativa; Usuario con nombre completo y correo; pantallas «Están bien»; logotipo en inicio. | ⏳ Contratos/validaciones de campos, versión exacta de pantallas y recurso visual; no recuperación por correo. |
| ✅ | Recorrido de diciembre conforme; después expediente/notas 1, reportes 2, directorio 3, reprogramar/cancelar 4. | ⏳ Baja/recuperación sin número; capacidad/secuencia técnica, sin renumerar B/HU/CU ni adelantar clínico. |
| ✅ | Expediente: motivo, antecedentes, notas y plan; anotaciones iniciales/por cita. Reportes: citas por mes y pacientes atendidos; anotación de lista de espera. | ⏳ Modelo/vínculo/indicadores/proceso; capacidades futuras, sin HU/CU nuevos por inferencia. |
| ✅ | Papel, práctica declarada de 5 años, acceso real solo la usuaria y consulta antes de decidir eliminación. | ⏳ R04, política detallada y revisión antes de datos reales. No borrar automáticamente ni archivar indefinidamente. |

Todos los estados ⏳ de ejecución se conservan. Incorporar a los casos existentes de CU01/CU02 (datos de Usuario), CU03/CU04 (campos/tutor/homónimos/baja), CU05–CU09 (horario/montos/pago) y futuros CU-F11–F13 el alcance aprobado cuando se implemente. No crear IDs ficticios ni contar respuestas de entrevista como casos correctos.

**Horario aclarado por Lucía, 14-oct:** ✅ 9:00–15:00, sesiones de 1 h; desde 15:00 puede haber sesiones de 30 min, hasta 19:00. ⏳ Último inicio o término a 19:00, días y cruce de las 15:00 por concretar. Referencia America/Hermosillo; no acredita implementación ni pruebas.
