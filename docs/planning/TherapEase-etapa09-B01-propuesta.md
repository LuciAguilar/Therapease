# TherapEase — B01 · Lo que ya existe y propuesta de Miguel

**Borrador del 17-oct-2026. Decisiones de Lucía registradas el mismo día (§3.5).** Las precisiones que Miguel añadió después de esas decisiones están marcadas como «Precisión de Miguel» y esperan confirmación de Lucía antes de programar lo que tocan. Autor: Miguel. Revisa y decide: Lucía. Rama `b01/acceso-y-usuarios`, a partir de `main` `ffb2494` más los documentos del equipo (`e918a3d`).

Este documento cumple dos entregas que pide el [plan B01](TherapEase-etapa09-B01.md): la lista corta de lo que existe y falta (§2, primera fila) y la propuesta conjunta de §3.2. **No contiene código.** Nada de lo propuesto se programa hasta que Lucía lo apruebe ([AGENTS](../../AGENTS.md) §3). Lo aprobado en §3.5 ya puede programarse con sus ajustes; la excepción de §3.2 se anota antes en AGENTS §2.3. ✅ indica algo ya existente o comprobado; ⏳ indica algo pendiente o propuesto.

## 1. Lista corta: lo que existe y lo que falta

### 1.1. Funciones del servidor

| Estado | Función | Dónde está | Qué hace hoy | Qué falta |
| --- | --- | --- | --- | --- |
| ✅ | Entrar y salir | `IServicioAcceso` | Valida credenciales, cuenta activa y bloqueo; el conteo de fallos se guarda siempre. La sesión se abre solo después de guardar. Salir renueva el sello y cierra la cookie. Mismo mensaje para nombre desconocido, clave incorrecta y bloqueo. | Registrar los hechos de acceso (§3.2). Páginas (Dulce). |
| ✅ | Administrar usuarios | `IServicioUsuarios` | Crear, cambiar rol, desactivar y restablecer. Exige el permiso `AdministrarUsuarios`. Cambio y evento se guardan juntos. El último Superusuario está protegido por el servicio y por la base (disparador `TE002`). | Elegir un usuario de una lista y ver su ficha (§3.1). Nombre y correo (§3.3). Páginas. |
| ✅ | Cambiar la contraseña propia | `IServicioContrasena` | Ordinario: exige la actual y un fallo cuenta para el bloqueo. Con temporal: pide solo la nueva. Cierra las demás sesiones y renueva la actual. | Formulario (Dulce). |
| ✅ | Primer Superusuario | `IServicioArranqueDeIdentidad` y comandos `--crear-superusuario` / `--restablecer-superusuario` | La temporal sale solo por la consola. | Nada para B01. |
| ✅ | Permisos | `IAutorizacion`, `MatrizDePermisos` | Tres permisos: `AdministrarUsuarios`, `ConsultarAuditoria`, `DarDeBajaPacientes`. Se comprueban en cada solicitud contra la base. | Nada: los permisos explícitos por rol, con su prueba de completitud (§3.4), se entregaron en el PR #7. |
| ✅ | Sesión y cookie | `ConfiguracionDeSeguridad` | 30 minutos con renovación; `HttpOnly`, `Secure`, `SameSite=Lax`; sello revisado en cada solicitud; claves en PostgreSQL. | Nada. |
| ✅ / ⏳ | Auditoría y consulta Q03 | `IServicioAuditoria`, `/api/auditoria/eventos` | Registra cada cambio de usuario y contraseña, sin valores. | Hechos de acceso, salida y bloqueo (§3.2): programados el 22-oct y código aprobado por Lucía el 24-oct, ver §7. |
| ✅ | Cabeceras y error 500 fijo | `CabecerasDeSeguridad`, `ManejadorDeErroresInesperados` | Incorporados en el PR #5. | Probar con las páginas. |
| ✅ | Comprobaciones automáticas (CI) | `.github/workflows/comprobaciones.yml` y `scripts/ci/` | `main` ya tiene el flujo, desde el PR #7 (`922a4e3`): cuatro trabajos que compilan, ejecutan las pruebas y revisan paquetes, fijaciones, secretos, migraciones e imagen. | Nada para B01; Lucía mide los minutos (A-06). Se ejecutó dos veces en el PR #7 y la segunda terminó correcta (§6). |

### 1.2. Pruebas disponibles

