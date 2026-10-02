# TherapEase — 09/B00 · Plan breve y registro de validación

**Estado:** B00 iniciado por autorización expresa de Lucía el 30-sep-2026; plan corregido aprobado por Lucía el 01-oct. Elaboración/LCA abierta; no hay decisión de pasar a Construcción. R04 abierto: únicamente datos ficticios. La aprobación del plan no acredita resultados ejecutables.

**Actualización del 01-oct-2026:** Lucía aprobó todos los puntos documentales, confirmó la fidelidad de ADR-01…05 tras las cuatro correcciones del contraste con 04, aprobó ADR-06 y la consulta Q03, eligió repositorio público y autorizó instalar .NET 10. No consta revisión humana de Miguel o Dulce; sigue pendiente sin atribuirla a esta aprobación. La documentación antigua no irá al repositorio; sí el proyecto StarUML y la selección de trabajo de 09. Se avisará a Lucía antes de comenzar a preparar el repositorio.

**Decisión T1 del 01-oct:** Lucía aprobó guía, `AGENTS.md`, ADR-01…06 corregidos y B00 corregido. Solo ella aprueba. Revisión documental inicial de Miguel/Dulce pendiente y no bloqueante: comunican cambios a Lucía, quien consulta a Claude y confirma. Desde B00, cada PR de código o pruebas requiere revisor distinto del autor y aprobación final de Lucía; Miguel o Dulce revisa las pruebas de Lucía. Antes de publicar, mostrar lista final y esperar su OK; primer push por Lucía.

## REPARTO — decisión de Lucía para 09

- **Miguel y Dulce** escriben las pruebas unitarias de su propio código en el mismo PR.
- **Lucía (QA)** dirige la estrategia y convierte criterios de aceptación y Q en casos de prueba. Escribe y ejecuta integración de reglas críticas **Q03, Q06, Q07 y Q20** con PostgreSQL desechable/Testcontainers; recorrido Playwright Q16; seguridad OWASP/07B y Astra; y B08 (k6, recuperación, uso y demostración). Revisa PR críticos y evidencia.
- **Miguel o Dulce revisa las pruebas que escriba Lucía.** Autor y revisor son personas distintas. El [plan de pruebas por CU](../testing/TherapEase-09-plan-de-pruebas.md), entregado por Lucía, es la referencia de trabajo de 09; esta decisión no requiere nueva ronda de auditoría de ese plan.

**Producto informado por Dulce y confirmado por Lucía:** no se desarrollará pantalla de auditoría; los eventos siguen registrándose en la base y Q03 se prueba. En su lugar habrá una pantalla de **directorio de pacientes**, con vigentes por defecto y filtro autorizado de bajas. Los campos visibles siguen pendientes de Dulce con Usuaria. La captura Q03 se exige desde B01; la consulta de auditoría HU14/CU10/B07 del plan de 08 queda desplazada del alcance de interfaz de 09.

## Tandas de 09 — plan aprobado; prioridades de producto pendientes

| Tanda | Trabajo y puerta |
| --- | --- |
| **09A — B00** | Entorno, límites, stack y pruebas críticas ejecutables; Claude audita la evidencia y Lucía decide LCA antes de Construcción. |
| **09B — B01–B04** | Solo tras la decisión de LCA: identidad, paciente, cita y pago; pruebas por PR y recorrido Q16 desde B04. Corte y prioridades sujetos a Dulce con Usuaria. |
| **09C — B05–B07** | Si hay capacidad y prioridad: cambios de agenda, bajas y directorio de pacientes; captura Q03 transversal, sin pantalla de auditoría. |
| **09D — B08** | Evidencia final que alcance a ejecutarse: rendimiento, recuperación, uso y demostración; sin declarar metas no medidas. |

## Plan de B00

