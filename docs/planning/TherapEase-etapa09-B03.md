# TherapEase — B03 · La agenda y las citas

**Estado vigente, 12-nov-2026.** ✅ #18 aprobado por Lucía y fusionado con squash en 8bcc7c6, tras Claude APROBADO v1, 12-nov. Preparación local para #19: 37 casos propios B03 y una unitaria de cita agendada sin nombre; 884/884 unitarias, 562/562 integraciones y 54/54 Chrome B01 correctas. ✅ Ampliación de #19: Claude APROBADO v1, 12-nov. ✅ Publicación autorizada por Lucía. ⌛ CI del nuevo SHA y revisión de Miguel. Páginas B03 con Dulce después de B02. B00/B01/B02/LCA/R04 siguen abiertos. Lucía autorizó esta entrega para el mismo PR #19; su CI y la revisión humana siguen pendientes.

✅ indica una tarea terminada en el alcance descrito. ⏳ indica un pendiente. Entregar código, ejecutar pruebas, revisar y aprobar son pasos distintos.

| Parte | Estado comprobado |
| --- | --- |
| Diseño y publicación | ✅ Decisiones del 01-nov y D1–D13 del 05-nov. Documentación compartida publicada en main `6dd13d9`. |
| Servidor de Miguel | ✅ Claude APROBADO v1, 12-nov; Lucía aprobó bfc78f2 y fusionó #18 con squash en 8bcc7c6. |
| CI de #18 | ✅ [43058777583](https://github.com/LuciAguilar/Therapease/actions/runs/43058777583), 4/4; 883 unitarias y 521 integraciones. GitHub probó 2c440f1, combinación con main 32e7bca. |
| QA propio de Lucía | ✅ 37 casos auditados por Claude el 12-nov. Preparados para #19, junto con una unitaria nueva; 562/562 integraciones y 884/884 unitarias locales correctas. ✅ Ampliación de #19: Claude APROBADO v1, 12-nov. ⌛ Autorización de publicación y revisión de Miguel. |
| Páginas y uso | ✅ #13 fusionado en 32e7bca; QA B01 propio en #19, revisión de Miguel pendiente. ⌛ Dulce termina pacientes B02 antes de páginas B03. HTTP, navegador y uso B03 siguen pendientes. |
| Decisión final | ✅ Lucía decidió fusionar #18. ⌛ #19 requiere auditoría de la ampliación, publicación, CI y revisión de Miguel antes de la decisión final. No se cierra B03. |

## 1. Qué abarca y qué comprobaremos

HU08–HU09 y CU05–CU06: consultar la agenda y reservar una cita ficticia en horario de Hermosillo. El servidor B01/B02 y el QA de pacientes ya están integrados; #12 quedó fusionado en main `b53c5b7`.

| Parte               | Qué comprobamos                                                                                                                                                                                                                                                                                                                                       |
| ------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Uso normal          | Consultar un periodo, ver un estado vacío y agendar para un paciente vigente y un ámbito permitido.                                                                                                                                                                                                                                                   |
| Datos y horario     | Los siete días; inicio en cualquier minuto sin segundos, desde las 9:00. Antes de las 15:00 dura una hora, aunque termine después de las 15:00; desde las 15:00 dura 30 minutos o una hora. Fin máximo a las 19:00 y mismo día. 14:30–15:30 sí se permite. Usar America/Hermosillo. Permitir fecha u hora pasada con leyenda calculada y evento fijo. |
| Acceso              | Comprobar sesión y permiso antes de consultar o reservar; no revelar datos a quien no tiene acceso.                                                                                                                                                                                                                                                   |
| Cruces              | Solo agendada y vigente ocupa horario. Cruces incluso entre ámbitos, citas contiguas y reservas simultáneas.                                                                                                                                                                                                                                          |
| Guardado y fallos   | Cita y evento juntos, éxito solo después de guardar y ningún cambio parcial si falla la auditoría.                                                                                                                                                                                                                                                    |
| Cambios simultáneos | Agendar mientras se da de baja al paciente; no dejar una cita activa de un paciente de baja.                                                                                                                                                                                                                                                          |
| Reenvío             | Respuesta perdida o envío repetido sin otra cita ni otro evento. Identificador fijo y relectura aprobados por Lucía e integrados en #18.                                                                                                                                                                                                                                    |
| Páginas y uso | ✅ #13 fusionado en 32e7bca; QA B01 propio en #19, revisión de Miguel pendiente. ⌛ Dulce termina pacientes B02 antes de páginas B03. HTTP, navegador y uso B03 siguen pendientes. |