| Estado | Pruebas | Cantidad y límite |
| --- | --- | --- |
| ✅ | Unitarias de toda la solución | **258 correctas, 0 fallidas**, ejecutadas el 17-oct-2026 a las 11:59 (UTC−7) sobre `e918a3d`. De ellas, **124** son de acceso, usuarios, contraseñas y permisos: `GestorIdentidad` 29, `ServicioUsuarios` 21, `Autorizacion` 16, `MatrizDePermisos` 15, `ServicioContrasena` 11, `ServicioArranqueDeIdentidad` 9, `ServicioAcceso` 7, `ManejadorDeRequisitoDePermiso` 6, `ReglasDeRol` 5, `GeneradorDeContrasenaTemporal` 4 y `ContrasenaTemporalEntregada` 1. |
| ✅ | Integración de B00 (servidor y PostgreSQL desechable) | Resultados en [B00 §4.1, §4.7 y §4.10–§4.11](TherapEase-etapa09-B00.md). Los casos de identidad son `IdentidadServidorTests` 21, `InterrupcionesIdentidadTests` 16 y `IdentidadComplementariaTests` 5. **No se repitieron en este documento.** |
| ⏳ | 35 casos de servidor de Lucía | `UsuariosServidorB01Tests` 24, `AuditoriaServidorB01Tests` 6 y `ConcurrenciaServidorB01Tests` 5. **Retirados de este PR por decisión de Lucía (18-oct):** ella los adaptará a los cambios del PR y los entregará en uno propio, para revisión de Miguel; esperan auditoría ([AGENTS](../../AGENTS.md) §3) y **no se cuentan como evidencia aprobada.** Actualización del 22-oct: Lucía las entregó en su PR #8, fusionado el 20-oct en `7a73e0a`. |

Sin esos 35, esta rama lista los 205 casos de integración de `main`. Después de cada cambio se guardan nueva fecha, versión y resultados; no se copian conteos anteriores.

### 1.3. Lo que faltaba completar, en orden, y cómo va

| # | Qué faltaba | Dónde se decide o se explica, y cómo va |
| --- | --- | --- |
| 1 | Consulta para elegir un usuario y ver su ficha (CU02) | §3.1. ✅ Entregada en el PR #7. |
| 2 | Eventos de acceso, salida y bloqueo | §3.2. ✅ Código aprobado por Lucía el 24-oct en el PR #9; ⏳ fusión. |
| 3 | Nombre completo y correo | §3.3. ✅ Entregados en el PR #7. |
| 4 | Permisos del Superusuario escritos uno por uno | §3.4. ✅ Entregados en el PR #7. |
| 5 | **Protección de intentos con nombres desconocidos.** No está en §3.2, pero [CU01 A1](../requirements/TherapEase-etapa08-casos-historias.md) la deja a Miguel. Hoy un nombre desconocido no tiene contador: solo se iguala el tiempo de respuesta. | §4. **Decisión de Lucía:** fuera de B01; pendiente antes de datos reales. |
| 6 | Comprobaciones automáticas (CI) | Empezó sin esperar decisiones. ✅ Entregadas en el PR #7; `main` ya tiene el flujo. |
| 7 | Contrato de cada función para Dulce | Documento aparte, después de §3. ✅ Entregado en el PR #7 y actualizado en el PR #9. |
| 8 | Cinco comentarios del código decían «pendiente de decisión» aunque ya estaba decidido (contraseña de 12 caracteres, sesión de 30 minutos, restablecer sin levantar bloqueo, comandos de operador). No cambian el comportamiento. | ✅ Corregidos en el PR #7. |

## 2. Cómo leer la propuesta

Cada asunto tiene **hoy**, **recomendación**, **ejemplo**, **si algo falla**, **qué decide Lucía** e **impacto**. Al final, §3.5 reúne las decisiones para responderlas juntas. Mi recomendación busca lo más simple que cumpla las reglas aprobadas; Lucía puede cambiarla.

## 3. Propuesta de §3.2 del plan B01

### 3.1. Elegir un usuario en CU02

**Hoy.** Cada operación recibe el identificador interno (`Guid`) de la cuenta. El servidor puede buscar una cuenta por identificador o por nombre exacto, pero no hay lista ni búsqueda. Una página no tiene de dónde sacar el identificador.

**Recomendación.**

