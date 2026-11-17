# TherapEase — Guía para Dulce · B02 · Páginas de pacientes

**Actualizado el 15-nov-2026.** Páginas de [#21](https://github.com/LuciAguilar/Therapease/pull/21) fusionadas en `a1a5e46`. Miguel colaboró con el listado y sus documentos. B02 conserva el QA propio, producto y Q13 pendientes.

> El alcance aprobado está en [plan B02](TherapEase-etapa09-B02.md). El uso del servidor está en el [contrato](TherapEase-etapa09-B02-contrato-para-paginas.md).

## 1. Qué tenemos y qué falta

| Parte | Estado |
| --- | --- |
| Servidor de pacientes y QA | ✅ Incorporados en #11 y #12. |
| Acceso, cuentas y sesiones | ✅ B01 cerrado por Lucía. Conservar sus protecciones. |
| Lista, búsqueda y ficha | ✅ Integradas en main por #21. |
| Registro, tutor y parecidos | ✅ Integrados en main por #21. |
| Edición y aviso de conflicto | ✅ Integrados en main por #21. |
| Dos opcionales B01 | ✅ Comentario de acceso denegado y mensajes de longitud recibidos y comprobados. |
| Auditoría de Claude | Paquete corregido; Claude aprobó código y pruebas tras su revisión. |
| QA inicial de páginas | 77/77 sobre main con los 23 casos auditados de Lucía. Falta autorización de publicación, CI y revisión de Miguel. |
| Producto y diseño | ⌛ Confirmación de Dulce con la usuaria y visto bueno a 08C sección 3. No se acreditan por recibir el PR. |
| Uso con otra persona | ⌛ Q13, cuando la versión esté lista para ese recorrido. |

## 2. Quién revisa ahora

| Persona | Trabajo pendiente |
| --- | --- |
| Dulce | Confirmar con la usuaria los detalles de producto que falten y revisar 08C sección 3. |
| Miguel | Revisar el QA de Lucía cuando se publique. Apoyar problemas del servidor si aparecen. |
| Lucía | Revisar las partes de Miguel y Dulce, elaborar su QA con Codex y decidir publicación y fusión. |
| Codex y Claude | Codex prepara y prueba con Lucía; Claude audita. No sustituyen la revisión humana. |

Miguel es coautor del listado y sus documentos: no puede revisar su propia parte como revisión independiente. La decisión expresa de Lucía fija el reparto de esta entrega.

## 3. Qué conservar en las páginas

| Función | Comportamiento |
| --- | --- |
| Lista | Cargar vigentes al abrir. El detalle de paginación está en el plan B02. |
| Búsqueda | Filtrar por nombre; al cambiarlo, volver a la primera página. Conservar la búsqueda mínima anterior como función distinta del servidor. |
| Homónimos | Mostrar nacimiento solo cuando sirva para distinguir fichas; avisar si es desconocido. |
| Registro | Conservar el mismo número de alta ante reenvío. Confirmar otra persona si aparecen parecidos. |
| Ficha de baja | Fuera de la lista; al abrir por número, mostrar solo su condición autorizada. |
| Edición | Enviar la versión original; ante conflicto conservar la captura sin reenviar automáticamente. |
| Sin cambios | Informar sin crear otro cambio ni evento. |
| Error o respuesta perdida | Mostrar el resultado permitido; no afirmar que se guardó si no está confirmado. |
| Seguridad | Sesión y permisos en servidor, protección de formularios y datos mostrados como texto. |

## 4. Producto que aún debe confirmar Dulce

| Tema | Qué resolver con la usuaria |
| --- | --- |
| Campos | Obligatoriedad y presentación, respetando los límites del contrato. |
| Tutor | Datos necesarios y su presentación. La regla provisional exige tutor para menores de 18. |
| Escuela y grupo | Finalidad y presentación. |
| Diseño | Visto bueno a [08C sección 3](../requirements/TherapEase-etapa08-modelos-interfaces.md). Lucía decide si cambia una regla. |

No se vuelven a pedir las diez decisiones provisionales aprobadas. Teléfono y última cita no son requisitos de las tarjetas de esta entrega. B06 y el resto de B07 conservan sus bloques; solo datos ficticios, sin nube ni gastos.

## 5. Qué debe leer tu IA

| Orden | Documento | Para qué |
| --- | --- | --- |
| 1 | [AGENTS](../../AGENTS.md), secciones 1–3 | Reglas, límites y revisión humana. |
| 2 | [Plan B02](TherapEase-etapa09-B02.md) y [contrato](TherapEase-etapa09-B02-contrato-para-paginas.md) | Alcance y llamadas del servidor. |
| 3 | [08B](../requirements/TherapEase-etapa08-casos-historias.md) y [08C](../requirements/TherapEase-etapa08-modelos-interfaces.md) | Casos, pantallas y navegación. |
| 4 | [Entrevista](../requirements/TherapEase-09-entrevista-requisitos.md) y [propuesta](TherapEase-etapa09-B02-propuesta.md) | Producto y decisiones ya tomadas. |
| 5 | [Plan de pruebas](../testing/TherapEase-09-plan-de-pruebas.md) | Casos existentes, sin copiarlos en otra tabla. |
| 6 | [Guía B00/B01](TherapEase-09-guia-Dulce-B00-B01.md) | Conservar la base de acceso ya entregada. |

## 6. Próxima entrega

Observaciones corregidas; 77 recorridos correctos con Web publicado localmente. La vista ya está en main por #21. El QA de Lucía está preparado para su propio PR. La condición de fusionar #21 se cumplió; la entrega propia de QA conserva autorización, CI y revisión de Miguel pendientes.

B02 sigue abierto. No se afirma que ya se hizo toda la revisión visual ni Q13.
