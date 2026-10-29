# TherapEase — ADR-01 a ADR-05

> **Entrevista recibida y revisada el 14-oct-2026:** ✅ respuestas de la psicóloga registradas en [respuestas y ajustes](../../requirements/TherapEase-09-entrevista-requisitos.md). ⏳ Montos, simplificación a solo particular y cancelación automática requieren decisión de alcance/mecanismo de Lucía; no están implementados por esta actualización. B00/LCA/R04 abiertos, preparación documental sin Construcción ni pruebas nuevas. Los resultados y aprobaciones anteriores se conservan como historia.

**Alcance de las referencias a asistentes — 04-oct-2026:** las revisiones con Codex/Claude aquí citadas corresponden al método personal de Lucía. Miguel y Dulce eligen sus herramientas y organización de revisión; usan este ADR como contexto. Las reglas compartidas están en AGENTS. Esta aclaración no modifica las decisiones arquitectónicas ni el texto fuente transcrito.

**Estado de este archivo:** transcripción corregida y aceptada por Lucía el 01-oct-2026 tras el veredicto CORREGIR (ajuste menor), que permite aprobar sin otra auditoría. Lucía confirmó la fidelidad con el texto de 04 que aportó. No es una prueba ejecutable: arquitectura y stack siguen sujetos a la validación de LCA en B00. Se conserva el nombre provisional para no romper referencias.

**Procedencia declarada:** chat **«04 - Arquitectura»**, entrega 04D v0.2 («Patrones, ADR y trazabilidad») y correcciones 04D v0.3 aprobadas por Lucía el **21-sep-2026**. El veredicto recibido el 01-oct contrastó esta transcripción con el texto aportado por Lucía; sus cuatro correcciones quedan aplicadas y aprobadas. Las líneas «Estado y revisión» se identifican como notas de transcripción, sin atribuirlas al texto fuente. Las notas de continuidad se separan de las decisiones de 04. La matriz Q01–Q20/R01–R05 vigente está en `TherapEase-etapa08-backlog-trazabilidad.md`.

## ADR-01 — Arquitectura general

- **Contexto 04D:** equipo pequeño, mantenimiento compartido y crecimiento moderado.
- **Alternativas 04D:** monolito, servicios gestionados o microservicios; Clean o capas.
- **Decisión 04D:** monolito modular.
- **Consecuencias 04D:** despliegue sencillo; el sistema escala inicialmente como unidad.
- **Nota de transcripción — Estado y revisión:** candidata; 04B v0.3 aprobada y auditada. No forma parte del texto fuente de 04.
- **Prueba futura 04D:** comprobar dependencias, preparación y mantenimiento en 09.

**Continuidad posterior:** Lucía eligió **Clean Architecture adaptada** en 04E, con abstracciones limitadas. Esta anotación posterior no demuestra funcionamiento ejecutable de la arquitectura.

## ADR-02 — Interfaz y publicación

- **Contexto 04D v0.3:** una publicación, mismo origen y facilidad de uso; **R01** se considera sin darle énfasis adicional.
- **Alternativas 04D:** interfaz separada, compilada o generada en el servidor.
- **Decisión 04D:** interfaz generada en el servidor, procedente de 05B como candidata.
- **Consecuencias 04D:** seguridad y publicación más sencillas; interfaz y servidor se liberan juntos.
- **Nota de transcripción — Estado y revisión:** candidata; 04C y 05B aprobadas. No forma parte del texto fuente de 04.
- **Prueba futura 04D:** Q12–Q15 en 09.

**Continuidad posterior:** 04E/05 consolidaron documentalmente Razor Pages, HTML/CSS/JavaScript Vanilla y una publicación en contenedor Docker con Render y Neon como stack candidato. En 08D, **Q15** quedó relacionado con ADR-02 por publicación y reproducción del entorno. Ninguna prueba de despliegue o uso se da por ejecutada.

## ADR-03 — Módulos y dependencias

- **Contexto 04D:** evitar acoplamiento y permitir pruebas.
- **Alternativas 04D:** capas técnicas solamente o módulos por capacidades.
- **Decisión 04D:** módulos por capacidades y servicios de aplicación.
- **Consecuencias 04D:** límites claros; puertos por módulo solo si aportan aislamiento.
- **Nota de transcripción — Estado y revisión:** candidata; 04B–04C aprobadas. No forma parte del texto fuente de 04.
- **Prueba futura 04D:** revisar dependencias y ejecutar pruebas por módulo en 09.

