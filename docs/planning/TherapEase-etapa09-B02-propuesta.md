# TherapEase — B02 · Lo que ya existe y propuesta de Miguel

**Borrador del 26-oct-2026.** Autor: Miguel. Revisa y decide: Lucía. Rama `b02/fichas-de-pacientes`, a partir de `main` `a7c0c52`, que ya incluye el PR #9 (eventos de acceso) y el PR #10 (sus diez casos de eventos). Parte de la autorización de B02 de AGENTS §3 (25-oct) y de las precisiones de Lucía de ese día, porque el plan B02 corregido todavía no está publicado.

Este documento es **la sola propuesta previa a programar** que pide Lucía para B02: HU04–HU06 y CU03–CU04, es decir, registrar, buscar, consultar y editar una ficha ficticia, con auditoría atómica y aviso de conflicto. **No contiene código.** Nada de lo propuesto se programa hasta que Lucía lo apruebe ([AGENTS](../../AGENTS.md) §3). **Fuera de alcance:** directorio completo (B07), bajas y recuperaciones nuevas (B06), montos, «solo particular» y cancelación automática; las páginas y los PageModel son de Dulce. Solo datos ficticios, sin nube ni gastos. En las tablas, ✅ indica algo ya existente o respondido y ⏳ algo pendiente o propuesto.

## 1. Lista corta: lo que existe y lo que falta

### 1.1. Lo que ya se puede reutilizar

| Estado | Pieza | Dónde está | Qué hace hoy | Qué falta para B02 |
| --- | --- | --- | --- | --- |
| ✅ | Ficha y sus reglas | `Paciente`, `ReglasDePaciente` | Registrar, actualizar, dar de baja y recuperar. `Nombre` (hasta 200), `Contacto` (un texto libre de hasta 200), uno o dos ámbitos distintos, versión como control de conflictos y metadatos de auditoría (`EntidadAuditable`). La versión anterior da conflicto y una ficha de baja no se actualiza. | Fecha de nacimiento, teléfono, correo, escuela, grupo y tutor. Editar ámbitos. Distinguir «sin cambios». No existe nada de esto. |
| ✅ | Persistencia de pacientes | `IRepositorioPacientes`, `RepositorioPacientes`, `PacienteConfiguracion` | Agregar y obtener por identificador, con ámbitos. La versión es el control de conflictos. | Buscar, leer solo la condición (sin el contenido) y listar coincidencias. |
| ✅ | Guardado y auditoría atómicos | `IUnidadDeTrabajo`, `IServicioAuditoria` | Cambio y evento se confirman juntos. `TipoRegistroAuditoria.Paciente` y las acciones `Alta`, `Actualizacion` y `Baja` ya existen; la baja ya las usa. | Usarlas para alta y edición. |
| ✅ | Permisos | `IAutorizacion`, `MatrizDePermisos`, `Permiso` | Tabla explícita por rol y prueba de completitud. Solo existe `DarDeBajaPacientes`. | Permisos de registrar, consultar y editar (§3.2). |
| ✅ | Baja de paciente | `CoordinadorDeBajaDePaciente` | Rechaza la baja con citas activas; la base lo impone también (disparador `TE001`). | Nada: B06 lo cambia, no B02. |
| ✅ | Consulta técnica de eventos Q03 | `/api/auditoria/eventos` | Ya filtra por `tipoRegistro=Paciente`. | Nada. |
| ✅ | Errores y mensajes | `RespuestaServicio<T>`, `ConflictoDeVersionException`, `TraductorDeErroresDeBase` | Patrón de B01: regla conocida = `Exito = false`; fallo inesperado = excepción y error 500 fijo. | Traducir la repetición de un identificador (§3.4). |
| ✅ | Reglas de texto de B01 | `ReglasDeDatosDeCuenta` | Valida nombre completo (sin caracteres de control ni de formato) y correo (dominio sin puntos al inicio, al final ni repetidos). | Moverlas a `Domain/Compartido` para que Pacientes no dependa de Identidad (§3.1). |
| ⏳ | **Servicio de aplicación de Pacientes** | No existe | Solo hay repositorio y el coordinador de baja. | Todo el contrato de §3.6. |
| ✅ | CI | `.github/workflows/comprobaciones.yml` | Cuatro trabajos en `main` desde el PR #7. | Nada nuevo; cubre la migración con `has-pending-model-changes`. |