1. **Base y límites.** Lucía crea el repo público y Codex lo clona en una carpeta nueva, vacía antes del clon y separada del archivo histórico y de `T1-preview`. Copiar solo los diez documentos, `AGENTS.md`, `CLAUDE.md` con solo `@AGENTS.md`, `.env.example` ficticio, `.gitignore` de .NET y carpetas base. Skills se adaptan en PR posterior y no bloquean. Lucía revisa la lista y da OK antes de publicar; hace el primer push y después protege `main` con una revisión y protección contra secretos. Miguel crea solución .NET 10, proyectos y carpetas internas. Construir y ejecutar Razor Pages mínimo y su imagen Docker con salud y proceso sin privilegios. Lucía escribe y ejecuta límites de capas/módulos y coordinador CU04; Miguel revisa. Con CI operativa Lucía mide minutos por PR A-06, visibilidad, runner y duración por trabajo; sin atribuir consumo aún no medido.
2. **Datos e integridad.** Miguel implementa migraciones EF Core/Npgsql y escribe unitarias de su código. Lucía escribe y ejecuta integración con xUnit, Testcontainers y PostgreSQL desechable para Q03/Q06/Q07/Q20: esquema real, cruce y dos reservas simultáneas, colisión de versiones, baja/recuperación y auditoría atómica. Comprueba **solo inserción en `EventoAuditoria`** usando el usuario real de la aplicación: `INSERT` permitido y `UPDATE`/`DELETE` denegados, junto con la prohibición de alterar el esquema; las migraciones se ejecutan con otro usuario y esas credenciales no se usan en ejecución normal. También cubre interrupción antes/después del commit, hora de Hermosillo y carrera entre cita y baja del paciente. Miguel o Dulce revisa sus pruebas. Los mocks solo cubren lógica aislada.
3. **Identidad y seguridad.** Miguel implementa Identity y cookies y escribe sus unitarias. Lucía escribe y ejecuta pruebas de seguridad con cuentas ficticias: acceso sin sesión o permiso, bloqueo de cinco fallos/15 min, contraseña temporal, salida y revocación, cambio de rol, último superusuario concurrente, hash y ausencia de secretos/datos privados. Revisa `HttpOnly`, `Secure`, `SameSite=Lax`, antifalsificación y TLS previsto; Q19 de transporte exige comprobación ejecutable local o en entorno de prueba. Astra se usa en la revisión de seguridad correspondiente. Miguel o Dulce revisa las pruebas de Lucía.
4. **Recuperación y decisión.** Lucía prueba `pg_dump` cifrado y restauración en PostgreSQL desechable con datos ficticios; compara registros y mide desde detección hasta servicio restablecido para Q10–Q11. Registra comandos, entorno, versiones, resultados, fallos y revisión de otra persona. Claude audita los entregables que correspondan; Lucía decide LCA y eventual paso a Construcción con evidencia completa.

## Q03 — consulta sin pantalla aprobada documentalmente

Lucía aprobó conservar `ConsultarEventosAutorizados` como operación técnica de **solo lectura**, invocable mediante un comando documentado que llama al servidor. El servidor comprueba la sesión de Identity y el rol de **superusuario activo** en cada solicitud; el rol de aplicación no equivale al superusuario de PostgreSQL. No se concede acceso directo a la base desde el cliente ni se crean pantallas de auditoría.

La operación permite filtrar por periodo y registro mediante consultas parametrizadas y devuelve únicamente actor, momento, registro/campo y hecho. No ofrece edición ni borrado ni incluye contraseñas, contactos o valores clínicos. Miguel implementa la operación y el comando; Lucía prueba lectura autorizada y denegación sin sesión, con sesión revocada o sin el permiso, además de la protección de eventos en PostgreSQL. **Concreción aprobada el 01-oct; implementación y pruebas pendientes. Q03 conserva su meta vigente.**

## Preparación documental del repositorio

Los seis archivos antiguos en `Docs/` son dos PDF y cuatro PNG: **no se publicarán**, por decisión de Lucía del 01-oct que sustituye el traslado previsto a `docs/legacy/desktop/`. `architecture/adr/` sí se incorpora. El proyecto editable StarUML se incluye con la advertencia de que antecede al cambio de pantalla. La selección y tareas de cada persona están en [la guía de equipo](../../README.md). `tmp/` queda excluida. Antes de publicar se revisan los archivos seleccionados, incluido StarUML, y se anonimiza la usuaria en las copias públicas si no consta su aceptación de aparecer. Esta elección no acredita un consentimiento no recibido.

