# TherapEase — Avance de Metodologías de Desarrollo Ágil

**Publicación documental, 17-oct-2026:** B01 está abierto y PR #6 ya está fusionado. Este resumen conserva la evidencia aprobada de B00 y las tareas previstas; la ampliación de pruebas B01 espera auditoría y aprobación y no se incorpora como avance cerrado.

> **Entrevista recibida y revisada el 14-oct-2026:** ✅ respuestas de la psicóloga registradas en [respuestas y ajustes](../requirements/TherapEase-09-entrevista-requisitos.md). ⏳ Montos, simplificación a solo particular y cancelación automática requieren decisión de alcance/mecanismo de Lucía; no están implementados por esta actualización. B00/LCA/R04 abiertos, preparación documental sin Construcción ni pruebas nuevas. Los resultados y aprobaciones anteriores se conservan como historia.

> **Corte: 12 de octubre de 2026 · Resumen para la materia.** Scrum con Sprints de dos semanas y RUP como marco de fases. La entrega del **4 de diciembre** mostrará el avance alcanzado.

## 1. Equipo y responsabilidades

| Integrante | Rol en Scrum | Trabajo y avance actual |
| --- | --- | --- |
| **Lucía** | **Scrum Master** | Facilita el proceso y seguimiento; responsable de arquitectura, documentación, seguridad, QA y pruebas de integración. Revisa cambios y conserva la aprobación final del proyecto. |
| **Dulce Hernandez** | **Product Owner** — gestión del producto | Contacto directo con la psicóloga: requisitos, prioridades y seguimiento. Responsable del frontend y sus pruebas unitarias; frontend todavía sin avance, según actualización del equipo. |
| **Miguel Leyva** | **Developer** | Implementa backend y sus pruebas unitarias; atiende correcciones y revisa el trabajo de Lucía. Backend inicial incorporado y correcciones en revisión. |

> **Nombre del rol:** en Scrum usamos *Product Owner* para Dulce, aunque su labor incluya gestión del producto. Los tres contribuyen a la calidad; Lucía y Dulce también actúan como Developers al trabajar en el incremento. Priorizar el producto corresponde a Dulce; facilitar Scrum corresponde a Lucía.

## 2. Tiempo y organización de cada Sprint

| Aspecto | Acuerdo o estimación |
| --- | --- |
| Duración | **Dos semanas**, con trabajo en tiempos libres. |
| Dedicación orientativa propuesta | **6–10 horas semanales por persona**: 12–20 por Sprint, 36–60 para el equipo. Incluye reuniones, elaboración, pruebas y revisión. Es una referencia inicial, no horas realizadas ni disponibilidad confirmada. |
| Capacidad real | Cada integrante confirma su disponibilidad al planear. Se reduce el trabajo seleccionado si hay menos tiempo; no se alarga el Sprint. |
| Margen sugerido | Reservar aproximadamente 20 % de la capacidad confirmada para correcciones e imprevistos; ajustar con la experiencia. |

| Momento | Actividad | Tiempo acordado |
| --- | --- | --- |
| Inicio | **Planning:** fijar un objetivo, disponibilidad, tareas y aceptación. | 30–45 min |
| Cada día de trabajo | **Daily:** revisar el objetivo, bloqueos y siguiente paso. | 15 min |
| Durante el ciclo | Elaborar, probar, revisar entre compañeros y ajustar el plan. | Según capacidad |
| Final | **Review:** mostrar resultados y recibir comentarios. Si Erika falta, Dulce recoge sus comentarios después. | 30 min |
| Después de la Review | **Retrospectiva:** elegir una mejora para el siguiente ciclo. | 20 min |

> **Registro honesto:** esta tabla describe la operación acordada; no acredita reuniones realizadas. Si se sustituye una reunión por mensajes, se registra como adaptación.

## 3. Trabajo por Sprint: propuesta de distribución