**Precisión posterior:** coordinadores externos cuando una operación involucre varios módulos. Se separa de «Decisión 04D» conforme al texto de 04 y a la mejora opcional del veredicto recibido el 01-oct-2026; no cambia el mecanismo acordado.

**Continuidad posterior:** 07A concretó un puerto de persistencia por módulo y la prueba automática de límites. 08C situó conceptualmente el coordinador de CU04 fuera de Pacientes y Citas, en Application, para evitar un ciclo; Miguel define su ubicación al crear los proyectos en 09. Lucía escribe y ejecuta la prueba de límites; Miguel la revisa.

**Decisión posterior — aprobada por Lucía el 08-oct-2026, tras recomendación de Claude:** se permite a Web usar de Domain solo enumeraciones y constantes en contenedores sin lógica. Entidades, reglas y servicios de Domain siguen accediéndose por Application. Se admite la referencia de compilación a Domain, directa o transitiva, con comprobación del uso real; no se obliga a modificar los archivos de proyecto existentes. La regla operativa se registra en AGENTS §1. No se modifica la transcripción de la decisión 04D.

**Validación ejecutable del 08-oct:** 30 comprobaciones de arquitectura: 28 correctas, 2 fallidas, 0 omitidas. Enumeraciones y constantes aceptadas; falla la llamada de Web a `MatrizDePermisos` y la presencia del coordinador CU04. Miguel corrige/implementa, Lucía repite y Miguel revisa estas pruebas. La prueba estructural no demuestra por sí sola el funcionamiento de CU04. B00/LCA siguen abiertos.

## ADR-04 — Integridad y ciclo de datos

- **Contexto 04D v0.3:** Q03, **Q05**, Q06–Q09, Q20 y R02.
- **Alternativas 04D:** validación solo en aplicación o protección adicional en almacenamiento.
- **Decisión 04D:** atomicidad, restricción de cruces, concurrencia optimista, auditoría de solo inserción y baja lógica.
- **Consecuencias 04D:** mayor integridad; exige tratar conflictos y errores de almacenamiento.
- **Nota de transcripción — Estado y revisión:** candidata; compatible con 05C. No forma parte del texto fuente de 04.
- **Prueba futura 04D:** concurrencia, interrupciones, auditoría, horarios y bajas en 09.

**Límite aprobado en 04D v0.3:** la baja lógica atiende **Q20**, no resuelve R04. La demostración usa datos ficticios (Q05). Una cita cancelada conserva su registro y estado de pago; cancelarla y darla de baja son operaciones distintas. 08B/08C precisaron que solo una cita agendada y vigente cuenta para cruces y que recuperar una agendada exige paciente vigente y nueva comprobación de cruce. Todo ello requiere prueba ejecutable en 09.

**Nota posterior B02 — propuesta aprobada por Lucía el 26-oct-2026; auditoría de Claude APROBADO v1 con ajustes de texto, sin otra auditoría. Miguel implementa y avisa si encuentra un problema técnico.** Solo registros ficticios. Lucía aprobó reconocer el doble envío de alta por el identificador fijo generado al abrir el formulario. El servidor valida sesión/permisos y, para reconocer un alta confirmada, el actor y los datos normalizados; una repetición reconocida no crea otra ficha, evento o versión. El identificador no es secreto ni concede acceso. La lista de parecidos requiere consultar, también durante el alta.

La clave primaria de Paciente impide dos filas con el mismo identificador. La propuesta concreta reconocer solo esa colisión, revertir la transacción fallida y releer lo persistido con un contexto válido antes de responder; un fallo de auditoría u otra restricción nunca significa «ya registrado». Se mantienen cambio/evento atómicos. Reenviar manualmente el mismo identificador no crea otra ficha; no hay reintento automático. Identificadores distintos pueden registrar homónimos: no se promete unicidad por nombre/nacimiento ni una deduplicación general.

