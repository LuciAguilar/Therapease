# TherapEase — Etapa 09 · B04 · Estado de pago y recorrido Q16

**Estado al 16-nov-2026: planes auditados por Claude APROBADO v2 y publicación autorizada por Lucía.** Plan compartido: publicación autorizada, SHA por registrar después de confirmarla. No hay propuesta B04 aprobada, código B04 autorizado ni pruebas nuevas ejecutadas. ✅ = avance comprobado en su alcance; ⏳ = pendiente. Las respuestas de producto recibidas no equivalen a implementación.

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
| ⏳ | Miguel | Una propuesta técnica completa de consulta/cambio de pago: contratos, permisos, versión, estados, reenvíos, fallos y auditoría. | Reutilizar Citas, la versión y EstadoPago existentes; indicar qué ya existe y qué requiere cambio. Lucía decide antes de implementar. |
| ⏳ | Miguel | Después de la autorización: servicios, persistencia, unitarias propias y apoyo a Dulce. | Si hay migración, justificar SQL, impacto y reversión; no darla por necesaria ahora. Mantener las dependencias entre capas. |
| ✅ respuestas / ⏳ diseño de pantalla | Dulce | Conservar pago inicial Pendiente, cancelada corregible y cita de baja sin escritura. Concretar dónde se consulta/cambia y sus mensajes. | No volver a pedir respuestas ya recibidas. Resolver con Lucía los detalles de interfaz pendientes; páginas y PageModel con sus unitarias después del contrato. |
| ✅ documentos / ⏳ diseño técnico aprobado | Lucía con Codex | Preparar estos planes, contrastar CU09 y diseñar QA dirigido de servidor, HTTP y navegador. | Revisar la propuesta con Claude; Lucía decide. Este documento no acredita pruebas ni autoriza escribirlas. |
| ⏳ | Lucía | Revisar cambios críticos, la CI y el alcance de Q16; preparar Q13 cuando proceda. | Código y pruebas por PR, evidencia identificada y aprobación final propia. |
| ⏳ | Miguel o Dulce | Revisar las pruebas de Lucía como persona distinta de la autora. | La auditoría de Claude no sustituye esta revisión humana. |

La preparación puede avanzar mientras Dulce termina B03. No retrasa el apoyo de Miguel a Dulce, el QA B02 en #22 ni las páginas del avance de diciembre. Miguel y Dulce eligen sus herramientas.

## 3. Reglas vigentes y decisiones que se conservan

