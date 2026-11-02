# TherapEase — Guía para Dulce · B00 y B01

> **Ya puedes comenzar a conectar las páginas de B01.** El servidor de acceso, cuentas y contraseñas está incorporado en main y tiene pruebas revisadas. No necesitas esperar a que termine B02 ni todo el QA de Lucía.

**Comprobado el 28-oct-2026.** Esta guía reúne el trabajo de B00 y B01. No cambia las reglas de [AGENTS](../../AGENTS.md) ni da por terminados los bloques.

| Bloque | Para entenderlo rápido | Situación |
| --- | --- | --- |
| **B00 · Los cimientos y los candados** | Base del sistema, protección de datos y comprobaciones de seguridad. | Hay base y pruebas incorporadas. Faltan comprobaciones con las páginas y otros pendientes del plan. |
| **B01 · La puerta de entrada y las cuentas** | Entrar, salir, administrar usuarios y cambiar contraseñas. | Las funciones del servidor ya están. Faltan las páginas conectadas y probarlas en el navegador. |

Una **cuenta** identifica a quien usa el sistema. Una **ficha de usuario** muestra los datos de esa cuenta. No es una ficha de paciente.

---

## 1. Qué te corresponde y con quién cuentas

| Persona | Responsabilidad |
| --- | --- |
| **Dulce** | Diseño, páginas, formularios, mensajes, navegación y PageModel: la parte que recibe el formulario y llama a las funciones de Miguel. También sus pruebas propias y las confirmaciones de producto con la usuaria. |
| **Miguel** | Funciones del servidor, reglas, permisos y guardado. Sus pruebas propias y apoyo cuando aparezca un problema al conectar las páginas. |
| **Lucía** | Pruebas del conjunto, seguridad y navegador. Decide los cambios y da la aprobación final. |

> Tú eliges tus herramientas y tu IA. El circuito personal Codex–Claude de Lucía no se te exige. El código y las pruebas necesitan revisión de otra persona, las comprobaciones automáticas correctas y aprobación final de Lucía.

## 2. B00 · Los cimientos y los candados

### Qué existe y qué te toca aprovechar

| Base que ya tenemos | Qué te corresponde |
| --- | --- |
| Comprobación de cuenta, sesión y permisos en el servidor. | Conectar las páginas con esas funciones. Mostrar las opciones de cada persona sin inventar un control de acceso aparte. |
| Conexión local segura y protección de la sesión. | Probar los formularios con HTTPS siguiendo el README. El diseño sin conexión al servidor no demuestra que el formulario esté protegido. |
| Protección para que otra página no pueda enviar cambios en nombre de la persona. | Conservar la protección que traen los formularios. Retirar la desactivación usada para diseñar el acceso. |
| Página general de error y protección para que el navegador no conserve contenido privado. | Mantener el error general, revisar que se vea bien y aplicar también la protección de caché al acceso. Lucía comprobará el comportamiento real del navegador. |
| Registro de lo ocurrido, sin contraseñas ni valores privados. | Llamar a las funciones existentes. Las páginas no crean ni guardan esos eventos por su cuenta. |
| Consulta autorizada de ese registro, sin pantalla propia. | Dejar funcionando entrar y salir. Eso permitirá a Lucía probar la consulta en la misma ventana autenticada. No necesitas construir una pantalla de auditoría. |
| Base probada de pacientes, citas, horarios y baja protegida. | Conservarla al integrar. No significa que estén listas todas las pantallas de pacientes o agenda. No se añaden aquí pantallas de baja ni recuperación. |
| Pruebas locales de copia y recuperación. | No tienes una pantalla de respaldos que construir. No hay autorización para conectar servicios en la nube. |

### Pendientes de B00 que dependen de tus páginas

