# TherapEase — B03 · Propuesta técnica de Miguel: agenda y citas

**Original de Miguel, 03-nov-2026 · D1–D13 aprobadas por Lucía el 05-nov.** Precisiones documentales aplicadas por Codex tras Claude CORREGIR; no se atribuye una nueva auditoría APROBADO. La versión recibida se conserva en la evidencia local. Actividad informada por Miguel el 03-nov: lectura y dos sondeos PostgreSQL desechables fuera del repositorio (3.11), sin evidencia recibida ni reproducción por Codex. Esta entrega no ejecuta nuevas validaciones de B03. Base: main `0e2838b` (B02 fusionado en #11 y #12; plan B03 y decisiones del 01-nov publicados). Rama de trabajo local: `b03/propuesta-tecnica`.

B03 abarca HU08–HU09 y CU05–CU06: consultar la agenda y agendar una cita ficticia en America/Hermosillo, sin cruces de citas activas, con paciente vigente y auditoría atómica. Quedan fuera B04 (pago), B05 (reprogramar y cancelar), nuevas bajas y recuperaciones B06, directorio B07, montos, «solo particular» y cancelación automática. Páginas y PageModel siguen con Dulce. Solo datos ficticios, sin nube ni gastos; B00/B01/B02/LCA/R04 siguen abiertos.

Fuentes leídas: [AGENTS, secciones 1–3](../../AGENTS.md), [plan B03](TherapEase-etapa09-B03.md), [avance y pendientes](TherapEase-09-avance-y-pendientes.md), [08B](../requirements/TherapEase-etapa08-casos-historias.md) (HU08–HU09, CU05–CU06), [08C](../requirements/TherapEase-etapa08-modelos-interfaces.md), [08D](TherapEase-etapa08-backlog-trazabilidad.md), [entrevista, sección 5](../requirements/TherapEase-09-entrevista-requisitos.md), [planes B00](TherapEase-etapa09-B00.md), [B01](TherapEase-etapa09-B01.md) y [B02](TherapEase-etapa09-B02.md), [contrato](TherapEase-etapa09-B02-contrato-para-paginas.md) y [evidencia](../testing/TherapEase-09-B02-evidencia-servicios.md) de B02, [plan de pruebas](../testing/TherapEase-09-plan-de-pruebas.md), [ADR-03/04](../architecture/adr/ADR-01-a-05-provisionales.md) y [ADR-06](../architecture/adr/ADR-06-monorepo.md), y el código de Domain, Application e Infrastructure de citas, pacientes, auditoría y permisos.

## 0. Resumen y recomendación

1. **Un servicio público `IServicioCitas`** en Application/Citas con tres operaciones: `ConsultarAgendaAsync`, `ObtenerCitaAsync` y `AgendarAsync`. Los PageModel solo llaman a esa interfaz; nada nuevo en Web salvo políticas y registro de dependencias.
2. **Dos permisos nuevos**, `ConsultarAgenda` y `AgendarCitas`, concedidos a Usuaria y Superusuario (este último solo con datos de prueba). Ambas operaciones exigen además `ConsultarPacientes`, porque devuelven o leen datos del paciente (sección 3.2).
3. **Reenvío con el identificador de la cita**, generado por la página al abrir el formulario, igual que el alta de B02. Los rechazos de negocio posteriores a la primera lectura se resuelven por una misma relectura con permisos vigentes, para no responder «horario ocupado» a la propia cita (sección 3.8).
4. **Horario en Domain**, sobre fecha y horas locales de Hermosillo, sin CHECK en PostgreSQL y sin migración. Las citas y pruebas anteriores fuera de horario no se tocan. Una sola copia de la regla permite comprobar sus 1 440 × 1 440 combinaciones en una prueba unitaria (sección 3.5).
5. **Cruces y baja simultánea**: se conservan, sin cambios, la restricción de exclusión y los dos disparadores de B00. B03 añade solo la traducción de la clave primaria de Cita y la relectura tras un rechazo (sección 3.6).
6. **Sin migración.** Un índice por inicio es opcional y no se recomienda todavía (sección 3.10).
7. **Un solo PR** con todo el servidor que se puede terminar sin páginas (sección 4).

Las decisiones aprobadas concretan el diseño del QA en tres puntos: el orden de las comprobaciones de ámbito (D10), la relectura ante rechazos de negocio posteriores a la primera lectura, con las exclusiones de 3.8 (D2) y la espera real que sufre una edición de paciente mientras otra operación guarda una cita (sección 3.11, sondeo 2).

## 1. Lo existente que se reutiliza

| Estado | Pieza actual | Uso en B03 y trabajo pendiente |
| --- | --- | --- |
| ✅ | `Cita`, `ReglasDeCita.Agendar`, `EstadoCita`, `EstadoPago`, `EntidadAuditable` | Se conservan sin cambios de firma. `Agendar` ya deja la cita agendada, vigente, con pago Pendiente y versión 1, y llena `FechaAlta` e `IdUsuarioAlta` con el reloj y el actor que recibe. |
| ✅ | Restricción `EX_Cita_SinCruceActivo` (exclusión GiST sobre `tstzrange(Inicio, Fin, '[)')`, solo agendada y vigente) | Fuente única de los cruces, también entre ámbitos y con intervalos contiguos permitidos. Sin cambios. |
| ✅ | Disparadores `TR_Cita_ActivaExigePacienteVigente` (con `FOR SHARE` sobre el paciente) y `TR_Paciente_BajaSinCitasActivas` | Fuente única de «nunca una cita activa de un paciente de baja». Ambos devuelven el código `TE001`. Sin cambios. |
| ✅ | `IRepositorioCitas`, `RepositorioCitas`, `CitaConfiguracion` | Se amplía el puerto del módulo (un puerto por módulo, ADR-03 y prueba de arquitectura): lectura por número sin rastreo, agenda del periodo y detalle. |
| ✅ | `IUnidadDeTrabajo` con `ChangeTracker.Clear()` tras un guardado fallido (B02) | Cita y evento en una transacción; el intento fallido no deja nada pendiente en el contexto. |
| ✅ | `TraductorDeErroresDeBase` (cruce → `CruceDeHorarioException`, `TE001` → `CitaActivaConPacienteDeBajaException`, clave de Paciente) | Se agrega solo la clave primaria de Cita. |
| ✅ | `IServicioAuditoria`, `TipoRegistroAuditoria.Cita`, `AccionAuditoria.Alta` | Evento de alta de cita con nombres de campos y un hecho fijo; sin valores privados. |
| ✅ | `IAutorizacion`, `IUsuarioActual`, `Permiso`, `MatrizDePermisos`, políticas web | Se agregan dos permisos con decisión expresa por rol. La temporal pendiente ya niega todo salvo CU11. |
| ✅ | `HoraHermosillo.AInstante` y `ALocal` (zona fija `America/Hermosillo`, sin depender de la zona del equipo) | Toda conversión de fecha y hora. Probada en la imagen con tres zonas de proceso. |
| ✅ | `RespuestaServicio<T>`, estados explícitos de B02 (`Registrado`, `YaRegistrado`, `CoincidenciasPorConfirmar`), `ResponderAlReenvio`, lectura sin rastreo | Patrón del reenvío y de los resultados de B03. |
| ✅ | `IServicioPacientes`, `IRepositorioPacientes.ObtenerCondicionAsync`, ámbitos editables sin tocar citas (B02) | Citas consulta Pacientes por un contrato público, sin ciclo (sección 3.3). |
| ✅ | Pruebas existentes de cruces, baja y citas (`RevisionDatosTests`, `BajaPacienteTests`, `HorasDockerTests`, `ReglasDeCitaTests`) | Se conservan. Ver sección 3.5 sobre citas fuera del horario nuevo. |
| ⏳ | Servicio de agenda, contrato de Pacientes para Citas, permisos, regla de horario y traducción de la clave de Cita | Es lo que implementa el PR de B03. |

## 2. Decisiones D1–D13 aprobadas por Lucía

Lucía aprobó D1–D13 el 05-nov. Se conservan recomendaciones y alternativas originales, con las precisiones de Claude incorporadas. No cambian las decisiones de producto del 01-nov.

| Nº | Decisión | Recomendación | Alternativa y consecuencia |
| --- | --- | --- | --- |
| D1 | Permisos | `ConsultarAgenda` y `AgendarCitas`, para Usuaria y Superusuario, más `ConsultarPacientes` en las dos operaciones (3.2). | Un permiso único de citas: adelanta B04–B06 y concede de más. O `AgendarCitas` sin `ConsultarPacientes`: obliga a un contrato de Pacientes sin permiso propio. |
| D2 | Reenvío | Identificador de la cita generado al abrir el formulario; una misma relectura por ese número antes de responder rechazos de negocio posteriores a la primera lectura, con permisos vigentes (3.8). | Tabla de solicitudes (migración, fuera de ADR-04 de B02) o clave natural paciente + horario (confunde una segunda reserva deliberada con un reenvío). |
| D3 | Dónde vive el horario | Solo en Domain, antes de convertir a instantes (3.5). | Además un CHECK en PostgreSQL: dos copias que comparar, migración con citas anteriores que fallan, y el reloj de la zona dentro de una restricción. |
| D4 | Forma de la entrada | Fecha, hora de inicio y hora de fin locales, sin zona; todos anulables para distinguir «no llegó» de un valor por omisión (3.1). | `DateTimeOffset` con zona: depende del navegador. |
| D5 | Periodo y límites de la agenda | De 1 a 35 días, hasta 500 filas con aviso de «hay más»; vista de activas y vista de canceladas separadas (3.4). | Límite menor o paginación: más código sin necesidad con los volúmenes previstos. |
| D6 | Cancelada de un paciente de baja | Se muestra el horario y el ámbito con «Paciente dado de baja»; sin nombre ni identificador (3.4). | Ocultarla por completo: la usuaria pierde ese hueco histórico sin motivo de privacidad adicional. |
| D7 | Contrato de Pacientes para Citas | Interfaz nueva `IConsultaDePacientesParaCitas`, con consulta de condición y ámbitos y consulta de nombres para la agenda (3.3). | Reutilizar `IServicioPacientes.ObtenerAsync`: trae la ficha completa y compara textos de mensaje. |
| D8 | Índice por inicio de cita | No ahora; migración y reversión descritas por si se decide (3.10). | Crearlo ya: sin beneficio con las decenas de citas ficticias. |
| D9 | Verificaciones de integración de Miguel | Incluir las regresiones de mi implementación que ya cubrí en B02 para el alta (clave repetida, reenvío en sus momentos, atomicidad), y dejar el agendar frente a baja y el ámbito retirado al diseño de Lucía (3.12). | No incluirlas: el servicio queda cubierto solo por unitarias hasta el QA. |
| D10 | Ámbito y bloqueos | Comprobar vigencia y ámbito con una lectura sin bloqueo; no bloquear `PacienteAmbito` ni tomar bloqueo explícito del paciente al comprobar (3.7). | Bloquear el paciente al comprobar: la edición esperaría y el orden forzado del plan B03 sección 4.1 («otra solicitud confirma su retiro») se convertiría en una espera. |
| D11 | Mensajes de rechazo del paciente | Tres mensajes fijos distintos (no encontrado, de baja, ámbito no permitido), porque quien agenda ya tiene `ConsultarPacientes` (3.3). | Un mensaje único: más opaco, pero distinto del comportamiento ya decidido en B02 para abrir una baja. |
| D12 | Citas y pruebas anteriores fuera del horario nuevo | No se cambian ni se borran; la agenda las muestra tal cual; la regla solo se aplica al agendar (3.5). | Corregirlas o migrarlas: pérdida de evidencia sin motivo. |
| D13 | Nota ADR | Nota posterior B03 en ADR-04, no un ADR nuevo: no hay tecnología nueva y el reenvío extiende el mecanismo aprobado en B02 (4.3). | ADR nuevo: solo si Lucía considera que el mecanismo de reenvío cambia. |

## 3. Propuesta por tema

### 3.1. Contrato del servicio y entradas

`IServicioCitas` (Application/Citas), con el actor siempre tomado de la sesión y permisos comprobados en el servidor antes de leer o escribir:

| Operación | Firma | Resultado |
| --- | --- | --- |
| Consultar agenda | `ConsultarAgendaAsync(DateOnly? desde, DateOnly? hasta, VistaDeAgenda vista = Activas)` | `RespuestaServicio<ResultadoDeAgenda>` |
| Detalle de una cita | `ObtenerCitaAsync(Guid idCita)` | `RespuestaServicio<DetalleDeCita>` |
| Agendar | `AgendarAsync(Guid idSolicitud, DatosDeCitaEntrada datos)` | `RespuestaServicio<ResultadoDeAgendar>` |

**Entrada de agendar** (`DatosDeCitaEntrada`, modelo de Application que usan las páginas; Web no usa entidades ni reglas de Domain): `IdPaciente` (`Guid?`), `Ambito` (`AmbitoAtencion?`), `Fecha` (`DateOnly?`), `HoraInicio` y `HoraFin` (`TimeOnly?`). Son los cuatro campos decididos; el identificador de la cita es `idSolicitud`. Todo anulable: un campo que no llegó no se confunde con 0001-01-01, 00:00 o el primer ámbito, y se rechaza como solicitud inválida.

**Identificador y fechas límite.** idSolicitud vacío se rechaza como solicitud inválida, igual que B02. Se valida que fecha/horas permitan instantes UTC representables antes de convertir: el 9999-12-31 con fin desde las 17:00 en Hermosillo se rechaza de forma controlada. No se deja escapar ArgumentOutOfRangeException ni se prohíben fechas pasadas.

**Cómo envía la página fecha y hora.** Solo la hora de pared de Hermosillo: un campo de fecha (`yyyy-MM-dd`) y dos de hora (`HH:mm`), sin zona ni desfase. La zona del navegador nunca entra. Si se agenda desde otro país, la hora acordada llega igual (HU09 CA4, L-CU06-06). La opción de que la página capture inicio y duración y calcule el fin es compatible: el servicio recibe siempre inicio y fin y valida igual.

**Resultado de agendar** (`ResultadoDeAgendar`, con estados explícitos; no decidir la navegación solo por `Exito`):

| Resultado | `Exito` | `Estado` | Contenido | ¿Anunciar guardado? |
| --- | --- | --- | --- | --- |
| Agendada | Verdadero, solo tras confirmar cita y evento | `Agendada` | Detalle de la cita, con «Registrada después de su horario» si corresponde | Sí |
| Ya estaba agendada | Verdadero, solo tras reconocer lo confirmado | `YaAgendada` | El mismo detalle | Sí, indicando que era la reserva anterior |
| Rechazo | Falso | — | Mensaje fijo, sin datos privados | No |

**Salidas de agenda y detalle.** `CitaDeAgenda`: `IdCita`, `InicioLocal` y `FinLocal` (`DateTime` de hora de pared de Hermosillo, ya convertidos en el servidor), `Ambito`, `Estado`, `IdPaciente` y `NombreDelPaciente` (nulos si el paciente está de baja), `PacienteDeBaja`, `RegistradaDespuesDeSuHorario`. `DetalleDeCita` agrega `EstadoPago` y `Version`. `ResultadoDeAgenda` agrega `Desde`, `Hasta`, `HoyEnHermosillo` (para marcar el día actual sin reloj del navegador), la vista y `HayMas`. Los `ToString` no imprimen valores personales, como en B02. Los nombres no llevan códigos de etapa ni fechas (AGENTS §1).

### 3.2. Permisos por operación y rol

| Operación | Permisos que exige | Usuaria | Superusuario | Observación |
| --- | --- | --- | --- | --- |
| `ConsultarAgendaAsync`, `ObtenerCitaAsync` | `ConsultarAgenda` y `ConsultarPacientes` | Sí | Sí, solo datos de prueba | La agenda muestra nombres de pacientes: sigue regida por `ConsultarPacientes`, sin crear un camino alterno. |
| `AgendarAsync` | `AgendarCitas` y `ConsultarPacientes` | Sí | Sí, solo datos de prueba | Lee condición y ámbitos del paciente, y sus rechazos dicen si está de baja. Mismo criterio que Registrar en B02. |
| Cambiar el pago, reprogramar, cancelar, dar de baja o recuperar citas, consultar citas de baja | Ninguno nuevo | No | No | B04, B05 y B06; no se conceden permisos por suposición. |

Con sesión ausente o revocada, cuenta desactivada o contraseña temporal pendiente, cada operación responde `SinPermiso` antes de leer. El capturista futuro (HU-F05) no tiene rol: no recibe nada. La matriz de B01 exige una decisión por rol para cada permiso nuevo, y su prueba de completitud fallará hasta que estén las dos. El valor del Superusuario sigue limitado a datos de prueba, como en B02; antes de datos reales se vuelve a decidir con R04.

### 3.3. Selección segura del paciente y contrato de Pacientes

**Flujo de la página** (sin contrato nuevo para ella): abre el formulario y genera `idSolicitud`; busca con `IServicioPacientes.BuscarAsync`, que solo devuelve fichas vigentes; muestra los ámbitos del elegido con `ObtenerAsync` y envía `IdPaciente` y `Ambito`. El servidor **no confía** en lo que muestra la página: vuelve a comprobar condición y ámbito.

**Contrato para Citas.** ADR-03 y la prueba de arquitectura exigen que Citas use solo tipos públicos de Pacientes y que Pacientes no dependa de Citas. Propongo `IConsultaDePacientesParaCitas` en Application/Pacientes/Interfaces/Servicios, con modelos públicos sin datos personales:

| Método | Qué devuelve | Permiso que comprueba |
| --- | --- | --- |
| `ConsultarCondicionYAmbitosAsync(idPaciente)` | Si existe, si está vigente o de baja y qué ámbitos tiene hoy. Una consulta SQL. | `ConsultarPacientes` |
| `ConsultarNombresParaAgendaAsync(idsPacientes)` | Nombre solo de los pacientes vigentes; para uno de baja no hay nombre ni indicio de ficha. Una consulta SQL. | `ConsultarPacientes` |

El puerto de Pacientes (`IRepositorioPacientes`) recibe los dos métodos de lectura correspondientes; no se crea un segundo repositorio. Con dos consultas (citas del periodo y nombres), la agenda no hace un `JOIN` entre módulos y es Pacientes quien decide qué nombre se puede mostrar.

**Rechazos al agendar** (quien agenda ya tiene `ConsultarPacientes`, y B02 ya decidió que abrir una baja dice «Está dada de baja»): paciente no encontrado, paciente de baja (con el texto fijo de B02) y ámbito que el paciente no tiene. El rechazo de la base por baja simultánea (`TE001`) responde el mismo texto de baja.

### 3.4. Agenda: periodo, límites, datos y estados

| Tema | Propuesta |
| --- | --- |
| Periodo | Dos fechas locales de Hermosillo, ambas inclusivas. Sin fechas, hoy y los seis días siguientes. Con una sola o con `desde` posterior a `hasta`, o más de 35 días: `Exito = false`, mensaje de periodo inválido y **sin consultar datos** (CU05 A4). Vista desconocida o fechas no representables se rechazan antes de consultar. |
| Conversión | `HoraHermosillo.AInstante(desde, 00:00)` hasta `AInstante(hasta + 1 día, 00:00)`, intervalo semiabierto. Hermosillo no cambia de hora, pero la conversión valida ambigüedad en cualquier caso. Antes de sumar el día o convertir, se rechaza hasta = DateOnly.MaxValue y cualquier extremo no representable, sin error 500. Se incluyen las citas que se solapan con el periodo (`Fin > inicio` y `Inicio < fin`), no solo las que empiezan en él. |
| Vistas | `Activas` (agendada y vigente) y `Canceladas` (cancelada y vigente), separadas: una cancelada nunca aparece en la agenda activa ni ocupa horario. Las citas de baja no aparecen nunca; su consulta es de B06. |
| Orden y límite | Por inicio y luego por identificador, estable. Hasta 500 filas (35 días por 14 citas posibles al día, ver 3.5); si hay más, `HayMas` y la página pide acotar. |
| Datos visibles | Horario local, ámbito, estado, paciente (nombre) y la leyenda «Registrada después de su horario». El estado de pago, la versión y el detalle llegan solo en `ObtenerCitaAsync`. |
| Estado vacío | `Exito = true` con lista vacía: no es un error (L-CU05-03). |
| Cancelada de un paciente de baja | Horario, ámbito y estado visibles; `PacienteDeBaja = true`, sin nombre ni identificador del paciente (no hay enlace a su ficha). B02 solo permite «Está dada de baja» y su consulta nueva queda en B06. |
| Detalle | `ObtenerCitaAsync(idCita)`: la cita vigente con los mismos datos y el pago. Una cita de baja o inexistente responde «no se encontró», sin distinguirlas. |
| Auditoría de lectura | Ninguna, igual que las consultas de B02 (decisión 9 de AGENTS §3); se revisa con R04 antes de datos reales. |
| Hora mostrada | Convertida en el servidor con `HoraHermosillo.ALocal`; la página no convierte nada (L-CU05-02). |

### 3.5. Horario: regla, conversión y citas anteriores

**Regla decidida (01-nov), sobre la fecha y las horas de pared de Hermosillo, igual los siete días:**

1. Inicio y fin con la misma fecha: el modelo de entrada solo tiene una fecha, de modo que «18:00 a 10:00 del día siguiente» no es expresable y se rechaza como fin anterior al inicio.
2. Sin segundos ni fracciones en ninguna de las dos horas.
3. Inicio desde las 9:00, fin hasta las 19:00 e inicio anterior al fin.
4. Si el inicio es anterior a las 15:00, dura exactamente una hora, aunque termine después de las 15:00 (14:30–15:30 sí). Desde las 15:00 dura 30 o 60 minutos.

Resultado: con una hora, inicios de 9:00 a 18:00 (541 minutos posibles); con 30 minutos, de 15:00 a 18:30 (211); en total **752 combinaciones válidas** de entre 1 440 × 1 440. Las 14 sesiones como máximo por día son 6 de una hora antes de las 15:00 y 8 de media hora después.

**Dónde vive.** Una clase nueva de Domain, `ReglasDeHorarioDeCita`, con constantes en un contenedor sin lógica (`HorarioDeAtencion`) y mensajes fijos en otro (`MensajesDeHorarioDeCita`). Recibe fecha y horas locales y devuelve los dos instantes UTC convertidos con `HoraHermosillo.AInstante`; `ReglasDeCita.Agendar` conserva su firma. El servicio es la única entrada de las páginas: llama primero al horario y después a `Agendar`.

**Por qué no también en SQL (D3).** Una sola copia: ya no hay dos reglas que mantener ni comparar, y la comparación de los 1 440 minutos (ahora, 1 440 × 1 440 combinaciones) es una prueba unitaria que se prevé comprobar contra una lista independiente de las 752 válidas. Un CHECK exigiría convertir con la zona dentro de la restricción, tendría que dejar pasar las citas anteriores (`NOT VALID`) y cualquier cambio futuro del horario sería otra migración. El costo de no tenerlo en SQL es que la base no defiende el horario ante una escritura directa; el usuario de la aplicación solo escribe a través del servicio, y los cruces y la baja, que sí dependen de la concurrencia, siguen en la base.

**Citas y pruebas anteriores fuera de horario (D12).** Hay ocho archivos de prueba que crean citas con horas arbitrarias (por ejemplo 23:45 de Hermosillo o 10:00 UTC): `BajaPacienteTests`, `ConsultaAuditoriaRevision`, `HorasDockerTests`, `PacientesFlujoTests`, `RecuperacionTests`, `RevisionDatosTests`, `CoordinadorDeBajaDePacienteTests` y `ReglasDeCitaTests`. Como la regla no entra en `ReglasDeCita.Agendar` ni en SQL, ninguna cambia, no hay migración y la agenda lee cualquier cita guardada sin validarla. B05 usará la misma clase para el nuevo intervalo de una cita reprogramada.

**«Registrada después de su horario».** Se calcula al leer, con `FechaAlta > Inicio` en UTC, en la proyección de la consulta, con la hora del servidor (`TimeProvider`, la misma que llena `FechaAlta`) y nunca con la de la página. Sin campo ni migración. El evento de alta usa dos textos fijos: «Se agendó una cita.» y «Se agendó una cita registrada después de su horario.»; los campos afectados son solo nombres (`IdPaciente`, `Ambito`, `Inicio`, `Fin`, `Estado`, `EstadoPago`).

### 3.6. Cruces, paciente vigente y baja simultánea

| Protección | Quién la impone | Qué agrega B03 |
| --- | --- | --- |
| Cruce con una cita activa, también entre ámbitos; contiguas permitidas | `EX_Cita_SinCruceActivo` (única fuente), traducida a `CruceDeHorarioException` | Mensaje fijo de horario ocupado, sin exponer datos de la otra cita. No hay consulta previa de cruce: una segunda comprobación solo agregaría otro momento que forzar. |
| Paciente vigente al guardar | `FnCitaActivaExigePacienteVigente` (`FOR SHARE` sobre el paciente hasta confirmar) | Lectura previa por el contrato de Pacientes para dar un mensaje claro; la base decide en la carrera. |
| Una baja después de una cita confirmada | `FnPacienteDeBajaSinCitasActivas` | Nada: el coordinador de baja y su rechazo con citas activas no cambian. |
| Baja y cita simultáneas | Los dos disparadores juntos | Nada nuevo. La cita por el servicio entra por el mismo `INSERT`, de modo que ambos órdenes quedan cubiertos por la protección de B00; la prueba por servicios es el QA de Lucía (L-CU04-06). |
| Solo agendada y vigente ocupa horario | El predicado de la restricción | Nada. |
| Dos y diez reservas distintas por el mismo horario | La restricción: las demás esperan al primero y reciben el conflicto | Cada una con su propio `idSolicitud`; solo una guarda cita y evento. |

### 3.7. Ámbito retirado mientras se agenda (D10)

El ámbito se comprueba **una vez**, con una lectura sin bloqueo, antes de guardar. No hay llave ni disparador que ate `Cita.Ambito` a `PacienteAmbito`, y `AgendarAsync` no lo vuelve a comprobar dentro de la transacción. Si el retiro se confirma entre la comprobación y el guardado, la cita se guarda con el ámbito elegido, que es lo decidido el 01-nov. Lo que protege ese orden:

- El servicio no toma ningún bloqueo sobre `PacienteAmbito` y no restaura el ámbito al paciente. Citas no escribe nunca en Pacientes.
- La edición del paciente (B02) solo borra filas de `PacienteAmbito` y actualiza `Paciente`; ninguna ruta de código toca `Cita.Ambito`. Las citas históricas conservan el suyo.
- El único acoplamiento es el bloqueo del paciente que toma la restricción al insertar la cita (3.11, sondeo 2): si la edición llega **durante** el insertado, espera a que se confirme la cita; si llega **antes** de él, se confirma sin esperar. En ambos casos la cita conserva su ámbito.
- Citas históricas con ámbito que el paciente ya no tiene son válidas y se muestran igual. Qué hacer al reprogramar o recuperarlas sigue pendiente de Dulce (B05/B06); no se resuelve aquí.

Alternativa descartada: bloquear al paciente al comprobar. Daría una comprobación más fuerte, pero la edición pasaría a esperar a quien está agendando y la prueba prevista en el plan B03 (4.1, «otra solicitud confirma su retiro» mientras agendar está en pausa) quedaría convertida en una espera.

### 3.8. Reenvío de la misma reserva

**Mecanismo (D2).** `idSolicitud` es el `IdCita`, generado por la página al abrir el formulario y conservado en un campo oculto mientras sea la misma reserva (incluso tras un error o una pérdida de respuesta). La base ya rechaza una clave repetida de Cita; hoy se traduce solo la de Paciente, así que se agrega `PK_Cita` (`IdentificadorDeCitaRepetidoException`). No hay tabla ni servicio de solicitudes nuevos, igual que decidió B02. El identificador no es secreto ni concede acceso.

**Orden de AgendarAsync:** 1) sesión y permisos; 2) identificador, entrada, horario y fechas representables, sin leer datos; 3) lectura por número sin rastreo; 4) condición y ámbitos del paciente; 5) guardado de cita/evento. Si tras la primera lectura hay un rechazo de negocio por paciente/ámbito o un rechazo conocido de base, se usa la misma resolución por relectura antes de contestar. Retirar sesión/permisos responde SinPermiso sin conceder otra lectura.

