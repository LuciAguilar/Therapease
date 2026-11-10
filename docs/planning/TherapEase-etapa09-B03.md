# TherapEase — B03 · La agenda y las citas

**05-nov-2026 · Claude: CORREGIR recibido; Lucía aprobó D1–D13 ✅.** Precisiones documentales aplicadas. El plan del 01-nov se publicó en d2302f2; esta versión ajustada y la propuesta siguen locales. No se acredita implementación, pruebas o nueva auditoría APROBADO.

> Los días, campos y horario del 01-nov siguen vigentes. La propuesta técnica y D1–D13 ya fueron revisadas/decididas; Lucía comparte las precisiones con Miguel. Esta entrega solo prepara documentos y el aviso del helper B01; no modifica código/pruebas B03 ni publica desde herramientas.

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
| Reenvío             | Respuesta perdida o envío repetido sin otra cita ni otro evento. El mecanismo lo propone Miguel y lo decide Lucía.                                                                                                                                                                                                                                    |
| Páginas y uso       | Campos, mensajes, doble clic, permisos y horas desde el navegador; requieren páginas y PageModel de Dulce.                                                                                                                                                                                                                                            |

**Estado:** toda esta cobertura está prevista. Las unitarias comprueban reglas por separado; la integración comprueba servicios y PostgreSQL juntos; el navegador comprueba las páginas. La base de B00 ayuda, pero no acredita el recorrido completo de B03.

