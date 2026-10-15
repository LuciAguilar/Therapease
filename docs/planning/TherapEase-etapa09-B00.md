# TherapEase — 09/B00 · Plan y estado de validación

**Actualización documental: 14-oct-2026.**

> **B00/LCA conservan pendientes.** Lucía autoriza expresamente el 13-oct avanzar a Construcción/B01 con Miguel en otro chat mientras se completa QA de B00. No declara cerrada LCA. R04 sigue abierto: solo datos ficticios, sin nube.
>
> **Cómo leer las tablas:** ✅ = la tarea concreta tiene ejecución o decisión registrada; ⏳ = falta completarla o revisarla. Una prueba correcta no significa revisión humana, fusión ni cierre del bloque.

## 1. Estado y documentación

| Entrega | Estado registrado | Referencia |
| --- | --- | --- |
| T1: repositorio y carpetas base | ✅ Aprobado y publicado el 01-oct. | Base `174afe0b6764f2415427f28b04d4627ce4c33b16`. |
| PR #1: pasos 1–3 de Miguel | ✅ Corregido, aprobado y fusionado con Squash and merge. | [PR #1](https://github.com/LuciAguilar/Therapease/pull/1); main `41123f2d2c115a05ff1df944457195a7f671cc17`. |
| PR #3: documentación compartida | ✅ Fusionado el 06-oct. | [PR #3](https://github.com/LuciAguilar/Therapease/pull/3); main `8baf8fbc34890d4710c2b78e920f10b8d3e92cb3`. |
| PR #4: pruebas de Lucía | ✅ Revisado por Miguel y fusionado el 11-oct. | [PR #4](https://github.com/LuciAguilar/Therapease/pull/4); main `709ff615e1f1add0b008deff57e567de719b241c`. |
| PR #5: correcciones de Miguel | ✅ Corregido, comprobado y fusionado el 13-oct, main 40785d2. | [PR #5](https://github.com/LuciAguilar/Therapease/pull/5); §4.10 e [informe](../reviews/TherapEase-09-B00-revision-PR05.md). |
| PR #6: ampliación QA de Lucía | ✅ Publicado y listo para revisión; ⏳ revisión de Miguel y fusión. | [PR #6](https://github.com/LuciAguilar/Therapease/pull/6), revision/etapa09; código 4ca53c4, 258 unitarias/205 integración. |
| Arquitectura | ✅ 30/30 correctas sobre PR #5; correcciones de Web y coordinador implementadas. | §4.9; páginas funcionales y aprobación del PR #5 siguen separadas. |
| Horas y Docker del 08–09-oct | ✅ 12 correctas; reinicio real y claves/sesión conservadas. | §4.4; PR #4 revisado/fusionado; repetido en §4.9. |
| HTTPS local del 10-oct | ✅ 12 correctas, 0 fallos/omitidas; TLS 1.3 negociado. | §4.5; PR #4 revisado/fusionado; cabeceras finales pendientes. |
| Q03 por API | ✅ 51/51 correctas sobre PR #5; cinco rechazos antes fallidos ya pasan. | §4.9; acceso/procedimiento final del operador pendiente. |
| Q08 de Identity del 10-oct | ✅ 16 correctas, 0 fallos/omitidas; ejecución local. | §4.7; auditoría de Claude APROBADO; PR #4 revisado/fusionado; formularios y demás servicios pendientes. |
| Errores/registros del 10-oct | ✅ 11 correctas; búsqueda acotada sin valores privados detectados. | §4.8; auditoría de Claude APROBADO; 500 de Identity vacío, corrección aprobada para PR #5; PR #4 revisado/fusionado. |
| Resultado conjunto del 08-oct | ✅ 50 correctas: 37 de Identity y 13 de recuperación; 0 fallos/omitidas, 19 s. | Release sin advertencias reportadas. Ejecución local, no CI. |

| Documento | Para qué sirve |
| --- | --- |
| [Guía / README](../../README.md) y [AGENTS](../../AGENTS.md) | Preparación y comandos; reglas compartidas en AGENTS. |
| [Informe PR01](../reviews/TherapEase-09-B00-revision-PR01.md) | Ajustes ya comprobados y mejoras opcionales del PR #1. |
| [Plan de pruebas](../testing/TherapEase-09-plan-de-pruebas.md) | Casos y estados de toda 09; cada caso entra en su bloque. |
| [08B](../requirements/TherapEase-etapa08-casos-historias.md), [08C](../requirements/TherapEase-etapa08-modelos-interfaces.md), [08D](TherapEase-etapa08-backlog-trazabilidad.md) y [Q/R](../requirements/TherapEase-requisitos-arquitectonicos.md) | Alcance, alternos, modelos, orden propuesto y criterios. |
| [ADR-01…05](../architecture/adr/ADR-01-a-05-provisionales.md) y [ADR-06](../architecture/adr/ADR-06-monorepo.md) | Decisiones y notas posteriores; las propuestas no se convierten en decisiones por ejecutarlas en QA. |

## 2. Responsabilidades y forma de trabajo

| Persona | Implementa / elabora | Revisa o decide |
| --- | --- | --- |
| Miguel | Solución, proyectos, módulos, backend, persistencia, migraciones e Identity; unitarias propias. | Prueba de límites de Lucía; puede revisar sus demás pruebas. |
| Dulce | Frontend, PageModel y unitarias propias; valida producto con la usuaria. | Puede revisar las pruebas de Lucía; registra respuestas reales de producto. |
| Lucía | Estrategia QA, arquitectura, integración crítica, seguridad y evidencia. | Cambios críticos, aprobación final y decisión de LCA. |

> Las herramientas y el método son libres. Las reglas compartidas de AGENTS son obligatorias. Autor y revisor humanos deben ser personas distintas; Miguel o Dulce revisa las pruebas de Lucía.

## 3. Decisiones aprobadas, propuestas y pendientes

| Tema | Situación | Qué se mantiene / qué falta |
| --- | --- | --- |
| Identidad | Aprobado | Mínimo 12 caracteres sin composición; sesión 30 min sin actividad; cinco fallos bloquean 15 min; restablecer conserva el bloqueo. |
| Primer superusuario | Aprobado | Comando local del operador; temporal solo en su consola, nunca en registros del servidor. |
| PageModel | Aprobado | Dulce los implementa y escribe sus unitarias. |
| Paciente de baja y cita activa | Aprobado | El bloqueo PostgreSQL **se conserva**. Nunca debe quedar una cita activa de un paciente de baja. |
| Auditoría y directorio | Aprobado | Sin pantalla de auditoría; consulta técnica autorizada Q03. Directorio separado y posterior (B07), fuera de CU03: lista completa de vigentes por defecto y filtro autorizado de bajas. Decisión de Dulce del 08-oct; registrar y buscar desde Pacientes se mantiene en el primer avance de diciembre. |
| Repositorio público | Aprobado | Documentación seleccionada y datos ficticios. Secret Protection y Push protection activadas: captura aportada por Lucía el 12-oct (ambos botones Disable). Verifica configuración, no una prueba de bloqueo ni ausencia de secretos. |
| Destino de respaldo | Opción A aprobada | Repositorio privado aparte, artefacto privado 14 días; no crear ni configurar todavía. |
| Permiso de baja | Aprobado e implementado para Usuaria y Superusuario, provisional | Comprobado en bce9409 y 14 casos de QA. La política operativa de citas existentes espera a Dulce con la usuaria. |
| Cabeceras S-30 | Aprobadas el 11-oct, implementadas y comprobadas el 13-oct | Nueve casos HTTPS en §4.10. Faltan navegador, CSP de recursos y HSTS de despliegue. |
| Respaldo: cifrado, claves y validación | Aprobado el 10-oct, ADR-05 posterior | age con clave pública en GitHub; clave privada de Lucía en su gestor fuera de GitHub, copia de Miguel. Restaurar/comparar antes de cifrar; volcado solo en ejecutor. Simulacro mensual de Lucía; antigüedad >24 h no bloquea y genera incidencia. Implementación no autorizada. |

| Pendiente de producto | Responsable de obtener respuesta | Impacto |
| --- | --- | --- |
| Citas existentes al dar de baja al paciente: impedir o cancelar primero | Dulce con la usuaria | CU04; no retirar el bloqueo de la base ni cancelar automáticamente. |
| Citas contiguas y pago inicial «Pendiente» | Dulce con la usuaria | Reglas hoy provisionales; no validarlas por omisión. |
| Retirar un ámbito con citas, incluidas agendadas de baja recuperables | Dulce con la usuaria | Baja/recuperación coherente. |
| Corregir pago de una cita cancelada o de baja | Dulce con la usuaria | Permisos y alternos del pago. |
| Campos de paciente/cita/directorio, ámbitos, duplicados y menores/tutor | Dulce con la usuaria | Validar antes del frontend afectado; `NombreCompleto`/`Correo` no se presuponen necesarios. |
| Pantallas de 08C, navegación y resto de prioridades/corte B00–B04 para 04-dic | Dulce con la usuaria y equipo | Registrar y buscar pacientes en el primer avance ya se mantiene. Directorio aparte posterior decidido el 08-oct; los demás compromisos y campos siguen por validar. B05–B06 dependen de capacidad. |

## 4. Evidencia registrada y límites

### PR #1 — antecedente del 03–04-oct

Revisión original `e418010882f9827ece2a1500d28f27490b45c585`; corrección documental `b60eba42edeb2fa8cdbea08ad1a4e51b041c21b0`. Windows, SDK 10.0.401, Docker 29.8.1 Linux, PostgreSQL desechable/Testcontainers 4.15.0, xUnit 2.9.3.

