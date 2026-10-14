# TherapEase — Ajustes del PR #5 · B00

**Revisión actual: 12-oct-2026.** [PR #5](https://github.com/LuciAguilar/Therapease/pull/5), commit `60e7932a953c7af72a33837b2e6347712ae423c4`, rama `b00/correcciones-pr4`. Base `709ff615`. Autor: Miguel. Aprobación final: Lucía.

> **Resultado propuesto: CORREGIR menor.** Los ajustes anteriores están implementados y las pruebas pasan. Queda alinear el mensaje genérico del 500 con Q08. Pendiente auditoría solicitada por Lucía; PR #5 y B00/LCA siguen abiertos. No iniciar todavía el PR de QA.

## 1. Alcance comprobado

| Estado | Ajuste | Resultado |
| --- | --- | --- |
| ✅ | Q03, autorización Web y CU04 mínimo | Se conservan los tres ajustes comprobados el 11-oct; arquitectura y consulta pasan de nuevo. |
| ✅ | Baja por Usuaria y Superusuario | Matriz, autorización y unitarias del coordinador cubren ambos roles, cuenta inactiva, temporal y sesión ausente. Usuaria no administra cuentas ni consulta auditoría. Política de citas existentes sigue provisional. |
| ✅ | Resultados de Identity | `GestorIdentidad` detiene la operación cuando Identity rechaza un cambio. Sus unitarias cubren rechazos y el orden de llamadas; la unidad de trabajo real conserva la transacción. |
| ✅ | Ámbitos de Paciente | Colección pública de solo lectura. Guardado y recarga de dos ámbitos en PostgreSQL real correctos; sin cambios de modelo pendientes. |
| ✅ | Cabeceras aprobadas | HTTPS real: nosniff, frame-ancestors 'none', DENY, no-referrer; no-store en contenido privado. CSS real no recibe no-store. CSP de recursos y HSTS final siguen para después. |
| ✅ | 500 fijo sin Identity/base | API y página responden con cuerpo fijo, cabeceras y no-store al retirar SELECT de Identity. Se recuperan al devolver el permiso. **El texto aún requiere el ajuste de §2.** |
| ✅ | README técnico | Corrige límites, excepción Web→Domain, estado del PR #4, permisos y alcance de `/salud`; conserva comandos de base, migraciones e identidad. Los 192/182 se citan como resultados históricos de 7e83c3c. |
| ✅ | AGENTS y descripción del PR | AGENTS coincide byte por byte con `8a08307`. Descripción identifica SHA, resultados nuevos, alcance y propuestas 7–8; aprobadas por Lucía el 12-oct; el recorrido final con páginas no está implementado/probado. |

**Ejecución independiente:** build Release sin errores/advertencias; **257 unitarias y 182 de integración correctas**, cero fallos/omitidas, en ejecuciones separadas. Las **9 adicionales de baja** también pasan por separado: son QA local de Lucía, fuera del PR. La comprobación HTTPS es apoyo temporal; no acredita páginas de negocio o navegador. Evidencia local identificada en las notas de revisión; no son resultados de CI.

## 2. Corrección de Miguel en este PR

| Dónde | Problema → cambio requerido |
| --- | --- |
| `src/Web/Configuracion/ManejadorDeErroresInesperados.cs:9` | El mensaje dice «Intenta de nuevo» para cualquier excepción. Un fallo puede ocurrir después de confirmar un guardado; repetir sin consultar puede duplicar o repetir cambios (Q08, 08B S5). Quitar esa invitación. Usar texto neutral o avisar que hay que comprobar el registro antes de repetir un cambio cuyo resultado no pudo confirmarse. Conservar cuerpo fijo, ausencia de detalles y funcionamiento sin Identity/base. |
| `tests/TherapEase.UnitTests/Web/ManejadorDeErroresInesperadosTests.cs` | Añadir una comprobación del mensaje que evite recomendar repetir a ciegas, para JSON y HTML. Repetir las pruebas afectadas y actualizar SHA/resultados en la descripción. |

**No cambiar la migración ni relajar permisos.** Miguel corrige en su misma rama del PR #5. B00 y plan se consolidan en la entrega de Lucía, después del veredicto; las pruebas de QA se incorporarán cuando ella lo indique.

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
| Miguel | Comprobar resultados de Identity; 500 fijo independiente; ámbitos de solo lectura; aclarar `/salud`. | ✅ Incluidos en PR #5. ⏳ Mensaje del 500, §2, y aprobación/fusión. |
| Lucía | Arranque de QA tolerante al tiempo de espera agotado; contenido del evento HTTP restaurado; documentar `crear-roles.sql` porque pg_dump no copia usuarios. | ⏳ Próximo PR conjunto de QA, con baja/cabeceras y revisión de Miguel o Dulce. No iniciado en esta revisión. |

**Otros pendientes:** ejecución final de los procedimientos 7–8 ya aprobados, producto/PageModel con Dulce, QA restante y decisión B00/LCA. Miguel puede corregir §2 sin esperar páginas; después revisar la entrega de QA cuando Lucía la presente. No adelantar B01 ni nube. Correo noreply sigue como recomendación personal, sin reescribir historial.