Los casos siguen en el [plan de pruebas: CU05](../testing/TherapEase-09-plan-de-pruebas.md#cu05--consultar-agenda-b03-) y [CU06](../testing/TherapEase-09-plan-de-pruebas.md#cu06--agendar-cita-b03-). No se copian sus tablas aquí ni al checklist personal.

---

## 2. Quién hace qué ahora

| Persona | Trabajo autorizado | Qué espera |
| --- | --- | --- |
| Miguel | Una propuesta completa; señalar qué reutiliza de B00/B01/B02 y qué falta decidir. Mantener el apoyo a Dulce. | Claude CORREGIR recibido y D1–D13 aprobadas; Lucía transmite las precisiones. Trabajo de servidor pendiente del PR de Miguel. |
| Dulce | Seguir conectando las páginas B01/B02; no quedan preguntas de producto B03 para Dulce. | Contrato decidido para las páginas de B03; conserva PageModel y sus unitarias. |
| Lucía con Codex | Preparar documentos y QA por escrito a partir de los casos existentes; revisar privacidad y reunir decisiones. | Propuesta para auditar con Claude y decidir. Las pruebas ejecutables esperan la implementación aprobada. |

Miguel y Dulce eligen sus herramientas. La auditoría de IA no sustituye la revisión humana del código y las pruebas.

## 3. Una sola propuesta y decisiones juntas

| Tema | Qué debe quedar claro |
| --- | --- |
| Campos de cita | Paciente, ámbito, inicio y fin; sin campos nuevos. Método de pago descartado; HU13/CU09 no cambian. Opcional: Miguel y Dulce pueden proponer inicio y duración en la página, calculando fin; 30 minutos solo desde las 15:00. El servidor valida siempre. |
| Permisos | Quién puede consultar y agendar, con una tabla expresa por rol. No conceder permisos nuevos por suposición. |
| Consulta de agenda | Mostrar los siete días y citas en cualquier minuto. Proponer periodo y límites, datos visibles, detalle autorizado y estado vacío; canceladas fuera de la agenda activa. Si el paciente de una cancelada está de baja, explicar qué se muestra sin revelar su nombre o ficha: B02 solo permite «Está dada de baja». Su consulta nueva queda en B06. Explicar cómo se convierten los días del periodo con HoraHermosillo. |
| Horario | Los siete días; inicio en cualquier minuto sin segundos, desde las 9:00. Antes de las 15:00 dura una hora, aunque termine después de las 15:00; desde las 15:00 dura 30 minutos o una hora. Fin máximo a las 19:00 y mismo día. 14:30–15:30 sí se permite. Explicar dónde vive y se valida la regla y cómo tratar citas ficticias o pruebas existentes fuera de horario, sin cambiarlas ni borrarlas ahora. La página debe enviar fecha y hora de forma definida, convertidas mediante HoraHermosillo sin depender de la zona del navegador o servidor. |
| Cruces y paciente | Conservar controles B00 frente a otra reserva y baja del paciente. Si se retira el ámbito mientras se agenda, la cita lo conserva, sin migración; Miguel explica cómo protege ese orden. |
| Reenvío seguro | Misma reserva sin otra cita ni evento. Nunca responder «horario ocupado» por chocar con su propia cita. Hoy la base reconoce el identificador repetido de pacientes, no de citas: Miguel propone cómo resolverlo, sin copiar B02 a ciegas. Mantener permisos, actor y datos comprobados. |
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

Hay reglas, repositorio y restricciones de citas. Todavía no hay un servicio completo para consultar agenda y agendar.

> **Lección de B02:** diseñar el QA para forzar los distintos momentos entre comprobar y guardar, también cuando una solicitud gemela termina antes de buscar coincidencias. Una coincidencia solo al guardar puede dejar un fallo sin comprobar. Son pruebas futuras, no resultados de esta apertura. Si una regla queda tanto en SQL como en .NET, comparar todos los minutos del día y sus límites para detectar diferencias; ahora solo se diseña por escrito.

### 4.1. Cómo prepararemos los cambios simultáneos

**Diseño escrito ajustado a D1–D13 y a Claude.** Se enlaza a los casos existentes; no añade identificadores ni sustituye sus tablas. Los puntos de espera se enlazan al servicio propuesto; se implementan cuando corresponda el PR, sin acreditar ejecución aquí.

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

En `tests/TherapEase.IntegrationTests/`, revisar `PostgreSqlRevision.cs`, `ConcurrenciaSqlRevision.cs`, `PacientesRevision.cs` y `InterrupcionesIdentidadRevision.cs`: entorno desechable, observación de bloqueos, solicitudes separadas y cortes de guardado. Reutilizar lo que sirva; las esperas para las nuevas operaciones aún no están implementadas. Un ensayo SQL aislado no sustituye el recorrido del servicio de agendar.

Aplicar [L-Q03-01/02](../testing/TherapEase-09-plan-de-pruebas.md#q03--auditoría-en-base-captura-desde-b01-sin-pantalla-cu10-en-09) y L-CU06-09 al fallo de auditoría y la respuesta perdida: verificar juntos cita y evento, tanto si se revirtió todo como si se confirmó todo antes del corte. El corte del guardado, el corte HTTP y el doble clic real son comprobaciones distintas; las últimas esperan las páginas correspondientes. Permisos y resultados siguen D1/D2 aprobados: sin cita, cruce conserva horario ocupado, TE001 texto de baja y clave repetida 500; errores inesperados/auditoría no se releen.

## 5. Seguimiento y publicación

| Estado | Paso | Resultado o dependencia |
| --- | --- | --- |
| ✅ | Autorizar preparación | Antecedente del 31-oct: solo preparación. D1–D13 aprobadas el 05-nov; esta entrega Codex sigue documental. |
| ✅ preparación documental | Preparar seguimiento compartido y personal | Plan aprobado por Lucía y publicado; checklist personal conservado localmente. |
| ✅ correcciones y publicación / ⏳ envío por Lucía | Revisar borradores y mensaje | Correcciones aplicadas; Lucía aprobó y se publicó en main d2302f2. Mensaje completo preparado para que Lucía lo pase a Miguel. No se ha enviado desde herramientas. |
| ✅ decisión 01-nov | Precisar horario | Días, inicios y duraciones decididos en AGENTS, sección 3; sustituyen la regla del 31-oct. |
| ✅ decisiones 01-nov | Producto B03 | Paciente, ámbito, inicio y fin; siete días y horario definidos; pasado permitido y ámbito retirado conservado durante el alta. Sin preguntas de producto para Dulce. |
| ✅ revisada y ajustada | Recibir propuesta técnica | Propuesta del 03-nov/0e2838b auditada CORREGIR; precisiones documentales aplicadas y D1–D13 aprobadas por Lucía. |
| ✅ auditoría recibida / ✅ decisión | Auditar y decidir | Claude CORREGIR del 05-nov; Lucía aprobó D1–D13. No se atribuye otro APROBADO. Implementación, pruebas y publicación son pasos distintos. |
| ✅ diseño escrito / ⏳ ejecución | Diseñar QA por escrito | Sección 4.1 como fuente única: agendar/baja, gemelo y ámbito retirado. Añadir en la implementación las tres salidas y límites; ninguna prueba B03 ejecutada aquí. |
| ✅ publicación anterior / ⏳ versión ajustada | Publicar documentación actual en main durante B03 | Dieciséis documentos del 01-nov publicados en d2302f2. La aprobación D1–D13 no publica esta versión: queda local; sin envío/publicación desde herramientas. |
| ⏳ | Implementación, QA ejecutable y páginas | D1–D13 aprobadas. [PR #18](https://github.com/LuciAguilar/Therapease/pull/18): Lucía revisó `fe6b79a` (810 unitarias, 27 integraciones de citas y CI 4/4; su QA dirigido dio 4/5) y aprobó el bloqueo de la agenda en un disparador de `Cita` (migración `BloqueoDeAgendaEnCitaActiva`; [ADR-04](../architecture/adr/ADR-01-a-05-provisionales.md#adr-04--integridad-y-ciclo-de-datos)). Esta corrección lo implementa, retira el tratamiento especial del `40P01` (vuelve a ser error 500, sin relectura) y agrega sus pruebas; SHA y CI de la corrección, en la [evidencia](../testing/TherapEase-09-B03-evidencia-servicios.md). [Contrato para páginas](TherapEase-etapa09-B03-contrato-para-paginas.md) actualizado. QA dirigido de Lucía y páginas de Dulce pendientes. Revisión distinta, CI y decisión final permanecen. |

Antes de cada entrega: conservar `revision/etapa09`, hacer fetch y merge de origin/main sin reset y preservar todos los cambios locales. En esta entrega, fetch/merge sin cambios sobre 0e2838b; rama global y cambios locales conservados. Nada de commit, push, PR, fusión de PR ni limpieza sin aprobación explícita de Lucía.

**Qué está disponible:** #11/#12 están publicados y fusionados. La versión del plan del 01-nov está publicada en main d2302f2; la propuesta recibida y esta revisión del 05-nov siguen locales. Su nota personal y la evidencia quedan locales. D1–D13 ya están aprobadas con las precisiones de Claude; implementación y pruebas siguen pendientes de su PR. Esta entrega Codex no las toca.

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