- Una consulta nueva `ListarUsuariosAsync` en `IServicioUsuarios`, con un texto de búsqueda opcional.
- Exige el permiso `AdministrarUsuarios`, sesión válida y sin contraseña temporal. **No se crea un permiso nuevo.**
- Devuelve solo lo que ya usa `DatosUsuario`: identificador, nombre de usuario, rol, activo y cambio de contraseña pendiente; con §3.3 aprobado, añade el nombre completo. **El correo no va en la lista**, solo en la ficha de una cuenta. Nunca devuelve hash, sello, contadores de fallos ni datos internos de Identity.
- La búsqueda coincide con el nombre de usuario o el nombre completo, sin distinguir mayúsculas, con consulta parametrizada. Orden por nombre de usuario; incluye cuentas desactivadas, marcadas como tales.
- **Sin paginación y con un tope de 100 resultados.** Hay cuatro cuentas ficticias y una usuaria operativa prevista; paginar sería trabajo sin uso.
- Leer no se audita: es de solo lectura, sin datos sensibles. Q03 conserva los cambios.
- **Precisión de Miguel, aprobada por Lucía el 18-oct (precisión 1): la «ficha» no tenía operación.** Se añade `ObtenerUsuarioAsync(idUsuario)` en `IServicioUsuarios`, con las mismas exigencias que la lista (permiso `AdministrarUsuarios`, sesión válida y sin temporal pendiente). Devuelve los datos de la lista y el correo. **El correo solo lo recibe el Superusuario activo, autorizado y sin temporal pendiente.** Si la cuenta no existe, responde que no se encontró, sin otros datos. Tampoco devuelve hash, sello ni contadores.

**Ejemplo.** El Superusuario abre «Usuarios y roles» y ve las cuentas. Escribe «mar» y aparece «mariana.ficticia, Usuaria, activa». Elige «Restablecer contraseña» y la página llama a `RestablecerContrasenaAsync` con ese identificador.

**Si algo falla.** Sin sesión, sin permiso o con temporal pendiente: rechazo con el mensaje de falta de permiso y sin datos. Si la base falla: el 500 fijo que ya existe.

**Decisión de Lucía (17-oct).** ✅ Aprobado como se propone: (a) sin paginación y tope de 100; (b) correo solo en la ficha; (c) mostrar cuentas desactivadas.

**Impacto.** Application e Infrastructure, sin migración. Incluye `ListarUsuariosAsync` y `ObtenerUsuarioAsync`. Unitarias mías; la prueba con base de datos es de Lucía.

### 3.2. Registrar acceso, salida, bloqueo y cierre forzado

**Hoy.** `ServicioAcceso` no registra eventos de auditoría. `AccionAuditoria` no tiene acciones de acceso. Un evento exige un identificador de actor y otro de registro, no vacíos; en un intento con nombre desconocido no existe ninguno. La acción se guarda como texto de hasta 20 caracteres **sin restricción en la base**, así que añadir acciones nuevas no requiere migración. 08C describe los eventos como «cambios relevantes»: registrar accesos **amplía ese alcance**, por eso lo pido como decisión.

**Recomendación.**

| Hecho | Cuándo | Acción | Actor y registro | Texto del hecho |
| --- | --- | --- | --- | --- |
| Entró | Credenciales correctas y sesión abierta | `InicioSesion` | La cuenta | «Inició sesión.» |
| Salió | Cierre voluntario | `CierreSesion` | La cuenta | «Cerró sesión.» |
| Acceso bloqueado | El quinto fallo inicia el bloqueo, una vez por bloqueo, **al entrar o al escribir mal la contraseña actual en CU11** | `BloqueoAcceso` | El usuario afectado | «Se bloqueó el acceso por cinco intentos fallidos.» |
| Cierre forzado | Desactivar, restablecer, cambiar rol o cambiar la contraseña propia | **Sin evento aparte** | — | Ya queda en el evento de esa acción |

- **No se registra** cada intento fallido, para no llenar la tabla y porque un nombre desconocido no tiene cuenta ni actor. **Tampoco el nombre escrito** en un fallo, porque puede contener una contraseña tecleada por error o texto malicioso. **Ni IP ni navegador**, que son datos personales no pedidos; Lucía puede pedirlos después.
- Los campos afectados quedan vacíos y no se guarda ningún valor. Se consulta con Q03, sin pantalla nueva.

**Si falla el registro.**