| Pendiente | Tu parte | Quién comprueba el resultado conjunto |
| --- | --- | --- |
| Entrar y salir de verdad desde el navegador. | Páginas conectadas y navegación clara. | Lucía. |
| Recibir una temporal y cambiarla con seguridad. | Mostrarla como se explica en B01 y dirigir al cambio propio. | Lucía. |
| Comprobar formularios protegidos y errores visibles. | Conservar las protecciones y preparar las respuestas de pantalla. | Lucía; tú aportas tus pruebas de PageModel. |
| No volver a ver datos privados al pulsar «atrás» después de salir. | Mantener la protección de las páginas privadas y del acceso. | Lucía en navegador. |
| Consultar el registro de auditoría desde una sesión autorizada. | Acceso y salida funcionales. Sin menú ni pantalla nueva de auditoría. | Lucía con un Superusuario activo y sin temporal pendiente. |
| Confirmar la política de baja con la usuaria. | Registrar lo que falte concretar y comunicarlo a Lucía. | Lucía decide cualquier cambio. La baja sigue rechazándose si hay citas activas; no se cancelan solas. |

**Otros pendientes de B00** —consolidar evidencia, respaldo real y decisión sobre la validación de la arquitectura— no son páginas que debas hacer. Se conservan en el [plan B00](TherapEase-etapa09-B00.md).

> El servidor ya tiene comprobaciones de seguridad. La seguridad de los formularios y del recorrido completo también necesita tus páginas y las pruebas de Lucía. B00 sigue abierto.

---

## 3. B01 · La puerta de entrada y las cuentas

### Funciones que ya puedes conectar

**Todas las funciones de esta tabla están en main.** Las páginas correspondientes no están entregadas en esa versión. Si ya avanzaste en tu rama, se revisará ese avance al preparar tu entrega.

| Función | Qué hace ya el servidor | Qué falta de tu lado |
| --- | --- | --- |
| **Entrar** | Comprueba usuario y contraseña, aplica el bloqueo y abre la sesión solo si pudo guardar lo necesario. | Formulario de acceso, mensaje seguro y destino después de entrar. |
| **Salir** | Cierra la sesión y quita validez a las demás sesiones de esa cuenta en su siguiente solicitud. | Botón de salir y regreso al acceso. También debe funcionar con una temporal pendiente. |
| **Cambiar mi contraseña** | Comprueba la actual cuando corresponde y guarda la nueva. Mantiene esta sesión y cierra las demás. | Formulario, confirmación de la nueva y navegación. Con temporal pendiente no se pide la actual otra vez. |
| **Ver mis datos** | Devuelve nombre, correo y situación de la propia cuenta. Con temporal pendiente solo sirve de apoyo al cambio de contraseña. | Usarlos donde corresponda. No habilita editar la cuenta propia ni ver datos ajenos. |
| **Buscar y listar usuarios** | Solo al Superusuario autorizado. Incluye cuentas desactivadas y devuelve hasta 100 resultados. | Búsqueda, lista vacía, cuentas desactivadas identificadas y acceso al detalle. Si llegan 100, sugerir afinar la búsqueda. |
| **Abrir una cuenta** | Devuelve sus datos y correo solo al Superusuario autorizado. | Vista de detalle y aviso si la cuenta no se encuentra. El correo no viene en la lista. |
| **Crear una cuenta** | Valida los datos y entrega una contraseña temporal una vez. | Formulario y presentación segura de la temporal. No hay registro público de usuarios. |
| **Corregir nombre y correo** | Guarda solo si hubo cambios. No cierra las sesiones. Puede corregirse una cuenta desactivada. | Cargar ambos datos actuales y enviar los dos. Dejar el correo vacío lo quita. Avisar cuando no hubo cambios. |
| **Cambiar el rol** | Solo acepta Usuaria o Superusuario. Retira las sesiones anteriores y protege al último Superusuario activo. | Selección del rol, confirmación y aviso. Si la persona cambia su propio rol, también pierde su sesión actual. |
| **Desactivar una cuenta** | Impide entrar y retira sus sesiones. Protege al último Superusuario activo. | Confirmación y resultado. No es dar de baja a un paciente. No se ofrece reactivar. |
| **Restablecer una contraseña ajena** | Entrega una temporal nueva, invalida la anterior y retira sesiones. Conserva un bloqueo vigente. | Recordar la verificación de identidad, confirmar la acción y mostrar la temporal con cuidado. No reenviar sola la operación. |

### Qué está probado y qué espera tus páginas

