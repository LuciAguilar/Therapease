# TherapEase — Etapa 08: historias y casos de uso

**Decisión de producto — Dulce, 08-oct-2026; comunicada por Lucía:** el directorio sale de CU03 y pasa a funcionalidad posterior como pantalla aparte. **La búsqueda de pacientes desde Pacientes se queda en el primer avance de diciembre**, junto con el registro. El directorio posterior mostrará la lista completa de pacientes vigentes por defecto y ofrecerá filtro de bajas solo con permiso; sus campos visibles siguen por validar. B07 conserva la referencia de backlog para esa funcionalidad posterior, sin crear un CU nuevo ni comprometerla para diciembre. Este cambio no elimina Q20 ni la consulta/recuperación autorizada de CU04, y no modifica permisos, auditoría, arquitectura ni la aprobación histórica de 08.

**Continuidad del 01-oct-2026:** Lucía aprobó el plan B00 corregido, ADR-06 y la consulta Q03 sin pantalla; confirmó la fidelidad de ADR-01…05 corregidos, eligió repositorio público y documentación seleccionada, sin antecedentes de escritorio. Ver [guía de equipo](../../README.md). LCA y R04 siguen abiertos; no se acredita revisión humana ni pruebas aún pendientes.

**Fecha:** 28-sep-2026; estado actualizado el 30-sep. **08A v0.3:** aprobada por Lucía tras auditoría APROBADO de Claude. **08B v0.4:** auditada APROBADO por Claude y aprobada por Lucía el 28-sep, con la aclaración opcional de CU11 incorporada sin cambiar permisos. **08C v0.5 y 08D v0.2:** aprobadas; etapa 08 cerrada documentalmente por Lucía el 30-sep. Documento de planeación, sin código ni estructura de implementación. Elaboración/LCA sigue abierta hasta B00 en 09 autorizada.

**Aclaración terminológica de Lucía:** se usan **Usuario** y **Rol** para la identidad y los permisos. Las referencias anteriores a «cuenta» en Q03 y documentos cerrados se interpretan como usuario. La ampliación v0.3 detalla caminos ya acordados en 07B y propone verificaciones adicionales. El ajuste v0.4 separa visual y textualmente el cambio de contraseña propia de la administración de otros usuarios.

**Adenda de 09, decidida por Lucía el 30-sep-2026:** Miguel implementa reglas críticas y seguridad, y escribe unitarias de su código; Lucía diseña, escribe y ejecuta sus pruebas de integración y seguridad Q03/Q06/Q07/Q20. Dulce confirmó que no se hará pantalla de auditoría; **HU14/CU10 quedan fuera de la interfaz de 09**, sin eliminar los eventos ni Q03. La adenda inicial integraba el directorio en HU05/CU03 (sustituido por la decisión del 08-oct): vigentes por defecto y filtro de bajas solo con permiso; campos visibles pendientes de Dulce con Usuaria. Esta adenda no atribuye una nueva auditoría de 08.

## 1. Límite y convenciones de 08B

- **Primer avance implementable propuesto:** acceso, pacientes, citas y estado de pago pagado/pendiente. Su orden definitivo es de Dulce, Product Owner. Los cuatro usuarios de demostración y todos los registros serán ficticios. No se procesan cobros, montos ni facturas.
- **Capacidades futuras documentadas ahora:** historial clínico, notas y reportes. Lucía pidió sus historias y casos en 08; no comprometió su implementación en el primer avance. Su contenido y prioridad requieren validación de Dulce con Usuaria. Capturista: permisos documentados, incorporación diferida (Q17).
- **R04 sigue abierto:** ningún dato real ni acceso clínico real hasta acordar procedencia, permisos, tratamiento escolar/particular, conservación y eliminación definitiva con Usuaria y revisar obligaciones oficiales vigentes. Por decisión de Lucía del 27-sep, 08D incluirá investigación documental con fuentes oficiales vigentes sobre datos de salud, menores y alojamiento fuera de México; la tarea no resuelve R04 por sí sola. Los criterios futuros son borradores, no autorización para programar o usar datos reales.
- **Origen:** «§2» remite al alcance de `TherapEase-requisitos-arquitectonicos.md` v0.4; Q/R a sus identificadores; «Lucía 26-sep» a la aclaración registrada en `ESTADO.md`. Cada HU del avance y cada capacidad futura delimitada nombra su CU; la opción HU-F07 por correo deja su CU pendiente hasta que se apruebe ese canal. Los CU-F son futuros. La matriz completa Q01–Q20, R01–R05 y ADR corresponde a 08D.

### 1.1 Base de datos y reglas acordadas/propuestas desde 08A

Paciente: identificador interno, nombre, ámbito escolar/independiente/ambos, contacto opcional y condición vigente/baja. Cita: identificador, paciente, un ámbito para esa atención, inicio y fin, estado agendada/cancelada, pago pagado/pendiente y condición vigente/baja. Los campos y la ubicación del ámbito son **propuestas pendientes de Dulce con Usuaria**; el contacto será ficticio en la demostración. Se preguntará si un paciente escolar necesita contacto de tutor.

**Cita activa = agendada y vigente.** Solo ella cuenta para cruces Q06; dos citas contiguas sin superposición se proponen como permitidas. Cancelar mantiene cita consultable y pago intacto. Dar de baja oculta del uso ordinario; recuperar una agendada exige que el paciente esté vigente y comprobar de nuevo el cruce. Si falla cualquiera de esas condiciones, se rechaza y avisa. Recuperar una cancelada conserva su estado cancelado y su pago. El éxito se comunica solo después del guardado; si el resultado es incierto se avisa y se consulta el estado antes de reintentar (Q08).

### 1.2 Permisos, incluida la extensión futura

