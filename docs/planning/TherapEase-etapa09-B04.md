# TherapEase — Etapa 09 · B04 · Estado de pago y recorrido Q16

**Estado al 19-nov-2026:** servidor #25 aprobado por Lucía y fusionado con squash en `e68dbbe`. Código y 25 casos propios de servidor auditados por Claude; el QA propio sigue local y requiere su PR y revisión humana. Por decisión de Lucía, ella y Codex aplican después de la fusión las correcciones documentales de plan, ADR-04 y README. Páginas B04, HTTP, Q16, Q13 y trazas siguen pendientes; no se cierra B04.

**En palabras simples:** después de entrar, registrar un paciente y agendar una cita, queremos ver si está pagada o pendiente y corregir ese estado. El sistema debe guardar el cambio y su evento juntos, sin perder una edición de otra persona. No se mueve dinero. B04 también une las páginas anteriores en el recorrido completo de navegador Q16.

## 1. Qué abarca y qué comprobaremos

| Parte | Qué comprobamos |
| --- | --- |
| Alcance | HU13/CU09: consultar y cambiar Pendiente ↔ Pagado de una cita ficticia, sin montos, abonos, método de pago ni cobro. |
| Funcionamiento | Una consulta no cambia nada; una modificación autorizada se confirma solo después del guardado. Cambiar pago conserva horario, paciente, ámbito, estado y condición de la cita. Pago inicial Pendiente ya forma parte del alta B03. |
| Seguridad | Sesión y permisos vigentes en servidor, contraseña temporal que solo permite cambiarla, antifalsificación, entradas previstas y datos mostrados como texto, sin interpretarlos como código. Un rol sin permiso no consulta ni cambia por URL o POST directo. |
| Estado de la cita | Corregir pago en cancelada sí; cambiarlo mientras la cita está de baja no. Cancelar y dar de baja conservan el pago guardado. No se crean aquí esas operaciones de B05/B06. |
| Cambios simultáneos | Comprobar la versión de la cita: si otra operación la cambió, avisar sin sobrescribir. Diseñar esperas controladas, sin depender de que dos clics coincidan por suerte. |
| Auditoría y fallos | Cambio y evento se guardan o revierten juntos. Respuesta perdida no significa éxito ni permite repetir automáticamente una modificación. |
| Recorrido Q16 | Acceder → registrar paciente → agendar cita → marcar pagada → salir, para Usuaria y Superusuario ficticios. Desde B04 se ejecuta por cada cambio posterior. |
| Fuera de alcance | Montos, cobros, facturas, estados nuevos, reprogramación/cancelación B05, nuevas bajas/recuperaciones B06, directorio B07, cancelación automática y datos reales. |