| Comprobación | Resultado histórico | Límite actual |
| --- | --- | --- |
| Build de siete proyectos | 0 errores/advertencias, también en `b60eba4`. | No se repite por reorganizar este documento. |
| Unitarias de Miguel | 149 correctas en la revisión original; Miguel declaró resultado correcto en `b60eba4`. | No son parte del nuevo conjunto de 50 de Lucía. |
| Integración externa | 11 correctas: migraciones, permisos, cruces, versiones, conservación, Identity y atomicidad inicial. | Ya incorporadas y repetidas dentro de los 37; no sumar 11 otra vez. |
| Docker y HTTP | Imagen construida; UID 1654; salud 200, inicio 200 y auditoría sin sesión 401. | Salud comprueba el proceso, no la base ni acceso funcional; no demuestra HTTPS. |
| Límites y paquetes | Inspección manual histórica y consulta NuGet sin vulnerabilidades reportadas entonces. | La prueba automática detecta Web → MatrizDePermisos; enumeraciones/constantes permitidas por decisión posterior del 08-oct (§4.3). Búsqueda de paquetes acotada. |

### 4.1. Identity en servidor — repetido el 08-oct

| Comprobación de Lucía | Resultado | Qué no acredita |
| --- | --- | --- |
| Integración incorporada y ampliada | ✅ 37 casos, incluidos los 11 anteriores. | Revisión humana aún pendiente. |
| Hash, longitud, bloqueo y temporal | ✅ Servicios reales; expiración controlada. | No se esperaron 15 minutos reales. |
| Revocación, permisos, rol y último superusuario | ✅ Incluye cambios simultáneos observados en PostgreSQL. | No cubre todos los recorridos de páginas. |
| Auditoría de Identity | ✅ Cambio/evento y reversión ante fallo en operaciones probadas. | Faltan otras operaciones y fallos de respuesta. |
| Cookies y caducidad | ✅ Componentes reales, atributos y reloj controlado. | No equivale a navegador, renovación por actividad ni TLS; reinicio normal probado después en §4.4. |
| Secretos/configuración | ✅ Revisión acotada de archivos, historial, imagen histórica y registros. | No garantiza ausencia universal ni nueva construcción de imagen. |

[Evidencia de Identity](../testing/TherapEase-09-B00-evidencia-identidad.md): alcance, comandos y limitaciones. Comentarios conservados según el skill del proyecto.

### 4.2. Recuperación Q10–Q11 — corregida y repetida el 08-oct

| Comprobación de Lucía | Resultado | Límite |
| --- | --- | --- |
| Casos de recuperación | ✅ 13; con Identity son **50**, todos correctos, 0 fallos/omitidas. | Muestra pequeña, no carga objetivo. |
| Copia y restauración | ✅ `pg_dump` cifrado, SHA-256, origen detenido y otro PostgreSQL vacío. | Sin nube ni respaldo diario. |
| Datos restaurados | ✅ 13 tablas iguales; permisos, Identity y consulta autorizada HTTP 200. | No recorrido funcional completo ni navegador. |
| Tiempos | ✅ Respaldo 458 ms; recuperación **7,375 s** desde detección simulada; pérdida simulada 460,8682 ms. | No prueba cadencia real de 24 h ni carga Q12. |
| Errores | ✅ Copia alterada/truncada, clave incorrecta, volcado fallido y restauración conflictiva. | Clave efímera de QA; falta custodia duradera. |
| Correcciones de Claude | ✅ Comentarios pegados al código; no bloquear por antigüedad. Copia de 25 h restaurada, 13 tablas iguales y Q10 incumplido informado. | No cierra Q10–Q11 en operación. |

[Evidencia de recuperación](../testing/TherapEase-09-B00-evidencia-recuperacion.md): reproducción y límites. Las correcciones obligatorias están verificadas; revisión humana y aprobación/fusión siguen pendientes.

### 4.3. Arquitectura — regla aprobada y repetida el 08-oct

Lucía aprobó la excepción recomendada por Claude: **Web puede usar enumeraciones y constantes de Domain; entidades, reglas y servicios, solo mediante Application.** Registrada en AGENTS §1 y en la nota posterior de ADR-03/validación ADR-06. Se conservan los comentarios revisados por Claude.

| Comprobación | Resultado real | Alcance / límite |
| --- | --- | --- |
| Compilación Release y arquitectura con la regla aprobada | ✅ **30 ejecutadas: 28 correctas, 2 fallidas, 0 omitidas**, 220 ms. | Compila sin advertencias reportadas; ejecución local, sin revisión humana atribuida. |
| Regla de enumeraciones/constantes | ✅ Nueve casos adicionales aceptan enumeraciones/constantes y rechazan entidades/reglas o clases con lógica. | Las constantes no autorizan usar cualquier clase estática ni una clase que tenga métodos. |
| Referencias, capas internas, módulos y contratos | ✅ Referencias declaradas permitidas; sin ciclos detectados en los cuatro módulos actuales; un puerto con implementación por módulo. | Revisar referencias declaradas no sustituye revisar el código compilado. |
| Único acceso Web → Domain indebido detectado | ⏳ `ManejadorDeRequisitoDePermiso` llama a `MatrizDePermisos`. | Miguel debe utilizar `IAutorizacion` de Application. `Permiso` y `TipoRegistroAuditoria` ya están permitidos: no requieren cambio por este hallazgo. |
| Coordinador CU04 | ⏳ Falta el candidato mínimo en Application fuera de Pacientes/Citas. | Debe consultar citas activas antes de la baja. Luego falta probar la operación real y la concurrencia. |
| PageModel actual y controles del inspector | ✅ ErrorModel sin persistencia directa; controles de colecciones, métodos, async y ciclos indirectos correctos. | No acredita las páginas funcionales que aún debe implementar Dulce. |
| Controles negativos reales anteriores | ✅ Ciclo Pacientes → Citas y PageModel con repositorio detectados y retirados en la ejecución anterior. | No se repitieron las modificaciones temporales; se conserva su evidencia histórica. |
| Revisión | ⏳ Miguel revisa las pruebas de límites de Lucía. | Veredicto de Claude recibido; la implementación de estas correcciones aún no tiene revisión humana. |

**Ajustes pendientes — Miguel:**

| Lugar                                                               | Cambio a realizar                                                                                                                        | Comprobación necesaria                                                                                                                                              |
| ------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `src/Web/Seguridad/Manejadores/ManejadorDeRequisitoDePermiso.cs:14` | Pedir el permiso a `IAutorizacion.TienePermisoAsync`, sin llamar directamente a `MatrizDePermisos`.                                      | Permiso vigente, usuario activo y temporal restringida; sin cambiar permisos aprobados.                                                                             |
| `src/Web/Seguridad/ConfiguracionDeSeguridad.cs:46`                  | Al inyectar `IAutorizacion`, registrar el manejador por solicitud, compatible con ese servicio; hoy es Singleton.                        | Arranque y solicitudes sin mezclar servicios de distinta duración.                                                                                                  |
| `src/Application/`, fuera de Pacientes/Citas                        | Implementar el coordinador mínimo CU04 que consulta citas activas antes de dar de baja al paciente, con contratos públicos y sin ciclos. | Conservar protección PostgreSQL, concurrencia y cambio/evento atómicos. No cancelar automáticamente ni decidir la política de citas existentes pendiente con Dulce. |

**Reproducción:** desde la raíz del repo:

```powershell
dotnet test tests/TherapEase.IntegrationTests/TherapEase.IntegrationTests.csproj -c Release --filter FullyQualifiedName~ArquitecturaTests --logger "trx;LogFileName=b00-arquitectura.trx"
```

> **Salida actual 1 por los dos hallazgos reales.** Las pruebas no se omiten ni fuerzan un fallo artificial: comprueban las reglas y seguirán fallando hasta corregir el código. `ArquitecturaTests.cs` contiene los casos y `ArquitecturaRevision.cs` su lectura de proyectos/código compilado. Pruebas comentadas según el skill del proyecto.
>
> **Trazabilidad:** base de trabajo `2d088e2c4cc3e2c6f7dc279452713fd1bbf3bced`, misma rama `revision/etapa09`; pruebas reunidas en la entrega conjunta del PR #4. No cambia backend, migraciones, proyectos ni paquetes. Ejecución anterior: 21 casos, 19 correctos/2 fallidos; la revisión detectó que la regla era demasiado estricta para enumeraciones. La aprobación del 08-oct permite esa excepción; el resultado vigente es el de 30 casos. No se repitieron aquí las 50 pruebas de Identity/recuperación.
>
> **Límites:** mapa de Pacientes, Citas, Identidad y Auditoría; ampliar al agregar módulos. No detecta dependencias formadas por texto/reflexión ni el origen de constantes eliminadas al compilar. Reconoce contenedores estáticos con campos constantes literales, sin métodos ni inicialización. La ubicación de un candidato CU04 no demuestra sus llamadas funcionales. Excepción de composición solo para registro de Infrastructure en arranque/comando local; no autoriza repositorios o DbContext.

### 4.4. Horas de Hermosillo y Docker — ejecutado el 08–09-oct

| Comprobación | Resultado real | Alcance / límite |
| --- | --- | --- |
| Nueva ejecución Release | ✅ **12 correctas, 0 fallos/omitidas**, repetidas el 09-oct tras corregir limpieza; 26,85 s con preparación. | xUnit; PostgreSQL 17 desechable/Testcontainers 4.15.0. No se repitieron aquí las 50 de Identity/recuperación ni las 30 de arquitectura. |
| Hermosillo dentro de la imagen final | ✅ 3 zonas de proceso: UTC, Nueva York y Madrid; 6 fechas por zona. | Se ejecuta la DLL de Domain extraída de la imagen, con cambios horarios externos y cambio de año. La hora explícita de Hermosillo no depende de TZ. |
| API de eventos y fechas | ✅ 3 entradas inválidas rechazadas; Z, -07:00 y +02:00 devuelven el mismo evento. | Comprueba el contenido: inicio incluido y fin excluido. Fecha sola/hora sin zona no se aceptan como instante. Q03 completo y páginas siguen pendientes. |
| Cita guardada y recuperada | ✅ EF/Npgsql conservan inicio/fin y fecha local al consultar PostgreSQL con otras 3 zonas. | Cruce de año; no equivale a agendar/reprogramar desde PageModel. |
| Docker vigente | ✅ Dockerfile construido; arranque, /salud 200, HEALTHCHECK healthy, usuario no root y ruta sin sesión 401. | /salud indica proceso vivo; disponibilidad de base se demuestra aparte en las consultas autenticadas. |
| Reinicio y claves (S-31) | ✅ Reinicio real; misma cookie aceptada, evento correcto y claves PostgreSQL idénticas. | Sin archivos key-*.xml observados en /app, /home/app y /tmp. HTTP local con cookie enviada manualmente; no acredita TLS/navegador ni política tras incidente. |