- **Entrar:** el evento y el reinicio del contador se guardan juntos; si falla, **no se abre la sesión** y la persona ve el mismo mensaje genérico (CU01 A7). Coincide con la regla de guardado de AGENTS §2.3. **Precisión de Miguel, aprobada por Lucía el 21-oct:** el «mensaje seguro» de CU01 A7 es el error general que ya existe, el 500 fijo. No se presenta como «contraseña incorrecta»: eso ocultaría una falla de la base y haría creer a la persona que escribió mal la clave.
- **Salir (ajuste de Lucía, 17-oct):** primero se renueva el sello y se retira la cookie; el evento se guarda después, en otra transacción. Si el evento falla, se deja un registro técnico sin datos privados y **la sesión queda cerrada igual**. Una falla de auditoría no debe impedir salir ni dejar abierta la sesión. Es la misma excepción que el bloqueo (opción A).
- **Bloqueo, con riesgo.** Si el evento se guardara junto al conteo de fallos y su inserción fallara, el conteo se desharía y **el bloqueo dejaría de aplicarse**. Tres opciones:
  - **A (recomendada):** primero se guarda el conteo y el bloqueo; el evento se guarda después en otra transacción. Si ese evento falla, se deja un registro técnico sin datos privados y el bloqueo sigue. Aplica igual si el bloqueo nace en CU11. **Es una excepción a «cambio y evento juntos» y necesita aprobación expresa.**
  - **B:** no registrar el bloqueo. Es lo más simple, pero Lucía lo pidió expresamente.
  - **C:** todo junto, sin excepción. Un fallo de auditoría permitiría saltarse el bloqueo; **no lo recomiendo.**
- **Dónde vive el registro técnico. Precisión de Miguel, aprobada por Lucía el 21-oct.** Application solo referencia a Domain y no tiene dependencia de registro; hoy solo Infrastructure registra. El puerto pequeño `IRegistroTecnico` está en Application y lo implementa Infrastructure con el registro que ya incluye .NET. **Sin paquetes nuevos ni ADR.** Cada aviso es un método **sin parámetros** con texto fijo: no puede recibir nombre de usuario, correo, valores, excepción ni datos de la solicitud. Hay dos avisos, uno para el evento de salida y otro para el de bloqueo (opción A). Si falla el evento de entrada, no hay aviso aparte: la excepción llega al error general, que ya registra el servidor.

**Ejemplo en Q03, un día cualquiera.** 09:02 `InicioSesion` de «ana.ficticia». 09:40 `CierreSesion` de la misma. 10:15 `BloqueoAcceso` de «luis.ficticio».

**Decisión de Lucía (17-oct).** ✅ (a) Se amplía la auditoría a acceso y salida. (b) Acciones `InicioSesion`, `CierreSesion` y `BloqueoAcceso`, con los textos de la tabla. (c) Bloqueo: opción A. (d) Entrar falla si el evento no se guarda; **salir no**: cierra primero y registra después. (e) Sin IP ni nombre escrito. La excepción a «cambio y evento juntos» para salida y bloqueo se anota en AGENTS §2.3 antes de programarla (anotada el 18-oct). (f) Aprobado por Lucía el 21-oct, para el PR de los eventos: el evento de entrada falla con el error general existente; `IRegistroTecnico` en Application, implementado en Infrastructure con el registro de .NET; avisos fijos, sin datos privados ni paquetes nuevos; un evento por bloqueo, también si nace al fallar la contraseña actual en CU11; los cierres por desactivar, cambiar el rol o la contraseña quedan en la operación que los causa, sin otro evento.

