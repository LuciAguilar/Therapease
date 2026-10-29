# TherapEase — Ajustes del PR #5 · B00

> **Continuidad documental del 14-oct-2026:** ✅ respuestas de producto recibidas en [registro de entrevista](../requirements/TherapEase-09-entrevista-requisitos.md). Este informe conserva revisión/evidencia histórica: montos, solo particular y cancelación automática no se atribuyen al código probado B00. ⏳ Detalle/aprobación/implementación de cambios; B00/LCA/R04 abiertos y esta preparación no autoriza Construcción.

**Revisión actual: 13-oct-2026.** [PR #5](https://github.com/LuciAguilar/Therapease/pull/5), commit `bce94091ba614e16f4cce3435697d8c58c2a31ff`, rama `b00/correcciones-pr4`. Base `709ff615`. Autor: Miguel. Aprobación final: Lucía.

> **Resultado de revisión: sin bloqueantes de código.** El 500 ya pide comprobar si se guardó antes de repetir. Build Release correcto, 258 unitarias y 182 integración correctas; PR #5 aprobado y fusionado por Lucía el 13-oct; main 40785d2. Lucía autorizó preparar el PR de QA y avanzar B01 con pendientes B00/LCA visibles.

## 1. Alcance comprobado

| Estado | Ajuste | Resultado |
| --- | --- | --- |
| ✅ | Q03, autorización Web y CU04 mínimo | Se conservan los tres ajustes comprobados el 11-oct; arquitectura y consulta pasan de nuevo. |
| ✅ | Baja por Usuaria y Superusuario | Matriz, autorización y unitarias del coordinador cubren ambos roles, cuenta inactiva, temporal y sesión ausente. Usuaria no administra cuentas ni consulta auditoría. Política de citas existentes sigue provisional. |
| ✅ | Resultados de Identity | `GestorIdentidad` detiene la operación cuando Identity rechaza un cambio. Sus unitarias cubren rechazos y el orden de llamadas; la unidad de trabajo real conserva la transacción. |
| ✅ | Ámbitos de Paciente | Colección pública de solo lectura. Guardado y recarga de dos ámbitos en PostgreSQL real correctos; sin cambios de modelo pendientes. |
| ✅ | Cabeceras aprobadas | HTTPS real: nosniff, frame-ancestors 'none', DENY, no-referrer; no-store en contenido privado. CSS real no recibe no-store. CSP de recursos y HSTS final siguen para después. |
| ✅ | 500 fijo sin Identity/base | API y página responden con cuerpo fijo, cabeceras y no-store al retirar SELECT de Identity. Se recuperan al devolver el permiso. **Mensaje corregido y comprobado en bce9409.** |
| ✅ | README técnico | Corrige límites, excepción Web→Domain, estado del PR #4, permisos y alcance de `/salud`; conserva comandos de base, migraciones e identidad. Los 192/182 se citan como resultados históricos de 7e83c3c. |
| ✅ | AGENTS y descripción del PR | AGENTS coincide byte por byte con `8a08307`. Descripción identifica SHA, resultados nuevos, alcance y propuestas 7–8; aprobadas por Lucía el 12-oct; el recorrido final con páginas no está implementado/probado. |

**Ejecución independiente:** build Release sin errores/advertencias; **258 unitarias y 182 de integración correctas**, cero fallos/omitidas, en ejecuciones separadas. Las **9 adicionales de baja** también pasan por separado: son QA local de Lucía, fuera del PR. La comprobación HTTPS es apoyo temporal; no acredita páginas de negocio o navegador. Evidencia local identificada en las notas de revisión; no son resultados de CI.

## 2. Correcciones verificadas

| Estado | Archivo / resultado |
| --- | --- |
| ✅ | ManejadorDeErroresInesperados.cs y Error.cshtml: «Ocurrió un error. Si estabas guardando algo, revisa si se guardó antes de repetirlo.». Cuerpo fijo sin Identity/base ni datos privados. |
| ✅ | ManejadorDeErroresInesperadosTests: comprueba JSON/HTML y ausencia de «Intenta de nuevo»; descripción del PR actualizada con SHA y resultados. |

No quedan correcciones obligatorias de código identificadas para Miguel en esta revisión. Conservar la migración y los permisos. Las precisiones de §3 no bloquean; el QA nuevo lo revisa Miguel o Dulce en el PR de Lucía.

## 3. Propuestas presentadas y precisiones

| Orden / tema | Situación |
| --- | --- |
| **7 — temporales** | **Aprobado por Lucía el 12-oct:** puntos 1–4, caducidad A para B00 ficticio, B de 24 h antes de datos reales. Verificación por canal conocido, entrega única autorizada sin registros, restablecimiento nuevo ante respuesta perdida sin reintento automático, CU11 y revocación acordados. Regla en AGENTS §2.2. La entrega desde páginas sigue pendiente; no implementar B ahora. |
| Precisión de la propuesta 7 | «Quedan dos eventos» solo si el primer intento confirmó el cambio. Si falló antes del commit y se revirtió, el nuevo intento exitoso deja uno. Conservar esta precisión en la descripción y en las pruebas; la aprobación no implica dos eventos ante cualquier 500. |
| **8 — acceso Q03** | **Aprobado por Lucía el 12-oct: opción A, navegador autenticado.** Superusuario con sesión/permiso vigente; abre la consulta en la misma ventana. Regla en AGENTS §2.3. Script B no incluido. Falta acceso/cierre de sesión de Dulce y prueba final; sin JWT, acceso anónimo ni conexión directa que eluda permisos. |
| Precisión de caché | El middleware propio no añade no-store a rutas anónimas; `/salud` sí lo emite por el framework y `/Error` por ResponseCache. Aclarar el comentario de `CabecerasDeSeguridad.cs:39` y el README si se describe la respuesta final. No es un fallo de las cabeceras ni se debe retirar esa protección. |
| Enlace de Dulce en README §9 | «Informe §8» corresponde al informe PR01, no a este PR05 (que tiene cuatro secciones). Conviene añadir el enlace/nombre explícito. |

## 4. Mejoras aprobadas — incluidas en la entrega

Decisión de Lucía del 11-oct: órdenes 1–3 aprobados completos. Se conserva la separación de autores y revisión humana.

| Quién | Alcance aprobado | Estado / entrega |
| --- | --- | --- |
| Miguel | Cinco cabeceras: `X-Content-Type-Options: nosniff`, `Content-Security-Policy: frame-ancestors 'none'`, `X-Frame-Options: DENY`, `Referrer-Policy: no-referrer`, `Cache-Control: no-store` en contenido privado. | ✅ Incluido en PR #5; comprobación HTTPS real. Navegador posterior; no-store no se impone al CSS. |
| Miguel | Comprobar resultados de Identity; 500 fijo independiente; ámbitos de solo lectura; aclarar `/salud`. | ✅ Incluidos en PR #5. ✅ Mensaje corregido en bce9409. ✅ PR #5 aprobado y fusionado el 13-oct. |
| Lucía | Arranque de QA tolerante al tiempo de espera agotado; contenido del evento HTTP restaurado; documentar `crear-roles.sql` porque pg_dump no copia usuarios. | ✅ Aplicadas y probadas el 13-oct, con baja/cabeceras. ⏳ Revisión humana y fusión del PR conjunto de QA. |

**Otros pendientes:** ejecución final de los procedimientos 7–8 ya aprobados, producto/PageModel con Dulce, QA restante y decisión B00/LCA. Miguel revisa la entrega de QA cuando Lucía la presente. B01 autorizado expresamente el 13-oct en otro chat; nube y datos reales siguen sin autorización. Correo noreply sigue como recomendación personal, sin reescribir historial.