| Operación | Usuaria | Tres superusuarios | Capturista futuro |
| --- | --- | --- | --- |
| Pacientes: consultar, registrar y actualizar | Sí | Sí, demostración ficticia | Sí, propuesto |
| Citas: consultar, agendar, reprogramar y cancelar | Sí | Sí, demostración ficticia | Sí, propuesto |
| Consultar y cambiar estado de pago | Sí | Sí, demostración ficticia | No |
| Dar de baja, consultar bajas y recuperar pacientes/citas | Sí, propuesta 08A | Sí, demostración ficticia | No |
| Consultar auditoría (histórico 08; sin pantalla en 09) | Alcance histórico de 08; la consulta técnica de B00 aprobada el 01-oct está reservada a superusuario activo | Consulta técnica de solo lectura de B00 para superusuario activo; aprobada por Lucía el 01-oct, pendiente de implementación | No |
| Administrar usuarios, roles y restablecimientos | No | Sí | No |
| Historial clínico y notas **(futuro)** | Sí, propuesta | Solo con datos ficticios; acceso real pendiente de R04 | No, propuesta |
| Reportes **(futuro)** | Sí, propuesta | Solo con datos ficticios; acceso real pendiente de R04 | No, propuesta pendiente de validar |

Lucía aprobó 08A como propuesta documental; Dulce aún debe validar con Usuaria los permisos operativos propuestos. Toda autorización se comprobará en el servidor (Q01/Q19). Cada usuario nuevo recibe una contraseña temporal que debe cambiarse al entrar. Un usuario desactivado o con contraseña restablecida pierde sus sesiones abiertas; al cambiar la propia contraseña se pide la actual y se cierran las demás sesiones. Los cambios de usuarios y roles se registran sin contraseñas; no se puede desactivar ni quitar el rol al último superusuario activo. La creación inicial del primer superusuario y la recuperación cuando ninguno pueda entrar se concretarán al autorizar 09, sin correo ni contraseña fija en el código.

## 2. Historias candidatas del primer avance

**HU01 — Acceder y salir (CU01).** Como usuaria registrada, quiero entrar y cerrar sesión para proteger la información. Origen: §2, Q01, Q02, Q19; mecanismo 07B. **CA:** (1) Con credenciales válidas, usuario activo y sin bloqueo, entra con su rol; sin sesión o permiso, pacientes, citas y pago se deniegan incluso por acceso directo. Una contraseña temporal solo permite completar su cambio antes de entrar a esas funciones. (2) Al salir, el sello de seguridad renovado hace que todas sus sesiones dejen de autorizar operaciones en la siguiente solicitud, conforme a 07B. (3) Al quinto fallo se bloquea el acceso durante 15 minutos; una contraseña correcta durante el bloqueo tampoco abre sesión. Respuestas de error genéricas no revelan si existe el usuario. (4) Una sesión vencida, revocada, de usuario desactivado o con rol sin permiso no revela información al solicitar una función protegida. (5) Si falla la validación del servidor, no se abre sesión ni se confirma acceso. **Miguel implementa el control; Lucía lo prueba en 09:** alcance del contador de fallos por usuario, reinicio tras éxito o vencimiento del bloqueo y conducta ante solicitudes simultáneas, sin debilitar el umbral acordado.

**HU02 — Administrar usuarios y roles (CU02).** Como superusuario, quiero crear y desactivar usuarios personales y asignar roles para controlar el acceso. Origen: §2, Q01, Q19. **CA:** (1) Solo un superusuario activo realiza estas acciones; el sistema registra actor, momento, usuario afectado y acción sin contraseña. Si no puede registrar el evento obligatorio, no confirma el cambio. (2) Cada usuario nuevo recibe contraseña temporal y exige cambiarla al primer acceso; se rechazan nombre de usuario duplicado y rol no permitido. (3) Desactivar invalida sesiones y niega nuevos accesos. Un cambio de rol debe surtir efecto en la siguiente solicitud autorizada; Miguel implementará la invalidación o actualización de sesiones; Lucía probará su efecto para impedir permisos anteriores. (4) Se rechaza desactivar o degradar al último superusuario activo, incluso si dos cambios concurren.

**HU03 — Restablecer contraseña ajena (CU02).** Como superusuario, quiero restablecer el acceso de otro usuario sin correo para que su titular pueda volver a entrar. Origen: §2, Q02, Q19; acuerdo 07B. **CA:** (1) Tras verificar la solicitud por un procedimiento acordado, se entrega una contraseña temporal por canal seguro y se exige cambiarla al entrar; no se presupone correo ni se codifica una clave fija. (2) El restablecimiento invalida las sesiones anteriores y queda auditado sin registrar la contraseña; si falla la invalidación necesaria no se comunica como completo. (3) La recuperación sin ningún superusuario disponible queda como procedimiento pendiente para 09. **Pendiente de Lucía y Miguel:** decidir si un restablecimiento autorizado levanta también un bloqueo temporal; hasta entonces no se presupone que lo haga.

**HU04 — Registrar paciente (CU03).** Como Usuaria, quiero registrar una sola ficha por paciente para organizar su atención en uno o ambos ámbitos. Origen: §2, Q05, Q13. **CA:** (1) Con nombre y al menos un ámbito propuestos, el guardado confirmado crea un identificador y deja la ficha consultable. (2) Antes de crear, se muestran posibles coincidencias para elegir una ficha existente; un homónimo distinto puede registrarse. (3) La acción queda auditada y solo usa datos ficticios en la demostración.

**HU05 — Buscar y consultar paciente (CU03).** Como la usuaria, quiero localizar un paciente para revisar su ficha sin duplicarla. Origen: §2, Q13, Q20. **CA:** (1) Desde Pacientes se pueden buscar fichas vigentes y distinguir homónimos por identificador y datos autorizados. (2) Se abre la ficha correcta con sesión y permiso; sin ellos no se revela. (3) Las bajas quedan fuera de la búsqueda ordinaria; su consulta autorizada se mantiene según CU04/Q20. (4) Los campos visibles siguen pendientes de Dulce con la usuaria. **Esta búsqueda forma parte del primer avance de diciembre; la pantalla de directorio completo queda fuera de CU03 y para después.**