**Una sola resolución del reenvío:** volver a validar sesión y permisos; releer por número sin rastreo. Comparar actor original, IdPaciente, Ambito, Inicio y Fin en UTC, y exigir cita agendada y vigente. Hay tres salidas:

- Coincide y sigue activa: YaAgendada, sin segunda cita ni evento.
- Existe pero otro actor/datos/estado impiden reconocerla: SolicitudYaUsada, sin contenido ajeno.
- No existe: conservar el rechazo original; cruce → «horario ocupado»; TE001 → texto fijo de paciente de baja; clave repetida sin cita → error 500, igual que B02.

**Rechazos incluidos:** paciente y ámbito posteriores a la primera lectura y, desde la base, solo clave repetida de Cita, cruce y TE001. Fallo de auditoría o inesperado: error 500 sin relectura ni anuncio de éxito. No depende del orden de las restricciones. B02 relee antes de devolver parecidos; se aplica el mismo patrón antes del rechazo.

El caso que exige este ajuste hoy es el retiro del ámbito. No se afirma que el paciente pueda darse de baja con la cita gemela activa: el disparador de baja lo impide.

| Situación | Respuesta |
| --- | --- |
| Primer intento confirmado antes de empezar el segundo, mismos actor y datos, cita aún activa | `YaAgendada`; sin segunda cita ni evento. |
| Primer intento confirmado después de la primera lectura del segundo, antes de su comprobación del paciente o de su guardado | El rechazo posterior de paciente/ámbito o guardado pasa por la misma relectura: YaAgendada si coincide; SolicitudYaUsada si existe sin poder reconocerse; rechazo original si no existe. |
| Primer intento todavía sin confirmar | El segundo espera la confirmación o reversión del primero y entonces cae en la fila anterior o en la siguiente. |
| Primer intento revertido o fallido | No hay fila: el segundo guarda con normalidad y deja su único evento. Solo una operación confirmada deja evento. |
| Mismo número con datos distintos, o de otro actor | `SolicitudYaUsada`, sin mostrar la otra cita. |
| Misma cita ya cancelada o dada de baja | `SolicitudYaUsada`: no se anuncia como agendada. |
| Permiso retirado o sesión revocada | `SinPermiso`, antes de leer, también en el reenvío y en la relectura. |
| Paciente o ámbito cambiados después de la reserva original | Un reenvío de una cita existente se reconoce sin repetir la comprobación del paciente ni del ámbito; basta con actor, datos y permisos vigentes. |
| Otra reserva, con otro número, por el mismo horario | «Horario ocupado»: la relectura por número no encuentra nada. |

