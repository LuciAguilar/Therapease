# TherapEase — B03 · Evidencia del servidor de agenda y citas (verificación de Miguel)

**Preparada el 07-nov-2026 · rama `b03/agenda-y-citas`, desde main `6dd13d9`**

## 1. Qué se implementó y con qué se comprueba

| Estado | Parte | Pruebas | Casos |
| --- | --- | --- | --- |
| ✅ | Dos permisos nuevos (`ConsultarAgenda`, `AgendarCitas`) para Usuaria y Superusuario, con sus políticas web | `MatrizDePermisosTests` (incluye su prueba de completitud) | 34 |
| ✅ | Regla de horario en Domain: las 752 combinaciones válidas contra una lista escrita aparte, los 1 440 × 1 440 pares de minutos, ejemplos del plan, segundos y fracciones, fin del día siguiente, siete días, fecha pasada y fechas límite | `ReglasDeHorarioDeCitaTests` | 41 |
| ✅ | Conversión: `HoraHermosillo.EsRepresentable` y leyenda «Registrada después de su horario» (`FechaAlta` mayor, igual y menor que `Inicio`) | `HoraHermosilloTests` (13, seis nuevos) y `ReglasDeCitaTests` (21, cuatro nuevos) | 34 |
| ✅ | Servicio de citas: permisos antes de leer, entradas, horario, paciente y ámbito, resultados, los estados del reenvío con sus tres salidas, interbloqueo, fallos de auditoría o de la base, agenda (periodos, vistas, 500 filas, bajas sin nombre, conversión de horas) y detalle | `ServicioCitasTests` | 96 |
| ✅ | Contrato de Pacientes para Citas: permiso, condición y ámbitos, baja sin ámbitos, nombres solo de vigentes, una sola consulta | `ConsultaDePacientesParaCitasTests` | 12 |
| ✅ | Guardado y evento en una transacción, `PK_Cita`, reenvío con la gemela confirmada antes del guardado, entre la primera lectura y el guardado, y antes del rechazo de ámbito (sin restaurarlo), reservas simultáneas (dos y diez), fallo de auditoría y alcance posterior, una sola consulta por lectura, agenda con PostgreSQL real y detalle | `CitasServidorTests` (PostgreSQL desechable) | 27 |

`CitasServidorTests` es verificación de Miguel de lo que pide la propuesta (secciones 3.11 y 3.12, D9: traducción de `PK_Cita`, atomicidad y fallo de auditoría, reenvío con la gemela, rechazo de ámbito y cruce con dos reservas de apoyo). Según AGENTS §2.6 el QA de integración con Testcontainers es de Lucía; estas pruebas no lo sustituyen. **Quedan para el QA dirigido de Lucía** y no se incluyen aquí: agendar frente a baja en sus tres órdenes (plan B03 §4.1), el ámbito retirado sin cita confirmada con sus pausas forzadas, las zonas horarias, las páginas y el navegador.

## 2. Resultados

| Estado | Comprobación | Resultado |
| --- | --- | --- |
| ✅ | Compilación Release de la solución, advertencias como errores | 0 advertencias y 0 errores. |
| ✅ | Unitarias | 810 correctas, 0 fallidas, 0 omitidas (antes de B03: 646). |
| ✅ | Integración completa con dos hilos | 492 correctas, 0 fallidas, 0 omitidas, 3 min 44 s (antes de B03: 465). |
| ✅ | `CitasServidorTests` repetida cinco veces seguidas | 27 de 27 en cada una, con las dos rondas de diez reservas simultáneas incluidas. |
| ✅ | `dotnet ef migrations has-pending-model-changes` | «No changes have been made to the model since the last migration.» Con la cadena ficticia que usa la CI. |
| ✅ | Comprobaciones estáticas de la CI que se pueden correr en local | `revisar-fijaciones.sh` y `revisar-archivos-sensibles.sh` sin hallazgos. |
| ✅ | Comprobaciones de que las pruebas no son vacías | Con una mutación a la vez, falló lo esperado y se restauró el código (sección 6). |
| ⏳ | CI | Sin PR todavía. |
| ⏳ | QA de Lucía, páginas de Dulce y navegador | Pendientes. |

```powershell
dotnet build TherapEase.sln -c Release --no-restore -warnaserror
dotnet test tests/TherapEase.UnitTests -c Release --no-build
dotnet test tests/TherapEase.IntegrationTests -c Release --no-build -- xUnit.MaxParallelThreads=2
dotnet ef migrations has-pending-model-changes --project src/Infrastructure --startup-project src/Web
```

Docker Desktop disponible antes de las pruebas de integración. `MaxParallelThreads=2` evita tiempos de espera falsos por carga; no cambia ninguna aserción.

## 3. Sin migración

B03 no cambia el esquema: `Cita` ya tiene todo lo necesario y `FechaAlta` e `Inicio` bastan para la leyenda de registro tardío. La restricción `EX_Cita_SinCruceActivo` y los dos disparadores de B00 no se tocan. Los permisos de `therapease_app` sobre `Cita` y `EventoAuditoria` ya cubrían las operaciones.

## 4. Hallazgo técnico: interbloqueo en reservas simultáneas