En edición, Lucía eligió mantener conflicto ante versión antigua, incluso con datos iguales. Solo la versión vigente permite «sin cambios», sin escritura/evento/incremento. Que la ficha exista no confirma una edición cuya respuesta se perdió: se revisan datos/versión con permiso de consultar y se mantiene la incertidumbre si no se puede atribuir el resultado. No se agrega un registro o servicio de solicitudes nuevo. Este mecanismo se comprobará con PostgreSQL real, solicitudes simultáneas, cortes antes/después de confirmar, actores/datos distintos y fallo del evento; **no se acredita implementación ni prueba ejecutada**. No resuelve R04 ni autoriza datos reales, B06/B07, nube o gastos.

## ADR-05 — Seguridad y recuperación

- **Contexto 04D v0.3:** Q01, Q02, Q04, Q10–Q11, Q14, **Q17**, Q19 y R03.
- **Alternativas 04D:** controles solo en interfaz o controles obligatorios en servidor.
- **Decisión 04D:** autorización por operación y roles en servidor, comunicación cifrada, secretos externos y respaldo recuperable.
- **Consecuencias 04D:** requiere configurar identidad, claves, respaldo y restauración.
- **Nota de transcripción — Estado y revisión:** candidata; compatible con 04C y 05C–05D. En 04D, el respaldo diario estaba pendiente para 05E. No forma parte del texto fuente de 04.
- **Prueba futura 04D:** accesos, registros, restauración y recorrido de 30 minutos en 09.

**Continuidad posterior:** 05E definió documentalmente el respaldo diario y su verificación de restauración; 07B detalló Identity, sesiones, TLS y auditoría. Lucía exigió TOTP con app autenticadora **antes de usar datos reales** en 08B. La incorporación de ese mecanismo exige revisar el impacto en ADR-05 conforme a 07A antes de implementarse. **R04 sigue abierto:** procedencia, acceso, conservación y eliminación definitiva se deciden con la usuaria antes de cualquier dato real; este ADR no los resuelve.

### Nota posterior ADR-05 — opción A del respaldo (antecedente del 01-oct)

**Decisión de Lucía, 01-oct-2026:** usar un repositorio **privado aparte**, dedicado únicamente al respaldo diario. **Estado al 01-oct:** propuesta de implementación para auditoría de Claude; no se atribuye auditoría recibida ni creación de infraestructura. El texto original de 04 se conserva. La decisión vigente de cifrado/custodia y validación está en la nota posterior del 10-oct.

El repo público contiene aplicación y documentación; el privado alojará el flujo del respaldo y sus artefactos privados, sin guardar volcados ni secretos en Git. Mantener 05E: `pg_dump` diario cifrado, huella SHA-256, retención de 14 días, restauración automática en PostgreSQL desechable y una incidencia asignada a Lucía ante fallo de respaldo o restauración, hasta resolverlo. Secretos de conexión/cifrado serán configuración externa del repo privado; nunca material del repositorio público. Acceso al repo privado, origen y permisos de la automatización, método de cifrado y recuperación de claves deben concretarse y revisarse antes de implementar.

**Consecuencia:** separar respaldo de la visibilidad pública sin cambiar Q10–Q11 ni retención. **Validación pendiente:** demostrar que personas sin acceso no descargan artefactos, que el flujo cifra y verifica la huella, que restaura correctamente en PostgreSQL desechable y que un fallo genera la incidencia a Lucía. La recuperación completa cronometrada sigue en el plan de Lucía.

**Límite actual:** no crear aún el repo de respaldo ni conectar con Neon. B00 puede continuar con PostgreSQL desechable y datos ficticios. La ayuda solicitada posteriormente para herramientas y cuentas Render/Neon no configura este respaldo ni acredita despliegue autorizado. Lucía prepara y audita personalmente esta concreción con Codex/Claude y decide su implementación; Miguel y Dulce siguen su propio método.

**Ajuste de la propuesta — 08-oct-2026, veredicto de Claude aplicado por autorización de Lucía:** la antigüedad se mide y se informa para Q10; no es condición para impedir la restauración. En un incidente se restaura la copia más reciente disponible e íntegra aunque tenga más de 24 horas, registrando el incumplimiento de Q10, para no prolongar la caída Q11. Una fecha futura se informa como anomalía de medición. La integridad y la clave correcta siguen siendo obligatorias. El código de QA separa medición y restauración; su nueva ejecución del 08-oct fue correcta (50 casos en conjunto); revisión humana y aprobación siguen pendientes.