**HU06 — Actualizar paciente (CU04).** Como Usuaria, quiero corregir la ficha de un paciente para mantener sus datos vigentes. Origen: §2, Q03, Q07, Q08. **CA:** (1) Un cambio válido se guarda y audita por usuario actor, momento, registro/campo y hecho, sin copiar valores privados a la auditoría. (2) Si otra persona modificó la ficha desde que se abrió, se avisa antes de sobrescribir; no se pierde silenciosamente el cambio. (3) Un fallo de respuesta no se presenta como guardado confirmado.

**HU07 — Dar de baja y recuperar paciente (CU04).** Como Usuaria, quiero retirar una ficha del uso ordinario y recuperarla si corresponde para conservar su información. Origen: Q20. **CA:** (1) La baja conserva la ficha y la saca de consultas ordinarias; solo roles autorizados la ven y recuperan. (2) Se propone impedir la baja mientras existan citas activas del paciente, sin cancelarlas automáticamente; Dulce debe validarlo con Usuaria. (3) Baja y recuperación quedan auditadas.

**HU08 — Consultar agenda (CU05).** Como Usuaria, quiero ver mis citas para ubicar horarios disponibles y consultar las canceladas. Origen: §2, Q09, Q13, Q20. **CA:** (1) La agenda ordinaria muestra citas activas en America/Hermosillo, con inicio, fin, paciente y ámbito autorizados. (2) Una cita cancelada sigue consultable en el historial o vista correspondiente; una dada de baja no aparece en la agenda ordinaria. (3) Sin citas para el periodo se muestra un estado vacío claro.

**HU09 — Agendar cita (CU06).** Como Usuaria, quiero asignar un intervalo a un paciente para reservar su atención. Origen: §2, Q06, Q08, Q09. **CA:** (1) Paciente vigente, ámbito permitido e inicio anterior al fin permiten intentar el guardado. (2) Si existe cruce con cualquier cita activa de Usuaria, aunque sea de otro ámbito, se rechaza y se señala el conflicto; dos intentos simultáneos no pueden guardar ambos. (3) El éxito solo se muestra tras confirmar el guardado y se audita. (4) Si se agenda desde un equipo con otra zona horaria, la cita conserva la hora acordada en America/Hermosillo al volver a consultarla.

**HU10 — Reprogramar cita (CU07).** Como Usuaria, quiero cambiar el horario de una cita activa para atender un cambio de agenda. Origen: §2, Q03, Q06, Q07, Q09. **CA:** (1) El nuevo intervalo se guarda solo si no cruza otra cita activa. (2) Si alguien cambió la cita entretanto, se avisa antes de sobrescribir. (3) Se conserva el estado de pago y se audita el cambio; si falla, permanece el horario anterior. (4) Al reprogramar desde un equipo con otra zona horaria, el nuevo horario acordado en America/Hermosillo permanece igual al consultarlo después.

**HU11 — Cancelar cita (CU07).** Como Usuaria, quiero cancelar una cita para liberar su horario sin perder el registro. Origen: §2, Q03, Q06, Q20. **CA:** (1) Una cita activa pasa a cancelada y deja de impedir nuevas reservas en ese intervalo. (2) Sigue consultable, no queda dada de baja y conserva exactamente su estado de pago. (3) La cancelación queda auditada.

**HU12 — Dar de baja y recuperar cita (CU08).** Como Usuaria, quiero ocultar una cita del uso ordinario y recuperarla con permiso para no borrar su registro. Origen: Q06, Q20. **CA:** (1) La baja no borra la cita ni altera estado de pago o de cancelación. (2) Al recuperar una cita agendada, su paciente debe estar vigente y se comprueba de nuevo el cruce; si el paciente está de baja o hay cruce, se rechaza, se avisa y la cita permanece de baja. (3) Recuperar una cancelada la deja cancelada; ambas operaciones se auditan.

**HU13 — Consultar y cambiar estado de pago (CU09).** Como Usuaria, quiero marcar por cita pagado o pendiente para saber qué estados debo revisar. Origen: §2, Q03, Q07, Q08. **CA:** (1) Solo se usan esos dos valores, sin monto, abono, método ni procesamiento de cobro. (2) Un cambio autorizado confirmado se guarda y audita; una edición concurrente avisa antes de sobrescribir. (3) Cancelar o dar de baja una cita no modifica su pago. (4) El capturista futuro y cualquier usuario sin permiso no cambian ni consultan el pago, tampoco por URL directa. **Pendiente de Dulce con Usuaria:** si se permite corregir el pago de una cita ya cancelada o de baja.

**HU14 — Consultar auditoría (CU10; antecedente de 08, retirado de la interfaz de 09).** Como Usuaria, quiero revisar quién cambió pacientes, citas y estados de pago para aclarar modificaciones. Origen: Q03, Q04, Q19. **CA históricos:** (1) Solo roles autorizados consultan usuario actor, momento inequívoco, registro/campo y hecho del cambio; Usuaria ve cambios de pacientes, citas y pago, mientras los superusuarios también ven eventos de usuarios en la demostración ficticia. (2) No se muestran contraseñas, contactos ni valores clínicos en auditoría o registros técnicos. (3) La interfaz no permite modificar o borrar los eventos; las acciones sobre usuarios y roles también quedan registradas. **09:** no se desarrolla la pantalla; la captura y comprobación de Q03 permanece. B00 concreta una consulta técnica de solo lectura para superusuario activo, autorizada por el servidor y aprobada por Lucía el 01-oct; implementación y pruebas pendientes.

**HU15 — Cambiar contraseña propia (CU11).** Como usuario autenticado, quiero cambiar mi propia contraseña para conservar el control de mi acceso. Origen: §2, Q02, Q19; acuerdo 07B; separación solicitada por Claude en la auditoría de 08C v0.4. **CA:** (1) Usuaria, superusuario y capturista futuro solo cambian su propia contraseña; se exige la actual, salvo el flujo restringido de primera entrada con contraseña temporal. (2) Una contraseña actual incorrecta o nueva inválida no cambia la credencial y produce respuesta segura. (3) El cambio confirmado cierra las demás sesiones y registra la acción sin contraseñas; el acceso de la sesión actual depende de la validación del sello. (4) Una contraseña temporal debe cambiarse antes de entrar a pacientes, citas u otras funciones protegidas.

