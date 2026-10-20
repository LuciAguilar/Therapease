# TherapEase — 09/B01 · Acceso e identidad segura

**Plan compartido actualizado el 18-oct-2026, tras aprobar y fusionar PR #7.**

En B01 haremos que una persona pueda entrar a TherapEase con su usuario y contraseña, usar solo las funciones que le corresponden y cerrar su sesión de forma segura.

**El inicio de sesión —login— es la parte central.** B01 también incluye administrar cuentas y roles, cambiar y restablecer contraseñas, bloquear intentos fallidos y registrar estas operaciones. Corresponde a HU01–HU03 y HU15; CU01, CU02 y CU11. Esos códigos relacionan el trabajo con las historias y casos de uso ya aprobados.

Parte del servidor ya existe y fue probada en B00. Miguel debe aprovecharla y completar lo que falte. Dulce prepara las páginas y su funcionamiento. Lucía comprueba que todo trabaje junto.

**✅ Construcción B01 abierta. PR #7 aprobado y fusionado por Lucía el 18-oct**, en `922a4e32705c50fa7b559bd95ad19f86800bcba2`. Incluye usuarios, nombre/correo, permisos explícitos y CI. Los eventos y las páginas siguen pendientes. Lucía aprobó Entrega 1 de QA —Servidor, 86 correctas— y su commit local, sin push. Permisos también está aprobado: 50 de integración y 44 unitarias existentes correctas, commit local autorizado sin push. Sesiones aprobado por Lucía el 19-oct: 24 correctas, con dos comentarios aclarados y commit local autorizado sin push. Contraseñas y la revisión de Miguel siguen pendientes. B00, LCA y R04 siguen abiertos: solo ficticios, sin nube ni gastos.

✅ indica una decisión aprobada o un resultado anterior identificado. ⏳ indica trabajo pendiente. Los resultados B00 conservan su versión y fecha. Los resultados locales de QA B01 se incorporarán al avance compartido cuando terminen la auditoría y las aprobaciones; no se acreditan aquí.

## 1. Qué vamos a entregar y de dónde partimos

| Parte de B01 | Qué podrá hacer la persona | Referencia |
| --- | --- | --- |
| Iniciar y cerrar sesión | Entrar con su cuenta, recibir un mensaje seguro si no puede entrar y salir quitando el acceso a sus sesiones anteriores. | HU01; CU01 |
| Administrar usuarios | El Superusuario podrá crear cuentas personales, asignar roles permitidos, desactivar cuentas y restablecer contraseñas. Siempre debe quedar al menos un Superusuario activo. | HU02–HU03; CU02 |
| Cambiar la contraseña propia | Cambiar su contraseña; si recibió una temporal, sustituirla antes de usar otras funciones. | HU15; CU11 |
| Proteger y registrar estas acciones | Comprobar permisos, sesiones, bloqueo y guardado. Registrar lo ocurrido sin conservar contraseñas ni datos privados en los registros. | Q01–Q04, Q08, Q19 |

### Estado registrado al preparar el plan

Un **PR** es una solicitud para revisar cambios. El **SHA** identifica una versión exacta del código. PR #7 se fusionó el 18-oct a las 15:00 de Hermosillo. Las pruebas de B00 conservan sus fechas y resultados anteriores.

