# ADR-06 — Monorepo para TherapEase

> **Entrevista recibida y revisada el 14-oct-2026:** ✅ respuestas de la psicóloga registradas en [respuestas y ajustes](../../requirements/TherapEase-09-entrevista-requisitos.md). ⏳ Montos, simplificación a solo particular y cancelación automática requieren decisión de alcance/mecanismo de Lucía; no están implementados por esta actualización. B00/LCA/R04 abiertos, preparación documental sin Construcción ni pruebas nuevas. Los resultados y aprobaciones anteriores se conservan como historia.

**Alcance de las referencias a asistentes — 04-oct-2026:** las revisiones con Codex/Claude aquí citadas corresponden al método personal de Lucía. Miguel y Dulce eligen sus herramientas y organización de revisión; usan este ADR como contexto. Las reglas compartidas están en AGENTS. Esta aclaración no modifica las decisiones arquitectónicas ni el texto fuente transcrito.

**Estado:** redacción corregida y aprobada por Lucía el 01-oct-2026 tras el veredicto CORREGIR (ajuste menor), sin otra auditoría exigida. La revisión documental inicial de Miguel o Dulce está pendiente y no bloquea T1, por decisión posterior de Lucía. Código y pruebas desde B00 sí requieren revisor distinto por PR y aprobación final de Lucía. **Fecha:** 30-sep-2026; registro actualizado el 01-oct. **Origen:** decisión documental de 06A–06C, aprobada por Lucía el 23-sep-2026. La estructura y el stack siguen candidatos hasta B00 ejecutable.

**Corrección del 01-oct-2026:** aplicado y aprobado el ajuste que alinea autor y revisor de la prueba de límites con B00. Lucía eligió repositorio público y documentación seleccionada; los antecedentes de escritorio y el archivo completo de planeación no se publicarán.

## Contexto

Tres integrantes construyen un solo sistema web y una publicación inicial. Los cambios en Razor Pages, servicios, persistencia y pruebas suelen requerir coordinación. Lucía crea el repositorio y solo las carpetas base; Miguel crea la solución, proyectos, carpetas internas, migraciones y backend; Dulce desarrolla frontend y prioriza con la usuaria. El autor y el revisor de cada cambio deben ser personas distintas.

## Alternativas consideradas

1. **Monorepo:** una historia de cambios, un PR y una CI para aplicación, pruebas, documentación e infraestructura.
2. **Repositorios separados para frontend y backend:** permiten permisos y ciclos de publicación separados, pero exigen sincronizar contratos, versiones, pruebas y documentación entre dos repositorios para este equipo y esta publicación.

## Decisión documental de 06

Usar **un monorepo** para el primer avance. Organizar la raíz según 06B–06C: `src/` con Web, Application, Domain e Infrastructure; `tests/` por tipo; `docs/`; `.claude/skills/`; `Dockerfile` en raíz; `.github/workflows/`, `scripts/` e `infra/` cuando corresponda. Los módulos por capacidad viven dentro de las capas donde necesiten código. `docs/architecture/adr/` guarda los ADR. Una sola imagen contiene la aplicación web; PostgreSQL queda externo. Esta descripción no asigna a Codex la creación de solución, proyectos o carpetas internas.

## Consecuencias y controles

- Un cambio puede recorrer interfaz, aplicación, datos y pruebas en un mismo PR, con trazabilidad Q15–Q16/Q18.
- CI debe comprobar los límites de capas y módulos y ejecutar las pruebas aplicables; el coordinador CU04 queda en Application fuera de Pacientes/Citas para evitar un ciclo. Citas consulta el contrato público de Pacientes; Pacientes no depende de Citas.
- La publicación conjunta acopla las versiones de interfaz y servidor. Si crecen el equipo, permisos o ciclos de entrega, se revisará esta decisión mediante otro ADR.
- Secretos y datos reales quedan fuera del repositorio. R04 sigue abierto; B00 usa solo datos ficticios.

## Validación pendiente en B00

