# B04 — Corrección del envío de pago desde el alta por URL

25-nov-2026. Ajuste solicitado y autorizado en PR #26 antes del QA de Lucía. Se incluyen los opcionales aprobados de ocultado CSS sin JavaScript y foco en aviso con texto o título visible. Se conserva el estilo aceptado de notificaciones: seis segundos, cierre manual y pausa al interactuar.

## Cambio

`pago-cita.js` comprueba el contenedor `data-contenido-detalle-cita` antes de cancelar el envío, marcarlo pendiente o bloquear controles. Si no existe —como en el detalle mostrado tras agendar por URL— conserva el POST normal y su redirección. No añade reintentos ni sustituye la versión.

Los resultados insertados por JavaScript enfocan un aviso de error con texto o el título visible; en el panel se usa su encabezado cuando el título interior está oculto. Reserva aplica la misma selección para evitar enfocar avisos vacíos. Sin JavaScript, el aviso conserva su diseño y se oculta mediante CSS tras seis segundos; el tiempo se pausa con interacción.

## Comprobación local

Regresiones nuevas en `tests/TherapEase.E2ETests/Citas/PagoDeCitaPaginasTests.cs`:

- Éxito desde alta por URL, detalle directo y panel, con Usuaria y Superusuario: seis casos. Confirman mensaje, pago vigente, versión nueva, persistencia después de otra consulta y un único POST. Verifican que solo el alta por URL usa navegación normal y el foco de los resultados AJAX llega al título visible.
- Conflicto desde los tres accesos: tres casos. Una edición competidora confirmada vuelve obsoleta la versión visible; se conserva la selección pendiente, no se reenvía ni se ofrece una versión nueva para guardar. La recarga explícita muestra el pago vigente. En AJAX se comprueba el foco en el error.
- Alta y cambio de pago sin JavaScript: un caso. Confirma redirección, estado y ocultado CSS del aviso.

Resultado final: **10/10 correctos, 0 fallidos, 0 omitidos**, Edge instalado, HTTPS local y PostgreSQL desechable. TRX local seguro: `TestResults/correccion-pago-edge/pago-corregido.trx`. Sin trazas de identidad ni formularios en evidencia. La primera ejecución tuvo 7 correctos y 3 fallos de preparación del panel por intentar cerrar un aviso detrás del diálogo; se retiró ese paso innecesario y se repitieron los diez casos completos.

Build E2E y publicación local Web correctos, sin advertencias ni errores. `node --check` de los dos scripts y `git diff --check` correctos. Los recursos publicados de ambos scripts coinciden por SHA-256 con los usados en la comprobación.

Esta regresión no sustituye el QA dirigido de Lucía, Q16, Q13, la revisión humana distinta ni la aprobación final. No se habilitan trazas, no se fusiona el PR y no se cierra B04. La CI del nuevo commit se verifica después del push; no se acredita por este registro anticipadamente.