| Comprobación | Resultado registrado | Qué significa para B01 |
| --- | --- | --- |
| Rama principal `main` | ✅ `922a4e32705c50fa7b559bd95ad19f86800bcba2`, [squash de PR #7](https://github.com/LuciAguilar/Therapease/pull/7). | Incluye la entrega revisada de Miguel. |
| PR #5 de Miguel | ✅ Revisado por Lucía y fusionado. Último SHA del PR: `bce94091ba614e16f4cce3435697d8c58c2a31ff`; incorporado a `main` como `40785d2`. [PR #5](https://github.com/LuciAguilar/Therapease/pull/5). | Reutilizar sus funciones y pruebas. Fusión: 13-oct, 20:49 de Hermosillo; 14-oct, 03:49 UTC. |
| PR #6 de pruebas de Lucía | ✅ [Miguel aprobó](https://github.com/LuciAguilar/Therapease/pull/6#pullrequestreview-5528518803) `0db344a06a03edb054e3e94759f38a4c6f7e340c`; Lucía fusionó mediante squash el 15-oct, 08:06 de Hermosillo. [PR #6](https://github.com/LuciAguilar/Therapease/pull/6). | Incorporado a `main` en `ffb2494ad9da026f24a57826c45f6f530afb467b`. Código probado anterior `0aa32f2b4f387f02ded7b1100a74e869a0ad40d7`; no son pruebas nuevas. |
| Rama de coordinación del equipo | ✅ `revision/etapa09` actualizada desde `main` `922a4e3`, conservando cambios locales. | Sin rama nueva, commit ni push de QA. |
| Páginas y comprobaciones automáticas | ✅ CI revisada en PR #7. ⏳ Páginas funcionales y navegador. | La CI correcta no demuestra login desde una página. |
| Diseño de Dulce | ✅ Se inspeccionó solo en lectura `frontend/diseno`, SHA `0dfd3a5419f24c5825b5362f9c3ef8374720ec6f`. ⏳ Sin PR asociado ni revisión de entrega acreditada. | Es diseño, todavía sin inicio de sesión funcional. Observaciones en §4.2. |
| B00, LCA y R04 | ⏳ Continúan abiertos. | B01 no cierra sus pendientes de pruebas, revisión o datos reales. |

### Entrega de Miguel incorporada — PR #7

✅ Propuesta aprobada el 17-oct; precisiones confirmadas el 18-oct. PR #7 aprobado y fusionado en `922a4e3`, con versión revisada `a2047ecd9574627798bfbeb051277767865b76fe`. **386 unitarias y 205 de integración correctas** en la revisión del 18-oct; las 30 de arquitectura están incluidas en las 205. [CI revisada](https://github.com/LuciAguilar/Therapease/actions/runs/39152597181): cuatro trabajos correctos. Son resultados de esa revisión, no pruebas ejecutadas de nuevo por actualizar este plan.

**El seguimiento del equipo está en §2.** Se marca cada entrega comprobada; completar la parte de Miguel no da por hechas las páginas ni el QA propio de Lucía. Sus pruebas antiguas se retiraron de PR #7 y se entregarán en un PR aparte, revisado por Miguel.

### Documentos que guían este trabajo

| Documento | Para qué lo usamos |
| --- | --- |
| [AGENTS](../../AGENTS.md), [guía del equipo](../../README.md) y [README técnico](https://github.com/LuciAguilar/Therapease/blob/main/README.md) | Reglas, organización del código y ejecución local. Las decisiones posteriores aprobadas prevalecen sobre pendientes antiguos del README. |
| ESTADO (contexto local de Lucía), REVISIONES (contexto local de Lucía) y prompt maestro (contexto local de Lucía) | Decisiones y resultados anteriores. |
| [08B: historias y casos de uso](../requirements/TherapEase-etapa08-casos-historias.md), [08C: modelos y pantallas](../requirements/TherapEase-etapa08-modelos-interfaces.md) y [08D: trabajo previsto](TherapEase-etapa08-backlog-trazabilidad.md) | Qué debe hacer el sistema, qué pasa si algo falla y qué corresponde a cada bloque. |
| [Plan de pruebas](../testing/TherapEase-09-plan-de-pruebas.md) y [requisitos de arquitectura](../requirements/TherapEase-requisitos-arquitectonicos.md) | Qué comprobar y cómo identificar la evidencia. No se inventan códigos de pruebas o requisitos. |
| [ADR-01…05](../architecture/adr/ADR-01-a-05-provisionales.md) y [ADR-06](../architecture/adr/ADR-06-monorepo.md) | Decisiones técnicas aprobadas; sus notas posteriores sustituyen pendientes anteriores. |

El checklist personal de Lucía complementa este documento y permanece fuera del repositorio compartido.

## 2. Qué hará cada persona

Miguel se encarga del **servidor**: código que comprueba usuarios, contraseñas, permisos y cambios en la base de datos. Dulce se encarga de **páginas y formularios**, incluidos los PageModel: el código que recibe lo escrito por la persona y llama al servidor. Lucía dirige las pruebas de conjunto y da la aprobación final.

### Trabajo completo de Miguel en B01

| Estado | Trabajo de Miguel | Entregado o pendiente |
| --- | --- | --- |
| ✅ | Revisar B00 y presentar la propuesta | Lista de lo existente y propuesta conjunta, aprobada el 17-oct. [Propuesta](TherapEase-etapa09-B01-propuesta.md). |
| ✅ | Lista y ficha de usuarios | `ListarUsuariosAsync` y `ObtenerUsuarioAsync`, solo para Superusuario autorizado. Incorporadas en PR #7. |
| ✅ | Nombre completo y correo | Alta, edición, consulta propia y migración. Incorporados en PR #7; correo no sirve para entrar ni recuperar contraseñas. |
| ✅ | Permisos explícitos | Tres permisos enumerados por rol, prueba que exige decidir cada permiso nuevo y denegación de valores desconocidos. PR #7. |
| ✅ | Entregar funciones a Dulce | [Contrato para páginas](TherapEase-etapa09-B01-contrato-para-paginas.md): parámetros, respuestas y errores. PageModel siguen con Dulce. |
| ✅ | Unitarias y comprobaciones de GitHub | Unitarias propias y cuatro trabajos de CI revisados en PR #7. Nuevos cambios siguen llevando sus unitarias. |
| ⏳ | Eventos de acceso, salida y bloqueo | Próximo PR. Cierre forzado queda en el evento de la operación que lo causa. Excepción de salida/bloqueo en AGENTS §2.3; falta confirmar el mecanismo de registro técnico antes de programarlo. |
| ⏳ | Alinear sus documentos | En el PR de eventos, actualizar propuesta y contrato con las cuatro precisiones del 18-oct. No quedan pendientes de decisión esas cuatro. |
| ⏳ | Integrar las páginas y corregir fallos propios | Mantener acceso, salida, bloqueo, usuarios y contraseñas conforme a las reglas; apoyar a Dulce cuando conecte sus páginas. Reutilizar B00. |
| ⏳ | Revisar el QA de Lucía | Revisar su PR de servidor, permisos, sesiones y contraseñas cuando esté listo. Lucía conserva la aprobación final. |


**Miguel no necesita pedir permiso por cada clase, método o prueba.** Dentro de las reglas aprobadas elige sus herramientas, organiza su código y ordena sus tareas. Debe respetar AGENTS y trabajar sobre la base actualizada de `main`, conservando los cambios locales. PR #6 ya está fusionado. Las propuestas de §3.2 ya aprobadas no se vuelven a preguntar. Las precisiones todavía pendientes, propuestas nuevas y cambios de alcance, tecnología o reglas sí necesitan decisión de Lucía.

### Trabajo de Dulce y Lucía que sigue pendiente

| Estado | Persona | Qué debe completar |
| --- | --- | --- |
| ⏳ | Dulce | PageModel y sus unitarias: acceso, salida, usuarios y contraseña propia. |
| ⏳ | Dulce | Formularios con protección contra envíos ajenos, mensajes, navegación, temporales seguras y conexión con las funciones de Miguel. |
| ⏳ | Dulce | Resolver las observaciones de diseño de §4.2 en su PR; revisión distinta y aprobación de Lucía. |
| ✅ / ⏳ | Lucía | ✅ Entrega 1 —Servidor— aprobada con 86 correctas y commit local autorizado. ✅ Entrega 2 —Permisos— aprobada con ajuste de comentario y commit local autorizado. ✅ Entrega 3 —Sesiones— aprobada con ajuste de comentarios: 24 correctas; commit local autorizado, sin push. ⏳ Contraseñas y revisión de Miguel. Un PR para las cuatro al terminar. |
| ⏳ | Lucía | Probar formularios, navegador y Q03 al recibir las páginas; aprobar el cierre solo con evidencia y revisión humana. |


Cada autor escribe sus unitarias. Otra persona revisa sus cambios. **Miguel o Dulce revisa las pruebas de Lucía.** Las herramientas son libres; una revisión de IA no sustituye la humana.

## 3. Reglas aprobadas y decisiones que faltan

### 3.1. Reglas que Miguel puede usar sin volver a preguntarlas

| Estado | Tema | Qué debe pasar |
| --- | --- | --- |
| ✅ | Organización del proyecto | Conservar .NET 10, Razor Pages y AGENTS §1. Las páginas llaman a Application y no llegan directamente a la base de datos. Otra tecnología o forma de acceso requiere decisión aprobada. |
| ✅ | Cuentas personales | Una cuenta por persona. Roles `Usuaria` y `Superusuario`; no crear roles nuevos desde CU02. Para pruebas: una Usuaria y tres Superusuarios ficticios. No compartir ni fijar contraseñas en código. |
| ✅ | Contraseñas y bloqueo | Mínimo 12 caracteres, sin exigir combinaciones de letras, números y símbolos. Identity, la herramienta de .NET para usuarios, administra contraseñas. Cinco fallos bloquean 15 minutos; acertar o restablecer no quita el bloqueo vigente. |
| ✅ | Duración de sesión | Vence tras 30 minutos sin actividad; el uso del sistema renueva ese tiempo. Cada solicitud comprueba cuenta, sesión y permisos vigentes. |
| ✅ | Cierre de sesiones | Salir, desactivar o restablecer quita acceso a sesiones anteriores. Un cambio de rol se aplica en la siguiente solicitud. Cambio propio cierra las demás; la actual continúa solo si se actualiza y sigue válida. |
| ✅ | Protección del navegador | HTTPS y cookie de sesión `HttpOnly`, `Secure`, `SameSite=Lax`: protección frente a lectura desde JavaScript, envío por conexión segura y restricciones de envío entre sitios. |
| ✅ | Primer Superusuario y recuperación | Reutilizar `--crear-superusuario` y `--restablecer-superusuario` desde la computadora del operador. Temporal solo en consola, sin registros ni correo. |
| ✅ | Acceso con temporal | Tras comprobarla al entrar, permitir CU11 y la consulta de datos propios que lo apoya, sin otras funciones. En ese primer cambio pedir solo la nueva contraseña. En cambio ordinario pedir actual y nueva. |
| ✅ | Entrega o pérdida de temporal | Procedimiento §8, aprobado el 12-oct: mostrar una vez, canal separado y sin repetir automáticamente una operación si se pierde su respuesta. |
| ✅ | Vencimiento de temporal | La opción A registrada para B00 ficticio no añade vencimiento. La opción B de **24 horas es obligatoria antes de datos reales**, con fecha de emisión y pruebas. Este documento no autoriza implementarla ahora ni usar A con datos reales. |
| ✅ | Guardado y auditoría | Cambio y evento se guardan juntos: ambos o ninguno. Éxito solo después de confirmar. Sin contraseñas, sus hashes —valores usados para comprobarlas—, cookies, contactos, datos clínicos ni conexiones en registros. La app inserta eventos, pero no los modifica/borra ni cambia el esquema de la base; migrador separado. |
| ✅ | Errores y formularios | Mismo mensaje seguro ante usuario desconocido, contraseña incorrecta o bloqueo. Proteger contra envíos desde otros sitios. Error 500 fijo, sin consultar Identity ni la base de datos; no invitar a repetir un guardado incierto. |
| ✅ | Cabeceras de seguridad | Conservar AGENTS §2.5. La página de acceso, respuestas con temporal y contenido privado no deben quedar guardados en caché; no aplicar esa prohibición a todos los archivos estáticos. Completar las restricciones de recursos de las páginas —CSP— al tenerlas; decidir la duración de la protección que obliga a usar HTTPS —HSTS— al desplegar con autorización. |
| ✅ | Consulta Q03 | Superusuario activo, sin temporal, inicia sesión en navegador y abre la URL de consulta en esa ventana. No crear pantalla de auditoría ni script B para evitar ese acceso. |

### 3.2. Propuestas aprobadas y trabajo que falta

Lucía aprobó la propuesta de Miguel el 17-oct. El PR #7 entregó lista/ficha, nombre/correo y permisos explícitos. Los eventos conservan su entrega separada.

| Estado | Asunto | Decisión y siguiente paso |
| --- | --- | --- |
| ✅ | Elegir usuario en CU02 | Lista de hasta 100, búsqueda por nombre de usuario o completo, incluye inactivos; correo solo en ficha individual para Superusuario autorizado. |
| ✅ / ⏳ | Acceso, salida y bloqueo | Alcance aprobado el 17-oct; todavía sin programar. `InicioSesion`, `CierreSesion`, `BloqueoAcceso`; sin IP ni nombre escrito. Cierre forzado en el evento que lo causa. Confirmar el registro técnico antes del PR de eventos. |
| ✅ | Nombre completo y correo | Nombre obligatorio al crear desde CU02, de 2 a 150 caracteres; correo opcional. Solo Superusuario edita; cada persona consulta los suyos. Editar no cierra sesiones. Sin recuperación por correo. |
| ✅ | Permisos del Superusuario | `AdministrarUsuarios`, `ConsultarAuditoria` y `DarDeBajaPacientes`, escritos uno por uno; ningún permiso nuevo por esta entrega. |

**Cuatro precisiones confirmadas el 18-oct, AGENTS §3:** ficha con correo solo para Superusuario activo, con sesión válida y sin temporal; dominio sin puntos iniciales, finales o repetidos; evento solo con nombres de campos cambiados y ninguno si no hubo cambios; datos propios con temporal únicamente como apoyo a CU11. Miguel las reflejará en sus documentos en el PR de eventos.


Dulce concreta campos, mensajes y destino después de entrar o cambiar contraseña, con Miguel y Lucía. B01 debe poder probarse sin esperar pacientes o agenda. Montos, «solo particular» y cancelación automática al dar de baja pacientes siguen fuera de B01 y sin autorizar.

## 4. Qué existe y qué falta completar

### 4.1. Trabajo de B00 que se aprovecha

| Estado | Qué existe | Prueba anterior identificada | Qué falta en B01 |
| --- | --- | --- | --- |
| ✅ | Funciones del servidor de acceso, usuarios, contraseña propia y primer Superusuario. | PR #5 `bce9409`: compilación correcta, 258 unitarias y 182 pruebas con servidor/base correctas. [B00 §4.10](TherapEase-etapa09-B00.md#410-revisión-y-ampliación-qa--13-oct-2026). | Conectar páginas y completar casos no comprobados. No escribir todo desde cero. |
| ✅ | Bloqueo, temporal, cambio propio, salida, roles, desactivación y último Superusuario. | [B00 §4.1](TherapEase-etapa09-B00.md#41-identity-en-servidor--repetido-el-08-oct). | Revisar combinaciones pendientes y probar desde páginas. Los resultados anteriores cubren casos concretos del servidor. |
| ✅ | Cambio/evento juntos y fallos antes/después de confirmar. | 16 casos de [B00 §4.7](TherapEase-etapa09-B00.md#47-q08--interrupciones-y-reintentos-de-identity-10-oct); PR #4 revisado/fusionado. | Probar qué ve la persona al perder la respuesta de una página; antes se simuló dentro del servidor. |
| ✅ | Separación del código y consulta Q03. | 30/30 y 51/51, respectivamente, sobre PR #5. [B00 §4.9](TherapEase-etapa09-B00.md#49-revisión-del-pr-5-y-repetición-conjunta--11-oct-2026). | Comprobar páginas nuevas y acceso desde navegador. |
| ✅ | HTTPS local y claves de sesión en PostgreSQL conservadas tras reiniciar el contenedor. | [B00 §4.4–§4.5](TherapEase-etapa09-B00.md#44-horas-de-hermosillo-y-docker--ejecutado-el-0809-oct). | Probar con navegador; el cliente anterior no lo era. No demuestra nube. |
| ✅ / ⏳ | Cabeceras, error 500 seguro, contenido restaurado y mejoras de pruebas; paquetes Pkcs/Xml en 10.0.13. | PR #6: 258 unitarias y 205 de integración correctas, código `0aa32f2`. Revisado y fusionado mediante PR #6 el 15-oct. [B00 §4.11](TherapEase-etapa09-B00.md#411-correcciones-de-auditoría-del-pr-6--14-oct-2026). | Conservar correcciones; probar páginas y recursos. Los 205 incluyen otros bloques. La revisión de paquetes de entonces no garantiza seguridad futura de paquetes, SDK o imagen. |

Reutilizar estas pruebas y antecedentes. Después de un cambio, guardar nueva fecha, versión y resultados. No copiar TRX, conteos o capturas anteriores como evidencia nueva.

### 4.2. Cómo se conectarán el servidor y las páginas

Los nombres de código ayudan a Miguel y Dulce a localizar funciones existentes.

| Función | Código existente | Qué falta conectar o completar |
| --- | --- | --- |
| Entrar y salir | `IServicioAcceso`: `IniciarSesionAsync`, `CerrarSesionAsync`. | Dulce lo llama desde sus páginas, según el [contrato](TherapEase-etapa09-B01-contrato-para-paginas.md). El registro de acciones se aprobó el 17-oct; La excepción ya está en AGENTS §2.3; falta confirmar el mecanismo de registro técnico antes de programar. |
| Crear, cambiar rol, desactivar y restablecer | `IServicioUsuarios`. | Acciones reutilizadas. ✅ Lista, ficha, nombre/correo y edición de datos aprobados el 17-oct y programados (`ListarUsuariosAsync`, `ObtenerUsuarioAsync`, `ActualizarDatosDeCuentaAsync`; datos propios en `IServicioCuentaPropia`). Entregar la temporal de forma segura (§8). |
| Cambiar contraseña propia | `IServicioContrasena.CambiarContrasenaPropiaAsync`. | Conectar formulario y comprobar temporal y sesiones anteriores. |
| Preparar identidad y comprobar permisos | `IServicioArranqueDeIdentidad`, `IGestorIdentidad`, `IAutorizacion`, `ContrasenaTemporalEntregada`. | Reutilizar código de apoyo a través de Application. No mostrar ni registrar datos internos o contraseñas fuera de la respuesta autorizada. |
| Proteger acciones de página | Políticas `AdministrarUsuarios`, `ConsultarAuditoria`, `CambiarContrasenaPropia` y protección de formularios existente. | Aplicarlas y probar cada acción nueva. Tener configuración no demuestra formulario protegido. |

| Permiso actual | Superusuario | Usuaria | Condición |
| --- | --- | --- | --- |
| `AdministrarUsuarios` | ✅ Permitido | ✅ Denegado | Cuenta activa, sesión válida y sin temporal pendiente. |
| `ConsultarAuditoria` | ✅ Permitido | ✅ Denegado | Cuenta activa, sesión válida y sin temporal pendiente. |
| `DarDeBajaPacientes` | ✅ Permitido | ✅ Permitido | Provisional B00 del 11-oct. Se conserva; no añade trabajo de pacientes a B01. |

Son los tres permisos actuales, no permisos futuros. Los nuevos requieren la lista aprobada y las pruebas de §3.2. ✅ 17-oct: la matriz es explícita por rol y una prueba de completitud falla si se añade un permiso sin decidirlo.

#### Diseño de Dulce revisado solo en lectura

Rama `frontend/diseno`, SHA `0dfd3a5419f24c5825b5362f9c3ef8374720ec6f`; parte de `709ff615e1f1add0b008deff57e567de719b241c`, anterior al PR #5. No se modificó, fusionó ni comentó. Sin PR asociado ni revisión de entrega acreditada.

| Estado | Qué se observó | Qué hará cada persona |
| --- | --- | --- |
| ✅ / ⏳ | `/Acceso` coincide con la configuración de sesión. Campos `Entrada.NombreUsuario` y `Entrada.Contrasena`. | Miguel y Dulce conservan esa correspondencia. Dulce escribe el PageModel; el diseño aún no inicia sesión. |
| ⏳ | `asp-antiforgery="false"` permite diseñar sin base de datos. | Miguel facilita usar el servidor local. Dulce quita la desactivación y conserva el código de protección del formulario; Lucía prueba S-16. |
| ⏳ | Base anterior al PR #5. | Al preparar su PR, Dulce incorpora cambios vigentes con el procedimiento del equipo, conservando correcciones de permisos, contraseñas, cabeceras y errores. |
| ⏳ | `_Layout` pasó a `_LayoutAcceso`, pero `_ViewStart` sigue pidiendo `_Layout`; la página de error usa esa selección. | Dulce corrige el diseño compartido y prueba la página de error. |
| ⏳ | Se eliminó `src/Web/Pages/Index.cshtml`. | Dulce define qué aparece al abrir la raíz y dónde continúa la persona después de entrar. |
| ⏳ | DM Sans sin licencia en el árbol inspeccionado. | Dulce acompaña la fuente con su licencia correspondiente. |

Fuentes: [página de acceso](https://github.com/LuciAguilar/Therapease/blob/0dfd3a5419f24c5825b5362f9c3ef8374720ec6f/src/Web/Pages/Login/Index.cshtml), [ViewStart](https://github.com/LuciAguilar/Therapease/blob/0dfd3a5419f24c5825b5362f9c3ef8374720ec6f/src/Web/Pages/_ViewStart.cshtml), [diseño compartido](https://github.com/LuciAguilar/Therapease/blob/0dfd3a5419f24c5825b5362f9c3ef8374720ec6f/src/Web/Pages/Shared/_LayoutAcceso.cshtml) y [archivos de la versión](https://github.com/LuciAguilar/Therapease/tree/0dfd3a5419f24c5825b5362f9c3ef8374720ec6f). Se atienden en el futuro PR de Dulce; no impiden preparar el servidor.

#### PostgreSQL local para usar el acceso y proteger los formularios

**Pasos documentados, no ejecutados en esta entrega.** Comandos en [README técnico §10 — Construir, probar y ejecutar](../../README.md#10-construir-probar-y-ejecutar).

1. Seguir el README desde la copia propia: versión de .NET indicada en `global.json`, Docker y datos ficticios. Crear PostgreSQL y sus usuarios solo si es instalación nueva. Preparar las tablas con el usuario migrador y ejecutar con el usuario app; son cuentas de la base de datos con permisos distintos. La aplicación no carga `.env` automáticamente.
2. En base ficticia nueva, crear el primer Superusuario con `--crear-superusuario`. Nombre ficticio y temporal solo en consola; obliga al cambio CU11. Si ya existe cuenta de prueba, usarla.
3. Usar perfil `https`, certificado local confiable y `https://localhost:7028/Acceso`. Data Protection, que protege la identificación de sesión, necesita claves en PostgreSQL. `/salud` comprueba el proceso, no la base.
4. Con el código que recibe formularios disponible, Dulce conserva su protección y Lucía comprueba envío válido y rechazo sin código de protección o con uno ajeno, S-16. La cookie de protección es Secure: HTTP de diseño no prueba el formulario final.
5. Retirar de la terminal la conexión al terminar. Sin secretos en Git, páginas, registros, capturas o resultados. Estos pasos no conectan nube ni modifican la rama de Dulce.

## 5. Qué debe funcionar para aceptar B01

### 5.1. Resultados que debe ver la persona

| Función | Resultado esperado | Referencias y estado |
| --- | --- | --- |
| Entrar y salir | Cuenta activa entra con rol actual. Sin sesión válida no ve datos. Salir quita acceso a otras sesiones en la siguiente solicitud, aunque después se repita la salida. | HU01/CU01; Q01–Q02/Q19; L-CU01-01–03. ✅ Base; ⏳ páginas/navegador. |
| Acceso incorrecto o bloqueado | Mismo mensaje para nombre desconocido, contraseña incorrecta y bloqueo. Quinto fallo bloquea 15 minutos; acertar bloqueada no permite entrar; al vencer se comprueba de nuevo. | CU01 A1–A4; S-01–S-03. ✅ Casos de servidor; ⏳ restantes y páginas. |
| Administrar cuentas | Solo Superusuario activo, sesión válida y sin temporal crea, cambia rol o desactiva. Rechaza duplicados y roles no permitidos. Conserva al último Superusuario aun con cambios simultáneos. Nombre/correo tras aprobar detalle. | HU02/CU02; M-01–M-03/M-05; L-CU02-01–05/07. ✅ Acciones; ⏳ consulta, campos y páginas. |
| Restablecer contraseña | Verificar identidad y entregar temporal una vez. Contraseña/sesiones anteriores dejan de servir; bloqueo vigente se conserva. Respuesta perdida según §8. | HU03/CU02; L-CU02-06; Q02–Q04/Q19. ✅ Procedimiento/base; ⏳ página y prueba completa. |
| Cambiar contraseña propia | Solo la del usuario de la sesión. Actual incorrecta o nueva de menos de 12 caracteres no cambia nada. Con temporal verificada pide solo nueva; ordinario pide ambas. Tras guardar cierra otras sesiones. | HU15/CU11; M-04; L-CU11-01–03. ✅ Servicio; ⏳ formulario/navegador. |
| Mantener sesión segura | Vence tras 30 minutos inactiva y se renueva al usarla. Rechaza cookie ausente, alterada o invalidada. Atrás tras salir no revela datos; no redirige a sitios externos. | CU01/CU02/CU11; S-04–S-10. ⏳ Comprobación completa con páginas. |
| Proteger formularios | Permisos y datos comprobados en servidor; rechazar campos extra para cambiar usuario o rol. Mostrar entradas como texto y consultar sin convertirlas en instrucciones. Proteger envío y errores sin secretos. | S1–S6; S-11–S-24/S-26–S-34. ⏳ Cada acción nueva, también acceso sin sesión. |
| Registrar y consultar | Un evento por cambio confirmado de usuario o contraseña: quién lo hizo, cuándo, cuenta y campos afectados, acción y qué ocurrió; sin valores privados. Nuevos hechos de acceso según detalle aprobado. Q03 solo con sesión y permiso vigentes. | Q03; L-Q03-01–04. ✅ Base/consulta; ⏳ hechos nuevos y navegador. |
| Entregar trabajo comprobable | Pruebas y comprobaciones automáticas correctas; otra persona revisa. Cada resultado identifica su versión de código. | Q15/Q16/Q18; A-01/A-06; S-33/S-34. ⏳ Evidencia B01. |

### 5.2. Qué debe pasar cuando algo falla

| Situación | Resultado esperado | Responsables / referencia |
| --- | --- | --- |
| Nombre desconocido, clave errónea o intentos simultáneos | No revelar si la cuenta existe ni evitar el bloqueo con intentos al mismo tiempo. | Miguel: unitarias; Lucía: servidor/navegador. CU01 A1–A3. |
| Cuenta desactivada, permiso retirado, sesión vencida o temporal pendiente | Denegar datos. Temporal conduce a CU11 sin mandar de una página a otra sin poder avanzar. Un fallo no se anuncia como éxito. | Miguel: servidor; Dulce: páginas; Lucía: conjunto. CU01 A4–A8; S1. |
| Duplicado, rol inválido, usuario inexistente o quitar al último Superusuario | Rechazar sin cambiar datos ni registrar éxito. Proteger cambios simultáneos. | Miguel y Lucía. CU02 A1–A4; S2–S3. |
| Sin identidad verificada, fallo al guardar/registrar o respuesta perdida | No entregar sin verificación ni anunciar cambio sin confirmarlo. Procedimiento §8 y comandos locales si ningún Superusuario puede entrar. | Miguel/Dulce preparan; Lucía prueba. CU02 A5–A8; S4–S5. |
| Clave propia incorrecta, nueva inválida o envío hacia otra cuenta | No cambiar contraseña ajena. Si no se guardó, conservar anterior. Sin secretos ni repetición automática de operación incierta. | Miguel: servidor; Dulce: formulario; Lucía: conjunto. CU11 A1–A5; S1/S4–S6. |
| Uso futuro con datos reales | Antes se necesita el segundo código de verificación TOTP y su recuperación, además de los demás controles. No se da por cumplido con B01 ficticio. | CU01 A9; HU-F06/CU-F14; R04. |

B01 no desarrolla pacientes, citas, pagos, bajas ni directorio. Desactivar una cuenta tampoco equivale a dar de baja un paciente, Q20. Cada función conserva su bloque.

## 6. Orden de trabajo y dependencias

| Cuándo | Persona | Qué puede avanzar | Qué debe esperar |
| --- | --- | --- | --- |
| ✅ Entregado en PR #7 | Miguel | Inventario, propuesta aprobada, usuarios, nombre/correo, permisos explícitos, funciones documentadas y CI. | Lo pendiente se sigue en §2; no rehacer lo entregado. |
| Ahora: preparación | Dulce | Concretar pantallas, campos, mensajes y navegación; coordinar uso del servidor y README. | No declarar diseño como login funcional. Datos nuevos requieren detalle aprobado. |
| Ahora: QA propio | Lucía | Propuesta y CI revisadas. Adaptar y probar servidor; continuar permisos, sesiones y contraseñas según su checklist. | Auditoría y aprobación antes de cada commit. |
| Siguiente PR, sin esperar páginas | Miguel | Eventos de acceso, salida y bloqueo con sus unitarias; alinear sus documentos con las cuatro precisiones. | Confirmar el mecanismo de registro técnico. La excepción de salida/bloqueo ya está en AGENTS §2.3. |
| Servidor disponible, sin páginas | Lucía | Probar cuentas, permisos, bloqueo, contraseñas, sesiones, cambios simultáneos y guardado con base. | Servidor solo no demuestra formulario protegido. |
| Funciones acordadas | Dulce | PageModel y unitarias; conectar acceso, salida, usuarios y cambio propio; formularios, mensajes y navegación. | No necesita esperar pacientes o agenda para terminar B01. |
| Código que recibe formularios disponible | Lucía | Enviar solicitudes reales; comprobar permisos, datos, protección y respuestas perdidas. | No requiere automatizar navegador todavía, pero sí acciones de página de Dulce. |
| Páginas disponibles | Lucía | Probar entrada/salida, dos sesiones, bloqueo, temporal, cambio propio, roles y Q03 en navegador. | Miguel o Dulce revisa sus pruebas. |
| Antes de cerrar B01 | Equipo; Lucía aprueba | Corregir fallos, revisar pruebas y comprobar resultados automáticos/manuales. | Cierre B00/LCA separado. |

Orden práctico: propuesta y PR #7 aprobados; rama actualizada. Continuar eventos y QA propio, conectar páginas cuando lleguen, probar todo y revisar antes de cerrar. Miguel puede completar el servidor mientras Dulce prepara páginas. No se fijan horas o fechas sin acordarlas con el equipo.

## 7. Qué pruebas y evidencia se necesitan

Las referencias ya existen en el [plan de pruebas](../testing/TherapEase-09-plan-de-pruebas.md). No son pruebas nuevas por aparecer aquí. Las filas permanecen ⏳ hasta ejecutar las comprobaciones, cerrar su auditoría y recibir las aprobaciones correspondientes. Después se actualizan el plan, el checklist y los demás documentos afectados.

| Estado | Qué se comprueba | Responsable | Referencias / evidencia |
| --- | --- | --- | --- |
| ⏳ | Aprovechar B00 y comprobar lo que falte. | Miguel con Lucía | M-01–M-05; L-CU02/L-CU11. Resultado esperado → prueba existente → comprobación necesaria. |
| ⏳ | Bloqueo, contador de fallos, su reinicio y cambios simultáneos. | Miguel: unitarias; Lucía: base de datos y servidor | S-01–S-03/S-08. Identificar intentos, resultados y duración; no generalizar a cantidades no probadas. |
| ⏳ | Permisos, temporal, último Superusuario, cambio propio y cierre de sesiones. | Miguel: unitarias; Lucía: conjunto | L-CU02-01–07/L-CU11-01–03. Permiso concedido/denegado, rol retirado y lista de permisos nuevos explícita. |
| ⏳ | Cambio/evento juntos, fallos y respuestas perdidas. | Miguel: código/unitarias; Lucía: conjunto | L-Q03-01–04; Q04/Q08. Leer desde otra conexión qué se guardó. Sin éxito antes de confirmar. |
| ⏳ | Páginas validan y muestran resultado correcto. | Dulce: unitarias propias | D-01–D-08 aplicables. Rechazo conocido y respuesta incierta distintos; temporal sin pedir actual otra vez. |
| ⏳ | Rechazar envíos sin permiso, campos extra o protección incorrecta. | Lucía; corrige el autor | S-11–S-16/S-19. Solicitudes reales a cada acción; configuración no basta. |
| ⏳ | Entradas, errores y registros sin ejecutar instrucciones ni revelar secretos. | Lucía; corrigen Miguel/Dulce | S-17–S-24/S-26–S-34 aplicables. Revisar texto, consultas y respuestas con datos ficticios rastreables; sin secretos en evidencia. |
| ⏳ | Dos navegadores: sesiones, inactividad, salida y contraseña. | Lucía | L-CU01-01–03; S-04–S-10; CU02/CU11. HTTPS, cookies reales, 30 min sin actividad, renovación, atrás y destinos permitidos. |
| ⏳ | Temporal y Q03 desde navegador. | Dulce: páginas; Lucía: prueba | L-CU02-01/06/07; L-Q03-03–04. §8; respuesta perdida antes/después de guardar. |
| ⏳ | Código separado correctamente y cambios de base revisables. | Miguel prepara; Lucía comprueba | A-01 y controles aplicables. Revisar SQL, efecto y cómo deshacer migración. Nueva tecnología/mecanismo requiere ADR y decisión; no afirmar restauración no ejecutada. |

### Comprobaciones automáticas de GitHub Actions — CI

Miguel configura y Lucía comprueba. Compilar, ejecutar pruebas aplicables por cambio y comprobar reglas de código, seguridad e integridad que puedan automatizarse. La revisión de paquetes debe fallar ante vulnerabilidad alta, S-33.

Para identificar las herramientas exactas, fijar acciones por SHA e imagen base por su huella `digest`, S-34. Registrar versión, enlace, resultados, duración y minutos usados, A-06. No asumir uso gratuito ilimitado ni autorizar gastos.

**✅ CI revisada y entregada en PR #7, fusionado el 18-oct.** Cuatro trabajos: compilación/unitarias/arquitectura; integración con PostgreSQL; paquetes, acciones e imágenes fijadas, archivos sensibles y migraciones; imagen sin administrador y con salud. [Ejecución revisada](https://github.com/LuciAguilar/Therapease/actions/runs/39152597181), sobre `a2047ec`: todos correctos, 386 unitarias y 205 de integración. Cada trabajo guarda duración y versión en su resumen para A-06; TRX durante siete días. La ejecución roja anterior queda como historia, con sus correcciones verificadas. Este check no acredita la futura CI del QA propio de Lucía ni de las páginas.

Cuando se incorporen pruebas de navegador con Playwright, guardar trazas solo ante fallo, siete días y sin secretos. Q16 completo —acceso, paciente, cita y pago— se exige desde B04. No añadir esas funciones para cerrar B01.

### Qué guardar de cada ejecución

Fecha/zona horaria, SHA completo y cambios locales, entorno, comando, pruebas correctas/fallidas/omitidas y archivos identificables —por ejemplo TRX y manifest—. Una omitida no cuenta como correcta. Otra persona revisa; Miguel o Dulce revisa pruebas de Lucía. Lucía revisa identidad, permisos, auditoría, secretos, dependencias, cambios de base y CI.

## 8. Cómo se entregan temporales y se consulta la auditoría

| Situación | Procedimiento aprobado |
| --- | --- |
| Verificar quién pide restablecer | En persona o llamada a un número previamente conocido. Canal distinto del de entrega; nunca comprobar solo por mensaje. |
| Entregar la temporal | CU02 la muestra una vez al Superusuario autorizado. `Cache-Control: no-store` impide guardar esa respuesta en caché. Entrega por canal separado, sin correo, URL, TempData, registros o auditoría con contraseñas. |
| Temporal o respuesta perdida | No repetir automáticamente ni mostrar anterior. Comprobar lo confirmado y hacer nuevo restablecimiento autorizado: invalida anterior y sesiones, conservando bloqueo. |
| Contar eventos tras un fallo | Dos si ambos cambios se guardaron. Uno si el primero se deshizo y solo el segundo quedó guardado. Un 500 no dice por sí solo qué quedó guardado. |
| Cambiar la temporal | Entrar por CU01, cambiarla en CU11 y cerrar demás sesiones. No mostrar ni capturar el secreto en evidencia. |
| Consultar Q03 | Superusuario activo, sin temporal, entra en navegador y abre consulta en esa ventana. URL `/api/auditoria/eventos`; `desde`/`hasta` son fechas/horas con zona, `tipoRegistro` admite solo nombres completos y `limite` va de 1 a 500. |
| Quitar permiso o salir con Q03 abierto | La siguiente consulta se deniega. Probar Usuaria, temporal, sesión vencida, desactivación y rol retirado. Sin pantalla ni script que evite iniciar sesión. |

Temporales y opción A de Q03 fueron aprobados el 12-oct. Falta completar su uso en páginas y probarlo. Los nuevos eventos de acceso, salida y bloqueo quedaron aprobados el 17-oct (el cierre forzado se queda dentro del evento que lo causa); la excepción está anotada en AGENTS §2.3; falta confirmar el registro técnico y programarlos.

## 9. Cuándo se cierra B01 y qué queda fuera

| Estado | Para cerrar B01 se necesita | Cómo se demuestra |
| --- | --- | --- |
| ⏳ | Funciones y errores de §5 satisfechos. | Resultados actuales de servidor, páginas y navegador. Servidor solo no cierra B01. |
| ⏳ | Seguridad y registros completos para B01. | Permisos, contraseñas, sesiones, formularios y errores; sin problemas de seguridad que impidan usar el bloque. |
| ✅ / ⏳ | Comprobaciones automáticas correctas y evidencia identificable. | ✅ Entrega de Miguel revisada en PR #7. ⏳ Ejecutarlas para las próximas entregas de QA y páginas. B00 conserva sus antecedentes. |
| ⏳ | Otra persona revisa y Lucía aprueba al final. | PR con unitarias del autor, pruebas de Lucía revisadas por Miguel/Dulce y observaciones resueltas. |
| ⏳ | Pendientes anteriores registrados sin darlos por resueltos aquí. | B00/LCA se deciden aparte; R04 abierto. |

| Límite o trabajo posterior | Qué se conserva |
| --- | --- |
| Rama y publicación | Publicación documental autorizada el 17-oct: nuevo commit en `revision/etapa09`, sin PR ni rama nueva. Las pruebas de Lucía esperan auditoría y aprobación antes de cualquier commit o publicación. Su checklist es solo local. |
| Otros bloques | Pacientes/citas/pago en B02–B04; directorio separado en B07. Montos, solo particular y baja automática sin autorizar. |
| Datos reales | Solo ficticios con R04 abierto. Antes de reales: temporal 24 h, TOTP y recuperación, más controles/decisiones aplicables. B01 no los acredita. |
| Nube y respaldo real | Sin Render, Neon, repo de respaldos, despliegue ni gastos. Se conserva ADR-05/AGENTS §2.8 para autorización posterior; no se implementa aquí. |

### Qué cambió la entrevista para B01

✅ Erika será la única usuaria operativa; las cuatro cuentas ficticias se conservan para pruebas. Pidió nombre completo/correo en su Usuario, dio conformidad general a pantallas y pidió logotipo en inicio.

✅ Miguel entregó nombre/correo en PR #7 según las decisiones del 17 y 18-oct. ⏳ Dulce concreta campos, mensajes, versión de pantalla y recurso del logotipo; Lucía aprueba lo que falte decidir. Correo no autoriza recuperación por email; visto bueno general no equivale a páginas funcionales o probadas.

La nota siguiente conserva el alcance histórico de la actualización de entrevista. La apertura de Construcción se mantiene y la espera de PR #6 terminó con su fusión del 15-oct.

> **Entrevista recibida y revisada el 14-oct-2026:** ✅ respuestas de la psicóloga registradas en [respuestas y ajustes](../requirements/TherapEase-09-entrevista-requisitos.md). ⏳ Montos, simplificación a solo particular y cancelación automática requieren decisión de alcance/mecanismo de Lucía; no están implementados por esta actualización. B00/LCA/R04 abiertos, preparación documental sin Construcción ni pruebas nuevas. Los resultados y aprobaciones anteriores se conservan como historia.
