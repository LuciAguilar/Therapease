# TherapEase — Respuestas de la psicóloga y ajustes de requisitos

**Revisión documental del 14-oct-2026.** Lucía aportó las imágenes de la entrevista y autorizó actualizar todos los documentos afectados. No hay fecha de realización visible en las hojas: el 14-oct es la fecha de recepción/revisión en este chat, no una fecha atribuida a la entrevista.

> **✅ = respuesta recibida y legible o actualización documental realizada. ⏳ = aclaración, decisión de alcance/técnica, implementación o prueba pendiente.** No confundir la respuesta de producto con autorización de Construcción, revisión humana de código ni prueba ejecutada. B00/LCA/R04 siguen abiertos; solo datos ficticios y cambios locales.

## 1. Fuentes y pendiente que se resuelve

Las fuentes originales son tres páginas de un mismo cuestionario, conservadas localmente por Lucía: Entrevista 1, Entrevista 2 y Entrevista 3. Se conservan sin modificación y solo localmente; no se publican automáticamente las imágenes ni datos de la entrevista. Las anotaciones manuscritas se distinguen de las casillas marcadas.

| Documento / ubicación anterior | Qué estaba pendiente | Estado actual |
| --- | --- | --- |
| 08B §5; 08C §3 y §5 | Campos, tutor, homónimos, pantallas y reglas de producto. | ✅ Respuestas recibidas; ⏳ concreción de campos/reglas y cambios de alcance indicados abajo. |
| 08D §1 y §4 | Validar corte B00–B04, prioridades posteriores y campos del directorio. | ✅ Conformidad al recorrido de diciembre y prioridades legibles; ⏳ capacidad, montos y prioridad de bajas sin numerar. |
| B00 §3 y §7 | Citas seguidas, pago inicial, baja con citas activas, retirar ámbito y corregir pagos. | ✅ Preferencias recibidas; ⏳ cambio del coordinador de baja, recuperación tras retirar ámbito y pruebas. |
| Plan de pruebas §6 | Decisiones de producto que cambian casos. | ✅ Se actualizan respuestas; ⏳ todas las pruebas no ejecutadas conservan su estado. |
| B01 §3, §6 y checklist local | Confirmar necesidad de `NombreCompleto`/`Correo` y pantallas de acceso. | ✅ Ambos datos solicitados y conformidad general de pantallas; ⏳ finalidad/validación/contratos y seguridad de la interfaz. |
| Requisitos §2 y R04; 08D D04 | Ámbitos escolar/particular, procedencia, acceso y conservación de datos reales. | ✅ Preferencia particular, papel, acceso solo titular y práctica declarada de 5 años; ⏳ decisión de alcance, revisión de R04, conservación/eliminación y controles previos. |

## 2. Transcripción y efecto de cada respuesta

