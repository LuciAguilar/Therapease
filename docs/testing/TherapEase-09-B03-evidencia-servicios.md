# TherapEase — B03 · Evidencia del servidor de agenda y citas (verificación de Miguel)

**Actualizada el 11-nov-2026 · [PR #18](https://github.com/LuciAguilar/Therapease/pull/18), rama `b03/agenda-y-citas`, desde main `6dd13d9`.**

## 0. Rondas de revisión

| Ronda | Contenido | Resultado |
| --- | --- | --- |
| 1 | `fe6b79a`: servidor de agenda y citas, con un tratamiento especial del `40P01` en el alta. | Lucía revisó ese SHA: 810 unitarias y 27 integraciones de citas. Su QA dirigido dio **4/5**: un `40P01` provocado solo en `EventoAuditoria` respondía «horario ocupado» con cero citas y eventos; debía subir como error 500 sin relectura, como estaba aprobado. Aprobó la alternativa del bloqueo de asesoría en un disparador de `Cita`. |
| 2 | `05a7936`: migración con el disparador (§3), retiro del `catch` y de la traducción del `40P01` (vuelve a ser error general), pruebas nuevas y documentos. | Lucía revisó ese SHA: ya aparecen el disparador y la retirada del tratamiento especial del `40P01`. Pidió incluir en este mismo PR la mejora de arranque. |
| 3 | Esta corrección: si falta `ConnectionStrings:TherapEase`, la aplicación y los comandos del operador se detienen con un mensaje fijo, sin mostrar la cadena (§4.1). | `479ab5b`. Resultados de la sección 2. |

## 1. Qué se implementó y con qué se comprueba

| Estado | Parte | Pruebas | Casos |
| --- | --- | --- | --- |
| ✅ | Dos permisos nuevos (`ConsultarAgenda`, `AgendarCitas`) para Usuaria y Superusuario, con sus políticas web | `MatrizDePermisosTests` (incluye su prueba de completitud) | 34 |
| ✅ | Regla de horario en Domain: las 752 combinaciones válidas contra una lista escrita aparte, los 1 440 × 1 440 pares de minutos, ejemplos del plan, segundos y fracciones, fin del día siguiente, siete días, fecha pasada y fechas límite | `ReglasDeHorarioDeCitaTests` | 41 |
| ✅ | Conversión: `HoraHermosillo.EsRepresentable` y leyenda «Registrada después de su horario» (`FechaAlta` mayor, igual y menor que `Inicio`) | `HoraHermosilloTests` (13, seis nuevos) y `ReglasDeCitaTests` (21, cuatro nuevos) | 34 |
| ✅ | Servicio de citas: permisos antes de leer, entradas, horario, paciente y ámbito, resultados, los estados del reenvío con sus tres salidas, fallos de auditoría o de la base sin relectura (también un fallo transitorio), agenda (periodos, vistas, 500 filas, bajas sin nombre, conversión de horas) y detalle | `ServicioCitasTests` | 94 |
| ✅ | Contrato de Pacientes para Citas: permiso, condición y ámbitos, baja sin ámbitos, nombres solo de vigentes, una sola consulta | `ConsultaDePacientesParaCitasTests` | 12 |
| ✅ | Guardado y evento en una transacción, `PK_Cita`, reenvío con la gemela confirmada antes del guardado, entre la primera lectura y el guardado y antes del rechazo de ámbito (sin restaurarlo), pausas antes de guardar, reservas simultáneas (dos y diez), fallo de auditoría y alcance posterior, **`40P01` solo en `EventoAuditoria` como error general**, una sola consulta por lectura, agenda con PostgreSQL real y detalle | `CitasServidorTests` (PostgreSQL desechable) | 26 |
| ✅ | Disparador del bloqueo: cuándo se toma y cuándo no (altas y cambios), orden antes del `FOR SHARE` del paciente, serialización con cruce real, liberación al revertir, migración que sube, baja y vuelve a subir, y el esquema que la aplicación no puede cambiar | `BloqueoDeAgendaEnCitaActivaTests` (PostgreSQL desechable) | 20 |
| ✅ | Arranque sin cadena de conexión: `AgregarPersistencia` se detiene con el mensaje fijo si la cadena es nula, vacía o solo espacios, antes de registrar nada; el mensaje nombra la clave y la variable y no lleva ninguna cadena ni secreto; con cadena registra la unidad de trabajo y los repositorios; una configuración posterior del contexto sigue sustituyendo la conexión | `InyeccionDeDependenciasTests` | 11 |
| ✅ | La aplicación y los comandos `--crear-superusuario` y `--restablecer-superusuario`, como procesos sin la cadena: salen con código 1, solo el mensaje fijo por el error estándar, sin traza, sin contraseña temporal y sin ningún valor de configuración; el error de uso (código 2) y la sonda de salud no cambian | `ArranqueSinCadenaDeConexionTests` (procesos; no necesitan PostgreSQL) | 10 |

`CitasServidorTests` y `BloqueoDeAgendaEnCitaActivaTests` son verificaciones de Miguel de lo que pide la propuesta (secciones 3.11 y 3.12, D9) y la revisión de Lucía. Según AGENTS §2.6 el QA de integración con Testcontainers es de Lucía; estas pruebas no lo sustituyen. **Quedan para el QA dirigido de Lucía** y no se incluyen aquí: agendar frente a baja en sus tres órdenes (plan B03 §4.1), el ámbito retirado sin cita confirmada con sus pausas forzadas, las zonas horarias, las páginas y el navegador.

## 2. Resultados

| Estado | Comprobación | Resultado |
| --- | --- | --- |
| ✅ | Compilación Release de la solución, advertencias como errores | 0 advertencias y 0 errores. |
| ✅ | Unitarias | 819 correctas, 0 fallidas, 0 omitidas (ronda 2: 808; ronda 1: 810; antes de B03: 646). |
| ✅ | Integración completa con dos hilos | 521 correctas, 0 fallidas, 0 omitidas, 4 min 1 s (ronda 2: 511; ronda 1: 492; antes de B03: 465). Incluye todas las pruebas que sustituyen la conexión. |
| ✅ | `CitasServidorTests` repetida cinco veces seguidas (ronda 2) | 26 de 26 en cada una, con las dos rondas de diez reservas simultáneas incluidas. |
| ✅ | `dotnet ef migrations has-pending-model-changes` | «No changes have been made to the model since the last migration.» Con una cadena de conexión ficticia. |
| ✅ | Fijaciones y archivos sensibles del repositorio | `revisar-fijaciones.sh` y `revisar-archivos-sensibles.sh` sin hallazgos. |
| ✅ | Comprobaciones de que las pruebas no son vacías | Con una mutación a la vez, falló lo esperado y se restauró el código (sección 6). |
| ✅ | SHA de esta ronda | `479ab5b` (`479ab5ba41bc617c510b799661874808981e6f0a`): arranque sin cadena de conexión, ya publicado en el PR #18. Rondas anteriores: `fe6b79a` (1) y `05a7936` (2). Es el código que se verificó y sobre el que Lucía repite su QA; el commit que registra este número solo cambia documentación. |
| ⏳ | QA de Lucía, páginas de Dulce y navegador | Pendientes. Lucía revisa el SQL de la migración antes de aprobar. |

```powershell
dotnet build TherapEase.sln -c Release --no-restore -warnaserror
dotnet test tests/TherapEase.UnitTests -c Release --no-build
dotnet test tests/TherapEase.IntegrationTests -c Release --no-build -- xUnit.MaxParallelThreads=2
dotnet ef migrations has-pending-model-changes --project src/Infrastructure --startup-project src/Web
```

Docker Desktop iniciado antes de las pruebas de integración. `MaxParallelThreads=2` evita tiempos de espera falsos por carga; no cambia ninguna aserción.

## 3. Migración `BloqueoDeAgendaEnCitaActiva`: SQL, impacto y reversión

Archivo: `src/Infrastructure/Migraciones/20261110025757_BloqueoDeAgendaEnCitaActiva.cs`. No cambia el modelo de EF (el Designer solo cambia de nombre y `has-pending-model-changes` queda limpio). Es la alternativa que Lucía aprobó en su revisión del PR #18.

**Por qué.** `EX_Cita_SinCruceActivo` es una restricción de exclusión: cada alta inserta su fila y después comprueba si choca. Con dos altas conflictivas que llegan a la vez, cada una encuentra la fila aún sin confirmar de la otra y espera su transacción; PostgreSQL aborta una por interbloqueo (`40P01`) en lugar de rechazarla con el cruce (`23P01`). Con el bloqueo, la segunda espera a que la primera confirme o se revierta y la restricción compara contra filas ya confirmadas.

**Subida: script que genera EF** (`dotnet ef migrations script 20261028032404_DatosDePacienteYTutor 20261110025757_BloqueoDeAgendaEnCitaActiva`), con su transacción y el registro en el historial de migraciones.

```sql
START TRANSACTION;
CREATE FUNCTION "FnCitaSerializaCruceActivo"() RETURNS trigger
LANGUAGE plpgsql AS $$
BEGIN
    PERFORM pg_advisory_xact_lock(7301);
    RETURN NEW;
END $$;

CREATE TRIGGER "TR_Cita_00SerializaAltaActiva"
BEFORE INSERT ON "Cita"
FOR EACH ROW WHEN (NEW."Estado" = 'Agendada' AND NEW."Condicion" = 'Vigente')
EXECUTE FUNCTION "FnCitaSerializaCruceActivo"();

CREATE TRIGGER "TR_Cita_00SerializaCambioActivo"
BEFORE UPDATE OF "Inicio", "Fin", "Estado", "Condicion" ON "Cita"
FOR EACH ROW WHEN (
    NEW."Estado" = 'Agendada' AND NEW."Condicion" = 'Vigente'
    AND (NEW."Inicio", NEW."Fin", NEW."Estado", NEW."Condicion")
        IS DISTINCT FROM (OLD."Inicio", OLD."Fin", OLD."Estado", OLD."Condicion"))
EXECUTE FUNCTION "FnCitaSerializaCruceActivo"();

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261110025757_BloqueoDeAgendaEnCitaActiva', '10.0.12');

COMMIT;
```

**Bajada: script que genera EF** (`dotnet ef migrations script 20261110025757_BloqueoDeAgendaEnCitaActiva 20261028032404_DatosDePacienteYTutor`).

```sql
START TRANSACTION;
DROP TRIGGER IF EXISTS "TR_Cita_00SerializaCambioActivo" ON "Cita";
DROP TRIGGER IF EXISTS "TR_Cita_00SerializaAltaActiva" ON "Cita";
DROP FUNCTION IF EXISTS "FnCitaSerializaCruceActivo"();

DELETE FROM "__EFMigrationsHistory"
WHERE "MigrationId" = '20261110025757_BloqueoDeAgendaEnCitaActiva';

COMMIT;
```

**Cómo cumple lo pedido.**

| Requisito | Cómo |
| --- | --- |
| Clave fija | `pg_advisory_xact_lock(7301)`: una sola clave para toda la agenda (hoy hay una sola). Se libera sola al confirmar o revertir. |
| Solo si la fila resultante queda agendada y vigente | La cláusula `WHEN` de ambos disparadores. Cancelar o dar de baja una cita libera horario y no toma el bloqueo. |
| Al insertar y al cambiar inicio, fin, estado o condición | Un disparador `BEFORE INSERT` y otro `BEFORE UPDATE OF "Inicio", "Fin", "Estado", "Condicion"`, este último solo si alguno de esos cuatro valores cambia de verdad. Cambiar solo el pago o los metadatos no toma el bloqueo. |
| Antes del `FOR SHARE` del paciente | Los disparadores `BEFORE` del mismo evento se ejecutan por orden alfabético de su nombre. Los nuevos empiezan con `00` y `TR_Cita_ActivaExigePacienteVigente` con `Ac`: primero el bloqueo de asesoría y después el control del paciente. Lo comprueba una prueba que lista el orden y otra que retiene la fila del paciente y observa que el alta, detenida en el `FOR SHARE`, ya tiene el bloqueo concedido. |
| Conservar la restricción de cruces y los controles del paciente | No se tocan `EX_Cita_SinCruceActivo`, `TR_Cita_ActivaExigePacienteVigente` ni `TR_Paciente_BajaSinCitasActivas`; la prueba de migración comprueba que siguen tras bajar y subir. |

**Impacto.**

- **Datos:** ninguno. No se lee ni se cambia ninguna fila; solo se agregan una función y dos disparadores. Las citas anteriores o de prueba no se validan ni se cambian.
- **Permisos:** nada que conceder. `pg_advisory_xact_lock` la puede ejecutar cualquier rol y `therapease_app` sigue sin poder cambiar el esquema (una prueba comprueba que no puede eliminar los disparadores: `42501`). La migración la aplica el migrador.
- **Concurrencia:** las altas y los cambios de citas activas se serializan en toda la base mientras dura cada transacción, hasta confirmar o revertir, incluido el evento de auditoría. Con una sola usuaria operativa y transacciones de milisegundos, el efecto es despreciable. La restricción de cruces ya era global (propuesta §5): si algún día hubiera más de una agenda, harían falta una columna y una migración, y esta clave fija también habría que revisarla.
- **Interbloqueos:** el orden es siempre bloqueo de asesoría → `FOR SHARE` del paciente → restricción. Las otras rutas que bloquean al paciente (la baja actualiza `Paciente`; la edición de ficha también) no escriben citas y la baja solo lee `Cita`, así que no se forma un ciclo. Quien en el futuro escriba una cita activa no debe bloquear antes a su paciente en la misma transacción; el disparador ya toma el bloqueo de asesoría primero.
- **Pausas del plan B03 §4.1:** el bloqueo se toma al insertar, no al iniciar la transacción. Una solicitud en pausa justo antes de guardar no retiene nada: la gemela puede confirmar su reserva completa mientras tanto. Una prueba lo demuestra con tiempo máximo.
- **Rendimiento:** una llamada de bloqueo por fila afectada, sin lecturas adicionales.
- **Qué pasa si se elimina el disparador:** vuelve el comportamiento anterior. La restricción sigue impidiendo cruces, pero dos altas simultáneas pueden terminar con un `40P01` (error 500) en lugar del cruce.

**Reversión.** `Down` elimina los dos disparadores y la función, y no hay datos que revertir. Con el migrador: `dotnet ef database update 20261028032404_DatosDePacienteYTutor --project src/Infrastructure --startup-project src/Web`. Una prueba sube, baja y vuelve a subir con una cita guardada: tras bajar desaparecen los disparadores y la función, la restricción y los controles del paciente siguen, la cita sigue y un cruce directo lo rechaza `23P01`; tras volver a subir todo está de nuevo.

## 4. Qué cambió en el servicio y qué se probó

**El hallazgo de la ronda 1.** La propuesta (§3.6) suponía que, con reservas simultáneas por el mismo horario, las demás esperan al primero y reciben el conflicto. No siempre ocurre: PostgreSQL puede abortar una con `40P01`. Se reprodujo en una base desechable aparte del repositorio y con el servicio (dos y diez solicitudes). Se probaron y descartaron reintentar la operación (con diez solicitudes algunas se interbloqueaban en los tres intentos; 4 de 5 ejecuciones fallaron y cada una tardó 31 s), un bloqueo de asesoría al iniciar la transacción (impediría las pausas del plan B03 §4.1) y traducir el `40P01` a «horario ocupado».

**Lo que falló en la revisión de Lucía.** Esa traducción confundía un `40P01` provocado solo en `EventoAuditoria` con un cruce: respondía «horario ocupado» con cero citas y eventos. Un fallo de la auditoría debe subir como error 500 sin relectura.

**Qué se hizo.**

- Se retiraron el `catch` del alta, la traducción especial del `40P01` en `TraductorDeErroresDeBase` (que vuelve a devolver `ReglaDeNegocioException?`) y `InterbloqueoDeBaseException`. Un interbloqueo u otro fallo de la base vuelve a ser error general en todos los módulos.
- Se agregó la migración de la sección 3, que hace que las reservas simultáneas por el mismo horario terminen siempre en el cruce real.

| Qué se comprueba | Prueba |
| --- | --- |
| Un `40P01` provocado solo en `EventoAuditoria` sube como error general: no es una regla de negocio, trae el `40P01`, deja cero citas y cero eventos y no hay relectura (una sola lectura de `Cita`) | `Agendar_SiLaAuditoriaFallaConInterbloqueo_SubeComoErrorGeneralSinRelecturaNiCambios` |
| Un fallo transitorio de la base, aunque la gemela ya estuviera confirmada, sube sin relectura ni anuncio | `Agendar_UnFalloTransitorioDeLaBase_SubeComoErrorGeneralSinRelecturaNiAnuncio` |
| Con dos y con diez solicitudes simultáneas por el mismo horario hay una sola cita y un solo evento, y las demás reciben «horario ocupado» (sin la traducción, un solo `40P01` haría fallar la prueba) | `Agendar_DosReservasDistintasPorElMismoHorario…` y `Agendar_DiezReservasSimultaneasPorElMismoHorario…` |
| La segunda alta espera a la primera y la restricción la rechaza con `23P01`, nunca con `40P01`; con horarios distintos también se serializan; una transacción revertida libera el bloqueo | `DosAltasDelMismoHorario_…`, `DosAltasDeHorariosDistintos_…` y `UnaTransaccionRevertida_…` |
| Pausa antes de guardar: la gemela confirma su reserva completa mientras la otra está detenida y no queda esperando el bloqueo | `Agendar_LaGemelaConfirmaCompletaMientrasLaOtraEstaEnPausaAntesDeGuardar_…` y las pruebas de la gemela de la ronda 1 |
| Cuándo se toma el bloqueo y cuándo no, en altas y cambios | `UnaAltaActiva_…`, `UnaAltaQueNoQuedaAgendadaYVigente_…` y `UnCambioDeCita_…` |
| El bloqueo se toma antes del `FOR SHARE` del paciente | `ElBloqueoSeTomaAntesDelBloqueoCompartidoDelPaciente` y `LosDisparadoresDeAntesSeEjecutanEnEsteOrden_…` |
| Reversión: subir, bajar y volver a subir | `LaMigracion_SubeBajaYVuelveASubir_…` |

Lo ensayado con simultaneidad es **dos y diez solicitudes**; no se afirma nada sobre otras cantidades.

### 4.1. Arranque sin cadena de conexión (ronda 3)

**Qué pidió Lucía.** Si falta `ConnectionStrings:TherapEase`, la aplicación y `--crear-superusuario` deben detenerse con un mensaje claro y fijo, sin mostrar la cadena. Centralizarlo en `AgregarPersistencia`, con su unitaria y conservando las pruebas que sustituyen la conexión.

**Qué se hizo.**

- `InyeccionDeDependencias.AgregarPersistencia` lanza `InvalidOperationException` con el mensaje fijo `MensajeSinCadenaDeConexion` si la cadena es nula, vacía o solo espacios, **antes de registrar nada**. Es el único lugar donde se decide.
- El mensaje nombra la clave `ConnectionStrings:TherapEase` y la variable `ConnectionStrings__TherapEase`; nunca una cadena de conexión ni un valor de configuración.
- `Program.cs` y `ComandosDeIdentidad` solo presentan el error: escriben ese mensaje por el error estándar y salen con código 1, sin traza. `--restablecer-superusuario` recibe el mismo trato que `--crear-superusuario`, porque ambos pasan por el mismo arranque.
- No cambian el error de uso del comando (código 2, antes de mirar la conexión), la sonda de salud del contenedor (corre antes de configurar la persistencia), `dotnet ef` (usa la conexión de migración) ni las pruebas que sustituyen la conexión: ninguna prueba existente se modificó.
- Efecto para quien desarrolla: sin la variable `ConnectionStrings__TherapEase` la aplicación ya no arranca. El README lo indica.

| Qué se comprueba | Prueba |
| --- | --- |
| Cadena nula, vacía o de espacios: mensaje fijo y nada registrado; el mensaje no lleva `Host=`, `Username=`, `Password=`, `Database=` ni `Port=` | `AgregarPersistencia_SinCadenaDeConexion_…`, `…NoRegistraNada` y `ElMensajeFijo_…` |
| Con cadena se registran la unidad de trabajo y los tres repositorios, y el contexto usa esa conexión | `AgregarPersistencia_ConCadena_…` |
| Una configuración posterior del contexto (como la de las pruebas de integración) sigue sustituyendo la conexión | `UnaConfiguracionPosteriorDelContexto_SustituyeLaConexion` |
| La aplicación sin cadena (variable ausente, vacía o de espacios) sale con código 1, solo el mensaje fijo, sin escuchar | `LaAplicacion_SinCadenaDeConexion_…` |
| Los dos comandos sin cadena: código 1, solo el mensaje fijo, sin contraseña temporal ni salida estándar | `ElComandoDelOperador_SinCadenaDeConexion_…` |
| Ninguna salida muestra un secreto de otra variable de configuración, ni «Password», ni traza | `ComprobarQueSoloSeMuestraElMensajeFijo` en esas pruebas |
| El error de uso sigue siendo código 2 y la sonda de salud no exige la cadena | `ElComandoSinNombreDeUsuario_…` y `LaSondaDeSalud_…` |

## 5. Pruebas existentes que se tocaron

| Prueba | Cambio | Por qué |
| --- | --- | --- |
| `MatrizDePermisosTests` | Cuatro casos nuevos de la tabla y las listas de permisos de cada rol pasan de seis a ocho y de cuatro a seis | Dos permisos nuevos; su prueba de completitud exige la decisión por rol. |
| `PermisosUsuariosTests` (integración) | Las listas de permisos esperados incluyen `ConsultarAgenda` y `AgendarCitas` | Mismo motivo. |
| `RevisionDatosTests` y `RecuperacionTests` (integración) | El conteo del historial de migraciones pasa de 4 a 5 | Hay una migración nueva. |
| `RepositorioCitasFalso` y `RepositorioPacientesFalso` | Implementan los métodos nuevos de los puertos, con contadores y ganchos para forzar momentos | Los puertos ampliaron su contrato. |
| `ServicioCitasTests` y `CitasServidorTests` | Se retiran las pruebas del tratamiento especial del `40P01` en el alta (tres unitarias y tres de integración); se agregan la regresión de la auditoría, la del fallo transitorio y la pausa | El tratamiento ya no existe. |

Ninguna otra comprobación se redujo. En la ronda 3 no se modificó ninguna prueba existente: las que sustituyen la conexión con otra configuración del contexto se ejecutaron completas en la suite de integración.

## 6. Comprobaciones de que las pruebas no son vacías

Una mutación a la vez, con compilación Release, y restauración del archivo original después de cada una.

**Ronda 3 (arranque sin cadena de conexión):**

| Mutación | Pruebas que fallaron |
| --- | --- |
| `AgregarPersistencia` deja de validar la cadena | 7 de las 11 unitarias y 8 de las 10 de proceso (la aplicación llega a escuchar; esa corrida tardó 2 min 8 s). |
| La aplicación no captura el error | 3 de las 10 de proceso: las de la aplicación sin cadena. |
| El comando no captura el error | 5 de las 10 de proceso: las de los dos comandos. |
| La aplicación imprime la excepción completa en lugar del mensaje fijo | 3 de las 10 de proceso: sin traza ni «Unhandled exception». |
| El mensaje fijo llevaría una cadena de conexión de ejemplo | 1 de las 11 unitarias y 8 de las 10 de proceso. |

**Ronda 2:**

| Mutación | Pruebas que fallaron |
| --- | --- |
| El traductor vuelve a convertir el `40P01` en cruce (el tratamiento de la ronda 1) | `Agendar_SiLaAuditoriaFallaConInterbloqueo_SubeComoErrorGeneralSinRelecturaNiCambios`: la regresión que pidió Lucía. |
| La función del disparador no toma el bloqueo (`PERFORM 1`) | Cuatro casos de `UnCambioDeCita_…`, `UnaTransaccionRevertida_LiberaElBloqueoYLaSiguienteAltaSeConfirma` y `Agendar_DiezReservasSimultaneasPorElMismoHorario…`. La corrida llegó a su límite de 330 s sin terminar (alguna prueba quedó esperando), así que no hay recuento total; lo que falló basta para detectar la mutación. |
| Los disparadores se llaman `TR_Cita_ZZ…` y se ejecutarían después del control del paciente | Las 2 de 2: `ElBloqueoSeTomaAntesDelBloqueoCompartidoDelPaciente` y `LosDisparadoresDeAntesSeEjecutanEnEsteOrden_…`. |
| El disparador del alta pierde su cláusula `WHEN` (tomaría el bloqueo aunque la cita no quede agendada y vigente) | Las 3 de 3 de `UnaAltaQueNoQuedaAgendadaYVigente_NoTomaElBloqueo`. |
| El disparador de cambios usa `IS NOT DISTINCT FROM` (condición invertida) | 5 de los 9 casos de `UnCambioDeCita_TomaElBloqueo…`. |
| `Down` no elimina el disparador de cambios | `LaMigracion_SubeBajaYVuelveASubir_SinTocarLaRestriccionNiLosControlesDelPaciente`: PostgreSQL no deja eliminar la función porque aún depende de ella. |

Una primera corrida de estas mutaciones se interrumpió y sus resultados se perdieron; se repitió completa, una a la vez, con límite de tiempo por corrida y comprobando al final que cada archivo quedó restaurado.

**Ronda 1, siguen vigentes** (el código que prueban no cambió):

| Mutación | Pruebas que fallaron |
| --- | --- |
| Sin la primera lectura por número en `AgendarAsync` | 9 de `ServicioCitasTests`: las seis variantes de «mismo número que no es la misma reserva», el reenvío con el paciente de baja, con el ámbito retirado y el reenvío ya confirmado. |
| El cambio de duración a las 14:00 en lugar de las 15:00 | 3 de `ReglasDeHorarioDeCitaTests`, entre ellas la comparación de los 1 440 × 1 440 pares con la lista independiente. |
| La agenda filtra solo por inicio, no por solape | `ConsultarAgenda_ConPostgreSqlReal_IncluyeLasQueSeSolapanConElPeriodoYExcluyeLasContiguas`. |
| El evento siempre dice «Se agendó una cita.» aunque sea registro tardío | `Agendar_ConFechaPasada_SePermiteConLeyendaYEventoDeRegistroTardio`. |