| Estado | Tema | Qué conservamos | Qué falta concretar |
| --- | --- | --- | --- |
| ✅ base | Datos | Solo EstadoPago.Pendiente y EstadoPago.Pagado; Cita ya guarda ese campo y una versión. | Propuesta de Miguel para consultar/cambiar mediante Application; no añadir dinero, tablas o dependencias por suposición. |
| ✅ respuesta | Pago inicial | Pendiente al agendar; ya está en ReglasDeCita. | Conservar la regresión y comprobarlo en la página cuando exista. No atribuir ejecución nueva. |
| ✅ respuesta / ⏳ implementación | Cancelada y de baja | Cancelada admite corrección autorizada; cita de baja conserva consulta autorizada y rechaza escritura. | ObtenerCitaAsync actualmente oculta bajas como inexistentes. Proponer acceso mínimo al pago de una baja, con autorización previa, sin abrir directorio B06 ni revelar datos del paciente. Distinguir cita de baja de paciente de baja; justificar cualquier ajuste al contrato B03. |
| ✅ base / ⏳ detalle | Permisos | Usuaria y Superusuario en datos ficticios; sin permiso o con temporal pendiente se rechaza. Sin capturista nuevo. | Miguel propone los permisos de consulta/cambio y su concesión expresa; Lucía decide. El detalle B03 ya entrega EstadoPago con ConsultarAgenda/ConsultarPacientes. Explicar cómo conservarlo sin ampliar acceso por suposición. |
| ✅ regla / ⏳ contrato | Versión | Toda edición concurrente de la cita puede producir conflicto, aunque no cambie el pago. | Precisar el orden de comprobaciones y su protección hasta guardar, incluida la salida sin cambios. Recomendación pendiente: versión antigua siempre produce conflicto, aunque coincida el pago; versión vigente y mismo pago no escribe, no cambia metadatos/versión ni genera evento. |
| ⏳ decisión técnica | Doble envío y respuesta perdida | No mostrar éxito incierto ni generar cambios/eventos duplicados por un reenvío. | Comparar versión sin identificador nuevo frente a reconocimiento persistente de una solicitud. No usar IdCita como identificador de cada corrección. Una consulta posterior muestra el estado actual; no demuestra quién confirmó el envío perdido ni descarta cambios intermedios. |
| ✅ regla / ⏳ mecanismo | Guardado y auditoría | Una modificación confirmada y su evento van en la misma transacción; fallo de auditoría revierte. Actor de sesión y hora del servidor. | Proponer evento de Actualizacion sobre Cita, con EstadoPago como campo y hecho fijo, sin valores anterior/nuevo ni otros datos privados; confirmar compatibilidad con el mecanismo existente. Una modificación efectiva produce un evento; consulta, rechazo y sin cambios no deben inventar modificaciones. |
| ⏳ precisión | Paciente o ámbito | CU09 trata el estado de la cita. B03 conserva su ámbito al retirarlo; no alterar citas ni revelar nombres sin permiso. | Explicar consulta de pago si se retira acceso al paciente/ámbito o este está de baja. No imponer por cuenta propia nuevas reglas de paciente vigente al pago. |
| ✅ límite | Estados futuros | Cancelar y dar de baja conservan pago. | Ensayar filas preparadas en pruebas cuando se autoricen; el conflicto pago/reprogramación completo se ampliará al existir B05. No implementar B05/B06 para probar B04. |
| ✅ solicitud / ⏳ decisión fuera | Montos | La entrevista pide montos, pero el alcance vigente los excluye. Método de pago también queda fuera. | Solo Lucía puede ampliar alcance tras definir reglas e impacto; esta preparación no lo hace. |
| ✅ Q16 / ⏳ trazas | Navegador y evidencia | Playwright .NET ya existe. Identidad sigue sin trazas hasta la decisión de Q16/B04; TRX seguros. | Decidir antes de implementar la captura Q16: revisar también red, cabeceras, formularios, capturas y archivos adjuntos. Ocultar un campo visual no demuestra ausencia de cookies/contraseñas. Hasta decisión y comprobación, conservar la excepción de identidad; no habilitar trazas por llegar a B04. |

Miguel reúne en una propuesta sus puntos técnicos: permisos, versión, reenvíos, auditoría, paciente o ámbito y cita de baja. Lucía decide las trazas enmascaradas y cualquier ampliación a montos; ambos temas siguen pendientes. Dulce, con Lucía, define dónde se consulta/cambia el pago y los mensajes de pantalla. La propuesta técnica se revisará con Claude y Lucía decidirá antes de implementarla.

### 3.1. Qué debe devolver la propuesta única de Miguel

Una propuesta documental, con versión de referencia y rutas exactas, debe separar lo existente, los cambios recomendados y las decisiones que necesita de Lucía. Incluir entradas/salidas de consulta y cambio, campos admitidos y rechazados (también enviados de más), versión obligatoria y válida, mensajes fijos del servicio y significado de la respuesta HTTP. Dulce, con Lucía, define su presentación y los mensajes de pantalla. El actor se toma de la sesión. Precisar respuestas para cita inexistente, baja, sin permiso, temporal pendiente, valor inválido, conflicto, sin cambios, cambio confirmado y fallo técnico; ordenar las comprobaciones para no revelar datos antes de autorizar.

Recomendación para comparar, todavía sin aprobar: ampliar Citas con la versión existente y sin identificador nuevo de solicitud. Un reenvío con versión antigua devuelve conflicto, sin atribuirle el éxito anterior; tras una respuesta perdida se ofrece consulta autorizada y recarga, sin repetir automáticamente ni volver a enviar con una versión nueva. La otra opción reconoce una solicitud guardada: Miguel debe justificar almacenamiento, actor, datos, reenvíos y costo si la considera necesaria. Elegir de nuevo el mismo pago tras una recarga es una acción nueva; ver el valor deseado no prueba que el envío anterior se guardó.

La propuesta debe incluir una matriz de permisos con nombres, combinación exigida por operación y decisión expresa para ambos roles ficticios. Precisar la convivencia con ConsultarAgenda/ConsultarPacientes, el acceso mínimo al pago de bajas y el límite exacto si se revoca sesión/permiso durante una operación. No prometer revocación instantánea a mitad de una transacción sin explicar su mecanismo. La consulta de pago y la consulta de auditoría son distintas: Q03 conserva el acceso técnico de Superusuario, sin pantalla CU10 ni permiso nuevo para Usuaria.