Si se pierde la respuesta, la página reenvía **el mismo número**; es seguro. No hay reintento automático.

### 3.9. Guardado, auditoría, errores y pago

- Cita y evento en la misma unidad de trabajo; el éxito se devuelve solo después de confirmar. Un fallo de auditoría o de la base sube como excepción y la aplicación responde su error 500 general, no como mensaje de validación. `ChangeTracker.Clear()` evita que lo fallido contamine la siguiente operación del mismo alcance.
- El pago inicial es Pendiente, por `ReglasDeCita.Agendar`; no se agrega ningún campo de pago al formulario y B04 no se adelanta.
- Mensajes fijos, sin nombres, fechas ni contactos. Los rechazos de validación vienen de constantes de Domain y los demás de `MensajesDeCitas`, que las páginas comparan por constante y no por texto.
- Los textos del evento y los campos son fijos (3.5); no hay valores privados en eventos ni en registros técnicos.

### 3.10. Datos, migración e impacto

**No hace falta ninguna migración para B03.** La tabla `Cita` ya tiene todo lo necesario: identificador, paciente, ámbito, inicio, fin, estado, pago, condición, versión y los seis metadatos. `FechaAlta` e `Inicio` bastan para la leyenda. Los permisos de `therapease_app` sobre `Cita` y `EventoAuditoria` (`SELECT`, `INSERT`, `UPDATE`; `SELECT`, `INSERT`) ya cubren las operaciones. Las citas y pruebas anteriores no se modifican.

