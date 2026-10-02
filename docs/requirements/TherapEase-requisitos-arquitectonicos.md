# TherapEase — Etapa 03: requisitos arquitectónicos

**v0.4 — Base aprobada; etapa 03 e Inicio/LCO cerrados por Lucía el 21-sep-2026.**
Claude aprobó técnicamente la base v0.2 y la ampliación v0.3. Lucía aprobó la ampliación y las tres mejoras opcionales, incorporadas aquí. La adenda de R05 registra Scrum, la cadencia y la capacidad variable informadas por Lucía. No consta una nueva auditoría integral de v0.4 con esta adenda. Lucía aprobó el cierre de etapa 03 e Inicio/LCO el 21-sep-2026. No se han ejecutado pruebas. Ubicación documental provisional.

## 1. Control por tandas

| Tanda | Puntos | Estado |
| --- | --- | --- |
| 01 — Propósito y alcance | 01A propósito; 01B datos; 01C capacidades y límites | Definida para revisión |
| 02 — Usuarios y acceso | 02A roles; 02B permisos generales; 02C cuentas | Permisos aprobados; cuentas confirmadas |
| 03 — Condiciones de uso | 03A entorno y concurrencia; 03B recursos; 03C dependencias y datos futuros | Definida; correcciones de revisión parcial incorporadas aquí |
| 04 — Calidad y riesgos | 04A protección; 04B.1–04B.5 funcionamiento, recuperación, rapidez, uso y mantenimiento; 04C riesgos y comprobaciones | Criterios y registro base aprobados por Lucía en el chat |
| 05 — Revisión y cierre | 05A consolidación; 05B auditoría y ajustes; 05C pendientes y aprobación | 05A–05C completas; cierre aprobado por Lucía el 21-sep-2026 |

## 2. Alcance y restricciones acordados

Demostración web académica en computadora y con internet. Entrega del avance alcanzado el **04-dic-2026**, sin exigir el 100 %. Registros de pacientes completamente ficticios este semestre; uso y datos reales mucho después. Usuaria usará su computadora propia.

Capacidades prioritarias: acceso seguro; registro, búsqueda, consulta y actualización de pacientes; agendamiento, consulta, reprogramación y cancelación de citas; estado de pago por cita, exclusivamente **pagado o pendiente**. Sin montos, abonos, procesamiento de cobros, facturación ni contabilidad. Usar «pacientes», no «clientes».

Gestión conjunta de pacientes escolares y de consulta independiente, distinguiendo el ámbito y evitando duplicar innecesariamente a quien recibe atención en ambos. La distinción escolar/independiente se mantiene en el alcance de este semestre, sin funciones específicas adicionales para la escuela. No determina separación técnica ni autoriza incorporar información escolar real.

Google Calendar y recordatorios por correo/WhatsApp: diferidos por acuerdo. Importación de datos reales: no decidida; la mayor parte de la información está en papel. Historial clínico, notas y reportes avanzados: fuera del avance previsto por acuerdo. Conservar la cita cancelada y su estado de pago; cancelarla no modifica automáticamente si fue pagada. Los flujos detallados corresponden a la etapa 08.

**Aclaración de continuidad para 08 (26-sep-2026):** «fuera del avance previsto» se refiere a su implementación en el primer avance, no a omitir su análisis. Lucía pidió elaborar en 08 las historias de usuario y los casos de uso necesarios para historial clínico, notas y reportes, con detalles aún por validar con Dulce y Usuaria. Esta aclaración no autoriza programarlos ahora ni usar datos reales; R04 permanece pendiente.

Los dos PDF y cuatro imágenes de `Docs` son antecedentes de escritorio, no requisitos completos, diseño ni tecnologías vigentes. La imagen de SIIFD aportada en el chat solo ejemplifica el formato de tandas.

### Usuarios y permisos generales

- Usuaria: única usuaria operativa inicialmente prevista; administradora de pacientes, citas y estado de pago.
- Equipo: tres personas, cada una con cuenta propia de superusuario para administrar sistema, cuentas y permisos. Acceso completo durante la demostración con datos ficticios; acceso clínico real por acordar antes del uso real.
- Cuatro cuentas iniciales y objetivo de cuatro usuarios simultáneos **para demostración y pruebas**, no cuatro profesionales en operación cotidiana.
- Capturista: posibilidad futura, sin prioridad ni cuenta exigida este semestre. Permisos detallados en etapa 08. Diseñar para permitir crecimiento; implementar lo necesario actualmente.