**Impacto.** Tres acciones nuevas en el enum, sin migración (la columna admite hasta 20 caracteres y no limita los valores). Cambian `ServicioAcceso` y `ServicioContrasena` (bloqueo nacido en CU11), y `IGestorIdentidad.VerificarCredencialesAsync` y `VerificarContrasenaActualAsync` ahora devuelven un resultado que indica si el intento inició el bloqueo. Además, el puerto `IRegistroTecnico`. Unitarias mías. Varias pruebas de integración contaban el total de eventos (por ejemplo, esperaban 0 o 2) en archivos que también inician sesión: se identificaron 43 casos afectados y los ajustes, sin quitar casos, están listados en el [plan de pruebas §0.8](../testing/TherapEase-09-plan-de-pruebas.md#08-avance-de-miguel-en-b01-eventos-de-acceso-salida-y-bloqueo--22-oct-2026) para que Lucía los revise.

**Dos detalles que ya estaban decididos.** (1) El actor de `BloqueoAcceso` es el usuario afectado: ya figuraba en la tabla aprobada el 17-oct y AGENTS §2.3 lo repite; no es una decisión nueva. (2) Si falla el evento de entrada se usa el error 500 general, sin un tercer aviso ni método en `IRegistroTecnico` (AGENTS §2.3, aprobado el 21-oct). No queda ninguna preferencia nueva pendiente de decisión.

### 3.3. Nombre completo y correo

**Hoy.** La cuenta solo tiene nombre de usuario. La tabla de Identity ya trae una columna `Email` (hasta 256 caracteres) sin uso y con `RequireUniqueEmail` desactivado. No existe `NombreCompleto`. La psicóloga pidió ambos datos; falta decidir finalidad, obligatoriedad, validación y quién los ve o cambia ([entrevista](../requirements/TherapEase-09-entrevista-requisitos.md)).

**Recomendación.**

- **Finalidad.** El nombre identifica a la persona en pantalla. El correo es dato de contacto interno. **El correo no sirve para entrar, recuperar contraseñas ni enviar mensajes** (§3.2 del plan).
- **Nombre completo.** Columna nueva `NombreCompleto`, texto de 2 a 150 caracteres, **obligatorio al crear un usuario desde CU02**. Se recortan los espacios y no se admiten caracteres de control. Las cuentas ficticias que ya existen quedan vacías hasta editarlas; la pantalla mostrará el nombre de usuario mientras tanto. **El comando `--crear-superusuario` no cambia** (ajuste de Lucía): crea el usuario con el nombre vacío y se completa después con `ActualizarDatosDeCuentaAsync`.
- **Correo.** Usa la columna `Email` que ya existe. **Opcional**; si se escribe, hasta 254 caracteres y guardado en minúsculas. No se exige que sea único. **Formato válido (ajuste de Lucía):** una sola @, texto antes y después, al menos un punto en el dominio y sin espacios; así «erika@ficticio» se rechaza. **Precisión de Miguel, aprobada por Lucía el 18-oct (precisión 2):** el dominio no empieza ni termina con punto y no tiene puntos seguidos; así «erika@.com», «erika@ficticio.» y «erika@ficticio..mx» también se rechazan.
- **Cambiar nombre o correo no cierra sesiones** (ajuste de Lucía): el correo no sirve para entrar. Ojo: `SetEmailAsync` de Identity renueva el sello y cerraría las sesiones del usuario; hay que guardar el dato de otra forma.
- **Quién lo ve y lo cambia.** El Superusuario ve y cambia los de cualquier cuenta, con una operación nueva `ActualizarDatosDeCuentaAsync` y el permiso `AdministrarUsuarios`. Cada persona ve **los suyos** pero no los cambia, para evitar que alguien se presente con otro nombre; si hay un error, lo pide al Superusuario. **Para ver los suyos se añade una consulta de solo lectura de los datos propios** (ajuste de Lucía): toma al usuario de la sesión actual y no recibe un identificador.
- **Datos propios con contraseña temporal pendiente. Precisión de Miguel, aprobada por Lucía el 18-oct (precisión 4).** La consulta de datos propios **sí funciona** con temporal pendiente, **solo como apoyo a CU11**: lee los de la propia sesión, que es de donde sale el identificador, y la página de CU11 puede mostrar el nombre. No habilita otras páginas, funciones operativas ni datos ajenos, y no concede permisos nuevos. AGENTS §2.2 sigue diciendo que con temporal solo se accede a CU11.
- **Auditoría.** Cada cambio genera un evento `Actualizacion` sobre la cuenta, **sin valores**. **Precisión de Miguel, aprobada por Lucía el 18-oct (precisión 3):** los campos afectados son únicamente los nombres de los campos que cambiaron (`NombreCompleto`, `Correo` o ambos); si no cambió nada, no se escribe nada ni se genera evento. Los registros técnicos y los errores nunca incluyen nombre ni correo.
- **Cookie.** No viajan en ella: se leen de la base cuando una página los necesita, así un cambio se ve en la siguiente solicitud.
- **Alta.** `CrearUsuarioAsync` recibe además el nombre completo y, si se quiere, el correo.

**Ejemplo.** Alta de `erika.ficticia` con nombre «Erika Ficticia Pérez» y sin correo: se crea. Con correo «erika@ficticio»: «El correo no es válido» y no se crea nada.

**Si algo falla.** Dato inválido: rechazo con mensaje de datos inválidos, sin guardar. Si falla el registro del evento: no se guarda, igual que el resto de cambios de usuario.

**Decisión de Lucía (17-oct).** ✅ (a) Nombre obligatorio al crear desde CU02 y correo opcional. (b) Solo el Superusuario cambia los datos; cada persona consulta los suyos. (c) Correo sin exigir unicidad. (d) Límite de 150 caracteres para el nombre. Cambiar estos datos no cierra sesiones.

**Impacto.** **Una migración** que añade `NombreCompleto` a `AspNetUsers`. Además, la consulta de datos propios. El rol `therapease_app` ya tiene `SELECT, INSERT, UPDATE` sobre toda la tabla, así que la columna queda cubierta. Revertirla elimina los nombres: aceptable con datos ficticios; con datos reales exigiría respaldo previo (R04). Lucía revisa SQL y reversión. Unitarias mías.

**Precisiones técnicas de Miguel.**

- `CrearAsync` del gestor de identidad recibe nombre y correo como datos opcionales, de modo que `--crear-superusuario` sigue llamándolo igual y crea la cuenta con el nombre vacío.
- Identity renueva el sello al cambiar el correo con `SetEmailAsync`. Antes de dar por buena la implementación se comprobará, con Identity real y no con el doble de pruebas, que guardar nombre o correo **no** cambia el sello ni cierra sesiones.

### 3.4. Permisos del Superusuario, uno por uno

**Hoy.** `RolTienePermiso` devuelve verdadero para el Superusuario con **cualquier** valor definido de `Permiso`, y su lista se calcula con `Enum.GetValues`. Hoy son tres. Si mañana se añade un cuarto, el Superusuario lo recibe sin que nadie lo decida.

**Recomendación.**

- Una tabla explícita por rol, con cada permiso nombrado. **El resultado de hoy no cambia.**
- Una **prueba de completitud**: para cada valor de `Permiso` exige una decisión escrita (permitido o denegado) por cada rol. Quien añada un permiso sin decidirlo ve fallar la prueba.
- **B01 no necesita permisos nuevos:** la lista de usuarios (§3.1) y la edición de nombre y correo (§3.3) usan `AdministrarUsuarios`. Si Lucía prefiere separar «consultar» de «administrar», se añade `ConsultarUsuarios`, con las mismas pruebas.

| Permiso | Superusuario | Usuaria |
| --- | --- | --- |
| `AdministrarUsuarios` | ✅ Permitido | ✅ Denegado |
| `ConsultarAuditoria` | ✅ Permitido | ✅ Denegado |
| `DarDeBajaPacientes` | ✅ Permitido | ✅ Permitido (provisional, decisión del 11-oct) |

**Ejemplo.** En B02 se añade `ConsultarPacientes`: no pertenece a ningún rol hasta que alguien lo escriba en la tabla y en la prueba, y esta falla recordándolo.

**Decisión de Lucía (17-oct).** ✅ (a) Ningún permiso nuevo en B01. (b) Tabla explícita con prueba de completitud.

**Impacto.** Solo Domain (`MatrizDePermisos`) y unitarias; sin migración. Es un cambio de permisos, que Lucía revisa.

### 3.5. Decisiones que pido

| # | Decisión | Mi recomendación | Decisión de Lucía (17-oct) |
| --- | --- | --- | --- |
| 1 | Lista de usuarios (§3.1) | Sin paginación, tope de 100, correo solo en la ficha, permiso existente. | ✅ Aprobado. |
| 2 | Alcance de los eventos (§3.2) | Ampliar a acceso y salida: `InicioSesion`, `CierreSesion`, `BloqueoAcceso`. Sin fallos individuales, nombre escrito ni IP. Cierre forzado dentro del evento que lo causa. | ✅ Aprobado. `BloqueoAcceso` también cuando el bloqueo nace en CU11. |
| 3 | Si el evento no se guarda (§3.2) | Entrar y salir fallan. Bloqueo: opción A, con su excepción. | ✅ Entrar falla. **Salir y bloqueo: opción A** (cerrar o bloquear primero, registrar después). Excepción anotada en AGENTS §2.3 antes de programar. |
| 4 | Nombre y correo (§3.3) | Nombre obligatorio de hasta 150, correo opcional y válido, solo el Superusuario edita, sin valores en auditoría, una migración. | ✅ Aprobado, con ajustes: correo con punto en el dominio; cambiar estos datos no cierra sesiones; comando del primer superusuario sin cambios; consulta de datos propios. |
| 5 | Permisos (§3.4) | Tabla explícita, prueba de completitud, ningún permiso nuevo. | ✅ Aprobado. |
| 6 | Intentos con nombres desconocidos (§4) | Sin umbral propuesto. | Fuera de B01; pendiente antes de datos reales. Miguel propone después un límite con el limitador que ya incluye .NET, sin paquetes nuevos. |
| 7 | Precisiones de Miguel posteriores a la aprobación | (a) Operación de ficha `ObtenerUsuarioAsync` (§3.1). (b) Si entrar falla por el evento, el error 500 fijo, no «contraseña incorrecta» (§3.2). (c) Puerto `IRegistroTecnico` en Application, sin paquetes ni ADR (§3.2). (d) Dominio del correo sin punto al inicio ni al final y sin puntos seguidos (§3.3). (e) Campos de auditoría solo los cambiados; sin cambio, sin evento (§3.3). (f) Datos propios consultables con temporal pendiente (§3.3). | Aprobadas. (a), (d), (e) y (f) el 18-oct, con la ficha con correo solo para el Superusuario activo, autorizado y sin temporal. (b) y (c) aprobadas por Lucía el 21-oct. |

## 4. Fuera de esta propuesta

- **Páginas, formularios y PageModel:** Dulce. El contrato de cada función se entrega aparte.
- **Intentos con nombres desconocidos (CU01 A1):** hoy no tienen contador y el servidor solo iguala el tiempo de respuesta. Hay que decidir si entra en B01 y con qué límite; **no propongo un umbral**. **Decisión de Lucía (17-oct):** fuera de B01 y pendiente antes de datos reales; Miguel propone después un límite con el limitador que ya incluye .NET, sin paquetes nuevos.
- **Temporal de 24 horas y TOTP:** obligatorios antes de datos reales, no se implementan ahora.
- **`UltimoAcceso` de 08C:** no lo pide el plan B01 y no se incluye.
- **Recuperación o notificaciones por correo, IP y navegador en los eventos:** fuera de alcance.
- Montos, «solo particular» y baja automática, como indica el plan.

## 5. Orden de trabajo después de aprobar

1. **Permisos explícitos** (§3.4): es pequeño y deja listas las reglas para lo siguiente.
2. **Nombre y correo** (§3.3): la migración y las operaciones de alta y edición.
3. **Lista y ficha de usuarios** (§3.1).
4. **Eventos de acceso** (§3.2): toca el inicio de sesión, la salida y el cambio de contraseña propia, así que va aparte y con revisión cuidadosa.

Cada cambio lleva sus unitarias en el mismo PR. Dos PR: uno con los puntos 1 a 3 y las comprobaciones automáticas (CI), y otro con el 4. El CI va en el mismo PR por indicación de Miguel del 17-oct, no en uno aparte. Después de cada PR actualizo este documento, el plan de pruebas y el README técnico con la fecha, la versión y los resultados.


## 6. Estado de implementación — 17-oct-2026

Rama `b01/acceso-y-usuarios`, código probado `c3f2b63de2fce7e5f265119ba292f6c2017d93c5`, con unitarias en cada cambio; [PR #7](https://github.com/LuciAguilar/Therapease/pull/7). Resultados, comandos y huellas en el [plan de pruebas §0.7](../testing/TherapEase-09-plan-de-pruebas.md#07-avance-de-miguel-en-b01--17-oct-2026). Falta la revisión de otra persona y la aprobación final de Lucía.

| # de §1.3 | Qué | Estado |
| --- | --- | --- |
| 1 | Consulta para elegir un usuario y ver su ficha (§3.1) | ✅ Programada: `ListarUsuariosAsync` y `ObtenerUsuarioAsync`. |
| 2 | Eventos de acceso, salida y bloqueo (§3.2) | Aprobados el 17-oct; programados después, ver §7. |
| 3 | Nombre completo y correo (§3.3) | ✅ Programados, con la migración `NombreCompletoDeUsuario`, la edición por el Superusuario y la consulta de datos propios. El evento de alta también lista `NombreCompleto` y, si se capturó, `Correo` (decisión de Lucía, 17-oct). |
| 4 | Permisos del Superusuario escritos uno por uno (§3.4) | ✅ Programados, con la prueba de completitud. |
| 5 | Protección de intentos con nombres desconocidos | Fuera de B01, por decisión de Lucía. |
| 6 | Comprobaciones automáticas (CI) | Comprobadas en local y en GitHub (PR #7): la ejecución 2, del 18-oct, terminó correcta en sus cuatro trabajos; la 1 tuvo 7 fallos de integración, por una causa probable en la ayuda de pruebas de Docker, ya corregida. Falta que Lucía compruebe el flujo y mida los minutos. |
| 7 | Contrato de cada función para Dulce | ✅ [Contrato para páginas](TherapEase-etapa09-B01-contrato-para-paginas.md). |
| 8 | Cinco comentarios del código que decían «pendiente de decisión» | ✅ Actualizados con la decisión del 04-oct. |

Las precisiones de Miguel marcadas en §3 (la ficha, la forma del error al entrar, el puerto `IRegistroTecnico`, el dominio del correo, los campos del evento de actualización y los datos propios con temporal pendiente) quedaron aprobadas: las de la ficha, el dominio, los campos del evento y los datos propios por Lucía el 18-oct, y la del error al entrar y `IRegistroTecnico` el 21-oct. El estado de los eventos está en §7.

## 7. Estado de implementación de los eventos de acceso — 25-oct-2026

[PR #9](https://github.com/LuciAguilar/Therapease/pull/9), rama `b00/correcciones-pr4`, usada como rama de trabajo por indicación de Miguel. El código probado está en el commit `e3f227c`; el revisado es `cb7da59`, cuyo commit posterior solo cambia documentos. Resultados, comandos, huellas y los ajustes a pruebas de Lucía en el [plan de pruebas §0.8](../testing/TherapEase-09-plan-de-pruebas.md#08-avance-de-miguel-en-b01-eventos-de-acceso-salida-y-bloqueo--22-oct-2026).

**Código ejecutado:** 422 unitarias y 319 de integración correctas en local (22-oct) y la [ejecución de CI de GitHub](https://github.com/LuciAguilar/Therapease/actions/runs/39905673597), correcta en sus cuatro trabajos (2 min 46 s). **Revisión:** Lucía aprobó el código el 24-oct tras la auditoría de Claude, según su revisión del PR #9. **Pendiente:** los ajustes documentales de esa revisión (aplicados el 25-oct, falta que Lucía los revise) y la fusión.

| Estado | Qué | Detalle |
| --- | --- | --- |
| ✅ | `InicioSesion` junto con la verificación de la entrada | Si falla el evento, no se abre sesión y llega el error general. Ejecutado con unitarias, a mano contra PostgreSQL real y en el QA local de Lucía. |
| ✅ | `CierreSesion` después de cerrar, en otra transacción | Si falla, la sesión queda cerrada y queda un aviso técnico fijo. |
| ✅ | `BloqueoAcceso`, una vez por bloqueo, al entrar y en CU11 | También con intentos simultáneos. El bloqueo se guarda antes que su evento y, si el evento falla, el bloqueo sigue y queda un aviso técnico fijo. |
| ✅ | Cierres forzados sin evento aparte | Cambiar el rol, restablecer y desactivar dejan solo su propio evento. |
| ✅ | `IRegistroTecnico` | Aprobado el 21-oct: dos avisos sin parámetros, con el registro de .NET y sin paquetes nuevos. |
| ✅ | Actor de `BloqueoAcceso` y fallo del evento de entrada | No son decisiones nuevas: el actor ya estaba en la tabla del 17-oct y, si falla la entrada, se usa el error 500 general sin un tercer aviso (AGENTS §2.3, 21-oct). |
| ⏳ | Ajustes documentales de la revisión del 24-oct | Aplicados el 25-oct en propuesta, plan B01, plan de pruebas, contrato y descripción del PR; falta su revisión. |
| ⏳ | Fusión del PR y probarlos con las páginas de Dulce | Lucía aprobó squash para este PR. Las páginas y el navegador siguen con Dulce y el QA posterior de Lucía. |