**Índice opcional (D8), no recomendado todavía:** `CREATE INDEX "IX_Cita_Inicio" ON "Cita" ("Inicio")` acelera la consulta por periodo. No se aporta medición que justifique crearlo ahora; el beneficio se comprobará cuando se autorice medir. Si se decide, es una migración nueva y pequeña, aplicada con el usuario migrador; no cambia datos ni permisos; la reversión es `DROP INDEX "IX_Cita_Inicio"`; y su revisión de SQL, impacto y reversión sigue las reglas de AGENTS §2.5.

### 3.11. Dónde se separan «comprobar» y «guardar», y sondeos hechos

Los métodos están pensados para que el QA de Lucía fuerce cada momento con las técnicas ya usadas en B02 (interceptores de comandos y de `SaveChanges` de EF, que no requieren código de producción adicional):

| Operación | Momento de comprobar | Momento de guardar | Dónde se separan |
| --- | --- | --- | --- |
| Agendar | Lectura por número (`IRepositorioCitas.ObtenerParaComparacionAsync`) y condición y ámbitos del paciente (`IConsultaDePacientesParaCitas.ConsultarCondicionYAmbitosAsync`), dos consultas sin bloqueo | `IUnidadDeTrabajo.EjecutarAsync`: inserta cita (restricción y disparador) y evento, y confirma | Cada paso es un método privado propio de `ServicioCitas` con una sola llamada a repositorio o contrato; el guardado se pausa antes del `SaveChanges`, o dentro de la transacción antes de confirmar, para mantenerla abierta. |
| Rechazo y relectura | — | Repite la lectura por número tras un rechazo de la base | Mismo método de lectura que la primera, con una consulta idéntica. |
| Dar de baja (ya existe) | `ExistenActivasDePacienteAsync` | `IUnidadDeTrabajo.EjecutarAsync` | `CoordinadorDeBajaDePaciente`, sin cambios. |
| Retirar un ámbito (B02) | — | `ServicioPacientes.ActualizarAsync` | Se ejecuta en otra solicitud mientras agendar está en pausa. |

