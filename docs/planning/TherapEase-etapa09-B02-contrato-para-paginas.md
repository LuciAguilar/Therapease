# TherapEase — B02 · Contrato de las funciones del servidor para las páginas de pacientes

**Actualizado el 30-oct-2026.** Autor: Miguel. Para: Dulce. Revisa: Lucía. Versión del servidor: `52332fb`, en el [PR #11](https://github.com/LuciAguilar/Therapease/pull/11). Su [CI 41114443516](https://github.com/LuciAguilar/Therapease/actions/runs/41114443516) tiene cuatro trabajos correctos. Incluye las correcciones del correo anterior y del doble envío; detalle en la [evidencia](../testing/TherapEase-09-B02-evidencia-servicios.md).

Concreta la sección 3.6 de la [propuesta aprobada](TherapEase-etapa09-B02-propuesta.md). El [contrato de B01](TherapEase-etapa09-B01-contrato-para-paginas.md) sigue siendo la referencia para llamar al servidor, usar `RespuestaServicio<T>`, mostrar el error 500, proteger formularios y evitar que se guarden páginas privadas en caché.

Explica cómo llamar desde un PageModel a las funciones de pacientes (registrar, buscar, listar, obtener y actualizar), qué responden y qué debe hacer la página con cada resultado. Las páginas, los formularios, los textos en pantalla y la navegación son de Dulce. **No contiene páginas ni código de interfaz.** Solo datos ficticios; los valores de los campos son provisionales hasta que Dulce confirme los detalles de producto (sección 6).

> **Cambio posterior — listado paginado (13-nov-2026).** Se añadió `ListarAsync` para que la pantalla Pacientes muestre todos los pacientes vigentes al abrir, de seis en seis, con la página actual, el total de páginas y el total de pacientes. Está en la rama `frontend/B02`, sin commit ni PR, y **espera la decisión de Lucía**: la propuesta aprobada dejaba el listado completo para el directorio B07 (propuesta §3.3) y este cambio lo adelanta en la pantalla Pacientes. Detalle en la [sección 3.2.1](#321-listar-con-páginas--cu03-cambio-posterior). `BuscarAsync` y su contrato no cambian.

| Estado | Qué significa en este documento |
| --- | --- |
| ✅ | Ya está en el código de la rama y lo comprueban pruebas unitarias o de integración (sección 7). |
| ⏳ | Pendiente de una confirmación, de las páginas o de la revisión de Lucía. |

## 1. Reglas para todas las páginas de pacientes

- Pedir por constructor `IServicioPacientes` (`TherapEase.Application.Pacientes.Interfaces.Servicios`). **Nunca** usar la base de datos, el repositorio ni las entidades o reglas de Domain.
- **El actor sale de la sesión.** Ninguna función recibe quién lo hace.
- Desde Domain, una página solo puede usar enumeraciones y constantes (AGENTS §1): `AmbitoAtencion`, `Permiso`, `LimitesDePaciente` (para `maxlength` de los campos) y `CamposDePaciente`. Desde Application puede usar `MensajesDePacientes` y `LimitesDeBusquedaDePacientes`. Los modelos de entrada y de resultado están en `TherapEase.Application.Pacientes.Modelos`.
- **Cada función vuelve a comprobar el permiso en el servidor** antes de leer o escribir; ocultar un botón no sustituye eso. Mensaje de rechazo: `MensajesDePacientes.SinPermiso`.
- Políticas de página (en `PoliticasDeAutorizacion`): `RegistrarPacientes`, `ConsultarPacientes` y `ActualizarPacientes`. Las tienen Usuaria y Superusuario; con contraseña temporal pendiente no se entra a ninguna.
- Mostrar `Mensaje` **como texto** y no decidir nada leyendo su contenido: comparar con las constantes de `MensajesDePacientes` o de `MensajesDeDatosDePaciente` (Domain, solo constantes).
- Los modelos de entrada y resultado **no imprimen valores personales en `ToString`**. No registrar su contenido ni el texto buscado.
- Formularios: antifalsificación activa, modelo de entrada propio y salida codificada. La búsqueda se envía por **POST**. Los datos privados llevan `Cache-Control: no-store` automáticamente en las páginas autorizadas.

## 2. Modelos que usa la página

| Modelo | Contenido |
| --- | --- |
| `DatosDePacienteEntrada` | `Nombre`, `FechaNacimiento` (`DateOnly?`), `Telefono`, `Correo`, `Escuela`, `Grupo`, `Ambitos` (`IReadOnlyList<AmbitoAtencion>`) y `Tutor` opcional. Es lo que captura el formulario, sin validar. |
| `DatosDeTutorEntrada` | `Nombre`, `Parentesco`, `Telefono`, `Correo`. |
| `FichaDePaciente` | `IdPaciente`, `Version`, datos de la ficha, `Edad` (`int?`: **nula** si la ficha anterior no tiene nacimiento; nunca cero), `Ambitos` y `Tutor`. |
| `ResumenDePaciente` | `IdPaciente`, `Nombre` y, **solo cuando hace falta distinguir homónimos**, `FechaNacimiento`; `NacimientoDesconocido` es verdadero si esa ficha anterior no tiene fecha. |
| `ResultadoDeBusqueda` | `Pacientes` (hasta 50) y `HayMas`. |
| `PaginaDePacientes` | Cambio posterior (3.2.1). `Pacientes` (`ResumenDePaciente`, hasta 6), `PaginaActual`, `TotalDePaginas`, `TotalDePacientes` y `TamanoDePagina` (6). Calculados: `HayAnterior`, `HaySiguiente`, `Desde` y `Hasta` (posición del primer y último paciente de la página). |
| `ResultadoDeAlta` | `Estado`, `IdPaciente`, `Coincidencias` (hasta 10) y `HayMasCoincidencias`. |
| `ResultadoDeEdicion` | `Estado` y `Ficha` con la versión vigente. |

La edad no se captura ni se guarda: se calcula con la fecha de hoy en America/Hermosillo. Los límites y formatos de cada campo (nombre de 2 a 200, teléfono de 7 a 15 dígitos, correo de hasta 254, etcétera) los valida el servidor; la página puede usar `LimitesDePaciente` solo como ayuda de captura.

## 3. Función por función

### 3.1. Registrar — CU03

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

### 3.2. Buscar — CU03

`BuscarAsync(string? texto, DateOnly? nacimientoExacto = null)` → `RespuestaServicio<ResultadoDeBusqueda>`

Requiere `ConsultarPacientes`. Solo fichas **vigentes**.

- Texto de 2 a 100 caracteres, hasta 6 palabras y sin caracteres de control. Si no cumple: `Exito = false` con `MensajesDePacientes.BusquedaInvalida`.
- No distingue acentos ni mayúsculas de ninguna escritura: `maria` encuentra `María`, `munoz` encuentra `Muñoz`, `øscar` encuentra `Øscar`, `жук` encuentra `Жук`, `istanbul` encuentra `İstanbul`, y la sigma final es igual a la sigma. Exige que aparezcan **todas** las palabras. `%`, `_` y `\` se buscan como texto literal.
- Orden estable por nombre normalizado y, para empates, por identificador. Devuelve como máximo 50; si hay más, `HayMas = true`: pedir que se afine el texto, no afirmar que la lista esté completa.
- `nacimientoExacto` filtra por esa fecha.
- Cada fila trae el nacimiento **solo si otra ficha vigente tiene el mismo nombre**, para distinguirlas; en las demás es nulo. `NacimientoDesconocido` indica una ficha anterior sin fecha.
- La búsqueda **no escribe nada, no registra evento y no guarda el texto buscado**. Un resultado vacío es un estado normal (`Exito = true`, lista vacía), no un error.
- Esta función no lista a todos los pacientes: exige texto de 2 caracteres o más. Para mostrar todos, paginados, existe `ListarAsync` (3.2.1). Tampoco es el directorio completo B07.

### 3.2.1. Listar con páginas — CU03 (cambio posterior)

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

### 3.3. Obtener — CU03

`ObtenerAsync(Guid idPaciente)` → `RespuestaServicio<FichaDePaciente>`

Requiere `ConsultarPacientes`. Hace **una sola consulta** a la base y solo entrega contenido si la ficha está vigente.

| Resultado | `Mensaje` | Qué hace la página |
| --- | --- | --- |
| `Exito = true` | vacío | Mostrar la ficha. **Guardar `Version` en el formulario de edición.** Si `Edad` es nula, mostrar «edad desconocida». |
| Ficha de baja | `MensajesDePacientes.FichaDeBaja`: «Está dada de baja.» | Mostrar solo ese texto. No hay contenido, ni siquiera el nombre. Esto no habilita consultar bajas (B06). |
| No existe o identificador vacío | `PacienteNoEncontrado` | Mostrar que no se encontró. |
| Sin permiso | `SinPermiso` | Rechazar. |

El permiso se comprueba antes de leer, de modo que quien no puede consultar tampoco sabe si el identificador existe.

### 3.4. Actualizar — CU04

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

## 4. Tabla de mensajes fijos

`MensajesDePacientes` (Application): `SinPermiso`, `PacienteNoEncontrado`, `FichaDeBaja`, `FichaDeBajaNoSeActualiza`, `ConflictoDeVersion`, `SolicitudInvalida`, `SolicitudYaUsada`, `BusquedaInvalida`, `CoincidenciasPorConfirmar`, `PacienteRegistrado`, `PacienteYaRegistrado`, `DatosActualizados`, `SinCambios`.

`MensajesDeDatosDePaciente` (Domain, solo constantes): `NombreInvalido`, `NacimientoObligatorio`, `NacimientoInvalido`, `TelefonoInvalido`, `CorreoInvalido`, `EscuelaInvalida`, `GrupoInvalido`, `TutorObligatorioParaMenores`, `TutorIncompleto`, `TutorInvalido`, `AmbitosInvalidos`, `FichaDeBajaNoSeActualiza`, `IdentificadorNecesario`.

## 5. Fallos inesperados

Una falla de la base o de la auditoría **no vuelve como `Exito = false`**: sube como excepción y el servidor responde el error 500 fijo de B01 §1.3. La página no la captura, no la presenta como error de validación y no reintenta sola. Cambio y evento se guardan juntos o no se guarda ninguno: tras un 500 en una alta, reenviar el mismo número (sección 3.1) es seguro. Si un guardado falla, la unidad de trabajo descarta lo que dejó pendiente en el contexto, de modo que la siguiente operación de la misma solicitud no repite el cambio fallido.

## 6. Pendiente de Dulce

Antes de conectar las páginas afectadas, según la sección 3.8 de la propuesta: confirmar con la usuaria la obligatoriedad real y la disponibilidad del nacimiento, si el nombre va en uno o varios campos, el teléfono, la finalidad de escuela y grupo, los datos del tutor y la presentación de homónimos, y validar 08C sección 3. Si la respuesta difiere de los valores provisionales, se registra el impacto y decide Lucía; el servidor no cambia solo.

| Estado | Pendiente |
| --- | --- |
| ⏳ | Confirmaciones de producto de Dulce (arriba). |
| ⏳ | Páginas, formularios, PageModel y sus unitarias, con antifalsificación y salida codificada. |
| ⏳ | Pruebas de navegador de Lucía y revisión humana de código y pruebas. |

## 7. Qué lo comprueba hoy

| Estado | Parte | Dónde |
| --- | --- | --- |
| ✅ | Reglas de la ficha, edad, tutor, teléfono y normalización | Unitarias en `tests/TherapEase.UnitTests/Domain`. |
| ✅ | Permisos de los tres roles de operación, resultados, parecidos, reenvío, conflicto y una sola lectura | `ServicioPacientesTests` y `MatrizDePermisosTests`. |
| ✅ | Guardado y evento en una transacción, carreras del mismo alta, conflicto de versión, ámbitos, una sola consulta y restricciones de la base | Integración con PostgreSQL desechable (`PacientesServidorTests`), como verificación de Miguel. |
| ✅ | Migración desde datos anteriores, subir, bajar y volver a subir | `MigracionDeDatosDePacienteTests`, como verificación de Miguel. |
| ✅ | Listado paginado (3.2.1): permisos, páginas de seis, totales, página inexistente, bajas, filtro y orden estable; el conteo aparte y `LIMIT`/`OFFSET` en la base; accesos retirados | `ServicioPacientesTests` (sección «Listar»), `PaginasDePacientesTests` y, con PostgreSQL desechable, `PacientesListadoTests`. Ejecutadas por Miguel el 13-nov-2026 sin commit; resultados en la [evidencia, sección 8](../testing/TherapEase-09-B02-evidencia-servicios.md). Falta revisión humana, CI y aprobación de Lucía. |
| ⏳ | QA de Lucía de los mismos casos con sus ayudas, y navegador | Espera las páginas y su propia entrega. |

Los resultados con número de casos, comando y entorno están en [la evidencia de B02](../testing/TherapEase-09-B02-evidencia-servicios.md) y no equivalen a la aprobación de Lucía.
