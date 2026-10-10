# TherapEase — 09/B00 · Plan y estado de validación

**Actualización documental: 10-oct-2026.**

> **B00 y Elaboración/LCA abiertos.** Solo datos ficticios; R04 abierto. No iniciar B01/Construcción ni desplegar sin decisión expresa de Lucía.
>
> **Cómo leer las tablas:** ✅ = la tarea concreta tiene ejecución o decisión registrada; ⏳ = falta completarla o revisarla. Una prueba correcta no significa revisión humana, fusión ni cierre del bloque.

## 1. Estado y documentación

| Entrega | Estado registrado | Referencia |
| --- | --- | --- |
| T1: repositorio y carpetas base | ✅ Aprobado y publicado el 01-oct. | Base `174afe0b6764f2415427f28b04d4627ce4c33b16`. |
| PR #1: pasos 1–3 de Miguel | ✅ Corregido, aprobado y fusionado con Squash and merge. | [PR #1](https://github.com/LuciAguilar/Therapease/pull/1); main `41123f2d2c115a05ff1df944457195a7f671cc17`. |
| PR #3: documentación compartida | ✅ Fusionado el 06-oct. | [PR #3](https://github.com/LuciAguilar/Therapease/pull/3); main `8baf8fbc34890d4710c2b78e920f10b8d3e92cb3`. |
| PR #4: pruebas de Lucía | ⏳ En borrador; revisión humana y aprobación/fusión pendientes. | [PR #4](https://github.com/LuciAguilar/Therapease/pull/4), `revision/etapa09`; código probado `83328783b2ea4f7487d28e2935e3c81acb8e521b`. |
| Arquitectura del 08-oct | ⏳ Pruebas preparadas y ejecutadas: 28 correctas y 2 fallidas; excepción Web → Domain aprobada, ajustes y revisión de Miguel pendientes. | §4.3; cambios locales aún sin publicar. |
| Horas y Docker del 08–09-oct | ✅ 12 correctas; reinicio real y claves/sesión conservadas. | §4.4; ampliación local para el mismo PR #4, revisión pendiente. |
| HTTPS local del 10-oct | ✅ 12 correctas, 0 fallos/omitidas; TLS 1.3 negociado. | §4.5; avance local para el mismo PR #4, cabeceras y revisión pendientes. |
| Q03 por API del 10-oct | ⏳ 47 casos nuevos: 46 correctos y 1 fallo real de filtro. | §4.6; además 12 HTTPS repetidos correctamente. Ajuste propuesto para Miguel y revisión pendientes. |
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
| Repositorio público | Aprobado | Documentación seleccionada y datos ficticios. Protección contra secretos aún por comprobar. |
| Destino de respaldo | Opción A aprobada | Repositorio privado aparte; no crear ni configurar todavía. |
| Cabeceras S-30 | Propuesta pendiente de Lucía | Inventario y propuesta en §4.5; no se implementó una política nueva ni se aprobó por ejecutar QA. |
| Cifrado/formato y antigüedad de copia | Propuesta de QA en ADR-05 | AES-256-GCM y clave efímera usados en pruebas. Restaurar la copia íntegra disponible aunque supere 24 h y reportar Q10; decisión de implementación definitiva pendiente. |

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

| Lugar | Cambio a realizar | Comprobación necesaria |
| --- | --- | --- |
| `src/Web/Seguridad/Manejadores/ManejadorDeRequisitoDePermiso.cs:14` | Pedir el permiso a `IAutorizacion.TienePermisoAsync`, sin llamar directamente a `MatrizDePermisos`. | Permiso vigente, usuario activo y temporal restringida; sin cambiar permisos aprobados. |
| `src/Web/Seguridad/ConfiguracionDeSeguridad.cs:46` | Al inyectar `IAutorizacion`, registrar el manejador por solicitud, compatible con ese servicio; hoy es Singleton. | Arranque y solicitudes sin mezclar servicios de distinta duración. |
| `src/Application/`, fuera de Pacientes/Citas | Implementar el coordinador mínimo CU04 que consulta citas activas antes de dar de baja al paciente, con contratos públicos y sin ciclos. | Conservar protección PostgreSQL, concurrencia y cambio/evento atómicos. No cancelar automáticamente ni decidir la política de citas existentes pendiente con Dulce. |

