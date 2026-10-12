# TherapEase — Ajustes del PR #5 · B00

**Revisión: 11-oct-2026.** [PR #5](https://github.com/LuciAguilar/Therapease/pull/5), commit `7e83c3c6033c6c0df9dd15c2b751950e631c5475`. Autor: Miguel. Aprobación final: Lucía.

> **Resultado propuesto: CORREGIR menor.** Los tres ajustes de código iniciales están comprobados sobre 7e83c3c. Lucía aprobó el 11-oct la baja provisional para Usuaria y Superusuario. Miguel debe implementar ese permiso, las cabeceras y las cuatro mejoras de backend aprobadas el 11-oct, con sus unitarias, y actualizar README/AGENTS. Se comprobará su nuevo commit; B00/LCA y R04 siguen abiertos.

## 1. Alcance comprobado

| Estado | Ajuste del PR #4 | Resultado |
| --- | --- | --- |
| ✅ | Filtro de auditoría Q03 | Acepta nombres completos sin distinguir mayúsculas; rechaza números y listas con comas con 400. |
| ✅ | Permisos desde Web | El manejador consulta `IAutorizacion`; ya no llama directamente a `MatrizDePermisos`. Se crea por solicitud. |
| ✅ | Coordinador CU04 mínimo | Está en Application, fuera de Pacientes/Citas. Consulta citas activas; conserva el bloqueo PostgreSQL y guarda baja/evento juntos. |

La evidencia ejecutable y sus límites se registran en **B00 §4.9**. Las comprobaciones adicionales de QA son locales; no forman parte del PR de Miguel.

## 2. Ajuste de Miguel en este PR

**Miguel reúne README, permiso, cabeceras y mejoras de backend aprobadas, con sus unitarias, en la misma rama del PR #5.** Lucía revisa ese commit. B00 y el plan de pruebas se actualizan en la entrega documental de QA; no duplicar esos cambios en este PR.

| Dónde | Qué corregir |
| --- | --- |
| `README.md:155` | Quitar que la prueba automática de límites aún no existe. Ya existe y pasa sobre este commit. Enlazar el estado de B00. |
| `README.md:162` | Añadir la excepción aprobada: Web puede usar enumeraciones y contenedores de constantes de Domain; entidades, reglas y servicios pasan por Application. AGENTS sigue siendo la fuente de reglas. |
| `README.md:96–120` | Actualizar el estado: PR #4 fusionado y revisado por Miguel; los siete fallos anteriores están corregidos en PR #5. Se reprodujeron 192 unitarias y 182 de integración correctas, en ejecuciones separadas; no son resultados de CI. Conservar comandos y documentación técnica existentes. |
| `src/Domain/Identidad/Reglas/MatrizDePermisos.cs` y unitarias afectadas | Autorizar `DarDeBajaPacientes` a Usuaria y Superusuario. Mantener administración de usuarios y consulta de auditoría solo para Superusuario. Corregir la unitaria que esperaba denegar la baja a Usuaria y comprobar permiso concedido/denegado, sesión ausente, cuenta inactiva y temporal pendiente. No debilitar la protección del último superusuario ni las otras reglas. |
| `AGENTS.md`, regla de seguridad en servidor | Registrar o conservar las decisiones del 11-oct de AGENTS §2.5–§2.6 (cabeceras y paquetes aprobados), sin duplicarlas, y la baja provisional para Usuaria y Superusuario, con autorización de servidor, evento atómico y protección de citas activas. Dulce valida la política operativa con la usuaria. No presentar esta aprobación como implementación comprobada. |

**Al subir el nuevo commit:** Miguel actualiza la descripción del PR con SHA y resultados reales; Codex comprueba el cambio y repite las pruebas afectadas. Los 192/182 y nueve adicionales registrados corresponden a la versión anterior 7e83c3c, no acreditan todavía el permiso ampliado.

**En la descripción del PR #5, Miguel presenta por escrito las propuestas de B00 §7, órdenes 7–8:** cómo verificar la identidad y entregar contraseñas temporales de forma segura (incluida respuesta perdida y repetición que invalida la anterior), y cómo accede el operador autenticado a la consulta Q03. Sin secretos en registros, JWT ni acceso sin autenticación. Son propuestas pendientes de decisión de Lucía; no se dan por aprobadas con los paquetes siguientes.

**Textos de referencia para el ajuste:**

- Encabezado de dependencias: «Dependencias permitidas según AGENTS §1. Las 30 pruebas automáticas de límites pasan sobre el commit revisado del PR #5; el recorrido sobre páginas funcionales sigue pendiente».
- Fila Web: «Application; Domain solo para enumeraciones y contenedores de constantes sin lógica; Infrastructure solo para componer dependencias en el arranque».
- Estado de QA: «PR #4 revisado por Miguel y fusionado. Sobre `7e83c3c` del PR #5: 192 unitarias y 182 de integración correctas, en ejecuciones separadas. Los siete fallos anteriores están corregidos; PR #5 sigue pendiente de aprobación/fusión. E2E espera las páginas de Dulce».

## 3. Decisión aprobada y política pendiente

| Tema | Implementación actual | Quién resuelve |
| --- | --- | --- |
| `DarDeBajaPacientes` | **Aprobado por Lucía el 11-oct: Usuaria y Superusuario, provisionalmente.** El commit revisado aún permite solo Superusuario. | Miguel implementa y prueba en PR #5; Lucía verifica/aprueba el PR. Dulce valida la política operativa con la usuaria. |
| Citas existentes al dar de baja | Rechaza la baja; no cancela citas automáticamente. | Dulce valida la política y Lucía decide. Conservar la protección aprobada: nunca cita activa de paciente de baja. |

**Alcance aprobado:** únicamente el permiso de baja de pacientes para ambos roles. No concede administración de usuarios ni consulta de auditoría a Usuaria. Mantener controles de servidor, evento obligatorio y protección ante citas activas. No resuelve automáticamente qué hacer con las citas existentes al solicitar la baja.

## 4. Mejoras y cabeceras aprobadas — incluidas en la entrega

**Decisión de Lucía, 11-oct-2026: órdenes 1–3 aprobados completos.** Miguel incluye cabeceras y las cuatro mejoras de backend en **este PR #5**. Lucía agrupa las tres mejoras de QA con sus pruebas en **su próximo PR conjunto**, revisado por Miguel o Dulce. La aprobación del alcance no acredita implementación, ejecución ni fusión.

| Quién | Cambio aprobado | Entrega / estado |
| --- | --- | --- |
| Miguel | `X-Content-Type-Options: nosniff`, `Content-Security-Policy: frame-ancestors 'none'`, `X-Frame-Options: DENY`, `Referrer-Policy: no-referrer` y `Cache-Control: no-store` en contenido privado. CSP de recursos se completa con las páginas; HSTS final se decide al desplegar. | **Incluido en este PR #5.** Implementar y añadir unitarias; Lucía verifica cabeceras reales en su QA. |
| Miguel | Comprobar resultados de `UpdateAsync`, `AddToRoleAsync`, `RemovePasswordAsync` y operaciones relacionadas de Identity; no confirmar una operación rechazada. | **Incluido en este PR #5.** Implementación y unitarias pendientes. |
| Miguel | Error 500 con respuesta genérica fija, independiente de Identity/base de datos y sin datos privados. | **Incluido en este PR #5.** Implementación y unitarias pendientes. |
| Miguel | Ámbitos de Paciente como colección de solo lectura. | **Incluido en este PR #5.** Implementación y unitarias pendientes. |
| Miguel | Aclarar que `/salud` comprueba el proceso, no la disponibilidad de PostgreSQL. | **Incluido en este PR #5.** Documentación técnica pendiente. |
| Lucía | Arranque del servidor de pruebas más tolerante al tiempo de espera agotado. | **Incluido en su próximo PR conjunto de QA.** Miguel reprodujo el problema en PR #4; no ocurrió en la última ejecución. |
| Lucía | Comprobar contenido del evento HTTP restaurado además del 200. | **Incluido en su próximo PR conjunto de QA.** Implementación y ejecución pendientes. |
| Lucía | Documentar `crear-roles.sql` para servidor nuevo: `pg_dump` no copia usuarios de PostgreSQL. | **Incluido en su próximo PR conjunto de QA.** Documentación pendiente. |

**Recomendación personal restante:** cada autor puede usar correo noreply en futuros commits; no implica reescribir el historial publicado.

Los pendientes compartidos y responsables se mantienen en **B00 §6–§7**. Las propuestas de los órdenes 7–8 van en la descripción del PR #5 (§2); requieren decisión posterior de Lucía. Páginas, producto y cierre B00/LCA continúan pendientes.