### Nota posterior ADR-05 — decisión de Lucía, 10-oct-2026

**Estado: aprobado documentalmente; implementación no autorizada.** Concreta y sustituye las propuestas anteriores sobre cifrado, custodia y validación del respaldo. Conserva el texto original de 04, Q10–Q11 y la opción A del 01-oct.

| Tema | Decisión aprobada | Implementación / evidencia pendiente |
| --- | --- | --- |
| Cifrado diario | pg_dump cifrado con age y clave pública; conservar SHA-256 de la copia cifrada. age es herramienta nueva registrada conforme a 07A. | Sin instalación, claves generadas ni cambio de dependencias. |
| Claves | Para cifrar, el flujo de GitHub solo tiene la clave pública. Lucía guarda la privada fuera de GitHub en su gestor de contraseñas y Miguel guarda una copia fuera de GitHub. | Generación, entrega y comprobación de custodia pendientes; no se afirma que las claves ya existan. |
| Validación diaria | En el mismo trabajo: generar volcado, restaurarlo en PostgreSQL desechable, comparar y después cifrarlo. El volcado sin cifrar nunca sale del ejecutor. | No subirlo a Git, artefactos o registros. El flujo diario no recibe la clave privada ni descifra; la comprobación mensual cubre ese recorrido. |
| Simulacro mensual | Lucía descifra una copia, restaura y mide el tiempo para Q10–Q11. | Pendiente de implementación/autorización y ejecución real; no acreditado por las pruebas actuales. |
| Antigüedad | Nunca bloquear la restauración por antigüedad: usar la última copia íntegra disponible aunque supere 24 h y abrir o actualizar incidencia asignada a Lucía. | Medir/reportar incumplimiento Q10; la integridad y la clave correcta siguen siendo necesarias. |
| Conservación | 14 días como artefacto privado del repositorio de respaldos, separado del público. | Repositorio, permisos y retención todavía sin configurar. |
| Fallos | Mantener incidencia asignada a Lucía ante fallo de respaldo/restauración hasta corregirlo. | Automatización pendiente. |
| Pruebas existentes | Cifrar/Abrir usan formato exclusivo de QA (AES-256-GCM). No se modifican ahora. | Al implementar el respaldo real, reemplazar las pruebas de cifrado —copia alterada, clave incorrecta, aleatorio nuevo y fecha protegida— por pruebas con age. Conservar las de volcado, restauración, comparación, fallos y antigüedad. |

**Límite:** no crear el repositorio de respaldos, conectar Neon ni implementar el flujo. No instalar age ni generar/guardar claves en esta entrega documental. Rotación de claves, usuario/permisos de respaldo, política de sesiones tras incidente, carga objetivo y demás detalles operativos siguen pendientes donde corresponda. La aprobación no acredita funcionamiento diario/mensual ni cierra B00/LCA/R04.

### Nota posterior ADR-05 — temporales y acceso Q03 (12-oct-2026)

**Decisión de Lucía, aprobada documentalmente.** No altera la transcripción original de 04. AGENTS §2.2–§2.3 conserva las reglas completas.

| Tema | Decisión | Implementación / evidencia pendiente |
| --- | --- | --- |
| Temporales, orden 7 | Puntos 1–4 aprobados: verificar identidad por canal conocido distinto al de entrega; temporal una vez, sin secretos en registros/URL/TempData; nuevo restablecimiento ante respuesta perdida, sin reintento automático; acceso solo CU11 y revocación acordada. | Entrega desde páginas y recorrido final pendientes. Solo registrar eventos de cambios confirmados: dos si ambos intentos confirmaron; uno si el primero se revirtió. |
| Caducidad A | B00 con datos ficticios, sin vencimiento adicional de la temporal. Otro restablecimiento invalida la anterior. No cambia sesión de 30 min ni bloqueo vigente. | Mantener evidencia local y límites del alcance; no acredita navegador. |
| Caducidad B | Temporal de 24 h obligatoria antes de datos reales: fecha de emisión, migración y pruebas. | Trabajo posterior; no implementar ahora. No autoriza datos reales ni resuelve R04/TOTP. |
| Q03, orden 8 | Opción A: superusuario activo, sin temporal, accede con navegador autenticado y abre la URL en la misma ventana; cookie y permisos vigentes. | Acceso/cierre de sesión de Dulce y prueba final. Sin pantalla de auditoría; script B no incluido. |