### Recursos

Presupuesto actual: **$0 MXN**. No equivale a viabilidad gratuita demostrada. Dominio pagado por la psicóloga si resulta necesario: planteamiento de Lucía, aún por confirmar con Usuaria; responsable de obtener respuesta: Lucía.

Capacidades del equipo: Java, C#/.NET, HTML/CSS/JavaScript y bases de datos; Lucía también conoce Windows Forms y WPF/XAML. SQLite es antecedente de TherapEase; SQLCipher es aprendizaje aún no realizado en SIIFD. **Ningún antecedente preselecciona tecnología.** Los tres darán mantenimiento. Su disponibilidad es variable por estudio y trabajo: no se fijan horas semanales; la capacidad se acuerda al planear cada Sprint de dos semanas.

## 3. Criterios y comprobaciones previstas

Cada fila identifica contexto/estímulo, respuesta o medida y evidencia futura. Son requisitos, no mecanismos arquitectónicos ni resultados obtenidos. Se comprobarán en la etapa 09 autorizada; sus procedimientos se precisarán en las etapas correspondientes.

| ID | Contexto y estímulo | Respuesta / aceptación | Comprobación prevista |
| --- | --- | --- | --- |
| Q01 | Intento sin sesión válida | Ninguna consulta o modificación protegida permitida | Intentos sobre pacientes, citas y pagos |
| Q02 | Cierre de sesión y nuevo intento de acceso | No consultar ni modificar información protegida sin volver a entrar | Intentos posteriores al cierre |
| Q03 | Una cuenta modifica paciente, cita o estado de pago | Registrar cuenta, momento inequívoco, registro/campo afectado y hecho del cambio. Historial inspeccionable con permiso, sin exposición de datos sensibles ni alteración silenciosa (Q04/Q19) | Para cada cambio de prueba, identificar esos cuatro elementos; verificar acceso autorizado, ausencia de datos sensibles y rechazo o detección de alteraciones de auditoría |
| Q04 | Fallo o registro técnico de operación | No exponer contraseñas ni datos privados en errores o registros técnicos | Revisión de mensajes y registros de pruebas |
| Q05 | Preparación y uso de la demostración | Registros de pacientes y sus contactos ficticios | Revisión del conjunto de datos |
| Q06 | Dos intentos de agendar citas superpuestas para Usuaria | Rechazar el cruce con otra cita activa, incluso en simultáneo | Pruebas de intervalos y concurrencia |
| Q07 | Demostración/pruebas con cuatro usuarios: dos personas editan el mismo registro | Avisar del conflicto antes de sobrescribir cambios. Robustez del escenario de demostración; la operación real inicial sigue prevista para Usuaria sola | Ediciones simultáneas controladas |
| Q08 | Guardado con respuesta normal o interrupción | Éxito solo con confirmación; avisar si el resultado es incierto | Interrumpir y contrastar con información guardada |
| Q09 | Acceso desde una computadora con otra zona horaria | Referencia **America/Hermosillo**; cita sin cambio accidental y registro temporal inequívoco | Comparar acceso desde zonas distintas |
| Q10 | Fallo con pérdida de datos ficticios | Pérdida máxima de **24 h de cambios** | Recuperación y comparación con datos previos al fallo |
| Q11 | Fallo detectado que interrumpe el sistema | Recuperar funcionamiento en **24 h como máximo desde la detección** | Medir detección y restablecimiento |
| Q12 | Consultar, buscar y guardar con 4 usuarios, 100 pacientes y 1,000 citas ficticios, conexión estable | **Hasta 3 s en al menos 95 % de los intentos** | Prueba de rendimiento con condiciones documentadas; incluir al menos un acceso posterior a suspensión en la muestra |
| Q13 | Persona ajena al desarrollo tras explicación breve | Registrar paciente, agendar cita y marcarla pagada sin ayuda durante las tareas | Observación y registro de dificultades |
| Q14 | Sesión de demostración con fecha/hora acordadas | Completar recorrido principal durante **30 min sin interrupciones del servicio** | Registro de sesión y fallos, incluidos los de conexión; se permite calentamiento documentado antes de iniciar los 30 minutos |
| Q15 | Otro integrante prepara el entorno | Prepararlo y ejecutar verificaciones usando la documentación, sin depender del autor de la configuración | Reproducción por otro integrante |
| Q16 | Cambio en el sistema | Pasar pruebas de funciones afectadas y del recorrido acceso–paciente–cita–estado de pago | Evidencias de pruebas por cambio |
| Q17 | Futura incorporación del capturista | Conservar información y permitir permisos limitados; implementación diferida | Revisión de viabilidad y prueba cuando se autorice incorporarlo |
| Q18 | Requisito usado para decidir o comprobar algo | Identificador y comprobación prevista, vinculables después con decisiones y resultados | Matriz de trazabilidad documental; evidencia ejecutable posterior |
| Q19 | Acceso, intercambio de información y manejo de credenciales | Contraseñas nunca almacenadas ni transmitidas en claro; comunicaciones cifradas; secretos fuera del código; controles contra accesos indebidos | En 09, inspeccionar almacenamiento, comunicaciones y código para comprobar los criterios, y probar denegación de acceso indebido. Mecanismos en 04–05 |
| Q20 | Se da de baja un registro gestionado de paciente o cita | Queda inactivo y fuera de listados/búsquedas ordinarios; sigue existiendo y es consultable/recuperable con permiso. Ninguna baja pierde el dato ni lo borra físicamente en el alcance de demostración | Comparar antes/después: ausente del uso ordinario, conservado y consultable/recuperable con autorización. Cancelar una cita no equivale a darla de baja |

