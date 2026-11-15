# TherapEase — Guía para Dulce · B03 · Agenda y citas

**Preparada el 14-nov-2026 por indicación de Lucía.** B03 permite consultar la agenda y agendar citas ficticias. El servidor ya está integrado; no necesitas esperar a que Lucía termine su QA de páginas para preparar tu parte.

> Parte del [plan B03](TherapEase-etapa09-B03.md) y su [contrato para páginas](TherapEase-etapa09-B03-contrato-para-paginas.md). Conserva B02 en #21; B03 va en tu rama y entrega propias. Para reutilizar las páginas de pacientes, incorpora main cuando #21 esté fusionado.

## 1. Qué tenemos y qué falta

| Parte | Estado |
| --- | --- |
| Acceso y sesiones | ✅ B01 cerrado. Reutilizar su protección y mensajes. |
| Pacientes | Servidor disponible; páginas B02 en #21, aún por integrar. |
| Servidor de agenda y citas | ✅ #18 fusionado en 8bcc7c6. Consulta, permisos, horario, guardado y reenvío disponibles. |
| QA de servidor B03 | ✅ 37 casos propios de Lucía incorporados en #19, fusionado en 674c124. No son pruebas de páginas B03. |
| Páginas B03 | ⌛ Agenda, formulario, consulta de cita, navegación y pruebas propias de Dulce. |
| QA de páginas B03 | ⌛ Lucía lo prepara con las páginas disponibles. Q16 completo sigue en B04. |
| Revisión y cierre | ⌛ CI, revisión ajena, aprobación final de Lucía y evidencia antes de cerrar B03. |

## 2. Quién hace qué ahora

| Persona | Trabajo |
| --- | --- |
| Dulce | Páginas, formularios, mensajes, navegación, PageModel —la parte que conecta la página con el servidor— y sus unitarias. |
| Miguel | Apoyar dudas o fallos del servidor ya entregado. No es necesario esperar nuevas funciones para empezar agenda y alta de citas. |
| Lucía | Dirigir QA, revisar privacidad y cambios, decidir publicación y fusión. Otra persona revisa sus pruebas. |
| Revisor distinto del autor | Revisar el PR y sus pruebas. Ninguna revisión de IA sustituye esa revisión humana. |

## 3. Qué conectar en las páginas

| Función | Qué verá y podrá hacer la usuaria |
| --- | --- |
| Agenda | Consultar un periodo y distinguir citas activas de canceladas con su filtro. Mostrar estado vacío. |
| Periodo | Entre 1 y 35 días; sin fechas, hoy y los seis siguientes. Si hay más de 500 citas, pedir un periodo menor; no presentar la lista como completa. |
| Detalle | Consultar una cita autorizada. Una cita inexistente o de baja no revela información. |
| Elegir paciente | Buscar una ficha vigente y elegir uno de sus ámbitos permitidos. No capturar pacientes ni ámbitos nuevos desde la cita. |
| Agendar | Capturar paciente, ámbito, fecha, inicio y fin. Conservar la captura cuando se rechace. |
| Horario ocupado | Mostrar el aviso sin revelar quién ocupa el horario. Solo las citas activas lo bloquean. |
| Reenvío | Conservar el mismo número de solicitud mientras sea la misma reserva. Distinguir “agendada” de “ya estaba agendada”; no reenviar automáticamente. |
| Respuesta perdida | Ofrecer comprobar o reenviar con el mismo número; no crear otra reserva con un número nuevo. |
| Registro tardío | Mostrar “Registrada después de su horario” cuando lo indique el servidor. |
| Datos ocultos | Una cancelada de paciente de baja muestra esa condición, sin nombre ni enlace. Si el servidor devuelve una cita confirmada sin nombre, no inventar una baja. |
| Acceso | Consultar agenda o cita exige consultar agenda y pacientes; agendar exige agendar citas y consultar pacientes. Conservar sesión y protección del formulario. |

Usa las tres funciones de Application del contrato: consultar agenda, obtener cita y agendar. No accedas a la base desde una página. Los textos y decisiones de guardado vienen del servidor; confirma éxito solo con su respuesta correcta.

## 4. Decisiones de producto ya resueltas

| Tema | Regla aprobada por Lucía |
| --- | --- |
| Zona horaria | Siempre Hermosillo, aunque el equipo esté en otro país. La página envía fecha y horas locales; no las convierte. |
| Días y límites | Los siete días, inicio desde las 9:00 y fin máximo a las 19:00 del mismo día. |
| Minutos | Puede empezar en cualquier minuto, sin segundos. Antes de las 15:00 dura una hora; desde las 15:00, media hora o una hora. 14:30–15:30 sí está aprobado. |
| Fecha pasada | Permitida con las mismas reglas; el servidor indica si se registró tarde. |
| Pago | Nace pendiente. No añadir campo para cambiarlo: eso corresponde a B04. |
| Otros cambios | Reprogramar y cancelar: B05. Nuevas bajas y recuperaciones: B06. Sin montos, “solo particular” ni cancelación automática. |

No hay preguntas de producto B03 pendientes de las decisiones D1–D13. Si aparece una contradicción real, consultarla con Lucía antes de cambiar una regla. Las confirmaciones de producto pendientes de B02 se siguen en su plan y no se dan por resueltas aquí.

## 5. Qué debe leer tu IA

| Orden | Documento | Para qué |
| --- | --- | --- |
| 1 | [AGENTS](../../AGENTS.md), secciones 1–3 | Reglas, permisos, autoría y límites. |
| 2 | [Plan B03](TherapEase-etapa09-B03.md) y [propuesta](TherapEase-etapa09-B03-propuesta.md) | Alcance y decisiones aprobadas. |
| 3 | [Contrato B03](TherapEase-etapa09-B03-contrato-para-paginas.md) | Funciones, campos y respuestas exactas. |
| 4 | [Contrato B02](TherapEase-etapa09-B02-contrato-para-paginas.md) | Reutilizar lista, búsqueda y ficha autorizada de pacientes. |
| 5 | [08B](../requirements/TherapEase-etapa08-casos-historias.md) y [08C](../requirements/TherapEase-etapa08-modelos-interfaces.md) | HU08–HU09, CU05–CU06, pantallas y navegación. |
| 6 | [Plan de pruebas](../testing/TherapEase-09-plan-de-pruebas.md) | Reutilizar los casos de cita y agenda; no copiarlos en otra tabla. |

## 6. Próxima entrega

Conecta agenda, selección de paciente, formulario y detalle con datos ficticios. Incluye tus unitarias: obligatorios, validación, rechazos que conservan captura, confirmación de guardado, reenvío, agenda vacía, filtros y horas de Hermosillo. Las pruebas de navegador y servidor de Lucía complementan las tuyas.

Entrega un PR con versión identificable, CI correcta y pasos para recorrerlo. Mantén fuera cambios de pago, cancelación y otros bloques. Lucía organiza la revisión ajena y decide la fusión. No se cierra B03 por preparar esta guía. Solo entorno local y datos ficticios, sin nube ni gastos.