### 1.2. Pruebas disponibles y lo que cambia

| Estado | Pruebas | Cantidad y límite |
| --- | --- | --- |
| ✅ | Unitarias de pacientes | `ReglasDePacienteTests` 24 y `CoordinadorDeBajaDePacienteTests` 18. |
| ✅ | Unitarias de permisos | `MatrizDePermisosTests` 22 y `AutorizacionTests` 16. La prueba de completitud falla si se añade un permiso sin decidirlo, que es lo que debe pasar en §3.2. |
| ✅ | Integración de pacientes | `BajaPacienteTests` 14, de Lucía. |
| ⏳ | Casos que cambian | Cambiar la firma de `Registrar` y `Actualizar` obliga a tocar llamadas en `ReglasDePacienteTests`, `CoordinadorDeBajaDePacienteTests` y, de Lucía, en `BajaPacienteTests`, `ConsultaAuditoriaRevision`, `ErroresRegistrosRevision`, `HorasDockerTests`, `RecuperacionTests` y `RevisionDatosTests`. Serían ajustes mecánicos sin cambiar lo que comprueba cada caso, listados antes de pedir revisión, como en el PR #9. |

### 1.3. Lo que falta, en orden

| # | Qué falta | Dónde se decide |
| --- | --- | --- |
| 1 | Campos de la ficha, tutor y qué hacer con `Contacto` | §3.1 |
| 2 | Permisos explícitos de alta, búsqueda, ficha y edición | §3.2 |
| 3 | Búsqueda, homónimos y abrir una baja por identificador | §3.3 |
| 4 | Reenvío seguro, solicitudes simultáneas y respuesta perdida | §3.4 |
| 5 | Ámbitos, edición sin cambios, auditoría y migración | §3.5 |
| 6 | Contrato para las páginas, pruebas y orden de trabajo | §3.6, §3.7 y §5 |

## 2. Cómo leer la propuesta

Cada asunto tiene **hoy**, **opciones y recomendación**, **ejemplo**, **si algo falla**, **qué decide Lucía** e **impacto**. §3.8 marca lo que Dulce confirma con la usuaria y §3.9 reúne las decisiones para responderlas juntas. La recomendación busca lo más simple que cumpla las reglas aprobadas y reutiliza lo existente. Mi regla para los detalles de producto sin respuesta: programo con el valor por defecto que propongo, **escrito en un solo lugar de Domain** para poder cambiarlo sin tocar el resto, y las páginas esperan la confirmación de Dulce.

## 3. Propuestas

### 3.1. Campos, tutor y tratamiento del `Contacto` actual

**Hoy.** Una ficha solo tiene `Nombre` y un `Contacto` de texto libre. La entrevista pidió nombre completo, teléfono y correo, fecha de nacimiento, escuela y grupo, y datos del tutor siempre en menores. Dejó pendientes los formatos, la obligatoriedad, si la edad se calcula o se guarda y los datos del tutor.

**Recomendación: campos.**

| Estado | Campo | Regla propuesta |
| --- | --- | --- |
| ✅ / ⏳ | **Nombre completo** | Reutiliza `Nombre`: obligatorio, de 2 a 200 caracteres, recortado, sin caracteres de control ni de formato. Los apellidos van dentro del mismo texto. Dulce confirma si prefiere nombres y apellidos por separado. |
| ✅ / ⏳ | **Fecha de nacimiento** | Fecha sin hora. Obligatoria al registrar, porque distingue homónimos (§3.3) y decide si hay tutor. No puede ser futura ni anterior a 1900. En la base queda anulable, por las fichas ficticias que ya existen. |
| ✅ / ⏳ | **Teléfono** | Opcional. De 7 a 15 dígitos, con espacios, guiones, paréntesis y un `+` inicial permitidos al escribirlo; se guardan solo los dígitos y el `+`. |
| ✅ | **Correo** | Opcional, hasta 254 caracteres, en minúsculas, con la misma regla ya aprobada para las cuentas. No sirve para enviar nada. |
| ✅ / ⏳ | **Escuela y grupo** | Opcionales, de hasta 150 y 30 caracteres. Se conservan aunque se pida «solo particular», que no está autorizado y no se toca. |
| ✅ | **Edad** | **No se guarda.** Se calcula desde la fecha de nacimiento con la fecha de hoy en America/Hermosillo. |
| ✅ / ⏳ | **Tutor** | Se pide **siempre si la persona es menor de 18 años**: nombre completo y parentesco (texto corto) obligatorios, y teléfono o correo, al menos uno. Si es mayor de edad, es opcional. **Un solo tutor**, en columnas de `Paciente`. Dulce confirma cuántos tutores, los datos obligatorios y la mayoría de edad. No se piden identificación ni consentimiento, porque nadie los definió. |