**Estado:** Miguel entregó cobertura de servicios en #18. El QA propio de servicios está ejecutado y auditado; quedan su incorporación a #19 y los recorridos de páginas. Las unitarias comprueban reglas por separado; la integración comprueba servicios y PostgreSQL juntos; el navegador comprueba las páginas. La base de B00 ayuda, pero no acredita el recorrido completo de B03.

Los casos siguen en el [plan de pruebas: CU05](../testing/TherapEase-09-plan-de-pruebas.md#cu05--consultar-agenda-b03-) y [CU06](../testing/TherapEase-09-plan-de-pruebas.md#cu06--agendar-cita-b03-). No se copian sus tablas aquí ni al checklist personal.

---

## 2. Qué hará cada persona

| Estado | Persona | Trabajo de agenda y citas | Qué falta |
| --- | --- | --- | --- |
| ✅ | Miguel | Propuesta, permisos, horario y servicios de agenda y citas. | Servidor fusionado en #18/8bcc7c6, con aprobación de Lucía y Claude APROBADO v1. |
| ✅ | Miguel | Unitarias, integración de apoyo y evidencia del servidor. | #18 integrado. Las pruebas propias de Lucía se incorporan a su #19 y Miguel las revisa. |
| ✅ contrato / ⏳ apoyo | Miguel | Contrato publicado en su rama para las páginas de Dulce. | Apoyar también B01/#13 y pacientes B02. Revisar el QA de Lucía cuando se comparta. |
| ✅ | Lucía con Miguel | Disparador y arranque sin conexión de #18. | SQL, impacto y reversión revisados; Claude APROBADO v1, 12-nov; Lucía aprobó y fusionó #18. |
| ⏳ | Dulce | Agenda con vistas, fechas, límite y estados vacíos; formulario de paciente, ámbito e inicio/fin; resultado y mensajes. | Usar el contrato B03. Prioridad en páginas de pacientes B02, desde main actualizado, que ya incluye #13 en 32e7bca. |
| ⏳ | Dulce | PageModel y sus unitarias, antifalsificación, campos y navegación. | Entregar por PR con revisión humana distinta. |
| ✅ ejecución y auditoría previa / ⌛ ampliación | Lucía con Codex | QA de servicios y PostgreSQL de la sección 4.1. | 37 casos aprobados por Claude; importados sin cambios a la rama de #19. Con la unitaria nueva: 884/884 unitarias y 562/562 integraciones. Claude APROBADO v1 sobre la ampliación, 12-nov. Falta publicación y revisión humana. |
| ✅ | Lucía | Claude APROBADO v1, 12-nov, sobre #18 y QA de 37 casos. | Aprobé fusionar #18; la nueva unitaria, los dos textos y su incorporación a #19 recibieron Claude APROBADO v1, 12-nov. Publicación autorizada por Lucía; falta CI y revisión de Miguel. |
| ⏳ páginas | Lucía con Dulce | Formularios, doble clic real, respuesta HTTP perdida, Chrome y uso. | Necesita las páginas. Los casos continúan en el plan de pruebas existente. |

Miguel y Dulce eligen sus herramientas. Lucía conserva la aprobación final.

## 3. Reglas aprobadas y decisiones que se conservan

| Tema | Qué debe quedar claro |
| --- | --- |
| Campos de cita | Paciente, ámbito, inicio y fin; sin campos nuevos. Método de pago descartado; HU13/CU09 no cambian. Opcional: Miguel y Dulce pueden proponer inicio y duración en la página, calculando fin; 30 minutos solo desde las 15:00. El servidor valida siempre. |
| Permisos | Quién puede consultar y agendar, con una tabla expresa por rol. No conceder permisos nuevos por suposición. |
| Consulta de agenda | Mostrar los siete días y citas en cualquier minuto. Periodo de 1 a 35 días y hasta 500 filas con aviso, datos visibles, detalle autorizado y estado vacío; canceladas fuera de la agenda activa. Si el paciente de una cancelada está de baja, explicar qué se muestra sin revelar su nombre o ficha: B02 solo permite «Está dada de baja». Su consulta nueva queda en B06. Explicar cómo se convierten los días del periodo con HoraHermosillo. |
| Horario | Los siete días; inicio en cualquier minuto sin segundos, desde las 9:00. Antes de las 15:00 dura una hora, aunque termine después de las 15:00; desde las 15:00 dura 30 minutos o una hora. Fin máximo a las 19:00 y mismo día. 14:30–15:30 sí se permite. Explicar dónde vive y se valida la regla y cómo tratar citas ficticias o pruebas existentes fuera de horario, sin cambiarlas ni borrarlas ahora. La página debe enviar fecha y hora de forma definida, convertidas mediante HoraHermosillo sin depender de la zona del navegador o servidor. |
| Cruces y paciente | Conservar controles B00 frente a otra reserva y baja del paciente. Si se retira el ámbito mientras se agenda, la cita lo conserva, sin migración; Miguel explica cómo protege ese orden. |
| Reenvío seguro | Misma reserva sin otra cita ni evento. Nunca responder «horario ocupado» por chocar con su propia cita. El traductor de #18 reconoce PK_Cita y la relectura compara la misma reserva confirmada. Mantener permisos, actor y datos comprobados. |
| Registro después del horario | Se permite con las mismas reglas. Calcular «Registrada después de su horario» comparando FechaAlta > Inicio; la hora procede del servidor, nunca de la página. Sin campo ni migración nuevos; su evento lo indica con texto fijo. |
| Guardado y auditoría | Cambio y evento juntos, respuesta de conflicto comprensible y evento sin contactos ni valores privados. |
| Datos y código existentes | Reutilización, cambios de datos necesarios, impacto y reversión si hubiera migración; ningún cambio de tecnología se da por aprobado. |

Reglas vigentes y seis decisiones: [AGENTS, sección 3](../../AGENTS.md#3-límites-vigentes). Sustituyen el horario del 31-oct; no quedan preguntas de producto para Dulce.

**No se vuelve a preguntar:** las citas contiguas están permitidas y el pago inicial es Pendiente. Esto no abre el cambio de estado de pago B04.

---

## 4. Base que aprovechamos y cuidado del QA

| Base existente | Uso previsto |
| --- | --- |
| Reglas y restricciones mínimas de citas de B00 | Partir de sus controles de horario, cruces y conservación; no duplicar entidades ni dar la agenda por implementada. |
| Pacientes B02 | Consultar por su contrato público, con paciente vigente y ámbito permitido. |
| Acceso B01 y auditoría B00 | Reutilizar sesiones, permisos, guardado y eventos; no inventar otra autenticación. |
| Ayudas de QA | Reutilizar PostgreSQL desechable, usuarios ficticios, reloj y fallos controlados. |

Las reglas y restricciones de B00 se reutilizan. El servicio de #18 quedó aprobado y fusionado en 8bcc7c6. Su QA de servicios tiene Claude APROBADO v1, 12-nov; la ampliación de #19 tiene Claude APROBADO v1, 12-nov; faltan publicación y revisión humana.

> **Lección de B02:** diseñar el QA para forzar los distintos momentos entre comprobar y guardar, también cuando una solicitud gemela termina antes de buscar coincidencias. Una coincidencia solo al guardar puede dejar un fallo sin comprobar. Los resultados ejecutados se identifican en la sección 5. Si una regla queda tanto en SQL como en .NET, comparar sus minutos y límites. El horario actual vive solo en Domain; el QA de servidor disponible se ejecutó el 12-nov.

### 4.1. Cómo prepararemos los cambios simultáneos

**Diseño escrito ajustado a D1–D13 y a Claude.** Se enlaza a los casos existentes; no añade identificadores ni sustituye sus tablas. Los puntos de espera se enlazan al servicio propuesto; sus ejecuciones y pendientes se identifican en la sección 5.

Cada solicitud tendrá su propio contexto y conexión a PostgreSQL. Prepararemos señales que indiquen «la comprobación terminó» y «puede continuar», con tiempo máximo y liberación también si falla el caso. La prueba deberá demostrar que alcanzó ese punto. Lanzar dos tareas juntas o esperar unos milisegundos no demuestra ese orden.

Para [L-CU04-06](../testing/TherapEase-09-plan-de-pruebas.md#cu04--actualizar-dar-de-baja-y-recuperar-paciente-b02-b06), usando la baja ya existente y su rechazo con citas activas:

1. **La baja se confirma primero.** Agendar comprueba que el paciente está vigente y se pausa antes de guardar. La baja se confirma en otra solicitud. Al continuar, agendar debe rechazarse: paciente de baja, ninguna cita nueva y solo el evento de baja.
2. **La cita se confirma primero.** La baja comprueba que no hay citas activas y se pausa antes de guardar. Agendar confirma la cita y su evento. Al continuar, la baja debe rechazarse: paciente vigente, una cita activa y su evento, sin evento de baja.
3. **Un cambio aún no se confirma.** Mantener abierta la transacción del primer cambio y observar en PostgreSQL que el segundo espera. Comprobar ambos órdenes; confirmar o revertir el primero y después dejar terminar el segundo. Revisar respuestas, paciente, citas y eventos. Una operación revertida no deja su cambio ni su evento. Si el segundo se rechaza sin esperar, registrar qué comprobación lo impidió; ese caso no acredita haber comprobado un bloqueo.

En todos los órdenes debe ser imposible terminar con una cita activa de un paciente de baja. Esto comprueba la protección existente; no incorpora cancelación automática ni nuevas funciones de B06.

Para [L-CU06-05 y L-CU06-09](../testing/TherapEase-09-plan-de-pruebas.md#cu06--agendar-cita-b03-), separar dos situaciones:

- **Reservas distintas por el mismo horario:** dos y diez solicitudes, cada una con su propia identidad de solicitud según D2 aprobado. Solo una puede guardar cita y evento; las demás reciben conflicto. Mantener los casos de ámbitos distintos y citas contiguas.
- **Reenvíos de la misma reserva:** mismo actor y mismos datos según D2 aprobado. Forzar ambos envíos antes del guardado, el primero ya confirmado antes de comenzar el segundo y el primero confirmado después de la lectura inicial del segundo, antes de su siguiente comprobación de paciente, ámbito o cruce. El resultado confirmado debe reconocerse sin otra cita ni otro evento, y nunca como «horario ocupado» por su propia cita. Si el primer intento se revierte, el segundo no puede anunciar un guardado inexistente y solo un cambio confirmado puede dejar evento.

D2 está aprobado con las precisiones de Claude: una misma relectura con permisos vigentes resuelve rechazos de negocio después de la primera lectura. Debe explicar qué pasa con datos distintos, otro actor o permisos retirados; conocer un identificador no concede acceso. También debe aclarar el reenvío si el paciente o su ámbito cambió después de la reserva original. El ámbito debe estar permitido al comprobarlo; si se retira después mientras se agenda, la cita lo conserva, conforme a la decisión del 01-nov, sin migración.

**Orden forzado de retiro de ámbito:** agendar comprueba el ámbito y se pausa antes de guardar. Otra solicitud confirma su retiro del paciente. Reanudar agendar y comprobar que conserva el ámbito ya elegido, sin restaurarlo al paciente ni cambiar citas históricas. Miguel explica cómo protege ese momento. Mantener la comprobación de paciente vigente y los eventos correspondientes.

**Ajuste del diseño escrito tras leer la propuesta de Miguel (05-nov; Claude CORREGIR recibido; D1–D13 aprobadas por Lucía):** A no encuentra su identificador y se pausa antes de consultar el paciente; el gemelo B confirma cita/evento; una tercera solicitud confirma el retiro del ámbito; A continúa. Debe reconocer la reserva activa propia con los mismos datos, sin restaurar el ámbito ni crear otro evento. Sin cita confirmada, el ámbito retirado sí debe rechazar el alta. Forzar también el retiro del permiso y otro actor/datos. Releer solo después de fallar el guardado no cubre un rechazo anterior de la comprobación del paciente. Se enlaza a L-CU06-09/L-CU04-07; no son pruebas ejecutadas.

### 4.2. Qué reutilizaremos al escribir las pruebas autorizadas

La lectura del código de `b53c5b7` confirma la restricción `EX_Cita_SinCruceActivo` y los controles `TR_Cita_ActivaExigePacienteVigente` y `TR_Paciente_BajaSinCitasActivas`, en la migración inicial de Infrastructure. Son base de protección, no un servicio de agenda terminado.

En `tests/TherapEase.IntegrationTests/`, revisar `PostgreSqlRevision.cs`, `ConcurrenciaSqlRevision.cs`, `PacientesRevision.cs` y `InterrupcionesIdentidadRevision.cs`: entorno desechable, observación de bloqueos, solicitudes separadas y cortes de guardado. Reutilizar lo que sirva; el QA local añade pausas antes de guardar una cita o una baja y comprueba que se alcanzaron. Un ensayo SQL aislado no sustituye el recorrido del servicio de agendar.

Aplicar [L-Q03-01/02](../testing/TherapEase-09-plan-de-pruebas.md#q03--auditoría-en-base-captura-desde-b01-sin-pantalla-cu10-en-09) y L-CU06-09 al fallo de auditoría y la respuesta perdida: verificar juntos cita y evento, tanto si se revirtió todo como si se confirmó todo antes del corte. El corte del guardado, el corte HTTP y el doble clic real son comprobaciones distintas; las últimas esperan las páginas correspondientes. Permisos y resultados siguen D1/D2 aprobados: sin cita, cruce conserva horario ocupado, TE001 texto de baja y clave repetida 500; errores inesperados/auditoría no se releen.

## 5. Seguimiento, revisión y publicación

| Estado | Paso | Resultado o dependencia |
| --- | --- | --- |
| ✅ | Preparar y decidir el diseño | D1–D13 aprobadas el 05-nov con las precisiones de Claude. |
| ✅ | Publicar documentación compartida | Main `6dd13d9`, con autorización de Lucía. Notas personales y evidencia quedan locales. |
| ✅ | Recibir implementación y pruebas de Miguel | #18 en bfc78f2; código 479ab5b. 21 cambios de archivo desde fe6b79a, sin modificar su código al revisar. |
| ✅ | Revisar y devolver correcciones | Claude APROBADO v1, 12-nov. #18 fusionado; la precisión del contrato sección 4.3 y la del impacto se preparan en #19. |
| ✅ comprobado | Tratamiento de 40P01 | Disparador con clave 7301, antes del FOR SHARE; sin traducción especial del interbloqueo. SQL, impacto y reversión revisados. Fallo preparado en auditoría: excepción técnica, sin relectura y cero cita/evento. |
| ✅ servicios / ⌛ entrega y páginas | QA propio de Lucía | 37/37, dos repeticiones adicionales 18/18, sin omitidas. Consulta, horarios, privacidad, reenvíos y baja/cita con commit y rollback; no acredita páginas ni cortes HTTP. |
| ✅ #18 y QA previo / ⌛ ampliación #19 | Auditoría, revisión humana y aprobación | Claude APROBADO v1, 12-nov; Lucía fusionó #18. Ampliación de #19: Claude APROBADO v1, 12-nov. Después de publicar y comprobar CI, Miguel revisa el PR completo y Lucía decide. |
| ✅ auditoría / ⌛ publicación | Incorporar el seguimiento de #18 a #19 | ✅ Una unitaria: Agendada, nombre nulo y PacienteDeBaja=false tras denegarse el nombre después de confirmar. ✅ Los 37 casos propios se conservan sin cambios. ✅ Contrato sección 4.3 e impacto del bloqueo precisados; B08 medirá su costo. Claude APROBADO v1, 12-nov; falta autorización de publicación, CI y Miguel. |
| ⏳ | Páginas, navegador y uso | Dulce conserva su entrega. B01/#13 fusionado en 32e7bca. B02 espera sus páginas. |
| ✅ autorización / ⌛ CI | Publicar esta actualización con el QA de #19 | Lucía autorizó commit y push normal de los 15 archivos auditados. Miguel revisará el PR completo tras la CI. |

Se conserva `revision/etapa09`, con fetch y merge de origin/main y los cambios locales. No se cierra ningún bloque por recibir #18.

**Resultado del 12-nov.** ✅ #18 aprobado por Lucía y fusionado con squash en 8bcc7c6, tras Claude APROBADO v1, 12-nov. Preparación local para #19: 37 casos propios B03 y una unitaria de cita agendada sin nombre; 884/884 unitarias y 562/562 integraciones correctas. ✅ Ampliación de #19: Claude APROBADO v1, 12-nov. ✅ Publicación autorizada por Lucía. ⌛ CI del nuevo SHA y revisión de Miguel. Páginas B03 con Dulce después de B02. Las cifras de las rondas anteriores conservan su versión y no se suman. El sondeo UTC/Tokio comprueba Domain, no páginas.

**Material disponible para el equipo.** El diseño aprobado está en main `6dd13d9`. El código, contrato y evidencia de Miguel ya están integrados en main 8bcc7c6 por #18. Las dos precisiones de texto posteriores están preparadas localmente para #19. Este seguimiento posterior y el checklist personal se preparan localmente.

## 6. Documentos de contexto

| Documento | Para qué leerlo |
| --- | --- |
| [AGENTS, sección 3](../../AGENTS.md#3-límites-vigentes) | Autorización, límites y decisiones. |
| [08D](TherapEase-etapa08-backlog-trazabilidad.md) | Fila B03 y relación con requisitos. |
| [08B](../requirements/TherapEase-etapa08-casos-historias.md) | HU08–HU09 y CU05–CU06, incluidos sus alternos. |
| [Entrevista, sección 5](../requirements/TherapEase-09-entrevista-requisitos.md#5-aclaración-del-horario-aportada-por-lucía) | Decisiones del 01-nov y antecedente histórico del 31-oct. |
| [Plan de pruebas](../testing/TherapEase-09-plan-de-pruebas.md) | M-13 a M-16, casos de CU05/CU06 y seguridad transversal. Mantener sus identificadores. |
| [Planes B00](TherapEase-etapa09-B00.md), [B01](TherapEase-etapa09-B01.md) y [B02](TherapEase-etapa09-B02.md) | Base entregada, cobertura y páginas pendientes. |
| [ADR-04](../architecture/adr/ADR-01-a-05-provisionales.md#adr-04--integridad-y-ciclo-de-datos) | Integridad, cruces, auditoría y reenvío. |

Fuera: B04 de cambio de pago, reprogramar/cancelar B05, nuevas bajas/recuperaciones B06, directorio B07, montos, «solo particular» y cancelación automática. No se cierra B00/B01/B02/LCA/R04. Solo datos ficticios, sin nube ni gastos.

Servidor fusionado: [#18](https://github.com/LuciAguilar/Therapease/pull/18). [Contrato para páginas](TherapEase-etapa09-B03-contrato-para-paginas.md) y [evidencia del servidor](../testing/TherapEase-09-B03-evidencia-servicios.md).
