# TherapEase — B03 · Contrato de las funciones del servidor para las páginas de agenda y citas

**Actualizado el 09-nov-2026.** Autor: Miguel. Para: Dulce. Revisa: Lucía. [PR #18](https://github.com/LuciAguilar/Therapease/pull/18), rama `b03/agenda-y-citas`. Lucía revisó `fe6b79a` y pidió las correcciones de esta versión: un `40P01` ya no se confunde con «horario ocupado» y la agenda serializa las altas con un bloqueo de asesoría dentro de un disparador de `Cita`. Detalle de lo comprobado en la [evidencia](../testing/TherapEase-09-B03-evidencia-servicios.md).

**18-nov-2026 · B04.** Se añade la función para cambiar el estado de pago de una cita (sección 4.4), según la propuesta técnica B04 aprobada por Lucía el 17-nov-2026 (D1–D8). Es solo servidor: las páginas siguen siendo de Dulce. No cambia ninguna de las tres funciones anteriores.

Concreta la sección 3.13 de la [propuesta aprobada](TherapEase-etapa09-B03-propuesta.md) (D1–D13, aprobadas por Lucía el 05-nov-2026). El [contrato de B01](TherapEase-etapa09-B01-contrato-para-paginas.md) sigue siendo la referencia para llamar al servidor, usar `RespuestaServicio<T>`, mostrar el error 500, proteger formularios y evitar que se guarden páginas privadas en caché. El [contrato de B02](TherapEase-etapa09-B02-contrato-para-paginas.md) explica la búsqueda y la ficha de pacientes, que la página de agendar usa para elegir al paciente.

Explica cómo llamar desde un PageModel a las cuatro funciones de citas, qué responden y qué debe hacer la página con cada resultado. Las páginas, los formularios, los textos en pantalla y la navegación son de Dulce. **No contiene páginas ni código de interfaz.** Solo datos ficticios.

| Estado | Qué significa en este documento |
| --- | --- |
| ✅ | Ya está en el código de la rama y lo comprueban pruebas unitarias o de integración (sección 8). |
| ⏳ | Pendiente de las páginas, del QA de Lucía o de su revisión. |

## 1. Reglas para todas las páginas de agenda y citas

- Pedir por constructor `IServicioCitas` (`TherapEase.Application.Citas.Interfaces.Servicios`). **Nunca** usar la base de datos, los repositorios ni las entidades o reglas de Domain. Para elegir al paciente, usar `IServicioPacientes` (B02); `IConsultaDePacientesParaCitas` es el contrato que usa el propio servidor de citas y **no es para las páginas**.
- **El actor sale de la sesión.** Ninguna función recibe quién lo hace.
- Desde Domain, una página solo puede usar enumeraciones y constantes (AGENTS §1): `AmbitoAtencion`, `EstadoCita`, `EstadoPago`, `Permiso`, `HorarioDeAtencion` (límites del horario, para ayudar a capturar) y `MensajesDeHorarioDeCita`. Desde Application puede usar `MensajesDeCitas` y `LimitesDeAgenda`. Los modelos están en `TherapEase.Application.Citas.Modelos`.
- **Cada función vuelve a comprobar los permisos en el servidor** antes de leer o escribir; ocultar un botón no sustituye eso. Mensaje de rechazo: `MensajesDeCitas.SinPermiso`.
- Políticas de página (en `PoliticasDeAutorizacion`): `ConsultarAgenda`, `AgendarCitas`, `CambiarPagoDeCitas` (B04) y, para elegir al paciente, `ConsultarPacientes`. Las tienen Usuaria y Superusuario (este último solo con datos de prueba); con contraseña temporal pendiente no se entra a ninguna.
- Consultar la agenda **y** agendar exigen también `ConsultarPacientes`, porque muestran o leen datos del paciente. Quien no la tiene recibe `SinPermiso` aunque tenga el otro permiso.
- Cambiar el pago exige `CambiarPagoDeCitas` **y** el par del detalle de la cita (`ConsultarAgenda` y `ConsultarPacientes`). Una página de pago declara las tres políticas, como la de editar una ficha declara las dos que usa.
- Mostrar `Mensaje` **como texto** y no decidir nada leyendo su contenido: comparar con las constantes de `MensajesDeCitas` o de `MensajesDeHorarioDeCita`.
- Los modelos **no imprimen valores en `ToString`**. No registrar su contenido.
- Formularios: antifalsificación activa, modelo de entrada propio y salida codificada. Los datos privados llevan `Cache-Control: no-store` automáticamente en las páginas autorizadas.

## 2. Modelos que usa la página

| Modelo | Contenido |
| --- | --- |
| `DatosDeCitaEntrada` | `IdPaciente` (`Guid?`), `Ambito` (`AmbitoAtencion?`), `Fecha` (`DateOnly?`), `HoraInicio` y `HoraFin` (`TimeOnly?`). Todo es anulable: un campo que no llegó se rechaza como `SolicitudInvalida` y nunca se confunde con 0001-01-01, 00:00 o el primer ámbito. |
| `CitaDeAgenda` | `IdCita`, `InicioLocal` y `FinLocal` (`DateTime`, **ya en hora de Hermosillo**), `Ambito`, `Estado`, `IdPaciente` y `NombreDelPaciente` (**nulos si el paciente está de baja**), `PacienteDeBaja` y `RegistradaDespuesDeSuHorario`. |
| `DetalleDeCita` | Lo mismo que `CitaDeAgenda` más `EstadoPago` y `Version`. |
| `ResultadoDeAgenda` | `Citas`, `Desde`, `Hasta`, `HoyEnHermosillo` (para marcar el día actual sin el reloj del navegador), `Vista` y `HayMas`. |
| `ResultadoDeAgendar` | `Estado` (`Agendada` o `YaAgendada`) y `Cita` (un `DetalleDeCita`). |
| `EstadoDePagoDeCita` | `IdCita`, `EstadoPago`, `Estado` (`EstadoCita`) y `Version`. Sin horario, ámbito, paciente, nombre, fechas ni actores. |
| `ResultadoDeCambioDePago` | `Estado` (`Actualizado` o `SinCambios`) y `Pago` (un `EstadoDePagoDeCita`, con la versión que quedó). |

`VistaDeAgenda` tiene dos valores: `Activas` (agendada y vigente) y `Canceladas` (cancelada y vigente). Una cita cancelada **nunca** aparece en la vista de activas ni ocupa horario; las citas dadas de baja no aparecen nunca (su consulta nueva es de B06).

## 3. Cómo envía la página la fecha y la hora

Solo la **hora de pared de Hermosillo**: un campo de fecha (`yyyy-MM-dd`, `DateOnly`) y dos campos de hora (`HH:mm`, `TimeOnly`), sin zona ni desfase. **La zona del navegador nunca entra**: si se agenda desde otro país, la hora acordada llega igual (HU09 CA4). Usar `type="time"` con paso de un minuto. La página no convierte nada: el servidor convierte con `HoraHermosillo` y devuelve las horas ya en Hermosillo.

La opción de que la página capture inicio y duración y calcule el fin es compatible: el servicio recibe siempre inicio y fin y valida igual. Solo hay una fecha, así que **una cita no puede cruzar la medianoche**: «18:00 a 10:00 del día siguiente» se rechaza como fin que no es posterior al inicio.

## 4. Función por función

### 4.1. Consultar agenda — CU05

`ConsultarAgendaAsync(DateOnly? desde, DateOnly? hasta, VistaDeAgenda vista = Activas)` → `RespuestaServicio<ResultadoDeAgenda>`

Requiere `ConsultarAgenda` **y** `ConsultarPacientes`.

- **Periodo:** dos fechas de Hermosillo, **ambas incluidas**, de 1 a 35 días (`LimitesDeAgenda`). Sin fechas se consulta hoy y los seis días siguientes. Una sola fecha, `desde` posterior a `hasta`, más de 35 días, una vista desconocida o una fecha que no se pueda representar (por ejemplo, 31-dic-9999) dan `Exito = false` con `MensajesDeCitas.PeriodoInvalido` y **sin consultar datos**.
- Se incluyen las citas que **se solapan** con el periodo, no solo las que empiezan en él: una que termina a las 00:30 del primer día aparece; una que termina justo a las 00:00 no.
- Orden estable por inicio y luego por identificador. **Hasta 500 filas**; si hay más, `HayMas = true`: pedir que se acote el periodo, no afirmar que la lista esté completa.
- Un periodo sin citas es un resultado normal (`Exito = true`, lista vacía), no un error.
- **Cancelada de un paciente de baja:** se muestra el horario, el ámbito y el estado con la leyenda `MensajesDeCitas.LeyendaPacienteDeBaja` («Paciente dado de baja»); `PacienteDeBaja = true` y **no hay nombre ni identificador**, así que no se enlaza a su ficha. B02 solo permite «Está dada de baja» y la consulta nueva de bajas queda en B06.
- **«Registrada después de su horario»** (`MensajesDeCitas.LeyendaRegistradaDespuesDeSuHorario`): se muestra cuando `RegistradaDespuesDeSuHorario` es verdadero. Se calcula en el servidor con su reloj (cita guardada cuando ya había empezado); la página nunca envía ni calcula esa hora.
- Las citas de prueba anteriores que quedaron fuera del horario nuevo se muestran **tal cual**: la agenda no valida lo ya guardado.
- La consulta **no escribe nada ni registra evento**.

### 4.2. Obtener una cita — CU05

`ObtenerCitaAsync(Guid idCita)` → `RespuestaServicio<DetalleDeCita>`

Requiere `ConsultarAgenda` y `ConsultarPacientes`. Una cita **dada de baja o inexistente** responde lo mismo: `MensajesDeCitas.CitaNoEncontrada`; no se distinguen. Una cancelada vigente sí se muestra, con su estado. `Version` y `EstadoPago` solo llegan aquí, no en la agenda. Esta es también la consulta del pago para el formulario de la sección 4.4: la `Version` que muestra es la que debe enviarse. La consulta del pago de una cita de baja queda para B06 (decisión de Lucía, 17-nov-2026).

### 4.3. Agendar — CU06

`AgendarAsync(Guid idSolicitud, DatosDeCitaEntrada datos)` → `RespuestaServicio<ResultadoDeAgendar>`

Requiere `AgendarCitas` **y** `ConsultarPacientes`.

**Elegir al paciente.** Con la búsqueda de B02 (`IServicioPacientes.BuscarAsync`, que solo devuelve fichas vigentes) y mostrando los ámbitos de la ficha con `ObtenerAsync`; enviar `IdPaciente` y `Ambito`. **El servidor no confía en lo que muestra la página:** vuelve a comprobar la condición y el ámbito.

**El número de la solicitud.** `idSolicitud` es el identificador que tendrá la cita. La página lo crea (`Guid.NewGuid()`) al **abrir el formulario** y lo conserva, por ejemplo en un campo oculto, mientras sea la misma reserva, incluido un reenvío tras un error o una pérdida de respuesta. Una reserva nueva usa un número nuevo. No es un secreto ni concede acceso.

| Resultado | `Exito` | `Datos.Estado` | Qué hace la página |
| --- | --- | --- | --- |
| Agendada | Verdadero, solo tras confirmar cita y evento | `Agendada` | Anunciar el guardado y mostrar `Datos.Cita`, con la leyenda de registro tardío si corresponde. |
| Ya estaba agendada | Verdadero, solo tras reconocer lo confirmado | `YaAgendada` | Era la misma reserva de antes; anunciar que **ya estaba agendada** (`MensajesDeCitas.CitaYaAgendada`). No hay segunda cita ni evento. |
| Rechazo | Falso, `Datos` nulo | — | Mostrar el mensaje fijo y conservar lo capturado. |

- **Decidir la navegación por `Datos.Estado`, no solo por `Exito`.**
- **Rechazos posibles:** `SinPermiso`; `SolicitudInvalida` (datos nulos, número vacío, algún campo faltante, ámbito desconocido o paciente vacío); los de horario de `MensajesDeHorarioDeCita` (sección 5); `PacienteNoEncontrado`, `PacienteDeBaja` («Está dada de baja.», el mismo texto de B02) y `AmbitoNoPermitido` (el paciente no tiene ese ámbito): tres mensajes distintos porque quien agenda ya puede consultar pacientes; `HorarioOcupado`; y `SolicitudYaUsada`.
- **Cruces:** solo las citas agendadas y vigentes ocupan horario, **también entre ámbitos**. Las citas contiguas (una termina cuando empieza la otra) se permiten. Una cancelada libera su horario. `HorarioOcupado` no revela datos de la otra cita.
- **Fecha u hora pasada:** se permite, con las mismas reglas. La cita lleva la leyenda de registro tardío y su evento lo indica con un texto fijo.
- **Ámbito retirado mientras se agenda:** si el retiro del ámbito se confirma después de la comprobación del servidor, la cita **conserva** el ámbito elegido y el servidor no se lo devuelve al paciente. Si todavía no hay cita confirmada, el ámbito retirado rechaza el alta con `AmbitoNoPermitido`.
- **Reservas distintas por el mismo horario:** con dos y con diez solicitudes simultáneas (lo ensayado) solo una guarda; las demás reciben `HorarioOcupado` (sección 6).

**Reenvío de la misma reserva.** Reenviar el mismo número es seguro. El servidor lo reconoce con una relectura por número, con permisos vigentes, y hay tres salidas:

| Situación | Respuesta |
| --- | --- |
| Misma persona, mismos datos y la cita sigue agendada y vigente, ya confirmada | `YaAgendada`, sin segunda cita ni evento. Vale aunque el paciente se haya dado de baja o su ámbito se haya retirado después de la reserva original: un reenvío de una cita existente no repite esas comprobaciones. |
| El número ya existe pero **no es la misma reserva**: otra persona, otros datos, otro paciente o ámbito, o la cita se canceló o se dio de baja | `SolicitudYaUsada`, sin mostrar la otra cita. Abrir de nuevo el formulario con un número nuevo. |
| No existe ninguna cita con ese número | El rechazo original: horario ocupado, texto de baja del paciente, etcétera. |

Si el permiso o la sesión ya no son válidos al comprobarlos, el servidor responde `SinPermiso`, también al reconocer un reenvío o releer por número. Si la cita ya se confirmó y después Pacientes niega la lectura del nombre, se conserva lo guardado y se responde `Agendada` sin nombre y con `PacienteDeBaja = false`; una reserva propia ya reconocida también puede devolverse como `YaAgendada` sin nombre. No se deshace una reserva confirmada por ese fallo de lectura. Si se pierde la respuesta, **volver a enviar el mismo número** y leer el resultado (`Agendada` o `YaAgendada`); no abrir un número nuevo, porque eso crearía una segunda cita. No hay reintento automático. La página muestra la cita sin nombre y no la presenta como paciente de baja.

### 4.4. Cambiar el estado de pago — CU09 (B04)

`CambiarPagoAsync(Guid idCita, int versionEsperada, EstadoPago? nuevoPago)` → `RespuestaServicio<ResultadoDeCambioDePago>`

Requiere `CambiarPagoDeCitas`, `ConsultarAgenda` **y** `ConsultarPacientes`. Solo hay dos valores, `Pendiente` y `Pagado`: sin monto, abono, método ni cobro.

**Qué envía el formulario.** El `idCita` de la ruta, la `Version` que mostró `ObtenerCitaAsync` (un entero desde 1, en un campo oculto) y el valor elegido. Nada más tiene dónde llegar al servicio: horario, paciente, ámbito, estado, condición, actor, fechas o montos que se envíen de más **se ignoran**, igual que en B01–B03. El actor sale de la sesión. `nuevoPago` es anulable para que un campo ausente no se tome por `Pendiente`.

**Orden de las comprobaciones.** Primero sesión y permisos (sin ellos no se averigua si la cita existe ni si la entrada era válida); después la forma de la entrada, sin consultar datos; después una sola lectura de la cita; y en la regla, por este orden: cita de baja, versión distinta y mismo pago.

| Caso | Resultado | Qué hace la página |
| --- | --- | --- |
| Sin sesión | No llega al servicio | Lo resuelve el acceso existente. |
| Sin permiso, con contraseña temporal pendiente o con la sesión revocada | `Fallida`, `SinPermiso` | Acceso denegado, como las demás páginas operativas. |
| Identificador vacío o cita inexistente | `Fallida`, `CitaNoEncontrada` | 404 con el mensaje. |
| Pago ausente o distinto de `Pendiente` y `Pagado`, o versión ausente o menor que 1 | `Fallida`, `SolicitudInvalida` | Mostrar el mensaje y conservar lo capturado. |
| Cita **dada de baja**, aunque el valor sea el mismo | `Fallida`, `PagoDeCitaDeBajaNoSeActualiza` | Mostrar el mensaje y no ofrecer el formulario. |
| Versión distinta de la vigente, aunque el pago coincida | `Fallida`, `ConflictoDeVersion` | Mostrar el mensaje, ofrecer recargar y **no** reenviar con otra versión sin que la persona vea el estado actual. |
| Versión vigente y el mismo pago | `Correcta`, `Estado = SinCambios`, `SinCambios` | Redirigir mostrando «No hubo cambios.». No se escribió nada ni hay evento. |
| Cambio confirmado | `Correcta`, `Estado = Actualizado`, `PagoActualizado` | Redirigir mostrando «Estado de pago actualizado.» con `Pago.EstadoPago` y `Pago.Version`. |
| Falla de la base o de la auditoría | Excepción | Error 500 fijo (sección 7). Ni éxito ni conflicto. |

- **Decidir por `Datos.Estado`, no solo por `Exito`:** `SinCambios` y `Actualizado` son correctos pero dicen cosas distintas.
- **Una cita cancelada vigente sí admite la corrección** y sigue cancelada. Cancelar o dar de baja no cambian el pago guardado.
- **El pago es de la cita:** un paciente de baja o un ámbito retirado no impiden corregirlo en una cita vigente, y esta función no consulta Pacientes. La respuesta no lleva nombre ni datos del paciente.
- **Doble envío.** No hay identificador de solicitud: el control es la versión. Dos envíos iguales con la misma versión dejan **un** cambio y **un** evento; el otro recibe `ConflictoDeVersion`. Por eso la página debe redirigir tras un éxito (PRG) y deshabilitar el botón al enviar; si no, un segundo clic muestra un conflicto aunque el primer cambio haya funcionado.
- **Respuesta perdida.** No se reintenta sola ni se muestra un éxito dudoso. La persona recarga con `ObtenerCitaAsync` y ve el estado real, sin saber quién lo cambió. Volver a elegir el mismo valor después de recargar es una acción nueva y responde «No hubo cambios.» si ya coincide.
- **«Sin cambios» describe la cita tal como estaba al leerla.** Si otra persona confirma un cambio justo después, la respuesta sigue siendo cierta para ese momento; al recargar se verá el valor nuevo.
- **Revocación a mitad de operación.** Sesión y permisos se validan al empezar la solicitud y al empezar el servicio, no dentro de la transacción. Una revocación confirmada antes de que empiece la solicitud la niega; una confirmada después no detiene una operación que ya empezó: se confirma y su evento registra a esa persona, y la siguiente solicitud ya se niega. No se revierte un cambio confirmado.
- **Evento.** Un cambio efectivo registra un evento `Actualizacion` sobre `Cita`, con el campo `EstadoPago` y un hecho fijo, sin valor anterior ni nuevo, y en la misma transacción que el cambio. Consultas, rechazos y «sin cambios» no registran eventos.

## 5. Reglas del horario

La regla vive solo en Domain (una sola copia, sin restricción en PostgreSQL) y el servidor la aplica siempre; la página puede usar `HorarioDeAtencion` solo como ayuda de captura. Es igual los siete días, con fecha y horas de pared de Hermosillo:

1. Inicio y fin del mismo día, **sin segundos ni fracciones**.
2. Inicio desde las **9:00**, fin hasta las **19:00** y el inicio anterior al fin.
3. Si el inicio es anterior a las **15:00**, la cita dura **exactamente una hora**, aunque termine después de las 15:00 (14:30 a 15:30 sí se permite).
4. Desde las 15:00 dura **30 minutos o una hora**.

Rechazos, todos con mensajes fijos de `MensajesDeHorarioDeCita`: `HorasConSegundos`, `FinNoPosteriorAlInicio`, `FueraDelHorarioDeAtencion`, `DuracionAntesDeLas15`, `DuracionDesdeLas15` y `FechaNoRepresentable` (una fecha cuyo instante no cabe en el rango, como el 31-dic-9999 con fin desde las 17:00). Ningún mensaje repite la fecha ni las horas escritas. La regla se aplica **al agendar**; las citas anteriores o de prueba fuera de horario no se cambian ni se borran.

## 6. Tabla de mensajes fijos

`MensajesDeCitas` (Application): `SinPermiso`, `SolicitudInvalida`, `PeriodoInvalido`, `PacienteNoEncontrado`, `PacienteDeBaja`, `AmbitoNoPermitido`, `HorarioOcupado`, `CitaNoEncontrada`, `SolicitudYaUsada`, `CitaAgendada`, `CitaYaAgendada`, `LeyendaRegistradaDespuesDeSuHorario` y `LeyendaPacienteDeBaja`; para el pago (B04), `PagoDeCitaDeBajaNoSeActualiza`, `ConflictoDeVersion` (el mismo texto que Pacientes), `PagoActualizado` y `SinCambios` (el mismo texto que Pacientes).

`MensajesDeHorarioDeCita` (Domain, solo constantes): `HorasConSegundos`, `FinNoPosteriorAlInicio`, `FueraDelHorarioDeAtencion`, `DuracionAntesDeLas15`, `DuracionDesdeLas15`, `FechaNoRepresentable`.

**Reservas simultáneas.** Si varias solicitudes distintas llegan a la vez por el mismo horario, un disparador de `Cita` toma un bloqueo de asesoría de transacción antes de escribir una cita activa: las altas se hacen una tras otra, la primera se guarda y la restricción de cruces rechaza a las demás con el cruce real. Para la página es el mismo `HorarioOcupado` de siempre, sin datos de la otra cita. El bloqueo se toma al insertar, no al abrir la solicitud, y lo libera PostgreSQL al confirmar o revertir; la página no hace nada especial. Ensayado con dos y con diez solicitudes simultáneas.

## 7. Fallos inesperados

Una falla de la base o de la auditoría **no vuelve como `Exito = false`**: sube como excepción y el servidor responde el error 500 fijo de B01 §1.3. La página no la captura, no la presenta como error de validación y no reintenta sola. Cita y evento se guardan juntos o no se guarda ninguno: tras un 500, reenviar el mismo número (sección 4.3) es seguro. Si un guardado falla, la unidad de trabajo descarta lo que dejó pendiente en el contexto, de modo que la siguiente operación de la misma solicitud no repite el cambio fallido.

**Un interbloqueo (`40P01`) u otro fallo de la base es un fallo inesperado y nada más:** no se traduce a ninguna regla de negocio. Si ocurre, también al guardar solo el evento de auditoría, el resultado es el error 500, sin releer la cita, sin anunciar éxito ni «horario ocupado», y sin dejar la cita ni su evento. Las reservas simultáneas ya no dependen de eso (sección 6).

El pago inicial de una cita es **Pendiente**; el formulario de agendar no tiene campo de pago y se cambia con la función de la sección 4.4 (B04). Reprogramar y cancelar son de B05; nuevas bajas y recuperaciones de citas, de B06.

## 8. Qué lo comprueba hoy

| Estado | Parte | Dónde |
| --- | --- | --- |
| ✅ | Regla de horario: las 752 combinaciones válidas contra una lista escrita aparte, los ejemplos del plan y las fechas límite | `ReglasDeHorarioDeCitaTests`, `HoraHermosilloTests`, `ReglasDeCitaTests` (unitarias). |
| ✅ | Permisos por rol y por estado de sesión, entradas, paciente, reenvío con sus tres salidas, fallos inesperados sin relectura y agenda | `ServicioCitasTests`, `ConsultaDePacientesParaCitasTests` y `MatrizDePermisosTests` (unitarias). |
| ✅ | Guardado y evento en una transacción, `PK_Cita`, reenvío con la gemela confirmada antes del guardado o antes del rechazo de ámbito, reservas simultáneas (dos y diez), pausas antes de guardar, fallo de auditoría incluido un `40P01` solo en `EventoAuditoria`, una sola consulta por lectura y agenda con PostgreSQL real | `CitasServidorTests` (integración con PostgreSQL desechable), como verificación de Miguel. |
| ✅ | Disparador del bloqueo de la agenda: cuándo se toma, orden antes del control del paciente, serialización con cruce real, liberación al revertir y migración de subida, bajada y subida | `BloqueoDeAgendaEnCitaActivaTests` (integración con PostgreSQL desechable), como verificación de Miguel. |
| ✅ | Cambiar el pago (B04), regla de Domain: los dos sentidos, versión antigua con y sin el mismo pago, cita de baja, cancelada, valores inválidos, actor vacío y pago conservado al cancelar, dar de baja y recuperar | `ReglasDeCambioDePagoTests` (unitarias). |
| ✅ | Cambiar el pago (B04), servicio: orden de las comprobaciones, sin permiso sin leer ni revelar nada, entradas inválidas, evento único con hecho fijo, «sin cambios» sin escritura ni evento, conflicto, cita de baja, doble envío, fallos que suben como error general y permisos de la matriz | `ServicioCitasCambioDePagoTests` y `MatrizDePermisosTests` (unitarias). |
| ✅ | Cambiar el pago (B04) con PostgreSQL real: guardado, metadatos y evento sin valores, recarga con la versión nueva, «sin cambios», versión antigua, baja y cancelada preparadas, usuario desactivado o con contraseña temporal, auditoría fallida incluido un `40P01` sin relectura, sin esperar al bloqueo de la agenda, dos y diez envíos iguales a la vez y otro cambio de la misma cita (reprogramación o baja) confirmado antes de guardar | `PagoDeCitasServidorTests` y `PagoDeCitaConcurrenteTests` (integración con PostgreSQL desechable), como verificación de Miguel. |
| ⏳ | QA de Lucía: agendar frente a baja en sus tres órdenes, ámbito retirado sin cita confirmada, zonas horarias, páginas y navegador; y, de B04, forzar cada orden de la sección 6 de la propuesta, revocar sesión o permiso a mitad de una operación, campos de más por HTTP y Q16 | Espera su propia entrega y las páginas. |

Los resultados con número de casos, comando y entorno están en [la evidencia de B03](../testing/TherapEase-09-B03-evidencia-servicios.md) y no equivalen a la aprobación de Lucía.