**Opciones para `Contacto`.**

| Opción | Qué hace | Ventaja | Riesgo |
| --- | --- | --- | --- |
| **A. Reemplazarlo (recomendada)** | Una migración añade teléfono y correo y elimina `Contacto`. Copia lo que se pueda: con `@` va al correo y solo dígitos al teléfono; el resto se descarta. `Down` recrea `Contacto` con el correo o el teléfono. | Sin columna muerta; es lo que pide «separar». Solo hay datos ficticios. | `Down` no devuelve el texto exacto de antes. Con datos reales habría que respaldar antes (R04). |
| B. Conservarlo sin usar | Añade las columnas nuevas y deja `Contacto` hasta otra migración. | No se pierde nada. | Columna que nadie escribe; hay que acordarse de quitarla. |
| C. Mantenerlo como «otros contactos» | Tres campos de contacto. | Nada se descarta. | Duplica datos y nadie lo pidió; no lo recomiendo. |

**Reutilización.** Las reglas de nombre completo y de correo pasan de `ReglasDeDatosDeCuenta` a `Domain/Compartido/Reglas`, y la de cuentas las llama desde ahí: así Pacientes no depende de Identidad. Sus 38 pruebas siguen pasando sin cambiar su comportamiento.

**Ejemplo.** Alta de «Ana Ficticia López», nacida el 10-mar-2015. Hoy tiene 11 años, así que se pide un tutor: «Luisa Ficticia», madre, con teléfono. Sin tutor, se rechaza el alta y se dice qué falta. Una persona nacida en 1990 se registra sin tutor.

**Si algo falla.** Un dato inválido devuelve el mensaje del campo, no guarda nada y no repite el valor escrito. Los datos de contacto y del tutor nunca entran a mensajes ni registros técnicos (Q04).

**Qué decide Lucía.** La opción para `Contacto`; que el tutor sea uno, en columnas de `Paciente`, y la regla de menor de 18; y si algún dato es demasiado personal para pedirse antes de R04. Yo reviso con ella la privacidad de cada campo.

**Impacto.** Domain (`Paciente`, `ReglasDePaciente`, reglas nuevas de teléfono, edad y tutor) e Infrastructure (migración, §3.5). Cambian las firmas de `Registrar` y `Actualizar`: ver §1.2.

### 3.2. Permisos explícitos de alta, búsqueda, ficha y edición

**Hoy.** Solo existe `DarDeBajaPacientes`. Registrar, consultar y editar pacientes no tienen permiso: 08B §1.2 los concede a la usuaria y, para demostración ficticia, a los superusuarios.

**Opciones.**

| Opción | Permisos | Ventaja | Costo |
| --- | --- | --- | --- |
| **A. Tres (recomendada)** | `RegistrarPacientes`, `ConsultarPacientes` (buscar y abrir la ficha) y `ActualizarPacientes` | Coincide con las tres operaciones de 08B §1.2 y con el capturista futuro. | Quien busca también puede abrir la ficha. |
| B. Cuatro | A, pero separando `BuscarPacientes` de abrir la ficha | Minimiza datos: buscar no daría acceso a teléfono, correo ni tutor. | Un permiso más sin un rol que hoy lo use. |
| C. Uno | `GestionarPacientes` | Lo más simple. | Pierde la tabla explícita por operación. |

**Matriz propuesta (opción A).** Condición común: cuenta activa, sesión válida y sin contraseña temporal pendiente, como hoy.

