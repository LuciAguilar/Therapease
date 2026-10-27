# TherapEase — B02 · Propuesta de Miguel ajustada con Lucía

**26-oct-2026 · Propuesta aprobada por Lucía; auditoría de Claude APROBADO v1 con ajustes de texto, sin otra auditoría.** Miguel implementa y avisa a Lucía si encuentra un problema técnico. Original de Miguel del 26-oct, huella `cb7a3b94504b58ef36a563eacbb064405460d55317d36650b5139e8cf5dbf58e`; ajustes documentales elaborados con Lucía/Codex. No se atribuye a Miguel la autoría de esos ajustes.

Base de contraste: main `e636ae7e9cb057d833b74919dbe7b67046fb1141`, con #9 y #10 fusionados y el plan B02 publicado. La rama `b02/fichas-de-pacientes` es la declarada en el original para el trabajo de Miguel. La copia recibida y el original se conservan intactos.

B02 abarca HU04–HU06 y CU03–CU04: registrar, buscar, consultar y editar registros ficticios de pacientes, con auditoría atómica y aviso de conflicto. Quedan fuera B06, B07, montos, «solo particular» y cancelación automática. Páginas, formularios y PageModel siguen con Dulce. Sin datos reales, nube ni gastos; B01/B00/LCA/R04 siguen abiertos.

