# B04 · QA de servidor y páginas de pago

**26-nov-2026:** 25/25 casos propios de servidor y 46/46 propios de páginas correctos sobre main `7578449`, después del squash de #26. Son dos ejecuciones, 71 casos distintos; no se suman las repeticiones anteriores. Build sin advertencias. Lucía autorizó publicar este QA en su PR desde `revision/etapa09`; revisión humana, CI de la entrega y aprobación final pendientes. B04 sigue abierto.

> **En palabras simples:** comprobamos que el pago cambie solo cuando está permitido, que quede guardado con su evento y que la pantalla muestre el resultado real. Si dos personas envían una versión antigua o se pierde una respuesta, no se repite el cambio automáticamente. El recorrido completo entra, registra paciente, agenda, paga y sale.

## Cobertura comprobada

| Parte propia | Casos | Qué se comprobó |
| --- | --- | --- |
| Servidor | 25 | Ambos roles y actor/hora; paciente o ámbito; salir, desactivar, restablecer y cambiar rol en tres momentos; bloqueos reales, confirmación y reversión; respuesta perdida y SinCambios. |
| HTTP de pago | 28 | Sesión y permisos, antifalsificación, valores y versiones inválidos, campos extra ignorados, cancelada/baja, salida como texto, no-store, 404 y reversión real al fallar auditoría. |
| Interfaz y envíos | 7 | Pagado → Pendiente con recarga y auditoría; pérdida de respuesta antes/después del guardado; doble clic desde alta/panel; duplicado HTTP forzado y dos POST esperando realmente en PostgreSQL. |
| Avisos compartidos | 9 | Ocho variantes: cita, agenda tras alta desde panel, pago, sin cambios, paciente, usuario, contraseña normal y temporal; duración de seis segundos, pausa y ocultado. Otro caso comprueba cierre manual. |
| Q16 | 2 | Usuaria y Superusuario: acceso → registrar paciente por formulario → agendar con Pendiente → pagar → recargar → salir; denegación de consulta privada posterior. |

L-CU09-01 queda comprobado en ambos sentidos. CU09/Q03/Q16 y seguridad se siguen en las [tablas existentes](TherapEase-09-plan-de-pruebas.md), sin duplicar casos. Los diez casos de Dulce integrados por #26 son distintos y no se cuentan como QA propio.

## Dónde están las pruebas

| Parte | Archivos |
| --- | --- |
| Servidor: preparación y reglas | [PagoDeCitasRevisionTests.cs](../../tests/TherapEase.IntegrationTests/Citas/PagoDeCitasRevisionTests.cs) |
| Servidor: revocación y cambios simultáneos | [Sesiones](../../tests/TherapEase.IntegrationTests/Citas/Seguridad/PagoDeCitasRevisionSesionesTests.cs) y [concurrencia](../../tests/TherapEase.IntegrationTests/Citas/Concurrencia/PagoDeCitasRevisionConcurrenciaTests.cs) |
| Ayudas y HTTP | [PagoRevision.cs](../../tests/TherapEase.E2ETests/Citas/PagoRevision.cs) y [PagoHttpRevisionTests.cs](../../tests/TherapEase.E2ETests/Citas/PagoHttpRevisionTests.cs) |
| Interfaz, avisos y Q16 | [PagoInterfazRevisionTests.cs](../../tests/TherapEase.E2ETests/Citas/PagoInterfazRevisionTests.cs) y [AvisosDePagoRevisionTests.cs](../../tests/TherapEase.E2ETests/Citas/AvisosDePagoRevisionTests.cs) |

Se reutilizan HTTPS, Identity, PostgreSQL desechable y los apoyos de navegador existentes. No cambia código del producto, dependencias, migraciones ni CI. Las fuentes corresponden a las versiones auditadas y los dos añadidos autorizados; al preparar el commit solo se retiró una línea vacía adicional al final de PagoHttpRevisionTests.cs. Se conservan ambas huellas, sin cambio de código ejecutable. Pruebas comentadas según la skill del proyecto.

## Orden controlado y evidencia

El servidor pausa antes de comprobar o confirmar. Cuando se necesita un bloqueo, se observa con `pg_blocking_pids`, que indica qué conexión espera a otra. Las pausas tienen plazo máximo y se liberan en `finally`.

El doble clic retiene la respuesta después del guardado real; el duplicado garantizado envía dos POST con cuerpo, token y versión idénticos y muestra la respuesta real de conflicto. Son comprobaciones distintas. La prueba simultánea bloquea la fila y observa ambas esperas antes de liberar. Perder la respuesta se simula desde Playwright; no representa una avería física de red. Para fallar auditoría se crea una restricción temporal solo en la base desechable, retirada al terminar.

La ejecución inicial del servidor necesitó corregir el contexto de solicitudes de la prueba; la preparación de páginas necesitó corregir una navegación retenida por la propia prueba. Sus intentos se conservan localmente y no se atribuyen al producto. Las rondas previas fueron 25/25 y dos de 21/21 en servidor; 44/44 y dos de 6/6 en páginas; después los dos opcionales pasaron 2/2. Esta entrega volvió a ejecutar los 25 y los 46 sobre el main fusionado, en rondas separadas.

Evidencia local de esta composición: `servidor.trx`, `paginas.trx`, build y manifiesto de las siete fuentes en la carpeta `evidencia/pr-qa` de la revisión B04. No se suben TRX locales, certificados, registros privados ni paquetes de temporales. Los resultados de CI se consultan en los trabajos del PR.

## Cómo repetir

Con Docker y .NET disponibles, desde la raíz:

```powershell
dotnet build tests/TherapEase.E2ETests -c Release -warnaserror
dotnet test tests/TherapEase.IntegrationTests -c Release --no-build --filter FullyQualifiedName~PagoDeCitasRevisionTests
pwsh scripts/qa/probar-paginas.ps1 -Canal msedge
```

El último comando prepara Web local y ejecuta todo el proyecto de navegador: **165 casos** (109 anteriores de Lucía, 10 de Dulce y 46 propios B04). En CI se usa Chromium. Los trabajos existentes ejecutan sin filtro las pruebas de integración y navegador, por lo que incorporan Q16 al publicar este PR.

## Revisión y límites

Claude aprobó los 25 casos de servidor y el QA de 44 páginas; indicó que basta ejecutar los dos opcionales sin otra auditoría, y ambos pasaron. La revisión humana de Miguel o Dulce sigue siendo obligatoria para este QA de Lucía; después decide Lucía. Su PR se fusiona con **Create a merge commit**.

S-13 usa una sesión de prueba sin rol: los dos roles actuales tienen permiso de pago y no se inventa un perfil de solo lectura. Revocación conserva el límite aprobado al inicio de solicitud. Reprogramación B05, consulta de pago de bajas B06, montos y nuevas operaciones de baja quedan fuera.

Q13 requiere otra persona y no lo acredita Playwright. La decisión de trazas sigue pendiente; permanecen deshabilitadas con resultados seguros, sin reducir pasos de Q16. El certificado local de una ejecución interrumpida queda fuera de cualquier paquete y espera autorización de limpieza. [#15](https://github.com/LuciAguilar/Therapease/issues/15) continúa abierto. [Plan B04](../planning/TherapEase-etapa09-B04.md).
