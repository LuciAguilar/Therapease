# TherapEase — Etapa 09 · Avance y pendientes

**Estado vigente, 12-nov-2026.** ✅ #18 aprobado por Lucía y fusionado con squash en 8bcc7c6, tras Claude APROBADO v1, 12-nov. Preparación local para #19: 37 casos propios B03 y una unitaria de cita agendada sin nombre; 884/884 unitarias y 562/562 integraciones correctas. ✅ Ampliación de #19: Claude APROBADO v1, 12-nov. ✅ Publicación autorizada por Lucía. ⌛ CI del nuevo SHA y revisión de Miguel. Páginas B03 con Dulce después de B02. B00/B01/B02/LCA/R04 siguen abiertos. Lucía autorizó esta entrega para el mismo PR #19; su CI y la revisión humana siguen pendientes.

> **El servidor de B01 y B02 ya está entregado.** Nuestro QA B02 está en el PR #12, con CI correcta, aprobación de Miguel y fusión por Lucía en `b53c5b7`. Las páginas B01 y sus PageModel ya están fusionados en #13, main 32e7bca. Las páginas B02 y posteriores siguen con Dulce.

El alcance de las pruebas está en las tablas de los planes [B00](TherapEase-etapa09-B00.md), [B01](TherapEase-etapa09-B01.md), [B02](TherapEase-etapa09-B02.md) y [B03](TherapEase-etapa09-B03.md). Allí se distingue servidor de páginas y navegador.

## 1. Qué ya tenemos