## 3. Historias futuras: fuera del primer avance implementable

Los siguientes criterios son **provisionales** y usan solo datos ficticios. No pertenecen al recorrido Q16 ni autorizan modelos clínicos detallados en 08C. El catálogo cubre las necesidades futuras conocidas, pero su completitud depende de que Dulce confirme con Usuaria qué historial, notas y reportes requiere. Dulce debe validar el contenido antes de convertirlo en trabajo de implementación; Lucía revisará el acceso y R04 antes de datos reales.

**HU-F01 — Consultar historial clínico (CU-F11).** Como Usuaria, quiero consultar el historial de un paciente para dar continuidad a su atención. Origen: Lucía 26-sep, R04, Q01/Q19. **CA preliminares:** acceso autorizado al paciente correcto; superusuarios solo con información ficticia; capturista sin acceso. **Pendiente:** secciones, procedencia y datos que Usuaria necesita.

**HU-F02 — Registrar o corregir historial clínico (CU-F11).** Como Usuaria, quiero registrar y corregir información del historial para conservarla actualizada. Origen: Lucía 26-sep, Q03/Q07, R04. **CA preliminares:** cambios autorizados auditados sin valores clínicos en el log; conflicto concurrente informado antes de sobrescribir. **Pendiente:** campos, correcciones admisibles, versiones y conservación.

**HU-F03 — Registrar y consultar notas (CU-F12).** Como Usuaria, quiero guardar y revisar notas de atención para recuperar lo ocurrido en cada seguimiento. Origen: Lucía 26-sep, Q03/Q19, R04. **CA preliminares:** solo personas autorizadas leen o registran notas ficticias; la operación queda auditada sin texto sensible en el evento. **Pendiente:** si se vinculan a sesión, cita o solo paciente; edición, corrección y conservación.

**HU-F04 — Consultar reportes (CU-F13).** Como Usuaria, quiero consultar reportes útiles del consultorio para revisar su actividad. Origen: Lucía 26-sep, R04, Q19. **CA preliminares:** acceso autorizado, periodo explícito, estado «sin datos» si procede; ninguna cifra monetaria se presupone. **Pendiente:** tipos de reporte, indicadores, filtros y exportación; Dulce los prioriza con Usuaria.

**HU-F05 — Trabajar como capturista (CU03, CU05–CU07, CU11).** Como capturista futuro, quiero registrar pacientes y gestionar citas dentro de mis permisos para apoyar a Usuaria. Origen: §2, Q17. **CA preliminares:** puede acceder solo a las operaciones de captura autorizadas y cambiar su propia contraseña; no ve historial, notas, pago, auditoría ni administración de otros usuarios. Su usuario y pruebas se difieren hasta autorizar el rol.

**HU-F06 — Verificar el acceso en dos pasos antes del uso real (CU-F14).** Como persona autorizada a datos reales, quiero confirmar mi acceso con una app autenticadora además de la contraseña para proteger la información. Origen: decisión de Lucía 27-sep, Q19, R04. **CA preliminares:** (1) Antes de permitir datos reales, todo usuario autorizado a ellos debe tener configurada la verificación TOTP y completar el segundo paso al entrar. (2) Un código inválido no abre sesión ni expone datos. (3) La recuperación por pérdida de la app debe definirse y probarse antes del uso real, sin presumir correo o SMS. La recuperación por correo queda como historia futura candidata HU-F07, sin servicio aprobado. La compatibilidad concreta con Identity se comprobará en 09.

**HU-F07 — Recuperar acceso por correo (opción futura; CU por definir).** Como usuario, querría poder recuperar el acceso por correo si más adelante se adopta ese canal. Origen: opción planteada por Claude y aceptada por Lucía solo como historia futura el 27-sep. **CA pendientes:** verificar identidad, limitar la vigencia y uso del medio de recuperación e invalidar sesiones según corresponda. No existe servicio de correo seleccionado, prioridad, CU detallado ni autorización de implementación; el restablecimiento del primer avance sigue en CU02 a cargo de un superusuario, sin correo.

## 4. Catálogo y flujos de casos de uso

Las relaciones entre CU describen dependencias de información, sin introducir `include/extend` sin justificación. Lucía eligió UML en StarUML para los diagramas de casos de uso y dominio de 08C. **CU11 es un objetivo independiente:** Usuaria, superusuario y capturista futuro cambian solo su propia contraseña; CU02 queda exclusivamente para la gestión de otros usuarios por superusuarios.

El recorrido **CU01 → CU03 → CU06 → CU09** es candidato para el primer alcance implementable y la comprobación Q16; Dulce conserva la decisión sobre prioridad del producto. Las fichas distinguen flujo básico y alternativos. Los ejemplos de restaurante fueron compartidos por Lucía con Codex **en el chat como referencia de detalle**, no son archivos del proyecto ni fuente de requisitos o estimaciones; el documento se entiende sin ellos.

### Alternos comunes para los CU del primer avance

Se aplican al CU indicado cuando corresponde; los alternos particulares de cada ficha precisan el resultado:

- **S1.** Sin sesión válida, con sesión revocada/caducada o sin permiso de operación: denegar en el servidor antes de leer o cambiar datos; un enlace directo no elude el control.
- **S2.** Dato inválido, registro inexistente o de baja fuera de su consulta autorizada: no guardar ni revelar información fuera del permiso; mostrar solo lo necesario para corregir.
- **S3.** Cambio concurrente de una versión, cruce de citas o carrera con una baja: no sobrescribir ni confirmar una operación inválida; informar el conflicto y permitir recargar.
- **S4.** Si falla el guardado o el evento obligatorio de auditoría, ninguno de los dos queda confirmado como éxito; Miguel implementa la atomicidad y Lucía la prueba en 09 con PostgreSQL/Testcontainers.
- **S5.** Si se pierde la respuesta después de enviar un cambio, indicar resultado incierto y consultar el estado autorizado antes de repetir, para evitar duplicados o cambios repetidos.
- **S6.** Mensajes y registros técnicos no contienen contraseñas, cookies, contactos, valores clínicos ni secretos. Las comprobaciones se hacen en el servidor aunque la interfaz oculte la acción.

