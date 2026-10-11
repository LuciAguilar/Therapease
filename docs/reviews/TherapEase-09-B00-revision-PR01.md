# TherapEase — B00 · Ajustes del PR #1

> **Ampliaciones:** arquitectura en §6 (08-oct) y filtro Q03 en §7 (10-oct, pruebas auditadas; corrección por implementar). Correcciones históricas de §2 ya fusionadas.

**Fecha de revisión:** 04-oct-2026. **Estado:** correcciones obligatorias documentales comprobadas en `b60eba42edeb2fa8cdbea08ad1a4e51b041c21b0`, incluida la descripción. Lucía revisó/aprobó y fusionó PR #1 en main (`41123f2d2c115a05ff1df944457195a7f671cc17`). Las políticas de producto y mejoras opcionales siguen abiertas.

## 1. Alcance revisado

- PR [#1 — Paso 1: Creacion de la base](https://github.com/LuciAguilar/Therapease/pull/1), rama `b00/solucion-base`.
- Commit revisado: `e418010882f9827ece2a1500d28f27490b45c585`; base: `174afe0b6764f2415427f28b04d4627ce4c33b16`.
- B00 pasos 1–3: solución, siete proyectos, reglas de backend, persistencia, dos migraciones, identidad, consulta técnica de auditoría y Docker.
- Estos ajustes corresponden a la revisión original. Comprobación posterior: `b60eba42edeb2fa8cdbea08ad1a4e51b041c21b0` cambia solo README y AGENTS, incorpora las correcciones y compila sin errores/advertencias. La descripción del PR ya incluye alcance, CU/Q, resultados, pendientes y revisora; no se ejecutaron nuevas pruebas en esa comprobación.

## 2. Correcciones obligatorias comprobadas — Miguel

Los seis ajustes siguientes quedaron incorporados en PR #1. Se conservan como registro del alcance corregido; no solicitan repetir cambios ya fusionados.

1. **Descripción del PR:** indicar qué incluye de B00 pasos 1–3, B/HU/CU/Q afectados, comandos y resultados, propuestas pendientes y persona revisora. Usar evidencia que corresponda al commit del PR.
2. **README:** retirar la afirmación de que una prueba automática de límites ya verifica la estructura. Esa prueba todavía no existe; una inspección parcial no la sustituye.
3. **README, migraciones:** sustituir `--output-dir Persistencia/Migraciones` por `--output-dir Migraciones`, que es la carpeta real. Quitar la referencia a «Rectoría».
4. **README, reglas de citas:** marcar como provisionales la política para citas existentes al dar de baja al paciente (impedir la baja o cancelar primero), el pago inicial «Pendiente» y las citas seguidas sin hueco. No presentarlas como políticas definitivas.
5. **Conservar el bloqueo de la base:** nunca debe quedar una cita activa de un paciente de baja. No quitar ni debilitar esta protección de `src/Infrastructure/Migraciones/20261003193524_EsquemaInicial.cs` (líneas 166 y 177 del commit revisado). Lo provisional es la política de atención a las citas existentes, no este bloqueo. No cancelar ni recuperar citas automáticamente por omisión.
6. **Reglas y referencias:** incorporar en `AGENTS.md` las condiciones de §3 y la libertad de método de cada integrante. Añadir expresamente: «El método y las herramientas son libres; las reglas de este archivo (seguridad, datos, pruebas y revisión) son obligatorias para todos, se usen o no asistentes.». Conservar revisión humana por persona distinta del autor, aprobación final y `CLAUDE.md` con solo `@AGENTS.md`. Para esta sincronización, Miguel modificó únicamente `AGENTS.md` y el contenido técnico del README; los planes de B00 y pruebas se actualizan en el PR #3 (`revision/etapa09`).

## 3. Condiciones que debe reflejar la documentación

| Condición | Ajuste |
| --- | --- |
| Contraseña | Mínimo 12 caracteres, sin exigir combinaciones de letras, números o símbolos. |
| Sesión | Caduca tras 30 minutos sin actividad. |
| Primer superusuario | Comando ejecutado solo en la máquina del operador. La contraseña temporal se muestra únicamente en su consola y nunca en los registros del servidor. |
| Restablecimiento | Conserva el bloqueo vigente; duración máxima de 15 minutos. |
| PageModel | Dulce los implementa y escribe sus unitarias en el mismo PR. Revisión y aprobación según AGENTS. |

Estas condiciones están resueltas; documentarlas no acredita que sus mecanismos estén completamente probados. Las políticas de citas indicadas en §2 siguen provisionales.

## 4. Ajuste de producto — Dulce

Validar con la usuaria las tres políticas provisionales de §2: baja con citas existentes, pago inicial y citas contiguas. Registrar la respuesta para concretar la regla correspondiente. Mientras no haya respuesta, mantener la indicación de provisional y conservar el bloqueo de la base.

## 5. Mejoras propuestas — opcionales

- **Identity:** en `src/Infrastructure/Identidad/Repositorios/GestorIdentidad.cs`, comprobar los resultados de `UpdateAsync`, `AddToRoleAsync` y `RemovePasswordAsync`. Si fallan, detener la operación en ese punto; no depender de que el guardado final la frene después.
- **Paciente:** en `src/Domain/Pacientes/Entidades/Paciente.cs:26`, valorar exponer los ámbitos como colección de solo lectura y modificarlos mediante las reglas previstas.
- **Salud:** documentar que `/salud` comprueba que el proceso está encendido. Puede responder aunque la base no esté disponible; no demuestra que funcione el acceso. Valorar una comprobación de disponibilidad para operación real.
- **Commits:** usar el correo noreply de GitHub en próximos commits. La propuesta no incluye reescribir commits ya publicados.

## 6. Ajustes nuevos de arquitectura B00 — 08-oct-2026

Estos ajustes provienen de la validación ejecutable sobre la base `2d088e2c4cc3e2c6f7dc279452713fd1bbf3bced`. Se añaden al seguimiento de B00; no reabren las correcciones documentales del PR #1 ya fusionadas.

**Regla vigente aprobada por Lucía el 08-oct:** Web puede usar enumeraciones y constantes sin lógica de Domain. Entidades, reglas y servicios siempre mediante Application; regla en AGENTS §1 y nota posterior de ADR-03. No es necesario retirar los usos de `Permiso` o `TipoRegistroAuditoria` por esta revisión.

| Responsable | Ajuste obligatorio | Dónde | Qué debe quedar comprobado |
| --- | --- | --- | --- |
| **Miguel** | El manejador pide el permiso a `IAutorizacion.TienePermisoAsync`; deja de llamar a `MatrizDePermisos` directamente. | `src/Web/Seguridad/Manejadores/ManejadorDeRequisitoDePermiso.cs:14`; contrato existente en `src/Application/Compartido/Interfaces/Servicios/IAutorizacion.cs`. | Usuario activo, temporal restringida y permiso vigente; conservar reglas y denegaciones. |
| **Miguel** | Registrar el manejador por solicitud al inyectar `IAutorizacion`, que ya se registra por solicitud. Hoy el manejador es Singleton y no debe conservar ese servicio compartido entre solicitudes. | `src/Web/Seguridad/ConfiguracionDeSeguridad.cs:46`; registro de Application en `src/Web/Configuracion/ServiciosDeAplicacion.cs:14`. | Arranque y autorización sin errores de duración de servicios ni mezcla de usuarios. |
| **Miguel** | Implementar un coordinador CU04 mínimo en Application, fuera de Pacientes/Citas, que consulte las citas activas antes de dar de baja al paciente. | `src/Application/`; mediante contratos públicos de los módulos, sin ciclo. | Baja solo en condición segura, cambio/evento atómicos y protección concurrente. Conservar el bloqueo PostgreSQL. No cancelar automáticamente ni resolver por omisión la política pendiente con Dulce. |

**Resultado actual:** 30 comprobaciones de arquitectura, 28 correctas y 2 fallidas, 0 omitidas. Las dos pendientes son el acceso a `MatrizDePermisos` y el coordinador CU04; no se omiten ni se alteran para aparentar cumplimiento. Tras corregir, repetir el comando de B00 §4.3 y las pruebas de identidad/operaciones afectadas. Verificar la operación real de CU04 además de su ubicación. Revisión del código por persona distinta del autor y aprobación de Lucía; Miguel revisa las pruebas de límites escritas por Lucía. B00/LCA siguen abiertos.

## 7. Ajuste propuesto del filtro Q03 — 10-oct-2026

**Hallazgo comprobado; Claude aprobó las pruebas y Lucía indicó ampliar los casos y precisar esta corrección. Implementación de Miguel y revisión humana pendientes.** La consulta pide un tipo de registro, pero acepta una lista con comas y la transforma en otro tipo.

| Responsable | Cambio propuesto | Dónde | Resultado esperado |
| --- | --- | --- | --- |
| **Miguel** | Aceptar únicamente nombres completos de TipoRegistroAuditoria, sin distinguir mayúsculas: validar primero con Enum.GetNames<TipoRegistroAuditoria>().Contains(valor, StringComparer.OrdinalIgnoreCase) y después convertir. Rechazar números (0, 1, 2) y listas con comas (Paciente,Cita y Paciente, Cita) con 400. Mantener filtros válidos, permisos y metadatos; añadir las unitarias en su PR. | src/Web/Endpoints/EndpointDeConsultaDeEventos.cs:39; enum TipoRegistroAuditoria. | 400, sin eventos, para los números y las listas. Actualmente Paciente,Cita devuelve 200 con Cita; los casos añadidos deben permanecer visibles hasta corregir. Conservar la prueba fallida ConsultaAuditoriaApiTests.Tipo_Invalido_O_Combinado_Se_Rechaza hasta corregir; después repetir Q03 y autorización afectada. |

Enum.TryParse combina los valores separados por comas; IsDefined acepta el resultado cuando coincide con un valor existente. No basta comprobar únicamente el valor resultante. [Referencia de Microsoft](https://learn.microsoft.com/en-us/dotnet/api/system.enum.tryparse?view=net-10.0). La coincidencia es contra el nombre completo; conservar el tratamiento actual del filtro ausente y no recortar una entrada inválida para aceptarla. No se propone cambiar permisos, arquitectura ni admitir filtros múltiples.
