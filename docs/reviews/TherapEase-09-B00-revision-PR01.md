# TherapEase — B00 · Ajustes del PR #1

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
