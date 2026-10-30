# TherapEase — B02 · Evidencia del servidor de pacientes (verificación de Miguel)

**Actualizado el 28-oct-2026 · [PR #11](https://github.com/LuciAguilar/Therapease/pull/11), rama `b02/fichas-de-pacientes`.** 

## 1. Qué se implementó y con qué se comprueba

| Estado | Parte | Pruebas | Casos |
| --- | --- | --- | --- |
| ✅ | Tres permisos nuevos (`RegistrarPacientes`, `ConsultarPacientes`, `ActualizarPacientes`) para Usuaria y Superusuario, con sus políticas web | `MatrizDePermisosTests` (incluye su prueba de completitud) | 29 |
| ✅ | Reglas de la ficha: campos, límites, nacimiento, tutor, metadatos, versión, sin cambios y ámbitos | `ReglasDePacienteTests` | 65 |
| ✅ | Clave de nombre normalizada: acentos, formas Unicode, minúsculas de cualquier escritura (Ø, Ж, İ, Σ), espacios e independencia de la cultura | `NormalizacionDeNombresTests` | 29 |
| ✅ | Teléfono: formatos aceptados y rechazados, y lo que se guarda | `ReglasDeTelefonoTests` | 25 |
| ✅ | Edad, mayoría de edad, 29 de febrero y fecha de Hermosillo frente a UTC | `ReglasDeEdadTests` | 15 |
| ✅ | Reglas compartidas de nombre/correo: cuentas conserva 150 y pacientes 200 | `ReglasDeNombreYCorreoTests` y las pruebas de cuentas existentes | 17 |
| ✅ | Servicio: permisos antes de leer, resultados del alta, parecidos, reenvío, carreras, búsqueda, una sola lectura, conflicto, bajas y fallos de auditoría o de la base | `ServicioPacientesTests` | 90 |
| ✅ | Guardado y evento en una transacción, carreras del mismo alta contra la clave primaria real, conflicto de versión, ámbitos que se quitan, una sola consulta SQL a `Paciente`, comodines literales, mayúsculas fuera de A–Z, alcance tras un guardado fallido, el mismo envío que se guarda justo antes de buscar parecidos y restricciones de la base | `PacientesServidorTests` (PostgreSQL desechable) | 40 |
| ✅ | Migración desde datos anteriores: correspondencia de Contacto, clave igual a la de Domain, subir, bajar y volver a subir, permisos y datos intactos, cancelación previa | `MigracionDeDatosDePacienteTests` (PostgreSQL desechable) | 7 |

`PacientesServidorTests` y `MigracionDeDatosDePacienteTests` son verificaciones de Miguel de lo que pide la propuesta (secciones 3.5 y 3.7: consulta SQL única, migración demostrada con muestras, carreras). Según AGENTS §2.6 el QA de integración con Testcontainers es de Lucía; estas pruebas no lo sustituyen. **Lucía decidió el 28-oct conservar las 38 pruebas de integración originales** (31 y 7); las ocho de `PacientesServidorTests` añadidas después son las regresiones de la sección 6.

## 2. Resultados

| Estado | Comprobación | Resultado |
| --- | --- | --- |
| ✅ | CI del PR #11 sobre `1eab29c` | [Ejecución 40639773900](https://github.com/LuciAguilar/Therapease/actions/runs/40639773900): cuatro trabajos correctos (38 s, 39 s, 27 s y 2 min 49 s); ventana de 3 min 33 s y suma de trabajos de 4 min 33 s. |
| ✅ | Compilación Release de la solución, advertencias como errores, con las correcciones | 0 advertencias y 0 errores. |
| ✅ | `dotnet ef migrations has-pending-model-changes`, con las correcciones | Sin cambios pendientes del modelo. |
| ✅ | Unitarias, con las correcciones de las dos revisiones | 646 correctas, 0 fallidas, 0 omitidas (en `a78016a`: 640; en `1eab29c`: 624; antes de B02: 422). |
| ✅ | Integración completa con dos hilos, con las correcciones de las dos revisiones | 376 correctas, 0 fallidas, 0 omitidas, 2 min 51 s (en `a78016a`: 375; en `1eab29c`: 367; antes de B02: 329). |
| ✅ | Concurrencia y migración repetidas tres veces | 18 correctas en cada una: `ConcurrenciaServidorTests`, las carreras de alta y edición de `PacientesServidorTests` y `MigracionDeDatosDePacienteTests`. |
| ✅ | Comprobaciones de que las pruebas no son vacías | Con una mutación a la vez, falló la prueba esperada y se restauró el código: recorte del espacio de no separación en la migración; quitar `ChangeTracker.Clear()` (fallaron las dos pruebas de alcance); volver a la minúscula solo A–Z (fallaron 14 pruebas de clave y de búsqueda). |
| ✅ | CI de las correcciones del 28-oct, PR #11 sobre `a78016a` | [Ejecución 40827839680](https://github.com/LuciAguilar/Therapease/actions/runs/40827839680): cuatro trabajos correctos (37 s, 46 s, 27 s y 2 min 11 s); ventana de 2 min 53 s y suma de trabajos de 4 min 1 s. Los registros de cada trabajo piden iniciar sesión, así que el número de casos de la CI no se consultó aquí. |
| ✅ | Doble envío, 30 repeticiones | Las dos pruebas del doble envío (cinco rondas simultáneas la primera) pasaron 30 de 30 veces. |
| ✅ | Concurrencia y migración repetidas tres veces, con las correcciones de la segunda revisión | 19 correctas en cada una. |
| ✅ | Comprobaciones de que las pruebas de la segunda revisión no son vacías | Quitando cada corrección por separado: sin volver a buscar por número de ficha fallan 2 pruebas unitarias y la de integración del momento forzado; con la minúscula ICU anterior del correo fallan 3 pruebas de migración («οσ» contra «ος»). |
| ⏳ | CI de las correcciones de la segunda revisión | Pendiente del commit y del push de Miguel. |
| ⏳ | QA de Lucía, páginas de Dulce y navegador | Pendientes. |

Dos incidencias de las ejecuciones, para que no se confundan con fallos del producto:

- En una primera ejecución completa, antes de la CI, fallaron 4 casos de pruebas existentes de Lucía por cambios esperados del propio B02 (lista de permisos explícitos, 2 casos; conteo de migraciones de 3 a 4, 2 casos). Se actualizaron (sección 5).
- Con las correcciones, una de las ejecuciones completas dio **un fallo en `Busqueda_NoDistingueAcentosNiMayusculasConPostgreSqlReal` («maria munoz»)** y no se capturó su mensaje. No se reprodujo: pasó 5 de 5 veces aislada y dos ejecuciones completas posteriores dieron 375/375. Esa ejecución fue más lenta (3 min 20 s frente a 2 min 41 s); la causa probable es carga del equipo, no confirmada. En la CI de `a78016a` no se repitió.
- Una prueba de regresión mía falló dos de tres veces porque comparaba una lista en orden fijo con GUID aleatorios; era un error de la aserción, corregido y repetido cinco veces.

```powershell
dotnet build TherapEase.sln -c Release --no-restore -warnaserror -m:1 -p:UseSharedCompilation=false
dotnet test tests/TherapEase.UnitTests -c Release --no-build --no-restore
dotnet test tests/TherapEase.IntegrationTests -c Release --no-build --no-restore -- xUnit.MaxParallelThreads=2
dotnet ef migrations has-pending-model-changes --project src/Infrastructure --startup-project src/Web
```

Docker Desktop iniciado a mano antes de las pruebas de integración. `MaxParallelThreads=2` evita tiempos de espera falsos por carga; no cambia ninguna aserción.

## 3. Migración `DatosDePacienteYTutor`

EF Core propuso renombrar `Contacto` como `TutorNombre`, lo que habría conservado un texto de contacto como nombre de tutor. Se descartó esa propuesta y la migración se escribió a mano con la correspondencia aprobada por Lucía el 26-oct. Se revisa con `dotnet ef migrations script 20261017205258_NombreCompletoDeUsuario 20261028032404_DatosDePacienteYTutor` (subida) y con los dos nombres invertidos (bajada); el SQL de ambas se ejecuta en una sola transacción. Los comentarios al inicio del archivo de la migración explican cada paso.

**Subida:** agrega las columnas; cancela con un mensaje que no incluye el nombre si algún nombre no produce una clave de búsqueda válida (vacía o de más de 600 caracteres); rellena `NombreNormalizado`; copia `Contacto` a `Correo` o `Telefono`; retira `Contacto`; agrega el índice y las restricciones. Nacimiento, escuela, grupo y tutor quedan nulos.

| Contacto anterior (ficticio) | Correo | Teléfono |
| --- | --- | --- |
| nulo, vacío o solo espacios | nulo | nulo |
| `Persona@Ficticio.TEST`, `ØSCAR@Ficticio.TEST`, con espacios, tabuladores o espacios de no separación a los lados | `persona@ficticio.test`, `øscar@ficticio.test` | nulo |
| `ΟΣ@Ficticio.TEST` (Σ mayúscula) y `İ@Ficticio.TEST` (İ turca) | `οσ@ficticio.test` (σ, no ς) y `İ@ficticio.test` (la İ no cambia), igual que en Domain | nulo |
| ` +52 (644) 123-4567 ` | nulo | `+526441234567` |
| `644-123-4567` | nulo | `6441234567` |
| `sin@dominio`, `a@b..c`, un correo con un carácter invisible dentro | nulo | nulo |
| `llamar al 6441234567`, `correo a@b.c o tel 6441234567`, `123456`, `1234567890123456` | nulo | nulo |

**Bajada:** recrea `Contacto` (`varchar(200)`) con el correo si existe y mide hasta 200 caracteres; si no, con el teléfono; si no, nulo. Si había ambos, se pierde el teléfono. Se pierden también nacimiento, escuela, grupo, tutor y clave de nombre. **Un correo de más de 200 caracteres no se trunca** (un correo cortado sería otro dato): se usa el teléfono o queda nulo. La prueba comprueba estos casos.

**Qué demuestra la prueba:** con 43 contactos y 31 nombres de muestra sembrados en el esquema anterior (entre ellos Ø, Œ, Ж, İ, Σ mayúscula y la sigma final), la clave y la conversión que calcula el SQL son iguales a las de Domain; subir, bajar y volver a subir da el mismo resultado esperado; una huella de identificadores, nombre, condición, versión, metadatos y ámbitos, y la lista de permisos de la aplicación, no cambian; y la clave de nuevas escrituras de Domain coincide con la que recalcula el SQL.

**Comparación exhaustiva, hecha una vez a mano y sin dejarla como prueba:** se calculó la clave de `a` + cada carácter + `b` con la función de Domain y con el SQL de la migración sobre PostgreSQL 17.11, para 1 112 063 puntos de código (todos salvo los sustitutos y U+0000). .NET rechazó uno (U+FFFE, que no es un carácter) y **de los 1 112 062 restantes difieren 27**: U+1C89, U+A7CB, U+A7CC, U+A7DA, U+A7DC y U+10D50 a U+10D65 (escritura garay). Son letras de Unicode 16 que el ICU de PostgreSQL ya baja a minúscula y el .NET de esta máquina todavía no. No se dejó como prueba permanente porque su resultado depende de la versión de Unicode de cada entorno.

## 4. Puntos técnicos para Lucía

Ninguno cambia el alcance ni las reglas aprobadas; se informan para que decida si algo le parece mal.

| Estado | Punto | Qué se hizo |
| --- | --- | --- |
| ✅ | Minúscula después de quitar las marcas. Lucía lo confirmó al revisar `ed162b8` («tu orden para la İ es correcto») | Lucía pidió pasar a minúscula **antes** de quitar acentos. Con ese orden la İ turca no coincide: `ToLowerInvariant` la deja como İ, que al descomponerse da una `I` mayúscula y la clave sería «Istanbul», mientras que ICU da `i` más un punto. Se baja a minúscula **después** de quitar las marcas, en .NET y en PostgreSQL: la İ llega como `I` y queda como `i` en ambos. El resultado para Ø, Ж, İ y Σ es el que ella pidió. Además se iguala la sigma final (ς) con la sigma (σ), porque ICU escribe una u otra según la posición y .NET siempre la segunda. |
| ⏳ | PostgreSQL 17 con ICU | La clave de nombre usa `lower(... COLLATE "und-x-icu")`, que no depende de la configuración regional del servidor pero exige un PostgreSQL compilado con ICU. La minúscula del correo usa la colación integrada `pg_c_utf8`, que existe desde PostgreSQL 17. La imagen `postgres:17` trae ambas. Si el servidor de destino no tuviera alguna, la migración falla antes de cambiar datos; debe comprobarse antes de aplicarla en otro servidor. |
| ⏳ | Restricción de la clave de nombre | La base exige que `NombreNormalizado` tenga la forma de una clave ya normalizada (no vacía, sin A–Z, sin marcas combinantes, sin espacios dobles ni en los extremos), pero **no** que sea igual a lo que recalcularía PostgreSQL. La comparación exhaustiva de la sección 3 confirma que .NET y PostgreSQL pueden diferir en letras nuevas de Unicode, y un nombre válido no debe fallar por eso. |
| ⏳ | SQL frente a Domain en contactos | La clase `[[:space:]]` de PostgreSQL no recorta el espacio de no separación en esta configuración; se agregó una clase explícita con los espacios que recorta `string.Trim()`. Los caracteres de formato invisibles se cubren en el plano básico de Unicode. El correo se baja con `lower(... COLLATE "pg_c_utf8")` partiendo antes cada İ (que `ToLowerInvariant` no cambia) y uniendo de nuevo: coincide con `ToLowerInvariant` en los 1 112 063 puntos de código, sin diferencias. Solo afecta a datos de prueba anteriores. |
| ✅ | 29 de febrero | Decidido por Lucía el 28-oct: en años no bisiestos quien nació un 29 de febrero cumple el 1 de marzo. |
| ⏳ | Contactos de más de 200 caracteres | La columna anterior no admitía más; el límite de 254 del correo nuevo solo se ejerce en escrituras nuevas y en la bajada. |
| ✅ | Reenvío frente a la colisión de la clave primaria | Solo la excepción de la clave primaria de `Paciente` se traduce a «ya existe esa ficha»; un fallo de auditoría u otra restricción no se convierte en alta repetida. La lectura que decide `YaRegistrado` no rastrea entidades. |
| ✅ | Quitar ámbitos con `DeleteBehavior.Restrict` | Se marcan para borrar sin detección intermedia de cambios, porque la clave de `PacienteAmbito` incluye al paciente. Comprobado con PostgreSQL real (quitar, agregar y volver a quitar). |

## 5. Pruebas existentes que se tocaron

Cambios mecánicos por la nueva firma de `ReglasDePaciente.Registrar` y `Actualizar` (datos y ámbitos en lugar de nombre y contacto) y por los permisos y migraciones nuevos. No se modificó ninguna comprobación de fondo.

| Archivo | Cambio |
| --- | --- |
| `BajaPacienteTests`, `HorasDockerTests`, `RecuperacionTests`, `ConsultaAuditoriaRevision`, `ErroresRegistrosRevision` | La ficha se registra con la ayuda nueva `PacientesDePrueba.Adulto(...)`; el contacto rastreable pasa a ser el correo. `ConsultaAuditoriaRevision` y `ErroresRegistrosRevision` comparan `Correo` en lugar de `Contacto`. |
| `RevisionDatosTests` | Registro y dos ediciones con la ayuda nueva; las fichas de las ediciones se leen con sus ámbitos porque `Actualizar` ahora los compara. El conteo de migraciones aplicadas pasa de 3 a 4. |
| `RecuperacionTests` | El conteo de migraciones aplicadas pasa de 3 a 4 en la base restaurada. |
| `PermisosUsuariosTests` | La lista de permisos explícitos de cada rol incluye los tres nuevos. |
| `CoordinadorDeBajaDePacienteTests`, `MatrizDePermisosTests` (unitarias) | Firma nueva de registrar; filas y casos de los permisos nuevos. |
| `RepositorioPacientesFalso` (doble) | Implementa el contrato ampliado, con contadores de lecturas. |

Nuevo: `tests/TherapEase.IntegrationTests/PacientesDePrueba.cs`, que entrega datos válidos de una persona adulta con nacimiento fijo.

## 6. Correcciones de Lucía del 28-oct-2026

Lucía revisó `1eab29c` (624 unitarias y 367 de integración correctas) y pidió corregir en el mismo PR.

| Estado | Corrección | Qué se hizo |
| --- | --- | --- |
| ✅ | Alta repetida: tras reconocer una colisión quedaba pendiente el intento fallido y la siguiente alta del mismo alcance fallaba con error 500 | `UnidadDeTrabajo` ejecuta `ChangeTracker.Clear()` cuando falla un guardado, sea cual sea la causa, antes de traducir o relanzar la excepción. Regresiones en `PacientesServidorTests`: una alta que choca con otra y una segunda alta del mismo alcance que se guarda; y un fallo de auditoría seguido de otra alta que no repite la ficha fallida. Sin la corrección, las dos fallan. |
| ✅ | Mayúsculas fuera de A–Z: buscar «øscar» no encontraba «Øscar» (igual con «жук» y «Жук») | La clave pasa a minúscula con `ToLowerInvariant` en Domain y `lower(... COLLATE "und-x-icu")` en la migración (punto 1 de la sección 4 sobre el orden y la sigma final). Pruebas con Ø, Œ, Ж, İ y Σ en la clave, en la búsqueda del servicio, en la búsqueda con PostgreSQL real, en la detección de parecidos y en la migración. |
| ✅ | Contrato y evidencia decían «sin PR ni CI» | Actualizados con el PR #11 y la CI de `1eab29c`: este documento, el [contrato para las páginas](../planning/TherapEase-etapa09-B02-contrato-para-paginas.md), el [plan B02](../planning/TherapEase-etapa09-B02.md) y el README. Además, este documento tenía por error otro texto pegado a media tabla en los commits anteriores; se reescribió completo. |

Decisiones de Lucía del mismo mensaje: conservar las 38 pruebas de integración de Miguel y el 1 de marzo para los nacidos el 29 de febrero. Después siguen la revisión humana y su aprobación final.

### Segunda revisión de Lucía, sobre `ed162b8`

Lucía dio por buenos R1, los nombres y los documentos, y confirmó el orden de la minúscula para la İ. Pidió dos correcciones más en el mismo PR.

| Estado | Corrección | Qué se hizo |
| --- | --- | --- |
| ✅ | Correos antiguos: la migración convertía «ΟΣ@…» en «ος» y «İ@…» en «i» con un punto aparte, mientras la aplicación guarda «οσ» e «İ» | La minúscula del correo usa `array_to_string(ARRAY(SELECT lower(parte COLLATE "pg_c_utf8") FROM unnest(string_to_array(t, U&'\0130')) WITH ORDINALITY AS u(parte, n) ORDER BY n), U&'\0130')`, como pidió Lucía. Se reprodujo el problema con la expresión anterior y se comprobó la nueva contra `ToLowerInvariant` en 1 112 063 puntos de código: 0 diferencias. Pruebas con esos dos correos en la migración (subir, bajar y volver a subir) y en Domain. Sin la corrección fallan 3 pruebas de migración. |
| ✅ | Doble envío: `Alta_ConDosEnviosSimultaneos…` fallaba a veces porque el segundo envío veía la ficha recién guardada como parecida | `RegistrarAsync` vuelve a buscar por número de ficha antes de devolver parecidos: si la ficha ya existe, responde como reenvío (`YaRegistrado`, o `SolicitudYaUsada` si los datos o la persona difieren). Pruebas que fuerzan ese momento: tres unitarias con un gancho en el doble del repositorio (reenvío, reenvío con otros datos y control de que un parecido legítimo sigue por confirmar) y una de integración con PostgreSQL real que ejecuta el otro envío justo antes de la consulta de parecidos. Sin la corrección fallan las tres que la ejercen. 30 de 30 repeticiones correctas. |


## 7. Límites de esta evidencia

- Solo datos ficticios, sin nube ni gastos. No se probaron páginas, formularios ni navegador.
- Las confirmaciones de producto de Dulce (propuesta 3.8) siguen pendientes; los valores son provisionales para datos de prueba.
- Los casos de este documento no se suman a la evidencia histórica de B00/B01.