**CU01 — Acceder y cerrar sesión (HU01).** Actor: Usuaria o superusuario; capturista si se incorpora. Disparador: iniciar o terminar el trabajo. Precondición para intentar entrar: aplicación disponible; el estado del usuario se comprueba durante el flujo. Postcondición: sesión autorizada con rol vigente, ningún acceso concedido, o sesiones invalidadas al salir.
- **Básico, acceso:** 1) Ingresa usuario y contraseña. 2) El sistema valida credenciales, condición activa, bloqueo y rol sin divulgar cuál falló. 3) Si debe cambiar la contraseña temporal, limita el acceso a **CU11 — Cambiar contraseña propia** y solo después permite las funciones de su rol; en otro caso abre la sesión autorizada. 4) Cada solicitud protegida comprueba sesión, sello de seguridad y permiso actual en el servidor.
- **Básico, salida:** 1) Solicita cerrar sesión. 2) El servidor renueva el sello de seguridad e invalida todas sus sesiones. 3) En la siguiente solicitud desde cualquiera de sus equipos ya no puede ver funciones protegidas.
- **Alternos:**
  - A1) Usuario desconocido o contraseña incorrecta: misma respuesta genérica, sin sesión; los fallos de un usuario existente cuentan para 07B sin revelar su existencia. Miguel revisará en 09 la protección de intentos con nombres desconocidos sin inventar un umbral aquí.
  - A2) Al quinto fallo, el usuario queda bloqueado durante 15 minutos según 07B; una contraseña correcta durante el bloqueo no abre sesión.
  - A3) Vencido el bloqueo, se vuelve a evaluar el acceso; el reinicio exacto del contador y los intentos simultáneos se comprobarán en 09 sin reducir la protección.
  - A4) Usuario desactivado o rol sin acceso: denegar y no mostrar datos; si ya tenía sesión, esta tampoco autoriza la siguiente solicitud.
  - A5) Contraseña temporal sin cambiar: solo permitir CU11 en flujo restringido; un enlace directo a pacientes o citas se deniega.
  - A6) Sesión caducada, sello revocado por salida, desactivación o restablecimiento, o cookie ausente/manipulada: denegar y solicitar nuevo acceso sin exponer datos.
  - A7) Error del servidor al comprobar identidad: no abrir sesión; registrar fallo técnico sin datos privados y mostrar mensaje seguro.
  - A8) Salida repetida o sesión ya vencida: terminar sin habilitar acceso.
  - A9) Antes del uso real, el segundo paso obligatorio se resuelve en CU-F14; una contraseña válida por sí sola no autoriza datos reales.

**CU02 — Gestionar usuarios, roles y restablecer contraseñas ajenas (HU02–HU03).** Actor: solo superusuario. Disparador: alta, cambio de rol, desactivación o restablecimiento del acceso de otro usuario. Precondición: superusuario activo. Postcondición: cambio confirmado y registrado sin secretos.
- **Básico, gestión de otros usuarios:** 1) El superusuario elige alta, rol, desactivación o restablecimiento. 2) El sistema comprueba permiso actual, usuario objetivo y regla del último superusuario. 3) Guarda el cambio y su evento de auditoría juntos, sin contraseñas ni códigos. 4) La creación entrega una contraseña temporal por procedimiento controlado pendiente de concretar; desactivación o restablecimiento invalidan sesiones. Al cambiar un rol, el nuevo permiso debe regir en la siguiente solicitud.
- **Alternos:**
  - A1) Último superusuario activo: rechazar desactivación o pérdida del rol, incluso ante dos solicitudes simultáneas.
  - A2) Nombre de usuario duplicado, rol inexistente/no permitido o datos incompletos: no crear ni cambiar.
  - A3) Usuario objetivo inexistente o ya desactivado para una operación incompatible: no confirmar cambio; no revelar su existencia a quien carezca de permiso.
  - A4) Solicitante sin sesión o sin rol de superusuario intenta gestionar a otro: denegar, incluida la URL directa.
  - A5) Restablecimiento solicitado sin verificación de la persona o sin canal de entrega acordado: no entregar contraseña temporal ni afirmar recuperación; el procedimiento concreto sigue pendiente para 09.
  - A6) Falla guardado, auditoría o invalidación necesaria de sesiones: no confirmar el cambio como completo; Miguel definirá cómo conservar la coherencia en 09.
  - A7) Rol cambiado durante una sesión: la siguiente solicitud no debe conservar permisos retirados; Miguel implementa el mecanismo y Lucía prueba la revocación.
  - A8) Sin superusuario capaz de entrar: no hay autoservicio ni recuperación por correo aprobada; el procedimiento inicial/de recuperación queda pendiente de 09. El bloqueo por intentos del CU01 no se elimina por un restablecimiento sin una regla explícita aprobada.

**CU03 — Registrar, buscar y consultar paciente (HU04–HU05).** Actor: Usuaria o superusuario; capturista futuro. Disparador: necesita localizar o incorporar un paciente. Precondición: sesión y permiso. Postcondición: ficha existente localizada o una nueva guardada y auditada con identificador.
- **Básico:** 1) Abre Pacientes y busca por los datos disponibles. 2) Revisa las coincidencias vigentes. 3) Abre la ficha correcta o captura los campos mínimos propuestos. 4) El sistema valida y guarda; la ficha queda consultable. Esta búsqueda se conserva en el primer avance de diciembre; la pantalla aparte de directorio se realizará posteriormente.
- **Alternos:**
  - A1) Coincidencia real: abrir la ficha existente y evitar el alta duplicada; un homónimo distinto puede registrarse, sin unicidad forzada del nombre.
  - A2) Falta un campo obligatorio, ámbito inválido o dato con formato inválido: señalarlo y no guardar.
  - A3) Ficha de baja: excluir de la búsqueda ordinaria de Pacientes; mantener consulta autorizada según CU04/Q20. El filtro de bajas de la pantalla aparte de directorio pertenece a la funcionalidad posterior.
  - A4) Falta sesión/permiso, incluso por enlace directo: no revelar ficha.
  - A5) Guardado o auditoría falla, o la respuesta es incierta: aplicar S4/S5; no presentar un alta no confirmada ni repetirla a ciegas.