Mantener una aplicación y las capas existentes: página → Application → reglas de Domain y puerto de Citas → Infrastructure. Las páginas no acceden a entidades ni repositorios. No hace falta distribuir servicios ni añadir patrones o dependencias por preparar pago; CAP/PACELC (compromisos entre réplicas separadas por red) no cambia en este diseño. Sí hay que resolver pérdidas/repeticiones de mensajes entre navegador y servidor. Justificar alternativas de guardado y conflictos por integridad, privacidad y simplicidad; si cambia un mecanismo Q/R, proponer nota ADR antes de implementarlo.

## 4. Base que aprovechamos y cuidado del QA

| Estado | Base disponible | Cómo la aprovechamos |
| --- | --- | --- |
| ✅ | B01 cerrado por Lucía; #19 fusionado | Acceso, sesiones, permisos, contraseñas y apoyo HTTPS/Playwright existentes. No reescribirlos para B04. |
| ✅ aplicación / ⏳ revisión QA | B02: #21 fusionado en a1a5e46; QA propio #22/1880007 abierto | Registrar y consultar pacientes. #22 tiene CI 43770074706 correcta: 923 unitarias, 574 integraciones y 77 recorridos; revisión de Miguel y fusión pendientes. No son pruebas B04 ni se vuelven a ejecutar por redactar este plan. |
| ✅ servidor / ⏳ páginas | B03: #18 y QA #19 integrados; guía de Dulce publicada | Agenda, alta de cita, pago inicial y versión disponibles. Las páginas de citas y su QA siguen con Dulce/Lucía, por separado. |
| ✅ lectura / ⏳ operación | Cita.EstadoPago, EstadoPago, ReglasDeCita e IServicioCitas | El contrato actual tiene consultar agenda, obtener cita y agendar; no se acredita un servicio de cambiar pago. Miguel propone su ampliación. |
| ✅ casos definidos / ⏳ ejecución B04 | Plan de pruebas, CU09/Q03/Q16 y seguridad | Mantener sus IDs y estados. Anotar cobertura y límites allí al ejecutar; no duplicar las tablas ni contar preparaciones como pruebas correctas. |
| ⏳ Q16 completo | Páginas B03 y cambio de pago B04 | El diseño y el servidor pueden prepararse antes; el recorrido completo espera esas páginas. No cerrar B03 por abrir B04. |

### 4.1. Cómo prepararemos los cambios simultáneos

| Situación | Cómo se forzará, después de autorizar las pruebas | Qué se comprobará |
| --- | --- | --- |
| Dos correcciones de pago | Solicitudes, contextos y conexiones separados leen la misma versión; una señal confirma que A terminó de comprobar y queda antes de guardar. Si A todavía no tiene el bloqueo, B termina primero. Si A ya lo tiene, B espera: se libera A y después termina B. Observar la espera real y probar confirmación/reversión, sin esperar que B termine mientras A conserva su bloqueo. Invertir el orden y cubrir destinos iguales, distintos y sin cambios. | Comprobar respuesta, pago, versión, metadatos y número/contenido de eventos desde otra conexión después de terminar ambas. Si se descarta una operación, no deja datos/eventos parciales. El instante de la salida sin cambios debe quedar definido en la propuesta. |
| Pago mientras cambia la cita | Preparar otra edición de la misma fila. Usar señales (avisos de que se alcanzó un paso) y transacciones controladas (cambios que la prueba decide cuándo confirmar o revertir). | Conflicto por versión. La reprogramación real por servicio/página se conserva para B05, sin inventar esa operación en B04. |
| Pago frente a baja | Distinguir fila cancelada vigente, cita de baja y paciente de baja. Para la condición de la cita: pausar pago tras comprobar; cambiar condición/versión en otra transacción y confirmar o revertir antes de liberarlo. Invertir el orden; preparar filas sin crear servicios B06. | No confirmar una escritura prohibida; conservar el pago y la auditoría coherentes. No implementar nuevas bajas B06. |
| Permiso retirado entre comprobación y guardado | Pausar tras autorización y antes del guardado; revocar sesión o permiso en otra operación, confirmar y liberar. Probar también la siguiente solicitud y la consulta posterior. | Resultado conforme al contrato aprobado, sin datos visibles para quien no pueda consultarlos; no prometer bloqueos todavía no diseñados. |
| Auditoría fallida o envío interrumpido | Fallar la inserción del evento y, por separado, cortar la respuesta antes del commit (confirmación de la transacción) y después de él. La prueba debe acreditar si hubo commit o reversión; no deducirlo de cerrar el cliente. | Fallo del evento: cero cambios parciales. Respuesta perdida: no reintento automático ni éxito inventado; comprobar el estado real. |
| Doble POST | Enviar el mismo POST (formulario al servidor) secuencialmente y solapado con pausas; conservar identificador de cita, versión y destino. Tras perder respuesta, consultar con permisos vigentes y probar la denegación si fueron retirados. | Resultado acordado para reenvíos, una modificación confirmada sin eventos extra indebidos. El caso sin cambios depende de la decisión previa. |