**Sondeos con PostgreSQL 17 desechable** (imagen `postgres:17`, contenedor propio, fuera del repositorio y sin pruebas de B03; copia el SQL de la migración inicial):

1. **Clave repetida frente a cruce propio.** Insertar de nuevo una cita con el mismo identificador y el mismo horario falla con `23505` sobre la clave primaria; otra cita con otro identificador y horario cruzado falla con `23P01` sobre `EX_Cita_SinCruceActivo`. Por eso el diseño no depende del orden de las restricciones.
2. **Edición del paciente frente a una cita sin confirmar.** Con una transacción que inserta una cita y aún no confirma, la actualización de `Paciente` desde otra sesión espera: tardó unos 4 s, hasta que la primera confirmó. Es el `FOR SHARE` del disparador. Consecuencia para QA: una edición de paciente que llega durante el insertado espera, y no es un fallo.

### 3.12. Pruebas previstas, todavía sin escribir ni ejecutar

Miguel escribe en el mismo PR sus unitarias y las regresiones de su implementación. El QA dirigido es de Lucía y no se sustituye.

| Nivel | Qué cubre | Casos del plan |
| --- | --- | --- |
| Unitarias de Domain | Horario: las 752 combinaciones válidas y el resto rechazadas, con una lista independiente; ejemplos del plan; segundos; fin anterior al inicio; fecha única; leyenda con `FechaAlta` mayor, igual y menor que `Inicio`; conservación de pago Pendiente y estado inicial (existentes). | M-13 a M-16, L-CU06-01 y -03 |
| Unitarias de Application | `ServicioCitas` con dobles: permisos por rol y por estado de sesión, entradas faltantes, paciente no encontrado, de baja y ámbito no permitido; cruce, clave repetida y baja simultánea simuladas; los nueve estados del reenvío de 3.8, con las tres salidas de relectura; idSolicitud vacío, vista desconocida, fecha máxima y conversión UTC fuera de rango; fallo de auditoría o de la base sin éxito; agenda: periodo inválido sin consultar, vacío, vistas, límite de 500, canceladas de baja sin nombre, conversión de horas a Hermosillo; `IConsultaDePacientesParaCitas` y la matriz de permisos con los dos nuevos. | L-CU05-01 y -03, L-CU06-01 a -04, -08 y -09 |
| Integración de Miguel (D9) | Traducción de `PK_Cita`; cita y evento atómicos y fallo de auditoría; reenvío secuencial y con la gemela confirmada entre la primera lectura y el guardado, como en B02; gemela confirmada antes del rechazo de ámbito; cruce con dos reservas, de apoyo. El QA dirigido por Lucía conserva sus momentos forzados. | L-Q03-01/02, L-CU06-05 y -09 |
| QA de Lucía | Agendar frente a baja en sus tres órdenes (plan B03 4.1), ámbito retirado, diez reservas simultáneas, zonas horarias, páginas y navegador. | L-CU04-06 y -07, L-CU06-05 a -07 |

