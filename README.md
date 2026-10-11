# TherapEase — Guía de arranque del equipo en 09

**Actualización documental: 10-oct-2026.** La base de esta guía, AGENTS, ADR-01…06 y el plan B00 fueron aprobados el 01-oct. Esta actualización separa ajustes para el equipo de las notas personales y alinea el estado parcial de B00. Los cambios de documentación inicial pueden comunicarse a Lucía; no se acredita una revisión no recibida.

**T1 aprobado y publicado:** Lucía aprobó los 22 archivos de documentación, reglas y carpetas base el 01-oct-2026 y realizó el primer push a [LuciAguilar/Therapease](https://github.com/LuciAguilar/Therapease), repositorio público. La validación ejecutable de B00 y la decisión de cierre de LCA siguen pendientes.

## 1. Estado y alcance

- **Autorizado:** iniciar 09 por B00, implementar lo necesario para validar el stack y registrar evidencia. Lucía aprobó el plan corregido, ADR-06, la fidelidad de ADR-01…05 corregidos y la consulta Q03 sin pantalla. Eligió repositorio público y autorizó instalar .NET 10.
- **Avance parcial incorporado:** PR #1 corregido, revisado por Lucía y fusionado en main con `41123f2d2c115a05ff1df944457195a7f671cc17`. Incluye solución/proyectos, backend inicial, migraciones e identidad. Hay resultados parciales, resumidos en B00; la validación completa sigue pendiente. B01–B08 no quedan autorizados por esta fusión.
- **Producto:** primer recorrido previsto: acceso → paciente → cita → pago pagado/pendiente, sin montos ni cobros. B00–B04 al 04-dic-2026 es un corte propuesto que Dulce debe validar con la usuaria. B05–B06 dependen de prioridad y capacidad; el directorio B07 queda para después del primer avance de diciembre; B08 recoge evidencia realmente obtenida.
- **Cambio vigente:** no hay pantalla de auditoría. Q03 registra eventos en base y permite consulta técnica autorizada de solo lectura. **Decisión de Dulce del 08-oct:** registrar y **buscar pacientes desde Pacientes** permanece en el primer avance de diciembre. El directorio sale de CU03 y pasa a una pantalla aparte posterior (B07), con la lista completa de vigentes por defecto y filtro autorizado de bajas; sus campos visibles siguen pendientes.
- **Datos:** solo ficticios. R04 sigue abierto. Expediente, notas, reportes, capturista, correo e integraciones están diferidos. TOTP es obligatorio antes de datos reales, con revisión del impacto en ADR-05.
- **Forma de trabajo:** solo Lucía elabora con Codex y audita con Claude. Miguel y Dulce eligen herramientas, asistentes y organización de sus revisiones; la documentación les sirve de contexto y soporte. Las reglas compartidas de revisión entre personas y aprobación están en [AGENTS.md](AGENTS.md).

## 2. Mapa de documentos del repositorio

| Documento | Ruta en el repo | Para qué sirve |
| --- | --- | --- |
| Reglas compartidas | [AGENTS.md](AGENTS.md) | Seguridad, datos, pruebas, revisión y límites obligatorios para todos. |
| Entrada del equipo | [README.md](README.md) | Guía, responsabilidades y preparación del entorno; conserva también los comandos técnicos. |
| Requisitos arquitectónicos | [docs/requirements/TherapEase-requisitos-arquitectonicos.md](docs/requirements/TherapEase-requisitos-arquitectonicos.md) | Q01–Q20 y criterios medibles. |
| Historias y casos de uso | [docs/requirements/TherapEase-etapa08-casos-historias.md](docs/requirements/TherapEase-etapa08-casos-historias.md) | Permisos, aceptación y caminos básicos/alternos. |
| Modelos e interfaces | [docs/requirements/TherapEase-etapa08-modelos-interfaces.md](docs/requirements/TherapEase-etapa08-modelos-interfaces.md) | Entidades, relaciones y propuestas de pantallas/campos. |
| Backlog y trazabilidad | [docs/planning/TherapEase-etapa08-backlog-trazabilidad.md](docs/planning/TherapEase-etapa08-backlog-trazabilidad.md) | B00–B08 y relación con Q/R/ADR. |
| Plan y estado B00 | [docs/planning/TherapEase-etapa09-B00.md](docs/planning/TherapEase-etapa09-B00.md) | Resultados, pendientes, siguientes pasos y puerta LCA. |
| Plan de pruebas | [docs/testing/TherapEase-09-plan-de-pruebas.md](docs/testing/TherapEase-09-plan-de-pruebas.md) | Casos por persona y caso de uso. |
| Ajustes del PR #1 | [docs/reviews/TherapEase-09-B00-revision-PR01.md](docs/reviews/TherapEase-09-B00-revision-PR01.md) | Correcciones concretas y mejoras propuestas para Miguel/Dulce. |
| ADR-01…05 | [docs/architecture/adr/ADR-01-a-05-provisionales.md](docs/architecture/adr/ADR-01-a-05-provisionales.md) | Decisiones arquitectónicas y sus límites. |
| ADR-06 | [docs/architecture/adr/ADR-06-monorepo.md](docs/architecture/adr/ADR-06-monorepo.md) | Monorepo y validación de dependencias. |
| StarUML | [docs/diagrams/TherapEase-08C-StarUML.mdj](docs/diagrams/TherapEase-08C-StarUML.mdj) | Dominio y casos de uso editables; antecedente de 08, anterior al cambio de pantalla. |

Las adendas de 09 y las decisiones incorporadas a AGENTS rigen el trabajo vigente. Las propuestas de producto marcadas como pendientes siguen pendientes hasta recibir una respuesta; los diagramas de 08 no validan por sí solos el frontend de 09.

## 3. Qué hace cada persona primero

| Persona | Ahora, durante B00 | Entrega y siguiente paso |
| --- | --- | --- |
| **Lucía** | Revisar los cambios críticos y la evidencia parcial; preparar límites, integración crítica y seguridad de B00. Revisar esta actualización documental antes de fusionarla. | PR propio de las 11 pruebas adicionales con revisión de Miguel/Dulce; completar evidencia y decidir LCA. |
| **Miguel** | PR #1 corregido y fusionado. Continuar la validación de B00 con sus unitarias propias y atender los pendientes ejecutables del plan. | Nuevo commit identificable, build y comandos/resultados. Mantener el bloqueo PostgreSQL. Revisa la prueba de límites; él o Dulce revisa las demás pruebas de Lucía. |
| **Dulce** | Validar con la usuaria las políticas provisionales de citas/pago, campos y prioridades. Coordinar contratos con Miguel y preparar PageModel/Razor mínimo para comprobar el acceso de B00. | Registrar respuestas y pendientes reales, unitarias propias y checklist visual. El frontend posterior depende de la decisión LCA y las prioridades acordadas. |

**PageModel asignado:** Lucía confirmó el 04-oct-2026 que lo escribe Dulce. Unitarias en el mismo PR, revisión distinta y aprobación de Lucía, conforme a [AGENTS.md](AGENTS.md).

Orden de lectura compartido: esta guía → B00 → 08D. Después Miguel: ADR → requisitos → 08B/08C → pruebas §1 y B00. Dulce: 08B/08C → pruebas §2 → requisitos aplicables. Lucía: requisitos → plan de pruebas §§3–5 → B00. Cada tarea debe citar su B/HU/CU/Q y criterio de aceptación.

## 4. Stack y estructura

Ver [AGENTS.md — Stack y estructura](AGENTS.md).

## 5. Reglas por cambio

Ver [AGENTS.md — Reglas por cambio](AGENTS.md).

## 6. Pendientes que no deben resolverse por suposición

| Pendiente | Quién lo resuelve | Qué trabajo depende de ello |
| --- | --- | --- |
| Prioridad/corte B00–B04, campos del directorio y paciente, contacto de tutor, ámbitos, duplicados, valor inicial del pago y corrección tras cancelar/baja | Dulce con la usuaria | Pantallas y reglas afectadas. Las propuestas de 08C no son respuestas recibidas. |
| Baja de paciente con citas activas; retirar un ámbito con citas y recuperación posterior | Dulce con la usuaria; Miguel implementa; Lucía prueba | Política funcional antes de implementarla. B00 valida los invariantes aprobados sin adjudicar una regla pendiente. |
| Resuelto el 04-oct: primer superusuario por comando local y restablecimiento sin levantar bloqueo | Lucía aprobó; Miguel implementa; Lucía prueba | Condiciones en [AGENTS.md](AGENTS.md); sin contraseña fija ni correo supuesto. |
| Resuelto el 04-oct: PageModel a cargo de Dulce | Lucía confirmó; Dulce implementa y escribe unitarias | Revisión distinta y aprobación de Lucía. |
| Respaldo: decisión de Lucía del 10-oct registrada | Implementación pendiente de autorización | Opción A, age, validación diaria antes de cifrar y simulacro mensual; reglas vigentes en AGENTS §2.8 y nota posterior ADR-05. No crear repo, conectar Neon ni configurar flujo. |
| Revisión de documentación inicial | Miguel o Dulce comunica observaciones con su propio método; Lucía confirma cambios | Pendiente y no bloqueante; guía, AGENTS y ADR corregidos ya aprobados por Lucía. |
| Revisión de código/pruebas, resultados B00 y decisión LCA | Autor distinto del revisor en cada PR; aprobación final de Lucía | Miguel/Dulce revisa pruebas de Lucía. Construcción continúa pendiente de evidencia y decisión LCA. |
| Acceso, procedencia, conservación y eliminación de datos reales; TOTP | Lucía con la usuaria y responsables técnicos | R04 sigue abierto: no utilizar datos reales. |

GitHub permite descargar artefactos a personas autenticadas con lectura del repositorio; en uno público ese acceso no restringe el respaldo al equipo. El cifrado no convierte el artefacto en privado. [Fuente oficial: descarga de artefactos](https://docs.github.com/en/actions/how-tos/manage-workflow-runs/download-workflow-artifacts). La nota posterior ADR-05 del 10-oct registra la decisión aprobada de cifrado, custodia y validación; su implementación no está autorizada. No se ha creado el repo de respaldo ni configurado el flujo.

## 7. Cómo dejar trabajo revisable sin depender de una llamada

En cada PR: B/HU/CU/Q afectados, qué se implementó, criterios cubiertos, comandos y resultados, pendientes y persona revisora. Adjuntar evidencia ficticia y sin secretos. Miguel documenta cómo arrancar, migrar y reproducir; Dulce deja respuestas de producto y capturas ficticias; Lucía añade resultados de sus pruebas cuando los ejecute. La menor disponibilidad de Lucía permite preparar trabajo revisable, no fusionar cambios críticos sin su revisión ni dar por aprobada LCA.

## 8. Instalar antes de empezar

Abrir PowerShell y comprobar las herramientas. .NET SDK y Docker son necesarios para correr y probar localmente; Git obtiene el código, el editor permite modificarlo y StarUML abre el diagrama editable.

| Herramienta | Comando de comprobación | Resultado esperado |
| --- | --- | --- |
| SDK .NET 10 | `dotnet --list-sdks` | Una entrada `10.0.xxx`; instalar el SDK, no solo el runtime. |
| Docker Desktop | `docker version` | Cliente y servidor disponibles, con contenedores Linux. |
| Git | `git --version` | Versión instalada. |
| VS Code (editor comprobado) | `code --version` | Versión del editor. Si se elige otro, comprobar su versión con su comando. |
| StarUML | `winget list --id MKLabs.StarUML --exact --source winget` | Instalación detectada; abrir el `.mdj` con StarUML. |

Fuentes: [instalar .NET en Windows](https://learn.microsoft.com/en-us/dotnet/core/install/windows), [Docker Desktop para Windows](https://docs.docker.com/desktop/setup/install/windows-install/), [Git](https://git-scm.com/downloads), [VS Code](https://code.visualstudio.com/download) y [StarUML](https://staruml.io/download/).

El PR #1 fusionado ya incluye solución, `.csproj`, migraciones y aplicación mínima; no acredita todavía B00 completo. Los comandos técnicos de Miguel se conservan en [§10 — Construir, probar y ejecutar](#10-construir-probar-y-ejecutar). No instalar PostgreSQL de producción para B00: Testcontainers crea bases desechables con Docker. Las dependencias ya declaradas en los proyectos no se sustituyen instalando SDK o creando carpetas.

`.env.example` contiene solo ejemplos ficticios. Copiar a `.env` únicamente para configuración local y nunca publicarlo; ASP.NET Core no carga ese archivo automáticamente. La configuración del servidor y las conexiones locales se documentan en §10.

**Render y Neon:** no se necesitan para B00. No crear cuentas ni aceptar condiciones hasta que Lucía autorice el despliegue. B00 utiliza PostgreSQL desechable mediante Testcontainers y Docker; las conexiones a servicios de nube y el respaldo diario siguen pendientes de su autorización.

## 9. Estado y entrega de B00

T1 está publicado; PR #1 y PR #3 ya están fusionados; main de referencia: `8baf8fb`. La protección de main con una revisión se comprobó el 03-oct; la protección contra secretos sigue sin verificar.

**[PR #4](https://github.com/LuciAguilar/Therapease/pull/4), rama `revision/etapa09`:** entrega conjunta de QA para revisión de Miguel. Incluye Identity, recuperación local, arquitectura, horas/Docker, HTTPS, API Q03, interrupciones Q08 y errores/registros. Los resultados por bloque están en [B00 §4](docs/planning/TherapEase-etapa09-B00.md#4-evidencia-registrada-y-límites); son ejecuciones locales, no CI ni una nueva ejecución conjunta. Quedan **siete fallos conocidos**: dos de arquitectura y cinco del filtro Q03. Se conservan visibles hasta corregir el código.

| Quién | Qué debe consultar y atender |
| --- | --- |
| Miguel | [Informe de ajustes](docs/reviews/TherapEase-09-B00-revision-PR01.md): §6–§7 obligatorios, §9 opcional. Revisar las pruebas de Lucía, corregir su código con unitarias propias y registrar resultados. Los ajustes históricos de §2 ya están resueltos. |
| Dulce | Informe §8: mensajes ante rechazo conocido, resultado incierto y restablecimiento repetido; PageModel, unitarias y pendientes de producto. |
| Equipo | [B00 §6–§7](docs/planning/TherapEase-etapa09-B00.md#6-siguientes-pasos-compartidos): tareas, responsables y dependencias; [AGENTS](AGENTS.md): reglas obligatorias. Cada persona puede avanzar en las tareas independientes de su alcance. |

**ADR-05 decidido el 10-oct:** age con clave pública, custodia privada fuera de GitHub, validación antes de cifrar y simulacro mensual; detalle en AGENTS §2.8. Las pruebas actuales de cifrado usan un formato de QA. Implementación del respaldo real todavía no autorizada.

Revisión humana por persona distinta de la autora, aprobación final y fusión pendientes. B00/LCA/R04 siguen abiertos; sin B01, nube ni datos reales. Se conservan los comandos técnicos de Miguel en §10; sus referencias históricas de QA se actualizan únicamente para enlazar el estado vigente.

## 10. Construir, probar y ejecutar

Requiere el SDK indicado en `global.json` (.NET 10). Visual Studio solo abre estos proyectos desde la versión 2026 (18.0 o superior); Visual Studio 2022 no carga ese SDK y muestra «The SDK 'Microsoft.NET.Sdk' specified could not be found». VS Code y la terminal funcionan con el SDK instalado. Desde la raíz del repositorio:

```bash
dotnet build TherapEase.sln
dotnet test TherapEase.sln
dotnet run --project src/Web
```

La aplicación escucha en `http://localhost:5052` y expone `/salud`. IntegrationTests contiene los bloques de QA de B00 §4.1–§4.8, pendientes de revisión humana/fusión; hay siete fallos conocidos de arquitectura/Q03. Los 50 casos de Identity/recuperación corresponden a la ejecución del 08-oct. E2ETests sigue sin pruebas; espera los recorridos con las páginas de Dulce.

Imagen Docker (requiere Docker Desktop; salud y usuario sin privilegios se declaran en el `Dockerfile`):

```bash
docker build -t therapease:local .
docker run --rm -p 8080:8080 therapease:local
```

**Base de datos local y migraciones** (PostgreSQL desechable con datos ficticios; el puerto 55432 evita chocar con un PostgreSQL ya instalado). Sustituye `<clave>` por contraseñas locales que no subas a Git:

```powershell
docker run -d --name therapease-pg -e POSTGRES_PASSWORD=<clave> -e POSTGRES_DB=therapease_ficticia -p 55432:5432 postgres:17
Get-Content scripts/db/crear-roles.sql -Raw | docker exec -i therapease-pg psql -U postgres -v ON_ERROR_STOP=1 -v base=therapease_ficticia -v clave_migrador=<clave> -v clave_app=<clave> -f -
$env:ConnectionStrings__Migraciones = "Host=localhost;Port=55432;Database=therapease_ficticia;Username=therapease_migrador;Password=<clave>"
dotnet tool restore
dotnet ef database update --project src/Infrastructure --startup-project src/Web
$env:ConnectionStrings__TherapEase = "Host=localhost;Port=55432;Database=therapease_ficticia;Username=therapease_app;Password=<clave>"
```

Las migraciones las aplica solo `therapease_migrador`; la aplicación se conecta con `therapease_app`, que no puede cambiar el esquema, borrar pacientes o citas, ni modificar `EventoAuditoria`. Una migración nueva se crea con `dotnet ef migrations add <Nombre> --project src/Infrastructure --startup-project src/Web --output-dir Migraciones`; si crea una tabla, debe incluir sus `GRANT` explícitos para `therapease_app`, y se revisa su SQL (`dotnet ef migrations script`) antes del PR.

**Reglas de citas implementadas.** Activa = agendada y vigente; solo las activas bloquean horarios. La base impone por sí misma que dos citas activas no se crucen y que nunca exista una cita activa de un paciente de baja (restricción de exclusión y triggers de `EsquemaInicial`); esa protección se conserva. Cancelar no es baja y conserva el pago. Ninguna cita se cancela ni se recupera automáticamente. **Provisional, pendiente de validar con la usuaria (Dulce):** (1) la política para las citas existentes al dar de baja a un paciente: hoy la baja se rechaza mientras haya citas activas, y la alternativa sería cancelarlas primero; (2) el pago inicial «Pendiente»; (3) las citas seguidas sin hueco, que hoy se permiten porque el intervalo es `[inicio, fin)`. Lo provisional es la política de atención a las citas existentes, no el bloqueo de la base.

**Identidad y acceso.** Los usuarios y roles los administra ASP.NET Core Identity; los roles `Usuaria` y `Superusuario` los siembra la migración. Con la aplicación configurada (`ConnectionStrings__TherapEase`), el primer superusuario se crea con un comando que se ejecuta solo en la máquina del operador: entrega una contraseña temporal únicamente por su consola, una sola vez, sin escribirla en los registros del servidor, sin correo y sin contraseña fija en el código. Condiciones decididas por Lucía: la contraseña tiene mínimo 12 caracteres y no exige combinaciones de letras, números o símbolos, y la sesión caduca tras 30 minutos sin actividad. Documentarlas no acredita que sus mecanismos estén completamente probados: la evidencia de QA en servidor está en B00 §4.1; los recorridos de navegador siguen pendientes.

```powershell
dotnet run --project src/Web -- --crear-superusuario <nombre-de-usuario>
dotnet run --project src/Web -- --restablecer-superusuario <nombre-de-usuario>
```

El segundo comando es la recuperación cuando ningún superusuario puede entrar; como todo restablecimiento de contraseña, conserva el bloqueo vigente en lugar de levantarlo (cinco fallos bloquean como máximo 15 minutos). La cookie de sesión es `HttpOnly`, `Secure` y `SameSite=Lax`; cada solicitud revalida el sello y el estado del usuario contra la base, y todo lo que no declare otra política exige sesión válida.

**Consulta técnica de auditoría (Q03, sin pantalla).** Solo lectura, solo superusuario activo con sesión vigente: `GET /api/auditoria/eventos?desde=<instante>&hasta=<instante>` con opcionales `tipoRegistro` (`Paciente`, `Cita`, `Usuario`), `idRegistro` y `limite` (1 a 500, 100 por defecto). Los instantes llevan zona, por ejemplo `2026-10-01T00:00:00Z`; sin zona se rechazan. Responde `401` sin sesión, `403` sin permiso y `400` ante un filtro inválido. Devuelve en JSON el actor, el momento, el registro y los campos afectados, la acción y el hecho, nunca valores. El comando que inicia sesión y llama a este servicio queda pendiente de que existan las pantallas de acceso: Dulce implementa los `PageModel` y escribe sus unitarias en el mismo PR.

**Dependencias entre proyectos permitidas** (regla acordada; hoy ninguna prueba automática la verifica, porque la prueba de límites de Lucía aún no existe):

| Proyecto | Puede referenciar |
| --- | --- |
| `TherapEase.Domain` | nada |
| `TherapEase.Application` | Domain |
| `TherapEase.Infrastructure` | Application, Domain |
| `TherapEase.Web` | Application; Infrastructure solo para componer la inyección de dependencias en el arranque |

**Estructura de carpetas.** Cada capa se organiza primero por módulo (Identidad, Pacientes, Citas, Auditoria y lo común en `Compartido`), porque `AGENTS.md` y ADR-03 piden módulos por capacidad; el espacio de nombres sigue el patrón `TherapEase.<Capa>.<Módulo>`, por ejemplo `TherapEase.Application.Pacientes`. Dentro de cada módulo, el código se separa por tipo y el espacio de nombres sigue la carpeta:

| Capa | Carpeta dentro del módulo | Qué contiene |
| --- | --- | --- |
| Domain | `Entidades` y `Entidades/Enums` | Solo datos: entidades y enumeraciones. |
| Domain | `Reglas` | Las operaciones sobre las entidades (`ReglasDePaciente`, `ReglasDeCita`, `HoraHermosillo`). |
| Domain | `Excepciones`, `Constantes` | Excepciones de regla de negocio y constantes. |
| Application | `Interfaces/Repositorios` | Puertos de persistencia (uno por módulo) y `IUnidadDeTrabajo`. |
| Application | `Interfaces/Servicios` | Contratos de los servicios y de lo que implementa Infrastructure (sesión, usuario actual). |
| Application | `Servicios` | Implementaciones de los servicios. |
| Application | `Modelos`, `Constantes` | DTO y resultados (`RespuestaServicio<T>`), mensajes. |
| Infrastructure | `Entidades`, `Configuraciones`, `Repositorios`, `Servicios`, `Constantes` | Entidades de Identity, mapeo de EF, implementación de puertos y adaptadores. |
| Infrastructure | `Data`, `Migraciones`, `Configuracion` | Contexto de EF, migraciones y registro de dependencias (no pertenecen a un módulo). |
| Web | `Pages`, `Endpoints`, `Seguridad`, `Comandos`, `Configuracion`, `Salud` | Pantallas, rutas técnicas, cookie y políticas, comandos de operador, composición y sonda de salud. |

Los setters de las entidades son `internal`: solo las clases de `Reglas` del propio dominio las modifican, así ninguna otra capa salta una regla. El coordinador de CU04 va en `TherapEase.Application.Coordinadores`, fuera de Pacientes y Citas. Citas puede usar el contrato público de Pacientes; Pacientes no usa Citas; Identidad usa Auditoría y Auditoría no usa Identidad.