**Control del orden:** aprovechar las señales y los interceptores existentes (apoyos que observan o pausan una operación de prueba). Cuando el contrato dependa de un bloqueo de PostgreSQL, observar la espera real, como en AgendaYBajaConTransaccionesTests; lanzar dos tareas juntas o esperar unos milisegundos no demuestra ese orden. Fijar tiempos máximos, liberar pausas en finally y terminar ambas tareas aun al fallar. Miguel debe señalar dónde se puede pausar sin introducir esperas de prueba en producción. Esto es diseño, no una prueba escrita ni ejecutada.

### 4.2. Qué reutilizaremos al escribir las pruebas autorizadas

| Herramienta | Para qué | Dónde se revisará |
| --- | --- | --- |
| xUnit y PostgreSQL desechable con Testcontainers/Docker | Integridad, permisos de base, versión, transacciones y auditoría reales. | tests/TherapEase.IntegrationTests; carpeta interna por función acordada al implementar. No se crea ahora. |
| HttpClient y servidor HTTPS local | Antifalsificación, permisos y envíos directos, sin depender de la interfaz. | Apoyos de integración existentes; archivos exactos cuando se preparen las pruebas. |
| Playwright .NET con xUnit | Página de pago, reenvíos y recorrido Q16. Chrome local y Chromium en CI se distinguen en resultados. | tests/TherapEase.E2ETests; reutilizar acceso, pacientes y, al estar listas, citas. |
| GitHub Actions | Función afectada y Q16 por cambio, sin omitidas aceptadas como correctas. | .github/workflows/comprobaciones.yml; conservar la espera de limpieza de Testcontainers antes del navegador. |

**Ejecución de esta entrega:** ninguna prueba B04. Solo lectura de documentos/código y preparación de Markdown. No se encendió un servidor ni Docker para este plan.

### 4.3. Cómo se enlaza Q16 con CU09 y Q03

Q16 usará las páginas reales y datos ficticios aislados para Usuaria y Superusuario: acceso, registro de paciente, alta de cita con pago Pendiente, cambio a Pagado, recarga que conserva el valor y salida que impide volver a consultar contenido privado. El recorrido completo espera las páginas B03 y B04; una llamada directa al servicio no sustituye esos pasos. Los rechazos, reenvíos, conflictos y atomicidad se relacionan con CU09 y Q03 en sus tablas existentes, sin convertir Q16 en una copia de todos esos casos.

Antes de habilitar trazas, Lucía decide con la revisión de Claude qué evidencia puede publicarse y cómo se demuestra que está libre de secretos. Las trazas son archivos que pueden reunir pantalla, acciones y tráfico de red. Una propuesta de enmascarado debe cubrir todo su contenido y sus fallos; si no lo garantiza, Lucía con Codex prepara para decisión la alternativa de resultados seguros sin trazas para el recorrido de identidad. Esta decisión de QA no se encarga a Miguel. La regla general de trazas ante fallo durante siete días sigue vigente para el resto. Ninguna opción reduce los pasos de Q16 ni se aprueba por silencio.

## 5. Seguimiento, revisión y publicación

