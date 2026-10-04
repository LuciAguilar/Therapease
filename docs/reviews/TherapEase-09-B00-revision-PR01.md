# TherapEase — B00 · Informe acumulativo de revisión

**Actualizado: 04-oct-2026.** Este archivo reúne revisión, pruebas, auditoría y decisiones. Se actualizará por cada PR: Codex revisa y explica, Lucía pasa el avance a Claude, Claude audita y Lucía decide. Se conserva el archivo actual para evitar repartir varios informes.

**Entrega única para Miguel:** leer §2 (correcciones obligatorias), §3 (decisiones aprobadas que debe incorporar), §6 (mejoras opcionales) y §7 (pendientes de B00, que no bloquean por sí solos este PR parcial). El PR #2 entrega exclusivamente este informe; las correcciones se hacen en la rama del PR #1. No requiere fusionar primero otro cambio de reglas. Las decisiones siguen aprobadas aunque todavía no estén incorporadas a AGENTS.md del repositorio.

## 1. PR #1 — estado y alcance

**[Paso 1: Creacion de la base](https://github.com/LuciAguilar/Therapease/pull/1) · Miguel (`GalloNav`) · CORREGIR menor, según Claude.** Puede aprobarse sin otra auditoría después de corregir y comprobar el nuevo código; aún no está aprobado ni fusionado.

- Rama: `b00/solucion-base`. Revisado: `e418010882f9827ece2a1500d28f27490b45c585`; base: `174afe0b6764f2415427f28b04d4627ce4c33b16`.
- Incluye B00 pasos 1–3: solución y siete proyectos, reglas, base de datos, dos migraciones, usuarios/permisos, API de auditoría, Docker y unitarias. Son **tres commits y 149 archivos cambiados**, no tres PR.
- Consulta de GitHub del 04-oct: PR #1 sigue abierto, en el SHA indicado y con descripción vacía. No hay correcciones nuevas verificadas. PR #2 entrega este informe completo; no representa otro avance de código de Miguel.
- Se revisó en copia aislada, sin modificar las fuentes de Miguel ni el clon de trabajo de Lucía. No se publicó revisión, commit o push y no se hizo merge. Otra revisión deberá identificar su propio commit.

## 2. Correcciones obligatorias de Miguel

1. **Descripción del PR:** indicar B00 pasos 1–3, tareas/casos/requisitos (B/CU/Q), qué cubre, comandos/resultados, propuestas pendientes y persona revisora.
2. **README:** quitar la frase que atribuye a Lucía una prueba automática de límites que todavía no existe; solo se hizo una inspección parcial.
3. **README:** cambiar `--output-dir Persistencia/Migraciones` por `--output-dir Migraciones`, la carpeta usada por este proyecto; quitar «Rectoría».
4. **README:** marcar como provisionales **la política para las citas existentes al dar de baja** (impedir la baja o cancelar primero), el pago inicial «Pendiente» y las citas seguidas sin hueco. Dulce valida producto con la cliente y Lucía confirma la decisión. **El bloqueo en la base se conserva:** protege la regla aprobada de que nunca quede una cita activa de un paciente de baja. No quitar ni debilitar ese bloqueo de la migración. Según Claude, las políticas pendientes pueden permanecer provisionales para aprobar este PR parcial.
5. **Documentación del mismo PR #1:** incorporar a AGENTS.md las cinco decisiones de §3. Actualizar los pendientes afectados en README, `docs/planning/TherapEase-etapa09-B00.md` y `docs/testing/TherapEase-09-plan-de-pruebas.md`, enlazando AGENTS.md como fuente única. En particular, el restablecimiento conserva el bloqueo y la contraseña exige 12 caracteres sin composición. CLAUDE.md conserva solo `@AGENTS.md`. La sincronización de reglas la pidió Claude; Lucía decide reunirla con esta entrega, sin un PR separado que publique solamente parte de las decisiones.

**De dónde salió el hallazgo funcional:** `README.md:68` y `docs/requirements/TherapEase-etapa08-modelos-interfaces.md:37` dejan pendiente la política para las citas existentes al dar de baja: impedir la baja o cancelar primero, pendiente de Dulce con la cliente. **El bloqueo de la migración `src/Infrastructure/Migraciones/20261003193524_EsquemaInicial.cs:166/177` se conserva**, porque protege la regla aprobada de que nunca quede una cita activa de un paciente de baja. La corrección es precisar la política pendiente en la documentación, no retirar la protección de la base. El pago inicial de `src/Domain/Citas/Reglas/ReglasDeCita.cs:46` y los intervalos contiguos también siguen provisionales; pasar pruebas no los aprueba. No cancelar ni recuperar citas automáticamente por omisión.

## 3. Decisiones ya aprobadas por Lucía

- Contraseña mínima de **12 caracteres**, sin exigir combinaciones de letras, números o símbolos (`InyeccionDeDependencias.cs:43`).
- Cierre tras **30 minutos sin actividad** (`ConfiguracionDeSeguridad.cs:16`).
- Primer superusuario mediante comando **solo en la máquina del operador**: contraseña temporal visible únicamente en su consola, nunca en registros del servidor (`ComandosDeIdentidad.cs:8`, `README.md:138`).
- Restablecer contraseña **no levanta el bloqueo vigente**: máximo 15 minutos (`README.md:145`).
- **Dulce escribe los PageModel y sus unitarias**. Las reglas vigentes están en AGENTS.md; otra persona revisa y solo Lucía aprueba.

Estas aprobaciones del 04-oct sustituyen los pendientes de seguridad del informe original. No acreditan por sí solas que los mecanismos estén completamente probados. **AGENTS.md del repositorio aún no incorpora estas decisiones.** Miguel las incorpora con las correcciones de su PR #1, conforme a §2. La entrega inicial de cuatro cambios documentales en PR #2 fue retirada por instrucción de Lucía: PR #2 contendrá únicamente este informe acumulativo.

## 4. Pruebas ejecutadas y resultados

Entorno: Windows, SDK .NET **10.0.401**, Docker **29.8.1**, contenedores Linux; PostgreSQL **17** temporal, Testcontainers **4.15.0** y xUnit **2.9.3**. Solo datos ficticios, sin Render ni Neon.

| Comprobación | Resultado | Evidencia local |
| --- | --- | --- |
| Compilar siete proyectos | 0 errores y 0 advertencias | `build.log` |
| Unitarias del PR | **149 pasan**, sin fallos ni omitidas | `test.log`, `unitarias/*.trx` |
| Integración y E2E del PR | Proyectos vacíos; no cuentan como pruebas | `test.log` |
| Pruebas adicionales de QA | **11 pasan**, sin fallos ni omitidas | `testcontainers.log`, `resultados-qa/revision-postgresql.trx` |
| Imagen y arranque Docker | Construye; bases fijadas por digest, proceso UID 1654, salud `healthy` | `docker-build.log`, inspección local |
| Rutas HTTP | `/salud` 200; auditoría sin sesión 401; inicio público 200 con aviso B00, sin datos privados | `http.json` |
| Límites de código | Sin infracciones encontradas en inspección parcial; CU04 aún sin código | `limites-estaticos.json`, `revisar-limites.py` |
| Paquetes NuGet, también indirectos | No reportó vulnerabilidades en PR ni borrador QA al consultar | `vulnerabilidades-pr.json`, `vulnerabilidades-qa.json` |

**Las 11 adicionales comprobaron:**

1. Aplicar las dos migraciones con usuario separado; la app usa un usuario sin privilegios de administrador PostgreSQL.
2. Auditoría: insertar permitido; modificar, borrar y crear esquema prohibidos (`42501`).
3. Citas superpuestas rechazadas (`23P01`); citas contiguas admitidas como política candidata.
4. Dos reservas simultáneas en conexiones distintas: solo una se guarda.
5. Dos ediciones de la misma versión: EF Core rechaza sobrescribir la segunda.
6. Baja/recuperación de paciente **sin citas activas**: conserva fila, limpia datos de baja y aumenta versión.
7. No permite desactivar al último superusuario (`TE002`).
8. Dos desactivaciones simultáneas de superusuarios: una se rechaza y queda uno activo.
9. Cinco fallos concurrentes bloquean el acceso, incluso con contraseña correcta. Los 15 minutos se comprobaron en configuración; no se esperó su expiración.
10. API con servidor real: sin sesión 401; Usuaria o temporal 403; superusuario activo 200; fecha sin zona 400; sesión revocada/desactivada 401.
11. Si falla la auditoría durante un alta de usuario, también se revierte el alta: no queda guardado parcial.

**Alcance real:** las cookies de QA se generaron con Identity/Data Protection y se enviaron manualmente por HTTP local. No prueba login en pantalla, navegador, atributos recibidos de cookies, cifrado TLS o antifalsificación. Las carreras cubren dos transacciones coordinadas, no carga ni todos los órdenes posibles. La reversión de un alta no demuestra todas las operaciones. Son pruebas preparadas por Codex para revisión/adopción de Lucía; Miguel o Dulce debe revisarlas en su PR propio.

## 5. Dónde mirar para entender el código

Rutas bajo `tmp/b00-pr1-e418010`, del commit revisado:

| Archivo y línea | Qué conecta o configura |
| --- | --- |
| `src/Infrastructure/TherapEase.Infrastructure.csproj:19` | Paquetes y versiones: Identity, EF Core y PostgreSQL |
| `src/Web/Program.cs:24` | Arranque: reúne base, identidad, servicios y rutas |
| `src/Infrastructure/Configuracion/InyeccionDeDependencias.cs:28` | `UseNpgsql` conecta EF Core a PostgreSQL; registros vinculan interfaces a implementaciones |
| `src/Infrastructure/Data/ContextoDeDatos.cs:19` | Entidades que se guardan en la base |
| `src/Web/Endpoints/EndpointDeConsultaDeEventos.cs:18` | API GET de auditoría y permiso requerido |
| `src/Web/Seguridad/ConfiguracionDeSeguridad.cs:23` | Cookie de sesión, duración y antifalsificación |
| `Dockerfile` | Construcción, arranque, usuario del proceso y comprobación de salud |

## 6. Mejoras opcionales

- **Identity:** en `src/Infrastructure/Identidad/Repositorios/GestorIdentidad.cs`, comprobar los resultados de `UpdateAsync`, `AddToRoleAsync` y `RemovePasswordAsync`, y detener la operación si fallan. Hoy el guardado final termina frenándolo, pero no conviene depender de ese efecto.
- **Paciente:** `src/Domain/Pacientes/Entidades/Paciente.cs:26` permite modificar la colección de ámbitos desde fuera. Valorar lista de solo lectura y cambios mediante reglas; no se encontró una página que aproveche esa posibilidad.
- **Salud:** aclarar que `/salud` comprueba el proceso. Respondió bien aunque faltaba conexión a la base y había errores de Data Protection; no demuestra base disponible ni login funcional. Valorar comprobación adicional de disponibilidad para operación real.
- **Miguel:** usar noreply de GitHub en próximos commits. No reescribir los actuales sin decisión expresa.

## 7. Pendientes de B00, separados de las correcciones de este PR

| Tema | Qué falta |
| --- | --- |
| A-01, límites y CU04 | Prueba automática que detecte incumplimientos, coordinador real y ausencia de ciclos; una carpeta vacía no los demuestra |
| A-04, Docker | Revisar evidencia con otra persona; comprobar America/Hermosillo dentro de la imagen. Salud de proceso no acredita persistencia |
| Q03/Q20, auditoría y permisos | Comando autenticado sin pantalla de auditoría, filtros/contenido seguro y cambio/evento atómicos en las demás operaciones. README:147 lo aplaza al acceso pendiente |
| Q06, reservas | Recuperar cita agendada, carrera cita/baja con política resuelta y estados alternos mediante servicios |
| Q07, ediciones | Informar el conflicto al usuario sin sobrescribir; cubrir demás registros |
| Q08, guardado | Interrupciones antes/después del guardado, resultado incierto, reintentos y duplicados |
| Q09, horas | Comparar recorrido desde otras zonas con conversión explícita a Hermosillo; una fecha sin hora no es un instante |
| Q10–Q11, recuperación | Aún no ejecutados: respaldo cifrado, huella, restauración temporal, comparación y tiempos desde detección; no configurar nube ni repo de respaldo |
| Identity/Q19, seguridad | Acceso/salida/cambio/restablecimiento, cambio de rol siguiente solicitud, expiración, navegador, antifalsificación y TLS; revisión completa de hashes/configuración/secretos |
| A-06, CI | Medir minutos reales, runner y duración por trabajo cuando exista CI; main protegido, protección contra secretos aún sin verificar |
| Revisión y decisión | Incorporar las 11 pruebas en `TherapEase.IntegrationTests`, PR propio de Lucía revisado por Miguel/Dulce; resolver bloqueantes y decisión expresa LCA |

Según Claude, estos pendientes de B00 no bloquean por sí solos este PR parcial. CI se exige desde B01; recorrido Q16 completo desde B04. **Aprobar el PR no cierra B00/LCA ni autoriza B01 o Construcción. R04 sigue abierto; solo datos ficticios.**

## 8. Reproducción y conservación de evidencia

Código: `C:\Users\lucia\OneDrive\Documentos\ChatGPT\Therapease\tmp\b00-pr1-e418010`. Evidencia: carpeta vecina `b00-pr1-evidencia`; borrador QA en `qa/RevisionDatosTests.cs` y `qa/RevisionB00.csproj`. `manifest-sha256.json` identifica fuentes y resultados. El detalle original sigue conservado en `revision-detallada-03-04oct.md`.

Desde el código revisado, con Docker Desktop activo:

```powershell
git rev-parse HEAD
dotnet build TherapEase.sln --configuration Release --verbosity minimal
dotnet test TherapEase.sln --configuration Release --no-build --no-restore --logger 'trx;LogFilePrefix=pr1' --results-directory '..\b00-pr1-evidencia\unitarias'
docker build --tag therapease-pr1-e418010:revision .
dotnet list TherapEase.sln package --vulnerable --include-transitive --format json
```

Desde `b00-pr1-evidencia/qa`:

```powershell
dotnet test RevisionB00.csproj --configuration Release --logger 'trx;LogFileName=revision-postgresql.trx' --results-directory '..\resultados-qa'
dotnet list RevisionB00.csproj package --vulnerable --include-transitive --format json
```

Guardar nuevas ejecuciones en otra carpeta para conservar la evidencia anterior. Hubo fallos iniciales del sandbox y de una ruta del arnés QA; se corrigieron y repitieron. La primera versión QA usó Testcontainers 4.8.1 con una dependencia vulnerable: se actualizó a 4.15.0 antes del resultado final. Esos incidentes no eran fallos del código de Miguel. Se retiraron los contenedores temporales; la imagen local permanece sin publicar. Contraseñas del arnés exclusivamente ficticias.

## 9. Orden de trabajo acordado

1. **Una sola entrega:** Lucía revisa este informe acumulativo y pasa el avance a Claude. Codex incorpora los veredictos recibidos y las decisiones de Lucía en el mismo documento, sin enviar correcciones por separado. PR #2 contiene únicamente el informe y sirve para entregar a Miguel todo lo revisado y auditado.
2. **Corrección del PR #1:** Miguel aplica §2 y las decisiones de §3 en su rama `b00/solucion-base`. Las mejoras de §6 son opcionales; los pendientes de §7 siguen B00. No modifica ni debilita el bloqueo de la migración.
3. **Verificación del nuevo commit:** cuando Miguel lo suba, Codex revisa las correcciones, explica rutas/líneas a Lucía y compila. Registra SHA, comandos y resultados nuevos en este mismo informe. Lucía decide aprobación y Squash and merge; el veredicto menor recibido no exige por sí solo otra auditoría del PR #1.
4. **Después, PR propio de las 11 pruebas:** Lucía las revisa/adopta y las incorpora a `tests/TherapEase.IntegrationTests`. Miguel o Dulce revisa: autor distinto del revisor. Codex reproduce las comprobaciones y Claude audita la evidencia con Extra High.
5. **Continuidad:** una nueva revisión identifica PR y SHA reales. Se mantienen evidencia, limitaciones y pendientes en el mismo documento. Solo Lucía aprueba; B00/LCA y R04 continúan abiertos hasta evidencia completa y decisión expresa.

## 10. Alcance del PR #2 y corrección del flujo

El [PR #2](https://github.com/LuciAguilar/Therapease/pull/2) se creó inicialmente con cuatro cambios a AGENTS.md, README y planes. Lucía aclaró que quería entregar a Miguel un informe único con todas las correcciones, decisiones, resultados de revisión y auditoría. **Autorizó retirar esos cuatro cambios y reemplazar el contenido del PR #2 exclusivamente por este informe.** Se conserva el número del PR y se reemplaza su commit; main no recibió el contenido anterior.

Este PR es una entrega de instrucciones, no la implementación de las correcciones ni una aprobación del PR #1. El informe conserva los resultados ya ejecutados, los veredictos reales de Claude, las decisiones de Lucía y los pendientes. No se atribuye una nueva auditoría a Claude, no se ejecutaron nuevas pruebas de código y no se fusionó ningún PR. Las referencias de líneas corresponden al SHA de §1; pueden cambiar al corregirlo. La evidencia de tmp sigue local y no se incluye en esta entrega pública.

GitHub mantiene su requisito de una revisión para fusionar en main. Miguel puede leer este documento directamente en la rama del PR #2; leerlo y corregir el PR #1 no requiere fusionar antes PR #2. Lucía conserva la decisión final de publicación/fusión. La sustitución de la entrega no revoca las cinco decisiones aprobadas de §3 ni elimina las pruebas o su evidencia.