**CU04 — Actualizar, dar de baja y recuperar paciente (HU06–HU07).** Actor: Usuaria o superusuario; capturista futuro solo para actualizar. Disparador: corregir u ocultar una ficha, o recuperarla. Precondición: ficha existente y permiso por operación. Postcondición: cambio guardado y auditado sin borrar datos.
- **Básico:** 1) Abre la ficha. 2) Edita datos permitidos. 3) El sistema comprueba que nadie la cambió entretanto. 4) Guarda y audita. Para una baja autorizada, la retira de consultas ordinarias; para recuperación, la devuelve.
- **Alternos:**
  - A1) Versión anterior: avisar y no sobrescribir; recargar antes de decidir.
  - A2) Baja con citas activas: se propone rechazar sin cancelarlas automáticamente, pendiente de Dulce con Usuaria; en 09 Miguel implementa la protección de la carrera entre agendar y dar de baja, y Lucía la prueba.
  - A3) Capturista futuro o cualquier usuario sin permiso de baja/recuperación: denegar aunque conozca el identificador.
  - A4) Ficha ya de baja al intentar actualizarla por el flujo ordinario, o ya vigente al intentar recuperarla: no efectuar un cambio silencioso; informar su condición actual.
  - A5) Ámbito que se pretende retirar mientras hay citas vinculadas: regla pendiente de Dulce con Usuaria, incluida una cita agendada de baja recuperable; no decidirlo por este CU.
  - A6) Fallo de guardado/auditoría o respuesta incierta: S4/S5; no ocultar una ficha sin confirmar el resultado.

**CU05 — Consultar agenda (HU08).** Actor: Usuaria o superusuario; capturista futuro. Disparador: revisar horarios. Precondición: sesión con permiso. Postcondición: citas del periodo consultadas sin modificar datos.
- **Básico:** 1) Elige periodo. 2) El sistema muestra citas activas con referencia America/Hermosillo. 3) Abre el detalle autorizado de una cita.
- **Alternos:**
  - A1) Sin citas: mostrar estado vacío.
  - A2) Cancelada: consultable fuera de la agenda activa y sin ocupar intervalo.
  - A3) Baja: excluida de la agenda y consultable solo con permiso especial.
  - A4) Periodo inválido: pedir corrección sin consultar datos.
  - A5) Sesión vencida o permiso retirado: S1; no mostrar resultados previamente protegidos tras una solicitud no autorizada.
  - A6) Equipo en otra zona: la hora mostrada conserva la referencia America/Hermosillo.

**CU06 — Agendar cita (HU09).** Actor: Usuaria o superusuario; capturista futuro. Disparador: acordar un horario. Precondición: paciente vigente y permiso. Postcondición: cita agendada y auditada, o ninguna cita nueva.
- **Básico:** 1) Elige paciente, ámbito e intervalo. 2) El sistema valida inicio, fin y cruce con citas activas de Usuaria. 3) Guarda comprobación y cita como una operación. 4) Confirma solo tras guardar y audita.
- **Alternos:**
  - A1) Paciente inexistente/de baja, ámbito no vigente para ese paciente, inicio no anterior al fin o instante ambiguo: rechazar sin guardar.
  - A2) Cruce con una cita activa de Usuaria, incluso por intentos simultáneos y aunque el ámbito sea distinto: rechazar el intento conflictivo e informar sin exponer datos no autorizados.
  - A3) El paciente es dado de baja mientras se agenda: ninguna combinación de resultados puede dejar cita activa de paciente de baja; Miguel define e implementa la protección en 09, y Lucía la prueba.
  - A4) Respuesta interrumpida: S5, consultar antes de reintentar.
  - A5) Equipo en otra zona horaria: conservar la hora acordada en America/Hermosillo.
  - A6) Sin permiso o con sesión revocada, o fallo de guardado/auditoría: S1/S4; no confirmar reserva.

**CU07 — Reprogramar o cancelar cita (HU10–HU11).** Actor: Usuaria o superusuario; capturista futuro. Disparador: cambia el horario acordado o se cancela la atención. Precondición: cita activa y permiso. Postcondición: nuevo horario o estado cancelada, con pago intacto y evento auditado.
- **Básico, reprogramación:** 1) Abre la cita. 2) Elige intervalo. 3) El sistema valida versión y cruces. 4) Guarda el horario y audita sin cambiar pago.
- **Alternos:**
  - A1) Cancelación autorizada: confirma la acción; el sistema marca cancelada, libera el intervalo y conserva consulta y pago. No la da de baja.
  - A2) Cruce o edición concurrente de cita/pago: rechazar el cambio conflictivo sin sobrescribir.
  - A3) Cita ya cancelada o de baja: no reprogramar como activa ni restaurarla implícitamente; informar estado actual.
  - A4) Nuevo intervalo inválido o paciente de baja: no reprogramar.
  - A5) Equipo en otra zona horaria: conservar la nueva hora acordada en America/Hermosillo.
  - A6) Falta permiso/sesión, falla el guardado/auditoría o respuesta incierta: S1/S4/S5; ni horario ni pago se confirman alterados.

