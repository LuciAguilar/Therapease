# TherapEase — B02 · Contrato de las funciones del servidor para las páginas de pacientes

**Actualizado el 15-nov-2026.** Contrato original de Miguel, reorganizado por Codex para Lucía con la estructura del contrato B01. Servidor integrado en #11, main `c90b3c0`, y QA de Lucía integrado en #12, `b53c5b7`. Las páginas y PageModel corresponden a Dulce. Esta actualización documental está preparada localmente.

> Listado inicial, páginas y aviso de nacimiento desconocido integrados por #21 en main `a1a5e46`. El QA propio de Lucía se incorporó y repitió localmente: 77/77; falta su publicación y revisión.

Usar junto al [contrato B01](TherapEase-etapa09-B01-contrato-para-paginas.md), el [plan B02](TherapEase-etapa09-B02.md), la [propuesta aprobada](TherapEase-etapa09-B02-propuesta.md) y la [guía para Dulce](TherapEase-09-guia-Dulce-B00-B01.md). Mantiene los servicios y decisiones aprobados. Solo datos ficticios.

✅ significa entregado en el alcance indicado. ⏳ significa pendiente de producto, páginas, pruebas o revisión.

## 1. Reglas para todas las páginas

### 1.1. Cómo se llama al servidor

- Pedir `IServicioPacientes` por constructor, desde `TherapEase.Application.Pacientes.Interfaces.Servicios`. Los PageModel llaman a Application.
- El actor sale de la sesión. La página no recibe ni envía un actor para autorizar.
- Usar modelos de entrada propios. Las entidades, reglas y repositorios quedan en el servidor.
- De Domain, las páginas pueden usar enumeraciones y constantes, como `AmbitoAtencion`, `Permiso`, `LimitesDePaciente`, `CamposDePaciente` y `MensajesDeDatosDePaciente`. De Application, `MensajesDePacientes` y `LimitesDeBusquedaDePacientes`.
- Cada función comprueba permisos antes de leer o escribir. El menú acompaña esos permisos.

### 1.2. La respuesta de las funciones: `RespuestaServicio<T>`

| Campo | Qué significa en pacientes |
| --- | --- |
| `Exito` | Resultado de la operación. En el alta, revisar además `Datos?.Estado`. |
| `Datos` | Contenido del resultado. El alta con coincidencias puede devolver datos aunque `Exito` sea falso. |
| `Mensaje` | Texto fijo permitido para mostrar, codificado por Razor. Comparar con las constantes si hace falta distinguir un rechazo. |

Alta distingue `Registrado`, `YaRegistrado` y `CoincidenciasPorConfirmar`. Edición distingue `Actualizado` y `SinCambios`. Los estados están descritos en la sección 2.

### 1.3. Cuando algo falla de verdad: error 500 fijo


Una falla de la base o de la auditoría **no vuelve como `Exito = false`**: sube como excepción y el servidor responde el error 500 fijo de B01, sección 1.3. La página no la captura, no la presenta como error de validación y no reintenta sola. Cambio y evento se guardan juntos o no se guarda ninguno: tras un 500 en una alta, reenviar el mismo número (sección 2.1) es seguro. Si un guardado falla, la unidad de trabajo descarta lo que dejó pendiente en el contexto, de modo que la siguiente operación de la misma solicitud no repite el cambio fallido.

### 1.4. Autorización por página (políticas existentes)

| Pantalla | Política y permiso necesarios |
| --- | --- |
| Alta | `RegistrarPacientes` y `ConsultarPacientes`, porque muestra fichas parecidas. |
| Búsqueda y ficha | `ConsultarPacientes`. |
| Edición | `ActualizarPacientes`. Para cargar la ficha, también necesita `ConsultarPacientes`. |

Políticas de `PoliticasDeAutorizacion`, en Web. Las tienen Usuaria y Superusuario dentro del alcance ficticio aprobado. Sesión ausente, revocada, inactiva o temporal pendiente impide usar pacientes. La página de pacientes queda protegida y no se marca anónima.

### 1.5. Formularios