> **Pendiente de acordar en Planning.** Los documentos actuales organizan el trabajo en bloques B00–B08, sin un historial de Sprints numerados y fechados. Un bloque puede ocupar varios Sprints. Esta distribución orienta el trabajo siguiente; no representa Sprints ya completados ni un calendario aprobado.

| Ciclo de dos semanas | Objetivo propuesto | Resultado esperado y condición |
| --- | --- | --- |
| Próximo ciclo: trabajo actual B00 | Completar la validación técnica prioritaria. | Miguel corrige backend; Lucía verifica seguridad e integración; Dulce prepara el frontend mínimo de acceso necesario. Registrar pendientes; cerrar LCA solo con evidencia y decisión expresa de Lucía. |
| Siguiente: B01 | Construcción abierta el 14-oct: backend, seguridad, CI y QA de servidor sin esperar páginas. | B00/LCA/R04 siguen abiertos; integrar páginas de Dulce cuando lleguen. No exige cerrar antes B00/LCA; PR #6 revisado y fusionado el 15-oct, espera cumplida; propuestas de detalle aún pendientes. Actualizar la copia local antes de continuar B01. |
| Posterior: B02 | Registrar y buscar pacientes ficticios. | Recorrido integrado y probado; respuestas de campos/prioridades recibidas; detalle de formatos y cambios de alcance por concretar/aprobar. |
| Posterior: B03–B04, según capacidad | Agendar y registrar estado pagado/pendiente. | Recorrido acceso → paciente → cita → pago probado. Dividir entre ciclos si no cabe. |

- **Diciembre:** la entrevista acepta el recorrido B01–B04; el corte B00–B04 sigue sujeto a capacidad y montos por decidir; esta secuencia no garantiza completarlo antes del 4 de diciembre.
- **Alcance adicional:** B05–B06 dependen de capacidad; B07, directorio completo, queda después del primer avance de diciembre. B08 reúne evidencia adicional de calidad alcanzada.
- **Ficha de cada Sprint:** fechas · objetivo · capacidad · tareas seleccionadas · resultado/evidencia · pendientes · mejora acordada. Completarla con datos reales.

## 4. Revisión y criterio de terminado

| Paso | Forma de trabajo |
| --- | --- |
| Seleccionar | Dulce ordena el Product Backlog; el equipo selecciona trabajo y mantiene el Sprint Backlog. |
| Elaborar | Cada autor implementa o documenta y añade las pruebas que le corresponden. |
| Revisar | Abrir un PR —solicitud de revisión de cambios— y asignarlo a otra persona. Miguel y Lucía ya se revisan; Dulce seguirá el mismo proceso. |
| Corregir e integrar | Resolver observaciones, obtener revisión humana y aprobación final de Lucía; fusionar a `main`. Miguel o Dulce revisa las pruebas de Lucía. |
| Marcar terminado | Aceptación cumplida, evidencia identificable, revisión distinta del autor, CI satisfactorio y bloqueantes resueltos. Lo incompleto vuelve a planearse. |

> **RUP + Scrum:** RUP organiza Inicio, Elaboración, Construcción y Transición; Scrum organiza el trabajo en ciclos cortos. Hoy continúa Elaboración con B00/LCA abierto. Finalizar un Sprint o fusionar un PR no cierra automáticamente una fase.

## 5. Evidencia del avance

| Evidencia documentada | Estado al corte |
| --- | --- |
| Etapas 01–08 | Cerradas documentalmente; requisitos, arquitectura, historias, casos de uso y backlog disponibles. |
| Backend inicial — PR #1 | Fusionado; no equivale a producto completo. |
| Pruebas de Lucía — PR #4 | Revisadas por Miguel y fusionadas. |
| Correcciones de Miguel — PR #5 | En revisión según el registro disponible; no se declara fusionado. |
| Frontend y validación completa | Pendientes. B00/LCA siguen abiertos; solo datos ficticios. |