| Permiso | Superusuario | Usuaria |
| --- | --- | --- |
| `RegistrarPacientes` | ✅ Permitido (solo datos ficticios) | ✅ Permitido |
| `ConsultarPacientes` | ✅ Permitido (solo datos ficticios) | ✅ Permitido |
| `ActualizarPacientes` | ✅ Permitido (solo datos ficticios) | ✅ Permitido |
| `DarDeBajaPacientes` | ✅ Permitido | ✅ Permitido (provisional, decisión del 11-oct) |

La prueba de completitud obliga a decidir cada permiso nuevo por rol. Cada operación vuelve a comprobar su permiso **en el servidor, antes de leer o cambiar nada**; sin permiso no se revela ni si la ficha existe. Añado tres políticas para las páginas con el mismo patrón que `AdministrarUsuarios`; Dulce las aplica.

**Si algo falla.** Sin sesión, con temporal pendiente o con un rol sin el permiso: `SinPermiso` y ningún dato.

**Qué decide Lucía.** La opción (A, B o C) y la matriz. Dejo escrito que la fila del Superusuario vale **solo mientras los datos sean ficticios**: antes de datos reales, R04 debe decidir su acceso, porque «el Superusuario no recibe acceso clínico real por su nombre de rol».

**Impacto.** Domain (`Permiso`, `MatrizDePermisos`), Web (políticas) y sus unitarias. Es un cambio de permisos: lo revisa Lucía. Sin migración.

### 3.3. Búsqueda, homónimos y respuesta al abrir una baja por identificador

**Hoy.** Solo se obtiene una ficha por identificador, y `ObtenerAsync` la devuelve completa aunque esté de baja.

**Búsqueda (recomendación).**

- Exige `ConsultarPacientes`. Texto de 2 a 100 caracteres, de hasta 6 palabras. **Sin texto o con menos de 2 caracteres se rechaza**: listar todos los pacientes es el directorio de B07, no esta búsqueda.
- Se comparan las palabras **sin distinguir mayúsculas ni acentos**, y la ñ cuenta como n (más aciertos, nunca menos). Cada palabra debe estar contenida en el nombre; `%` y `_` se buscan como texto; consulta parametrizada.
- Solo fichas **vigentes**; orden por nombre; tope de 50 resultados, con aviso de afinar si llega al tope.
- Cada resultado trae identificador, nombre y fecha de nacimiento, que es lo que pidió la entrevista para distinguir homónimos. **No** trae teléfono, correo, escuela, tutor ni ámbitos. Leer no se audita.
- Un filtro opcional por fecha de nacimiento exacta ayuda a elegir entre homónimos.

| Cómo ignorar acentos | Qué hace | Costo |
| --- | --- | --- |
| **A. Columna derivada (recomendada)** | Se guarda `NombreNormalizado` (minúsculas y sin acentos) al escribir; la búsqueda la consulta. | Una columna más, mantenida por Domain. Sin tecnología nueva. |
| B. Comparación simple | `ILIKE` sobre `Nombre`: «María» no encuentra «Maria». | Peor búsqueda. |
| C. Extensión de PostgreSQL | Quitar acentos en la base. | Mecanismo nuevo: exige ADR. No lo recomiendo. |

**Homónimos al registrar.** Antes de crear, el servicio busca fichas **vigentes con el mismo nombre normalizado**. Si hay alguna y no vino la confirmación, **no guarda** y devuelve las coincidencias (identificador, nombre y nacimiento, hasta 10) para abrir una existente. Si la persona confirma que es otra, guarda. **El nombre y el nacimiento nunca son clave única**: un homónimo legítimo, incluso con el mismo nacimiento, puede registrarse con confirmación. Las bajas no aparecen entre las coincidencias.

**Abrir una baja por identificador.**

| Opción | Respuesta | Comentario |
| --- | --- | --- |
| A. Negar como si no existiera | «No se encontró el paciente.» | No revela la baja. Pierde la regla de CU04 A4. |
| **B. Mostrar solo su condición (recomendada)** | «La ficha está dada de baja.», sin datos. | Cumple M-09 y CU04 A4. Solo lo ve quien ya tiene `ConsultarPacientes`. |

Se implementa con una lectura que trae **únicamente la condición**, antes de cargar la ficha; si es baja, el contenido nunca se consulta. La unitaria usa un repositorio falso cuya lectura de contenido falla, para demostrarlo. La edición por el flujo ordinario sigue la misma vía (M-09). Quien no tiene permiso recibe `SinPermiso` **antes** de cualquier lectura.