- Mantener antifalsificación activa en todos los POST, incluida la búsqueda. No poner datos de búsqueda en la URL.
- Enlazar solo campos previstos. Identificador de solicitud, paciente y versión son entradas que el servidor comprueba.
- Fecha de nacimiento mediante `DateOnly?`, por ejemplo `yyyy-MM-dd`. Edad calculada por el servidor con la fecha de Hermosillo.
- Mantener lo capturado ante rechazo o conflicto, con salida codificada. No registrar ficha, tutor, contactos, texto buscado ni valores de modelos.
- Las longitudes del navegador ayudan a capturar. El servidor valida de nuevo.
- Un alta nueva genera número nuevo. Los reenvíos y la confirmación de parecidos conservan el número de esa misma alta.

### 1.6. Caché

Las páginas privadas reciben `Cache-Control: no-store`. No registrar fichas, contactos, tutor o texto buscado. Los resultados se muestran solo a la sesión autorizada.

## 2. Función por función

### 2.1. Registrar — CU03

`RegistrarAsync(Guid idSolicitud, DatosDePacienteEntrada datos, bool confirmaQueEsOtraPersona = false)` → `RespuestaServicio<ResultadoDeAlta>`

Requiere `RegistrarPacientes` **y** `ConsultarPacientes`, porque comprueba si ya hay fichas parecidas. Sin el segundo no se recibe ni siquiera la lectura.

**El número de la solicitud.** `idSolicitud` es el identificador que tendrá la ficha. La página lo crea (`Guid.NewGuid()`) al **abrir el formulario** y lo conserva, por ejemplo en un campo oculto, mientras la persona siga con la misma alta, incluido un reenvío tras un error o una pérdida de respuesta. Una alta nueva usa un número nuevo. No es un secreto ni concede acceso; el servidor lo trata como dato de entrada.

| Resultado | `Exito` | `Datos.Estado` | Qué hace la página |
| --- | --- | --- | --- |
| Guardada | Verdadero | `Registrado` | Anunciar el guardado y llevar a la ficha (`Datos.IdPaciente`). |
| Ya estaba guardada | Verdadero | `YaRegistrado` | Era el mismo envío de antes; anunciar que **ya estaba registrada** (`MensajesDePacientes.PacienteYaRegistrado`) y llevar a la ficha. No hay segunda ficha, evento ni versión. |
| Hay fichas parecidas | **Falso** | `CoincidenciasPorConfirmar` | **No se guardó nada** y no se anuncia éxito. Mostrar `Datos.Coincidencias` para abrir una existente o confirmar que es otra persona, reenviando **el mismo `idSolicitud`** con `confirmaQueEsOtraPersona = true`. Si `HayMasCoincidencias`, indicar que hay más. |
| Rechazo | Falso, `Datos` nulo | — | Mostrar el mensaje fijo y conservar lo capturado. |

- **No decidir la navegación solo por `Exito`:** con `CoincidenciasPorConfirmar` es falso y trae contenido. Comparar `Datos?.Estado`.
- Rechazos posibles: `SinPermiso`, `SolicitudInvalida` (datos nulos o número vacío), `SolicitudYaUsada` (el número ya pertenece a otra ficha con otros datos o de otra persona; abrir de nuevo el formulario con un número nuevo; **no se revela la otra ficha**) y los mensajes fijos de validación de `MensajesDeDatosDePaciente`: nombre, nacimiento obligatorio o inválido (futuro o anterior a 1900), teléfono, correo, escuela, grupo, tutor obligatorio para menores de 18, tutor incompleto o inválido, y ámbitos (uno o dos, sin repetir). Ningún mensaje repite el valor escrito.
- Los parecidos son fichas **vigentes** con el mismo nombre al comparar sin acentos ni mayúsculas. Las bajas no cuentan. Si entre la primera lectura y la búsqueda de parecidos se guarda la ficha de este mismo envío (un doble clic), se responde `YaRegistrado` y no se muestra como parecida.
- La lista de parecidos y el reenvío exitoso no cambian por reintentar: reenviar el mismo número con los mismos datos es seguro. Si se pierde la respuesta, **volver a enviar el mismo número** y leer el resultado (`Registrado` o `YaRegistrado`); no abrir un número nuevo, porque eso crearía una segunda ficha.