| Bloque | Entregado | Sigue pendiente |
| --- | --- | --- |
| **B00 · Los cimientos y los candados** | Base, controles de datos y acceso, horas de Hermosillo, recuperación local y QA de #4/#6. Los controles desde páginas B01 se ejecutaron en #13. | Revisiones del [plan B01](TherapEase-etapa09-B01.md), pendientes propios de B00/LCA y decisión de cierre. R04 sigue abierto. |
| **B01 · La entrada y las cuentas** | Servidor y QA integrados en #8–#10. Dulce revisada en #13/8b18422, Claude APROBADO v2, Miguel aprobó, QA correcto y CI 4/4. Lucía fusionó con squash en 32e7bca. | QA propio publicado en [#19](https://github.com/LuciAguilar/Therapease/pull/19), 656bba1; Claude APROBADO v1. CI: 469/469 integración y 52/54 navegador. Falta diagnosticar/corregir CI, revisión de Miguel, fusión y cierre. Ver [plan B01](TherapEase-etapa09-B01.md). |
| **B02 · Las fichas de pacientes** | Servidor revisado por Lucía y fusionado en [#11](https://github.com/LuciAguilar/Therapease/pull/11), main `c90b3c0`. Registrar, buscar, consultar y editar con sus controles y auditoría. | QA [#12](https://github.com/LuciAguilar/Therapease/pull/12) revisado y fusionado. Faltan confirmaciones de producto, páginas, navegador y uso con una persona ajena. |
| **B03 · La agenda y las citas** | ✅ Servidor fusionado en #18/8bcc7c6. Claude APROBADO v1, 12-nov; 37 casos propios auditados. | Ampliación local de #19 con Claude APROBADO v1, 12-nov: unitaria de cita sin nombre, 37 integraciones y dos textos. Publicación autorizada por Lucía; falta CI y Miguel. Páginas con Dulce después de B02. |

| Evidencia vigente de B02 | Resultado y límite |
| --- | --- |
| QA de Lucía en #12, `5e46883` | 89 casos propios, incluidos en 465 integraciones correctas; 646 unitarias correctas. Sin fallidas ni omitidas en la repetición local sobre la rama global. |
| CI de #12 | [41134402751](https://github.com/LuciAguilar/Therapease/actions/runs/41134402751): cuatro trabajos correctos. |
| Auditoría | Claude APROBADO v1, aceptado por Lucía. El ajuste del parche documental ya se aplicó y comprobó. |
| Revisión humana de nuestro QA | Miguel (`GalloNav`) aprobó #12 el 31-oct. No hay correcciones en su revisión. |

Los 89 están incluidos en los 465. No sumar ambas cifras ni las repeticiones como casos nuevos. El contenido revisado de #11 era `52332fb`; su fusión conserva ese código. Los resultados anteriores conservan su fecha, versión y alcance.

## 2. Qué puede avanzar ahora

| Persona | Puede avanzar ya | Espera una dependencia |
| --- | --- | --- |
| **Miguel** | ✅ Servidor B03 fusionado en #18. Ayudar a Dulce con pacientes B02 y revisar el QA completo de #19 cuando esté publicado. | Espera la ampliación de #19 auditada y publicada; no necesita otra ronda en #18. |
| **Lucía** | ✅ Aprobó #18 y su fusión. Prepara B01+B03 para #19: 884 unitarias y 562 integraciones correctas. | Claude APROBADO v1 sobre la ampliación, 12-nov. Falta autorización de publicación, CI, revisión de Miguel y decisión final. |
| **Dulce** | ✅ B01 fusionado. Conectar B02 con los servicios disponibles, escribir PageModel y sus pruebas; confirmar campos/tutor/escuela/grupo y dar su visto bueno a 08C sección 3. | No necesita esperar a que termine todo el QA para comenzar sus páginas. Lucía decide las respuestas de producto que cambien lo aprobado. |

Para cerrar B00, Lucía también debe consolidar la evidencia, revisar los riesgos que sigan abiertos y decidir expresamente el cierre. Puede preparar ese registro ahora, sin dar el cierre por hecho. Quedan por comprobar desde páginas la entrega segura de contraseñas temporales, salida/sesiones, formularios protegidos, caché y cabeceras; CSS con sesión y política de recursos. La protección actual de ramas se puede reverificar y documentar, sin cambiarla.

La descripción de #12 quedó vacía al fusionar; se conserva el texto preparado como antecedente. La limpieza sigue pendiente de tabla y OK; la fusión no autoriza borrar la copia del 30-oct ni otra evidencia.

## 3. B03 · Servidor integrado y QA en preparación para #19

**Avance del 12-nov.** Seguir el [plan B03](TherapEase-etapa09-B03.md). Diseño aprobado, servidor integrado por #18 y QA propio auditado; #19 reunirá B01 y el seguimiento B03. Páginas y navegador B03 siguen pendientes.

| Persona | Primer trabajo autorizado ahora |
| --- | --- |
| Miguel | Una propuesta de agenda y alta de citas: campos, permisos por rol, periodo consultable, horarios/duración, cruce de citas activas, paciente vigente, ámbitos, reenvío y auditoría. Identificar qué controles de B00/B02 puede reutilizar y qué debe decidir Lucía. |
| Lucía | Revisar el conjunto corregido antes del OK de publicación; no se requiere otra auditoría de estos textos. Diseño escrito de agendar/baja y dobles envíos en el plan B03, secciones 4.1–4.2; ajustar después a la propuesta de Miguel, auditarla y decidir antes de programar. |
| Dulce | Días, campos y horario decididos por Lucía el 01-nov; sin preguntas de producto B03 pendientes. Preparar sus páginas cuando haya contrato decidido; conserva los PageModel y sus unitarias. |

Reglas B03 vigentes: [sección 3 de AGENTS](../../AGENTS.md#3-límites-vigentes), decisiones de Lucía del 01-nov-2026; sustituyen el horario del 31-oct.

**Qué se reutiliza:** reglas y restricciones mínimas de citas ya comprobadas en B00, más pacientes, sesiones, auditoría y ayudas de QA. Hoy no existe un servicio completo de Application para consultar agenda y agendar. Esas funciones completas no se dan por entregadas por tener tablas y restricciones.

No se incorporan reprogramación/cancelación (B05), bajas/recuperaciones nuevas (B06), directorio (B07), montos, «solo particular» ni cancelación automática. B04 conserva el estado de pago sin montos. B08 sigue pendiente para la evidencia final adicional; las comprobaciones de seguridad y datos se mantienen en cada bloque.

## 4. Documentos para decidir

| Documento | Uso |
| --- | --- |
| [AGENTS, sección 3](../../AGENTS.md#3-límites-vigentes) | Decisiones y límites. |
| [Plan B00](TherapEase-etapa09-B00.md), [B01](TherapEase-etapa09-B01.md) y [B02](TherapEase-etapa09-B02.md) | Qué cubre cada bloque y qué falta. |
| [Plan de pruebas](../testing/TherapEase-09-plan-de-pruebas.md) | Casos existentes; reutilizar antes de escribir otros. |
| [08D](TherapEase-etapa08-backlog-trazabilidad.md) y [08B](../requirements/TherapEase-etapa08-casos-historias.md) | B03: HU08–HU09 y CU05–CU06. |
| [Entrevista, sección 5](../requirements/TherapEase-09-entrevista-requisitos.md#5-aclaración-del-horario-aportada-por-lucía) | Decisiones del 01-nov y antecedente del 31-oct; sin pendientes de horario. |

Solo datos ficticios y trabajo local. Sin datos reales, nube, gastos ni cierre automático de otros bloques. #12 ya está fusionado. Los 16 documentos compartidos actuales ya se publicaron directamente en main d2302f2 con aprobación de Lucía. Código, pruebas y limpieza conservan sus autorizaciones.