## 6. Qué entregar al profesor

| Documento | Uso sugerido |
| --- | --- |
| **Este resumen** | Documento principal: equipo, método, ciclos, revisión y avance. |
| [Backlog y trazabilidad](TherapEase-etapa08-backlog-trazabilidad.md) | Anexo principal: prioridades, bloques y relación con requisitos. |
| [Historias y casos de uso](../requirements/TherapEase-etapa08-casos-historias.md) | Anexo: historias, aceptación y comportamiento esperado. |
| [Plan de pruebas](../testing/TherapEase-09-plan-de-pruebas.md) y [estado B00](TherapEase-etapa09-B00.md) | Extractos de evidencia y pendientes, acompañados de PR #1 y #4. |
| Metodología aprobada (registro local de Lucía) y [AGENTS.md](../../AGENTS.md) | Respaldo del acuerdo original y reglas vigentes; consultar las decisiones posteriores. |

- **Formato recomendado:** PDF para presentar y conservar una versión con fecha; Markdown como fuente editable y enlace al repositorio. Si el profesor acepta Markdown, también es correcto entregarlo renderizado.
- **Selección:** entregar resumen y anexos pertinentes; conservar `ESTADO.md`, `REVISIONES.md`, prompt maestro y notas personales como continuidad interna.
- **Base:** documentos enlazados, actualización de Lucía y [Guía oficial de Scrum](https://scrumguides.org/scrum-guide.html). Las horas y la distribución futura son propuestas de este resumen; no modifican los acuerdos aprobados.

## Actualización de producto recibida el 14-oct-2026

### Respuestas recibidas que sustituyen esperas de producto

| Estado | Respuesta recibida | Pendiente que permanece |
| --- | --- | --- |
| ✅ | Paciente: nombre completo, teléfono, correo, nacimiento, escuela/grupo; anotación de edad. Tutor siempre en menores; homónimos por nacimiento y apellidos; lista solo nombre. | ⏳ Campos/formatos/obligatoriedad, datos del tutor y edad calculada o guardada; escuela/grupo ante «solo particular». |
| ✅ | Citas contiguas permitidas; pago inicial Pendiente; corregir pago cancelada sí, de baja no. | ⏳ Implementación/QA restante; horario aclarado por Lucía (9–15: 1 h; desde 15: posibilidad de 30 min, hasta 19); límite de 19 por precisar. |
| ✅ | Solicita solo particular, montos y cancelación automática de citas al dar de baja paciente. | ⏳ Aprobación de cambios frente a la base y mecanismo/modelo/pruebas; conservar invariantes y pago. |
| ✅ | Solo Erika operativa; Usuario con nombre completo y correo; pantallas «Están bien»; logotipo en inicio. | ⏳ Contratos/validaciones de campos, versión exacta de pantallas y recurso visual; no recuperación por correo. |
| ✅ | Recorrido de diciembre conforme; después expediente/notas 1, reportes 2, directorio 3, reprogramar/cancelar 4. | ⏳ Baja/recuperación sin número; capacidad/secuencia técnica, sin renumerar B/HU/CU ni adelantar clínico. |
| ✅ | Expediente: motivo, antecedentes, notas y plan; anotaciones iniciales/por cita. Reportes: citas por mes y pacientes atendidos; anotación de lista de espera. | ⏳ Modelo/vínculo/indicadores/proceso; capacidades futuras, sin HU/CU nuevos por inferencia. |
| ✅ | Papel, práctica declarada de 5 años, acceso real solo Erika y consulta antes de decidir eliminación. | ⏳ R04, política detallada y revisión antes de datos reales. No borrar automáticamente ni archivar indefinidamente. |

La entrevista no acredita una Review o Sprint realizado ni horas trabajadas. Dulce conserva priorización y el equipo acuerda capacidad. No renumerar bloques por el orden manuscrito ni comprometer expediente/reportes para diciembre.