### 2.2. Buscar — CU03

`BuscarAsync(string? texto, DateOnly? nacimientoExacto = null)` → `RespuestaServicio<ResultadoDeBusqueda>`

Requiere `ConsultarPacientes`. Solo fichas **vigentes**.

- Texto de 2 a 100 caracteres, hasta 6 palabras y sin caracteres de control. Si no cumple: `Exito = false` con `MensajesDePacientes.BusquedaInvalida`.
- No distingue acentos ni mayúsculas de ninguna escritura: `maria` encuentra `María`, `munoz` encuentra `Muñoz`, `øscar` encuentra `Øscar`, `жук` encuentra `Жук`, `istanbul` encuentra `İstanbul`, y la sigma final es igual a la sigma. Exige que aparezcan **todas** las palabras. `%`, `_` y `\` se buscan como texto literal.
- Orden estable por nombre normalizado y, para empates, por identificador. Devuelve como máximo 50; si hay más, `HayMas = true`: pedir que se afine el texto, no afirmar que la lista esté completa.
- `nacimientoExacto` filtra por esa fecha.
- Cada fila trae el nacimiento **solo si otra ficha vigente tiene el mismo nombre**, para distinguirlas; en las demás es nulo. `NacimientoDesconocido` indica una ficha anterior sin fecha.
- La búsqueda **no escribe nada, no registra evento y no guarda el texto buscado**. Un resultado vacío es un estado normal (`Exito = true`, lista vacía), no un error.
- Esto no es el directorio completo: no hay forma de listar todos los pacientes con esta función.

### 2.2.1. Listar con páginas — CU03 (cambio posterior)

`ListarAsync(string? texto, DateOnly? nacimientoExacto = null, int pagina = 1)` → `RespuestaServicio<PaginaDePacientes>`

Requiere `ConsultarPacientes`, que se comprueba antes de leer nada. Solo fichas **vigentes**. Es lo que usa la pantalla Pacientes; `BuscarAsync` se conserva sin cambios.

- **Sin texto** (nulo o en blanco) **ni fecha** devuelve todas las vigentes. **Con texto** rigen las reglas de `BuscarAsync`: de 2 a 100 caracteres, hasta 6 palabras, sin caracteres de control, sin distinguir acentos ni mayúsculas y con todas las palabras; una sola letra da `BusquedaInvalida`. `nacimientoExacto` filtra por esa fecha, con o sin texto.
- **Páginas de seis** (`LimitesDeBusquedaDePacientes.TamanoDePagina`), en el orden estable de la búsqueda: nombre normalizado y, para empates, identificador. `pagina` empieza en 1; menor que 1 da `BusquedaInvalida`.
- **Nunca se cargan todas las fichas.** Se hacen dos consultas: un conteo del total que cumple el filtro y, solo si hay fichas, las filas de la página (`LIMIT`/`OFFSET`). No hay transacción entre las dos: si una ficha se da de baja entre ellas, la página puede traer una fila menos y el siguiente cambio de página corrige los totales.
- **Página inexistente:** pedir una posterior a la última devuelve la última, y `PaginaActual` indica la real; un número enorme no desborda. Sin fichas la página va vacía: `TotalDePaginas` y `TotalDePacientes` en 0, `PaginaActual` en 1, `Desde` y `Hasta` en 0.
- Cada fila es un `ResumenDePaciente`, con la misma regla de nacimiento solo para homónimos. **No trae** teléfono, correo, última cita ni estado: todas son vigentes.
- No escribe, no registra evento y no guarda el texto buscado. Rechazos: `SinPermiso` y `BusquedaInvalida`; no añade mensajes nuevos.
- **Página (Web).** `OnGetAsync` pide la página 1 sin filtro y conserva `ficha` para abrir su panel. `OnPostAsync(numeroPagina)` pide la elegida con el texto y la fecha del formulario, y al filtrar se vuelve a la página 1. La vista muestra «Página X de Y» y «Mostrando a–b de N pacientes»; los botones Anterior y Siguiente aparecen solo con más de una página y se desactivan en los extremos. Con una sola letra, `busqueda-pacientes.js` espera a tener dos antes de filtrar; con el campo vacío ya no se bloquea y se lista todo.

Queda fuera de este cambio: teléfono y fecha de la última cita en cada fila, y el resto del directorio B07 (filtro de bajas con permiso, columnas, pantalla propia). Mostrar la última cita exigiría un coordinador en Application, porque Pacientes no puede depender de Citas.

### 2.3. Obtener — CU03

`ObtenerAsync(Guid idPaciente)` → `RespuestaServicio<FichaDePaciente>`

Requiere `ConsultarPacientes`. Hace **una sola consulta** a la base y solo entrega contenido si la ficha está vigente.

| Resultado | `Mensaje` | Qué hace la página |
| --- | --- | --- |
| `Exito = true` | vacío | Mostrar la ficha. **Guardar `Version` en el formulario de edición.** Si `Edad` es nula, mostrar «edad desconocida». |
| Ficha de baja | `MensajesDePacientes.FichaDeBaja`: «Está dada de baja.» | Mostrar solo ese texto. No hay contenido, ni siquiera el nombre. Esto no habilita consultar bajas (B06). |
| No existe o identificador vacío | `PacienteNoEncontrado` | Mostrar que no se encontró. |
| Sin permiso | `SinPermiso` | Rechazar. |

El permiso se comprueba antes de leer, de modo que quien no puede consultar tampoco sabe si el identificador existe.

### 2.4. Actualizar — CU04

`ActualizarAsync(Guid idPaciente, int versionEsperada, DatosDePacienteEntrada datos)` → `RespuestaServicio<ResultadoDeEdicion>`

Requiere `ActualizarPacientes`. `versionEsperada` es la `Version` de la ficha **al abrirla**. Se pueden editar datos, tutor y ámbitos; la condición, las fechas, los identificadores de auditoría y las citas no.

| Resultado | `Exito` | `Datos.Estado` / `Mensaje` | Qué hace la página |
| --- | --- | --- | --- |
| Guardada | Verdadero | `Actualizado` | Anunciar el guardado y usar `Datos.Ficha` (con la **nueva versión**) para el siguiente envío. |
| Sin cambios | Verdadero | `SinCambios` | Decir que no hubo cambios. No se escribió nada ni se registró evento ni subió la versión. |
| Conflicto de versión | Falso | `ConflictoDeVersion` | Avisar que el registro cambió y **conservar lo capturado** en el formulario; permitir recargar la ficha (`ObtenerAsync`) antes de decidir. El servidor no recarga solo ni expone la ficha actual. |
| Ficha de baja | Falso | `FichaDeBajaNoSeActualiza` | Informar la condición; no se recupera aquí. |
| No existe | Falso | `PacienteNoEncontrado` | Informar. |
| Otros rechazos | Falso | `SinPermiso`, `SolicitudInvalida` y los mensajes de validación | Mostrar el mensaje y conservar lo capturado. |

- **Una versión anterior da conflicto siempre**, aunque los datos enviados sean iguales a los guardados, y nunca se responde éxito por versión antigua.
- «Sin cambios» solo se decide con la versión vigente y comparando los datos ya normalizados (espacios, mayúsculas del correo y formato del teléfono no cuentan como cambio).
- **Pasar de adulto a menor** exige tutor completo **en la misma edición**; **pasar a adulto no borra el tutor**: se conserva si la página lo envía. Cambiar el nacimiento y el tutor se guarda junto, con una sola versión y un solo evento.
- **Fichas anteriores sin nacimiento:** se pueden abrir y mostrar con edad desconocida, y enviarlas sin cambios no obliga a inventar la fecha. Una edición real exige completar el nacimiento y, si resulta menor, el tutor; mientras falten, el servidor rechaza con el mensaje que dice qué falta.
- Los ámbitos enviados reemplazan a los actuales: al menos uno, sin repetir. Quitar uno no modifica citas.
- **Si se pierde la respuesta** tras enviar una edición, **no reenviar a ciegas**. Que la ficha exista no demuestra que el cambio se guardó. Volver a abrirla con `ObtenerAsync` y comparar con lo que se quería guardar antes de decidir si se repite; reenviar la versión anterior da conflicto si el cambio sí se guardó.

### 2.5. Eventos que el servidor guarda

Las páginas no escriben auditoría. El alta y la edición real confirman cambio y evento juntos, con el actor de la sesión. El evento nombra los campos afectados y conserva valores privados fuera de su contenido. Buscar, consultar, reenviar un alta reconocida y editar sin cambios no generan otro evento. Un fallo inesperado conserva el tratamiento de la sección 1.3.

## 3. Confirmación de parecidos y reenvío

1. Abrir el formulario crea un `idSolicitud` fijo.
2. Con `CoincidenciasPorConfirmar`, mostrar los parecidos devueltos y el aviso de más resultados. La persona puede abrir una ficha existente o confirmar que es otra persona.
3. Confirmar conserva los datos y el mismo número de solicitud y envía `confirmaQueEsOtraPersona = true`.
4. `Registrado` anuncia guardado y `YaRegistrado` anuncia el resultado anterior reconocido. En ambos casos se puede abrir la ficha autorizada.
5. Tras respuesta perdida de alta, conservar el número para comprobar el mismo envío. La página no reintenta automáticamente.
6. Tras respuesta perdida de edición, volver a consultar y comparar antes de decidir. Una versión antigua produce conflicto. Abrir la ficha no demuestra por sí solo que la edición se confirmó.

## 4. Mensajes que puede devolver el servidor

`MensajesDePacientes` (Application): `SinPermiso`, `PacienteNoEncontrado`, `FichaDeBaja`, `FichaDeBajaNoSeActualiza`, `ConflictoDeVersion`, `SolicitudInvalida`, `SolicitudYaUsada`, `BusquedaInvalida`, `CoincidenciasPorConfirmar`, `PacienteRegistrado`, `PacienteYaRegistrado`, `DatosActualizados`, `SinCambios`.

`MensajesDeDatosDePaciente` (Domain, solo constantes): `NombreInvalido`, `NacimientoObligatorio`, `NacimientoInvalido`, `TelefonoInvalido`, `CorreoInvalido`, `EscuelaInvalida`, `GrupoInvalido`, `TutorObligatorioParaMenores`, `TutorIncompleto`, `TutorInvalido`, `AmbitosInvalidos`, `FichaDeBajaNoSeActualiza`, `IdentificadorNecesario`.

## 5. Tipos que devuelven las funciones

| Modelo | Contenido |
| --- | --- |
| `DatosDePacienteEntrada` | `Nombre`, `FechaNacimiento` (`DateOnly?`), `Telefono`, `Correo`, `Escuela`, `Grupo`, `Ambitos` (`IReadOnlyList<AmbitoAtencion>`) y `Tutor` opcional. Es lo que captura el formulario, sin validar. |
| `DatosDeTutorEntrada` | `Nombre`, `Parentesco`, `Telefono`, `Correo`. |
| `FichaDePaciente` | `IdPaciente`, `Version`, datos de la ficha, `Edad` (`int?`: **nula** si la ficha anterior no tiene nacimiento; nunca cero), `Ambitos` y `Tutor`. |
| `ResumenDePaciente` | `IdPaciente`, `Nombre` y, **solo cuando hace falta distinguir homónimos**, `FechaNacimiento`; `NacimientoDesconocido` es verdadero si esa ficha anterior no tiene fecha. |
| `ResultadoDeBusqueda` | `Pacientes` (hasta 50) y `HayMas`. |
| `PaginaDePacientes` | Cambio posterior (2.2.1). `Pacientes` (`ResumenDePaciente`, hasta 6), `PaginaActual`, `TotalDePaginas`, `TotalDePacientes` y `TamanoDePagina` (6). Calculados: `HayAnterior`, `HaySiguiente`, `Desde` y `Hasta` (posición del primer y último paciente de la página). |
| `ResultadoDeAlta` | `Estado`, `IdPaciente`, `Coincidencias` (hasta 10) y `HayMasCoincidencias`. |
| `ResultadoDeEdicion` | `Estado` y `Ficha` con la versión vigente. |

La edad no se captura ni se guarda: se calcula con la fecha de hoy en America/Hermosillo. Los límites y formatos de cada campo (nombre de 2 a 200, teléfono de 7 a 15 dígitos, correo de hasta 254, etcétera) los valida el servidor; la página puede usar `LimitesDePaciente` solo como ayuda de captura.

**Constantes para los formularios.** Verificar sus nombres en `LimitesDePaciente` y `LimitesDeBusquedaDePacientes`.

| Dato | Límite del servidor |
| --- | --- |
| Nombre | 2 a 200 caracteres |
| Nacimiento | Desde 1900 hasta hoy en Hermosillo |
| Teléfono | Hasta 20 caracteres de captura, entre 7 y 15 dígitos al normalizar |
| Correo | Hasta 254 caracteres |
| Escuela y grupo | Hasta 150 y 30 caracteres |
| Tutor | Nombre hasta 200, parentesco de 2 a 40, teléfono y correo con los límites anteriores |
| Ámbitos | Escolar, Independiente o ambos, sin repetir |
| Búsqueda | 2 a 100 caracteres, hasta 6 palabras, máximo 50 resultados |
| Parecidos del alta | Hasta 10, con aviso de más coincidencias |

La edad decide si el tutor es obligatorio. Para nacimiento el 29 de febrero, en año no bisiesto se usa el 1 de marzo al calcular edad. Teléfono, correo, escuela y grupo se usan según el contrato provisional y las confirmaciones de producto de la sección 7.

## 6. Trampas conocidas

1. La búsqueda de pacientes se envía por POST. Sus resultados limitados y ordenados tienen aviso `HayMas`.
2. Un nombre igual no impide registrar a otra persona. La confirmación de parecidos exige una decisión de la persona y conserva la solicitud.
3. No convertir edad nula de datos anteriores en cero. Una edición real exige completar los datos que falten.
4. Una ficha de baja solo permite mostrar su condición con permiso. No revela nombre ni abre acciones de recuperación.
5. La versión se conserva desde que se abre la ficha. Ante conflicto no se sobrescribe ni se recarga automáticamente lo capturado.
6. Al pasar a adulto, conservar el tutor enviado. Cambiar ámbitos no cambia citas.
7. El pago, el directorio completo y nuevas bajas/recuperaciones pertenecen a otros bloques.
8. Los errores inesperados conservan el error general y la incertidumbre del guardado. El servidor guarda el cambio y el evento juntos.

## 7. Qué viene y no cambia las firmas

### 7.1. Producto y páginas

Antes de conectar las páginas afectadas, según la sección 3.8 de la propuesta: confirmar con la usuaria la obligatoriedad real y la disponibilidad del nacimiento, si el nombre va en uno o varios campos, el teléfono, la finalidad de escuela y grupo, los datos del tutor y la presentación de homónimos, y validar 08C sección 3. Si la respuesta difiere de los valores provisionales, se registra el impacto y decide Lucía; el servidor no cambia solo.

| Estado | Pendiente |
| --- | --- |
| ⏳ | Confirmaciones de producto de Dulce (arriba). |
| ✅ #21 | Páginas, formularios, PageModel y sus unitarias integrados; QA propio aún por publicar y revisar. |
| ✅ ejecución / ⏳ revisión propia | 23 casos de navegador de Lucía correctos; falta publicar su QA y que Miguel lo revise. |

### 7.2. Evidencia y pendientes

| Estado | Parte | Dónde |
| --- | --- | --- |
| ✅ | Reglas de la ficha, edad, tutor, teléfono y normalización | Unitarias en `tests/TherapEase.UnitTests/Domain`. |
| ✅ | Permisos de los tres roles de operación, resultados, parecidos, reenvío, conflicto y una sola lectura | `ServicioPacientesTests` y `MatrizDePermisosTests`. |
| ✅ | Guardado y evento en una transacción, carreras del mismo alta, conflicto de versión, ámbitos, una sola consulta y restricciones de la base | Integración con PostgreSQL desechable (`PacientesServidorTests`), como verificación de Miguel. |
| ✅ | Migración desde datos anteriores, subir, bajar y volver a subir | `MigracionDeDatosDePacienteTests`, como verificación de Miguel. |
| ✅ | Listado paginado (2.2.1): permisos, páginas de seis, totales, página inexistente, bajas, filtro y orden estable; el conteo aparte y `LIMIT`/`OFFSET` en la base; accesos retirados | `ServicioPacientesTests` (sección «Listar»), `PaginasDePacientesTests` y, con PostgreSQL desechable, `PacientesListadoTests`. Ejecución histórica de Miguel del 13-nov-2026; resultados en la [evidencia, sección 8](../testing/TherapEase-09-B02-evidencia-servicios.md). Integrado por #21 en a1a5e46. El QA propio conserva su revisión y aprobación. |
| ✅ servidor y navegador local / ⏳ publicación propia | QA de Lucía de pacientes | 89 casos propios dentro de 465 integraciones correctas. Miguel revisó #12 y Lucía lo fusionó en b53c5b7. Páginas integradas por #21 y 23 casos propios de navegador ejecutados; su publicación y revisión siguen pendientes. |

Los resultados con número de casos, comando y entorno están en [la evidencia de B02](../testing/TherapEase-09-B02-evidencia-servicios.md) y se conservan con su fecha y alcance. El QA de #12 ya está revisado y fusionado.

### 7.3. Responsables y entrega

Dulce entregó páginas, formularios, PageModel y unitarias en #21, fusionado en a1a5e46. Lucía prepara su PR propio de QA desde revision/etapa09; Miguel revisa esas pruebas. Producto/08C y uso con participante siguen pendientes; Lucía decide el cierre.

En ese mismo PR de Dulce se incluyen las dos mejoras opcionales B01 recibidas el 10-nov. Explicar `AllowAnonymous` y el indicador de temporal distinto de `"false"` en `DenegadoModel`. Usar `{1}` y `{2}` en los mensajes de `StringLength` de cuentas.

## 8. Lista usada para revisar las páginas de #21

**Páginas integradas por #21.** Las marcas de entrega se refieren al código recibido y auditado. El QA propio de Lucía conserva publicación y revisión pendientes; producto/08C y Q13 siguen abiertos.

| Estado | Comprobación de la entrega de Dulce |
| --- | --- |
| ⌛ | Confirmaciones de producto y revisión de 08C, sección 3, identificadas. Lucía decide cualquier diferencia de reglas. |
| ✅ #21 | Alta, parecidos, confirmación, búsqueda, ficha y edición conectadas a `IServicioPacientes`. |
| ✅ #21 | Políticas correctas en rutas y PageModel. Menú y enlaces de pacientes según registrar, consultar y actualizar. Sin acceso directo a base de datos. |
| ✅ #21 | Modelos propios, antifalsificación, salida codificada y no-store. |
| ✅ #21 | Edad desconocida, tutor, homónimos, estados vacíos y límites visibles. |
| ✅ #21 | Solicitud fija conservada en reenvíos y confirmación, y versión original en edición. |
| ✅ #21 | Rechazo, conflicto, guardado, sin cambios y resultado incierto tienen tratamiento propio. |
| ✅ #21 | Ningún campo adicional puede cambiar condición, auditoría o citas. |
| ✅ #21 | Unitarias de PageModel en el mismo PR, con entradas válidas e inválidas, permisos, resultados y excepciones. Reutilizar D-01 a D-05, D-08, D-09 y D-12 a D-14 aplicables del plan de pruebas. |
| ✅ #21 | CI del SHA final correcta, revisión por otra persona y decisión final de Lucía. |
| ⌛ | Explicar a Lucía cómo recorrer las páginas y qué queda pendiente para su QA. |

[Casos existentes de pacientes](../testing/TherapEase-09-plan-de-pruebas.md) y [plan B02](TherapEase-etapa09-B02.md). Sus tablas mantienen los identificadores y el detalle de cobertura.