**Reproducción:** Docker Desktop iniciado; desde la raíz del repo:

```powershell
dotnet test tests/TherapEase.IntegrationTests/TherapEase.IntegrationTests.csproj -c Release --filter FullyQualifiedName~HorasDockerTests --logger "trx;LogFileName=b00-horas-docker.trx"
```

[Casos de prueba](../../tests/TherapEase.IntegrationTests/HorasDockerTests.cs) y [preparación del entorno](../../tests/TherapEase.IntegrationTests/HorasDockerRevision.cs). La consola temporal de QA se genera en TestResults, no agrega un proyecto a la solución. El entorno actual usa host.docker.internal para comunicar la imagen con el PostgreSQL desechable de Docker Desktop.

| Trazabilidad | Dato |
| --- | --- |
| Código base | 2d088e2c4cc3e2c6f7dc279452713fd1bbf3bced; pruebas reunidas en revision/etapa09 para el PR #4. |
| Imagen final | sha256:104ff4c79b0242fb8ac62d138e644d8d17750074f33d0bf0502caae9d2f10c02 |
| Resultado conservado | horas-docker-corregido-09oct.trx; SHA-256 edda84c4e0c97e574baf65babd8b51a6222300f2af5903a7c4acae3b5da8b693. |
| Primer intento | 10 correctas y 2 fallidas por puerto automático reasignado al reiniciar. Se corrigió la preparación de QA, sin tocar producción, y se repitieron las 12. Se conserva el TRX inicial. |
| Corrección de Claude, 09-oct | DisposeAsync elimina también la imagen propia con docker rmi; PostgreSQL se libera incluso si falla esa limpieza. 12 pruebas repetidas correctamente. La evidencia permanece; no se atribuye revisión humana. |
| Revisión / límites | Revisión humana pendiente. Pruebas comentadas según el skill del proyecto. Contenedores e imagen de cada ejecución retirados al terminar; identificador conservado en imagen.json. Las dos imágenes anteriores también se retiraron el 09-oct. HTTPS local probado después (§4.5); páginas, arquitectura pendiente y decisión B00/LCA siguen abiertos. |

### 4.5. HTTPS local Q19 — ejecutado el 10-oct

| Comprobación | Resultado ejecutable | Alcance / límite |
| --- | --- | --- |
| Ejecución | ✅ 12 correctas, 0 fallos/omitidas/abortadas; Release sin advertencias reportadas; 13,64 s con preparación. | Servidor Web real en Windows y PostgreSQL desechable/Testcontainers. Las 50 anteriores, arquitectura y horas/Docker no se repitieron. |
| Certificado y conexión | ✅ TLS 1.3 negociado; certificado y nombre validados. Autoridad desconocida y nombre incorrecto rechazados. | Autoridad ficticia aceptada solo por el cliente QA; no se modifica confianza de Windows ni DNS. No prueba que todos los protocolos antiguos estén deshabilitados. |
| HTTP → HTTPS | ✅ 307 en salud/API; conserva ruta/consulta, sin cuerpo ni cookie de sesión. | Puertos/certificado suministrados por QA; no acredita el Dockerfile HTTP ni un despliegue con proxy. |
| Cookie del servidor | ✅ Renovación real por HTTPS: Secure, HttpOnly, SameSite=Lax. Cliente .NET usa la cookie por HTTPS y la excluye de HTTP. | Sesión inicial preparada con servicios reales, 16 min de antigüedad simulada. Sin login por formulario ni navegador. |
| Autorización | ✅ Consulta HTTPS devuelve el evento ficticio esperado; sin sesión devuelve 401. | No sustituye completar filtros/límites Q03 ni permisos de las demás operaciones. |
| HSTS | ✅ max-age=2592000 en HTTPS bajo modo Production local con nombre .test. Ausente en HTTP y localhost. | Se comprueba la cabecera; no el comportamiento de un navegador ni HSTS en nube. Sin includeSubDomains/preload; duración final por decidir al desplegar. |
| Mensajes y limpieza | ✅ Mensajes capturados sin los secretos ficticios buscados; certificado privado, proceso y PostgreSQL retirados. | Búsqueda acotada a este arranque/consulta. Solo se conservan metadatos públicos y cabeceras permitidas, sin valores de cookies. No sustituye las pruebas ampliadas de errores/registros. |

**Reproducción:** Docker Desktop iniciado; desde la raíz del repo:

```powershell
dotnet test tests/TherapEase.IntegrationTests/TherapEase.IntegrationTests.csproj -c Release --filter FullyQualifiedName~HttpsLocalTests --logger "trx;LogFileName=b00-https.trx"
```

[Pruebas HTTPS](../../tests/TherapEase.IntegrationTests/HttpsLocalTests.cs) y [entorno temporal](../../tests/TherapEase.IntegrationTests/HttpsLocalRevision.cs). La evidencia automática queda en TestResults/https y se excluye de Git. TRX de esta ejecución: b00-https-final-10oct.trx, SHA-256 dd0dd08048c161a5cf2d7dd1fde06ee92d98f94c5c44edc89282d6b1193a3b46. Base local 47ca601; ampliación incluida en el mismo PR #4. Pruebas comentadas según el skill del proyecto. Auditoría HTTPS recibida de Claude: APROBADO, sin correcciones obligatorias; revisión humana pendiente. La ampliación Q03 de §4.6 ya tiene auditoría APROBADO; implementación de Miguel y revisión humana pendientes.