| Parte | Comprobación disponible | Qué falta |
| --- | --- | --- |
| Servidor, permisos, sesiones y contraseñas. | QA de Lucía incorporado en PR #8, revisado por Miguel. Incluye rechazos, revocación de sesiones, guardado y fallos. | Comprobar esos recorridos mediante tus formularios y en navegador. |
| Registro de entradas, salidas y bloqueos. | Servidor incorporado en PR #9 y diez pruebas adicionales de Lucía en #10, revisadas por Miguel. | Recorrido visible con tus páginas y consulta de auditoría desde la sesión autorizada. |
| Cada página y su formulario. | El servidor y su contrato permiten comenzar. | Tus pruebas propias en la entrega de páginas; las de servidor no las sustituyen. |
| Seguridad del conjunto. | Hay evidencia de la base B00 y del servidor B01 en sus planes. | Lucía prueba los formularios y el navegador. No se afirma que todo el sistema ya esté validado. |

### Quién puede ver cada cosa

| Persona o situación | Qué puede usar en B01 |
| --- | --- |
| Sin sesión | Acceso y mensaje de acceso denegado. Ningún dato privado. |
| Usuaria con sesión válida | Cambiar su propia contraseña y salir. No administrar usuarios ni consultar auditoría. |
| Superusuario activo, con sesión válida y sin temporal pendiente | Lo anterior y todas las funciones de administración de cuentas. |
| Cualquier persona con temporal pendiente | Cambiar su contraseña y salir. Los datos propios solo apoyan ese cambio. Sin acceso a otras funciones. |
| Cuenta desactivada o sesión vencida, alterada o revocada | No puede consultar ni cambiar datos protegidos. |

El menú debe acompañar esos permisos. Con temporal pendiente, dejar solo cambio de contraseña y salida. No activar enlaces de funciones futuras.

### Datos y reglas de los formularios

| Dato o regla | Qué debe quedar claro |
| --- | --- |
| Nombre de usuario | Obligatorio, hasta 50 caracteres y con los caracteres permitidos del contrato. No se cambia después de crear la cuenta. Los duplicados no se distinguen por mayúsculas. |
| Nombre completo | Obligatorio al crear o corregir la cuenta; entre 2 y 150 caracteres. |
| Correo | Opcional, hasta 254 caracteres y con formato válido. No necesita ser único; no sirve para entrar ni para recuperar o enviar contraseñas. |
| Rol | Solo Usuaria o Superusuario. No se crean roles nuevos desde la página. |
| Contraseña nueva | Mínimo 12 caracteres. No exigir combinaciones de mayúsculas, números o símbolos. La página comprueba que la confirmación coincida. |
| Error de formulario | Indicar qué corregir y conservar los datos permitidos. Nunca volver a rellenar una contraseña. |
| Sesión | Vence tras 30 minutos sin actividad. El uso del sistema renueva ese tiempo. No se programa un reloj de sesión distinto desde la página. |
| Intentos fallidos | Cinco fallos bloquean 15 minutos. No mostrar cuántos quedan ni revelar si la cuenta existe o está bloqueada. |

### Entrega de una contraseña temporal

| Paso | Qué hacer |
| --- | --- |
| 1. Verificar | Antes de restablecer, recordar que se verifica la identidad en persona o con una llamada a un número ya conocido, por un canal distinto al de entrega. Un mensaje solo no basta. |
| 2. Mostrar una vez | Mostrar la temporal solo en la respuesta que confirma crear o restablecer. No enviarla por correo ni guardarla en otra página, dirección, campo oculto, registro o captura. |
| 3. Entregar | La persona autorizada la entrega por el canal separado aprobado. No se agrega un servicio de envío. |
| 4. Cambiar | Quien entra con ella pasa directamente a cambiarla. No puede usar el resto del sistema mientras siga pendiente. |
| 5. Si se pierde | No se recupera ni se reenvía automáticamente. Se comprueba el resultado y, si hace falta, el Superusuario realiza un nuevo restablecimiento que invalida la anterior. |

Para las pruebas actuales no hay vencimiento adicional de la temporal. La temporal de 24 horas y el segundo código de acceso quedan para antes de datos reales; no se agregan en esta entrega.

### Mensajes y situaciones que también te corresponden