**Ejemplo.** Se escribe «maria lopez»: aparecen «María López Ficticia» (10-mar-2015) y «María López Ficticia» (02-feb-1990), cada una con su nacimiento. Registrar a una tercera «María López Ficticia» muestra esas dos y pide confirmar.

**Si algo falla.** Texto inválido: rechazo sin buscar. Sin resultados: éxito con lista vacía. Identificador inexistente: «No se encontró el paciente.».

**Qué decide Lucía.** La opción de acentos, la A o B para las bajas, y si la lista puede mostrar la fecha de nacimiento (es un dato personal). **Dulce confirma** que sea solo por nombre y el mínimo de 2 caracteres.

**Impacto.** Application e Infrastructure (nueva lectura de condición y búsqueda) y la columna de §3.5. Sin tecnología nueva ni ADR.

### 3.4. Reenvío seguro, solicitudes simultáneas y consulta del resultado ante respuesta perdida

**Hoy.** `IdPaciente` ya lo asigna quien llama (`ValueGeneratedNever`), y la versión protege las ediciones. Falta cerrar el doble clic y la respuesta perdida (Q07, Q08; S3 y S5).

**Recomendación: registrar.**

- La página genera un identificador al **abrir** el formulario y lo manda oculto; ese es el `IdPaciente` de la ficha. El servicio no confía en él: lo valida y comprueba que la persona que repite sea la misma.
- Si ya existe una ficha con ese identificador, **misma persona que la creó y mismos datos**: éxito «ya estaba registrado», **sin segunda ficha, segundo evento ni nueva versión**. Si el identificador existe con otros datos u otra persona: rechazo, sin revelar nada.
- **Dos envíos simultáneos con el mismo identificador:** PostgreSQL hace esperar al segundo; este recibe la repetición del identificador, el servicio la traduce, relee y responde como el caso anterior. Hay que traducir ese error de base (clave primaria repetida) a una regla conocida.
- **Dos fichas iguales de personas distintas a la vez** no se evitan: el nombre no es único por decisión de producto. Queda anotado como límite.

**Recomendación: editar.**

- La versión protege: la segunda edición simultánea recibe conflicto y no sobrescribe (M-08, L-CU04-01).
- **Doble clic en «guardar cambios».** El segundo envío llega con la versión vieja pero con los mismos datos que ya quedaron guardados. En ese caso no hay conflicto real: se responde éxito «los datos ya estaban guardados», sin evento ni cambio. Con datos distintos sí es conflicto.

**Respuesta perdida.** La página muestra «No pudimos confirmar si se guardó» y **nunca reintenta sola**. Para consultar: abre la ficha con ese identificador (`ObtenerAsync`). Si existe, se guardó; si no existe, no se guardó y se puede reenviar con **el mismo** identificador sin riesgo.

**Si algo falla.** Un fallo de base o de auditoría sube como excepción y la página responde el 500 fijo: **nunca éxito sin commit** (M-22). La excepción no se captura en la página, igual que en B01.

**Qué decide Lucía.** Que el identificador lo cree la página al abrir el formulario y sirva como identificador de la solicitud, y la regla del segundo envío con datos idénticos. Lucía prueba con PostgreSQL real los envíos simultáneos y el corte de la respuesta.

**Impacto.** Application, Infrastructure (traducción del error) y una excepción nueva de Domain. Sin migración.

### 3.5. Ámbitos actuales, edición sin cambios, auditoría y migración

**Ámbitos (hoy y recomendación).** Se conservan los dos de ahora, **Escolar** e **Independiente**; una ficha tiene uno o ambos. **«Solo particular» no se toca**: no está autorizado. Registrar sin ámbito, con uno repetido o con uno inválido se rechaza (M-06). La entrevista permite **retirar** un ámbito y que las citas conserven el original, así que la edición permite añadir o quitar ámbitos, siempre con al menos uno, y **no modifica ninguna cita**. Qué pasa al recuperar o reprogramar una cita con el ámbito retirado sigue pendiente y es de B05/B06. La base ya permite añadir y quitar ámbitos.