> La redirección no protege datos que un cliente ya haya enviado por HTTP. La prueba envía las credenciales de sesión únicamente por HTTPS. HSTS necesita un cliente compatible y una primera conexión segura; ASP.NET Core excluye localhost por defecto. [Referencia oficial de Microsoft](https://learn.microsoft.com/en-us/aspnet/core/security/enforcing-ssl?view=aspnetcore-10.0).

**Cabeceras S-30 — aprobadas por Lucía el 11-oct-2026:** el inventario siguiente corresponde a la ejecución del 10-oct, antes de esta decisión. Miguel implementa la política en PR #5; Lucía verifica las cabeceras reales en su entrega de QA. No se acredita todavía su implementación.

| Cabecera | Observado el 10-oct | Política aprobada el 11-oct; implementación pendiente |
| --- | --- | --- |
| X-Content-Type-Options | Ausente | nosniff, con Content-Type correcto. |
| Content-Security-Policy | Ausente | frame-ancestors 'none'; completar restricciones de recursos después de validar las páginas de Dulce. |
| X-Frame-Options | Ausente | DENY como apoyo a navegadores antiguos, coherente con frame-ancestors. |
| Referrer-Policy | Ausente | no-referrer, para evitar enviar la dirección de origen a otros sitios. |
| Cache-Control | no-store, no-cache en las dos respuestas observadas | Exigir no-store en contenido privado autenticado, incluida respuesta sin renovación; sin aplicarlo indiscriminadamente a archivos estáticos. |

Propuesta fundamentada en [OWASP HTTP Headers](https://cheatsheetseries.owasp.org/cheatsheets/HTTP_Headers_Cheat_Sheet.html). Lucía aprobó la política el 11-oct; Miguel la implementa en PR #5 y Lucía prueba en su PR de QA. Navegador, antifalsificación, configuración del despliegue y cierre B00/LCA siguen pendientes.

### 4.6. Consulta Q03 por API — ejecutada el 10-oct

| Comprobación | Resultado ejecutable | Alcance / pendiente |
| --- | --- | --- |
| Ejecución | ⏳ 51 casos: 46 correctos, 5 fallidos, 0 omitidos en la ampliación auditada. Antes: 47 Q03 y 12 HTTPS, 58 correctos de 59. | Release sin advertencias reportadas. Servidor real por HTTPS, PostgreSQL desechable y datos ficticios; sin repetir las 50 anteriores, arquitectura ni horas/Docker. |
| Filtros válidos | ✅ Periodo incluye inicio/excluye fin; orden descendente; tipo, identificador y combinación de ambos; sin coincidencias devuelve lista vacía. | Cinco eventos conocidos dentro del periodo y dos fuera; no depende de la zona del equipo. |
| Límites | ✅ 1, 100, 499 y 500 devuelven exactamente los más recientes de 501 eventos. Sin límite: 100. Cero, negativo, 501, decimal, texto y desbordamiento: 400. | No es una prueba de rendimiento ni paginación. |
| Entradas inválidas | ⏳ Identificador mal formado, fechas ausentes/iguales/invertidas y varios tipos inválidos: 400. **0, 1, 2, Paciente,Cita y Paciente, Cita devuelven 200 en vez de 400.** | Miguel acepta solo nombres completos, sin distinguir mayúsculas; rechaza números y comas antes de convertir. Pruebas auditadas por Claude: APROBADO; corrección y revisión humana pendientes. |
| Permiso actual | ✅ Sin cookie, inválida, desactivada y rol cambiado: 401. Usuaria y superusuario temporal: 403, sin eventos. | Sesiones reales preparadas con Identity; baja y cambio de rol por servicios reales. Acceso/comando final del operador siguen pendientes. |
| Solo lectura | ✅ GET no cambia filas de auditoría; POST/PUT/PATCH/DELETE: 405 y filas idénticas. | Huella de todas las filas antes/después; permisos INSERT/UPDATE/DELETE de PostgreSQL conservan su evidencia anterior. |
| Contenido | ✅ Solo ocho campos de metadatos, actor/fecha/registro/acción correctos; sin nombre/contacto, usuario, contraseña, hash, sello, cookie, conexión ni clave de sesión ficticios buscados. | Se buscan también cadenas JSON descodificadas. Eventos paciente/cita preparados por QA; no acredita todos los productores de eventos ni textos clínicos, que no existen en este alcance. |
| Preparación y limpieza | ✅ Se corrigió el reloj de preparación; sesiones usan hora real y eventos de administración fecha fija. Procesos, contenedores y PFX retirados. | Primer intento conservado: Q03 no llegó a ejecutarse por sesiones vencidas en QA; no es un fallo de producción. |

**Reproducir Q03:** Docker Desktop iniciado; desde la raíz del repo:

```powershell
dotnet test tests/TherapEase.IntegrationTests/TherapEase.IntegrationTests.csproj -c Release --filter FullyQualifiedName~ConsultaAuditoriaApiTests --logger "trx;LogFileName=b00-q03.trx"
```

[Casos API](../../tests/TherapEase.IntegrationTests/ConsultaAuditoriaApiTests.cs) y [datos/entorno](../../tests/TherapEase.IntegrationTests/ConsultaAuditoriaRevision.cs). Las cinco entradas inválidas citadas **permanecen fallidas**, sin omitirla ni cambiar el resultado esperado. [Ajuste propuesto para Miguel, §7](../reviews/TherapEase-09-B00-revision-PR01.md). Para comprobarlo, GET /api/auditoria/eventos con fechas válidas y tipoRegistro=Paciente%2CCita, usando una sesión ficticia de superusuario por HTTPS: esperado 400; observado 200 y tipo Cita. Enum.TryParse acepta listas y combina sus valores; IsDefined no basta si el resultado combinado coincide con otro valor válido. [Documentación de Microsoft](https://learn.microsoft.com/en-us/dotnet/api/system.enum.tryparse?view=net-10.0).

**Evidencia:** b00-q03-https-entrega-10oct.trx, SHA-256 17e9fe894f1d4895c171ec9089019b3b20f19af1555964892747376155ddb3eb. Resumen del hallazgo original en tipo-combinado.json, sin valores de sesión. Ampliación: q03-ampliado-10oct.trx, 51 casos (46 correctos, 5 fallidos). Base local e97646d; ampliación incluida en el mismo PR #4. Pruebas comentadas según el skill del proyecto. Veredicto recibido: APROBADO para las pruebas; números/listas ampliados conforme a Claude. Corrección de Miguel y revisión humana pendientes; B00/LCA/R04 abiertos.

### 4.7. Q08 — interrupciones y reintentos de Identity, 10-oct

| Comprobación | Resultado ejecutable | Alcance / pendiente |
| --- | --- | --- |
| Ejecución | ✅ **16 correctas, 0 fallidas/omitidas/abortadas**, Release sin advertencias reportadas; **29,94 s** incluida preparación. | Dos archivos nuevos de integración, sin dependencias nuevas ni cambios en producción. Primera ejecución: 12 correctas; después 14 con cambio propio temporal; entrega final de 16 incluye cambio propio ordinario. |
| Antes de confirmar | ✅ Alta, baja, rol, restablecimiento y cambios propios ordinario/temporal: excepción antes del commit revierte usuarios, roles y auditoría. La cookie anterior conserva validez y un intento posterior se guarda una sola vez. | Comparación desde otra conexión, no desde el estado en memoria de EF. |
| Éxito después de guardar | ✅ Mientras se pausa antes del commit, la operación no termina y otra conexión sigue viendo las filas anteriores. Al liberar, devuelve éxito y se ven usuario y evento. | No acredita el mensaje que mostrará un PageModel. |
| Cancelación y conexión | ✅ Cancelar en ese punto revierte todo. También se termina realmente la conexión de la transacción ficticia con pg_terminate_backend; no quedan filas parciales y otro intento funciona. | Solo PostgreSQL desechable; no se corta ningún servicio externo. |
| Respuesta perdida después | ✅ Commit real seguido de excepción de QA: cambio y evento quedan guardados aunque el servicio no entregue respuesta. Baja, rol y contraseña revocan la cookie anterior. | La excepción simula la pérdida entre confirmación y retorno del servicio; no es un corte real de HTTP o navegador. |
| Repetir alta, baja o rol | ✅ Devuelven resultado fallido al pedir otra vez el mismo estado; filas y evento permanecen idénticos, sin duplicar. | Una nueva solicitud/scope; no habilita reintentos ciegos en toda operación. |
| Cambio propio temporal y ordinario | ✅ Si se pierde su respuesta, la cookie vieja queda revocada; se puede iniciar sesión con la nueva clave conocida y sin restricción de temporal. | Ambos cambios propios probados; en el ordinario se deja pasar y cuenta la comprobación de la clave actual antes de cortar el cambio. Páginas/red pendientes. |
| Restablecimiento | ✅ Se observa una diferencia importante: repetirlo crea otra temporal válida y otro evento. ⏳ Procedimiento de nueva entrega pendiente. | No se acredita «reintento sin duplicar» para este caso ni se aprueba automáticamente una política. Mantener la decisión de entrega ya pendiente; no almacenar/revelar la primera temporal. |
| Revisión y otros servicios | ✅ Auditoría de Claude APROBADO el 10-oct; ⏳ revisión humana y demás operaciones/formularios. | Miguel o Dulce revisa estas pruebas. Pacientes, citas, pago y duplicados de citas siguen pendientes; B00/LCA/R04 abiertos. |

**Reproducir:** Docker Desktop iniciado; desde la raíz del repo:

~~~powershell
dotnet test tests/TherapEase.IntegrationTests/TherapEase.IntegrationTests.csproj -c Release --filter FullyQualifiedName~InterrupcionesIdentidadTests --logger "trx;LogFileName=b00-q08.trx"
~~~

[Pruebas Q08](../../tests/TherapEase.IntegrationTests/InterrupcionesIdentidadTests.cs) y [preparación/cortes de confirmación](../../tests/TherapEase.IntegrationTests/InterrupcionesIdentidadRevision.cs). Herramientas: xUnit, Testcontainers, Npgsql y callbacks de transacción de EF sobre la unidad de trabajo real. La aplicación usa su usuario PostgreSQL sin privilegios; solo el corte de conexión usa el administrador del contenedor ficticio. Datos exclusivamente ficticios. Pruebas comentadas según el skill del proyecto.

**Evidencia:** q08-entrega-ampliada-10oct.trx, SHA-256 2beba1cf37a328f877ea2de216aaf6d320bb08e30a4fa5f46372eba4a9819f27; resúmenes JSON solo con estados/huellas y manifest.json. Se comprobó que no quedan contenedores Testcontainers; no se construyeron imágenes nuevas. No se repitieron los bloques anteriores. Ampliación incluida en el mismo PR #4; sin cierre B00/LCA.

**Veredicto Q08 — 10-oct:** Claude APROBADO, sin correcciones a las pruebas. Para Dulce: rechazo conocido → «No se guardó» y motivo; excepción inesperada → «No pudimos confirmar si se guardó; revisa el registro antes de repetir», sin reintento automático; nuevo restablecimiento → avisar que la temporal anterior deja de servir. Implementación/PageModel, procedimiento de entrega y revisión humana pendientes.

### 4.8. Errores y registros sin valores privados — 10-oct

| Comprobación | Resultado ejecutable | Alcance / pendiente |
| --- | --- | --- |
| Ejecución | ✅ 11 correctas, 0 fallidas/omitidas/abortadas; Release, 0 errores/advertencias al compilar. 76,22 s incluida preparación. | Solo este bloque nuevo; no se repitieron ni se dan por corregidos los fallos de arquitectura/Q03. |
| Entradas inválidas | ✅ Siete respuestas 400: fecha, tipo, identificador, límite de texto, límite desbordado, periodo invertido y límite 501. | Valores ficticios enviados en campos inválidos/parámetros extra; respuesta y registros capturados no los repiten. |
| Acceso rechazado | ✅ Dos respuestas 401, sin sesión/con cookie inválida; sin redirección ni valores privados buscados. | Permisos de los otros roles ya tienen evidencia separada Q03; no son casos nuevos de este bloque. |
| Fallo real de auditoría | ✅ Se retira SELECT únicamente en EventoAuditoria de la base ficticia; devuelve 500 con página genérica y no-store. | El registro contiene 42501, sin los datos sensibles buscados. SELECT se restituye y la consulta vuelve a 200. |
| Fallo real de Identity | ✅ Retirar SELECT de AspNetUsers produce 500 vacío, sin valores privados ni detalles técnicos. Restituirlo permite otra consulta 200. | ⏳ Mejora propuesta de mensaje genérico que funcione si falla Identity; S-20 sigue parcial. No se confunde ausencia de filtración con mensaje útil. |
| Búsqueda y evidencia | ✅ Nombre/contacto guardados, cuenta/contraseña ficticia, hashes, sellos, claves de sesión, cookies inicial/renovada y conexiones/claves ficticias; representaciones de URL/HTML/JSON. | Texto clínico solo como entrada ficticia: el modelo actual no tiene ese campo. Se buscan respuestas y salida/errores capturados tras detener el proceso; no se guardan mensajes crudos. Set-Cookie se permite como entrega normal de sesión, nunca en cuerpo/registros. |
| Limpieza y revisión | ✅ 11 entornos retirados; sin contenedores Testcontainers ni imágenes nuevas. | ✅ Auditoría de Claude APROBADO; ⏳ revisión humana. No acredita toda la suite, páginas, todos los valores posibles, nube ni Q04/S-20/S-21 completos. |

**Reproducir:** Docker Desktop iniciado; desde la raíz del repo:

~~~powershell
dotnet test tests/TherapEase.IntegrationTests/TherapEase.IntegrationTests.csproj -c Release --filter FullyQualifiedName~ErroresRegistrosTests --logger "trx;LogFileName=b00-errores-registros.trx"
~~~

[Pruebas de errores](../../tests/TherapEase.IntegrationTests/ErroresRegistrosTests.cs) y [preparación/búsqueda](../../tests/TherapEase.IntegrationTests/ErroresRegistrosRevision.cs). Se reutiliza el servidor HTTPS existente mediante tres consultas de registros en memoria, sin modificar su arranque o limpieza. Herramientas: xUnit/HttpClient, Kestrel real en modo Production local, EF/Npgsql y PostgreSQL desechable/Testcontainers con migraciones/roles reales. Solo el migrador altera los permisos temporales de prueba; el servidor conserva su usuario limitado.

**Evidencia:** errores-registros-10oct.trx, SHA-256 efba171d31b618949584a1da04dcdc97d9783be7a3cdf17c7f2b8b49f8f8cdd2; manifest.json y resúmenes por caso sin valores privados. Base local 1bf4f45; ampliación incluida en el mismo PR #4. Pruebas comentadas según el skill del proyecto. Auditoría de errores/registros: Claude APROBADO, sin correcciones obligatorias. Decisión ADR-05 registrada el 10-oct, sin implementación; revisión humana y otros pendientes B00/LCA/R04 permanecen abiertos.

## 4.9. Revisión del PR #5 y repetición conjunta — 11-oct-2026

> **Commit exacto:** `7e83c3c6033c6c0df9dd15c2b751950e631c5475`, base `709ff615e1f1add0b008deff57e567de719b241c`. PR #4 fusionado; revisión humana APPROVED de Miguel sobre f06dc7b. PR #5 sigue abierto, sin aprobación/fusión en esta revisión.

| Comprobación | Resultado / alcance |
| --- | --- |
| `dotnet build TherapEase.sln -c Release` | 0 errores, 0 advertencias. |
| UnitTests, Release | 192 correctas; 0 fallidas/omitidas. |
| IntegrationTests, Release | 182 correctas; 0 fallidas/omitidas. Incluye arquitectura 30/30 y Q03 51/51: los siete fallos anteriores están corregidos. |
| Nueve adicionales locales CU04 | 9 correctas; 0 fallidas/omitidas. Sin citas/con citas inactivas, rechazo de activa, cita posterior a consulta, versión concurrente, fallo de auditoría, respuesta perdida/repetición y bloqueo real baja frente a cita. Archivo local BajaPacienteTests, renombrado el 12-oct; correspondencia con casos existentes en plan §0.3. Incorporación pendiente en un PR de Lucía, con revisión distinta. No están en PR #5 ni tienen aún revisión humana. |
| Alcance técnico | Windows, .NET 10.0.401, Docker 29.8.1, PostgreSQL 17 desechable/Testcontainers. Migraciones, roles y servicios reales; datos ficticios. Tres ejecuciones de pruebas separadas, no CI. |
| Arranque intermitente de QA | Miguel lo reprodujo en PR #4/PR #5: tiempo de espera HTTP de 3 s no capturado. No ocurrió en esta ejecución; mejora del apoyo de pruebas de Lucía, no defecto nuevo de producción. |
| Resultado propuesto | CORREGIR menor: README desactualizado y permiso ampliado aprobado por Lucía el 11-oct, aún por implementar/probar. Los resultados citados pertenecen al commit anterior a esa ampliación. Código solicitado comprobado. [Informe PR #5](../reviews/TherapEase-09-B00-revision-PR05.md). |

Los TRX y el manifiesto local identifican el commit y las huellas. Sin contenedores Testcontainers ni imágenes therapease-b00-* restantes al finalizar. El checkout revisado conserva intactos los archivos versionados; solo se agregó QA local. No se acreditan páginas, permisos operativos definitivos, respaldo diario ni cierre B00/LCA/R04.

### 4.10. Revisión y ampliación QA — 13-oct-2026

| Comprobación | Resultado ejecutable / límite |
| --- | --- |
| PR #5, `bce94091ba614e16f4cce3435697d8c58c2a31ff` | Build Release: 0 errores/advertencias; 258 unitarias y 182 integración, todas correctas, 0 omitidas. Mensaje 500 corregido en JSON, HTML fijo y Error.cshtml; descripción actualizada. |
| BajaPacienteTests | 14 casos correctos: nueve de guardado/concurrencia/atomicidad y cinco adicionales de ambos roles, temporal y sesión ausente. PostgreSQL/Identity reales; no es recorrido de página. |
| CabecerasSeguridadTests | 9 casos correctos: cuatro cabeceras en salud, CSS, API y páginas; no-store privado, CSS sin no-store; 500 JSON/HTML con SELECT de Identity retirado, mensaje seguro y recuperación 200 al restituirlo. No es navegador. |
| Recuperación | 13 casos correctos, conservando cifrado de QA. La consulta restaurada compara ahora identificador de evento y paciente, tipo, acción y hecho; ya no basta recibir 200. |
| Arranque de QA | ServidorRevision reintenta cuando una consulta de salud agota sus 3 s, con ventana de arranque de 40 s y limpieza si falla. La selección afectada pasó; no se fuerza artificialmente un arranque lento. |
| Ejecución de ampliación | 36/36 (14 baja + 9 cabeceras + 13 recuperación), 0 fallos/omitidas. Pruebas comentadas según el skill del proyecto. Revisión humana de Miguel o Dulce pendiente. |
| Entrega | Misma rama revision/etapa09, un PR conjunto de QA. Notas personales y TestResults no se publican. PR #5 fusionado por Lucía en main 40785d2; revisión humana APPROVED registrada en GitHub. El conector no pudo fusionarlo (403). |
| Avance B01 | Autorizado expresamente por Lucía el 13-oct, con datos ficticios y pendientes B00/LCA visibles. No autoriza nube, datos reales, cierre de riesgos ni fusión del QA sin revisión distinta. |

Las secciones 4.1–4.9 conservan las ejecuciones históricas. Para resultados y pendientes actuales prevalecen esta sección y las tablas de §7. El cifrado real age, páginas/navegador, producto, revisión humana del QA nuevo y consolidación de LCA siguen separados.


**Repetición final en revision/etapa09:** código `4ca53c490eb69265c3853576d2be3ba2d89ff4dc`, sobre main `40785d2`: **258 unitarias y 205 integración correctas**, 0 fallos/omitidas, en ejecuciones separadas. Compilación de los proyectos afectados sin advertencias/errores. Sin cambios de producción. No quedaron contenedores Testcontainers ni imágenes therapease-b00-* tras las pruebas.

| Resultado local | Huella SHA-256 del TRX |
| --- | --- |
| qa-unitarias-final-13oct.trx | `9be67c9fb3e12212987d72132ba5ba6eced7d14f8adfd220677f09891024086e` |
| qa-integracion-final-13oct.trx | `a2e9a47fa6ebef22b445e2dde04f5c251445853335905cd4b11a3824e5a872be` |

```powershell
dotnet test tests/TherapEase.UnitTests/TherapEase.UnitTests.csproj -c Release --logger "trx;LogFileName=qa-unitarias.trx" --results-directory TestResults/QA
dotnet test tests/TherapEase.IntegrationTests/TherapEase.IntegrationTests.csproj -c Release --logger "trx;LogFileName=qa-integracion.trx" --results-directory TestResults/QA
```

Docker Desktop Linux debe estar iniciado. Pruebas comentadas según el skill del proyecto. TRX/manifest permanecen locales; la revisión humana del QA nuevo y su fusión están pendientes.

### 4.11. Correcciones de auditoría del PR #6 — 14-oct-2026

**Código probado:** `0aa32f2b4f387f02ded7b1100a74e869a0ad40d7`. La documentación posterior conserva ese código sin cambios.

| Estado | Corrección / comprobación | Resultado y límite |
| --- | --- | --- |
| ✅ | Paquetes vulnerables de pruebas | Pkcs y Xml: 10.0.12 → 10.0.13, con referencias explícitas en UnitTests e IntegrationTests. Decisión registrada en nota posterior ADR-06. |
| ✅ | Auditoría NuGet con transitivos | Siete proyectos: cero paquetes vulnerables reportados. Se verificó que ambos proyectos resuelven 10.0.13; no se suprimieron avisos. No es auditoría completa de SDK/runtime o imagen. |
| ✅ | Encabezados de recuperación | Cuatro en RecuperacionRevision y cinco en RecuperacionTests, de 96 caracteres; comentarios y lógica conservados. Se comparó el texto restante con 48018df. |
| ✅ | Build Release y repetición completa | 0 errores/advertencias; 258 unitarias y 205 integración correctas, 0 fallos/omitidas. PostgreSQL desechable/Testcontainers, datos ficticios, Docker disponible. |
| ⏳ | Revisión humana y fusión | Mismo PR #6, misma rama revision/etapa09; Miguel revisa las correcciones y el QA anterior. Lucía decide fusión; B00/LCA mantienen pendientes. |

Pruebas comentadas según el skill del proyecto. Las ejecuciones del 13-oct se conservan como historia; esta repetición usa los paquetes corregidos.

```powershell
dotnet restore TherapEase.sln --force-evaluate
dotnet list package --vulnerable --include-transitive --format json --no-restore
dotnet build TherapEase.sln -c Release --no-restore
dotnet test tests/TherapEase.UnitTests/TherapEase.UnitTests.csproj -c Release --no-build --logger "trx;LogFileName=qa-unitarias-14oct.trx" --results-directory TestResults/QA-14oct
dotnet test tests/TherapEase.IntegrationTests/TherapEase.IntegrationTests.csproj -c Release --no-build --logger "trx;LogFileName=qa-integracion-14oct.trx" --results-directory TestResults/QA-14oct
```

| Evidencia local | SHA-256 |
| --- | --- |
| qa-unitarias-14oct.trx | `46f81d1f23524848ac7855a377b706762331dc25fa9d21ef497e4530aef44fdd` |
| qa-integracion-14oct.trx | `da40586b9f2a0a84a296944af5ef4d7369627e977a5a6cac806f7cfbbf010b66` |
| paquetes-vulnerables.json | `67484d004f6a8d77f37315adf57404f69fbbb243d887b64c13d4fdb53b5f74fe` |

TRX, JSON y manifest permanecen locales; se publica este resumen, sin datos privados ni registros crudos.

## 5. Cobertura y alcance de los pendientes

| Área | Avance vigente | Lo que falta |
| --- | --- | --- |
| Arquitectura | 30/30 sobre PR #5; pruebas del PR #4 revisadas por Miguel. CU04 mínimo implementado y nueve adicionales locales correctas. | Revisión del QA adicional; límites sobre PageModel funcionales. |
| Datos Q03/Q06/Q07/Q08/Q20 | Q03 51/51; persistencia/Identity/recuperación repetidas dentro de 182/182. CU04 mínimo probado con PostgreSQL. | Acceso final Q03; restantes operaciones, estados, concurrencia e interrupciones disponibles del plan §7. |
| Horas y Docker | 12/12 en la suite repetida; revisión humana del PR #4 registrada. | Interfaz/navegador cuando existan páginas. |
| Identidad y seguridad | QA de servidor revisado; HTTPS, Q08 y errores/registros repetidos. | Entrega segura de temporales, PageModel/navegador y validación de cabeceras. Secret Protection/Push protection activadas según captura del 12-oct; reverificación actual de main pendiente. |
| Recuperación local | Procedimiento QA repetido; PR #4 revisado/fusionado. ADR-05 decidido, sin implementación real. | Mantener límites de carga/operación; respaldo real solo con autorización posterior. |
| Decisión LCA | B01 autorizado expresamente por Lucía el 13-oct mientras termina B00. | No acredita cierre LCA: páginas, revisión humana del QA y consolidación de riesgos residuales siguen pendientes. |

> §7 reúne los pendientes conocidos; pueden aparecer nuevos hallazgos. Las funciones completas de B01–B08 y la operación real se distinguen en §9. No quedan autorizadas por una fusión parcial.

## 6. Siguientes pasos compartidos

**Usar la tabla de orden de §7 para decidir y trabajar; el informe PR05 contiene los ajustes concretos de Miguel.**

| Entrega agrupada | Responsable | Qué reúne |
| --- | --- | --- |
| **PR #5 existente** | Miguel; Lucía revisa y aprueba | Correcciones obligatorias, permiso aprobado y unitarias; las cuatro mejoras de backend y cabeceras aprobadas el 11-oct; propuestas de temporales/acceso Q03 y documentación propia. |
| **[PR #6](https://github.com/LuciAguilar/Therapease/pull/6), revision/etapa09** | Lucía; Miguel o Dulce revisa | Pruebas de baja con nombre definitivo y cobertura del permiso aprobado, las tres mejoras de QA aprobadas el 11-oct, verificaciones ejecutables disponibles y documentación/evidencia actualizada. Se prepara como una entrega conjunta después de integrar PR #5. |
| **Pendientes con páginas/producto** | Dulce implementa/valida; Lucía prueba | Se agrupan cuando estén disponibles. No obligan a mantener abiertos los dos PR anteriores B01 tiene autorización expresa del 13-oct; sus criterios y pruebas se siguen en ese bloque. |

No crear un PR por cada mejora. GitHub Settings y el correo local se verifican/configuran fuera de los PR; en la documentación solo se registra evidencia sin datos privados. Lucía autorizó el 13-oct preparar su PR conjunto de QA; no crear otra rama.

## 7. Plan de validación autorizado

> **Esta es la lista de trabajo compartida.** Cada fila indica persona, avance y dependencia. No se repiten como pendientes las pruebas ya ejecutadas. Una fila de revisión sigue ⏳ aunque el código de prueba pase.
>
> Los códigos como **A-01** son referencias del [plan de pruebas, sección «B00 — Validación de arquitectura»](../testing/TherapEase-09-plan-de-pruebas.md); **no son pasos nuevos**. A-01 corresponde a la prueba de límites del **paso 1** siguiente.

### Orden para agrupar los pendientes actuales

> **Decisión de Lucía, 11-oct-2026: órdenes 1–3 aprobados completos.** Las filas 1–3 marcan la decisión; las tareas 4–6 y la ejecución de QA de 12 están realizadas, §4.10. La revisión humana y fusión del QA nuevo siguen pendientes en 14.
>
> Los números indican prioridad de trabajo, **no nuevas etapas ni nuevos identificadores de pruebas**. Se puede avanzar en paralelo donde no exista dependencia. Aprobar el alcance no acredita ejecución ni aprobación del PR. El detalle de cada tarea permanece en los pasos 1–4 siguientes y §8.

| Orden    | Estado | Quién / trabajo                                                           | Decisión / trabajo restante                                                                                                                                                                                                                                                     | Entrega                                                                                               |
| -------- | ------ | ------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------- |
| **1**    | ✅      | **Lucía: cabeceras S-30 aprobadas.**                                      | `X-Content-Type-Options: nosniff`, `Content-Security-Policy: frame-ancestors 'none'`, `X-Frame-Options: DENY`, `Referrer-Policy: no-referrer` y `Cache-Control: no-store` en contenido privado. CSP de recursos se completa con las páginas; HSTS final se decide al desplegar. | Miguel implementa en PR #5 (6); Lucía verifica en su PR de QA (12).                                   |
| **2**    | ✅      | **Lucía: paquete completo de backend aprobado.**                          | comprobar resultados de Identity, respuesta genérica del error 500 independiente de Identity/base de datos, ámbitos de Paciente de solo lectura y aclarar que `/salud` mide el proceso, no la disponibilidad de PostgreSQL. Implementación y comprobación realizadas, §4.10.           | Incluido en PR #5 (5), con unitarias propias.                                                         |
| **3**    | ✅      | **Lucía: paquete completo de QA aprobado.**                               | arranque de QA más tolerante al tiempo de espera agotado, comprobar el contenido del evento HTTP restaurado además del 200 y documentar `crear-roles.sql` para un servidor nuevo porque `pg_dump` no copia los usuarios de PostgreSQL. Preparación y ejecución realizadas, §4.10.      | Incluido con las pruebas de baja/cabeceras en su próximo PR conjunto de QA (12).                      |
| **4**    | ✅      | **Miguel: ajustes obligatorios del PR #5.**                               | Implementado en bce9409 y comprobado con 258 unitarias/182 integración; ampliación QA de §4.10. | PR #5 existente.                                                                                      |
| **5**    | ✅      | **Miguel: aplicar las cuatro mejoras de backend aprobadas.**              | Implementado en bce9409 y comprobado con 258 unitarias/182 integración; ampliación QA de §4.10. | Mismo PR #5.                                                                                          |
| **6**    | ✅      | **Miguel: configurar cabeceras aprobadas.**                               | Implementado en bce9409 y comprobado con 258 unitarias/182 integración; ampliación QA de §4.10. | Mismo PR #5; QA en la entrega de Lucía.                                                               |
| **7**    | ✅      | **Miguel: propuesta de temporales presentada y aprobada.**                | Lucía aprobó el 12-oct puntos 1–4 y caducidad A para B00 ficticio; B de 24 h antes de datos reales. Regla en AGENTS §2.2 y nota posterior ADR-05. Entrega/interfaz y prueba final siguen pendientes.                                                                            | Decisión registrada; recorrido final con Dulce y QA de Lucía.                                         |
| **8**    | ✅      | **Miguel: propuesta de acceso Q03 presentada y aprobada.**                | Lucía aprobó el 12-oct opción A: navegador autenticado de superusuario con cookie/permiso vigente. No incluye el script B. Falta acceso/cierre de sesión de Dulce y prueba final.                                                                                               | Decisión registrada; implementación/prueba pendientes en 16–17.                                       |
| **9**    | ✅      | **Lucía: procedimientos 7–8 decididos.**                                  | Orden 7 puntos 1–4, caducidad A provisional y B antes de datos reales; orden 8 opción A. Aprobar el procedimiento no acredita implementación ni autoriza datos reales.                                                                                                          | Registrar en AGENTS y ADR-05; sin PR por la decisión sola.                                            |
| **10** | ✅ | **Lucía: secretos y revisión exigida en main verificados.** | Secret Protection/Push protection: captura 12-oct. Main exigió al menos una revisión con permisos de escritura, según bloqueo comunicado por Lucía el 13-oct; después registró APPROVED y fusión de PR #5. No se ensayó un push con secreto ni se inspeccionaron otras reglas de main. | Configuración y ejecución del flujo de revisión; no garantía universal de seguridad. |
| **11**   | ⏳      | **Cada autor: correo noreply para futuros commits.**                      | Recomendación opcional de privacidad. No incluye reescribir historial publicado.                                                                                                                                                                                                | Configuración personal, sin PR propio.                                                                |
| **12** | ✅ | **Lucía: QA reunido y ejecutado.** | Baja 14, cabeceras 9, recuperación 13 y arranque mejorado; copia de roles documentada. Revisión/fusión separadas en 14. | Un PR conjunto de QA en revision/etapa09; sin notas personales. |
| **13** | ✅ | **Lucía: PR #5 revisado, aprobado y fusionado.** | bce9409: build correcto, 258 unitarias/182 integración; main 40785d2. | Fusión squash del 13-oct; B00/LCA conservan pendientes. |
| **14**   | ⏳      | **Miguel o Dulce: revisar el PR de QA; Lucía decide fusión.**             | Revisar código y evidencia del nuevo alcance; la aprobación de PR #4 no lo cubre. Se publica el conjunto, sin PR por archivo.                                                                                                                                                   | Próximo PR de QA de Lucía, después de integrar #5, en revision/etapa09.                               |
| **15**   | ⏳      | **Dulce: validar políticas de producto con la usuaria.**                  | Citas contiguas, pago inicial, tratamiento de citas al dar de baja, retirada de ámbitos con citas y pago en citas canceladas/de baja. Campos del directorio: avance posterior. Lucía confirma decisiones; no exigir respuestas para mejoras independientes.                     | Registrar respuestas en los documentos existentes; aplicar en el bloque autorizado.                   |
| **16**   | ⏳      | **Dulce: acceso mínimo/PageModel y unitarias.**                           | Llamadas a Application y mensajes Q08 acordados; revisión distinta del autor. B01 autorizado por Lucía el 13-oct en otro chat; mantener trazabilidad del acceso mínimo B00.                                                                                                                                                     | Su entrega del acceso mínimo de B00.                                                                  |
| **17**   | ⏳      | **Lucía: completar validaciones que aún necesitan servicios/páginas.**    | Navegador, cookies, antifalsificación, horas y mensajes; demás estados, conflictos, eventos y concurrencia de los pasos siguientes. Miguel/Dulce aportan lo que falte del alcance autorizado.                                                                                   | Entrega agrupada posterior cuando existan esas partes; no frena el QA independiente disponible ahora. |
| ****18** | ⏳      | **Lucía: consolidar evidencia y decidir B00/LCA.**                        | Pendientes de la puerta tratados expresamente, resultados identificados y revisión humana.                                                                                                                                                                                      | Decisión final; no se deduce de fusionar un PR parcial.                                               |

### Paso 1. Base y límites — arquitectura, Docker y entorno

| Estado | Tarea | Responsable | Dependencia / evidencia necesaria |
| --- | --- | --- | --- |
| ✅ | Crear repositorio y carpetas base; publicar documentos seleccionados. | Lucía | T1 publicado. |
| ✅ | Crear solución, siete proyectos y base Razor Pages/Docker. | Miguel | PR #1 fusionado; build y arranque históricos en §4. |
| ✅ | Escribir y ejecutar pruebas automáticas de capas/módulos, ciclos y contratos; comprobar que detectan infracciones. | Lucía | 30/30 sobre PR #5 (§4.9); ejecución histórica y controles que detectan infracciones en §4.3. |
| ✅ | Resolver la llamada directa Web → MatrizDePermisos y repetir las pruebas de límites. | Miguel corrige; Lucía prueba | PR #5 usa IAutorizacion con registro por solicitud; arquitectura 30/30, §4.9. |
| ✅ | Revisar humanamente las pruebas de arquitectura. | Miguel | Revisión APPROVED en PR #4 el 11-oct; autor distinto de Lucía. |
| ✅ | Implementar coordinador CU04 en Application, fuera de Pacientes/Citas. | Miguel | PR #5; ubicación correcta, sin ciclo ni dependencia inversa. No autoriza B02 completo. |
| ✅ | Probar ubicación y operación real del CU04 mínimo. | Lucía; Miguel revisa el nuevo QA | Arquitectura 30/30 y nueve adicionales locales con PostgreSQL real; revisión humana de estas últimas pendiente. |
| ⏳ | Repetir límites sobre PageModel funcionales. | Lucía; Miguel revisa | Depende de las páginas de Dulce; ErrorModel no acredita páginas aún inexistentes. |
| ✅ | Repetir imagen, arranque, salud y ejecución sin privilegios para el código vigente. | Lucía prueba; Miguel corrige | Imagen actual construida; HEALTHCHECK healthy, HTTP 200 y usuario no root (§4.4). |
| ✅ | Comprobar `America/Hermosillo` dentro de la imagen. | Lucía prueba; Miguel corrige | DLL real de imagen ejecutada bajo UTC, Nueva York y Madrid; 6 fechas por zona (§4.4). |
| ✅ | Reproducir entorno y verificaciones con la documentación. | Miguel; Lucía verifica | Miguel reprodujo 182 pruebas dos veces en PR #4 y registró resultados/fallo de arranque; §4.9. No acredita un simulacro operativo real. |
| ✅ | Comprobar Secret Protection y Push protection. | Lucía | Captura del 12-oct: ambos botones Disable, configuración activada. No acredita bloqueo ejecutado ni ausencia universal de secretos. |
| ⏳ | Reverificar revisión exigida en main. | Lucía | **Orden 10.** Una revisión comprobada el 03-oct; falta comprobación actual. Visibilidad pública ya decidida. |

### Paso 2. Datos — persistencia, concurrencia y horas

| Estado | Tarea | Responsable | Dependencia / evidencia necesaria |
| --- | --- | --- | --- |
| ✅ | Implementar persistencia, migraciones y usuarios separados de PostgreSQL. | Miguel | PR #1 fusionado. |
| ✅ | Probar migraciones desde cero, permisos y auditoría solo de inserción. | Lucía | Repetido dentro de las 50: `INSERT` permitido; `UPDATE`/`DELETE`/alteración denegados. |
| ✅ | Probar cruces SQL, dos reservas simultáneas, versión EF y conservación inicial del paciente. | Lucía | Alcance parcial de los 11 incorporados; no cubre todos los servicios/estados. |
| ✅ | Probar filtros válidos, límites 1–500, contenido, permisos actuales y solo lectura de la API Q03. | Lucía | 46 casos correctos por HTTPS; alcance y evidencia en §4.6. |
| ✅ | Rechazar números/listas de tipos y repetir Q03 sin los cinco fallos. | Miguel corrige; Lucía prueba | PR #5; Q03 51/51, §4.9. Sin omitir pruebas. |
| ⏳ | Completar/probar acceso y comando autenticado de lectura Q03. | Miguel/Dulce implementan su parte; Lucía prueba | **Órdenes 8–9 aprobados el 12-oct; 16–17 pendientes.** Opción A: navegador autenticado. La API exige permiso actual; sesiones de QA no sustituyen el acceso/cierre de sesión de Dulce ni el recorrido final. Sin pantalla de auditoría. |
| ⏳ | Probar cambio y evento atómicos en restantes operaciones; sin datos sensibles ni éxito ante fallo. | Lucía prueba; Miguel corrige | Identity ya tiene casos; ampliar pacientes, citas y pago en la validación disponible. |
| ⏳ | Probar reservas y recuperación de cita agendada por servicios; estados alternos y nuevas comprobaciones de cruce. | Lucía prueba; Miguel implementa/corrige | Paciente vigente; cancelada/de baja no bloquea; políticas contigua/pago pendientes de Dulce. |
| ⏳ | Completar concurrencia por servicios: agendar frente a baja y recuperar frente a otra reserva. | Lucía prueba; Miguel implementa/corrige | Nueve adicionales locales prueban el CU04 mínimo, incluida cita frente a baja; falta revisión y recorrido por servicio de reservas/recuperación. Conservar bloqueo PostgreSQL. |
| ⏳ | Probar conflictos de edición en otros registros; avisar sin sobrescribir. | Lucía prueba; Miguel implementa/corrige | Servicios disponibles y concurrencia controlada; versión EF aislada no basta. |
| ⏳ | Probar baja/consulta/recuperación autorizada, sin borrado físico y fuera de listados ordinarios. | Lucía prueba; Miguel/Dulce implementan su parte | Conservación inicial probada; faltan servicios, filtros y caminos restantes (Q20). |
| ✅ | Probar interrupciones/reintentos de Identity en el alcance disponible. | Lucía; Miguel o Dulce revisa | 16 casos correctos: seis escenarios, pausa, cancelación y corte real de conexión; límites en §4.7. |
| ⏳ | Completar Q08 en demás servicios, formularios/red real. | Lucía prueba; Miguel/Dulce implementan/corrigen | Pacientes/citas/pago, reintentos de citas y presentación de resultado incierto; no extrapolar las pruebas de Identity. |
| ✅ | Probar conversión explícita a Hermosillo desde otras zonas; separar fecha sin hora de instante, en servidor/imagen. | Lucía prueba; Miguel corrige | 18 conversiones en imagen, API rechaza entradas sin zona y EF/PostgreSQL conserva la cita; interfaz/navegador siguen en paso 3 (§4.4). |
| ⏳ | Revisar SQL, permisos, impacto y reversión de nuevas migraciones de esta validación. | Miguel prepara; Lucía revisa/prueba | Solo si hay nuevas migraciones; no dar por ejecutado un rollback no probado. |

### Paso 3. Identidad y seguridad

| Estado | Tarea | Responsable | Dependencia / evidencia necesaria |
| --- | --- | --- | --- |
| ✅ | Implementar Identity inicial y condiciones aprobadas. | Miguel | PR #1 fusionado. |
| ✅ | Probar hashes, longitud, bloqueo, temporal, revocación, rol y último superusuario concurrente. | Lucía | 37 casos, incluidos 11 anteriores; repetidos dentro de 50 correctos del 08-oct. |
| ✅ | Probar cookies/caducidad en componentes, claves en PostgreSQL, comando local y fallo de auditoría de Identity. | Lucía | Reloj controlado; no acredita navegador, TLS o reinicio real. |
| ✅ | Revisar secretos/configuración, registros capturados y `.env.example`. | Lucía | Alcance acotado e imagen histórica; mantener revisión por cada cambio. |
| ✅ | Revisar humanamente las pruebas del PR #4. | Miguel | APPROVED el 11-oct sobre f06dc7b; PR #4 fusionado. No incluye las nuevas pruebas locales del PR #5. |
| ✅ | Procedimiento de temporales decidido el 12-oct. | Lucía decidió; Miguel propuso | AGENTS §2.2; entrega/interfaz y validación final pendientes con Dulce. |
| ⏳ | Implementar acceso mínimo/PageModel y sus unitarias. | Dulce | Llamadas a Application. Rechazo conocido: «No se guardó» y motivo. Excepción inesperada: «No pudimos confirmar si se guardó; revisa el registro antes de repetir», sin reintento automático. Restablecer otra vez: avisar que la temporal anterior deja de servir. Informe §8; Q08/S5. |
| ⏳ | Probar entrada/salida, temporal, cambio/restablecimiento y rol desde las páginas. | Lucía | Depende de PageModel de Dulce; cuentas ficticias y distintas sesiones. |
| ⏳ | Probar cookies reales y renovación por actividad; cookie manipulada/ausente, redirección externa y caché tras salir. | Lucía | Navegador y acceso mínimo; no reutilizar el resultado de componentes como prueba de páginas. |
| ⏳ | Probar antifalsificación, permisos por URL/envío directo y validación de las rutas disponibles. | Lucía prueba; Miguel/Dulce corrigen | PageModel disponibles; ampliar con las rutas de cada bloque posterior. |
| ✅ | Probar errores/respuestas y registros de la API disponible. | Lucía prueba; Miguel/Dulce corrigen | 11 correctas; búsqueda de valores privados y fallos reales de lectura, §4.8. |
| ⏳ | Completar revisión de errores/registros/eventos en otras operaciones y páginas. | Lucía prueba; Miguel/Dulce corrigen | No acreditar toda 09; S-20/S-21 siguen parciales. 500 fijo corregido y comprobado en PR #5; revisión humana del QA nuevo pendiente. |
| ✅ | Probar HTTPS local, certificado, redirección, cookie renovada y HSTS de modo Production local. | Lucía | 12 correctas; servidor real y PostgreSQL desechable. Sin navegador ni despliegue (§4.5). |
| ✅ | Implementar y comprobar las cabeceras aprobadas el 11-oct. | Miguel implementó; Lucía probó | Nueve casos HTTPS del 13-oct, §4.10; navegador y revisión humana del QA pendientes. |
| ✅ | Reiniciar el contenedor de aplicación y comprobar continuidad de claves/sesión, sin claves en disco. | Lucía prueba; Miguel corrige | Reinicio real, cookie previa y evento correctos; claves PostgreSQL iguales y sin key-*.xml observados en /app, /home/app y /tmp. No acredita TLS (§4.4). |

### Paso 4. Recuperación y decisión

| Estado | Tarea | Responsable | Dependencia / evidencia necesaria |
| --- | --- | --- | --- |
| ✅ | Probar copia cifrada, huella, otro PostgreSQL, datos/permisos/Identity y consulta restaurados. | Lucía | 13 casos; 13 tablas iguales en la muestra local. |
| ✅ | Probar copia alterada, clave incorrecta y fallos de copia/restauración; medir pérdida y recuperación. | Lucía | 7,375 s desde detección simulada; no acredita operación diaria. |
| ✅ | Aplicar correcciones de Claude y repetir las 50; restaurar copia íntegra de 25 h e informar Q10. | Lucía | Ejecución del 08-oct, 0 fallos/omitidas. |
| ✅ | Revisar las pruebas de recuperación y reproducir sus resultados. | Miguel | APPROVED y reproducción de la suite del PR #4 el 11-oct; no acredita respaldo operativo real. |
| ✅ | Registrar la decisión ADR-05 sobre cifrado, custodia, antigüedad y validación. | Lucía | Decisión del 10-oct: age, clave pública solo para cifrar en GitHub, privada fuera de GitHub y copia de Miguel; restaurar/comparar antes de cifrar, simulacro mensual y 14 días privados. No implementar todavía. |
| ✅ | Resolver o registrar hallazgos y aprobar/fusionar el PR #4. | Lucía; Miguel revisa las pruebas | Fusionado el 11-oct. Hallazgos tratados en PR #5; no cierra B00. |
| ✅ | Permiso, README/AGENTS y PR #5 comprobados y fusionados. | Miguel implementó; Lucía aprobó/fusionó | bce9409; main 40785d2. Nuevo QA requiere revisión propia, orden 14. |
| ⏳ | Consolidar evidencia Q06–Q11/Q19, límites y riesgos residuales; decidir B00/LCA. | Lucía | Resultados ejecutables, revisión humana distinta y pendientes de esta puerta tratados expresamente; no cierre automático. |

## 8. Consulta Q03, respaldo y mejoras propuestas

| Tema | Regla / situación |
| --- | --- |
| Consulta técnica Q03 | API de lectura por HTTPS: 51/51 correctas sobre PR #5. Opción A aprobada el 12-oct: navegador autenticado de superusuario; falta acceso/cierre de sesión de Dulce y prueba final. Sin pantalla de auditoría. |
| Respaldo aprobado | Decisión del 10-oct en ADR-05 y AGENTS §2.8: age/clave pública, restaurar y comparar antes de cifrar, volcado solo en ejecutor, 14 días privados y simulacro mensual de Lucía. No crear repo, conectar Neon ni configurar flujo. |
| Permiso de baja | Aprobado e implementado para Usuaria y Superusuario, provisional | Comprobado en bce9409 y 14 casos de QA. La política operativa de citas existentes espera a Dulce con la usuaria. |
| Cabeceras S-30 | Implementadas en PR #5, comprobadas por nueve casos HTTPS de QA; revisión humana nueva y navegador pendientes. |
| QA y respaldo real | Cifrar/Abrir mantienen AES-256-GCM exclusivo de QA. Las pruebas de cifrado se reemplazan por age al implementar; volcado, restauración, comparación, fallos y antigüedad se conservan. La decisión no acredita operación real ni autoriza modificar las pruebas ahora. |

| Orden en §7 | Mejoras incluidas y recomendación restante | Quién / entrega | Situación |
| --- | --- | --- | --- |
| **2 → 5** | Comprobar resultados de `UpdateAsync`, `AddToRoleAsync` y `RemovePasswordAsync`. | Miguel; PR #5, incluido | Aprobada el 11-oct; Implementado y comprobado en PR #5, bce9409. |
| **2 → 5** | Ámbitos de Paciente como colección de solo lectura; aclarar alcance de `/salud`. | Miguel; mismo PR #5, incluido | Aprobadas el 11-oct; Implementado/documentado en bce9409. |
| **2 → 5** | Respuesta 500 genérica independiente de Identity/base de datos. | Miguel; mismo PR #5, incluido | Aprobada el 11-oct; Implementado y comprobado en PR #5, bce9409. |
| **3 → 12** | Comprobar el evento HTTP restaurado además del 200; documentar `crear-roles.sql` para servidor nuevo. | Lucía; mismo PR de QA, incluidas | Aprobadas el 11-oct; Aplicadas y comprobadas el 13-oct; revisión humana pendiente. |
| **3 → 12** | Mejorar tolerancia del arranque de ServidorRevision ante timeout. | Lucía; mismo PR de QA, incluida | Aprobada el 11-oct; hallazgo de Miguel, no reproducido en el último conjunto. Corrección aplicada; selección afectada correcta, sin forzar un arranque lento. |
| **11** | Correo noreply en futuros commits. | Cada autor; configuración personal | No incluye reescribir historial publicado ni un PR independiente. |

## 9. Publicación, trabajos posteriores y límites

| Pendiente que continúa | Cuándo / quién | Relación con B00 |
| --- | --- | --- |
| Permisos nuevos de Superusuario: enumerarlos expresamente. | B01 en adelante, al añadir permisos; Miguel implementa unitarias y Lucía revisa. | Observación no bloqueante: MatrizDePermisos concede hoy todo permiso nuevo automáticamente. Antes de añadir, por ejemplo, acceso clínico, listar permisos uno por uno y probar denegación de los no autorizados. No modificar ahora PR #5 ni iniciar B01. |
| Temporales con caducidad de 24 h (opción B). | Antes de datos reales; Miguel implementa fecha/migración y unitarias, Lucía verifica. | Decisión de Lucía del 12-oct, nota posterior ADR-05. B00 conserva A con datos ficticios; no implementar B ahora ni considerar resuelto R04. |
| CI: build, pruebas y controles; medir minutos, runner y duración (A-06). | Desde B01; Miguel configura, Lucía verifica. | Visibilidad pública ya aprobada. La medición espera a CI. B01 autorizado expresamente por Lucía el 13-oct; no se acredita CI aún. |
| Funciones completas de acceso, pacientes, agenda, pago y bajas. | B01–B06 según prioridad autorizada; Miguel/Dulce implementan, Lucía prueba. | El primer avance conserva registrar y buscar pacientes desde Pacientes. B00 valida mecanismos y riesgos; no autoriza esos incrementos. |
| Directorio como pantalla aparte, fuera de CU03. | B07, posterior al primer avance de diciembre; decisión de Dulce del 08-oct. | Lista completa de vigentes por defecto y filtro autorizado de bajas; campos y prioridad posterior por validar. No sustituye la búsqueda mínima de Pacientes. |
| Recorrido completo Playwright Q16. | Desde B04; Lucía, revisión Miguel/Dulce. | No exigible como recorrido completo inexistente en B00. |
| Rendimiento/carga Q12, uso Q13, demo Q14 y reproducción Q15 final. | B08; Lucía coordina, otro integrante reproduce. | Recuperación con carga objetivo y funcionamiento completo siguen pendientes; Q15 básico del entorno sí figura en §7. |
| Repo privado, respaldo diario con age, validación antes de cifrar, retención 14 días e incidencia. | Solo tras autorización expresa de implementación. | Decisión documental tomada; no operar nube ni declarar Q10–Q11 completos en producción. |
| Simulacro mensual: descifrar, restaurar y medir. | Lucía, cuando el respaldo real esté autorizado y disponible. | El flujo diario solo usa la clave pública y no comprueba descifrado; recorrido mensual aún no ejecutado. |
| Generar/custodiar claves según la decisión; concretar rotación y política de sesiones tras incidente. | Lucía custodia privada fuera de GitHub; Miguel conserva copia; solo implementar cuando se autorice. | Custodia decidida, todavía no ejecutada. La copia restaura Identity; la cookie antigua usada en QA no fija la política tras incidente real. |
| TLS Neon, Render y controles del despliegue. | Solo con autorización expresa. | Q19 local se prueba sin nube; no se presumen servicios configurados. |
| R04, datos reales, TOTP y recuperación; expediente/notas/reportes/capturista. | Fuera de B00; Lucía decide con respuestas de la usuaria y equipo. | R04 sigue abierto. Solo datos ficticios; no trasladar estos pendientes a una falsa aprobación actual. |
| Skills adaptadas. | PR posterior autorizado. | No bloquean por sí solas el trabajo del equipo. |

> **Documentación:** ajustes en `docs/reviews/`, B00 en `docs/planning/`, pruebas/evidencia en `docs/testing/`, reglas en AGENTS y entrada técnica en README. El StarUML de 08 es un antecedente: no autoriza la pantalla de auditoría retirada.
>
> **Cierre:** publicar documentos, fusionar un PR parcial y cerrar LCA son decisiones distintas. Conservar los pendientes visibles y actualizar este documento cuando exista nueva evidencia o decisión.