| Situación | Qué debe pasar en la página |
| --- | --- |
| Entrada rechazada | Mostrar siempre «Usuario o contraseña incorrectos.», sin distinguir el motivo. |
| Fallo al guardar el evento de entrada | Mantener el error general. No presentarlo como una contraseña incorrecta: no se abrió sesión. |
| Cuenta sin permiso | No mostrar datos. Dar una salida clara, sin mandar de una página a otra indefinidamente. Con temporal, dirigir al cambio propio. |
| El servidor rechaza un cambio | Mostrar el motivo permitido, sin anunciar éxito. |
| No se sabe si se guardó | Pedir revisar el resultado antes de repetir. No reenviar automáticamente ni asegurar que no se guardó. En un restablecimiento, un nuevo intento genera otra temporal. |
| El servidor confirma | Mostrar la confirmación o continuar según corresponda. Los eventos ya los guarda el servidor. |
| Falló registrar la salida o el bloqueo | La sesión queda cerrada o la cuenta bloqueada. La página no intenta deshacer esa protección. |
| El destino después de entrar viene de una dirección recibida | Aceptar solo destinos dentro de TherapEase. Nunca enviar a una página externa. |

---

## 4. Por dónde comenzar y qué revisar del diseño anterior

| Orden sugerido | Entrega de Dulce | Apoyo disponible |
| --- | --- | --- |
| 1 | Actualizar su rama con main conservando su trabajo y preparar el entorno local. | README sección 5; Miguel ayuda con el entorno. |
| 2 | Conectar acceso, cambio con temporal y salida juntos. | Ya existen las tres funciones. El primer Superusuario de prueba se crea con el procedimiento local del README, sin pantalla nueva. |
| 3 | Conectar cambio ordinario de contraseña, acceso denegado y menú según la sesión. | El mismo servidor y el contrato B01. |
| 4 | Conectar lista y detalle de usuarios, creación, corrección, rol, desactivación y restablecimiento. | Funciones y reglas ya entregadas. |
| 5 | Añadir sus pruebas propias y entregar el código para revisión. | Plan de pruebas; Miguel o Lucía revisa según la autoría y las reglas. |
| 6 | Dar a Lucía las páginas conectadas y explicar cómo recorrerlas. | Lucía prueba formularios, seguridad y navegador; Miguel corrige fallos del servidor si aparecen. |

**Observaciones ya registradas del diseño anterior:** son puntos por comprobar, no una afirmación sobre cambios más recientes de tu rama.

| Qué revisar | Qué resolver |
| --- | --- |
| Acceso diseñado sin inicio de sesión real. | Conectar el formulario en la ruta de acceso acordada. |
| Protección del formulario desactivada para diseñar. | Volver a activarla al conectar. |
| Diseño compartido y página de error. | Que ambos usen una plantilla existente y se vean bien. |
| Página inicial retirada en ese diseño. | Definir qué se ve al abrir el sistema y después de entrar. |
| Fuente DM Sans. | Acompañar el archivo de la fuente con su licencia. |
| Pantallas y presentación. | Dar visto bueno a 08C sección 3 antes de las páginas afectadas. Identificar la versión del PDF y confirmar con la usuaria los detalles que falten, incluido el recurso del logotipo. |