**CU08 — Dar de baja y recuperar cita (HU12).** Actor: Usuaria o superusuario. Disparador: retirar una cita del uso ordinario o revertir esa baja. Precondición: cita existente y permiso. Postcondición: cita conservada, con condición vigente/baja actualizada y pago intacto.
- **Básico, baja:** 1) Selecciona cita y confirma baja. 2) El sistema la excluye del uso ordinario sin borrar estado ni pago. 3) Audita.
- **Recuperación:** 1) Comprueba permiso y condición de baja. 2) Si estaba agendada, comprueba paciente vigente y vuelve a comprobar cruce. 3) Solo si se cumplen las condiciones, vuelve a vigente y registra el evento; el pago no cambia.
- **Alternos:**
  - A1) Paciente de baja o cruce con otra cita activa: conserva la cita de baja y avisa.
  - A2) Si estaba cancelada, puede volver a vigente pero sigue cancelada; no ocupa intervalo y conserva pago.
  - A3) Cita ya vigente al intentar recuperarla, o ya de baja al intentar darla de baja: informar condición actual sin duplicar el cambio.
  - A4) Usuario sin permiso o sesión revocada: S1; la consulta de bajas tampoco se revela.
  - A5) Guardado/auditoría falla o respuesta incierta: S4/S5.
  - A6) El ámbito de una cita agendada de baja fue retirado del paciente: regla pendiente de Dulce con Usuaria; no decidir recuperación automática.

**CU09 — Consultar o cambiar estado de pago (HU13).** Actor: Usuaria o superusuario. Disparador: revisar o corregir el estado de una cita. Precondición: cita existente y permiso. Postcondición: una consulta no cambia nada; una corrección confirmada queda en pagado o pendiente y auditada, sin mover dinero.
- **Básico:** 1) Abre la cita y ve su estado. 2) Elige pagado o pendiente. 3) El sistema comprueba versión, guarda y audita. 4) Confirma el estado resultante.
- **Alternos:**
  - A1) Edición concurrente: avisar antes de sobrescribir, aunque el cambio competidor sea de cita y no de pago.
  - A2) Fallo de guardado/auditoría o respuesta incierta: S4/S5; no confirmar éxito.
  - A3) Corrección tras cancelación: regla pendiente de Dulce con Usuaria; cancelar por sí mismo no altera pago.
  - A4) Cita de baja: solo consulta autorizada de bajas; cambio de pago mientras está de baja pendiente de Dulce con Usuaria, sin habilitarlo por omisión.
  - A5) Capturista futuro, usuario sin permiso o sin sesión: denegar incluso por URL directa.
  - A6) Valor distinto de pagado/pendiente: rechazar; no introducir monto, método o cobro.

**CU10 — Consultar auditoría (HU14; antecedente de 08, no implementado como pantalla en 09).** Actor histórico: Usuaria o superusuario. Disparador: aclarar un cambio. Precondición: permiso. Postcondición: historial inspeccionado sin modificar eventos. Los pasos siguientes documentan la propuesta aprobada en 08; la decisión de 09 la retira de la interfaz. Q03 continúa verificándose en base.
- **Básico:** 1) Elige registro o periodo. 2) El sistema muestra eventos autorizados con usuario actor, momento, registro/campo y hecho. Usuaria consulta cambios de pacientes, citas y pago; los superusuarios también eventos de usuarios de la demostración. 3) Consulta el evento de interés.
- **Alternos:**
  - A1) Sin sesión o permiso, incluido capturista y enlace directo: denegar sin revelar eventos.
  - A2) Usuaria solicita eventos de gestión de usuarios: filtrarlos en el servidor; ese alcance corresponde al superusuario en la demostración ficticia.
  - A3) Sin eventos o filtro válido sin coincidencias: estado vacío.
  - A4) Filtro inválido o registro fuera de alcance: pedir corrección o denegar sin ampliar visibilidad.
  - A5) Intento de editar/borrar un evento: no existe operación autorizada; el almacenamiento de solo inserción impide modificación silenciosa según 07B.
  - A6) Fallo de consulta: mensaje seguro y registro técnico sin contraseñas, contactos ni valores clínicos.

**CU11 — Cambiar contraseña propia (HU15).** Actor: Usuaria, superusuario o capturista futuro, únicamente sobre su propio usuario. Disparador: desea cambiarla o debe sustituir una contraseña temporal al entrar. Precondición: identidad comprobada; si la contraseña es temporal, CU01 ya la verificó y restringió el acceso a este CU. Postcondición: contraseña propia cambiada y acción auditada sin secreto, o credencial anterior intacta.
- **Básico:** 1) En un cambio ordinario, el titular indica contraseña actual y nueva; en el primer acceso con contraseña temporal ya verificada por CU01, indica solo la nueva. 2) El sistema comprueba la identidad, la contraseña actual cuando corresponde y las reglas acordadas para la nueva. 3) Guarda el cambio mediante Identity y registra el hecho sin contraseña. 4) Invalida las demás sesiones; la sesión actual continúa solo si supera la validación del sello.
- **Alternos:**
  - A1) Contraseña actual errónea o nueva que no cumple las reglas acordadas: no cambiar y responder sin revelar secretos. Miguel revisará si esos fallos contribuyen al bloqueo 07B.
  - A2) Contraseña temporal pendiente: completar este CU antes de permitir pacientes, citas u otras funciones; si falla, el acceso sigue restringido.
  - A3) Sin sesión válida, sello revocado o intento de cambiar la contraseña de otro usuario: denegar. El restablecimiento ajeno corresponde exclusivamente a CU02.
  - A4) Guardado, auditoría o invalidación necesaria falla: no comunicar éxito completo; Miguel definirá la coherencia de esos pasos en 09.
  - A5) Respuesta incierta: comprobar de forma segura el estado del acceso antes de repetir; no mostrar la contraseña en mensajes, eventos ni registros técnicos.

**CU-F11 — Consultar y mantener historial clínico (futuro; HU-F01–F02).** Actor propuesto: Usuaria; superusuario solo con datos ficticios. Disparador: dar continuidad a la atención. Precondición: paciente identificado y permiso; R04 resuelto antes de uso real. Postcondición preliminar: información consultada o cambio autorizado auditado.
- **Flujo preliminar:** 1) Localiza paciente. 2) Consulta su historial. 3) Registra o corrige información permitida. 4) Guarda y audita sin valores clínicos en el evento.
- **Alternos preliminares:**
  - A1) Sin permiso o paciente fuera del alcance: denegar.
  - A2) R04 pendiente: no cargar datos reales.
  - A3) Conflicto concurrente o falla del evento de auditoría: no sobrescribir ni confirmar cambio. Los detalles clínicos permanecen pendientes de Usuaria.