Si una regla estuviera en SQL y en .NET, se compararían todos sus minutos; con la propuesta (D3) hay una sola copia y se prueba en la unitaria. Las pruebas existentes cambian solo donde cambie una firma o la lista de permisos (por ejemplo `PermisosUsuariosTests`), sin reducir comprobaciones, y su lista se entrega en la evidencia, como en B02.

### 3.13. Contrato para Dulce

El PR incluye el contrato de B03 en `docs/planning` con firmas, campos, permisos, validaciones, conversión de horas, resultados, conflictos, reenvío y ejemplos ficticios, al estilo del de B02. Lo que fija ya esta propuesta para las páginas:

- Generar el `idSolicitud` al abrir el formulario de agendar y conservarlo en un campo oculto; una reserva nueva usa otro número. Reenviarlo tras un error o una pérdida de respuesta es seguro.
- Enviar `IdPaciente`, `Ambito`, fecha y las dos horas de pared de Hermosillo; nunca zona ni desfase. Las horas con `type="time"` y paso de un minuto.
- Elegir al paciente con la búsqueda de B02 y mostrar sus ámbitos con la ficha; el servidor repite las comprobaciones.
- Decidir la navegación por `Estado`, no solo por `Exito`; mostrar «Registrada después de su horario» con el dato `RegistradaDespuesDeSuHorario`; mostrar «Paciente dado de baja» sin enlace cuando `PacienteDeBaja`.
- Comparar los mensajes con constantes, mostrarlos como texto y no reintentar automáticamente. Un 500 se resuelve reenviando el mismo número.
- Pedir `ConsultarAgenda`, `AgendarCitas` y `ConsultarPacientes` con políticas web nuevas.