Las observaciones están identificadas en [B01 sección 4.2](TherapEase-etapa09-B01.md#42-cómo-se-conectarán-el-servidor-y-las-páginas). No se modificó la rama de Dulce en esta preparación.

## 5. Pruebas que te corresponden en estas páginas

**Cada página lleva sus pruebas propias en el mismo PR.** Las pruebas de Miguel y Lucía ayudan, pero no prueban por sí solas tus formularios.

| Qué comprobar | Resultado esperado | Referencia |
| --- | --- | --- |
| Faltan datos o la confirmación no coincide. | Avisar antes de enviar un cambio inválido. | D-01; CU11. |
| El servidor rechaza. | Mostrar el rechazo sin éxito falso. | D-03. |
| Hay un fallo inesperado o se pierde la respuesta. | Mantener el error previsto; no repetir sola ni afirmar un guardado que no se confirmó. | D-03; contrato sección 1.3. |
| El servidor confirma. | Mostrar éxito solo después de esa respuesta. | D-04. |
| El formulario llama a las funciones entregadas. | La página no lee ni guarda directamente en la base de datos. | D-05. |
| Cambio con temporal y cambio ordinario. | Sin contraseña actual en el primero; con ella en el segundo. | D-06. |
| Acceso rechazado. | Mismo mensaje para todos los motivos, incluido el quinto fallo. | D-07. |
| Menú y rutas. | Opciones por rol, salida disponible con temporal y sin ciclos al denegar acceso. Sin pantalla de auditoría. | D-08; contrato secciones 1.4 y 6. |
| Crear o restablecer entrega una temporal. | Mostrarla una vez, sin conservarla ni reenviar automáticamente. | Contrato sección 3. |
| Textos y protección de formularios. | Tratar lo recibido como texto y conservar la protección de los envíos. No aceptar campos extra para cambiar otra cuenta o rol. | D-13; contrato sección 1.5. |
| Mensajes y diseño. | Validación, resultado incierto y confirmaciones comprensibles; error general y estados vacíos visibles. | D-12. |

D-02 corresponde a conflictos cuando la función los devuelve; no añade un mecanismo nuevo a las cuentas de B01. Pacientes y citas tienen sus propios bloques. Lucía hará las comprobaciones reales de formularios y navegador, incluida la consulta de auditoría, cuando estén conectados.

---

## 6. Qué debe leer tu IA y en qué orden

> Pásale primero esta guía y las referencias compartidas. Tus asistentes no necesitan las notas personales de Lucía ni rutas de su computadora.

| Orden | Documento | Para qué leerlo |
| --- | --- | --- |
| 1 | [AGENTS, secciones 1–3](../../AGENTS.md) | Reglas obligatorias, quién hace qué, decisiones y límites vigentes. Prevalece sobre esperas históricas. |
| 2 | Esta guía y [contrato B01 para páginas](TherapEase-etapa09-B01-contrato-para-paginas.md) | Guía: alcance de tu entrega. Contrato: cómo se usa cada función y cómo se trata cada respuesta. Leer completo el contrato al conectar B01. |
| 3 | [Plan B00](TherapEase-etapa09-B00.md), secciones 2, 5, 7 y 8; [plan B01](TherapEase-etapa09-B01.md), secciones 2–8 | Responsabilidades, base disponible, observaciones del diseño y pendientes reales. |
| 4 | [08B: historias y casos](../requirements/TherapEase-etapa08-casos-historias.md) | HU01–HU03, HU15 y CU01, CU02, CU11: lo que la persona debe poder hacer y sus casos alternos. |
| 5 | [08C: modelos y pantallas](../requirements/TherapEase-etapa08-modelos-interfaces.md), sección 3 | Nombres, acciones, orden y navegación. Revisar junto con la versión del PDF que Lucía comparta; no asumir que el PDF está publicado en Git. |
| 6 | [08D: orden de bloques](TherapEase-etapa08-backlog-trazabilidad.md) y [entrevista](../requirements/TherapEase-09-entrevista-requisitos.md), sección 3 | Separar lo de ahora de lo futuro y aprovechar respuestas ya recibidas. No volver a preguntar lo ya resuelto. |
| 7 | [Plan de pruebas](../testing/TherapEase-09-plan-de-pruebas.md), sección 2 y casos B00/B01 de secciones 3–4 | Tus pruebas D-01–D-08, D-12 y D-13 aplicables; qué comprobará Lucía al integrar. |
| 8 | [README, sección 5](../../README.md#5-construir-probar-y-ejecutar) y [ADR](../architecture/adr/ADR-01-a-05-provisionales.md) | Preparación local. ADR-03, nota ADR-04 de B01 y ADR-06: límites al conectar páginas y reglas de sesión. No sustituirlos por una arquitectura nueva. |

### Referencias exactas para quien conecte el código

La explicación para Dulce está arriba. Esta tabla permite a su IA encontrar lo existente sin adivinar nombres.

| Parte | Referencia del repositorio |
| --- | --- |
| Base consultada | Main `edff8b81a95fb22358c890d0abd0daed77ca854b`. B01 incorporado en PR #7, #8, #9 y #10; base B00 y QA en los PR anteriores. No se ejecutaron pruebas nuevas al preparar esta guía. |
| Entrar y salir | [IServicioAcceso](https://github.com/LuciAguilar/Therapease/blob/main/src/Application/Identidad/Interfaces/Servicios/IServicioAcceso.cs): `IniciarSesionAsync`, `CerrarSesionAsync`. |
| Cambiar mi contraseña | [IServicioContrasena](https://github.com/LuciAguilar/Therapease/blob/main/src/Application/Identidad/Interfaces/Servicios/IServicioContrasena.cs): `CambiarContrasenaPropiaAsync`. |
| Mis datos | [IServicioCuentaPropia](https://github.com/LuciAguilar/Therapease/blob/main/src/Application/Identidad/Interfaces/Servicios/IServicioCuentaPropia.cs): `ObtenerMisDatosAsync`. |
| Administrar cuentas | [IServicioUsuarios](https://github.com/LuciAguilar/Therapease/blob/main/src/Application/Identidad/Interfaces/Servicios/IServicioUsuarios.cs): `ListarUsuariosAsync`, `ObtenerUsuarioAsync`, `CrearUsuarioAsync`, `ActualizarDatosDeCuentaAsync`, `CambiarRolAsync`, `DesactivarUsuarioAsync`, `RestablecerContrasenaAsync`. |
| Acceso y formularios | `/Acceso`, `/Acceso/Denegado`; campos de acceso `Entrada.NombreUsuario` y `Entrada.Contrasena`. Detalles, modelos de entrada, respuestas y protección en contrato secciones 1–5 y 8. |
| Autorización y salida | [Políticas](https://github.com/LuciAguilar/Therapease/blob/main/src/Web/Seguridad/PoliticasDeAutorizacion.cs). CU02: `AdministrarUsuarios`; CU11 y salida: `CambiarContrasenaPropia`; acceso y denegado: anónimos. Acceso lleva `no-store`; salida por `POST`; destino recibido solo local. |
| Errores y datos recibidos | Usar `RespuestaServicio<T>` y los mensajes acordados; no decidir comparando texto escrito a mano. Sin `Html.Raw` ni datos insertados con `innerHTML`; usar `textContent`. No usar acceso a datos, `UserManager`, `SignInManager` o `IGestorIdentidad` desde PageModel. |
| Presentar una temporal | Solo en la misma respuesta del `POST` autorizado (`return Page()`), sin redirigir ni pasarla por `TempData`, sesión, cookies, URL, campos ocultos o registros. Contrato sección 3. |
| Consulta de auditoría para QA | `GET /api/auditoria/eventos`, misma ventana autenticada del Superusuario activo y sin temporal. Parámetros y resultados en README sección 5. No requiere una pantalla nueva. |
| Estado del proceso | `/salud` comprueba que el proceso responde; no confirma conexión a PostgreSQL ni que las pantallas funcionen. |
| Diseño anterior revisado | `frontend/diseno`, `0dfd3a5419f24c5825b5362f9c3ef8374720ec6f`, según B01 sección 4.2. Comprobar cambios posteriores antes de dar una observación por resuelta. |

## 7. Límites y entrega

| Ahora | Después o con una nueva decisión |
| --- | --- |
| Conectar B00/B01 con los servicios existentes. Solo datos ficticios y entorno local. | Servidor y QA de pacientes B02 ya integrados. B03 solo prepara su propuesta; B04 sigue posterior. No necesitas esperar esos bloques para conectar B01. |
| Mantener el permiso provisional de baja y sus protecciones existentes. | Nuevas páginas de baja/recuperación B06 y directorio completo B07. |
| Usar las cuentas y reglas ya aprobadas. | Reactivar cuentas, cambiar nombre de usuario, recuperar por correo, nuevas formas de acceso o roles. |
| Mantener las decisiones y protecciones actuales. | Datos reales, nube, gastos, montos, «solo particular», cancelación automática y respaldo real. No se habilitan por esta guía. |

Antes de entregar:

- [ ] Páginas conectadas y recorrido explicado, incluidas temporal, denegación y salida.
- [ ] Diseño anterior revisado; versión de pantallas y detalles faltantes confirmados.
- [ ] Pruebas propias junto al código y comprobaciones automáticas correctas.
- [ ] Revisión por otra persona y aprobación final de Lucía. Nada se publica o fusiona por preparar esta guía.
- [ ] Explicar a Lucía qué puede probar, qué falta y si hace falta apoyo de Miguel.

> **Podemos avanzar en paralelo:** tú conectas B01, Miguel atiende las correcciones de sus funciones y Lucía prepara su QA. El bloque se cierra cuando el conjunto funciona, tiene evidencia y Lucía lo aprueba.