- **Pendiente:** secciones, campos, correcciones, conflictos y conservación. No se detalla modelo clínico en 08C.

**CU-F12 — Registrar y consultar notas (futuro; HU-F03).** Actor propuesto: Usuaria; superusuario solo con datos ficticios. Disparador: documentar o recordar una atención. Precondición: paciente identificado y permiso; R04 resuelto antes de uso real. Postcondición preliminar: nota ficticia guardada y consultable por autorizados.
- **Flujo preliminar:** 1) Elige paciente y contexto de atención por definir. 2) Registra nota. 3) Guarda y audita sin copiar su texto al evento. 4) Consulta después.
- **Alternos preliminares:**
  - A1) Sin permiso o contexto de atención no confirmado: no mostrar ni guardar nota.
  - A2) R04 pendiente: no cargar datos reales.
  - A3) Guardado o evento de auditoría falla: no confirmar la nota. Reglas de corrección y vínculo siguen sin aprobar.
- **Pendiente:** vínculo con cita o sesión, corrección, acceso y conservación.

**CU-F13 — Consultar reportes (futuro; HU-F04).** Actor propuesto: Usuaria; superusuario solo con datos ficticios. Disparador: revisar la actividad del consultorio. Precondición: permiso; R04 resuelto antes de uso real. Postcondición preliminar: resultado autorizado mostrado sin modificar registros.
- **Flujo preliminar:** 1) Elige tipo de reporte aún por validar. 2) Selecciona periodo. 3) El sistema muestra resultado autorizado.
- **Alternos:**
  - A1) Sin datos: mensaje claro.
  - A2) Periodo inválido: solicitar corrección.
  - A3) Sin permiso: denegar antes de consultar; un reporte no puede ampliar el acceso a pacientes/citas.
  - A4) R04 pendiente: solo datos ficticios. Tipos, indicadores y exportación pendientes; se dividirá el CU si Usuaria necesita objetivos distintos. No implica cobros ni montos.

**CU-F14 — Verificar acceso en dos pasos (obligatorio antes del uso real; HU-F06).** Actor: cualquier persona a la que se autorice acceso a datos reales. Disparador: inicio de sesión con contraseña válida. Precondición: inscripción de la app autenticadora mediante procedimiento autorizado; su ausencia impide entrar a datos reales. Postcondición: sesión de acceso real abierta solo tras comprobar el segundo paso, o ningún acceso concedido.
- **Flujo preliminar:** 1) Completa el primer paso con su contraseña. 2) Introduce el código temporal de la app. 3) El sistema comprueba el código y solo entonces permite la sesión con sus permisos.
- **Alternos:**
  - A1) Código inválido, caducado o de otra persona: denegar sin revelar datos.
  - A2) Falta inscripción autorizada: no habilitar datos reales; la inscripción y recuperación quedan por definir y probar.
  - A3) Pérdida de la app: aplicar procedimiento controlado aún pendiente, sin eludir el segundo paso por una excepción informal.
  - A4) Contraseña incorrecta, usuario bloqueado/desactivado o sesión revocada: CU01 sigue denegando el primer paso.
  - A5) Intentos repetidos, posible reutilización de código y desfase de reloj: Miguel propondrá e implementará controles en 09 y Lucía los probará para decisión de Lucía antes del uso real; no se inventa aquí un umbral. El correo queda como posible opción futura, sin servicio seleccionado.

## 5. Pendientes de decisión y traspaso

| Responsable | Qué falta | Límite para continuar |
| --- | --- | --- |
| **Dulce, con Usuaria** | Validar datos de paciente/cita y contacto de tutor; ámbito por paciente/cita; detección de duplicados; permiso del capturista; baja de paciente con citas activas; pago de cita cancelada; secciones del historial, vínculo de notas y tipos de reporte. Precisar si hay datos de menores y cómo se obtendrían. Fijar prioridades de producto y revisar pantallas/navegación de 08C desde frontend. | Lucía difirió el visto bueno a pantallas: no bloquea por sí solo 08D, pero sí debe ocurrir antes de desarrollar frontend y declarar listas las HU/CU afectados en 09. Registrar solo respuestas efectivamente recibidas. |
| **Miguel** | Implementar identidad, sesiones, auditoría atómica, cruces, recuperación, control de versiones y guardado; escribir unitarias de su código en el mismo PR. Proponer el mecanismo de rol y recuperación sin correo y las rutas técnicas de R04; implementar TOTP cuando se priorice antes de datos reales. | Lucía prueba integración crítica y seguridad; Miguel revisa sus pruebas cuando corresponda. |
| **Lucía** | Definir estrategia de QA desde CA y Q; escribir y ejecutar integración Q03/Q06/Q07/Q20 con Testcontainers, Playwright Q16, seguridad OWASP/07B/Astra y B08. Revisar PR críticos, permisos, diagramas y evidencia; coordinar R04 y el impacto de TOTP en ADR-05. Crear el repositorio y trasladar documentación según su responsabilidad. | Miguel o Dulce revisa las pruebas de Lucía; R04 antes de datos reales. |

**Estado al cierre de 08:** la prioridad final, los tamaños y Sprints siguen pendientes de Dulce/equipo; la matriz completa quedó en [08D v0.2](../planning/TherapEase-etapa08-backlog-trazabilidad.md) y los modelos e interfaces en 08C v0.5 aprobada, con su §3 pendiente de Dulce antes del frontend afectado. Los alternos documentados cubren los estados y fallos relevantes conocidos, sin aprobar campos pendientes ni anticipar procedimientos de recuperación o mecanismos de 09. La validación ejecutable de arquitectura y el cierre de Elaboración/LCA siguen al inicio autorizado de 09.