## 4. Un solo PR

### 4.1. Contenido

- **Domain:** `ReglasDeHorarioDeCita`, `HorarioDeAtencion`, `MensajesDeHorarioDeCita`, `IdentificadorDeCitaRepetidoException`; dos permisos y su decisión por rol en `MatrizDePermisos`.
- **Application:** `IServicioCitas` y `ServicioCitas`; modelos, `MensajesDeCitas` y `LimitesDeAgenda`; `IConsultaDePacientesParaCitas` y su implementación en Pacientes; ampliación de `IRepositorioCitas` y `IRepositorioPacientes`.
- **Infrastructure:** consultas de agenda y detalle, lectura por número sin rastreo, lecturas del contrato de Pacientes y `PK_Cita` en `TraductorDeErroresDeBase`. Sin migración.
- **Web:** dos políticas y el registro de dependencias, dentro del límite aprobado; sin PageModel ni páginas.
- **Pruebas** (3.12), **contrato para Dulce** y actualización de documentos: plan B03 y su estado, evidencia de B03, trazabilidad y cobertura de 08D, README y la nota ADR-04 posterior (4.3). Los identificadores del plan de pruebas se conservan; su columna de estado la actualiza Lucía al fusionar.

### 4.2. Orden

1. Decisión de Lucía D1–D13 completada; incorporar estas precisiones documentales. 2. Rama nueva desde main actualizado. 3. Domain y permisos. 4. Contrato de Pacientes y repositorios. 5. Servicio, traducción de errores y registro de dependencias. 6. Unitarias y regresiones. 7. Contrato para Dulce y documentos. 8. Compilación con advertencias como errores, `has-pending-model-changes` (sin migración, debe mostrar que no cambió el modelo), unitarias, integración completa y CI. 9. Revisión humana distinta y aprobación final de Lucía.