Las cantidades de Q12 son **carga de prueba aprobada**, no un censo real ni una garantía de crecimiento ilimitado. UTC, almacenamiento y conversiones se resolverán en etapas 04–05; una fecha sin hora no se tratará automáticamente como un instante. Q10–Q11 son metas de demostración; revisar con Usuaria antes del uso real. Q14 no exige servicio continuo las 24 horas ni queda garantizado por Q11.

La auditoría de cambios (Q03), los registros técnicos (Q04) y la relación requisitos–decisiones–pruebas (Q18) tienen propósitos distintos.

**Ampliación aprobada técnicamente por Claude y aceptada por Lucía:** Q03 extendido y Q20. «Historial relevante» significa como mínimo cuenta, momento, registro/campo afectado y hecho del cambio; no exige duplicar valores privados anteriores/posteriores. Conservar no implica hacerlo indefinidamente: retención y eliminación definitiva siguen pendientes en R04/sección 5. Mecanismos en 04–05; permisos y flujos detallados en 08.

**Prioridad aprobada:** primero seguridad e integridad; después recuperación y recorrido principal; luego rapidez y ampliaciones. Si hay conflicto con costo o plazo, Lucía decide el ajuste; no reducir silenciosamente criterios aprobados.

## 4. Riesgos aprobados como registro base

| ID | Riesgo | Tratamiento y límite |
| --- | --- | --- |
| R01 | $0 limita servicio y metas | Evaluar condiciones y costos en etapa 05; si son incompatibles, revisar alcance con Lucía |
| R02 | Ediciones simultáneas o manejo de horarios producen errores | Diseñar atención al riesgo en 04 y comprobar Q06–Q09 en 09 |
| R03 | Pérdida de datos o interrupción | Diseñar recuperación en 04–05 y comprobar Q10–Q11 y Q14 en 09 |
| R04 | Incorporar o conservar datos reales sin aclarar sus condiciones | Lucía y Usuaria aclaran procedencia, acceso y tratamiento escolar/particular, plazos de conservación y eliminación definitiva antes del uso real; investigar obligaciones con fuentes oficiales vigentes de México. No se presume conservación indefinida |
| R05 | Método y capacidad del equipo | **Tratado el 21-sep-2026:** Scrum aceptado, Scrumban descartado y Sprints de dos semanas. Sin horas fijas: el equipo acuerda una carga realista en cada planeación según su disponibilidad. Consolidación v0.3 aprobada por Claude y etapa 02 cerrada por Lucía el 21-sep-2026; la 03 requiere su propia aprobación final |