| Estado de respuesta | Página / pregunta | Respuesta observada | Cambio documental y pendiente real |
| --- | --- | --- | --- |
| ✅ | 1 · Datos del paciente | Marcados nombre completo, teléfono, correo, fecha de nacimiento, escuela y grupo. Manuscrito «Edad» en Otro, casilla sin marcar. | Campos solicitados registrados en HU04–HU06/CU03–CU04 y 08C. ⏳ Obligatoriedad, formatos, finalidad de escuela/grupo en atención particular y si edad se calcula desde nacimiento; no duplicar edad persistida por suposición. |
| ✅ | 1 · Escolar e independiente a la vez | Marcado No; manuscrito «solo particular». | Preferencia recibida que contradice la base escolar/independiente/ambos. ⏳ Lucía decide si se elimina el ámbito escolar y cómo se simplifica modelo/contratos/pruebas. No retirar campos/restricciones del código por esta actualización. |
| ✅ | 1 · Menores y tutor | Marcado «Sí, siempre». | Contacto/datos de madre, padre o tutor para menores solicitado. ⏳ Campos, titular/relación, qué es obligatorio, validación y condiciones de datos reales; no inventar identificación o consentimiento. |
| ✅ | 1 · Homónimos | Marcada fecha de nacimiento; manuscrito «Apellidos» en Otro, casilla sin marcar. | Distinguir coincidencias con fecha de nacimiento y apellidos solicitados; conservar identificador interno. ⏳ Composición del nombre; no convertir nombre/fecha en clave única ni rechazar homónimos legítimos. |
| ✅ | 1 · Lista de pacientes | Marcado solo Nombre. | Columna inicial solicitada: nombre. Búsqueda mínima B02 permanece; directorio aparte B07. ⏳ Cómo distinguir coincidencias al seleccionar sin agregar teléfono/cita/ámbito a la lista por suposición. |
| ✅ | 1 · Baja con citas agendadas | Marcado «Cancelar sus citas automáticamente». | Preferencia de producto recibida. ⏳ Cambia la regla provisional B00 que rechaza la baja con citas activas: requiere aprobación de Lucía, operación atómica baja/cancelaciones/eventos, concurrencia y conservación de pago. Recuperar paciente no reactiva citas automáticamente. |
| ✅ | 1 · Retirar un tipo de atención | Marcado «Permitirlo; las citas conservan su tipo original». | Conservar el ámbito histórico de citas; no modificar citas por retirar ámbito. ⏳ Si sigue aplicando tras «solo particular» y qué pasa al recuperar/reprogramar una agendada con ámbito retirado; la pregunta no resuelve ese alterno. |
| ✅ | 2 · Citas seguidas | Marcado Sí. | Se resuelve la duda sobre contiguas: se permiten sin hueco. El mecanismo `[inicio, fin)` existente coincide; no es una prueba nueva. ⏳ Horario/duración de la nota final y pruebas del servicio/formulario. |
| ✅ | 2 · Solo pagado/pendiente | Marcado «No, necesito registrar montos». | Solicitud de ampliación de HU13/CU09/B04, incompatible con la exclusión aprobada de montos. ⏳ Lucía decide alcance/corte; concretar moneda, significado del monto, precisión, validación, correcciones y auditoría sin valores privados. No inferir abonos, cobros en línea, facturas, contabilidad ni nuevos estados. |
| ✅ | 2 · Pago inicial | Marcado Pendiente. | Se resuelve el valor inicial del estado: Pendiente. ⏳ Si se aprueban montos, su valor inicial y vínculo con pagado/pendiente requieren especificación separada. |
| ✅ | 2 · Pago de cita cancelada | Marcado Sí. | Se solicita permitir corrección autorizada del pago en cita cancelada. Cancelar no modifica el pago por sí mismo. ⏳ Implementación y pruebas de permisos/concurrencia/evento; montos aún pendientes de decisión. |
| ✅ | 2 · Pago de cita de baja | Marcado No. | Se solicita denegar cambios de pago mientras la cita esté de baja; conservar consulta autorizada y valor guardado. ⏳ Implementación y pruebas de envío directo. |
| ✅ | 2 · Quién usa el sistema | Marcado «Solo yo». | la usuaria es la única usuaria operativa prevista. No incorporar asistente/capturista ahora. Las cuatro identidades ficticias de equipo/QA no son cuatro profesionales operativos ni acceso real del equipo. |
| ✅ | 2 · Datos del usuario | Marcados Nombre completo y Correo. | La necesidad de ambos se registra en B01/08C; ya no pedir si los desea. ⏳ Finalidad, obligatoriedad, validación, edición/consulta y minimización. Solicitar correo no autoriza recuperación por correo ni proveedor de envío. |
| ✅ | 2 · Primer avance de diciembre | Marcado «Me parece bien» para acceso, registro/búsqueda de pacientes, agenda y pago. | Conformidad de producto al recorrido B01–B04; B00 sigue como puerta técnica. ⏳ Montos cambian el alcance de pago, capacidad real y planificación por el equipo. No garantiza completar todo el 04-dic ni autoriza Construcción. |
| ✅ / ⏳ | 2 · Prioridades después | Se leen 1 en Expediente y notas, 2 Reportes, 3 Directorio y 4 Reprogramar/cancelar. La fila de baja/recuperación no tiene número legible. | Registrar ese orden de preferencia sin renumerar B/HU/CU. ⏳ Dulce concreta secuencia viable con dependencias/capacidad y obtiene prioridad de bajas; las capacidades clínicas continúan futuras. |
| ✅ | 2 · Pantallas propuestas | Marcado «Están bien». | Recibido visto bueno general a lo mostrado. No consta versión exacta del material mostrado. ⏳ Dulce identifica versión y ajusta campos/cambios de esta entrevista; no se reintroduce la pantalla de auditoría retirada ni se acredita UX/seguridad/navegador. |
| ✅ | 3 · Expediente | Marcados motivo de consulta, antecedentes, notas de cada sesión y plan de tratamiento. Manuscrito «Notas iniciales / Notas de cada cita», casilla Otro sin marcar. | Contenido solicitado para HU-F01–F03/CU-F11–F12; notas iniciales y por cita registradas como anotación. ⏳ Modelo/vínculo concreto, correcciones, acceso/retención; fuera del primer avance. |
| ✅ | 3 · Reportes | Marcados citas por mes y pacientes atendidos. «Pagos pendientes» sin marcar. Manuscrito «Pacientes en lista de espera», casilla Otro sin marcar. | Tipos solicitados para HU-F04/CU-F13; no inferir reporte de pagos pendientes. Lista de espera es solicitud adicional futura: ⏳ aclarar qué datos/proceso existen antes de inventar entidad, HU/CU o comprometarla. |
| ✅ | 3 · Origen de información actual | Marcado Papel. | Procedencia declarada incorporada a R04. ⏳ No autoriza digitalización, importación ni migración de expedientes reales. |
| ✅ | 3 · Conservación actual | Marcado 5 años. | Práctica declarada recibida; no equivale a política técnica/jurídica aprobada. ⏳ Categorías, inicio del cómputo, archivo/eliminación, respaldos y revisión antes de datos reales. No aplicar plazo automático a toda tabla. |
| ✅ | 3 · Quién ve datos reales | Marcado «Solo yo». | Preferencia de acceso exclusivo de la usuaria. ⏳ Formalizar permisos operativos/soporte dentro de R04; Superusuario no recibe acceso clínico real por su nombre de rol. Demostración y QA siguen ficticios. |
| ✅ | 3 · Al vencer plazo o pedir supresión | Marcado «Lo consulto antes». | No hay aprobación de borrado definitivo ni de archivo indefinido. ⏳ Procedimiento y decisión antes de uso real; baja lógica no resuelve eliminación. |
| ✅ / ⏳ | 3 · Horario y medias horas | Lucía aclaró el 14-oct: la anotación dice 3:00, no 8:00. De 9:00 a 15:00 son sesiones de 1 h; desde 15:00 puede haber sesiones de 30 min, en la franja hasta 19:00. | ✅ Duración/franja recibidas; ⏳ si 19:00 es último inicio o término, días y citas que crucen 15:00. Conservar America/Hermosillo; «puede» no impone 30 min a todas las citas de la tarde. |
| ✅ | 3 · Inicio visual | Manuscrito «Incluir un logotipo en pantalla de inicio». | Solicitud registrada para Dulce. ⏳ Logotipo autorizado/archivo, ubicación y accesibilidad; no generar ni publicar uno por este encargo documental. |