### 4.3. Nota ADR-04 propuesta (D13)

«Nota posterior B03: el alta de una cita se reconoce por su identificador generado al abrir el formulario. Se validan sesión/permisos y, para reconocer lo confirmado, actor, paciente, ámbito, inicio y fin UTC y estado agendada/vigente. Todo rechazo de negocio posterior a la primera lectura (paciente/ámbito, clave, cruce o TE001) pasa por la misma relectura con permisos actuales: coincidencia → YaAgendada; existencia no reconocible → SolicitudYaUsada; ausencia → rechazo original. Sin cita: cruce → horario ocupado; TE001 → texto de baja; clave repetida → 500. Auditoría/errores inesperados → 500, sin relectura. No hay tabla ni servicio de solicitudes nuevos. Horario en Domain; cruces y baja simultánea en la base. No resuelve R04 ni autoriza datos reales, B04 a B07, nube o gastos.»


## 5. Riesgos y puntos técnicos para Lucía

| Punto | Descripción y manejo |
| --- | --- |
| Restricción de cruce global | `EX_Cita_SinCruceActivo` no distingue profesionales: vale mientras «solo yo» sea la única usuaria operativa. Si hubiera más de una agenda, haría falta una columna y una migración. No se cambia. |
| Horario solo en .NET | La base no defiende el horario ante una escritura directa. Mitigación: el usuario de la aplicación escribe solo por el servicio; la regla es una sola copia y se prueba con todas sus combinaciones. |
| Relectura tras cada rechazo | Agrega una consulta en el camino de error y un momento más para forzar. Es lo que evita «horario ocupado» contra la propia cita sin depender del orden de las restricciones. |
| Espera de la edición del paciente | Una edición (por ejemplo, retirar un ámbito) espera a una cita en curso. Es breve y correcta, pero el QA no debe tomarla por un fallo (3.11). |
| Ámbito retirado | Puede existir una cita de un ámbito que el paciente ya no tiene. Es lo decidido; su tratamiento al reprogramar o recuperar queda para B05/B06 con Dulce. |
| Dos lecturas en la agenda | Citas del periodo y nombres de pacientes se leen en dos consultas: una instantánea no única. No afecta a las activas, porque un paciente con cita activa no puede darse de baja; para canceladas el nombre se decide en la segunda consulta. |
| Hora del servidor | La leyenda depende del reloj del servidor, como pidió Lucía. Una desviación del reloj la adelanta o la retrasa; no hay otro dato que compararle. |
| Citas anteriores fuera de horario | Se conservan; la agenda las muestra. No se interpretan como error. |
| Imagen con datos de zona | `HoraHermosillo` busca la zona por nombre y falla si la imagen no la trae; ya se comprobó en la imagen final (B00). |

## 6. Trazabilidad con los casos existentes

| Caso | Dónde se cubre en esta propuesta |
| --- | --- |
| M-13 a M-16 | 3.5 y 3.12: horario, estado inicial, pago Pendiente, cruces; unitarias de Domain y Application. |
| L-CU05-01 a -03 | 3.4: vistas, otra zona, estado vacío. |
| L-CU06-01 a -04 y -08 | 3.2, 3.3, 3.5, 3.6: alta, cruces, contiguas, canceladas y bajas, paciente y ámbito. |
| L-CU06-05 | 3.6: dos y diez reservas distintas; 3.8 las separa de los reenvíos. |
| L-CU06-06 y -07 | 3.1 y 3.5: hora de pared de Hermosillo, sin zona de entrada. |
| L-CU06-09, S5 | 3.8: respuesta perdida y reenvíos en todos sus órdenes. |
| L-CU04-06 | 3.6 y 3.11: sin cambios en la baja; momentos separados para el QA. |
| L-CU04-07 | 3.7: ámbito retirado conservado en la cita. |
| L-Q03-01/02, Q06, Q08, Q09, R02 | 3.5, 3.6, 3.8 y 3.9: eventos, atomicidad, cruces, hora local y concurrencia. |

La aprobación D1–D13 y estos ajustes no acreditan implementación, pruebas ni cierre de B03. En esta entrega Codex solo modifica documentos y el helper local B01; no programa ni ejecuta B03. Código y pruebas mantienen PR, revisión humana distinta, CI y aprobación final de Lucía. No autoriza datos reales, nube, gastos ni B04 a B07.