**Edición sin cambios.** Se comparan los datos enviados, ya normalizados, con los guardados. Si **nada** cambió: no se escribe, **no hay evento**, la versión no sube y la respuesta es «No hubo cambios.». Si algo cambió, se guarda y el evento lista **solo los nombres de los campos que cambiaron**, como en B01.

**Auditoría.** Alta: acción `Alta` sobre el paciente, con los nombres de los campos capturados (los opcionales solo si se escribieron) y el hecho fijo «Se registró un paciente.». Edición: `Actualizacion`, solo con los campos cambiados y «Se actualizaron los datos de un paciente.». **Nunca valores**: ni nombre, teléfono, correo, escuela, grupo ni tutor. Actor, momento, tipo e identificador salen de la sesión y del reloj (M-24). Los metadatos de `EntidadAuditable` se llenan en el alta y en cada actualización real (M-12). No hay acciones nuevas en el enum. **Leer, buscar y abrir fichas no se audita**; lo anoto como decisión para Lucía.

**Migración.** Una sola, con nombre descriptivo, sin código de etapa.

| Cambio | Detalle |
| --- | --- |
| Columnas nuevas | `FechaNacimiento` (`date`, anulable), `Telefono` (hasta 20), `Correo` (hasta 254), `Escuela` (hasta 150), `Grupo` (hasta 30), `NombreNormalizado` (hasta 200, obligatoria), `TutorNombre` (hasta 200), `TutorParentesco` (hasta 40), `TutorTelefono` (hasta 20) y `TutorCorreo` (hasta 254). |
| Se elimina | `Contacto`, con la copia descrita en §3.1 (opción A). |
| Para las fichas existentes | `NombreNormalizado` se calcula con funciones nativas de PostgreSQL para los acentos del español; las demás columnas quedan vacías. |
| Índice y restricción | Índice sobre `NombreNormalizado` y una restricción de que no esté vacío. El resto de las reglas las valida Domain. |
| Permisos de la base | Sin tablas nuevas, así que no hacen falta `GRANT` nuevos: `SELECT, INSERT, UPDATE` sobre `Paciente` ya cubre las columnas. El usuario de la aplicación sigue sin borrar pacientes ni tocar la auditoría. |
| Reversión | `Down` quita las columnas nuevas y recrea `Contacto` (§3.1). |

Lucía revisa el SQL de subida y de bajada (`dotnet ef migrations script`) antes de fusionar, y el CI comprueba que el modelo y las migraciones coincidan.

**Ejemplo.** Se abre una ficha y se pulsa «guardar» sin cambiar nada: «No hubo cambios.» y la tabla de eventos queda igual. Se cambia solo el teléfono: el evento dice `Telefono`, sin el número.

**Si algo falla.** Si no se puede guardar el cambio o su evento, ninguno queda guardado y llega el 500 fijo.

**Qué decide Lucía.** Edición de ámbitos tal como se propone; que leer no se audite hasta R04; y el SQL de la migración.

**Impacto.** Una migración, Domain, Application e Infrastructure. Con datos reales exigiría respaldo previo (R04).

### 3.6. Contrato propuesto para las páginas

**Es un borrador:** tras la aprobación lo detallo en un documento aparte, como el de B01. Todas las operaciones están en `IServicioPacientes`, devuelven `RespuestaServicio<T>` (solo `Exito` decide qué hace la página) y toman a la persona de la sesión, nunca de un parámetro.

| Operación | Permiso | Recibe | Devuelve | Rechazos y errores |
| --- | --- | --- | --- | --- |
| `RegistrarAsync` | `RegistrarPacientes` | Identificador de la solicitud, datos de la ficha y confirmación de homónimo | Identificador y estado: registrada, ya registrada o **coincidencias por confirmar** (con la lista) | Dato inválido, tutor faltante en menores, identificador usado por otra persona |
| `BuscarAsync` | `ConsultarPacientes` | Texto y fecha de nacimiento opcional | Lista de identificador, nombre y nacimiento | Texto inválido |
| `ObtenerAsync` | `ConsultarPacientes` | Identificador | Ficha completa con versión y edad calculada | No encontrado; **ficha de baja** (solo la condición) |
| `ActualizarAsync` | `ActualizarPacientes` | Identificador, versión esperada y datos | Ficha actualizada, «sin cambios» o «ya estaba guardado» | Dato inválido, ficha de baja, **conflicto de versión** (la página recarga con `ObtenerAsync`) |