Para R01–R03, Lucía coordina y el equipo revisa su parte. Investigación documental inicial aprobada: hasta 60 minutos por riesgo, en su etapa autorizada, sin programación. Si no basta, registrar lo pendiente; el límite no implica que el riesgo esté resuelto. Conservar fuentes, alternativas, evidencia revisada, riesgo residual y comprobación ejecutable pendiente.

## 5. Pendientes explícitos para la revisión

- **Cierre aprobado el 21-sep-2026:** Lucía confirmó los pendientes diferidos y autorizó cerrar etapa 03 e Inicio/LCO. La etapa 02 ya estaba cerrada y R05 resuelto. No quedan bloqueantes de cierre de 03. Las auditorías aprobatorias de Claude corresponden a base v0.2 y ampliación v0.3; las mejoras posteriores y el cierre fueron aprobados por Lucía.
- **Resuelto por Lucía en 05A (18-sep):** prioridades, coordinación/esfuerzo de R01–R03, cancelaciones, diferimientos clínicos y protección de contraseñas, comunicaciones y secretos, con controles contra accesos indebidos. La auditoría integral aprobó técnicamente la v0.2; mecanismos en etapas 04–05.
- **Parámetros de comprobación por concretar:** condiciones de red/navegador/equipo, muestra y distribución de intentos de Q12; preparación y participante de Q13; ventana de Q14. Equipo propone y Lucía acuerda antes de ejecutar en 09, sin modificar las metas aprobadas. Claude confirmó que son parámetros de ejecución de etapa 09: no condicionan el diseño ni bloquean el cierre de 03.
- **Etapa 04:** mecanismos, límites de confianza y arquitectura candidata. **05:** productos, costos y dominio. **08:** permisos y flujos detallados. **09:** validación ejecutable obligatoria antes de Construcción.
- **Antes del uso real:** condiciones de datos escolares/particulares, acceso clínico, incorporación/migración, metas de operación reales y plazos de conservación/eliminación definitiva (R04; investigación con fuentes oficiales vigentes de México). Lucía coordina con Usuaria. Ninguna conclusión de este borrador demuestra cumplimiento jurídico o aptitud de producción.

## 6. Revisiones recibidas

- Parcial 01–03: CORREGIR; corregidas experiencia sin preselección y concurrencia de demostración frente a Usuaria como única usuaria operativa inicial.
- Integral v0.2: **APROBADO técnicamente**, sin bloqueantes de contenido ni cierre. Claude aceptó que los escenarios medibles corresponden a etapa 03 y que los parámetros pendientes de Q12–Q14 se concretan en 09.
- **Tres mejoras opcionales aprobadas por Lucía y aplicadas:** Q19 verificable; contexto de demostración explícito en Q07; distinción escolar/independiente conservada este semestre sin añadir funciones escolares.
- Ampliación v0.3: Claude emitió **APROBADO**, verificando Q20, Q03 extendido, historial mínimo y retención pendiente; sin bloqueantes y sin reauditar v0.2. Lucía aprobó incorporar la ampliación en firme junto con las tres mejoras opcionales el 18-sep-2026. No equivale al cierre de etapa.
- Etapa 02 cerrada por Lucía el 21-sep-2026, tras la aprobación de Claude de la consolidación v0.3. Scrum, Sprints de dos semanas y capacidad variable por planeación quedan consolidados, sin horas ficticias. Lucía aprobó el cierre de la etapa 03 e Inicio/LCO el 21-sep-2026. Se incorporan en Q12/Q14 las condiciones de prueba ya aprobadas, conservando sus metas. Las etapas 04 y 05 siguen abiertas y requieren sus propios cierres.

**Evidencia de cierre:** Lucía indicó «Apruebo ambas y procede a los ajustes correspondientes» tras revisar los ajustes documentales y la aprobación final de 03 e Inicio/LCO. Se mantienen los diferimientos a 05/05E, 08, 09 y antes del uso real. No se atribuye una nueva auditoría integral ni validación ejecutable a este cierre.
