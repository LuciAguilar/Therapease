# TherapEase — Guía para Dulce · B04 · Estado de pago

**Actualizada el 19-nov-2026 por Lucía con Codex; publicación aprobada por Lucía tras aplicar la corrección de auditoría.** El servidor está integrado en main por [#25](https://github.com/LuciAguilar/Therapease/pull/25), squash `e68dbbe`; las correcciones documentales están publicadas en `50777d6`. Puedes preparar páginas, PageModel —la parte que conecta la pantalla con Application— y sus unitarias con ese servidor. Esta guía concreta el uso del contrato existente y no acredita páginas o pruebas nuevas.

## 1. Qué está disponible

| Parte | Estado y uso |
| --- | --- |
| Acceso, pacientes y agenda | ✅ Integrados. Conservar sus páginas, permisos, navegación, hora de Hermosillo y mensajes. B03 conserva su decisión de cierre aparte. |
| Consulta del pago | ✅ `ObtenerCitaAsync` ya devuelve `EstadoPago` y `Version`. El detalle B03 ya muestra el pago; la lista de agenda no recibe esos campos. |
| Cambio de pago | ✅ `CambiarPagoAsync` y sus pruebas de servidor integrados por #25. CI del PR 4/4; CI de main e68dbbe también correcta. |
| QA propio de servidor de Lucía | ✅ 25 casos auditados; ⏳ su PR, publicación y revisión humana. No bloquea tu preparación de páginas. |
| Tu entrega | ⏳ Consulta y cambio de pago en pantalla, navegación, mensajes y unitarias en un mismo PR. Otra persona revisa; Lucía aprueba. |
| QA de páginas y Q16 | ⏳ Lucía lo prepara con las páginas disponibles. La decisión de trazas sigue pendiente. |

**Alcance:** HU13/CU09, solo Pendiente y Pagado. Una cita nueva sigue naciendo Pendiente, sin campo de pago al agendar. Sin montos, abonos, método, cobros, facturas ni nuevos estados. No implementar reprogramación/cancelación B05, bajas/recuperaciones B06 ni consulta de citas de baja.

## 2. Qué conectar y dónde

Reutilizar el [contrato existente, §4.2 y §4.4](TherapEase-etapa09-B03-contrato-para-paginas.md#44-cambiar-el-estado-de-pago--cu09-b04). Allí están las entradas y todas las respuestas; no crear otro contrato de pago ni copiar sus tablas.

| Parte | Trabajo para tu entrega |
| --- | --- |
| Detalle y panel de agenda | Mantener la consulta autorizada y la distinción entre estado de cita y estado de pago. Integrar la edición sin romper el detalle abierto en panel ni su acceso por URL directa. |
| Control de pago | Puede verse dentro del detalle, pero el formulario envía a una página propia que declara los tres permisos: `CambiarPagoDeCitas`, `ConsultarAgenda` y `ConsultarPacientes`, siguiendo el patrón de `Pacientes/Editar`. Mostrar el control solo si `(await Autorizacion.AuthorizeAsync(User, PoliticasDeAutorizacion.CambiarPagoDeCitas)).Succeeded`, como hace `Pacientes/Detalle.cshtml` con su permiso de edición. Quien solo consulta sigue abriendo el detalle con sus dos permisos. La presentación visual y los textos de ayuda se acuerdan con Lucía. |
| Formulario | Modelo de entrada propio: identificador de la ruta, versión mostrada y pago elegido. Versión ausente o menor que 1 y pago ausente/inválido se rechazan; un campo que no llegó no se convierte en un valor válido por defecto. Conservar lo capturado ante un rechazo conocido. |
| Guardado | Llamar a `CambiarPagoAsync(idCita, versionEsperada, EstadoPago?)`. No enviar actor, fechas, paciente, ámbito, horario ni condición al servicio. Campos extra se ignoran, conforme a S-14. |
| Después del éxito | Distinguir `Datos.Estado`: `Actualizado` o `SinCambios`. Redirigir a una consulta GET —leer después de guardar— para que recargar la página no repita el formulario. El aviso fijo de éxito sí puede ir en TempData, como `Confirmacion` en `Pacientes/Editar`; el modelo privado no se guarda en URL, TempData o registros. |
| Recarga | Consultar de nuevo por `ObtenerCitaAsync` y mostrar el estado vigente. La respuesta mínima de cambio de pago no tiene datos del paciente ni permite reconstruir el detalle de la cita. |
| Cancelada vigente | Se puede consultar y corregir el pago; la cita sigue cancelada. No agregar aquí un botón para cancelar. |
| Cita de baja | GET de detalle responde igual que una inexistente; no mostrar formulario ni crear una consulta nueva. Si un POST autorizado alcanza una cita de baja, mostrar el rechazo fijo del servicio sin datos de la cita. |

**Punto importante:** paciente de baja y cita de baja son cosas distintas. No introducir en la pantalla una prohibición nueva por el estado del paciente o por un ámbito retirado; respetar lo que responde el servicio y ocultar los datos del paciente como ya hace B03.

En Razor Pages, `[Authorize]` protege la página completa, no un `OnPost` por separado. Conservar los dos permisos de `Agenda/Detalle`; no añadirle el de escritura. La página propia que recibe el pago declara los tres permisos y protege sus accesos directos, aunque el control se muestre dentro del detalle o del panel.

Archivos de partida reales: `src/Web/Pages/Agenda/Detalle.cshtml.cs`, `Detalle.cshtml`, `_DetalleDeCita.cshtml`, `src/Web/wwwroot/js/panel-cita.js` y `src/Web/wwwroot/css/agenda.css`. El detalle ya muestra el pago. La ubicación definitiva del formulario se acuerda con Lucía; no hace falta modificar el servidor para prepararlo.

## 3. Mensajes, conflictos y respuesta perdida

Los mensajes exactos están en `MensajesDeCitas`, en Application. Comparar con sus constantes, no con texto escrito otra vez en la página. Mostrar los datos como texto, sin interpretarlos como código.

- **Cambio confirmado:** «Estado de pago actualizado.». Solo anunciarlo tras respuesta correcta del servicio con `Actualizado`.
- **Mismo pago y versión vigente:** «No hubo cambios.». No anunciar una modificación ni esperar un evento de auditoría: describe el pago cuando el servidor lo leyó.
- **Conflicto:** mostrar `ConflictoDeVersion` y ofrecer recargar. Conservar la selección como captura pendiente, sin presentarla como guardada. Tras recargar, mostrar el estado real antes de pedir una nueva decisión; no sustituir la versión antigua y reenviar automáticamente.
- **Doble envío:** frenar clics repetidos mientras se espera; la protección real es la versión en el servidor. Dos envíos que intentan cambiar con la misma versión producen un cambio/evento y un conflicto. Si el valor ya coincidía al leer, pueden responder ambos «Sin cambios», sin evento.
- **Sin respuesta:** no afirmar «No se guardó», no inventar éxito y no reenviar automáticamente. Usar el aviso de resultado incierto de D-03: «No pudimos confirmar si se guardó; revisa el registro antes de repetir». No reutilizar para el pago «No se pudo completar la consulta. Vuelve a intentarlo.» de `panel-cita.js`: afirma un fallo e invita a repetir. Permitir consultar de nuevo el detalle para ver el estado actual; eso no prueba quién lo cambió ni cuál envío lo guardó. Enviar tras esa consulta exige una nueva acción explícita de la persona.
- **Error inesperado:** conservar el error 500 general existente; no capturar una falla de base/auditoría para convertirla en validación, conflicto o éxito. Si el navegador no pudo recibir la respuesta, aplicar el aviso de resultado incierto anterior. No reutilizar el mecanismo de reenvío de reservas B03: el pago no tiene identificador de solicitud.

Mantener estas salidas tanto en página completa como dentro del panel. Si se usa JavaScript, gestionar el botón, el aviso, la recarga y el cierre/reapertura del panel sin dejar una versión vieja oculta. También debe poder completarse mediante el formulario normal, sin depender de JavaScript para los permisos o el guardado.

## 4. Protección y límites que se conservan

| Punto | Qué debe conservar la página |
| --- | --- |
| Consulta | Sesión válida más `ConsultarAgenda` y `ConsultarPacientes`. Consultar no exige el permiso adicional de escritura. |
| Escritura | La página propia de pago declara `CambiarPagoDeCitas`, `ConsultarAgenda` y `ConsultarPacientes`, antes de validar/leer datos. Proteger también la URL y el envío directos; esconder el botón no protege. El servicio vuelve a comprobarlos. |
| Sesión | Ambos roles tienen el permiso con datos ficticios. Sin sesión o con temporal pendiente, reutilizar el acceso existente; sin permiso, denegar sin revelar si existe la cita. Revocar después del inicio de una solicitud no garantiza detenerla; la siguiente solicitud se niega. No prometer revocación instantánea. |
| Formulario | Antifalsificación de Razor activa, incluso en el panel; validar también en servidor. Usar una entrada pequeña, no enlazar una entidad de Domain ni datos internos. La versión oculta se puede manipular y siempre se valida. |
| Datos y errores | Mantener `no-store` en contenido privado, salida codificada y `textContent` para datos. No registrar formularios, contraseñas, cookies, contactos ni valores privados; tampoco en evidencia descargable de CI. |
| Capas y evidencia | PageModel llama a Application; sin base de datos directa, dependencias nuevas, migraciones ni cambios al mecanismo de pago. No habilitar trazas de identidad/Q16 mientras Lucía no decida y se compruebe su seguridad. |

## 5. Qué cubrir en tu PR y qué sigue con Lucía

Tu PR reúne las páginas y sus unitarias. Reutiliza `tests/TherapEase.UnitTests/Web/PaginasDeAgendaTests.cs`, actualizando sus dobles del servicio con comportamiento explícito de pago; la implementación provisional que lanza `NotSupportedException` no sirve para probar esta función. Nombres descriptivos sin B04 ni fechas, conforme a AGENTS.

En las unitarias, cubrir lo que decide el PageModel: entrada ausente/inválida; argumentos correctos enviados al servicio; permisos y rechazo sin datos; `Actualizado` frente a `SinCambios`; conflicto sin cambiar la versión del reenvío; inexistente/de baja; redirección tras éxito; error inesperado sin falsa confirmación. Comprobar que el detalle conserva sus dos permisos y la página propia de pago declara los tres. No duplicar las pruebas de reglas y PostgreSQL de Miguel. Que quien solo consulta vea el detalle sin control de pago, además del comportamiento de JavaScript, panel y tráfico HTTP, requiere comprobarse en navegador/integración: una unitaria del PageModel no lo acredita.

Referencias de cobertura en el [plan de pruebas](../testing/TherapEase-09-plan-de-pruebas.md), sin copiar sus tablas:

- **D-01–D-05 y D-11–D-14:** formularios, mensajes y detalle; de D-11 solo el pago, las acciones B05/B06 conservan su bloque.
- **CU09, M-17/M-22 y Q03:** pago, errores, versión y auditoría. Servidor ya probado; Lucía completa HTTP/navegador. Cancelación, baja o reprogramación reales se amplían en B05/B06.
- **S-11, S-13–S-17 y S-19:** URL y POST directos, permisos, campos extra, identificadores, antifalsificación, salida como texto y validación; Lucía dirige este QA.
- **L-Q16-01/02:** con Usuaria y Superusuario, entrar → registrar paciente → agendar con Pendiente → marcar Pagado → recargar → salir. Lo prepara Lucía con páginas reales; las llamadas al servicio no sustituyen Q16. Q13 requiere además que otra persona use el recorrido sin ayuda.

Los cambios simultáneos y las respuestas perdidas se comprueban con pausas controladas en pruebas, sin depender de acertar dos clics a la vez ni introducir pausas en producción. Lucía conserva ese QA; señala si tu interfaz necesita un apoyo de prueba antes de añadir mecanismos nuevos.

## 6. Lectura y entrega

Leer [AGENTS §1–3](../../AGENTS.md), [plan B04](TherapEase-etapa09-B04.md), contrato B03 §4.2/§4.4/§7 y HU13/CU09 de [08B](../requirements/TherapEase-etapa08-casos-historias.md). Usa el plan de pruebas para el estado de los casos; esta guía no los marca ejecutados.

Antes de empezar, incorporar main en tu rama de trabajo; no necesitas esperar la publicación del QA propio de Lucía. Entregar un PR de páginas, PageModel y unitarias, con resultado de build/pruebas aplicables, comprobación del panel y URL directa, revisión de otra persona y CI correcta. Informar qué quedó sin comprobar. Lucía revisa, decide los detalles de pantalla y aprueba la fusión; después completa su QA. Miguel mantiene apoyo técnico sin rehacer el servidor aprobado. Solo datos ficticios, sin nube ni gastos.