Casos existentes, sin copiar sus tablas: [CU09](../testing/TherapEase-09-plan-de-pruebas.md#cu09--estado-de-pago-b04-), [Q03](../testing/TherapEase-09-plan-de-pruebas.md#q03--auditoría-en-base-captura-desde-b01-sin-pantalla-cu10-en-09), [Q16](../testing/TherapEase-09-plan-de-pruebas.md#recorrido-completo-q16-desde-b04-) y [citas y pago de Miguel](../testing/TherapEase-09-plan-de-pruebas.md#citas-y-pago-cu05cu09-b03b06). Q13 —uso por otra persona sin ayuda— tiene evidencia propia; que pase Playwright no acredita Q13.

## 2. Qué hará cada persona

| Estado | Persona | Trabajo de pago y recorrido | Qué falta |
| --- | --- | --- | --- |
| ✅ | Miguel | Propuesta técnica auditada; D1–D8 aprobadas por Lucía el 17-nov. | Diseño implementado en #25; sin decisiones técnicas pendientes de esta propuesta. |
| ✅ servidor integrado | Miguel | Servidor y pruebas de #25/0c7b35c, fusionados con squash en e68dbbe. | CI del PR 44393017247 correcta 4/4; Lucía aprobó como revisora distinta del autor. Continúa apoyo a Dulce. |
| ✅ respuestas / ⏳ diseño de pantalla | Dulce | Conservar pago inicial Pendiente, cancelada corregible y cita de baja sin escritura. Concretar dónde se consulta/cambia y sus mensajes. | No volver a pedir respuestas ya recibidas. Resolver con Lucía los detalles de interfaz pendientes; páginas y PageModel con sus unitarias después del contrato. |
| ✅ documentos y QA de servidor / ⏳ entrega propia | Lucía con Codex | 25 casos propios correctos y dos rondas adicionales de 21; auditados por Claude. | Publicar el QA en PR separado, revisión de Miguel o Dulce y aprobación final de Lucía. HTTP y navegador esperan páginas B04. |
| ✅ servidor / ⏳ páginas y recorrido | Lucía | Revisó y aprobó #25; decidió squash y corrección documental posterior. | Revisar páginas B04, Q16, Q13 y evidencia; decidir trazas antes de habilitarlas. |
| ⏳ | Miguel o Dulce | Revisar las pruebas de Lucía como persona distinta de la autora. | La auditoría de Claude no sustituye esta revisión humana. |

La preparación puede avanzar mientras Dulce termina B03. No retrasa el apoyo de Miguel a Dulce ni las páginas del avance de diciembre. El QA B02 de #22 ya está integrado en 010ebaf; Q13 y cierre B02 conservan seguimiento aparte. Miguel y Dulce eligen sus herramientas.

## 3. Reglas vigentes y decisiones que se conservan

| Estado | Tema | Qué conservamos | Qué falta concretar |
| --- | --- | --- | --- |
| ✅ base | Datos | Solo EstadoPago.Pendiente y EstadoPago.Pagado; Cita ya guarda ese campo y una versión. | Diseño aprobado: cambiar mediante Application y conservar la consulta existente de citas vigentes; sin dinero, tablas o dependencias nuevos. |
| ✅ respuesta | Pago inicial | Pendiente al agendar; ya está en ReglasDeCita. | Conservar la regresión y comprobarlo en la página cuando exista. No atribuir ejecución nueva. |
| ✅ implementado | Cancelada y de baja | Cancelada admite corrección; cita de baja rechaza escritura, incluso sin cambio. Cancelar/baja conservan pago. | Consulta del pago de citas de baja aplazada a B06 (D2). ObtenerCitaAsync no cambia; sin ConsultarPagoAsync nuevo. |
| ✅ implementado | Permisos | CambiarPagoDeCitas para Usuaria y Superusuario, solo datos de prueba. | Cambio exige también ConsultarAgenda y ConsultarPacientes. Consulta de vigentes por ObtenerCitaAsync, sin ampliar la consulta B03. |
| ✅ implementado | Versión y sin cambios | Versión anterior produce conflicto aunque coincida el pago. Versión vigente y mismo pago no escribe ni cambia metadatos/versión ni genera evento. | Sin bloqueo: SinCambios vale al leer. Dos envíos que ya coinciden pueden responder ambos SinCambios; servidor comprobado, HTTP pendiente. |
| ✅ servidor / ⏳ HTTP | Doble envío y respuesta perdida | Versión existente, sin identificador nuevo de solicitud ni reintento automático. | Recargar muestra el estado actual sin atribuir el éxito del envío perdido. No sustituir automáticamente la versión para reenviar. |
| ✅ servidor | Guardado y auditoría | Cambio/evento juntos; hecho fijo sin valores, Actualizacion de Cita y campo EstadoPago. | Sin disparador extra; las escrituras previstas suben Version. Fallo de auditoría revierte todo; no se presenta como éxito ni conflicto. |
| ✅ servidor | Paciente o ámbito | Cambiar pago no consulta Pacientes ni ámbitos; se conservan protecciones de base existentes. | Comprobado con paciente de baja o ámbito retirado. Solo la cita de baja prohíbe escribir pago; consulta de bajas, B06. |
| ✅ límite | Estados futuros | Cancelar y dar de baja conservan pago. | Ensayar filas preparadas en pruebas cuando se autoricen; el conflicto pago/reprogramación completo se ampliará al existir B05. No implementar B05/B06 para probar B04. |
| ✅ solicitud / ⏳ decisión fuera | Montos | La entrevista pide montos, pero el alcance vigente los excluye. Método de pago también queda fuera. | Solo Lucía puede ampliar alcance tras definir reglas e impacto; esta preparación no lo hace. |
| ✅ Q16 / ⏳ trazas | Navegador y evidencia | Playwright .NET ya existe. Identidad sigue sin trazas hasta la decisión de Q16/B04; TRX seguros. | Decidir antes de implementar la captura Q16: revisar también red, cabeceras, formularios, capturas y archivos adjuntos. Ocultar un campo visual no demuestra ausencia de cookies/contraseñas. Hasta decisión y comprobación, conservar la excepción de identidad; no habilitar trazas por llegar a B04. |

Claude auditó la propuesta ajustada con APROBADO v1 y Lucía decidió D1–D8. Trazas y montos siguen pendientes por separado; Dulce con Lucía define ubicación y mensajes de pantalla.

### 3.1. Diseño aprobado

Diseño aprobado por Lucía el 17-nov-2026 (D1–D8), tras auditoría de Claude.

Entradas: identificador de cita, versión esperada válida y pago Pendiente/Pagado. El actor sale de la sesión. Campos admitidos; los enviados de más se ignoran (S-14, D6). El rechazo de un valor de pago inválido no se confunde con ignorar campos extra. Dulce, con Lucía, concreta los mensajes de pantalla.

D5 fija el límite en el inicio de la solicitud: volver a consultar permisos en el servicio no garantiza una lectura fresca del usuario ni una nueva comparación del sello de cookie. Una operación ya iniciada puede confirmar; no se revierte el commit por revocación posterior. La respuesta queda acotada al pago, estado y versión de esa cita, sin datos del paciente. El QA de servidor comprobó salir, desactivar, restablecer y cambiar rol, con denegación de la siguiente solicitud. En el caso de rol se comprueba revocación por sello; ambos roles conservan el permiso de pago.

Se mantienen las capas y el puerto de Citas existentes; sin migración, dependencias nuevas ni operación de consulta de bajas en B04. Servidor y QA de servidor ejecutados y auditados; #25 aceptado e integrado. Entrega propia del QA, revisión humana y publicación pendientes.

## 4. Base que aprovechamos y cuidado del QA

| Estado | Base disponible | Cómo la aprovechamos |
| --- | --- | --- |
| ✅ | B01 cerrado por Lucía; #19 fusionado | Acceso, sesiones, permisos, contraseñas y apoyo HTTPS/Playwright existentes. No reescribirlos para B04. |
| ✅ aplicación y QA | B02: #21 y #22 integrados; #22 en 010ebaf | Reutilizar pacientes y sesiones; producto confirmado. Q13 y cierre B02 conservan su seguimiento aparte. No son pruebas B04. |
| ✅ integrado | B03: servidor, páginas #23 y QA #24 integrados | #24 fusionado en 9d0f3a0; CI main 44594280472 correcta 4/4 y uso funcional confirmado por Lucía con Dulce. Cierre B03 requiere decisión expresa. |
| ✅ operación disponible | Cita.EstadoPago, EstadoPago, ReglasDeCita e IServicioCitas | #25 incorpora CambiarPagoAsync; consulta y versión existentes se conservan. Contrato de pago en sección 4.4 del contrato B03. |
| ✅ casos definidos / ⏳ ejecución B04 | Plan de pruebas, CU09/Q03/Q16 y seguridad | Mantener sus IDs y estados. Anotar cobertura y límites allí al ejecutar; no duplicar las tablas ni contar preparaciones como pruebas correctas. |
| ⏳ Q16 completo | Páginas B03 integradas; páginas de pago B04 pendientes | Servidor de pago disponible. Q16 espera las páginas B04; la decisión de trazas precede a su captura. No se cierra B03 por preparar B04. |

### 4.1. Cómo prepararemos los cambios simultáneos

| Situación | Diseño del orden controlado (servidor ejecutado; HTTP pendiente) | Qué se comprobará |
| --- | --- | --- |
| Dos correcciones de pago | Solicitudes, contextos y conexiones separados leen la misma versión; una señal confirma que A terminó de comprobar y queda antes de guardar. Si A todavía no tiene el bloqueo, B termina primero. Si A ya lo tiene, B espera: se libera A y después termina B. Observar la espera real y probar confirmación/reversión, sin esperar que B termine mientras A conserva su bloqueo. Invertir el orden y cubrir destinos iguales, distintos y sin cambios. | Comprobar respuesta, pago, versión, metadatos y número/contenido de eventos desde otra conexión después de terminar ambas. Si se descarta una operación, no deja datos/eventos parciales. La salida SinCambios describe el momento de la lectura, según D4. |
| Pago mientras cambia la cita | Preparar otra edición de la misma fila. Usar señales (avisos de que se alcanzó un paso) y transacciones controladas (cambios que la prueba decide cuándo confirmar o revertir). | Conflicto por versión. La reprogramación real por servicio/página se conserva para B05, sin inventar esa operación en B04. |
| Pago frente a baja | Distinguir fila cancelada vigente, cita de baja y paciente de baja; sin consulta de pago de bajas en B04 (B06). Para la condición de la cita: pausar pago tras comprobar; cambiar condición/versión en otra transacción y confirmar o revertir antes de liberarlo. Invertir el orden; preparar filas sin crear servicios B06. | No confirmar una escritura prohibida; conservar el pago y la auditoría coherentes. No implementar nuevas bajas B06. |
| Permiso retirado entre comprobación y guardado | Pausar tras autorización y antes del guardado; revocar sesión o permiso en otra operación, confirmar y liberar. Probar también la siguiente solicitud y la consulta posterior. | Resultado conforme a D5/§6.1 de la propuesta: límite al inicio de solicitud, respuesta acotada y siguiente solicitud denegada. No prometer revocación instantánea. |
| Auditoría fallida o envío interrumpido | Fallar la inserción del evento y, por separado, cortar la respuesta antes del commit (confirmación de la transacción) y después de él. La prueba debe acreditar si hubo commit o reversión; no deducirlo de cerrar el cliente. | Fallo del evento: cero cambios parciales. Respuesta perdida: no reintento automático ni éxito inventado; comprobar el estado real. |
| Doble POST | Enviar el mismo POST (formulario al servidor) secuencialmente y solapado con pausas; conservar identificador de cita, versión y destino. Tras perder respuesta, consultar con permisos vigentes y probar la denegación si fueron retirados. | Cambio efectivo: una modificación/evento y conflicto en reenvíos antiguos. Destino ya guardado: SinCambios, sin evento; según D3/D4. |

**Control del orden:** aprovechar las señales y los interceptores existentes (apoyos que observan o pausan una operación de prueba). Cuando el contrato dependa de un bloqueo de PostgreSQL, observar la espera real, como en AgendaYBajaConTransaccionesTests; lanzar dos tareas juntas o esperar unos milisegundos no demuestra ese orden. Fijar tiempos máximos, liberar pausas en finally y terminar ambas tareas aun al fallar. P3 reutiliza PausaDeConfirmacion antes del commit y pg_blocking_pids; no pausar en ReaderExecuting del INSERT, pues EF puede agruparlo con UPDATE. SinCambios se pausa en P1; no pasa por SaveChanges. Sin esperas de prueba en producción. Este apartado conserva el diseño. El QA de servidor ejecutado usa pausas y espera real de PostgreSQL; no acredita HTTP ni navegador.

### 4.2. Qué reutilizaremos al escribir las pruebas autorizadas

| Herramienta | Para qué | Dónde se revisará |
| --- | --- | --- |
| xUnit y PostgreSQL desechable con Testcontainers/Docker | Integridad, permisos de base, versión, transacciones y auditoría reales. | tests/TherapEase.IntegrationTests; reutilizar Citas y Concurrencia; el QA propio permanece local hasta su PR. |
| HttpClient y servidor HTTPS local | Antifalsificación, permisos y envíos directos, sin depender de la interfaz. | Apoyos de integración existentes; archivos exactos cuando se preparen las pruebas. |
| Playwright .NET con xUnit | Página de pago, reenvíos y recorrido Q16. Chrome local y Chromium en CI se distinguen en resultados. | tests/TherapEase.E2ETests; reutilizar acceso, pacientes y, al estar listas, citas. |
| GitHub Actions | Función afectada y Q16 por cambio, sin omitidas aceptadas como correctas. | .github/workflows/comprobaciones.yml; conservar la espera de limpieza de Testcontainers antes del navegador. |

**Evidencia anterior, sin repetir por editar texto:** Codex ejecutó build, 1031 unitarias, 600 integraciones originales (incluidas 30 de arquitectura), 25 casos propios y dos rondas adicionales de 21. Claude comprobó 1031 unitarias, 51 integraciones de pago y dos rondas de 25 (21 propias + 4 de Miguel). Son ejecuciones distintas; no se suman. HTTP de pago y Q16 siguen sin ejecutarse.

### 4.3. Cómo se enlaza Q16 con CU09 y Q03

Q16 usará las páginas reales y datos ficticios aislados para Usuaria y Superusuario: acceso, registro de paciente, alta de cita con pago Pendiente, cambio a Pagado, recarga que conserva el valor y salida que impide volver a consultar contenido privado. El recorrido completo espera las páginas B03 y B04; una llamada directa al servicio no sustituye esos pasos. Los rechazos, reenvíos, conflictos y atomicidad se relacionan con CU09 y Q03 en sus tablas existentes, sin convertir Q16 en una copia de todos esos casos.

Antes de habilitar trazas, Lucía decide con la revisión de Claude qué evidencia puede publicarse y cómo se demuestra que está libre de secretos. Las trazas son archivos que pueden reunir pantalla, acciones y tráfico de red. Una propuesta de enmascarado debe cubrir todo su contenido y sus fallos; si no lo garantiza, Lucía con Codex prepara para decisión la alternativa de resultados seguros sin trazas para el recorrido de identidad. Esta decisión de QA no se encarga a Miguel. La regla general de trazas ante fallo durante siete días sigue vigente para el resto. Ninguna opción reduce los pasos de Q16 ni se aprueba por silencio.

## 5. Seguimiento, revisión y publicación

| Estado | Paso | Responsable | Evidencia o siguiente acción |
| --- | --- | --- | --- |
| ✅ | Leer alcance, entrevista y casos existentes | Lucía con Codex | HU13/CU09, respuestas de pago y Q16 contrastados; enlaces en sección 6. |
| ✅ | Preparar, corregir y auditar los planes | Lucía con Codex; Claude audita | Claude APROBADO v2, 16-nov; Lucía aprobó la publicación documental. Encargo a Miguel después de publicar. |
| ✅ | Recibir propuesta completa | Miguel | Diseño aprobado por Lucía el 17-nov-2026 (D1–D8), tras auditoría de Claude. |
| ✅ código y QA / ✅ ajustes de texto aplicados | Auditar propuesta y entrega | Claude; Lucía decide | Claude aprobó código y QA de servidor sin cambios; sus observaciones documentales se aplican por decisión de Lucía. Esta aplicación no inventa una nueva reauditoría. |
| ✅ diseño y servidor | Decidir reglas y aceptar implementación | Lucía | D1–D8 aprobadas el 17-nov. Aprobación humana de #25 registrada el 19-nov y fusión en e68dbbe; aceptación de páginas/Q16 pendiente. |
| ✅ | Implementar servidor, contrato y unitarias propias | Miguel | #25/0c7b35c aprobado por Lucía y fusionado con squash en e68dbbe; CI del PR 44393017247 correcta 4/4. |
| ⏳ | Implementar páginas, PageModel y unitarias propias | Dulce | Servidor y contrato disponibles; reutilizar las páginas B03 integradas. Ubicación y mensajes de interfaz con Lucía; revisión distinta del autor. |
| ✅ servidor / ⏳ entrega propia | Preparar/ejecutar QA dirigido | Lucía con Codex | Servidor ejecutado localmente: 25 casos + 2 rondas de 21; auditado por Claude. Entrega propia, revisión humana y publicación pendientes. HTTP y navegador B04 pendientes. |
| ⏳ | Decidir evidencia y trazas antes de habilitar captura Q16 | Lucía; Claude audita | §4.3; conservar excepción de identidad y TRX seguros hasta decisión y comprobación. |
| ⏳ | Completar Q16 | Lucía; Miguel/Dulce revisa | CU01 → CU03 → CU06 → CU09 → salir, por cambio desde B04; espera páginas B03/B04. Seguimiento: [#15](https://github.com/LuciAguilar/Therapease/issues/15). |
| ✅ servidor / ⏳ entregas restantes | Revisión humana, CI y aceptación | Equipo; aprueba Lucía | #25 revisado y fusionado. El QA propio, páginas y Q16 conservan sus revisiones/CI y aceptación separadas. |
| ✅ inicial / correcciones en esta revisión | Publicar documentación compartida | Lucía autoriza | Inicial c1c4090; contrato y servidor en #25/e68dbbe. Esta revisión reúne las correcciones autorizadas de plan, ADR-04, README y AGENTS para main; SHA final en seguimiento local tras comprobar el push. Sin notas personales. |
| ✅ decisión y corrección | Corregir documentación después de fusionar #25 | Lucía con Codex | Decisión de Lucía, 19-nov: ella y Codex corrigen plan B04, ADR-04 y README en otro commit solo documental directo a main. AGENTS registra la decisión. El QA conserva PR separado. |
| ⏳ | Decidir cierre B04 | Lucía | Evidencia completa, Q16, revisión y pendientes tratados. B00/LCA y B01 ya cerrados por Lucía; R04 sigue abierto. |

**Decisión y autoría de la corrección, 19-nov-2026:** Lucía aprobó #25 y lo fusionó con squash en e68dbbe. Decidió que Lucía con Codex aplicara después las correcciones de plan, ADR-04 y README, en un commit exclusivamente documental directo a main. El QA propio no forma parte de esta publicación; conserva PR y revisión humana separados. Sin cambios de código, pruebas, dependencias ni CI; B04 sigue abierto.

Conservar revision/etapa09. Antes de cada entrega: fetch y merge de origin/main sin reset; preservar modelo, PDF y cambios locales. Tras cada fusión, sincronizar también el segundo clon sin cambiar de rama ni publicar cambios por su cuenta. Las notas personales y evidencias locales quedan fuera del remoto.

## 6. Documentos de contexto

| Documento | Qué leer |
| --- | --- |
| [AGENTS](../../AGENTS.md) | Reglas vigentes, estado más reciente y límites; no tomar un antecedente como el estado actual. |
| [08B: historias y casos](../requirements/TherapEase-etapa08-casos-historias.md) | HU13 y CU09, incluidos alternos A1–A6 y seguridad común. |
| [08D: backlog y trazabilidad](TherapEase-etapa08-backlog-trazabilidad.md) | B04 y trazabilidad Q03/Q07/Q08/Q13/Q16; trabajos posteriores B05–B08. |
| [Entrevista](../requirements/TherapEase-09-entrevista-requisitos.md) | Sección 2: pago inicial, cancelada, de baja y solicitud de montos aún fuera del alcance. |
| [Plan de pruebas](../testing/TherapEase-09-plan-de-pruebas.md) | M-17/M-22, CU09, Q03/Q16 y controles S aplicables; no duplicar sus tablas. |
| [Plan B03](TherapEase-etapa09-B03.md) y [Guía B03 de Dulce](TherapEase-09-guia-Dulce-B03.md) | Servidor, páginas y QA B03 integrados; contrato y continuidad de trabajo de Dulce. |
| [Plan B02](TherapEase-etapa09-B02.md) | #21 y #22 integrados; producto confirmado. Q13 y decisión de cierre conservan seguimiento. |
| [ADR-04](../architecture/adr/ADR-01-a-05-provisionales.md) | ADR-04: versión, transacciones, auditoría y conservación. Si la propuesta cambia el mecanismo, explicar impacto y pedir decisión. |
| [IServicioCitas](../../src/Application/Citas/Interfaces/Servicios/IServicioCitas.cs) y [EstadoPago](../../src/Domain/Citas/Entidades/Enums/EstadoPago.cs) | Consulta existente y CambiarPagoAsync integrados; solo Pendiente/Pagado. Contrato de página: sección 4.4 del contrato B03. |

La propuesta y los borradores que no estén publicados se comparten expresamente con Miguel cuando Lucía lo decida. No suponer que puede abrir rutas de esta computadora.