| Estado | Paso | Responsable | Evidencia o siguiente acción |
| --- | --- | --- | --- |
| ✅ | Leer alcance, entrevista y casos existentes | Lucía con Codex | HU13/CU09, respuestas de pago y Q16 contrastados; enlaces en sección 6. |
| ✅ | Preparar, corregir y auditar los planes | Lucía con Codex; Claude audita | Claude APROBADO v2, 16-nov; Lucía aprobó la publicación documental. Encargo a Miguel después de publicar. |
| ⏳ | Presentar una propuesta completa reutilizando lo existente | Miguel | Resolver las precisiones de la sección 3; apoyo a Dulce y B02/B03 conserva prioridad. |
| ⏳ | Auditar propuesta y QA diseñado | Claude | Auditoría técnica pendiente de recibir la propuesta de Miguel; APROBADO v2 corresponde solo a estos planes. |
| ⏳ | Decidir reglas pendientes y autorizar implementación | Lucía | Aprobación expresa antes de código y pruebas B04. |
| ⏳ | Implementar servidor, contrato y unitarias propias | Miguel | Solo después de autorización; PR con otra persona revisora y decisión final de Lucía. |
| ⏳ | Implementar páginas, PageModel y unitarias propias | Dulce | Contrato disponible y páginas B03 preparadas; revisión distinta del autor. |
| ⏳ | Preparar/ejecutar QA de servicios, HTTP y navegador | Lucía con Codex | Después de autorizar; archivo, comando, entorno, SHA y resultado reales en cada entrega. |
| ⏳ | Decidir evidencia y trazas antes de habilitar captura Q16 | Lucía; Claude audita | §4.3; conservar excepción de identidad y TRX seguros hasta decisión y comprobación. |
| ⏳ | Completar Q16 | Lucía; Miguel/Dulce revisa | CU01 → CU03 → CU06 → CU09 → salir, por cambio desde B04; espera páginas B03/B04. Seguimiento: [#15](https://github.com/LuciAguilar/Therapease/issues/15). |
| ⏳ | Revisión humana, CI y aceptación | Equipo; aprueba Lucía | Otra persona revisa cada autor; CI satisfactoria y sin bloqueantes. La IA no sustituye revisión humana. |
| ⏳ | Publicar documentación compartida | Lucía autoriza; Codex prepara | Lucía autorizó publicar directamente en main solo este plan y AGENTS. Confirmar publicación y registrar SHA; sin código ni notas personales. |
| ⏳ | Decidir cierre B04 | Lucía | Evidencia completa, Q16, revisión y pendientes tratados. B00/LCA/R04 permanecen separados; B01 ya está cerrado. |

**Nota breve de esta entrega:** Claude APROBADO v2 sobre los planes; Lucía autorizó publicarlos con AGENTS. Aplicados dos ajustes opcionales de texto. Encargo a Miguel después de publicar. Propuesta técnica, auditoría y decisión antes de código o pruebas B04.

Conservar revision/etapa09. Antes de cada entrega: fetch y merge de origin/main sin reset; preservar modelo, PDF y cambios locales. Tras cada fusión, sincronizar también el segundo clon sin cambiar de rama ni publicar cambios por su cuenta. Las notas personales y evidencias locales quedan fuera del remoto.

## 6. Documentos de contexto

| Documento | Qué leer |
| --- | --- |
| [AGENTS](../../AGENTS.md) | Reglas vigentes, estado más reciente y límites; no tomar un antecedente como el estado actual. |
| [08B: historias y casos](../requirements/TherapEase-etapa08-casos-historias.md) | HU13 y CU09, incluidos alternos A1–A6 y seguridad común. |
| [08D: backlog y trazabilidad](TherapEase-etapa08-backlog-trazabilidad.md) | B04 y trazabilidad Q03/Q07/Q08/Q13/Q16; trabajos posteriores B05–B08. |
| [Entrevista](../requirements/TherapEase-09-entrevista-requisitos.md) | Sección 2: pago inicial, cancelada, de baja y solicitud de montos aún fuera del alcance. |
| [Plan de pruebas](../testing/TherapEase-09-plan-de-pruebas.md) | M-17/M-22, CU09, Q03/Q16 y controles S aplicables; no duplicar sus tablas. |
| [Plan B03](TherapEase-etapa09-B03.md) y [Guía B03 de Dulce](TherapEase-09-guia-Dulce-B03.md) | Servidor integrado, páginas y QA B03 pendientes, contrato y orden de trabajo de Dulce. |
| [Plan B02](TherapEase-etapa09-B02.md) | #21 integrado y QA #22 por revisar; producto/08C y Q13 conservan seguimiento. |
| [ADR-04](../architecture/adr/ADR-01-a-05-provisionales.md) | ADR-04: versión, transacciones, auditoría y conservación. Si la propuesta cambia el mecanismo, explicar impacto y pedir decisión. |
| [IServicioCitas](../../src/Application/Citas/Interfaces/Servicios/IServicioCitas.cs) y [EstadoPago](../../src/Domain/Citas/Entidades/Enums/EstadoPago.cs) | Contrato Citas actual y enumeración existente; completar después las rutas reales del cambio de pago. |

La propuesta y los borradores que no estén publicados se comparten expresamente con Miguel cuando Lucía lo decida. No suponer que puede abrir rutas de esta computadora.