Todos los rechazos usan constantes de mensaje en `MensajesDePacientes`, sin datos privados. Los modelos devueltos ocultan los valores en su `ToString`, para que ningún registro los imprima por accidente. La página usa un **modelo de entrada propio**, mantiene la antifalsificación, pide la búsqueda por **POST** (el nombre de una persona no debe quedar en la URL ni en registros) y recibe `no-store` en las páginas privadas. La denegación real se comprueba siempre en el servidor.

### 3.7. Pruebas previstas

**Mías, unitarias, en el mismo PR.** Las cifras son estimaciones, no compromiso.

| Estado | ID | Qué comprueban |
| --- | --- | --- |
| ⏳ | **M-06** | Registrar sin nombre, sin ámbito o con ámbito repetido o inválido: rechaza. Con uno o dos: válido. Además, cada regla de campo nueva: nombre, nacimiento, teléfono, correo, escuela, grupo y tutor, con sus límites. |
| ⏳ | **M-07** | Buscar por nombre sin acentos ni mayúsculas, solo vigentes, tope de 50, `%` y `_` como texto. Un homónimo se registra con confirmación y sin ella devuelve las coincidencias; el nombre y el nacimiento no son clave única. |
| ⏳ | **M-08** | Versión anterior: conflicto y no sobrescribe. Doble clic con datos idénticos: «ya estaba guardado», sin evento. Registro repetido con el mismo identificador: sin segunda ficha ni evento. |
| ⏳ | **M-09** | Actualizar o abrir una ficha de baja: rechaza e informa su condición **sin leer el contenido** (repositorio falso que falla si se lee). |
| ⏳ | **M-12** | El alta y cada actualización real llenan fecha y usuario de `EntidadAuditable`; una edición sin cambios no los toca ni sube la versión. |
| ⏳ | **M-22** | Si falla el guardado o el evento: excepción, ningún éxito y nada confirmado. Se comprueba para registrar y actualizar. |
| ⏳ | **M-24** | El evento lleva actor, momento, tipo e identificador, acción y solo nombres de campos; con valores rastreables en teléfono, correo, escuela, grupo y tutor, ninguno aparece en el evento, el mensaje ni el `ToString`. |
| ⏳ | Permisos | Matriz completa por rol; cada operación con sesión ausente, temporal pendiente, rol sin permiso y cuenta desactivada, sin leer nada antes de autorizar. |
| ⏳ | Edad y tutor | Límites del cumpleaños 18, un día antes, 29 de febrero y la fecha de hoy en America/Hermosillo; tutor obligatorio solo en menores. |
| ⏳ | Normalización | Acentos, ñ, mayúsculas, espacios múltiples y caracteres invisibles. |

**Sugeridas para Lucía, con PostgreSQL real; ella decide y asigna.** L-CU03-01 a 04 (uno y dos ámbitos, coincidencias, bajas fuera de la búsqueda, doble envío), L-CU04-01 y 02 (dos ediciones y respuesta cortada), L-CU04-07 (ámbito retirado conserva las citas), L-Q03-01 y 02 (un evento por cambio y atomicidad), migración de subida y bajada con fichas ficticias previas, búsqueda con caracteres especiales y con inyección SQL, y envíos simultáneos con el mismo identificador. D-01 a D-05 y D-09 son de Dulce.

**CI.** Sin cambios; la migración la cubre el control de modelo al día.

### 3.8. Qué confirma Dulce con la usuaria antes de sus páginas

Valida también 08C §3. Estas respuestas **no bloquean el servidor**, que programo con el valor por defecto; sí bloquean sus pantallas.

