# TherapEase — 09/B02 · Registrar, buscar, consultar y editar pacientes

**Actualizado el 26-oct-2026 · Plan aprobado por Lucía tras incorporar las dos correcciones obligatorias de Claude.** Claude recomendó aprobar después de aplicarlas sin otra auditoría; Lucía respondió «Apruebo» al plan corregido el 26-oct. La apertura de B02 está autorizada en AGENTS §3; se publicó en `b862d99` y sigue vigente en main `a7c0c52`. Este plan organiza el trabajo; las diez decisiones posteriores del 26-oct están en AGENTS sección 3 y prevalecen sobre los pendientes iniciales de este plan. La propuesta ajustada está aprobada por Lucía y auditada por Claude (APROBADO v1 con ajustes de texto, sin otra auditoría). Miguel implementa y avisa si encuentra un problema técnico. Cada entrega conserva las autorizaciones y revisiones de Lucía que correspondan.

**Punto de partida:** #9 y #10 ya están fusionados. Miguel (`GalloNav`) aprobó los diez casos de #10 sobre `3a44332`; Lucía fusionó con Create a merge commit en `a7c0c52`, el 26-oct a las 13:23:52 de Hermosillo. La condición previa de #10 está cumplida y la rama global ya incorpora main. Miguel ya entregó su propuesta y Lucía aprobó las diez decisiones el 26-oct, incluidas reglas provisionales solo para datos de prueba y la pérdida no convertible de `Contacto` (opción A). Las decisiones vigentes están en AGENTS sección 3. La versión ajustada y la nota ADR-04 quedan aprobadas; Miguel implementa y avisa a Lucía si encuentra un problema técnico. B01 sigue abierto por páginas/navegador; B00/LCA/R04 también siguen abiertos.

B02 permite guardar una ficha ficticia, encontrarla, consultarla y corregirla. Si dos personas intentan cambiarla, se avisa para evitar que una borre sin saberlo los cambios de la otra. Cada alta o edición se guarda junto con su registro de auditoría.

Miguel prepara el servidor y sus unitarias. Dulce hace las páginas, formularios y PageModel. Lucía prepara y ejecuta QA; Miguel o Dulce revisa sus pruebas y Lucía da la aprobación final.

✅ = decisión aprobada o comprobación indicada en la fila. ⏳ = trabajo, decisión o prueba pendiente. Leer el código no equivale a ejecutar sus pruebas.

## 1. Qué vamos a entregar y de dónde partimos

| Estado | Parte | Resultado esperado | Referencia |
| --- | --- | --- | --- |
| ⏳ | Registrar | Crear una ficha válida y poder consultarla después de guardar. | HU04; CU03 |
| ⏳ | Buscar y consultar | Encontrar pacientes vigentes, distinguir personas con el mismo nombre y abrir la ficha autorizada. | HU05; CU03 |
| ⏳ | Editar | Corregir datos permitidos y avisar si la ficha cambió desde que se abrió. | HU06; parte de CU04 |
| ⏳ | Proteger y auditar | Comprobar sesión y permiso en cada operación; guardar cambio y evento juntos, sin datos privados en los registros. Solo pacientes y tutores ficticios de demostración. | Q01, Q03, Q04, Q05, Q07, Q08, Q20 |
| ✅ | Apertura documental | Autorización del 25-oct registrada en AGENTS y publicada en main `b862d99`. | Decisión de Lucía |