## 3. Qué se palomea y qué requiere autorización o concreción

| Estado | Trabajo | Responsable / condición |
| --- | --- | --- |
| ✅ | Leer/transcribir las tres páginas y actualizar documentación y checklists afectados. | Autorización de Lucía en este chat; sin código, pruebas nuevas, publicación ni nube. |
| ✅ | Registrar respuestas sobre contiguas, pago inicial, correcciones de pago, campos, tutor, homónimos, lista, uso personal, pantallas y preferencias futuras. | Respuesta recibida de producto; las filas de implementación/QA permanecen ⏳. |
| ⏳ | Autorizar incorporación de montos y su corte, tras concretar reglas. | Lucía decide con Dulce/Miguel; afecta requisitos §2, HU13/CU09/B04, modelo de Cita, persistencia/migración y pruebas. Sigue vigente la base sin montos hasta esa decisión. |
| ⏳ | Autorizar simplificación a atención solo particular. | Lucía decide con el equipo; afecta ámbitos de Paciente/Cita y contratos/reglas/pruebas. Escuela/grupo también fueron pedidos: aclarar finalidad antes de eliminarlos o asumir contexto escolar. |
| ⏳ | Autorizar sustitución de la baja provisional por cancelación automática. | Lucía aprueba mecanismo/impacto propuestos por Miguel, nota ADR-04 y pruebas Q03/Q06/Q07/Q08/Q20. No retirar protección PostgreSQL; cancelar conserva pago y cada cambio/evento se confirma de forma coherente. |
| ⏳ | Concretar datos nuevos de Usuario y Paciente/Tutor. | Dulce obtiene detalles que faltan; Miguel propone contratos/modelo y migración si corresponde; Lucía revisa privacidad/seguridad. Registrar necesidad no acredita desarrollo. |
| ⏳ | Concretar si 19:00 es último inicio o término, prioridad de bajas, versión de pantallas y lista de espera. | Dulce con la usuaria; no volver a preguntar respuestas que sí están legibles. |
| ⏳ | Aprobar actualización técnica del modelo/diagrama conforme a cambios de alcance decididos. | 08C y StarUML conservan base candidata; se registra impacto en sus notas. No fingir que la entrevista cambia automáticamente esquema o capacidades. |
| ⏳ | Resolver R04 y autorizar uso real. | Lucía con la usuaria y revisión especializada cuando corresponda; 5 años/solo titular/papel son insumos, no cierre. Mantener TOTP/recuperación y temporal 24 h como requisitos previos. |
| ⏳ | Autorizar Construcción e implementación por incremento. | Decisión separada de Lucía. B01 mantiene alcance de identidad, con datos de Usuario solicitados; montos/bajas/ámbitos no entran en B01 por esta entrevista. |