Fuentes: [AGENTS sección 3](../../AGENTS.md), [plan B02](TherapEase-etapa09-B02.md), [entrevista](../requirements/TherapEase-09-entrevista-requisitos.md), [plan de pruebas](../testing/TherapEase-09-plan-de-pruebas.md), revisión local con la opinión de Claude (Lucía la comparte aparte) y [ADR-04](../architecture/adr/ADR-01-a-05-provisionales.md#adr-04--integridad-y-ciclo-de-datos).

## 1. Lo existente que se reutiliza

| Pieza actual | Uso para B02 y trabajo pendiente |
| --- | --- |
| `Paciente`, `PacienteAmbito`, `ReglasDePaciente` | Conservar identificador, versión, ámbitos y metadatos; ampliar campos y reglas sin duplicar entidades. La versión antigua ya da conflicto. |
| `IRepositorioPacientes`, `RepositorioPacientes`, `PacienteConfiguracion` | Mantener el puerto de Pacientes. Ampliar consultas y escritura; no crear un repositorio por campo o entidad. |
| `IUnidadDeTrabajo`, `IServicioAuditoria` | Alta/edición y su evento en la misma transacción. Éxito de escritura solo tras confirmar. |
| `IAutorizacion`, `Permiso`, `MatrizDePermisos` | Añadir los tres permisos decididos y su matriz completa por rol; conservar controles de B01. |
| `ReglasDeDatosDeCuenta`, `TimeProvider`, `HoraHermosillo` | Extraer validaciones comunes conservando límites; usar reloj y zona existentes, sin dependencias nuevas. |
| `RespuestaServicio<T>` y error 500 general | Reutilizarlos con resultados explícitos; el error general ya aconseja comprobar el resultado antes de repetir. |
| `CoordinadorDeBajaDePaciente` y pruebas existentes | Conservar su comportamiento. No ampliar bajas/recuperaciones ni cancelar citas. |
| CI existente | Mantener build, pruebas y comprobaciones aplicables. Modelo al día no acredita migración de datos anteriores ni reversión. |

Las pruebas existentes deben adaptarse solo donde cambien firmas, sin reducir sus comprobaciones. Miguel identifica esas llamadas antes de implementar; las pruebas de Lucía conservan su autoría y revisión distinta. No se acredita aquí ninguna ejecución nueva.

## 2. Aprobación y confirmaciones de producto

Las diez decisiones de la sección 3.9 ya están aprobadas por Lucía. Los valores provisionales son decisiones expresas para pruebas, no respuestas de la usuaria ni permiso para datos reales. Dulce confirma obligatoriedad, tutor, finalidad de escuela/grupo y presentación, y valida 08C sección 3 antes de las páginas afectadas.

La propuesta y la concreción de registros anteriores de la sección 3.1 quedan aprobadas por Lucía. Claude emitió APROBADO v1 con ajustes de texto y no exige otra auditoría. Miguel implementa esta versión y avisa a Lucía si encuentra un problema técnico; esa comunicación no es una espera previa para empezar. Las confirmaciones de Dulce se conservan antes de las páginas afectadas.

## 3. Propuesta ajustada

### 3.1. Campos, tutor y Contacto

| Campo | Valor aprobado provisionalmente para datos de prueba |
| --- | --- |
| Nombre | Obligatorio, entre 2 y 200 caracteres, recortado y sin caracteres de control/formato. Nombres y apellidos en el mismo texto. |
| Fecha de nacimiento | Fecha sin hora; obligatoria en nuevas altas, no futura ni anterior a 1900. Columna anulable para conservar registros anteriores sin inventar fechas. |
| Teléfono | Opcional, de 7 a 15 dígitos. Admite separadores y `+` inicial al capturar; se guardan dígitos y, si existe, ese signo inicial. |
| Correo | Opcional, máximo 254, recortado y en minúsculas, con la regla de cuentas; no se utiliza para enviar mensajes. |
| Escuela y grupo | Opcionales, hasta 150 y 30 caracteres respectivamente. Su finalidad y presentación las confirma Dulce. |
| Edad | Calculada desde nacimiento y fecha actual en America/Hermosillo; no se guarda. Si falta nacimiento, edad desconocida, nunca cero o una fecha inventada. |
| Tutor | Uno en el mismo registro; obligatorio para menores de 18, opcional para adultos. Nombre hasta 200, parentesco hasta 40 y al menos teléfono o correo con los límites anteriores. Si se proporciona tutor, sus datos requeridos se validan juntos. |

**Editar nacimiento y tutor.** La regla aprobada de menor de 18 se comprueba con el nacimiento resultante de la misma edición: pasar de adulto a menor exige tutor completo antes de guardar. Pasar a adulto no borra automáticamente el tutor; puede conservarse como dato opcional. Cambiar solo el tutor valida sus datos completos. Se guardan ambos campos con un único control de versión y evento; un rechazo conserva íntegro el registro anterior. No se modifica historial ni citas.

**Tratamiento aprobado para registros anteriores:** se pueden consultar sin nacimiento y mostrar edad desconocida. Una edición sin cambios y con versión vigente no obliga a inventar datos. Para confirmar una edición real, se debe completar el nacimiento y, si resulta menor, el tutor; mientras falten, rechazar la escritura explicando qué falta. No se completan ni borran valores automáticamente en la migración. Esta concreción forma parte de la propuesta aprobada; no requiere una nueva decisión antes de implementar.

**Contacto: opción A aprobada.** Lucía aceptó por escrito perder lo que no pueda separarse, exclusivamente en datos de prueba. No se añade un tercer campo de formulario. Antes de retirar la columna, se aplica esta correspondencia aprobada, con las mismas validaciones que se usarán al escribir:

| Contacto anterior | Teléfono / correo nuevos | Pérdida admitida |
| --- | --- | --- |
| Nulo o vacío | Ambos nulos | Ningún dato de contacto recuperable. |
| Correo válido según la regla compartida | Correo normalizado; teléfono nulo | Espacios exteriores y mayúsculas originales. Una `@` por sí sola no acredita un correo válido. |
| Teléfono válido según la regla nueva | Teléfono normalizado; correo nulo | Separadores originales. No eliminar letras para convertir texto libre en un teléfono. |
| Texto libre, mezcla o formato inválido | Ambos nulos | Se descarta el texto no convertible, como aprobó Lucía. |

Se elimina `Contacto` después de copiar los valores convertibles, dentro de la migración. Al revertir, se recrea y se toma el correo si existe; de lo contrario, el teléfono; de lo contrario, nulo. No se recupera texto descartado ni se conservan ambos contactos en una sola columna. Los datos nuevos de nacimiento, escuela, grupo y tutor también se pierden al retirar sus columnas. La revisión SQL y las pruebas deben hacer explícitas esas limitaciones.

**Reutilización.** Compartir las partes puras de validación de nombre/correo, manteniendo 2–150 para nombres de cuentas y 2–200 para pacientes. No mover la dependencia de Identidad a Compartido: el código común no depende de reglas o constantes de Identidad. Conservar las pruebas y comportamiento de cuentas; los nombres de código describen su función, sin códigos de etapa.

### 3.2. Permisos por operación y rol

Condición común: cuenta activa, sesión válida y sin contraseña temporal pendiente. Comprobar el permiso en servidor antes de consultar o cambiar datos. No basta ocultar botones ni llamarse Superusuario.

| Permiso | Usuaria | Superusuario |
| --- | --- | --- |
| `RegistrarPacientes` | Permitido | Permitido solo para datos de prueba |
| `ConsultarPacientes` | Permitido; búsqueda, abrir y ver parecidos | Permitido solo para datos de prueba |
| `ActualizarPacientes` | Permitido | Permitido solo para datos de prueba |
| `DarDeBajaPacientes` | Conservar la decisión provisional ya existente | Conservar lo existente |

Todo B02 utiliza datos ficticios. Antes de datos reales se vuelve a decidir acceso con R04. La lista de parecidos exige `ConsultarPacientes` también dentro de un alta. Para una cuenta hipotética con registrar pero sin consultar, se rechaza la comprobación/confirmación de parecidos antes de leer y devolver solo `SinPermiso`, sin lista ni indicios. La matriz actual concede ambos, pero la prueba separa los permisos para evitar una fuga futura. Esta respuesta no concede un permiso adicional.

Las políticas de Web siguen el patrón existente; Dulce las aplica en sus páginas. Permisos retirados, sesión revocada o temporal pendiente deniegan la siguiente solicitud. Ninguna respuesta de rechazo incluye nombres, fechas o existencia de registros ajenos.

### 3.3. Búsqueda, homónimos y baja por identificador

- Texto de 2 a 100 caracteres, hasta 6 palabras; sin texto o demasiado corto se rechaza. No listar todo al abrir: el directorio es B07.
- Buscar sin distinguir mayúsculas ni acentos; tratar ñ como n. Todas las palabras deben aparecer en el nombre normalizado. Consulta parametrizada; `%` y `_` son literales escapados.
- Solo registros vigentes. Orden estable por nombre y, para empates, identificador; máximo 50 y aviso de afinar al llegar al límite. Las coincidencias de alta tienen límite 10, sin afirmar que la lista sea completa si hay más.
- Cada fila ordinaria muestra identificador/nombre. Nacimiento se expone para distinguir nombres iguales, incluida la lista de parecidos, o para el filtro exacto opcional; no se añade a toda fila por defecto. Si el nacimiento anterior es desconocido, indicarlo sin inventar un valor.
- Sin teléfonos, correo, tutor, escuela, grupo o ámbitos en resultados de búsqueda. Texto buscado fuera de URL, registros técnicos y eventos. La página usa POST y antifalsificación.

Se mantiene `NombreNormalizado`, sin extensiones de PostgreSQL. El mismo contrato de normalización rige alta, edición, búsqueda y conversión de datos anteriores; no depender del idioma del servidor. Incluir como muestras «María/Maria/MARÍA», «Muñoz/Munoz», espacios exteriores/múltiples y nombres admitidos antes. Las formas compuesta y descompuesta de un mismo acento deben coincidir. Miguel demuestra en sus pruebas que el cálculo de la migración produce exactamente las mismas claves que Domain. No se acredita equivalencia por tener funciones SQL parecidas ni se promete que el índice acelere toda búsqueda por fragmentos.

**Homónimos al registrar.** Tras comprobar registrar y consultar, buscar vigentes con el mismo nombre normalizado. Sin confirmación y con coincidencias, no insertar ni auditar: devolver `CoincidenciasPorConfirmar`. Confirmar que es otra persona permite el alta; nombre y nombre/nacimiento no son únicos. Una lista vacía no garantiza que otra solicitud no registre después un nombre igual; se permiten homónimos legítimos. La fecha visible ayuda a seleccionar, sin añadir contactos.

**Baja por identificador: una sola consulta.** Comprobar primero `ConsultarPacientes`. La consulta devuelve uno de tres resultados: no existe; está de baja con solo su condición; o está vigente con contenido y versión. El contenido se proyecta en la base exclusivamente para la condición vigente; para una baja los campos de contenido no se traen. No cargar una entidad completa y ocultarla después, ni leer condición y lanzar una segunda consulta sin protección. Si está de baja: «Está dada de baja». No se habilita B06. La implementación debe demostrar un solo comando SQL para esta consulta, incluida la información necesaria de ámbitos; Miguel lo demuestra en sus pruebas.

Una baja que ocurre después de la lectura no autoriza editar: la escritura vuelve a comprobar condición y versión en servidor y almacenamiento. La prueba con dos conexiones para la ventana de lectura es opcional según Claude; la comprobación de permiso y de ausencia de contenido de una baja es obligatoria.

### 3.4. Doble envío, conflictos y respuesta perdida

**Alta.** Dulce genera el identificador fijo al abrir el formulario y lo conserva al reenviar. Application valida ese identificador y obtiene el actor de la sesión. No se confía en campos ocultos para autorizar.

Si ya existe el mismo identificador y coinciden actor de alta y datos normalizados, devolver `YaRegistrado`, sin una segunda ficha, evento ni incremento de versión. Con otro actor, otros datos o un estado que impida reconocer el reenvío, rechazo fijo sin revelar contenido. No generar otro identificador para resolver la respuesta perdida. Dos personas con identificadores diferentes pueden registrar homónimos; no se promete impedirlo.

Ante dos inserciones simultáneas, reconocer solo la colisión de la clave primaria de Paciente (`PK_Paciente`), no cualquier error de unicidad. Revertir la transacción fallida y descartar la entidad pendiente/contexto fallido antes de una lectura fresca de lo confirmado. Solo esa lectura, con permisos vigentes y comprobación de actor/datos, puede producir `YaRegistrado`. Fallos de auditoría o almacenamiento no se traducen en alta confirmada. Miguel implementa el ámbito de contexto/unidad de trabajo conservando el guardado atómico existente y avisa a Lucía si encuentra un problema técnico.

Reenviar manualmente un alta con el mismo identificador no crea otra ficha. No ver aún una fila no demuestra que la primera solicitud haya terminado; no es necesario afirmarlo para que ese reenvío sea seguro. Sin reintento automático y sin éxito antes de confirmar el cambio/evento o reconocer el alta ya confirmada.

**Edición.** Comprobar primero versión y condición. Una versión antigua siempre da conflicto, incluso si los datos coinciden. Con versión vigente, datos iguales dan `SinCambios`; datos distintos y válidos guardan cambio/evento e incrementan versión. No existe `YaEstabaGuardado` que convierta una versión vieja en éxito.

**Respuesta perdida al editar.** Consultar la misma ficha con permiso vigente y revisar datos/versión; su mera existencia no confirma esa edición. Si no se puede atribuir inequívocamente el resultado a la solicitud, mantener «No pudimos confirmar si se guardó». No repetir automáticamente ni sobrescribir con una versión nueva. La página conserva lo capturado y ofrece recargar para comparar; recargar requiere consultar y es una acción explícita, no reemplazo automático de la captura. No se añade una tabla o servicio nuevo de reenvíos en esta propuesta.

Un fallo inesperado conserva el error 500 fijo existente. No capturar sus detalles en el PageModel ni presentarlo como rechazo de validación. El mecanismo aprobado y sus límites están registrados en la nota de ADR-04.

### 3.5. Ámbitos, auditoría y migración

Escolar e Independiente se pueden añadir/quitar, con al menos uno, sin duplicados ni valores inválidos. No modificar citas actuales o históricas; recuperación/reprogramación con un ámbito retirado sigue para B05/B06.

Sin cambios, después de comprobar versión vigente: sin escritura, evento, cambio de metadatos ni incremento de versión. Si cambia un dato válido, el evento solo contiene nombres de campos cambiados. Alta usa acción `Alta`; edición, `Actualizacion`; frases fijas sin valores. Actor, fechas y metadatos proceden de sesión/reloj, nunca de captura libre. Cambio y evento atómicos. No auditar búsqueda/consulta ni guardar su texto; decidirlo de nuevo antes de datos reales con R04.

Una migración con nombre funcional, sin códigos de bloque:

| Parte | Tratamiento aprobado |
| --- | --- |
| Columnas | Nacimiento `date` anulable; teléfono hasta 20; correo 254; escuela 150; grupo 30; tutor nombre 200, parentesco 40, teléfono 20, correo 254; nombre normalizado según su contrato y límite compatible con nombres admitidos. |
| Datos anteriores | Copiar Contacto convertible conforme a sección 3.1 y luego retirarlo. Solo los demás campos nuevos quedan nulos; nombre normalizado se rellena para todas las filas. No inventar nacimiento/tutor. |
| Normalización | Igual clave en migración y Domain, demostrada con la misma muestra anterior y nuevas escrituras; tratar errores antes de retirar Contacto, sin pérdida distinta de la aprobada. |
| Índice | Índice de nombre normalizado y coherencia con el nombre. No imponer unicidad por nombre/nacimiento. |
| Relaciones/seguridad | Conservar identificadores, versión, condición, metadatos y ámbitos; sin alterar citas, auditoría ni permisos del usuario de app. Revisar que los permisos existentes de Paciente cubran el cambio. |
| Reversión | Recrear Contacto con correo y, si falta, teléfono, antes de retirar columnas. Al combinar dos campos en uno se pierde el otro; los campos nuevos retirados no se recuperan. Explicar todo en SQL de subida/bajada y ejemplos. |

La pérdida aprobada de Contacto no autoriza alterar identificadores, eventos, citas o datos fuera de las columnas descritas. Probar desde el esquema anterior con contactos nulos, texto libre, correo, teléfono y mezclas; aplicar, deshacer y volver a aplicar, comparando el resultado esperado. Las muestras son ficticias y privadas. El control `has-pending-model-changes` y la migración de una base nueva no sustituyen esta prueba ni el examen del SQL por Lucía antes de fusionar.

### 3.6. Contrato para Dulce

Un servicio público de Application, `IServicioPacientes`, coordina el módulo y devuelve resultados sin lógica de persistencia en páginas. Actor obtenido de sesión; identificadores de auditoría, fechas y condición no se admiten como cambios libres. La versión capturada solo sirve para detectar conflicto.

Se reutiliza `RespuestaServicio<T>`, sin cambiar globalmente sus factorías. Para alta, el contenido debe tener un estado explícito; no decidir navegación solo por `Exito`:

| Resultado del alta | `Exito` | Contenido permitido | ¿Anunciar guardado? |
| --- | --- | --- | --- |
| `Registrado` | Verdadero, solo tras confirmar cambio/evento | Identificador y estado | Sí. |
| `YaRegistrado` | Verdadero, solo tras reconocer lo confirmado | Identificador y estado | Sí, indicando que era el alta anterior. |
| `CoincidenciasPorConfirmar` | Falso, con contenido explícito | Estado y lista permitida tras comprobar consultar | No; esperar decisión de registrar otra persona. |
| Rechazo | Falso | Mensaje fijo, sin valores ni lista no autorizada | No. |

La combinación de coincidencias usa las propiedades públicas actuales del contenedor, con una construcción central del servicio y sus pruebas; `Fallida(mensaje)` por sí sola no lleva la lista. No cambiar el significado del resto de servicios por este caso. Es el contrato aprobado; su implementación y comprobación corresponden a Miguel. La aprobación documental no acredita código implementado.

| Operación | Permiso | Entrada | Resultado |
| --- | --- | --- | --- |
| Registrar | Registrar; consultar antes de obtener parecidos | Número fijo, datos y confirmación de homónimo | Los cuatro resultados anteriores. |
| Buscar | Consultar | Texto y nacimiento exacto opcional | Lista permitida, vacía o limitada; sin escritura. |
| Obtener | Consultar | Identificador | Vigente con contenido/versión/edad conocida o desconocida; baja con solo condición; o no encontrado. |
| Actualizar | Editar | Identificador, versión y datos | Actualizado tras confirmar; sin cambios con versión vigente; rechazo o conflicto. Nunca éxito por versión antigua. |

La respuesta de conflicto no recarga por sí misma ni expone la ficha actual sin consultar. Los DTO evitan mostrar valores privados en `ToString`. Mensajes fijos reutilizables en `MensajesDePacientes`. Dulce usa modelos de entrada propios, antifalsificación, salida codificada, POST de búsqueda y `no-store` en contenido privado. Sus pruebas deben comprobar estos estados, sin afirmar guardado al recibir parecidos.

### 3.7. Pruebas previstas, todavía sin ejecutar

| Caso | Comprobación |
| --- | --- |
| M-06 | Campos, límites y ámbitos; tutor exigido en menor y rechazo sin guardar parcialmente. |
| M-07 | Acentos y formas Unicode equivalentes, vigentes, límites, orden estable, comodines literales y homónimos legítimos. Nacimiento solo donde esté permitido. |
| M-08 | Dos ediciones, versiones antiguas con datos iguales/distintos y datos que vuelven al valor anterior: conflicto. Alta repetida: una sola ficha/evento. |
| M-09 | Apertura de baja con solo condición y sin contenido; no actualizar bajas. Consulta SQL única y permisos antes de leer. |
| M-12 | Metadatos de alta/cambio real; sin cambios con versión vigente no altera nada. |
| M-22/M-24 | Fallo de escritura/evento: sin éxito ni cambio parcial. Evento con nombres de campos, sin sus valores ni datos privados en mensajes/resultados técnicos. |
| Permisos | Cada rol/operación, sesión ausente/revocada, cuenta desactivada y temporal pendiente. Registrar sin consultar no recibe parecidos ni ejecuta su lectura. |
| Nacimiento/tutor | Cumpleaños 18, cambio de día en Hermosillo, 29-feb, adulto a menor y viceversa, tutor incompleto/cambiado/retirado y registros anteriores sin datos. Aplicar el tratamiento aprobado de sección 3.1. |
| Resultados | Coincidencias con `Exito=false` conservan lista autorizada y no anuncian éxito; registrado/repetido requieren confirmación. Conflicto conserva captura. |
| Migración | Datos ficticios anteriores, correspondencia de Contacto, claves normalizadas iguales, subida/bajada/nueva subida y pérdida documentada. |
| Reenvío y cortes | Mismo identificador con datos iguales/distintos, actores distintos y solicitud en curso; respuesta cortada antes/después de confirmar. Error de otra restricción o auditoría no se convierte en alta repetida. |

Miguel escribe sus unitarias en el mismo PR como parte de esta implementación aprobada. Lucía adapta las ayudas existentes para integración con PostgreSQL/Testcontainers: L-CU03-01–04, L-CU04-01–02 y 07, Q03/Q07/Q08, permisos y migración. Repetir casos simultáneos; leer estado/eventos tras terminar. La prueba de lectura/baja con dos conexiones es opcional; no se omiten las comprobaciones obligatorias de versión y de no revelar bajas. D-01–D-05/D-09 y el navegador esperan páginas de Dulce. CI aplica sus pruebas reales; no acreditar ejecuciones inexistentes.

### 3.8. Confirmaciones de Dulce

Antes de las páginas afectadas: confirmar obligatoriedad real y disponibilidad de nacimiento, nombre en uno o varios campos, teléfono, finalidad de escuela/grupo, número/datos del tutor, umbral y presentación de homónimos; validar 08C sección 3. Utilizar respuestas ya recibidas, sin volver a preguntar si se quieren los campos solicitados. La búsqueda ordinaria no muestra bajas; la apertura directa autorizada sí muestra solo su condición según la decisión 6.

Los valores provisionales expresamente aprobados permiten preparar el servidor sin esperar otra auditoría ni las páginas. No programar otros valores pendientes por defecto ni modificar silenciosamente reglas cuando llegue una respuesta distinta; se registra el impacto y decide Lucía.

### 3.9. Las diez decisiones ya aprobadas

| Número | Decisión de Lucía |
| --- | --- |
| 1 | Valores de Miguel provisionales solo con datos de prueba; edad calculada, no guardada. |
| 2 | Contacto A; pérdida no convertible aceptada por escrito para datos de prueba. |
| 3 | Un tutor en el mismo registro, obligatorio para menores de 18, provisional. |
| 4 | Tres permisos para Usuaria/Superusuario; los de Superusuario solo con datos de prueba; parecidos requiere consultar. |
| 5 | Búsqueda sin acentos/extensiones nuevas; nacimiento para distinguir homónimos. |
| 6 | Baja por número: solo «Está dada de baja», en una consulta y sin más datos. |
| 7 | Alta con número fijo y nota ADR-04; en edición, mantener conflicto. |
| 8 | Ámbitos editables, al menos uno y citas intactas; sin cambios no guarda/audita. |
| 9 | Sin auditoría de lecturas por ahora; revisar antes de datos reales. |
| 10 | Reutilizar nombre/correo con límites propios: nombres 150 en cuentas, 200 en pacientes. |

Estas decisiones no se someten otra vez como opciones pendientes. Claude comprobó esta versión y emitió APROBADO v1. Las concreciones incluidas forman parte de la propuesta aprobada; no se acreditan pruebas no ejecutadas.

## 4. Trazabilidad de las correcciones

| Corrección de Claude | Ajuste en esta versión |
| --- | --- |
| C1 | Secciones 2, 3.1, 3.8 y 3.9: valores provisionales aprobados y confirmaciones de Dulce separadas. |
| C2 | Secciones 3.1 y 3.5: opción A, correspondencia y límites de reversión; no quedan todos los contactos vacíos por defecto. |
| C3 | Sección 3.4: existencia no confirma edición; alta con mismo número no duplica. |
| C4 | Secciones 3.4, 3.6 y M-08: Lucía eligió conflicto incluso con datos iguales. |
| C5 | Sección 3.6: resultados explícitos; coincidencias no anuncia guardado y conflicto conserva captura. |
| C6 | Sección 3.3: una consulta, contenido solo vigente y condición sola de baja. |
| C7 | Secciones 3.1, 3.3, 3.5 y 3.7: edición de nacimiento/tutor, legado, cálculo compatible y migración con datos previos. Tratamiento de registros anteriores aprobado. |
| C8 | Secciones 3.2, 3.3 y 3.6: consultar para ver parecidos, también al registrar. |
| C9 | Nota registrada en ADR-04 como parte de la propuesta aprobada. |

## 5. Orden de trabajo

1. Miguel implementa la propuesta aprobada por Lucía: permisos, reglas compartidas, campos, migración/persistencia y servicios con sus unitarias. No espera otra auditoría de Claude ni una conformidad previa propia.
2. Si encuentra un problema técnico, avisa a Lucía con el punto concreto y su alternativa; no cambia silenciosamente el alcance o las reglas. Las diez decisiones y el tratamiento de registros anteriores ya están aprobados.
3. Agrupar el trabajo independiente de páginas en un PR, con revisión humana distinta del autor, CI correcta y aprobación final de Lucía. Cualquier división del trabajo requiere su decisión.
4. Mantener el contrato de páginas y los documentos de resultados. Lucía prueba servidor cuando existan servicios; Dulce prepara páginas/PageModel y sus unitarias; navegador después.

La aprobación documental no acredita implementación, pruebas ni cierre de B02. Se conservan el original y la evidencia. Código y pruebas mantienen PR, revisión humana distinta, CI y aprobación final de Lucía. No autoriza datos reales, nube, gastos o limpieza.