**Reproducción:** desde la raíz del repo:

```powershell
dotnet test tests/TherapEase.IntegrationTests/TherapEase.IntegrationTests.csproj -c Release --filter FullyQualifiedName~ArquitecturaTests --logger "trx;LogFileName=b00-arquitectura.trx"
```

> **Salida actual 1 por los dos hallazgos reales.** Las pruebas no se omiten ni fuerzan un fallo artificial: comprueban las reglas y seguirán fallando hasta corregir el código. `ArquitecturaTests.cs` contiene los casos y `ArquitecturaRevision.cs` su lectura de proyectos/código compilado. Pruebas comentadas según el skill del proyecto.
>
> **Trazabilidad:** base de trabajo `2d088e2c4cc3e2c6f7dc279452713fd1bbf3bced`, misma rama `revision/etapa09`; cambios aún locales para la entrega conjunta del PR #4. No cambia backend, migraciones, proyectos ni paquetes. Ejecución anterior: 21 casos, 19 correctos/2 fallidos; la revisión detectó que la regla era demasiado estricta para enumeraciones. La aprobación del 08-oct permite esa excepción; el resultado vigente es el de 30 casos. No se repitieron aquí las 50 pruebas de Identity/recuperación.
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
| Código base | 2d088e2c4cc3e2c6f7dc279452713fd1bbf3bced; nuevas pruebas locales en revision/etapa09, sin publicar. |
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

[Pruebas HTTPS](../../tests/TherapEase.IntegrationTests/HttpsLocalTests.cs) y [entorno temporal](../../tests/TherapEase.IntegrationTests/HttpsLocalRevision.cs). La evidencia automática queda en TestResults/https y se excluye de Git. TRX de esta ejecución: b00-https-final-10oct.trx, SHA-256 dd0dd08048c161a5cf2d7dd1fde06ee92d98f94c5c44edc89282d6b1193a3b46. Base local 47ca601; ampliación para el mismo PR #4 aún sin subir. Pruebas comentadas según el skill del proyecto. Auditoría HTTPS recibida de Claude: APROBADO, sin correcciones obligatorias; revisión humana pendiente. La ampliación Q03 de §4.6 tiene auditoría pendiente.