## 4. Reglas de propagación y evidencia

Se actualizan las fuentes Markdown vigentes, sus resúmenes de Obsidian, las copias documentales del clon de revisión y los documentos académicos Word afectados. B01 compartido y checklist personal mantienen secciones 1–9. Se incorpora una actualización al PDF de pantallas; el diseño previo se identifica como antecedente, no como interfaz ya desarrollada. Los comentarios/documentación del modelo StarUML identifican la entrevista y los cambios por decidir sin alterar entidades o relaciones como si estuvieran aprobados.

Se conservan las entrevistas originales, los cuestionarios vacíos y los PDF/storyboards de escritorio de 2025 como fuentes históricas. También se conservan los snapshots de QA, TRX, evidencia de código probado y conteos históricos: **no se reescribe una prueba ejecutada para adjudicarle una regla nueva**. El comportamiento actual de baja de PR #5 continúa siendo rechazo con cita activa hasta cambio autorizado, implementado y probado.

La revisión de Claude de este ajuste aún no se ha recibido. Miguel o Dulce revisa pruebas de Lucía y Lucía conserva aprobación final. Los cambios son locales en la misma `revision/etapa09`; no se publica ni se agrega el checklist personal al clon.

## 5. Aclaración del horario aportada por Lucía

✅ El 14-oct Lucía confirmó que la segunda línea dice **3:00 p. m.**: de **9:00 a 15:00** hay sesiones de **una hora**; desde **15:00** puede haber sesiones de **media hora**, en la franja indicada hasta **19:00**. ⏳ Precisar último inicio o fin a las 19:00, días y conducta de una cita que cruce las 15:00; no convertir «puede» en duración fija obligatoria para toda la tarde. Se incorpora a CU05–CU07/B03/B05 y a sus casos existentes cuando se autoricen.

## 6. Control de propagación y revisión local

| Estado | Documentos actualizados | Alcance del ajuste |
| --- | --- | --- |
| ✅ | Requisitos, 08B, 08C y 08D | Respuestas recibidas, aceptación/alternos afectados, propuestas de alcance y prioridades sin renumerar identificadores. |
| ✅ | B00, B01 compartido, plan de pruebas y guía del equipo | Pendientes de respuesta sustituidos por respuesta recibida y detalle/decisión/ejecución restantes. B01 conserva secciones 1–9. |
| ✅ | AGENTS, ADR-01…05 y ADR-06 | Nota posterior de impacto; reglas/código probado se conservan hasta decisión. No introducir tecnología ni cambiar el mecanismo vigente automáticamente. |
| ✅ | ESTADO, REVISIONES, prompt maestro y avance Scrum | Continuidad, límites y evidencia documental. No se declaran Sprints, horas, pruebas ni revisiones nuevas. |
| ✅ | Resúmenes aplicables de Obsidian, informes PR01/PR05 y notas/checklists de Lucía | Remisión a entrevista y actualización posterior; historia de QA preservada. Checklist personal fuera del clon publicable. |
| ✅ | Copias de documentación en revision/etapa09 y README técnico | Enlaces ajustados, comandos técnicos conservados y convenciones de anonimización existentes respetadas. Sin commit/push ni actualización de PR. |
| ✅ | Cuatro Word académicos: plan de desarrollo, informe de avance, anexos A y B | Adenda de entrevista de dos páginas por archivo; aviso de adenda en portada y contenido anterior conservado como corte histórico. |
| ✅ | PDF de pantallas | Página inicial con respuestas/ajustes; borrador anterior conservado como antecedente. Dos páginas; no se reintroduce Auditoría como pantalla vigente. |
| ✅ | StarUML local y copia de revisión | Solo documentación del proyecto con impacto de entrevista; actores, entidades, relaciones y posiciones de cada copia conservados exactamente. Modelo detallado de cambios aún pendiente. |
| ✅ | Fuentes originales y evidencia histórica | Entrevistas, cuestionarios vacíos, PDF/storyboards de escritorio y snapshots/ejecuciones QA conservados. No modificar ni acreditar de nuevo sus resultados. |

**Verificación realizada:** revisión de estructura/tablas/enlaces Markdown y comparación estructural del modelo; render con inspección de todas las páginas de los cuatro Word (8, 8, 8 y 9 páginas, respectivamente anexos A/B, informe y plan) y del PDF de pantallas (2). Solo verificación documental; no pruebas de software. Registro local de control en `tmp/entrevistas-14oct`, con originales preservados y huellas de fuentes/resultados. Revisión de Claude y decisiones de alcance aún pendientes.