La decisión no cierra B00/LCA y no cambia el respaldo ni autoriza nube.

## Estado del traslado y aprobación — 01-oct-2026

Lucía autorizó el traslado el 30-sep-2026. El 01-oct aportó el veredicto de contraste y aprobó las cuatro correcciones, la fidelidad y los demás puntos documentales de B00/ADR-06/Q03. No consta una nueva auditoría ni revisión humana de Miguel o Dulce; esta aprobación no las inventa. Los ADR pueden separarse posteriormente conservando identificadores y procedencia. LCA sigue abierta hasta resultados ejecutables y decisión de Lucía; R04 sigue abierto.

### Nota posterior ADR-05 — dependencia de QA en B00 (06-oct-2026)

**Concreción propuesta para revisión y decisión:** incorporar `Testcontainers.PostgreSql` 4.15.0 al proyecto `TherapEase.IntegrationTests`, con xUnit 2.9.3 existente. Testcontainers/PostgreSQL desechable ya forman parte del stack acordado; esta nota registra la versión y la referencia de pruebas conforme a 07A. Se prepararon y ejecutaron 37 casos locales con datos ficticios; la revisión humana de Miguel o Dulce y la aprobación de la entrega siguen pendientes. Las dependencias de ejecución de la aplicación permanecen iguales.

La clase de apoyo usa servicios Identity y migraciones reales, usuarios separados y una base por caso. El reloj controlado de cookie y el contexto HTTP de componente viven solo en pruebas. No agrega rutas a la aplicación, no acredita páginas/navegador/TLS ni implementa el respaldo privado. Q10–Q11 y LCA conservan sus pendientes. Esta nota no modifica la transcripción ni las decisiones originales de 04.


### Propuesta de validación local Q10–Q11 — 07-oct-2026

Solo para pruebas B00: `pg_dump`/`pg_restore` 17 del contenedor y AES-256-GCM mediante `System.Security.Cryptography` de .NET, sin paquetes nuevos. Clave aleatoria de 32 bytes solo en memoria, valor aleatorio nuevo de 12 bytes por copia, autenticación de 16 bytes y SHA-256 del archivo cifrado. Fecha y versión del formato de prueba autenticadas; rechazar copia/clave inválidas antes de escribir en destino. Restaurar en una transacción en otro PostgreSQL desechable; comparar todas las tablas, permisos, identidad y tiempos.

**Propuesto, no aprobado para producción:** el formato, cifrado y clave efímera permiten únicamente la demostración local. Gestión y recuperación de claves duraderas, usuario de respaldo, retención diaria de 14 días, repositorio privado, incidencia automática, nube y pruebas a carga objetivo siguen pendientes de auditoría y decisión. No cambia ADR-05 original ni acredita Q10–Q11 completos o cierre LCA. Lucía autorizó elaborar y ejecutar la validación local; Miguel o Dulce debe revisar sus pruebas.


## Nota posterior documental — entrevista del 14-oct-2026

✅ Entrevista recibida; cambios de requisitos registrados, no implementación autorizada. ⏳ Solo particular puede cambiar el modelo de ámbitos; montos requieren definir datos/reglas/migración; cancelación automática en baja requiere atomicidad/eventos/concurrencia y pago conservado conforme a ADR-04. Miguel propone impacto y Lucía decide antes de modificar mecanismos Q/R. No se elige tecnología nueva por esta entrevista.

✅ Campos de Usuario/Paciente/Tutor solicitados, pantallas aceptadas en general y preferencias futuras recibidas. ⏳ Detalle/contratos/persistencia y pruebas. Papel/5 años/solo la usuaria/consulta antes de eliminar son insumos R04, no política legal aprobada. Se conserva texto original de ADR, historia ejecutada y límites B00/LCA/R04. Véase el registro local de entrevista.
