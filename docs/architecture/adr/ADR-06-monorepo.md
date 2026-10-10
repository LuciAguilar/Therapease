# ADR-06 — Monorepo para TherapEase

**Estado:** redacción corregida y aprobada por Lucía el 01-oct-2026 tras el veredicto CORREGIR (ajuste menor), sin otra auditoría exigida. La revisión documental inicial de Miguel o Dulce está pendiente y no bloquea T1, por decisión posterior de Lucía. Código y pruebas desde B00 sí requieren revisor distinto por PR y aprobación final de Lucía. **Fecha:** 30-sep-2026; registro actualizado el 01-oct. **Origen:** decisión documental de 06A–06C, aprobada por Lucía el 23-sep-2026. La estructura y el stack siguen candidatos hasta B00 ejecutable.

**Corrección del 01-oct-2026:** aplicado y aprobado el ajuste que alinea autor y revisor de la prueba de límites con B00. Lucía eligió repositorio público y documentación seleccionada; los antecedentes de escritorio y el archivo completo de planeación no se publicarán.

## Contexto

Tres integrantes construyen un solo sistema web y una publicación inicial. Los cambios en Razor Pages, servicios, persistencia y pruebas suelen requerir coordinación. Lucía crea el repositorio y solo las carpetas base; Miguel crea la solución, proyectos, carpetas internas, migraciones y backend; Dulce desarrolla frontend y prioriza con Usuaria. El autor y el revisor de cada cambio deben ser personas distintas.

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
