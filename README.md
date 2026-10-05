# TherapEase — Guía de arranque del equipo en 09

**Fecha:** 01-oct-2026. Guía aprobada por Lucía, junto con `AGENTS.md`, ADR-01…06 corregidos y B00 corregido. Solo Lucía aprueba. La revisión de Miguel/Dulce de esta documentación inicial queda pendiente y no bloquea: sus cambios se comunican a Lucía, quien consulta a Claude y confirma. No se acredita una revisión no recibida.

**T1 aprobado y publicado:** Lucía aprobó los 22 archivos de documentación, reglas y carpetas base el 01-oct-2026 y realizó el primer push a [LuciAguilar/Therapease](https://github.com/LuciAguilar/Therapease), repositorio público. La validación ejecutable de B00 y la decisión de cierre de LCA siguen pendientes.

## 1. Estado y alcance

- **Autorizado:** iniciar 09 por B00, implementar lo necesario para validar el stack y registrar evidencia. Lucía aprobó el plan corregido, ADR-06, la fidelidad de ADR-01…05 corregidos y la consulta Q03 sin pantalla. Eligió repositorio público y autorizó instalar .NET 10.
- **Pendiente:** ejecución de B00, revisión por otra persona, auditoría de su evidencia y decisión expresa de Lucía para cerrar LCA y pasar a Construcción. No confundir implementación de validación con autorización de B01–B08.
- **Producto:** primer recorrido previsto: acceso → paciente → cita → pago pagado/pendiente, sin montos ni cobros. B00–B04 al 04-dic-2026 es un corte propuesto que Dulce debe validar con la usuaria. B05–B07 dependen de prioridad y capacidad; B08 recoge evidencia realmente obtenida.
- **Cambio vigente:** no hay pantalla de auditoría. Q03 registra eventos en base y permite consulta técnica autorizada de solo lectura. El directorio muestra pacientes vigentes por defecto y filtro autorizado de bajas; sus campos visibles siguen pendientes.
- **Datos:** solo ficticios. R04 sigue abierto. Expediente, notas, reportes, capturista, correo e integraciones están diferidos. TOTP es obligatorio antes de datos reales, con revisión del impacto en ADR-05.
- **Gobierno:** Codex propone, Claude audita y solo Lucía aprueba. Desde B00 cada PR de código o pruebas exige revisor distinto del autor y aprobación final de Lucía; Miguel o Dulce revisa las pruebas de Lucía. La revisión documental inicial no bloquea T1 ni el trabajo autorizado.

## 2. Qué documentación llevar al repositorio

Diez archivos, contando esta guía, los dos archivos de ADR y el proyecto StarUML, más archivos y carpetas base de T1. Las rutas siguientes son las de la entrega para revisión; su copia no constituye aprobación para publicar.

**Destino obligatorio y ejecutado para el clon:** carpeta nueva y vacía antes de clonar, `Therapease/repo` bajo la carpeta GitHub indicada por Lucía. La carpeta histórica conserva su `.git`, archivo histórico y `Docs/` antigua (misma carpeta que `docs/` en Windows). En el clon se crea `docs/` en minúsculas y se copian exclusivamente los diez documentos y elementos de T1, ajustando enlaces y anonimización. No se copia el árbol histórico completo.

| Archivo local | Destino previsto | Para qué sirve |
| --- | --- | --- |
| Esta guía | `README.md` | Entrada para todo el equipo: estado, tareas, enlace a reglas y orden de lectura. |
| [Requisitos arquitectónicos](docs/requirements/TherapEase-requisitos-arquitectonicos.md) | `docs/requirements/` | Q01–Q20, restricciones y criterios medibles; fuente de las metas. |
| [08B — Historias y casos](docs/requirements/TherapEase-etapa08-casos-historias.md) | `docs/requirements/` | Permisos, criterios de aceptación, camino básico y alternos CU01–CU11. |
| [08C — Modelos e interfaces](docs/requirements/TherapEase-etapa08-modelos-interfaces.md) | `docs/requirements/` | Entidades, relaciones, reglas y propuestas de pantallas/campos. |
| [08D — Backlog y trazabilidad](docs/planning/TherapEase-etapa08-backlog-trazabilidad.md) | `docs/planning/` | B00–B08, prioridades pendientes y relación de trabajo con Q/R/ADR. |
| [Plan y evidencia B00](docs/planning/TherapEase-etapa09-B00.md) | `docs/planning/` | Validaciones que deben ejecutarse y puerta de LCA; resultados actuales. |
| [Plan de pruebas por persona y CU](docs/testing/TherapEase-09-plan-de-pruebas.md) | `docs/testing/` | Casos de Miguel, Dulce y Lucía; funcionales, seguridad y evidencia final. |
| [ADR-01…05](docs/architecture/adr/ADR-01-a-05-provisionales.md) | `docs/architecture/adr/` | Decisiones arquitectónicas, corregidas y aceptadas por Lucía. |
| [ADR-06](docs/architecture/adr/ADR-06-monorepo.md) | `docs/architecture/adr/` | Monorepo, organización y validación de límites. |
| [StarUML editable](docs/diagrams/TherapEase-08C-StarUML.mdj) | `docs/diagrams/` | Dominio y casos de uso; se abre con StarUML. |

**T1 — preparación inicial:** además de los diez documentos, incluir [AGENTS.md](AGENTS.md), `CLAUDE.md` con solo `@AGENTS.md`, `.env.example` con valores ficticios y `.gitignore` para .NET que excluya `.env` y secretos. Crear carpetas base `src/Web`, `src/Application`, `src/Domain`, `src/Infrastructure`, `tests/`, `docs/requirements`, `docs/planning`, `docs/testing`, `docs/architecture/adr`, `docs/diagrams`, `.github/workflows`, `scripts` e `infra`, con `.gitkeep` donde estén vacías. **Skills aplazadas a un PR posterior; no bloquean a Miguel/Dulce y no se copian versiones heredadas.** Solución, proyectos, migraciones y carpetas internas siguen a cargo de Miguel. No crear el repo privado de respaldo, flujos de CI o conexiones de nube en T1.

**Se conserva localmente:** el archivo histórico completo (`ESTADO.md`, `REVISIONES.md`, prompt maestro y notas de Obsidian), antecedentes de escritorio, `Contexto.txt`, `siifd-comentarios.skill` y `tmp/`. El PDF de pantallas anterior no se necesita: su contenido vigente está en 08C y su pantalla de auditoría quedó retirada. Los resúmenes de Obsidian no sustituyen los seis documentos de trabajo seleccionados.

Antes de publicar, ajustar enlaces a sus destinos y retirar o sustituir enlaces al archivo local excluido. Revisar nombres/contactos en los diez archivos, incluido el JSON de StarUML: si no consta aceptación de la usuaria de aparecer, usar un alias consistente en las copias públicas. La elección de repositorio público no acredita ese consentimiento. Los originales locales se conservan.

**Precedencia:** las adendas de 09 y decisiones del 01-oct rigen los cambios de auditoría, directorio y reparto. El StarUML sigue siendo el antecedente gráfico aprobado de 08: no está actualizado al cambio de pantalla y no autoriza construirla. Cualquier propuesta o pendiente marcado en 08B/08C/08D sigue pendiente, salvo aprobación expresa registrada.

## 3. Qué hace cada persona primero

| Persona | Ahora, durante B00 | Entrega y siguiente paso |
| --- | --- | --- |
| **Lucía** | Crear repositorio público y solo carpetas base/documentación seleccionada; escribir y ejecutar límites, integración crítica y seguridad de B00. Revisar arquitectura, PR críticos y evidencia. | Plan y pruebas con comandos, resultados y revisión distinta. Decide LCA con evidencia; su disponibilidad reducida no transfiere estas tareas. |
| **Miguel** | Tras disponer del repo y SDK: solución, proyectos, carpetas internas y aplicación Razor Pages mínima; contratos Application, coordinador CU04, persistencia, migraciones, Identity y mecanismos críticos necesarios para B00. Unitarias propias en el mismo PR. | Build, arranque/salud, comandos reproducibles y backend que Lucía pueda probar con PostgreSQL/Testcontainers. Revisa la prueba de límites de Lucía; él o Dulce revisa sus demás pruebas. |
| **Dulce** | Leer 08B/08C, validar con la usuaria los pendientes que afectan B00–B04, fijar prioridades y proponer estados de pantalla y contratos con Miguel. Preparar Razor/HTML/CSS/JavaScript para la validación mínima de B00. | Registrar respuestas reales y decisiones pendientes; unitarias de su código y checklist visual. Tras decisión de LCA, frontend de B01–B04 según prioridades acordadas. |

**PageModel pendiente:** Miguel y Dulce deben acordar quién escribe esa parte de servidor de las pantallas antes de repartirla. Su autor escribe las unitarias en el PR; la guía no asigna por omisión todo PageModel a Dulce.

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
| Primer superusuario y recuperación sin correo; si restablecer levanta bloqueo | Miguel propone; Lucía decide | Flujos correspondientes; ninguna contraseña fija en código ni servicio de correo supuesto. |
| Autor de PageModel | Miguel y Dulce | División del trabajo y sus unitarias. |
| Opción A del respaldo decidida: repo privado aparte, dedicado al respaldo diario | Codex redacta nota ADR-05; Claude audita; Lucía decide implementación | Nota propuesta para auditoría; no crear aún el repo ni conectar Neon. Conservar `pg_dump` cifrado, SHA-256, 14 días, restauración desechable e incidencia a Lucía ante fallo. No frena B00 desechable. |
| Revisión de documentación inicial | Miguel o Dulce informa a Lucía; ella consulta con Claude y confirma cambios | Pendiente y no bloqueante; guía, AGENTS y ADR corregidos ya aprobados por Lucía. |
| Revisión de código/pruebas, resultados B00 y decisión LCA | Autor distinto del revisor en cada PR; aprobación final de Lucía | Miguel/Dulce revisa pruebas de Lucía. Construcción continúa pendiente de evidencia y decisión LCA. |
| Acceso, procedencia, conservación y eliminación de datos reales; TOTP | Lucía con la usuaria y responsables técnicos | R04 sigue abierto: no utilizar datos reales. |

GitHub permite descargar artefactos a personas autenticadas con lectura del repositorio; en uno público ese acceso no restringe el respaldo al equipo. El cifrado no convierte el artefacto en privado. [Fuente oficial: descarga de artefactos](https://docs.github.com/en/actions/how-tos/manage-workflow-runs/download-workflow-artifacts). Lucía eligió la opción A: la concreción se redactó como nota posterior de ADR-05 para Claude. No se ha creado el repo de respaldo ni configurado el flujo.

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

La solución, `.csproj`, migraciones y comandos reales para arrancar los añadirá Miguel; en T1 aún no existe una aplicación que ejecutar. No instalar PostgreSQL de producción para B00: Testcontainers crea bases desechables con Docker. EF Core/Npgsql, Identity, xUnit y Playwright se incorporarán como dependencias de los proyectos; su preparación no se sustituye instalando SDK o creando carpetas.

`.env.example` contiene solo ejemplos ficticios. Copiar a `.env` únicamente para configuración local y nunca publicarlo; ASP.NET Core no carga ese archivo automáticamente: Miguel documentará cómo proporciona la configuración al servidor y a Testcontainers. Los nombres de conexiones del ejemplo son propuestos y deben alinearse con el código.

**Render y Neon:** no se necesitan para B00. No crear cuentas ni aceptar condiciones hasta que Lucía autorice el despliegue. B00 utiliza PostgreSQL desechable mediante Testcontainers y Docker; las conexiones a servicios de nube y el respaldo diario siguen pendientes de su autorización.

## 9. Revisión y publicación de T1

T1 está publicado en el repositorio público y aprobado por Lucía. **Antes del primer PR de Miguel, Lucía debe activar en `main` la protección de rama con una revisión y la protección contra secretos.** Su activación sigue pendiente de verificación. La revisión de Miguel/Dulce de documentos permanece pendiente y no bloquea; código y pruebas desde B00 requieren autor y revisor distintos y aprobación final de Lucía. LCA y R04 siguen abiertos.

## 10. Construir, probar y ejecutar

Requiere el SDK indicado en `global.json` (.NET 10). Visual Studio solo abre estos proyectos desde la versión 2026 (18.0 o superior); Visual Studio 2022 no carga ese SDK y muestra «The SDK 'Microsoft.NET.Sdk' specified could not be found». VS Code y la terminal funcionan con el SDK instalado. Desde la raíz del repositorio:

```bash
dotnet build TherapEase.sln
dotnet test TherapEase.sln
dotnet run --project src/Web
```

La aplicación escucha en `http://localhost:5052` y expone `/salud`. Los proyectos de integración y E2E están creados sin pruebas: las escribe Lucía.

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

**Identidad y acceso.** Los usuarios y roles los administra ASP.NET Core Identity; los roles `Usuaria` y `Superusuario` los siembra la migración. Con la aplicación configurada (`ConnectionStrings__TherapEase`), el primer superusuario se crea con un comando que se ejecuta solo en la máquina del operador: entrega una contraseña temporal únicamente por su consola, una sola vez, sin escribirla en los registros del servidor, sin correo y sin contraseña fija en el código. Condiciones decididas por Lucía: la contraseña tiene mínimo 12 caracteres y no exige combinaciones de letras, números o símbolos, y la sesión caduca tras 30 minutos sin actividad. Documentarlas no acredita que sus mecanismos estén completamente probados: hoy solo existen las pruebas unitarias de Miguel.

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
