# TherapEase — 09/B02 · Registrar, buscar, consultar y editar pacientes

## Estado vigente al 31-oct-2026

> **Servidor y QA de Lucía fusionados.** Lucía aprobó y fusionó [#11](https://github.com/LuciAguilar/Therapease/pull/11) en `c90b3c0`. Nuestro [#12](https://github.com/LuciAguilar/Therapease/pull/12), `5e46883`, tiene CI correcta y aprobación de Miguel (`GalloNav`). Lucía fusionó #12 el 31-oct con Create a merge commit en `b53c5b7`.

| Entrega | Estado actual |
| --- | --- |
| Miguel | Registrar, buscar, consultar y editar, permisos, auditoría, conflictos y reenvío entregados. Sin correcciones obligatorias conocidas pendientes. |
| QA de Lucía | Claude APROBADO v1, aceptado por Lucía; cinco archivos y dos documentos publicados en #12. 89 propios incluidos en 465 integraciones y 646 unitarias correctas en la rama global. No sumar grupos. |
| CI | [41134402751](https://github.com/LuciAguilar/Therapease/actions/runs/41134402751): cuatro trabajos correctos sobre `5e46883`. |
| Próximo paso | #12 ya está fusionado. Faltan confirmaciones de producto, páginas, navegador y uso; después Lucía decide el cierre de B02. |
| Páginas y producto | Dulce confirma detalles, valida 08C sección 3 y entrega páginas/PageModel con sus pruebas; Lucía comprueba formularios, navegador y uso cuando estén disponibles. |
| Listado paginado (cambio posterior, 13-nov) | La pantalla Pacientes muestra todos los pacientes vigentes al abrir, de seis en seis, con «Página X de Y» y el total de pacientes, sin cargarlos todos de una vez. Implementado en la rama `frontend/B02` |

B02 sigue abierto. [Avance y preparación de B03](TherapEase-09-avance-y-pendientes.md). [B03](TherapEase-etapa09-B03.md) está abierto solo para preparación. Este seguimiento no acredita otra ejecución ni publica documentos nuevos.

---


**Decisiones de base:** plan y diez decisiones aprobados por Lucía el 26-oct; propuesta ajustada auditada y aprobada. Los criterios provisionales siguen limitados a datos ficticios. El estado de ejecución y revisión está en las tablas actuales.

**Base integrada:** #9/#10 ya fusionados; su condición previa quedó cumplida. El servidor de #11 está revisado y fusionado; el QA de #12 ya fue aprobado por Miguel y fusionado por Lucía en `b53c5b7`. B00/B01/B02/LCA/R04 siguen abiertos.

B02 permite guardar una ficha ficticia, encontrarla, consultarla y corregirla. Si dos personas intentan cambiarla, se avisa para evitar que una borre sin saberlo los cambios de la otra. Cada alta o edición se guarda junto con su registro de auditoría.

Miguel prepara el servidor y sus unitarias. Dulce hace las páginas, formularios y PageModel. Lucía prepara y ejecuta QA; Miguel o Dulce revisa sus pruebas y Lucía da la aprobación final.

✅ = decisión aprobada o comprobación indicada en la fila. ⏳ = trabajo, decisión o prueba pendiente. Leer el código no equivale a ejecutar sus pruebas.

## Qué cubren las pruebas de B02 · Las fichas de pacientes

| Parte | Qué comprobamos |
| --- | --- |
| Funcionamiento normal | Registrar, buscar, consultar y editar los datos de un paciente ficticio. |
| Validaciones | Datos incorrectos, nacimiento y edad calculada, menores con tutor y al menos un tipo de atención: Escolar o Independiente. |
| Seguridad | Permisos y sesiones; bajas fuera de la búsqueda y, al abrirlas por identificador, solo «Está dada de baja». Sin datos privados en registros ni texto buscado guardado. |
| Guardado y fallos | Cambio y auditoría juntos; sin cambios no se guarda ni registra nada. Qué pasa si falla el guardado o se pierde la respuesta. |
| Cambios simultáneos | Dos ediciones no se pisan; reenviar un alta con el mismo identificador no duplica al paciente ni su evento. Una edición con versión anterior sigue dando conflicto. |
| Páginas y uso | Formularios, homónimos, estado vacío, mensajes, navegador y aporte de B02 a la prueba con una persona ajena al desarrollo. Espera páginas conectadas; no acredita el recorrido completo posterior. |

> **Son casos normales, validaciones, seguridad y fallos; no solo caminos alternos.** La cobertura de servidor está ejecutada y su QA publicado en #12, con auditoría y revisión humana aprobadas; el servidor de #11 ya está fusionado. La parte de páginas y navegador sigue prevista, sin ejecución acreditada. Las secciones 5 y 7 detallan casos, responsables y evidencia. No incorpora bajas/recuperaciones nuevas B06 ni el directorio B07.

Las pruebas de funciones por separado y las de servidor/base de datos no sustituyen las de páginas y navegador. Cada resultado debe indicar qué se probó.

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
| ✅ | Miguel | Registrar, buscar, consultar y editar desde Application, con permisos, datos, auditoría, conflicto, reenvío y pruebas. | Revisado por Lucía y fusionado en #11, `c90b3c0`. No sustituye el QA de Lucía ni las páginas. |
| ✅ | Miguel | Contrato para las páginas y migración entregados. Apoyar a Dulce si aparece una duda o fallo al conectar. | Contrato y evidencia en #11; ajuste de texto incluido en #12. PageModel siguen con Dulce. |
| ⏳ | Dulce | Obtener con la usuaria los detalles de producto que faltan: obligatoriedad de campos, datos del tutor y finalidad de escuela/grupo, además de presentación y selección. Dar su visto bueno a 08C §3 antes de las páginas afectadas; preparar páginas, formularios, PageModel y sus unitarias. | Usar las respuestas ya recibidas y las opciones/contrato de Miguel; Lucía revisa privacidad y decide. Conectar las páginas con los servicios aprobados. |
| ⏳ | Lucía | QA de servidor escrito, auditado y publicado en #12. Preparar los recorridos de páginas y navegador. | 89 propios correctos, incluidos en 465 integraciones; revisión de Miguel aprobada y fusión realizada en `b53c5b7`. Las páginas esperan a Dulce. |
| ⏳ | Otra persona | Revisar código y pruebas del autor correspondiente. Miguel o Dulce revisa las de Lucía. | Entrega lista y CI correcta; aprobación final de Lucía. |

Miguel y Dulce eligen sus herramientas. Cada autor escribe sus unitarias. La auditoría de IA no sustituye la revisión humana.

## 3. Decisiones aprobadas para implementar

La [propuesta ajustada](TherapEase-etapa09-B02-propuesta.md) concreta las diez decisiones de AGENTS sección 3 y el tratamiento de registros anteriores. Claude emitió APROBADO v1 con ajustes de texto, sin otra auditoría. Las marcas de esta sección acreditan decisiones. Las secciones 5 y 7 distinguen la ejecución de servidor, las páginas y la revisión humana del QA.

| Estado | Tema | Regla aprobada |
| --- | --- | --- |
| ✅ | Alcance | HU04–HU06 y CU03–CU04 con registros ficticios; B06/B07, montos, «solo particular» y cancelación automática fuera. |
| ✅ | Campos y edad | Valores de la propuesta provisionales solo para pruebas; edad calculada, no almacenada. Dulce confirma producto y valida 08C sección 3 antes de las páginas. |
| ✅ | Tutor y registros anteriores | Un tutor, obligatorio para menores de 18. Consulta de legado con edad desconocida; una edición real exige nacimiento y tutor si resulta menor. Con versión vigente y sin cambios, no inventar datos ni escribir. Editar nacimiento/tutor se valida y guarda junto; no borrar tutor automáticamente al pasar a adulto. |
| ✅ | Contacto | Opción A: sustituir por teléfono/correo. Pérdida no convertible aceptada por Lucía solo para datos de prueba. Correspondencia, reversión y SQL comprobados en #11, ya fusionado. El QA propio de #12 ya fue aprobado por Miguel y fusionado por Lucía. |
| ✅ | Permisos | Registrar, consultar y editar para Usuaria/Superusuario; los de Superusuario solo con datos de prueba. Consultar obligatorio también para la lista de parecidos. Sesión y autorización en servidor antes de leer/escribir. |
| ✅ | Búsqueda y homónimos | Sin distinguir acentos ni extensiones nuevas; nacimiento para homónimos, no por defecto en toda fila. Vigentes, límites y orden estable según propuesta. No unicidad por nombre/nacimiento ni directorio B07. |
| ✅ | Baja por identificador | Una consulta, contenido solo si está vigente; de una baja mostrar solo «Está dada de baja», después de autorizar. No consulta de contenido B06. |
| ✅ | Reenvío y conflicto | Alta con identificador fijo y nota ADR-04, sin duplicar ficha/evento. Edición con versión antigua da conflicto incluso con datos iguales. La existencia de la ficha no demuestra una edición cuya respuesta se perdió. |
| ✅ | Ámbitos y cambios | Al menos Escolar o Independiente; se editan sin modificar citas. Con versión vigente y sin cambios, sin escritura, evento ni incremento de versión. |
| ✅ | Auditoría y contrato | Alta/edición y evento atómicos, sin valores privados. Sin auditoría de lecturas por ahora. Contrato distingue parecidos y guardado; conflicto conserva captura. |
| ✅ | Reutilización | Validaciones comunes con límites propios: nombres 150 en cuentas y 200 en pacientes. Conservar comportamiento y pruebas de cuentas. |

Miguel entregó el servidor y avisa a Lucía si aparece un problema técnico. Si surge un cambio de mecanismo o arquitectura, se aplica la regla de ADR de AGENTS; no modificar reglas silenciosamente.

## 4. Qué existe y qué falta completar

Comprobación por lectura de main `6526607fbd59ffda966093d65f04467da1a959ad` y del clon activo. Los archivos de pacientes y permisos citados coinciden con esa base. No se ejecutaron pruebas de software en esta preparación.

| Estado | Base encontrada | Cómo se aprovecha / qué falta |
| --- | --- | --- |
| ✅ | `Paciente` y `PacienteAmbito` | Hay nombre, contacto, condición, versión y ámbitos. Los campos adicionales de entrevista y tutor no están desarrollados en esta ficha. |
| ✅ | `ReglasDePaciente` | Ya valida nombre y ámbitos y controla versión y condición. La edición actual modifica nombre/contacto; adaptar solo lo que se apruebe. |
| ✅ | `IRepositorioPacientes` y su implementación | Permiten agregar y obtener por identificador con ámbitos. Esta base se amplió para la búsqueda y los servicios completos, ya entregados en #11. |
| ✅ | Identidad y matriz de permisos | Sesión y controles de B01 reutilizados; los tres permisos de pacientes ya están implementados en #11, para datos ficticios y los dos roles aprobados. |
| ✅ | Base de baja y sus pruebas | Se conserva. B02 no añade bajas, recuperaciones ni cancelación automática. Comprobar que lo existente no se rompa al cambiar la ficha. |
| ✅ | Servicios completos de pacientes | Implementados, revisados por Lucía y fusionados en #11. QA propio de #12 aprobado por Miguel y fusionado por Lucía. |
| ✅ datos / ⏳ páginas | Campos nuevos y páginas | Datos y migración entregados en #11; Dulce confirma producto y conecta sus páginas. |

Fuentes de código en esa base: [ficha](https://github.com/LuciAguilar/Therapease/blob/6526607fbd59ffda966093d65f04467da1a959ad/src/Domain/Pacientes/Entidades/Paciente.cs), [reglas](https://github.com/LuciAguilar/Therapease/blob/6526607fbd59ffda966093d65f04467da1a959ad/src/Domain/Pacientes/Reglas/ReglasDePaciente.cs), [acceso a datos](https://github.com/LuciAguilar/Therapease/blob/6526607fbd59ffda966093d65f04467da1a959ad/src/Application/Pacientes/Interfaces/Repositorios/IRepositorioPacientes.cs), [implementación](https://github.com/LuciAguilar/Therapease/blob/6526607fbd59ffda966093d65f04467da1a959ad/src/Infrastructure/Pacientes/Repositorios/RepositorioPacientes.cs) y [permisos](https://github.com/LuciAguilar/Therapease/blob/6526607fbd59ffda966093d65f04467da1a959ad/src/Domain/Identidad/Reglas/MatrizDePermisos.cs). Existencia comprobada; los resultados previos de B00/B01 no acreditan estas funciones nuevas.

## 5. Qué debe funcionar y qué pasa si algo falla

Estas condiciones se concretan con las diez decisiones aprobadas de AGENTS sección 3 y la propuesta ajustada aprobada. El servidor está comprobado. La revisión humana del QA propio ya está aprobada. Su fusión ya se completó. Quedan las páginas; no se presentan como ejecutadas.

| Estado | Situación | Resultado esperado | Referencia existente |
| --- | --- | --- | --- |
| ✅ servidor / ⏳ página | Alta válida | Una ficha con identificador, consultable tras guardar, y su evento sin valores privados. | M-06, M-12, M-24; L-CU03-01 |
| ✅ servidor / ⏳ selección visual | Coincidencias o mismo nombre | Elegir una ficha existente o registrar otra persona; sin impedir homónimos legítimos. | M-07; L-CU03-02 |
| ✅ servidor / ⏳ página | Búsqueda ordinaria | Mostrar solo fichas vigentes y datos permitidos. La búsqueda no se convierte en directorio completo. | L-CU03-03; Q20 |
| ✅ servidor / ⏳ página | Apertura directa por identificador de una ficha de baja | Después de comprobar sesión/permiso, una sola consulta devuelve contenido solo si está vigente; para una baja muestra únicamente «Está dada de baja». No se entrega su contenido ni se habilita consulta de bajas B06. | CU03 A3; Q01, Q20 |
| ✅ servidor / ⏳ envío web | Texto de búsqueda con comillas o comodines | Consulta parametrizada según AGENTS; Miguel explicita el tratamiento de comodines y su escape al buscar texto literal, sin ampliar coincidencias accidentalmente. No guardar el texto buscado en registros técnicos ni eventos. | AGENTS §2.1/§2.3; Q04 |
| ✅ servidor / ⏳ formulario | Campo obligatorio ausente, tutor exigido ausente o formato inválido | Informar la causa sin repetir datos privados y no guardar parcialmente. Aplicar las reglas de la propuesta ajustada aprobada. | CU03 A2; M-06; D-01 |
| ✅ servidor / ⏳ rutas web | Falta sesión o permiso, o hay temporal pendiente | Denegar cada operación en el servidor, incluso con identificador conocido o envío directo; no revelar la ficha ni guardar cambios. Comprobar también sesión revocada y permiso retirado. | CU03 A4; Q01; AGENTS §2 |
| ✅ servidor / ⏳ aviso visual | Dos ediciones parten de la misma versión | Solo una se confirma; la otra recibe conflicto y no sobrescribe lo ya guardado. En la página, avisar y conservar lo capturado para poder recargar antes de decidir. | M-08; L-CU04-01; D-02; Q07 |
| ✅ servidor / ⏳ página | Ficha de baja al editar | Rechazar el cambio ordinario e informar la condición sin recuperar la ficha. | M-09; CU04 A4 |
| ✅ servidor / ⏳ doble clic real | Repetición de la misma solicitud, también simultánea | Una sola alta y su evento; respuesta y consulta del resultado conforme al mecanismo aprobado, sin revelar resultados fuera del permiso vigente. No confundir un reenvío con otra persona que tenga el mismo nombre. | L-CU03-04; CU03 A1/A5 |
| ✅ servidor / ⏳ página | Cambio confirmado | Evento con actor, momento, registro, acción y nombres de campos según Q03; nunca valores de contacto o clínicos. Sin cambios y con versión vigente no se escribe ni se genera evento. | M-12, M-24; Q03, Q04 |
| ✅ guardado / ⏳ recorrido HTTP | Falla la base o la auditoría | No anunciar éxito; cambio y evento se confirman juntos o se revierten. | M-22, M-24; Q03, Q08 |
| ✅ guardado / ⏳ respuesta HTTP | Se pierde la respuesta después de guardar | No repetir a ciegas; consultar el resultado antes de intentar otra vez. | L-CU04-02; D-03; Q08 |

## 6. Orden de trabajo y dependencias

| Estado | Momento | Qué puede avanzar | Condición |
| --- | --- | --- | --- |
| ✅ | Base disponible | Main `a7c0c52`, con #9 y #10 fusionados; apertura B02 y A-06 publicados. | No cierra B01/B00/LCA/R04. |
| ✅ | Revisión e integración del QA B01 | Miguel aprobó `3a44332`; Lucía fusionó #10 con Create a merge commit en `a7c0c52`. CI 40393607611: cuatro trabajos correctos. | B01 sigue abierto por páginas y navegador; no equivale a cerrar todo B01. |
| ✅ | Preparación documental | Propuesta ajustada, ADR-04, plan y decisiones aprobados; Claude APROBADO v1. | Miguel entregó el servidor y comunica cualquier problema técnico. QA de servidor ejecutado y publicado en #12; revisión humana aprobada por Miguel. #10 ya está fusionado. |
| ✅ | Implementación de Miguel | Servidor B02 fusionado en #11; conserva sus pruebas originales y regresiones. | Revisión de Lucía y CI correctas; no cierra las páginas. |
| ✅ servidor y revisión humana | Servidor independiente | Servicios de Miguel y QA de Lucía disponibles, sin esperar páginas. | #11 fusionado; #12 con CI correcta y aprobación de Miguel y fusión en `b53c5b7`. |
| ✅ revisión y fusión | Publicación del QA de B02 | Publicado en una sola entrega, #12, con cinco archivos y dos documentos. | Miguel aprobó el 31-oct; Lucía fusionó ese día con Create a merge commit en `b53c5b7`. |
| ⏳ | Integración web | PageModel, formularios y navegación de Dulce; pruebas directas a esos formularios y pruebas de navegador de Lucía. | Espera páginas funcionales y servicios listos. |

Antes de cada entrega se conserva la rama global y se ejecutan `git fetch` y `git merge origin/main`, sin reset ni pérdida de cambios locales. El 26-oct se incorporó main `a7c0c52` a `revision/etapa09` mediante avance directo, sin crear otro commit; modelo/PDF conservados. Si aparecen diferencias en README, plan B01 o plan de pruebas, se propone a Lucía cómo combinarlas antes de resolverlas. El servidor y contrato están entregados; el QA funcional de servidor ya está publicado, aprobado por Miguel y fusionado por Lucía en `b53c5b7`. No requiere otra auditoría de la propuesta.

## 7. Qué pruebas y evidencia se necesitan

| Estado | Pruebas o evidencia | Autor / revisión y pendiente real |
| --- | --- | --- |
| ✅ | Validaciones, búsqueda, permisos, conflicto, guardado y errores del servidor | Miguel las entregó; Lucía revisó y fusionó #11. Se conservan las pruebas originales y sus regresiones. |
| ✅ ejecución, auditoría y revisión humana | Alta/consulta, homónimos, bajas ocultas, permisos, reenvío, edición simultánea, auditoría y respuesta perdida en el guardado | Lucía: 89 casos correctos en #12, incluidos en 465 integraciones. Claude APROBADO v1 aceptado por Lucía; Miguel (`GalloNav`) aprobó su QA el 31-oct. |
| ⏳ | Campos permitidos/extra, validaciones y protección de formularios | Dulce entrega PageModel y unitarias; Lucía prueba el conjunto. El servicio no prueba el formulario. |
| ⏳ | Registrar → buscar → abrir → editar; estados vacíos, mensajes, selección y navegador | Lucía, después de recibir páginas. Doble clic y pérdida de respuesta HTTP reales pendientes. Q16 completo sigue desde B04. |
| ✅ datos de servidor / ⏳ demostración desde páginas | Pacientes, contactos y tutores ficticios de Q05 | Usados en el QA; no hay datos reales ni mensajes enviados. Preparar la demostración al recibir páginas. |
| ⏳ | Uso con una persona ajena al desarrollo | Lucía prepara y observa; necesita páginas y participante. Solo el aporte de B02 a Q13, no el recorrido completo de cita/pago. |
| ✅ versión publicada / ⏳ cambios futuros | Compilación, pruebas y controles automatizados | #12, `5e46883`, CI 41134402751: cuatro trabajos correctos. Mantener controles por cambio; no acreditar ejecuciones futuras. |

Los 89 propios están incluidos en los 465; no sumar grupos ni repeticiones. Los resultados completos y las notas personales se conservan locales. El PR contiene las pruebas y los dos ajustes documentales auditados.

**Estado actual:** decisiones, implementación de Miguel, ejecución del QA de servidor, auditoría y publicación realizadas. Miguel aprobó #12 y Lucía lo fusionó. Quedan páginas, navegador, uso y cierre del bloque. No se anuncian pruebas nuevas por actualizar este plan.


## 8. Reglas para integrar y entregar

| Estado | Regla | Qué debe acompañar la entrega |
| --- | --- | --- |
| ✅ | Páginas mediante Application | Los PageModel no llegan directo a la base ni usan entidades/reglas de Domain; siguen asignados a Dulce. Web conserva únicamente la excepción de enumeraciones/constantes y la composición autorizada en AGENTS. |
| ✅ | Permisos en servidor | Tabla explícita por operación y rol, incluido Superusuario; pruebas de concesiones y rechazos. |
| ✅ | Datos ficticios | Sin nube, servicios externos nuevos ni gastos por esta apertura. |
| ✅ | Contrato para las páginas | Contrato aprobado en la propuesta ajustada; Miguel lo implementó y lo documenta para las páginas en el [contrato B02](TherapEase-etapa09-B02-contrato-para-paginas.md); Dulce lo conecta y prueba. |
| ✅ | Cambio de datos aprobado | Migración de #11 explicada, probada al subir/bajar/volver a subir y revisada por Lucía. QA adicional de #12 aprobado por Miguel y fusionado por Lucía. |
| ✅ actualización local / ⏳ publicación | Documentos actuales | Actualizar los afectados tras auditoría y aprobación. Documentación aprobada se publica directamente en main, con autorización explícita de Lucía; código/pruebas conservan PR y revisión por otra persona. El QA conserva su PR y la revisión humana correspondiente; cada publicación requiere el OK explícito de Lucía. |

## 9. Cuándo se cierra B02 y qué queda fuera

| Estado | Condición de cierre | Quién confirma |
| --- | --- | --- |
| ⏳ | Propuesta completa aprobada y funciones de servidor comprobadas. | Miguel entrega; revisa otra persona; Lucía aprueba. |
| ⏳ | Pruebas de Lucía auditadas, correctas y revisadas por Miguel o Dulce; CI correcta. | Lucía conserva evidencia y aprobación final. |
| ⏳ | Páginas, formularios y navegador integrados y comprobados. | Dulce y Lucía. |
| ⏳ | Documentación/evidencia actuales y pendientes bloqueantes resueltos. | Lucía decide el cierre. |

Quedan fuera: directorio completo B07, bajas y recuperaciones nuevas B06, montos, «solo particular» y cancelación automática. No se añaden roles futuros ni funciones clínicas. Terminar el servidor permite seguir el trabajo independiente autorizado, pero no acredita páginas ni cierra B02 completo. B01/B00/LCA/R04 siguen abiertos.