| Estado | Detalle por confirmar | Valor por defecto de esta propuesta |
| --- | --- | --- |
| ⏳ | ¿Se conoce siempre la fecha de nacimiento? | Obligatoria al registrar. |
| ⏳ | ¿Nombre completo en un solo campo o nombres y apellidos separados? | Un solo campo. |
| ⏳ | Formato del teléfono. | De 7 a 15 dígitos, guardados sin separadores. |
| ⏳ | ¿Escuela y grupo se piden a todos o solo al ámbito escolar? ¿Qué sentido tienen en atención particular? | Opcionales para todos, sin obligar. |
| ⏳ | Tutor: ¿uno o dos?, ¿qué datos son obligatorios?, ¿18 años? | Uno; nombre, parentesco y teléfono o correo; menor de 18. |
| ⏳ | Búsqueda: ¿solo por nombre?, ¿mínimo de 2 caracteres?, ¿la lista muestra nombre y nacimiento? | Sí, sí y sí; sin lista completa al abrir (eso es B07). |
| ⏳ | Homónimos: aviso con coincidencias y «registrar de todos modos». | Así. |
| ⏳ | ¿Aparece un aviso si la persona buscada ya está de baja? | No; las bajas no se muestran en B02. |
| ✅ | Retirar un ámbito conserva las citas; lista solo con el nombre en el directorio; nacimiento y apellidos para homónimos. | Ya respondido (entrevista); no se vuelve a preguntar. |
| ⏳ | Versión exacta de las pantallas y 08C §3 antes del frontend. | Pendiente de Dulce, sin bloquear el servidor. |

### 3.9. Decisiones que pido

| # | Decisión | Mi recomendación | Decisión de Lucía |
| --- | --- | --- | --- |
| 1 | Campos y reglas de la ficha (§3.1) | Los de la tabla de §3.1; la edad se calcula y no se guarda. | ⏳ Pendiente. |
| 2 | Qué hacer con `Contacto` (§3.1) | Opción A: reemplazarlo en la migración. | ⏳ Pendiente. |
| 3 | Tutor (§3.1) | Uno, en columnas de `Paciente`, obligatorio solo en menores de 18. | ⏳ Pendiente. |
| 4 | Permisos (§3.2) | Opción A: tres nuevos, concedidos a Usuaria y Superusuario solo con datos ficticios. | ⏳ Pendiente. |
| 5 | Búsqueda y acentos (§3.3) | Opción A: columna derivada, sin ADR. Lista con nombre y nacimiento. | ⏳ Pendiente. |
| 6 | Abrir una baja por identificador (§3.3) | Opción B: mostrar solo la condición, sin leer el contenido. | ⏳ Pendiente. |
| 7 | Reenvío seguro (§3.4) | Identificador creado al abrir el formulario, ficha idempotente y «ya estaba guardado» ante un segundo envío con los mismos datos. | ⏳ Pendiente. |
| 8 | Ámbitos y edición (§3.5) | Se editan, con al menos uno, sin tocar citas. Edición sin cambios sin evento. | ⏳ Pendiente. |
| 9 | Auditar lecturas (§3.5) | No en B02; decidir antes de datos reales con R04. | ⏳ Pendiente. |
| 10 | Mover las reglas de nombre y correo a `Domain/Compartido` (§3.1) | Sí, sin cambiar su comportamiento. | ⏳ Pendiente. |

## 4. Fuera de esta propuesta

- **Bajas y recuperaciones nuevas, y cancelación automática de citas (B06):** la baja sigue como en B00, con su rechazo ante citas activas.
- **Directorio completo (B07), «solo particular» y montos:** sin autorización.
- **Páginas, PageModel y navegador:** de Dulce y del QA posterior de Lucía.
- **Datos reales, TOTP y R04:** siguen abiertos; solo datos ficticios.
- **Importar datos de papel, expediente, notas y reportes:** futuros.

## 5. Orden de trabajo después de aprobar

1. **Permisos** (§3.2): matriz, política y sus unitarias; es un cambio de permisos.
2. **Reglas de campos** (§3.1): reglas compartidas, edad, tutor, normalización, edición sin cambios y ámbitos, en Domain y con sus unitarias.
3. **Migración y persistencia** (§3.5, §3.3): columnas, búsqueda, lectura de la condición y traducción del error de repetición.
4. **Servicio de Pacientes** (§3.3, §3.4): registrar, buscar, abrir y actualizar, con auditoría y permisos.
5. **Contrato para Dulce** y actualización de documentos: plan B02, plan de pruebas y README, con fecha, versión y resultados.

Un solo PR desde `b02/fichas-de-pacientes`, con sus unitarias y el CI en verde, sin commits ni push míos: los hace Miguel. Si resulta demasiado grande para revisarlo, lo parto en dos: pasos 1 a 3, y pasos 4 y 5. Lucía decide.
