# TherapEase — Guía de arranque del equipo en 09

**Actualización documental: 04-oct-2026.** La base de esta guía, AGENTS, ADR-01…06 y el plan B00 fueron aprobados el 01-oct. Esta actualización separa ajustes para el equipo de las notas personales y alinea el estado parcial de B00. Los cambios de documentación inicial pueden comunicarse a Lucía; no se acredita una revisión no recibida.

**T1 aprobado y publicado:** Lucía aprobó los 22 archivos de documentación, reglas y carpetas base el 01-oct-2026 y realizó el primer push a [LuciAguilar/Therapease](https://github.com/LuciAguilar/Therapease), repositorio público. La validación ejecutable de B00 y la decisión de cierre de LCA siguen pendientes.

## 1. Estado y alcance

- **Autorizado:** iniciar 09 por B00, implementar lo necesario para validar el stack y registrar evidencia. Lucía aprobó el plan corregido, ADR-06, la fidelidad de ADR-01…05 corregidos y la consulta Q03 sin pantalla. Eligió repositorio público y autorizó instalar .NET 10.
- **Avance parcial:** PR #1 tiene solución/proyectos, backend inicial, migraciones e identidad en revisión. Hay resultados parciales, resumidos en B00; correcciones y validación completa siguen pendientes. B01–B08 no quedan autorizados por este avance.
- **Producto:** primer recorrido previsto: acceso → paciente → cita → pago pagado/pendiente, sin montos ni cobros. B00–B04 al 04-dic-2026 es un corte propuesto que Dulce debe validar con la usuaria. B05–B07 dependen de prioridad y capacidad; B08 recoge evidencia realmente obtenida.
- **Cambio vigente:** no hay pantalla de auditoría. Q03 registra eventos en base y permite consulta técnica autorizada de solo lectura. El directorio muestra pacientes vigentes por defecto y filtro autorizado de bajas; sus campos visibles siguen pendientes.
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
| **Lucía** | Revisar los cambios críticos y la evidencia parcial; preparar límites, integración crítica y seguridad de B00. Revisar esta actualización documental antes de publicar. | PR propio de las 11 pruebas adicionales con revisión de Miguel/Dulce; completar evidencia y decidir LCA. |
| **Miguel** | Corregir el PR #1 con el informe de ajustes: descripción, README, condiciones documentales y políticas provisionales. Continuar la validación de B00 con sus unitarias propias. | Nuevo commit identificable, build y comandos/resultados. Mantener el bloqueo PostgreSQL. Revisa la prueba de límites; él o Dulce revisa las demás pruebas de Lucía. |
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
| Opción A del respaldo decidida: repo privado aparte, dedicado al respaldo diario | Lucía revisa la nota de implementación ADR-05 con su método personal y decide | Nota propuesta para auditoría; no crear aún el repo ni conectar Neon. Conservar `pg_dump` cifrado, SHA-256, 14 días, restauración desechable e incidencia a Lucía ante fallo. No frena B00 desechable. |
| Revisión de documentación inicial | Miguel o Dulce comunica observaciones con su propio método; Lucía confirma cambios | Pendiente y no bloqueante; guía, AGENTS y ADR corregidos ya aprobados por Lucía. |
| Revisión de código/pruebas, resultados B00 y decisión LCA | Autor distinto del revisor en cada PR; aprobación final de Lucía | Miguel/Dulce revisa pruebas de Lucía. Construcción continúa pendiente de evidencia y decisión LCA. |
| Acceso, procedencia, conservación y eliminación de datos reales; TOTP | Lucía con la usuaria y responsables técnicos | R04 sigue abierto: no utilizar datos reales. |

GitHub permite descargar artefactos a personas autenticadas con lectura del repositorio; en uno público ese acceso no restringe el respaldo al equipo. El cifrado no convierte el artefacto en privado. [Fuente oficial: descarga de artefactos](https://docs.github.com/en/actions/how-tos/manage-workflow-runs/download-workflow-artifacts). La opción A tiene una nota posterior de implementación en ADR-05, pendiente de revisión y decisión de Lucía. No se ha creado el repo de respaldo ni configurado el flujo.

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

El PR #1 ya incluye solución, `.csproj`, migraciones y aplicación mínima; su revisión no acredita todavía B00 completo. Miguel mantiene los comandos de arranque y migración alineados con el commit revisado. No instalar PostgreSQL de producción para B00: Testcontainers crea bases desechables con Docker. EF Core/Npgsql, Identity, xUnit y Playwright se incorporarán como dependencias de los proyectos; su preparación no se sustituye instalando SDK o creando carpetas.

`.env.example` contiene solo ejemplos ficticios. Copiar a `.env` únicamente para configuración local y nunca publicarlo; ASP.NET Core no carga ese archivo automáticamente: Miguel documentará cómo proporciona la configuración al servidor y a Testcontainers. Los nombres de conexiones del ejemplo son propuestos y deben alinearse con el código.

**Render y Neon:** no se necesitan para B00. No crear cuentas ni aceptar condiciones hasta que Lucía autorice el despliegue. B00 utiliza PostgreSQL desechable mediante Testcontainers y Docker; las conexiones a servicios de nube y el respaldo diario siguen pendientes de su autorización.

## 9. Estado y orden de integración

T1 fue publicado y aprobado. La protección de main con una revisión se comprobó el 03-oct; protección contra secretos todavía sin verificar. Las reglas de revisión de código/pruebas están en AGENTS. B00/LCA y R04 siguen abiertos.

**Orden de fusión:** primero PR #1, con sus correcciones verificadas; después PR #2 actualizado sobre el main resultante. Al integrar esta guía en README se conservan las secciones técnicas de Miguel sobre construcción/arranque, base de datos local y migraciones, identidad, consulta Q03, dependencias y estructura. La documentación del PR #2 puede consultarse para corregir #1 sin fusionarla antes.

Consultar [B00 — siguientes pasos compartidos](docs/planning/TherapEase-etapa09-B00.md#6-siguientes-pasos-compartidos) para el avance y los pendientes de integración y pruebas.
