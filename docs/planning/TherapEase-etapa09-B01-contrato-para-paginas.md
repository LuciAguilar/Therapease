# TherapEase — B01 · Contrato de las funciones del servidor para las páginas

**Borrador del 17-oct-2026.** Autor: Miguel. Para: Dulce. Revisa: Lucía. Rama `b01/acceso-y-usuarios`, código de referencia en el commit `d18cf29` ([PR #7](https://github.com/LuciAguilar/Therapease/pull/7); llegará a `main` con ese PR).

Este documento cumple la entrega «explicar qué recibe y devuelve cada función, sus errores y la entrega de temporales» del [plan B01](TherapEase-etapa09-B01.md) (§2). Dice **cómo llamar a cada función desde un PageModel**, qué responde y qué debe hacer la página con cada resultado. Las páginas, los formularios, los mensajes en pantalla y la navegación son de Dulce; aquí solo se fija lo que el servidor garantiza y lo que la página no debe romper. **No contiene páginas ni código de interfaz.** ✅ indica algo que ya está en el código y con pruebas; ⏳ indica algo pendiente de decisión de Lucía.

## 1. Reglas para todas las páginas

### 1.1. Cómo se llama al servidor

- El PageModel pide por constructor la interfaz de `Application` (por ejemplo `IServicioAcceso`) y la llama. **Nunca** usa la base de datos, `UserManager`, `SignInManager` ni `IGestorIdentidad` (AGENTS §1).
- **El actor sale de la sesión.** Ninguna función recibe «quién lo hace»; un campo oculto con un identificador de actor no sirve ni debe existir.
- Desde `Domain` el PageModel solo puede usar enumeraciones y constantes: `Permiso`, `NombresDeRol`, `LimitesDeCuenta`. Las constantes de `Application` (`MensajesDeIdentidad`, `LimitesDeIdentidad`) también se pueden usar.
- Ocultar un botón no sustituye la autorización: **cada función vuelve a comprobar el permiso en el servidor**.

### 1.2. La respuesta de las funciones: `RespuestaServicio<T>`

| Campo | Qué es |
| --- | --- |
| `Exito` | `true` si la operación se confirmó. **Es lo único que decide qué hace la página.** |
| `Datos` | El resultado (`T`) si hubo éxito; `null` si no. |
| `Mensaje` | Texto en español, seguro para mostrar y sin datos privados. Vacío en las consultas y en `IniciarSesionAsync`. |

- Mostrar `Mensaje` **como texto** (Razor lo codifica; no usar `Html.Raw` ni `innerHTML`).
- No decidir nada leyendo el texto del mensaje. Si hace falta distinguir un caso, comparar con la constante de `MensajesDeIdentidad` (§4).
- Un resultado con `Exito = false` **no se anuncia nunca como éxito** y no completó la acción. **Una excepción importante:** un intento fallido de contraseña, al entrar o al cambiarla, sí deja guardado el aumento del contador de bloqueo; cinco seguidos bloquean la cuenta 15 minutos aunque cada respuesta haya sido un fallo.

### 1.3. Cuando algo falla de verdad: error 500 fijo

Un fallo inesperado (base de datos, Identity, registro de auditoría) **no vuelve como `Exito = false`**: sube como excepción y, fuera del entorno de desarrollo, el servidor responde la página de error fija: «Ocurrió un error. Si estabas guardando algo, revisa si se guardó antes de repetirlo.» (En desarrollo se ve la página de excepciones del propio .NET; no es lo que verá la usuaria.)

- No capturar excepciones para mostrar su texto ni para reintentar.
- **Nunca reintentar automáticamente** una operación que cambia datos (crear, cambiar rol, desactivar, restablecer, cambiar contraseña). Si se pierde la respuesta, la persona comprueba qué quedó guardado antes de repetir (§3 y plan B01 §8).

### 1.4. Autorización por página (políticas existentes)

Todo lo que no declare otra política exige **sesión válida y sin contraseña temporal pendiente** (política por defecto `AccesoOperativo`). Solo lo anónimo debe marcarse `[AllowAnonymous]`.

| Página | Atributo | Quién entra |
| --- | --- | --- |
| `/Acceso` (entrar) | `[AllowAnonymous]` | Cualquiera. |
| `/Acceso/Denegado` | `[AllowAnonymous]` | Ver «trampa» de §6. |
| Cambiar contraseña propia (CU11) | `[Authorize(Policy = PoliticasDeAutorizacion.CambiarContrasenaPropia)]` | Cualquier sesión válida, **incluso con temporal pendiente**. Es la única página que lo permite. |
| Usuarios y roles (CU02, todas) | `[Authorize(Policy = PoliticasDeAutorizacion.AdministrarUsuarios)]` | Superusuario activo, sesión válida y sin temporal pendiente. |
| Salir | `[Authorize(Policy = PoliticasDeAutorizacion.CambiarContrasenaPropia)]` | Cualquier sesión válida, **también con temporal pendiente**: con la política por defecto esa persona no podría ni salir. |

Fuera de las páginas, una solicitud sin sesión recibe 401 si la ruta empieza con `/api`; en una página, el servidor redirige a `/Acceso`.

### 1.5. Formularios

- **Protección antifalsificación:** viene activa por defecto en Razor Pages. No poner `[IgnoreAntiforgeryToken]` ni dejar `asp-antiforgery="false"` en ningún formulario (el diseño actual lo trae; B01 §4.2).
- **Modelo de entrada propio** para cada formulario, solo con los campos previstos. No enlazar `DatosUsuario`, `FichaDeUsuario` ni entidades: así un campo de más enviado a mano no cambia un rol ni un usuario.
- **Nunca reponer una contraseña** en el formulario después de un error (ni la actual, ni la nueva, ni la confirmación).
- Salir se hace con **POST**, no con un enlace.

### 1.6. Caché

Las páginas privadas reciben `Cache-Control: no-store` automáticamente. **La página de acceso es anónima, así que no lo recibe sola:** el PageModel de `/Acceso` debe llevar `[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]`, como ya hace la página de error (B01 §3.1, «Cabeceras de seguridad»).

## 2. Función por función

### 2.1. Entrar — CU01 · `/Acceso`

`IServicioAcceso.IniciarSesionAsync(string nombreUsuario, string contrasena)` → `RespuestaServicio<DatosUsuario>`

| | |
| --- | --- |
| **Campos del formulario** | `Entrada.NombreUsuario` y `Entrada.Contrasena` (los del diseño; no cambiarlos). |
| **Qué hace el servidor** | Quita los espacios exteriores del nombre, verifica la cuenta, cuenta los fallos y, **solo si todo es correcto y ya se guardó**, abre la sesión (la cookie la emite el servidor dentro de esta llamada). |
| **Si hay éxito** | `Datos.DebeCambiarContrasena = true` → llevar a la página de CU11 (con temporal pendiente no se puede entrar a nada más). Si es `false` → destino normal. |
| **Si falla** | `Mensaje = MensajesDeIdentidad.CredencialesInvalidas` («Usuario o contraseña incorrectos.»). Es **el mismo texto** para nombre desconocido, contraseña incorrecta, cuenta desactivada, cuenta bloqueada y campos vacíos. |

La página:

- **No distingue** esos casos ni muestra que la cuenta está bloqueada, cuántos intentos quedan ni si el nombre existe. Cinco fallos seguidos bloquean 15 minutos; mientras dure, ni la contraseña correcta entra, y el mensaje es el mismo.
- Puede volver a mostrar el nombre escrito, nunca la contraseña.
- **`returnUrl`:** cuando el servidor redirige a `/Acceso` agrega `ReturnUrl`. Solo se acepta si es una dirección local (`Url.IsLocalUrl`); si no, se usa el destino por defecto. Nunca redirigir a un sitio externo.
- Si la persona ya tiene sesión y abre `/Acceso`, es decisión de Dulce a dónde llevarla.

### 2.2. Salir — CU01

`IServicioAcceso.CerrarSesionAsync()` → `Task` (sin resultado)

- Se llama desde un **POST** (la página de salir lleva la política de §1.4, para que también pueda salir quien tiene la temporal pendiente). Quita la cookie y renueva el sello de seguridad: **todas las sesiones de esa cuenta** (también las de otros navegadores) dejan de valer en su siguiente solicitud.
- Es idempotente: sin sesión, o repetida, no falla.
- Después, redirigir a `/Acceso`. Con «atrás» no debe verse nada privado (las páginas privadas llevan `no-store`).

### 2.3. Cambiar la contraseña propia — CU11

`IServicioContrasena.CambiarContrasenaPropiaAsync(string? contrasenaActual, string contrasenaNueva)` → `RespuestaServicio<DatosUsuario>`

| Situación de la cuenta | Qué pide el formulario | Qué manda la página |
| --- | --- | --- |
| **Ordinaria** (`DebeCambiarContrasena = false`) | Contraseña actual, nueva y confirmación de la nueva. | `contrasenaActual` y `contrasenaNueva`. |
| **Con temporal pendiente** | **Solo** la nueva y su confirmación (no se vuelve a pedir la temporal). | `contrasenaActual = null` y `contrasenaNueva`. |

- Cómo saber en qué situación está: ✅ la reclamación `Reclamaciones.DebeCambiarContrasena` de la sesión (`"true"` = pendiente; si falta, también se trata como pendiente), o ⏳ la consulta de datos propios (§2.4). El servidor **decide con el estado real de la cuenta**, no con lo que mande la página: si la cuenta no tiene temporal pendiente y la actual falta o es incorrecta, no cambia nada.
- **La confirmación de la nueva la valida la página**; el servidor no la recibe.
- Regla de la nueva contraseña: **mínimo 12 caracteres, sin exigir mayúsculas, números ni símbolos**. El servidor no fija un máximo.
- Solo cambia la contraseña **de la sesión actual**. No hay forma de pasar un identificador.

| Resultado | `Mensaje` | Qué hace la página |
| --- | --- | --- |
| ✅ Éxito | «Contraseña actualizada.» (`Datos.DebeCambiarContrasena = false`) | Llevar al destino normal. El servidor **ya reemitió la cookie de esta sesión**; las demás sesiones de la cuenta dejaron de valer. |
| Actual vacía o incorrecta, cuenta inactiva, sin sesión | `CambioDeContrasenaNoRealizado` | Mostrar el mensaje sin decir qué campo falló. **Cada actual incorrecta cuenta para el bloqueo** (el mismo contador del acceso: cinco fallos, 15 minutos). |
| Nueva de menos de 12 caracteres | `ContrasenaNuevaNoCumpleReglas` | Mostrarlo junto al campo de la nueva. Nada se cambió; la contraseña anterior sigue valiendo. |

### 2.4. Datos propios — ⏳

`IServicioCuentaPropia.ObtenerMisDatosAsync()` → `RespuestaServicio<FichaDeUsuario>`

- **No recibe identificador:** toma la cuenta de la sesión. Sirve para mostrar el nombre completo y el correo de quien está dentro, y para saber si tiene una temporal pendiente.
- Funciona **también con la contraseña temporal pendiente** (la página de CU11 puede mostrar el nombre).
- Cada persona ve **solo** sus datos y **no los cambia** desde aquí; los cambia el Superusuario (§2.9).
- Falla con `SinPermiso` si no hay sesión o la cuenta está desactivada: llevar a `/Acceso`.
- ⏳ Lucía debe confirmar que se permita con temporal pendiente (la regla dice «con temporal solo se accede a CU11»).

### 2.5. Administrar usuarios — CU02

**Todas** las funciones de esta sección:

- Solo las puede usar un **Superusuario activo, con sesión válida y sin temporal pendiente**. Si no, responden `SinPermiso` («No tienes permiso para realizar esta operación.») sin hacer nada.
- Pertenecen a `IServicioUsuarios`.
- Cada cambio queda registrado junto con su evento de auditoría (ambos o ninguno). Las consultas no se registran.

#### 2.5.1. Lista de usuarios ✅

`ListarUsuariosAsync(string? busqueda = null)` → `RespuestaServicio<IReadOnlyList<DatosUsuario>>`

| | |
| --- | --- |
| **Búsqueda** | Opcional. Coincide con el **nombre de usuario o el nombre completo**, parcial y sin distinguir mayúsculas. Sin texto (o solo espacios) devuelve todas. Máximo `LimitesDeIdentidad.LongitudMaximaBusquedaDeUsuarios` (150) caracteres. |
| **Resultado** | Ordenado por nombre de usuario; **incluye las cuentas desactivadas** (`Activo = false`; mostrarlas marcadas). Hasta `LimitesDeIdentidad.MaximoDeUsuariosListados` (100) filas, sin paginación. |
| **Errores** | `SinPermiso`; `BusquedaDeUsuariosInvalida` («El texto de búsqueda no es válido.») si el texto pasa de 150. |
| **Notas** | Si llegan exactamente 100, puede haber más: sugerir afinar la búsqueda. Una búsqueda sin coincidencias es **éxito con lista vacía**. El correo no viene en la lista (solo en la ficha). |

#### 2.5.2. Ficha de un usuario ✅

`ObtenerUsuarioAsync(Guid idUsuario)` → `RespuestaServicio<FichaDeUsuario>`

- Devuelve los datos de la cuenta **y su correo**. No devuelve contraseña, hash, sello ni contadores.
- Errores: `SinPermiso`; `UsuarioNoEncontrado` («No se encontró el usuario.»).
- El identificador (`Guid`) sale de la fila de la lista; la página lo lleva en una ruta o campo oculto, y **el servidor lo vuelve a comprobar** con la autorización.

#### 2.5.3. Crear un usuario ✅

`CrearUsuarioAsync(string nombreUsuario, string rol, string nombreCompleto, string? correo = null)` → `RespuestaServicio<ContrasenaTemporalEntregada>`

**Esta firma cambió:** antes eran dos parámetros; el **nombre completo ahora es obligatorio**.

| Dato | Regla |
| --- | --- |
| `nombreUsuario` | Se recortan los espacios; hasta `LimitesDeIdentidad.LongitudMaximaNombreUsuario` (50) caracteres. Solo letras, dígitos y `-` `.` `_` `@` `+` (regla de Identity). No distingue mayúsculas para detectar duplicados. |
| `rol` | Uno de `NombresDeRol.Usuaria` o `NombresDeRol.Superusuario`. No se crean roles nuevos. |
| `nombreCompleto` | Obligatorio, 2 a 150 caracteres (`LimitesDeCuenta`), recortado. Sin caracteres de control ni de formato (saltos de línea, tabuladores, inversión de sentido, espacio de ancho cero). |
| `correo` | Opcional (`null` o vacío = sin correo). Hasta 254 caracteres; se guarda en minúsculas. Una sola `@`, texto antes y después, al menos un punto en el dominio, sin punto al inicio, al final ni repetido del dominio, sin espacios. **No se exige que sea único** y **no sirve para entrar, recuperar contraseñas ni recibir mensajes.** |

| Resultado | `Mensaje` |
| --- | --- |
| ✅ Éxito | «Usuario creado. Entrega la contraseña temporal por un canal seguro.» `Datos` trae la contraseña (§3). |
| Nombre de usuario vacío o largo, caracteres no permitidos o rol desconocido | `DatosDeUsuarioInvalidos` |
| Nombre completo inválido | `NombreCompletoInvalido` |
| Correo inválido | `CorreoInvalido` |
| Nombre de usuario ya existe (también si la cuenta está desactivada o cambian las mayúsculas) | `NombreDeUsuarioDuplicado` |

Si varias cosas están mal, el servidor informa **la primera** en este orden: nombre de usuario o rol, nombre completo, correo. La página puede validar antes en el navegador para ayudar, pero **no sustituye** la validación del servidor.

#### 2.5.4. Cambiar los datos de una cuenta ✅

`ActualizarDatosDeCuentaAsync(Guid idUsuario, string nombreCompleto, string? correo)` → `RespuestaServicio<FichaDeUsuario>`

- **Reemplaza** el nombre completo y el correo: el formulario se llena con la ficha actual y manda los dos valores. `correo` vacío o `null` **quita** el correo.
- El nombre completo es obligatorio también aquí (mismas reglas que al crear). Una cuenta creada sin nombre —como la del primer Superusuario— se completa con esta función.
- **No cierra sesiones** de la persona editada. Se puede corregir una cuenta desactivada y la propia.
- Si nada cambió, no se escribe nada ni se registra evento. Si cambió algo, el evento dice solo **qué campos** cambiaron, nunca sus valores.

| Resultado | `Mensaje` |
| --- | --- |
| ✅ Cambió algo | «Datos actualizados.» (`Datos` es la ficha ya actualizada) |
| ✅ Sin cambios | «No hubo cambios.» |
| Errores | `SinPermiso`, `NombreCompletoInvalido`, `CorreoInvalido`, `UsuarioNoEncontrado` |

#### 2.5.5. Cambiar el rol ✅

`CambiarRolAsync(Guid idUsuario, string nuevoRol)` → `RespuestaServicio<DatosUsuario>`

- Éxito: «Rol actualizado.» El cambio **cierra las sesiones de esa cuenta**: surte efecto en su siguiente solicitud. Si la persona se cambia **su propio** rol, su sesión actual también dejará de valer.
- Errores: `SinPermiso`; `DatosDeUsuarioInvalidos` (rol desconocido); `UsuarioNoEncontrado`; «El usuario ya tiene ese rol.»; `UltimoSuperusuario` («Debe quedar al menos un superusuario activo.»). Esta última protección vale aun con dos cambios simultáneos.

#### 2.5.6. Desactivar ✅

`DesactivarUsuarioAsync(Guid idUsuario)` → `RespuestaServicio<DatosUsuario>`

- Éxito: «Usuario desactivado.» **Cierra sus sesiones.** Desactivar una cuenta no es dar de baja a un paciente.
- Errores: `SinPermiso`; `UsuarioNoEncontrado`; «El usuario ya está desactivado.»; `UltimoSuperusuario`.
- No existe «reactivar» en B01.

#### 2.5.7. Restablecer la contraseña ✅

`RestablecerContrasenaAsync(Guid idUsuario)` → `RespuestaServicio<ContrasenaTemporalEntregada>`

- Genera una **contraseña temporal nueva**, invalida la anterior y **cierra las sesiones** de esa cuenta. **No levanta un bloqueo vigente** (máximo 15 minutos).
- Éxito: «Contraseña restablecida. Entrega la contraseña temporal por un canal seguro.» `Datos` trae la contraseña (§3).
- Errores: `SinPermiso`; `UsuarioNoEncontrado` (también si la cuenta está desactivada).
- **Antes de ejecutarlo**, la página debe recordar el procedimiento aprobado (B01 §8): verificar a la persona **en persona o con una llamada a un número que ya se conocía**, por un canal distinto al de entrega; nunca solo por mensaje. Conviene un paso de confirmación, porque repetir el envío genera **otra** contraseña y deja inservible la anterior.

## 3. Entrega de contraseñas temporales (crear y restablecer)

Aplica a `CrearUsuarioAsync` y `RestablecerContrasenaAsync` (decisión de Lucía, 12-oct, AGENTS §2.2).

1. La contraseña viaja en `Datos.ContrasenaTemporal` (16 caracteres, sin caracteres ambiguos como `0/O` o `1/l/I`, para poder dictarla). La respuesta **no se puede repetir**: el servidor no la guarda en texto.
2. La página la muestra **una sola vez, en la misma respuesta del POST** (`return Page()`, **no** una redirección), junto con el mensaje. Esa respuesta ya sale con `no-store`.
3. **Prohibido** ponerla en: dirección URL, `TempData`, sesión, cookies, campos ocultos que se reenvíen, mensajes de registro, correo, auditoría o capturas. `ContrasenaTemporalEntregada.ToString()` oculta el secreto, pero eso no autoriza a registrar la propiedad.
4. Quien administra la **entrega por otro canal** (en persona o por una vía que no sea correo). Si se cierra la página sin anotarla, no se puede recuperar: se hace un **nuevo restablecimiento**.
5. **Respuesta perdida o error 500 tras enviar:** no reintentar automáticamente ni desde JavaScript. La persona comprueba si se guardó (la lista de usuarios lo muestra) y, si hace falta, restablece otra vez.
6. La persona que recibe la temporal **solo puede entrar a CU11**, y ahí cambia la contraseña sin que se le pida la temporal otra vez (§2.3).

## 4. Mensajes que puede devolver el servidor

Constantes en `MensajesDeIdentidad` (`TherapEase.Application.Identidad.Constantes`). La página las muestra tal cual.

| Constante | Texto | Se usa en |
| --- | --- | --- |
| `CredencialesInvalidas` | Usuario o contraseña incorrectos. | Entrar. |
| `SinPermiso` | No tienes permiso para realizar esta operación. | Todo CU02; datos propios. |
| `UsuarioNoEncontrado` | No se encontró el usuario. | Ficha, actualizar, cambiar rol, desactivar, restablecer. |
| `DatosDeUsuarioInvalidos` | Los datos del usuario no son válidos. | Crear; cambiar rol. |
| `NombreCompletoInvalido` | El nombre completo no es válido. | Crear; actualizar datos. |
| `CorreoInvalido` | El correo no es válido. | Crear; actualizar datos. |
| `BusquedaDeUsuariosInvalida` | El texto de búsqueda no es válido. | Lista. |
| `NombreDeUsuarioDuplicado` | El nombre de usuario ya existe. | Crear. |
| `UltimoSuperusuario` | Debe quedar al menos un superusuario activo. | Cambiar rol; desactivar. |
| `CambioDeContrasenaNoRealizado` | No fue posible cambiar la contraseña. Verifica los datos e intenta de nuevo. | CU11. |
| `ContrasenaNuevaNoCumpleReglas` | La contraseña nueva no cumple las reglas de seguridad. | CU11. |

Además hay mensajes de éxito y dos de estado, sin constante: «Usuario creado…», «Rol actualizado.», «Usuario desactivado.», «Contraseña restablecida…», «Contraseña actualizada.», «Datos actualizados.», «No hubo cambios.», «El usuario ya tiene ese rol.» y «El usuario ya está desactivado.».

## 5. Tipos que devuelven las funciones

| Tipo | Campos |
| --- | --- |
| `DatosUsuario` | `IdUsuario` (`Guid`), `NombreUsuario`, `Rol`, `Activo`, `DebeCambiarContrasena`, `NombreCompleto` (vacío si la cuenta aún no lo tiene; mostrar el nombre de usuario en su lugar). |
| `FichaDeUsuario` | `Datos` (un `DatosUsuario`) y `Correo` (`string?`; `null` si no tiene). |
| `ContrasenaTemporalEntregada` | `Usuario` (un `DatosUsuario`) y `ContrasenaTemporal` (se muestra una vez, §3). |
| `RespuestaServicio<T>` | `Exito`, `Datos`, `Mensaje` (§1.2). |

**Constantes para los formularios:**

| Constante | Valor | Uso |
| --- | --- | --- |
| `LimitesDeIdentidad.LongitudMaximaNombreUsuario` | 50 | `maxlength` del nombre de usuario al crear. |
| `LimitesDeCuenta.LongitudMinimaNombreCompleto` / `LongitudMaximaNombreCompleto` | 2 / 150 | Nombre completo. |
| `LimitesDeCuenta.LongitudMaximaCorreo` | 254 | Correo. |
| `LimitesDeIdentidad.LongitudMaximaBusquedaDeUsuarios` | 150 | Cuadro de búsqueda. |
| `LimitesDeIdentidad.MaximoDeUsuariosListados` | 100 | Aviso de «afina la búsqueda». |
| `NombresDeRol.Usuaria` / `Superusuario` | — | Selector de rol. |

Los límites del navegador (`maxlength`, `required`) **ayudan a la persona**, pero el servidor vuelve a validar todo.

## 6. Trampas conocidas

1. **Bucle con `/Acceso/Denegado`.** Una cuenta con temporal pendiente que abra cualquier página operativa recibe «acceso denegado» y el servidor la manda a `/Acceso/Denegado`. Si esa página usa la política por defecto, vuelve a negarla y **se forma un bucle**. Debe ser `[AllowAnonymous]` y, si la sesión tiene la temporal pendiente, ofrecer o redirigir a CU11 (plan B01 §5.2: «Temporal conduce a CU11 sin mandar de una página a otra sin poder avanzar»).
2. **`_Layout` y menús.** Un menú que muestre enlaces operativos a quien tiene la temporal pendiente le lleva a páginas que le negarán el paso. Con temporal pendiente, los únicos enlaces útiles son el de CU11 y el de salir (la reclamación `Reclamaciones.DebeCambiarContrasena` dice en qué caso está).
3. **Salir no es «volver atrás».** Una sesión cerrada no se recupera con el botón del navegador: la siguiente solicitud ya no tiene sesión válida.
4. **Cuenta propia.** Si el Superusuario cambia su propio rol o se desactiva (cuando hay otro Superusuario), **su propia sesión dejará de valer** en la siguiente solicitud. La página puede avisar antes.
5. **Restablecer no es instantáneo para el bloqueo.** Una cuenta bloqueada sigue bloqueada hasta que venza (máximo 15 minutos), aunque se le restablezca la contraseña. No prometer lo contrario en pantalla.
6. **Duplicados por repetición.** Repetir el envío de «Crear» da «El nombre de usuario ya existe.»; repetir «Restablecer» genera **otra** contraseña. La página no debe reenviar sola.

## 7. Qué viene y no cambia las firmas

| Estado | Qué | Efecto en las páginas |
| --- | --- | --- |
| ⏳ | Eventos de inicio de sesión, salida y bloqueo (decisión de Lucía, 17-oct; espera que se anote la excepción en AGENTS §2.3). | **Las llamadas no cambian.** Lucía aprobó que, si el evento de **entrar** no se puede guardar, no se abra la sesión; Miguel propone que la persona vea el **error 500 fijo** y no «contraseña incorrecta» (⏳ pendiente de que Lucía lo confirme). Salir siempre cierra la sesión, aunque falle su evento. |
| ⏳ | Precisiones de Miguel sobre la ficha (§2.5.2) y los datos propios con temporal pendiente (§2.4). | Ya están en el código; si Lucía cambia algo, se avisa. Lo demás (lista, nombre y correo, permisos) está aprobado. |
| ⏳ | Temporal de 24 horas y segundo código de verificación (TOTP). | **Obligatorios antes de datos reales**; no se implementan en B01. |
| — | Reactivar cuentas, recuperar contraseña por correo, cambiar el nombre de usuario. | Fuera de B01. Las páginas no deben ofrecerlos. |

## 8. Antes de abrir el PR de las páginas

- [ ] Ningún PageModel usa la base de datos ni `UserManager` / `SignInManager` / `IGestorIdentidad`.
- [ ] Ningún formulario desactiva la antifalsificación; los modelos de entrada tienen solo los campos previstos.
- [ ] `/Acceso` y `/Acceso/Denegado` son `[AllowAnonymous]`; `/Acceso` lleva `no-store`; `returnUrl` solo acepta rutas locales.
- [ ] CU11 usa la política `CambiarContrasenaPropia`; CU02 usa `AdministrarUsuarios`.
- [ ] Mismo mensaje para todos los fallos de entrada; ninguna contraseña vuelve a pintarse.
- [ ] La contraseña temporal se muestra solo en la respuesta del POST: ni URL, ni `TempData`, ni registros.
- [ ] Salir es POST; nada reintenta solo un cambio.
- [ ] Todo texto del servidor se muestra como texto (sin `Html.Raw`).
- [ ] Unitarias propias en el mismo PR (AGENTS §2.6). Las pruebas con servidor, navegador y seguridad son de Lucía.

Para ejecutar el servidor local con PostgreSQL y probar los formularios, ver [B01 §4.2](TherapEase-etapa09-B01.md#42-cómo-se-conectarán-el-servidor-y-las-páginas) y el [README técnico §10](../../README.md#10-construir-probar-y-ejecutar).