Fuentes: [AGENTS](../../AGENTS.md), [08B: historias y casos](../requirements/TherapEase-etapa08-casos-historias.md), [08C: datos y páginas](../requirements/TherapEase-etapa08-modelos-interfaces.md), [08D: alcance de B02](TherapEase-etapa08-backlog-trazabilidad.md), [entrevista recibida](../requirements/TherapEase-09-entrevista-requisitos.md), [plan de pruebas](../testing/TherapEase-09-plan-de-pruebas.md), [requisitos de arquitectura](../requirements/TherapEase-requisitos-arquitectonicos.md) y [README técnico](https://github.com/LuciAguilar/Therapease/blob/main/README.md). Las decisiones posteriores aprobadas prevalecen sobre pendientes históricos.

## 2. Qué hará cada persona

| Estado | Persona | Trabajo completo de B02 | Dependencia |
| --- | --- | --- | --- |
| ✅ | Miguel | Propuesta ajustada, contrato, tratamiento de registros anteriores y nota ADR-04 aprobados para implementar. | Claude APROBADO v1; no requiere otra auditoría ni confirmación previa de viabilidad. Avisar a Lucía si aparece un problema técnico. |
| ⏳ | Miguel | Implementar registrar, buscar, consultar y editar desde Application; validar permisos y datos, guardar auditoría, resolver conflictos y proteger reenvíos. Escribir sus unitarias y mantener CI. | Implementado el 27-oct y publicado en el [PR #11](https://github.com/LuciAguilar/Therapease/pull/11) (CI correcta sobre `1eab29c`); correcciones de Lucía del 28-oct aplicadas en `a78016a`, con CI correcta ([evidencia](../testing/TherapEase-09-B02-evidencia-servicios.md)). Sigue ⏳ hasta la revisión de otra persona. |
| ⏳ | Miguel | Entregar a Dulce las entradas, resultados y errores que usarán sus páginas; documentar migraciones y ejecución local si cambian. Corregir fallos de integración del servidor. | [Contrato B02](TherapEase-etapa09-B02-contrato-para-paginas.md) escrito; migración explicada en la evidencia. Sigue ⏳ hasta su revisión. No escribir los PageModel de Dulce. |
| ⏳ | Dulce | Obtener con la usuaria los detalles de producto que faltan: obligatoriedad de campos, datos del tutor y finalidad de escuela/grupo, además de presentación y selección. Dar su visto bueno a 08C §3 antes de las páginas afectadas; preparar páginas, formularios, PageModel y sus unitarias. | Usar las respuestas ya recibidas y las opciones/contrato de Miguel; Lucía revisa privacidad y decide. Conectar las páginas con los servicios aprobados. |
| ⏳ | Lucía | Preparar el plan y los casos de QA local ahora; después comprobar servidor, permisos, guardado, reenvíos y cambios simultáneos. Probar formularios y navegador al recibir las páginas. | Sin commits. #10 ya está fusionado; los casos funcionales esperan los servicios aprobados e implementados. Por ahora se mantiene la preparación escrita. |
| ⏳ | Otra persona | Revisar código y pruebas del autor correspondiente. Miguel o Dulce revisa las de Lucía. | Entrega lista y CI correcta; aprobación final de Lucía. |

Miguel y Dulce eligen sus herramientas. Cada autor escribe sus unitarias. La auditoría de IA no sustituye la revisión humana.

## 3. Decisiones aprobadas para implementar

La [propuesta ajustada](TherapEase-etapa09-B02-propuesta.md) concreta las diez decisiones de AGENTS sección 3 y el tratamiento de registros anteriores. Claude emitió APROBADO v1 con ajustes de texto, sin otra auditoría. Las marcas siguientes acreditan decisiones documentales, no funciones implementadas o pruebas ejecutadas.

| Estado | Tema | Regla aprobada |
| --- | --- | --- |
| ✅ | Alcance | HU04–HU06 y CU03–CU04 con registros ficticios; B06/B07, montos, «solo particular» y cancelación automática fuera. |
| ✅ | Campos y edad | Valores de la propuesta provisionales solo para pruebas; edad calculada, no almacenada. Dulce confirma producto y valida 08C sección 3 antes de las páginas. |
| ✅ | Tutor y registros anteriores | Un tutor, obligatorio para menores de 18. Consulta de legado con edad desconocida; una edición real exige nacimiento y tutor si resulta menor. Con versión vigente y sin cambios, no inventar datos ni escribir. Editar nacimiento/tutor se valida y guarda junto; no borrar tutor automáticamente al pasar a adulto. |
| ✅ | Contacto | Opción A: sustituir por teléfono/correo. Pérdida no convertible aceptada por Lucía solo para datos de prueba. Correspondencia y límites de reversión en la propuesta; SQL y pruebas pendientes de implementación. |
| ✅ | Permisos | Registrar, consultar y editar para Usuaria/Superusuario; los de Superusuario solo con datos de prueba. Consultar obligatorio también para la lista de parecidos. Sesión y autorización en servidor antes de leer/escribir. |
| ✅ | Búsqueda y homónimos | Sin distinguir acentos ni extensiones nuevas; nacimiento para homónimos, no por defecto en toda fila. Vigentes, límites y orden estable según propuesta. No unicidad por nombre/nacimiento ni directorio B07. |
| ✅ | Baja por identificador | Una consulta, contenido solo si está vigente; de una baja mostrar solo «Está dada de baja», después de autorizar. No consulta de contenido B06. |
| ✅ | Reenvío y conflicto | Alta con identificador fijo y nota ADR-04, sin duplicar ficha/evento. Edición con versión antigua da conflicto incluso con datos iguales. La existencia de la ficha no demuestra una edición cuya respuesta se perdió. |
| ✅ | Ámbitos y cambios | Al menos Escolar o Independiente; se editan sin modificar citas. Con versión vigente y sin cambios, sin escritura, evento ni incremento de versión. |
| ✅ | Auditoría y contrato | Alta/edición y evento atómicos, sin valores privados. Sin auditoría de lecturas por ahora. Contrato distingue parecidos y guardado; conflicto conserva captura. |
| ✅ | Reutilización | Validaciones comunes con límites propios: nombres 150 en cuentas y 200 en pacientes. Conservar comportamiento y pruebas de cuentas. |

Miguel implementa y avisa a Lucía si encuentra un problema técnico. Si surge un cambio de mecanismo o arquitectura, se aplica la regla de ADR de AGENTS; no modificar reglas silenciosamente.

## 4. Qué existe y qué falta completar

Comprobación por lectura de main `6526607fbd59ffda966093d65f04467da1a959ad` y del clon activo. Los archivos de pacientes y permisos citados coinciden con esa base. No se ejecutaron pruebas de software en esta preparación.

| Estado | Base encontrada | Cómo se aprovecha / qué falta |
| --- | --- | --- |
| ✅ | `Paciente` y `PacienteAmbito` | Hay nombre, contacto, condición, versión y ámbitos. Los campos adicionales de entrevista y tutor no están desarrollados en esta ficha. |
| ✅ | `ReglasDePaciente` | Ya valida nombre y ámbitos y controla versión y condición. La edición actual modifica nombre/contacto; adaptar solo lo que se apruebe. |
| ✅ | `IRepositorioPacientes` y su implementación | Permiten agregar y obtener por identificador con ámbitos. No son todavía los servicios autorizados de registrar, buscar, consultar y editar. |
| ✅ | Identidad y matriz de permisos | Reutilizar sesión y controles de B01. Hoy existen Usuaria y Superusuario, con decisiones explícitas para administración de usuarios, auditoría y baja de pacientes; se implementan los permisos de alta, búsqueda/ficha y edición ya aprobados. |
| ✅ | Base de baja y sus pruebas | Se conserva. B02 no añade bajas, recuperaciones ni cancelación automática. Comprobar que lo existente no se rompa al cambiar la ficha. |
| ⏳ | Servicios completos de pacientes | Implementados en el [PR #11](https://github.com/LuciAguilar/Therapease/pull/11) según el contrato aprobado; las correcciones del 28-oct ya tienen CI correcta; falta la revisión humana distinta. |
| ⏳ | Campos nuevos y páginas | Miguel prepara los cambios aprobados de datos; Dulce los conecta y prueba en sus PageModel. |

Fuentes de código en esa base: [ficha](https://github.com/LuciAguilar/Therapease/blob/6526607fbd59ffda966093d65f04467da1a959ad/src/Domain/Pacientes/Entidades/Paciente.cs), [reglas](https://github.com/LuciAguilar/Therapease/blob/6526607fbd59ffda966093d65f04467da1a959ad/src/Domain/Pacientes/Reglas/ReglasDePaciente.cs), [acceso a datos](https://github.com/LuciAguilar/Therapease/blob/6526607fbd59ffda966093d65f04467da1a959ad/src/Application/Pacientes/Interfaces/Repositorios/IRepositorioPacientes.cs), [implementación](https://github.com/LuciAguilar/Therapease/blob/6526607fbd59ffda966093d65f04467da1a959ad/src/Infrastructure/Pacientes/Repositorios/RepositorioPacientes.cs) y [permisos](https://github.com/LuciAguilar/Therapease/blob/6526607fbd59ffda966093d65f04467da1a959ad/src/Domain/Identidad/Reglas/MatrizDePermisos.cs). Existencia comprobada; los resultados previos de B00/B01 no acreditan estas funciones nuevas.

## 5. Qué debe funcionar y qué pasa si algo falla

Estas condiciones se concretan con las diez decisiones aprobadas de AGENTS sección 3 y la propuesta ajustada aprobada. Los resultados de pruebas siguen pendientes.

| Estado | Situación | Resultado esperado | Referencia existente |
| --- | --- | --- | --- |
| ⏳ | Alta válida | Una ficha con identificador, consultable tras guardar, y su evento sin valores privados. | M-06, M-12, M-24; L-CU03-01 |
| ⏳ | Coincidencias o mismo nombre | Elegir una ficha existente o registrar otra persona; sin impedir homónimos legítimos. | M-07; L-CU03-02 |
| ⏳ | Búsqueda ordinaria | Mostrar solo fichas vigentes y datos permitidos. La búsqueda no se convierte en directorio completo. | L-CU03-03; Q20 |
| ⏳ | Apertura directa por identificador de una ficha de baja | Después de comprobar sesión/permiso, una sola consulta devuelve contenido solo si está vigente; para una baja muestra únicamente «Está dada de baja». No se entrega su contenido ni se habilita consulta de bajas B06. | CU03 A3; Q01, Q20 |
| ⏳ | Texto de búsqueda con comillas o comodines | Consulta parametrizada según AGENTS; Miguel explicita el tratamiento de comodines y su escape al buscar texto literal, sin ampliar coincidencias accidentalmente. No guardar el texto buscado en registros técnicos ni eventos. | AGENTS §2.1/§2.3; Q04 |
| ⏳ | Campo obligatorio ausente, tutor exigido ausente o formato inválido | Informar la causa sin repetir datos privados y no guardar parcialmente. Aplicar las reglas de la propuesta ajustada aprobada. | CU03 A2; M-06; D-01 |
| ⏳ | Falta sesión o permiso, o hay temporal pendiente | Denegar cada operación en el servidor, incluso con identificador conocido o envío directo; no revelar la ficha ni guardar cambios. Comprobar también sesión revocada y permiso retirado. | CU03 A4; Q01; AGENTS §2 |
| ⏳ | Dos ediciones parten de la misma versión | Solo una se confirma; la otra recibe conflicto y no sobrescribe lo ya guardado. En la página, avisar y conservar lo capturado para poder recargar antes de decidir. | M-08; L-CU04-01; D-02; Q07 |
| ⏳ | Ficha de baja al editar | Rechazar el cambio ordinario e informar la condición sin recuperar la ficha. | M-09; CU04 A4 |
| ⏳ | Repetición de la misma solicitud, también simultánea | Una sola alta y su evento; respuesta y consulta del resultado conforme al mecanismo aprobado, sin revelar resultados fuera del permiso vigente. No confundir un reenvío con otra persona que tenga el mismo nombre. | L-CU03-04; CU03 A1/A5 |
| ⏳ | Cambio confirmado | Evento con actor, momento, registro, acción y nombres de campos según Q03; nunca valores de contacto o clínicos. Sin cambios y con versión vigente no se escribe ni se genera evento. | M-12, M-24; Q03, Q04 |
| ⏳ | Falla la base o la auditoría | No anunciar éxito; cambio y evento se confirman juntos o se revierten. | M-22, M-24; Q03, Q08 |
| ⏳ | Se pierde la respuesta después de guardar | No repetir a ciegas; consultar el resultado antes de intentar otra vez. | L-CU04-02; D-03; Q08 |

## 6. Orden de trabajo y dependencias

| Estado | Momento | Qué puede avanzar | Condición |
| --- | --- | --- | --- |
| ✅ | Base disponible | Main `a7c0c52`, con #9 y #10 fusionados; apertura B02 y A-06 publicados. | No cierra B01/B00/LCA/R04. |
| ✅ | Revisión e integración del QA B01 | Miguel aprobó `3a44332`; Lucía fusionó #10 con Create a merge commit en `a7c0c52`. CI 40393607611: cuatro trabajos correctos. | B01 sigue abierto por páginas y navegador; no equivale a cerrar todo B01. |
| ✅ | Preparación documental | Propuesta ajustada, ADR-04, plan y decisiones aprobados; Claude APROBADO v1. | Miguel implementa y avisa si encuentra un problema técnico. QA funcional todavía no ejecutado; #10 ya está fusionado. |
| ⏳ | Implementación de Miguel | Implementa en su rama desde main con el detalle aprobado. | #9 ya está fusionado. No reutilizar su rama del #9 ni trabajar B02 en la rama global de Lucía. |
| ⏳ | Servidor independiente | Funciones, permisos, unitarias y CI de Miguel; después pruebas de servicios de Lucía. | No espera las páginas; sí el código necesario. |
| ⏳ | Publicación del QA de B02 | Agrupar y revisar las pruebas y documentos afectados. | Condición de #10 cumplida; cualquier commit/publicación de QA conserva aprobación explícita de Lucía y revisión humana. |
| ⏳ | Integración web | PageModel, formularios y navegación de Dulce; pruebas directas a esos formularios y pruebas de navegador de Lucía. | Espera páginas funcionales y servicios listos. |

Antes de cada entrega se conserva la rama global y se ejecutan `git fetch` y `git merge origin/main`, sin reset ni pérdida de cambios locales. El 26-oct se incorporó main `a7c0c52` a `revision/etapa09` mediante avance directo, sin crear otro commit; modelo/PDF conservados. Si aparecen diferencias en README, plan B01 o plan de pruebas, se propone a Lucía cómo combinarlas antes de resolverlas. La preparación documental está aprobada y Miguel implementa; el QA funcional espera los servicios. No requiere otra auditoría de la propuesta.

## 7. Qué pruebas y evidencia se necesitan

| Estado | Pruebas | Autor / revisión | Dependencia |
| --- | --- | --- | --- |
| ⏳ | Validaciones, búsqueda, permisos, conflicto, guardado y errores de los servicios. | Miguel; otra persona revisa. M-06–M-09, M-12, M-22 y M-24 aplicables. | Propuesta aprobada; no incluir M-10/M-11 como desarrollo nuevo de B02. |
| ⏳ | Alta/consulta, homónimos, bajas excluidas, reenvío, edición simultánea, fallo de auditoría y respuesta perdida con PostgreSQL desechable mediante Testcontainers. Incluir cada operación y rol aprobado. | Lucía; revisa Miguel o Dulce. L-CU03-01–04 y L-CU04-01–02; Q01/Q03/Q04/Q07/Q08/Q20 aplicables. | Servicios implementados; la condición de fusionar #10 ya está cumplida. Repetir los casos simultáneos y comprobar estado/eventos después del resultado; leer reglas existentes no acredita el caso. |
| ⏳ | Campos permitidos y extra, permisos, validaciones y protección del formulario contra envíos falsificados. | Dulce escribe unitarias; Lucía prueba el conjunto. D-01–D-05, D-13 y seguridad aplicable. | PageModel/formularios; los servicios solos no prueban antifalsificación, campos extra del formulario ni mensajes visibles. |
| ⏳ | Registrar → buscar → abrir → editar, estado vacío, selección de homónimos, errores y navegación. | Lucía; revisión humana de sus pruebas. D-09 y D-12 aplicables. | Páginas conectadas. El doble clic real espera el formulario; el reenvío al servicio se prueba antes. Q16 completo sigue desde B04. |
| ⏳ | Pacientes, contactos y tutores ficticios, identificados como demostración y sin datos de personas reales. | Lucía prepara el conjunto; Q05. | Adaptar los valores a las reglas que se aprueben antes de usarlos en pruebas; prepararlos no acredita ejecución. |
| ⏳ | Persona ajena al desarrollo registra una ficha tras una explicación breve y sin ayuda durante la tarea. | Lucía prepara y observa el aporte de B02 a Q13, registrando dificultades. | Espera páginas y participante. Q13 completo también incluye agendar y marcar pagado; esa parte no se acredita con B02 solo. |
| ⏳ | Compilar, pruebas aplicables y controles de seguridad/integridad de cada cambio. | Cada autor mantiene CI; Lucía comprueba y registra minutos. | Evidencia de la nueva versión, sin copiar resultados históricos como nuevos. |

Cada evidencia identifica fecha, SHA, comando, entorno, casos correctos/fallidos/omitidos y quién revisó. Sin nombres reales, contactos, contraseñas ni rutas personales en informes públicos. Los resultados locales y la nota de aprendizaje de Lucía se conservan aparte.

**Estado de esta entrega (27-oct-2026):** propuesta ajustada y nota ADR-04 aprobadas para implementar, con veredicto de Claude APROBADO v1 y ajustes de texto que no requieren otra auditoría. Miguel implementó el servidor en la rama `b02/fichas-de-pacientes`, con unitarias propias y verificaciones con PostgreSQL desechable; en el [PR #11](https://github.com/LuciAguilar/Therapease/pull/11), con la [CI de `1eab29c`](https://github.com/LuciAguilar/Therapease/actions/runs/40639773900) correcta; las tres correcciones de Lucía del 28-oct están en `a78016a`, con su [CI](https://github.com/LuciAguilar/Therapease/actions/runs/40827839680) correcta. Los resultados de esas ejecuciones están en la [evidencia de servicios](../testing/TherapEase-09-B02-evidencia-servicios.md) y no sustituyen la revisión humana distinta, la CI, el QA de Lucía ni las páginas. El diseño privado de QA conserva su revisión.

## 8. Reglas para integrar y entregar

| Estado | Regla | Qué debe acompañar la entrega |
| --- | --- | --- |
| ✅ | Páginas mediante Application | Los PageModel no llegan directo a la base ni usan entidades/reglas de Domain; siguen asignados a Dulce. Web conserva únicamente la excepción de enumeraciones/constantes y la composición autorizada en AGENTS. |
| ✅ | Permisos en servidor | Tabla explícita por operación y rol, incluido Superusuario; pruebas de concesiones y rechazos. |
| ✅ | Datos ficticios | Sin nube, servicios externos nuevos ni gastos por esta apertura. |
| ✅ | Contrato para las páginas | Contrato aprobado en la propuesta ajustada; Miguel lo implementó y lo documenta para las páginas en el [contrato B02](TherapEase-etapa09-B02-contrato-para-paginas.md); Dulce lo conecta y prueba. |
| ⏳ | Cambio de datos aprobado | Si requiere migración, explicar qué cambia, cómo probarla y cómo revertirla. No crear tablas/campos por suposición. |
| ⏳ | Documentos actuales | Actualizar los afectados tras auditoría y aprobación. Documentación aprobada se publica directamente en main, con autorización explícita de Lucía; código/pruebas conservan PR y revisión por otra persona. El QA conserva su PR y la revisión humana correspondiente; cada publicación requiere el OK explícito de Lucía. |

## 9. Cuándo se cierra B02 y qué queda fuera

| Estado | Condición de cierre | Quién confirma |
| --- | --- | --- |
| ⏳ | Propuesta completa aprobada y funciones de servidor comprobadas. | Miguel entrega; revisa otra persona; Lucía aprueba. |
| ⏳ | Pruebas de Lucía auditadas, correctas y revisadas por Miguel o Dulce; CI correcta. | Lucía conserva evidencia y aprobación final. |
| ⏳ | Páginas, formularios y navegador integrados y comprobados. | Dulce y Lucía. |
| ⏳ | Documentación/evidencia actuales y pendientes bloqueantes resueltos. | Lucía decide el cierre. |

Quedan fuera: directorio completo B07, bajas y recuperaciones nuevas B06, montos, «solo particular» y cancelación automática. No se añaden roles futuros ni funciones clínicas. Terminar el servidor permite seguir el trabajo independiente autorizado, pero no acredita páginas ni cierra B02 completo. B01/B00/LCA/R04 siguen abiertos.