**Precisión aprobada el 08-oct:** aplicar la excepción Web → Domain de AGENTS §1 y la nota posterior de ADR-03: solo enumeraciones y constantes sin lógica. Las pruebas deben aceptar esa excepción y seguir rechazando entidades/reglas/servicios, ciclos y persistencia directa. La ejecución actual registra 30 casos, 28 correctos y 2 fallidos; no cierra la validación.

Miguel crea los proyectos. **Lucía escribe y ejecuta la prueba automática de límites; Miguel la revisa.** La prueba debe fallar ante referencias de capa prohibidas, un ciclo entre módulos o acceso de Web directo a persistencia, e incluir al coordinador CU04 fuera de Pacientes/Citas en Application. Otro integrante reproduce construcción y pruebas (Q15). Lucía revisa arquitectura, seguridad y evidencias; Miguel o Dulce revisa los demás archivos que ella haya creado. La aprobación documental de Lucía no acredita esa revisión ni los resultados pendientes. El cierre de LCA se decide por separado, únicamente con todos los resultados ejecutables de B00.

## Nota posterior — dependencias de QA, 14-oct-2026

Corrección autorizada por Lucía al transmitir el veredicto CORREGIR del PR #6, conforme a 07A. Miguel revisó esta ampliación junto con el resto de PR #6 y aprobó `0db344a`. Lucía fusionó mediante squash el 15-oct-2026; main `ffb2494ad9da026f24a57826c45f6f530afb467b`. No se ejecutan pruebas nuevas por actualizar este estado.

| Decisión / motivo | Alcance y comprobación |
| --- | --- |
| Fijar System.Security.Cryptography.Pkcs y System.Security.Cryptography.Xml en 10.0.13 | Referencias explícitas en UnitTests e IntegrationTests. Antes llegaban transitivamente en 10.0.12 con avisos; se conserva la misma serie 10. No cambia arquitectura, servicios, dependencias de producción ni migraciones. |
| Verificar la actualización | Restore completo; dotnet list package --vulnerable --include-transitive no reporta paquetes vulnerables en los siete proyectos, incluidos transitivos. Ambos assets resuelven 10.0.13. Build Release sin errores/advertencias; 258 unitarias y 205 integración correctas, 0 omitidas. |
| Conservar trazabilidad | B00 §4.11 reúne comandos y huellas. Las 30 pruebas de arquitectura están dentro de las 205 correctas; el resultado 28/30 del 08-oct anterior es histórico. Revisión humana de la ampliación completada en PR #6; cierre B00/LCA pendiente. |

Fuentes primarias: [Pkcs, GHSA-846f-f764-76mx](https://github.com/advisories/GHSA-846f-f764-76mx), [Pkcs relacionado](https://github.com/advisories/GHSA-2fmv-rv2c-59x7), [Xml, GHSA-r955-8q5j-3h83](https://github.com/advisories/GHSA-r955-8q5j-3h83), [Xml relacionado 1](https://github.com/advisories/GHSA-7hqp-7c5m-4vq6), [Xml relacionado 2](https://github.com/advisories/GHSA-c399-9853-6jmj).

La auditoría comprueba paquetes NuGet; no sustituye la revisión de SDK/runtime, imagen ni despliegue. No introduce una herramienta nueva ni autoriza nube o datos reales.

## Nota posterior documental — entrevista del 14-oct-2026

✅ Entrevista recibida; cambios de requisitos registrados, no implementación autorizada. ⏳ Solo particular puede cambiar el modelo de ámbitos; montos requieren definir datos/reglas/migración; cancelación automática en baja requiere atomicidad/eventos/concurrencia y pago conservado conforme a ADR-04. Miguel propone impacto y Lucía decide antes de modificar mecanismos Q/R. No se elige tecnología nueva por esta entrevista.

✅ Campos de Usuario/Paciente/Tutor solicitados, pantallas aceptadas en general y preferencias futuras recibidas. ⏳ Detalle/contratos/persistencia y pruebas. Papel/5 años/solo la usuaria/consulta antes de eliminar son insumos R04, no política legal aprobada. Se conserva texto original de ADR, historia ejecutada y límites B00/LCA/R04. Véase el registro local de entrevista.