T1 se prepara en el clon nuevo de [LuciAguilar/Therapease](https://github.com/LuciAguilar/Therapease), en `Therapease/repo` bajo la carpeta GitHub indicada por Lucía. Ella creó el repo vacío y proporcionó la URL; Codex lo clonó por HTTPS porque SSH no verificó la clave del servidor. `AGENTS.md` concentra reglas; skills aplazadas a un PR posterior. La carpeta histórica con `.git`, antecedentes y `Docs/` queda fuera. No se utilizó la vista previa prevista; no hay commits o push.

**Respaldo — opción A decidida, implementación pendiente:** repo privado aparte solo para respaldo diario. Conservar `pg_dump` cifrado, huella SHA-256, retención de 14 días, restauración desechable e incidencia a Lucía ante fallo. Nota ADR-05 propuesta para Claude: no crear aún ese repo ni conectar Neon. B00 prueba recuperación con datos ficticios en PostgreSQL desechable. La ayuda posterior solicitada para herramientas locales y cuentas Render/Neon no configura infraestructura ni acredita despliegue. [Fuente oficial sobre artefactos](https://docs.github.com/en/actions/how-tos/manage-workflow-runs/download-workflow-artifacts).

## Resultado de prevalidación — 30-sep-2026

| Comprobación ejecutada | Resultado observado | Estado |
| --- | --- | --- |
| `dotnet --info` / `dotnet --list-sdks` | SDK 8.0.425 y 9.0.318; .NET 10 no está instalado. | El stack candidato .NET 10 no se puede compilar aún en este equipo. |
| `docker version` inicial | Cliente 29.4.1; sin conexión al daemon. El servicio `com.docker.service` estaba detenido. | Se inició Docker Desktop para continuar la prevalidación. |
| `docker version` fuera del sandbox, tras iniciar Docker Desktop | Cliente y servidor Docker 29.4.1; contexto `desktop-linux`, servidor Linux/amd64 operativo. | Prerrequisito de Testcontainers disponible; aún no hay proyecto ni prueba PostgreSQL ejecutable. |
| Inventario local de `src/`, `tests/`, `scripts/`, `infra/`, `.github/` | No existen todavía; la carpeta tiene Git local sin commits ni remoto. | No hay solución, migraciones ni pruebas de B00 que ejecutar; su creación corresponde a Miguel y el repositorio a Lucía. |

**Evidencia pendiente:** SDK .NET 10; salida de build/tests con PostgreSQL/Testcontainers; arranque y salud de imagen Docker; resultados Q06–Q11/Q19 y límites de módulos; recuperación cronometrada; revisión de autor distinto; auditoría de Claude; decisión expresa de Lucía. Docker está disponible, pero no se declara aprobado ninguno de esos controles.

## Decisiones y pendientes

- **Aprobado previamente:** monorepo documental, stack candidato, PostgreSQL/Testcontainers para integración, datos ficticios, responsabilidades y revisión por persona distinta.
- **Decisión de Lucía:** reparto QA, uso del plan de pruebas sin nueva auditoría, plan B00 corregido, ADR-06, fidelidad de ADR-01…05 corregidos, consulta Q03, repositorio público e instalación de .NET 10. La selección documental y guía de equipo sintetizan estos acuerdos; no sustituyen el diseño/migraciones de Miguel.
- **Pendiente:** Dulce y Usuaria validan corte B00–B04, prioridades, campos y pantallas antes del frontend afectado; Lucía y Miguel definen arranque/recuperación sin correo y efecto del restablecimiento sobre bloqueo; R04 y TOTP antes de datos reales.
- **Antes de publicar:** revisión documental inicial pendiente y no bloqueante; entrega de lista final y OK de Lucía antes de publicar; primer push por ella. Repo vacío clonado por HTTPS y archivos T1 para revisión. Skills en PR posterior. .NET 10 instalado y Docker comprobado, sin pruebas B00 aún. LCA y R04 siguen abiertos; solo datos ficticios y revisión distinta para código/pruebas.

## Entorno local comprobado — 01-oct-2026

- `winget install --id Microsoft.DotNet.SDK.10 --exact --source winget ...`: instalador oficial, hash verificado, instalado correctamente; `dotnet --list-sdks` confirmó SDK **10.0.401** junto a 8.0.425/9.0.318.
- Docker Desktop iniciado; comprobación fuera del sandbox confirmó cliente y servidor **29.8.1**. Git **2.52.0.windows.1**, VS Code **1.140.0** y StarUML **6.3.4** detectados. No se actualizaron Git/editor/StarUML.
- Estas salidas validan prerrequisitos; no ejecutan solución, migraciones, PostgreSQL/Testcontainers ni cierran Q/LCA.