> La redirección no protege datos que un cliente ya haya enviado por HTTP. La prueba envía las credenciales de sesión únicamente por HTTPS. HSTS necesita un cliente compatible y una primera conexión segura; ASP.NET Core excluye localhost por defecto. [Referencia oficial de Microsoft](https://learn.microsoft.com/en-us/aspnet/core/security/enforcing-ssl?view=aspnetcore-10.0).

**Cabeceras S-30 — propuesta para decisión de Lucía:** inventario de salud y consulta autenticada con renovación, no de todas las páginas/respuestas. No hay corrección obligatoria atribuida al equipo por una política aún sin aprobar.

| Cabecera | Observado | Propuesta pendiente |
| --- | --- | --- |
| X-Content-Type-Options | Ausente | nosniff, con Content-Type correcto. |
| Content-Security-Policy | Ausente | frame-ancestors 'none'; completar restricciones de recursos después de validar las páginas de Dulce. |
| X-Frame-Options | Ausente | DENY como apoyo a navegadores antiguos, coherente con frame-ancestors. |
| Referrer-Policy | Ausente | no-referrer, para evitar enviar la dirección de origen a otros sitios. |
| Cache-Control | no-store, no-cache en las dos respuestas observadas | Exigir no-store en contenido privado autenticado, incluida respuesta sin renovación; sin aplicarlo indiscriminadamente a archivos estáticos. |

Propuesta fundamentada en [OWASP HTTP Headers](https://cheatsheetseries.owasp.org/cheatsheets/HTTP_Headers_Cheat_Sheet.html). Lucía decide; Miguel/Dulce implementan su parte y Lucía prueba. No se añade política por iniciativa de QA. Navegador, antifalsificación, configuración del despliegue y cierre B00/LCA siguen pendientes.

### 4.6. Consulta Q03 por API — ejecutada el 10-oct

| Comprobación | Resultado ejecutable | Alcance / pendiente |
| --- | --- | --- |
| Ejecución | ⏳ 47 nuevos: 46 correctos, 1 fallido, 0 omitidos. Más 12 HTTPS correctos: total 59, 58 correctos y 1 fallido, 16,01 s con preparación. | Release sin advertencias reportadas. Servidor real por HTTPS, PostgreSQL desechable y datos ficticios; sin repetir las 50 anteriores, arquitectura ni horas/Docker. |
| Filtros válidos | ✅ Periodo incluye inicio/excluye fin; orden descendente; tipo, identificador y combinación de ambos; sin coincidencias devuelve lista vacía. | Cinco eventos conocidos dentro del periodo y dos fuera; no depende de la zona del equipo. |
| Límites | ✅ 1, 100, 499 y 500 devuelven exactamente los más recientes de 501 eventos. Sin límite: 100. Cero, negativo, 501, decimal, texto y desbordamiento: 400. | No es una prueba de rendimiento ni paginación. |
| Entradas inválidas | ⏳ Identificador mal formado, fechas ausentes/iguales/invertidas y varios tipos inválidos: 400. **Paciente,Cita devuelve 200 con un evento Cita.** | Debe rechazarse la lista con 400. Hallazgo de validación, sin eludir permisos; corrección propuesta para Miguel, pendiente de auditoría/aprobación. |
| Permiso actual | ✅ Sin cookie, inválida, desactivada y rol cambiado: 401. Usuaria y superusuario temporal: 403, sin eventos. | Sesiones reales preparadas con Identity; baja y cambio de rol por servicios reales. Acceso/comando final del operador siguen pendientes. |
| Solo lectura | ✅ GET no cambia filas de auditoría; POST/PUT/PATCH/DELETE: 405 y filas idénticas. | Huella de todas las filas antes/después; permisos INSERT/UPDATE/DELETE de PostgreSQL conservan su evidencia anterior. |
| Contenido | ✅ Solo ocho campos de metadatos, actor/fecha/registro/acción correctos; sin nombre/contacto, usuario, contraseña, hash, sello, cookie, conexión ni clave de sesión ficticios buscados. | Se buscan también cadenas JSON descodificadas. Eventos paciente/cita preparados por QA; no acredita todos los productores de eventos ni textos clínicos, que no existen en este alcance. |
| Preparación y limpieza | ✅ Se corrigió el reloj de preparación; sesiones usan hora real y eventos de administración fecha fija. Procesos, contenedores y PFX retirados. | Primer intento conservado: Q03 no llegó a ejecutarse por sesiones vencidas en QA; no es un fallo de producción. |

**Reproducir Q03:** Docker Desktop iniciado; desde la raíz del repo:

```powershell
dotnet test tests/TherapEase.IntegrationTests/TherapEase.IntegrationTests.csproj -c Release --filter FullyQualifiedName~ConsultaAuditoriaApiTests --logger "trx;LogFileName=b00-q03.trx"
```

[Casos API](../../tests/TherapEase.IntegrationTests/ConsultaAuditoriaApiTests.cs) y [datos/entorno](../../tests/TherapEase.IntegrationTests/ConsultaAuditoriaRevision.cs). La prueba de **Paciente,Cita permanece fallida**, sin omitirla ni cambiar el resultado esperado. [Ajuste propuesto para Miguel, §7](../reviews/TherapEase-09-B00-revision-PR01.md). Para comprobarlo, GET /api/auditoria/eventos con fechas válidas y tipoRegistro=Paciente%2CCita, usando una sesión ficticia de superusuario por HTTPS: esperado 400; observado 200 y tipo Cita. Enum.TryParse acepta listas y combina sus valores; IsDefined no basta si el resultado combinado coincide con otro valor válido. [Documentación de Microsoft](https://learn.microsoft.com/en-us/dotnet/api/system.enum.tryparse?view=net-10.0).

**Evidencia:** b00-q03-https-entrega-10oct.trx, SHA-256 17e9fe894f1d4895c171ec9089019b3b20f19af1555964892747376155ddb3eb. Resumen del hallazgo en tipo-combinado.json, sin valores de sesión. Base local e97646d; mismo PR #4, avance sin subir. Pruebas comentadas según el skill del proyecto. Auditoría de Q03 y revisión humana pendientes; B00/LCA/R04 abiertos.

## 5. Cobertura y alcance de los pendientes

| Área de B00 | Avance | Lo que aún falta | Dónde seguir |
| --- | --- | --- | --- |
| Arquitectura | Pruebas automáticas ejecutadas: 30, con 28 correctas y 2 fallidas (§4.3). | Corregir llamada Web → MatrizDePermisos, implementar/probar CU04, repetir sobre páginas funcionales y revisión de Miguel. | §7, paso 1. |
| Datos Q03/Q06/Q07/Q08/Q20 | Migraciones/permisos y escenarios ejecutados; consulta API Q03: 46 correctos y 1 fallo (§4.6). | Corregir filtro combinado, acceso/comando de consulta y demás operaciones, estados, concurrencia, conflictos e interrupciones. | §7, paso 2. |
| Horas Q09 y Docker | ✅ Imagen, horas, API, EF/PostgreSQL y reinicio probados (§4.4). | Revisión/reproducción por otra persona; horas en páginas/navegador cuando existan. | §7, pasos 1–3. |
| Identidad y seguridad Q19 | Servidor, reinicio y HTTPS local probados (§4.5); revisión acotada de secretos. | PageModel, navegador, decisión/verificación de cabeceras y revisión humana. | §7, paso 3. |
| Recuperación Q10–Q11 | Procedimiento local corregido, ejecutado. | Revisión, decisiones de implementación y límites de operación registrados. | §7, paso 4; operación posterior en §9. |
| Decisión LCA | Evidencia parcial disponible. | Completar validación, revisión distinta y decisión expresa de Lucía. | §7, paso 4. |

> **Cobertura documental:** §7 reúne los pendientes conocidos de B00 contrastados con 08B–08D, Q/R, AGENTS, plan de pruebas y evidencia. No garantiza que no aparezcan hallazgos nuevos. Las funciones completas de B01–B08 y los pendientes de uso real se distinguen en §9; no se autorizan ni se dan por hechos aquí.

## 6. Siguientes pasos compartidos

| Orden de trabajo | Quién | Qué falta / dependencia |
| --- | --- | --- |
| Revisar la entrega conjunta de QA del PR #4 | Miguel o Dulce; Lucía decide fusión | Autor distinto del revisor. No necesita esperar a los PageModel. |
| **Arquitectura: resolver hallazgos del paso 1** | Miguel implementa/corrige y revisa las pruebas; Lucía repite | Pruebas ejecutadas: faltan acceso Web → MatrizDePermisos y coordinador CU04. No se da por cerrada la validación (§4.3). |
| Corregir filtro Q03 y repetir | Miguel propone/implementa; Lucía prueba | Rechazar listas de tipos como Paciente,Cita; hallazgo/ajuste en §4.6, auditoría y aprobación pendientes. |
| Datos restantes y revisión de horas/Docker | Lucía prueba; Miguel implementa/corrige y otro integrante revisa | Horas/Docker ejecutados en §4.4; datos por servicios esperan implementación/políticas donde corresponda. |
| Acceso mínimo y producto | Dulce implementa/valida | Unitarias propias, respuestas reales de la usuaria y frontend afectado validado. |
| Navegador y cabeceras de seguridad | Lucía decide/prueba; Miguel/Dulce implementan | HTTPS local ya probado (§4.5); faltan páginas, decisión S-30 y comprobación de su implementación. |
| Consolidar evidencia y decidir LCA | Lucía, con revisión humana distinta | Solo después de completar la validación; no autorizar B01 por una fusión parcial. |

## 7. Plan de validación autorizado

> **Esta es la lista de trabajo compartida.** Cada fila indica persona, avance y dependencia. No se repiten como pendientes las pruebas ya ejecutadas. Una fila de revisión sigue ⏳ aunque el código de prueba pase.
>
> Los códigos como **A-01** son referencias del [plan de pruebas, sección «B00 — Validación de arquitectura»](../testing/TherapEase-09-plan-de-pruebas.md); **no son pasos nuevos**. A-01 corresponde a la prueba de límites del **paso 1** siguiente.

### Paso 1. Base y límites — arquitectura, Docker y entorno

| Estado | Tarea | Responsable | Dependencia / evidencia necesaria |
| --- | --- | --- | --- |
| ✅ | Crear repositorio y carpetas base; publicar documentos seleccionados. | Lucía | T1 publicado. |
| ✅ | Crear solución, siete proyectos y base Razor Pages/Docker. | Miguel | PR #1 fusionado; build y arranque históricos en §4. |
| ✅ | Escribir y ejecutar pruebas automáticas de capas/módulos, ciclos y contratos; comprobar que detectan infracciones. | Lucía | 30 casos: 28 correctos y 2 fallidos; nueve controles nuevos de la excepción aprobada (§4.3). |
| ⏳ | Resolver la llamada directa Web → MatrizDePermisos y repetir las pruebas de límites. | Miguel corrige; Lucía prueba | No basta que las referencias de proyectos sean correctas; ver archivos y cambios en §4.3. |
| ⏳ | Revisar humanamente las pruebas de arquitectura. | Miguel | Revisor distinto de Lucía, como establece ADR-06; aún sin revisión. |
| ⏳ | Implementar coordinador CU04 en Application, fuera de Pacientes/Citas. | Miguel | No introducir dependencia inversa ni ciclo; pertenece a validación B00, no autoriza B02 completo. |
| ⏳ | Probar ubicación y llamadas reales de CU04; PageModel funcionales sin persistencia directa. | Lucía; Miguel revisa | Ubicación falla por coordinador ausente; la única página actual, ErrorModel, sí pasa. Depende de Miguel y de las páginas de Dulce. |
| ✅ | Repetir imagen, arranque, salud y ejecución sin privilegios para el código vigente. | Lucía prueba; Miguel corrige | Imagen actual construida; HEALTHCHECK healthy, HTTP 200 y usuario no root (§4.4). |
| ✅ | Comprobar `America/Hermosillo` dentro de la imagen. | Lucía prueba; Miguel corrige | DLL real de imagen ejecutada bajo UTC, Nueva York y Madrid; 6 fechas por zona (§4.4). |
| ⏳ | Reproducir entorno y verificaciones con la documentación. | Miguel o Dulce; Lucía verifica | Una persona distinta de la autora del procedimiento; evidencia identificada (Q15). |
| ⏳ | Comprobar protección contra secretos; conservar revisión exigida en main. | Lucía | Una revisión en main comprobada el 03-oct; protección de secretos sin verificar. Visibilidad pública ya decidida. |

### Paso 2. Datos — persistencia, concurrencia y horas

| Estado | Tarea | Responsable | Dependencia / evidencia necesaria |
| --- | --- | --- | --- |
| ✅ | Implementar persistencia, migraciones y usuarios separados de PostgreSQL. | Miguel | PR #1 fusionado. |
| ✅ | Probar migraciones desde cero, permisos y auditoría solo de inserción. | Lucía | Repetido dentro de las 50: `INSERT` permitido; `UPDATE`/`DELETE`/alteración denegados. |
| ✅ | Probar cruces SQL, dos reservas simultáneas, versión EF y conservación inicial del paciente. | Lucía | Alcance parcial de los 11 incorporados; no cubre todos los servicios/estados. |
| ✅ | Probar filtros válidos, límites 1–500, contenido, permisos actuales y solo lectura de la API Q03. | Lucía | 46 casos correctos por HTTPS; alcance y evidencia en §4.6. |
| ⏳ | Rechazar listas de tipos y repetir Q03 sin el fallo. | Miguel corrige tras revisión/aprobación; Lucía prueba | Paciente,Cita devuelve 200/Cita en vez de 400. Propuesta de ajuste en informe §7; no se omite la prueba. |
| ⏳ | Completar/probar acceso y comando autenticado de lectura Q03. | Miguel/Dulce implementan su parte; Lucía prueba | La API ya exige permiso actual; sesiones de QA no sustituyen el procedimiento final del operador. Sin pantalla de auditoría. |
| ⏳ | Probar cambio y evento atómicos en restantes operaciones; sin datos sensibles ni éxito ante fallo. | Lucía prueba; Miguel corrige | Identity ya tiene casos; ampliar pacientes, citas y pago en la validación disponible. |
| ⏳ | Probar reservas y recuperación de cita agendada por servicios; estados alternos y nuevas comprobaciones de cruce. | Lucía prueba; Miguel implementa/corrige | Paciente vigente; cancelada/de baja no bloquea; políticas contigua/pago pendientes de Dulce. |
| ⏳ | Probar agendar frente a baja de paciente y recuperar frente a otra reserva, con cambios simultáneos. | Lucía prueba; Miguel implementa/corrige | CU04 y política de citas existentes; nunca debilitar el bloqueo PostgreSQL. |
| ⏳ | Probar conflictos de edición en otros registros; avisar sin sobrescribir. | Lucía prueba; Miguel implementa/corrige | Servicios disponibles y concurrencia controlada; versión EF aislada no basta. |
| ⏳ | Probar baja/consulta/recuperación autorizada, sin borrado físico y fuera de listados ordinarios. | Lucía prueba; Miguel/Dulce implementan su parte | Conservación inicial probada; faltan servicios, filtros y caminos restantes (Q20). |
| ⏳ | Interrumpir antes/después del commit; comprobar resultado incierto, reintento y ausencia de duplicados. | Lucía prueba; Miguel implementa/corrige | No confundir reversión por auditoría con pérdida de respuesta (Q08). |
| ✅ | Probar conversión explícita a Hermosillo desde otras zonas; separar fecha sin hora de instante, en servidor/imagen. | Lucía prueba; Miguel corrige | 18 conversiones en imagen, API rechaza entradas sin zona y EF/PostgreSQL conserva la cita; interfaz/navegador siguen en paso 3 (§4.4). |
| ⏳ | Revisar SQL, permisos, impacto y reversión de nuevas migraciones de esta validación. | Miguel prepara; Lucía revisa/prueba | Solo si hay nuevas migraciones; no dar por ejecutado un rollback no probado. |

### Paso 3. Identidad y seguridad

| Estado | Tarea | Responsable | Dependencia / evidencia necesaria |
| --- | --- | --- | --- |
| ✅ | Implementar Identity inicial y condiciones aprobadas. | Miguel | PR #1 fusionado. |
| ✅ | Probar hashes, longitud, bloqueo, temporal, revocación, rol y último superusuario concurrente. | Lucía | 37 casos, incluidos 11 anteriores; repetidos dentro de 50 correctos del 08-oct. |
| ✅ | Probar cookies/caducidad en componentes, claves en PostgreSQL, comando local y fallo de auditoría de Identity. | Lucía | Reloj controlado; no acredita navegador, TLS o reinicio real. |
| ✅ | Revisar secretos/configuración, registros capturados y `.env.example`. | Lucía | Alcance acotado e imagen histórica; mantener revisión por cada cambio. |
| ⏳ | Revisar humanamente las pruebas del PR #4. | Miguel o Dulce | Persona distinta de Lucía; falta revisión, no ejecución de esas 50. |
| ⏳ | Concretar verificación y entrega segura de temporales a otros usuarios, sin correo ni registros de contraseñas. | Miguel propone; Lucía decide; Dulce integra páginas | HU03/CU02; el comando del primer superusuario no resuelve por sí solo la entrega a otros usuarios. |
| ⏳ | Implementar acceso mínimo/PageModel y sus unitarias. | Dulce | Validar frontend afectado; llamadas a Application. Si falla auditoría, mostrar «no se guardó». |
| ⏳ | Probar entrada/salida, temporal, cambio/restablecimiento y rol desde las páginas. | Lucía | Depende de PageModel de Dulce; cuentas ficticias y distintas sesiones. |
| ⏳ | Probar cookies reales y renovación por actividad; cookie manipulada/ausente, redirección externa y caché tras salir. | Lucía | Navegador y acceso mínimo; no reutilizar el resultado de componentes como prueba de páginas. |
| ⏳ | Probar antifalsificación, permisos por URL/envío directo y validación de las rutas disponibles. | Lucía prueba; Miguel/Dulce corrigen | PageModel disponibles; ampliar con las rutas de cada bloque posterior. |
| ⏳ | Revisar errores, entradas/salidas y datos rastreables de la validación ampliada. | Lucía prueba; Miguel/Dulce corrigen | Sin datos privados ni secretos en eventos, respuestas o registros; no acreditar toda 09. |
| ✅ | Probar HTTPS local, certificado, redirección, cookie renovada y HSTS de modo Production local. | Lucía | 12 correctas; servidor real y PostgreSQL desechable. Sin navegador ni despliegue (§4.5). |
| ⏳ | Decidir cabeceras exigidas y comprobar la implementación aprobada. | Lucía decide/prueba; Miguel/Dulce configuran su parte | S-30: inventario y propuesta en §4.5; no autoriza nube. |
| ✅ | Reiniciar el contenedor de aplicación y comprobar continuidad de claves/sesión, sin claves en disco. | Lucía prueba; Miguel corrige | Reinicio real, cookie previa y evento correctos; claves PostgreSQL iguales y sin key-*.xml observados en /app, /home/app y /tmp. No acredita TLS (§4.4). |

### Paso 4. Recuperación y decisión

| Estado | Tarea | Responsable | Dependencia / evidencia necesaria |
| --- | --- | --- | --- |
| ✅ | Probar copia cifrada, huella, otro PostgreSQL, datos/permisos/Identity y consulta restaurados. | Lucía | 13 casos; 13 tablas iguales en la muestra local. |
| ✅ | Probar copia alterada, clave incorrecta y fallos de copia/restauración; medir pérdida y recuperación. | Lucía | 7,375 s desde detección simulada; no acredita operación diaria. |
| ✅ | Aplicar correcciones de Claude y repetir las 50; restaurar copia íntegra de 25 h e informar Q10. | Lucía | Ejecución del 08-oct, 0 fallos/omitidas. |
| ⏳ | Revisar las pruebas de recuperación y reproducir sus resultados. | Miguel o Dulce | Revisión distinta del autor; puede integrarse con Q15 del paso 1. |
| ⏳ | Revisar/decidir la propuesta ADR-05: cifrado/formato, antigüedad y pendientes operativos. | Lucía | Auditoría personal de Claude cuando corresponda; no hay decisión definitiva de producción atribuida. |
| ⏳ | Resolver o registrar hallazgos y aprobar/fusionar el PR #4. | Lucía; Miguel o Dulce revisa las pruebas | La aprobación parcial no cierra B00. |
| ⏳ | Consolidar evidencia Q06–Q11/Q19, límites y riesgos residuales; decidir B00/LCA. | Lucía | Resultados ejecutables, revisión humana distinta y pendientes de esta puerta tratados expresamente; no cierre automático. |

## 8. Consulta Q03, respaldo y mejoras propuestas

| Tema | Regla / situación |
| --- | --- |
| Consulta técnica Q03 | API de lectura por HTTPS probada en §4.6: filtros válidos, límites, metadatos y denegaciones. Un fallo de tipo combinado pendiente; comando/acceso final del operador y revisión humana pendientes. Sin pantalla ni acceso directo del cliente a PostgreSQL. |
| Respaldo aprobado | Opción A: repo privado aparte; `pg_dump` cifrado, SHA-256, 14 días, restauración desechable e incidencia asignada a Lucía ante fallo. Sin crear repo, conectar Neon ni desplegar. |
| Cabeceras S-30 | HTTPS local probado; política adicional propuesta en §4.5, pendiente de decisión e implementación. |
| Propuesta ADR-05 | La prueba local no decide custodia duradera, formato definitivo ni operación diaria. Esos límites deben quedar explícitos en la decisión de LCA. |

| Mejora opcional ya registrada | Quién podría aplicarla | Situación |
| --- | --- | --- |
| Comprobar resultados de `UpdateAsync`, `AddToRoleAsync` y `RemovePasswordAsync`. | Miguel | Propuesta del informe PR01; no confundir con corrección obligatoria pendiente. |
| Ámbitos de Paciente como colección de solo lectura; aclarar alcance de `/salud`. | Miguel | Propuestas del informe PR01. |
| Comprobar el evento HTTP restaurado además del 200; documentar `crear-roles.sql` para servidor nuevo. | Lucía en pruebas/documentación | Opcionales de Claude, aún sin aplicar. |
| Correo noreply en futuros commits. | Cada autor | Recomendación; no incluye reescribir historial publicado. |

## 9. Publicación, trabajos posteriores y límites

| Pendiente que continúa | Cuándo / quién | Relación con B00 |
| --- | --- | --- |
| CI: build, pruebas y controles; medir minutos, runner y duración (A-06). | Desde B01; Miguel configura, Lucía verifica. | Visibilidad pública ya aprobada. La medición espera a que exista CI; no está ejecutada ni autoriza B01. |
| Funciones completas de acceso, pacientes, agenda, pago y bajas. | B01–B06 según prioridad autorizada; Miguel/Dulce implementan, Lucía prueba. | El primer avance conserva registrar y buscar pacientes desde Pacientes. B00 valida mecanismos y riesgos; no autoriza esos incrementos. |
| Directorio como pantalla aparte, fuera de CU03. | B07, posterior al primer avance de diciembre; decisión de Dulce del 08-oct. | Lista completa de vigentes por defecto y filtro autorizado de bajas; campos y prioridad posterior por validar. No sustituye la búsqueda mínima de Pacientes. |
| Recorrido completo Playwright Q16. | Desde B04; Lucía, revisión Miguel/Dulce. | No exigible como recorrido completo inexistente en B00. |
| Rendimiento/carga Q12, uso Q13, demo Q14 y reproducción Q15 final. | B08; Lucía coordina, otro integrante reproduce. | Recuperación con carga objetivo y funcionamiento completo siguen pendientes; Q15 básico del entorno sí figura en §7. |
| Repo privado, respaldo diario, retención 14 días, restauración programada e incidencia. | Tras autorización; implementación por concretar, Lucía decide/verifica. | Demostración local hecha; no operar nube ni declarar Q10–Q11 completos en producción. |
| Custodia/rotación de claves duraderas y política de sesiones tras incidente. | Decisión de Lucía; Miguel implementa, Lucía prueba cuando se autorice. | La copia restaura Identity; la cookie antigua usada en QA no fija la política tras incidente real. |
| TLS Neon, Render y controles del despliegue. | Solo con autorización expresa. | Q19 local se prueba sin nube; no se presumen servicios configurados. |
| R04, datos reales, TOTP y recuperación; expediente/notas/reportes/capturista. | Fuera de B00; Lucía decide con respuestas de la usuaria y equipo. | R04 sigue abierto. Solo datos ficticios; no trasladar estos pendientes a una falsa aprobación actual. |
| Skills adaptadas. | PR posterior autorizado. | No bloquean por sí solas el trabajo del equipo. |

> **Documentación:** ajustes en `docs/reviews/`, B00 en `docs/planning/`, pruebas/evidencia en `docs/testing/`, reglas en AGENTS y entrada técnica en README. El StarUML de 08 es un antecedente: no autoriza la pantalla de auditoría retirada.
>
> **Cierre:** publicar documentos, fusionar un PR parcial y cerrar LCA son decisiones distintas. Conservar los pendientes visibles y actualizar este documento cuando exista nueva evidencia o decisión.