**La propuesta suponía** (sección 3.6) que, con dos o diez reservas distintas por el mismo horario, «las demás esperan al primero y reciben el conflicto». **No siempre ocurre así.** `EX_Cita_SinCruceActivo` es una restricción de exclusión: cada alta inserta su fila y su entrada de índice y solo después comprueba si choca. Si dos altas conflictivas llegan a la vez, cada una encuentra la fila aún sin confirmar de la otra, espera su transacción, y PostgreSQL aborta una por interbloqueo (`40P01`, «deadlock detected») en lugar de rechazarla con `23P01`.

| Qué se hizo | Resultado |
| --- | --- |
| Reproducción aislada, fuera del repositorio: tabla mínima con la misma restricción y un disparador que espera un segundo para que las dos inserciones lleguen juntas | Una inserción se confirma y la otra falla con `deadlock detected`, con el contexto «while checking exclusion constraint». |
| Primera prueba de integración (dos reservas simultáneas, sin tratamiento) | Falló en la primera ejecución con `40P01`; el servicio habría respondido el error 500. |
| Reintentar la operación completa hasta tres veces (probado y descartado) | Con dos reservas sirve, pero con diez algunas se interbloquean en los tres intentos; 4 de 5 ejecuciones fallaron y cada una tardó 31 s. |
| Un bloqueo de asesoría tomado al iniciar la transacción (probado en una base aparte y descartado) | Determinista (una se guarda y nueve reciben `23P01`), pero mantendría tomado el horario durante las pausas con las que el plan B03 §4.1 fuerza «comprobar» y «guardar»: la gemela no podría confirmar mientras la otra está en pausa. |

**Tratamiento actual, sin migración y sin tocar la restricción ni los disparadores:** la unidad de trabajo traduce un interbloqueo a `InterbloqueoDeBaseException` (en Domain, **no** es `ReglaDeNegocioException`), de modo que el resto de los módulos siguen viéndolo como error general (500), como antes. Solo el alta de una cita lo trata como conflicto de horario, después de la misma relectura por número que usa el cruce, por si la cita confirmada fuera la propia. Con ello, con cualquier cantidad de reservas simultáneas por el mismo horario solo una guarda y las demás reciben «horario ocupado» (dos y diez rondas, cinco ejecuciones seguidas correctas).

**Límite de ese tratamiento:** si la transacción ganadora se revierte (por ejemplo, un fallo de auditoría), una reserva abortada por interbloqueo recibiría «horario ocupado» aunque el horario quede libre; reenviar a mano lo resuelve. No hay reintento automático.

**Alternativa más robusta, que requiere decisión de Lucía** (no se implementó): una migración que tome el bloqueo de asesoría dentro de un disparador de inserción de `Cita`, justo antes de insertar. Sería determinista, compatible con las pausas del QA y protegería también a B05 y B06, pero cambia B00 y exige revisión de SQL, impacto y reversión (AGENTS §2.5). Si se aprueba, el tratamiento actual se retira.

## 5. Pruebas existentes que se tocaron

| Prueba | Cambio | Por qué |
| --- | --- | --- |
| `MatrizDePermisosTests` | Cuatro casos nuevos de la tabla y las listas de permisos de cada rol pasan de seis a ocho y de cuatro a seis | Dos permisos nuevos; su prueba de completitud exige la decisión por rol. |
| `PermisosUsuariosTests` (integración) | Las listas de permisos esperados incluyen `ConsultarAgenda` y `AgendarCitas` | Mismo motivo. |
| `RepositorioCitasFalso` y `RepositorioPacientesFalso` | Implementan los métodos nuevos de los puertos, con contadores y ganchos para forzar momentos | Los puertos ampliaron su contrato. |

Ninguna comprobación se redujo.

## 6. Comprobaciones de que las pruebas no son vacías

Una mutación a la vez sobre el código de producción, con compilación Release, y restauración del archivo original después de cada una:

| Mutación | Pruebas que fallaron |
| --- | --- |
| El alta no trata `InterbloqueoDeBaseException` como conflicto (`catch … when (false)`) | 3 unitarias de `ServicioCitasTests` y 2 de integración (`…AbortaPorInterbloqueoYNadieConfirmoLaCita…` y `…PeroLaGemelaYaConfirmo…`). |
| Sin la primera lectura por número en `AgendarAsync` | 9 de `ServicioCitasTests`: las seis variantes de «mismo número que no es la misma reserva», el reenvío con el paciente de baja, con el ámbito retirado y el reenvío ya confirmado. |
| El cambio de duración a las 14:00 en lugar de las 15:00 | 3 de `ReglasDeHorarioDeCitaTests`, entre ellas la comparación de los 1 440 × 1 440 pares con la lista independiente. |
| La agenda filtra solo por inicio, no por solape | `ConsultarAgenda_ConPostgreSqlReal_IncluyeLasQueSeSolapanConElPeriodoYExcluyeLasContiguas`. |
| El evento siempre dice «Se agendó una cita.» aunque sea registro tardío | `Agendar_ConFechaPasada_SePermiteConLeyendaYEventoDeRegistroTardio`. |
| El traductor no reconoce el interbloqueo | Las 3 pruebas de integración del interbloqueo (nadie confirmó, la gemela ya confirmó y el alta de una ficha que debe subir como excepción). |

Una primera mutación del interbloqueo no compiló por un nombre mal escrito, así que su «resultado» no valía; se repitió con una mutación válida.


