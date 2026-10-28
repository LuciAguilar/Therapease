# TherapEase — B02 · Evidencia del servidor de pacientes (verificación de Miguel)

**27-oct-2026 · rama `b02/fichas-de-pacientes`, sobre main `a7c0c52` más la fusión que incorporó la propuesta aprobada; commit inicial `c9fc7ff`, todavía sin push, PR ni CI.** El SHA final de la entrega y su CI se añaden aquí cuando existan; los resultados de la sección 2 son del código de ese commit. Este documento reúne lo que Miguel ejecutó localmente con datos ficticios. **No es la aprobación de Lucía, no sustituye la revisión humana distinta ni su QA, y no incluye páginas ni navegador.**

## 1. Qué se implementó y con qué se comprueba

| Estado | Parte | Pruebas | Casos |
| --- | --- | --- | --- |
| ✅ | Tres permisos nuevos (`RegistrarPacientes`, `ConsultarPacientes`, `ActualizarPacientes`) para Usuaria y Superusuario, con sus políticas web | `MatrizDePermisosTests` (incluye su prueba de completitud) | 29 |
| ✅ | Reglas de la ficha: campos, límites, nacimiento, tutor, metadatos, versión, sin cambios y ámbitos | `ReglasDePacienteTests` | 65 |
| ✅ | Clave de nombre normalizada (acentos, formas Unicode, espacios, independiente de la cultura) | `NormalizacionDeNombresTests` | 18 |
| ✅ | Teléfono: formatos aceptados y rechazados, y lo que se guarda | `ReglasDeTelefonoTests` | 25 |
| ✅ | Edad, mayoría de edad, 29 de febrero y fecha de Hermosillo frente a UTC | `ReglasDeEdadTests` | 15 |
| ✅ | Reglas compartidas de nombre/correo: cuentas conserva 150 y pacientes 200 | `ReglasDeNombreYCorreoTests` y las pruebas de cuentas existentes | 14 |
| ✅ | Servicio: permisos antes de leer, resultados del alta, parecidos, reenvío, carreras, búsqueda, una sola lectura, conflicto, bajas y fallos de auditoría o de la base | `ServicioPacientesTests` | 82 |
| ✅ | Guardado y evento en una transacción, carreras del mismo alta contra la clave primaria real, conflicto de versión entre dos personas, ámbitos que se quitan, una sola consulta SQL a `Paciente`, comodines literales, restricciones y permisos de la base | `PacientesServidorTests` (PostgreSQL desechable) | 31 |
| ✅ | Migración desde datos anteriores: correspondencia de Contacto, clave igual a la de Domain, subir, bajar y volver a subir, permisos y datos intactos, cancelación previa | `MigracionDeDatosDePacienteTests` (PostgreSQL desechable) | 7 |

`PacientesServidorTests` y `MigracionDeDatosDePacienteTests` son verificaciones de Miguel de lo que pide la propuesta (secciones 3.5 y 3.7: consulta SQL única, migración demostrada con muestras, carreras). Según AGENTS §2.6 y la propuesta, el QA de integración con Testcontainers es de Lucía; estas pruebas no lo sustituyen y su inclusión en el PR queda a decisión de Lucía.

## 2. Resultado de esta ejecución

| Estado | Comprobación | Resultado |
| --- | --- | --- |
| ✅ | Compilación Release de la solución, advertencias como errores | 0 advertencias y 0 errores. |
| ✅ | `dotnet ef migrations has-pending-model-changes` | Sin cambios pendientes del modelo. |
| ✅ | Unitarias | 624 correctas, 0 fallidas, 0 omitidas (antes de B02: 422). |
| ✅ | Integración completa con dos hilos | 367 correctas, 0 fallidas, 0 omitidas, 2 min 27 s (antes de B02: 329; 38 casos nuevos). |
| ✅ | Comprobación de que la prueba de migración no es vacía | Se quitó a propósito el recorte del espacio de no separación en el SQL: la prueba falló con la muestra afectada; se restauró la migración. |
| ⏳ | CI de GitHub | Pendiente del push y del PR de Miguel. |
| ⏳ | QA de Lucía, páginas de Dulce y navegador | Pendientes. |

En una primera ejecución completa fallaron 4 casos de pruebas existentes de Lucía, por cambios esperados del propio B02: la lista de permisos explícitos de Usuaria y Superusuario (2 casos) y el conteo de migraciones aplicadas (2 casos, de 3 a 4). Se actualizaron (sección 5) y la ejecución final es la de la tabla.

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
| `Persona@Ficticio.TEST`, con espacios, tabuladores o espacios de no separación a los lados | `persona@ficticio.test` | nulo |
| ` +52 (644) 123-4567 ` | nulo | `+526441234567` |
| `644-123-4567` | nulo | `6441234567` |
| `sin@dominio`, `a@b..c`, un correo con un carácter invisible dentro | nulo | nulo |
| `llamar al 6441234567`, `correo a@b.c o tel 6441234567`, `123456`, `1234567890123456` | nulo | nulo |

**Bajada:** recrea `Contacto` (`varchar(200)`) con el correo si existe y mide hasta 200 caracteres; si no, con el teléfono; si no, nulo. Si había ambos, se pierde el teléfono. Se pierden también nacimiento, escuela, grupo, tutor y clave de nombre. **Un correo de más de 200 caracteres no se trunca** (un correo cortado sería otro dato): se usa el teléfono o queda nulo. La prueba comprueba estos casos.

**Qué demuestra la prueba:** con 40 contactos y 24 nombres de muestra sembrados en el esquema anterior, la clave y la conversión que calcula el SQL son iguales a las de Domain; subir, bajar y volver a subir da el mismo resultado esperado; una huella de identificadores, nombre, condición, versión, metadatos y ámbitos, y la lista de permisos de la aplicación, no cambian; y la clave de nuevas escrituras de Domain coincide con la que recalcula el SQL.

## 4. Puntos técnicos para Lucía

Ninguno cambia el alcance ni las reglas aprobadas; se informan para que decida si algo le parece mal.

| Estado | Punto | Qué se hizo |
| --- | --- | --- |
| ⏳ | Restricción de la clave de nombre | La base exige que `NombreNormalizado` tenga la forma de una clave ya normalizada (no vacía, sin A–Z, sin marcas combinantes, sin espacios dobles ni en los extremos), pero **no** que sea igual a lo que recalcularía PostgreSQL. .NET y PostgreSQL pueden diferir en caracteres exóticos según su versión de Unicode y un nombre válido no debe fallar por eso. La igualdad se demuestra con muestras en la prueba de migración. |
| ⏳ | SQL frente a Domain en contactos | La clase `[[:space:]]` de PostgreSQL no recorta el espacio de no separación en esta configuración; se agregó una clase explícita con los espacios que recorta `string.Trim()`. Los caracteres de formato invisibles se cubren en el plano básico de Unicode; los de planos superiores no. `lower()` de PostgreSQL y `ToLowerInvariant` coinciden con las muestras, incluidas letras acentuadas. Solo afecta a datos de prueba anteriores. |
| ⏳ | 29 de febrero | En años no bisiestos quien nació un 29 de febrero cumple el 1 de marzo (no se le trata como adulto un día antes). Es una elección de Miguel; la propuesta pide cubrir el caso pero no fija el día. |
| ⏳ | Contactos de más de 200 caracteres | La columna anterior no admitía más; el límite de 254 del correo nuevo solo se ejerce en escrituras nuevas y en la bajada. |
| ✅ | Reenvío frente a la colisión de la clave primaria | Solo la excepción de la clave primaria de `Paciente` se traduce a «ya existe esa ficha»; un fallo de auditoría u otra restricción no se convierte en alta repetida. La lectura que decide `YaRegistrado` no rastrea entidades, para no leer la ficha candidata que pudo quedar en el contexto. |
| ✅ | Quitar ámbitos con `DeleteBehavior.Restrict` | Se marcan para borrar sin detección intermedia de cambios, porque la clave de `PacienteAmbito` incluye al paciente. Comprobado con PostgreSQL real (quitar, agregar y volver a quitar). |

## 5. Pruebas existentes que se tocaron

Cambios mecánicos por la nueva firma de `ReglasDePaciente.Registrar` y `Actualizar` (datos y ámbitos en lugar de nombre y contacto) y por los permisos y migraciones nuevos. No se modificó ninguna comprobación de fondo.

| Archivo | Cam# TherapEase — B02 · Evidencia del servidor de pacientes (verificación de Miguel)

**27-oct-2026 · rama `b02/fichas-de-pacientes`, sobre main `a7c0c52`** El SHA de la entrega y su CI se añaden aquí cuando existan. Este documento reúne lo que Miguel ejecutó localmente con datos ficticios. **No es la aprobación de Lucía, no sustituye la revisión humana distinta ni su QA, y no incluye páginas ni navegador.**

## 1. Qué se implementó y con qué se comprueba

| Estado | Parte | Pruebas | Casos |
| --- | --- | --- | --- |
| ✅ | Tres permisos nuevos (`RegistrarPacientes`, `ConsultarPacientes`, `ActualizarPacientes`) para Usuaria y Superusuario, con sus políticas web | `MatrizDePermisosTests` (incluye su prueba de completitud) | 29 |
| ✅ | Reglas de la ficha: campos, límites, nacimiento, tutor, metadatos, versión, sin cambios y ámbitos | `ReglasDePacienteTests` | 65 |
| ✅ | Clave de nombre normalizada (acentos, formas Unicode, espacios, independiente de la cultura) | `NormalizacionDeNombresTests` | 18 |
| ✅ | Teléfono: formatos aceptados y rechazados, y lo que se guarda | `ReglasDeTelefonoTests` | 25 |
| ✅ | Edad, mayoría de edad, 29 de febrero y fecha de Hermosillo frente a UTC | `ReglasDeEdadTests` | 15 |
| ✅ | Reglas compartidas de nombre/correo: cuentas conserva 150 y pacientes 200 | `ReglasDeNombreYCorreoTests` y las pruebas de cuentas existentes | 14 |
| ✅ | Servicio: permisos antes de leer, resultados del alta, parecidos, reenvío, carreras, búsqueda, una sola lectura, conflicto, bajas y fallos de auditoría o de la base | `ServicioPacientesTests` | 82 |
| ✅ | Guardado y evento en una transacción, carreras del mismo alta contra la clave primaria real, conflicto de versión entre dos personas, ámbitos que se quitan, una sola consulta SQL a `Paciente`, comodines literales, restricciones y permisos de la base | `PacientesServidorTests` (PostgreSQL desechable) | 31 |
| ✅ | Migración desde datos anteriores: correspondencia de Contacto, clave igual a la de Domain, subir, bajar y volver a subir, permisos y datos intactos, cancelación previa | `MigracionDeDatosDePacienteTests` (PostgreSQL desechable) | 7 |

`PacientesServidorTests` y `MigracionDeDatosDePacienteTests` son verificaciones de Miguel de lo que pide la propuesta (secciones 3.5 y 3.7: consulta SQL única, migración demostrada con muestras, carreras). Según AGENTS §2.6 y la propuesta, el QA de integración con Testcontainers es de Lucía; estas pruebas no lo sustituyen y su inclusión en el PR queda a decisión de Lucía.

## 2. Resultado de esta ejecución

| Estado | Comprobación | Resultado |
| --- | --- | --- |
| ✅ | Compilación Release de la solución, advertencias como errores | 0 advertencias y 0 errores. |
| ✅ | `dotnet ef migrations has-pending-model-changes` | Sin cambios pendientes del modelo. |
| ✅ | Unitarias | 624 correctas, 0 fallidas, 0 omitidas (antes de B02: 422). |
| ✅ | Integración completa con dos hilos | 367 correctas, 0 fallidas, 0 omitidas, 2 min 27 s (antes de B02: 329; 38 casos nuevos). |
| ✅ | Comprobación de que la prueba de migración no es vacía | Se quitó a propósito el recorte del espacio de no separación en el SQL: la prueba falló con la muestra afectada; se restauró la migración. |


En una primera ejecución completa fallaron 4 casos de pruebas existentes de Lucía, por cambios esperados del propio B02: la lista de permisos explícitos de Usuaria y Superusuario (2 casos) y el conteo de migraciones aplicadas (2 casos, de 3 a 4). Se actualizaron (sección 5) y la ejecución final es la de la tabla.

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
| `Persona@Ficticio.TEST`, con espacios, tabuladores o espacios de no separación a los lados | `persona@ficticio.test` | nulo |
| ` +52 (644) 123-4567 ` | nulo | `+526441234567` |
| `644-123-4567` | nulo | `6441234567` |
| `sin@dominio`, `a@b..c`, un correo con un carácter invisible dentro | nulo | nulo |
| `llamar al 6441234567`, `correo a@b.c o tel 6441234567`, `123456`, `1234567890123456` | nulo | nulo |

**Bajada:** recrea `Contacto` (`varchar(200)`) con el correo si existe y mide hasta 200 caracteres; si no, con el teléfono; si no, nulo. Si había ambos, se pierde el teléfono. Se pierden también nacimiento, escuela, grupo, tutor y clave de nombre. **Un correo de más de 200 caracteres no se trunca** (un correo cortado sería otro dato): se usa el teléfono o queda nulo. La prueba comprueba estos casos.

**Qué demuestra la prueba:** con 40 contactos y 24 nombres de muestra sembrados en el esquema anterior, la clave y la conversión que calcula el SQL son iguales a las de Domain; subir, bajar y volver a subir da el mismo resultado esperado; una huella de identificadores, nombre, condición, versión, metadatos y ámbitos, y la lista de permisos de la aplicación, no cambian; y la clave de nuevas escrituras de Domain coincide con la que recalcula el SQL.

## 4. Puntos técnicos para Lucía

Ninguno cambia el alcance ni las reglas aprobadas; se informan para que decida si algo le parece mal.

| Estado | Punto | Qué se hizo |
| --- | --- | --- |
| ⏳ | Restricción de la clave de nombre | La base exige que `NombreNormalizado` tenga la forma de una clave ya normalizada (no vacía, sin A–Z, sin marcas combinantes, sin espacios dobles ni en los extremos), pero **no** que sea igual a lo que recalcularía PostgreSQL. .NET y PostgreSQL pueden diferir en caracteres exóticos según su versión de Unicode y un nombre válido no debe fallar por eso. La igualdad se demuestra con muestras en la prueba de migración. |
| ⏳ | SQL frente a Domain en contactos | La clase `[[:space:]]` de PostgreSQL no recorta el espacio de no separación en esta configuración; se agregó una clase explícita con los espacios que recorta `string.Trim()`. Los caracteres de formato invisibles se cubren en el plano básico de Unicode; los de planos superiores no. `lower()` de PostgreSQL y `ToLowerInvariant` coinciden con las muestras, incluidas letras acentuadas. Solo afecta a datos de prueba anteriores. |
| ⏳ | 29 de febrero | En años no bisiestos quien nació un 29 de febrero cumple el 1 de marzo (no se le trata como adulto un día antes). Es una elección de Miguel; la propuesta pide cubrir el caso pero no fija el día. |
| ⏳ | Contactos de más de 200 caracteres | La columna anterior no admitía más; el límite de 254 del correo nuevo solo se ejerce en escrituras nuevas y en la bajada. |
| ✅ | Reenvío frente a la colisión de la clave primaria | Solo la excepción de la clave primaria de `Paciente` se traduce a «ya existe esa ficha»; un fallo de auditoría u otra restricción no se convierte en alta repetida. La lectura que decide `YaRegistrado` no rastrea entidades, para no leer la ficha candidata que pudo quedar en el contexto. |
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

## 6. Límites de esta evidencia

- Solo datos ficticios, sin nube ni gastos. No se probaron páginas, formularios ni navegador.
- El resultado de la CI, la revisión de otra persona y la aprobación de Lucía son pendientes de esta entrega.
- Las confirmaciones de producto de Dulce (propuesta 3.8) siguen pendientes; los valores son provisionales para datos de prueba.
- Los casos de este documento no se suman a la evidencia histórica de B00/B01.
bio |
| --- | --- |
| `BajaPacienteTests`, `HorasDockerTests`, `RecuperacionTests`, `ConsultaAuditoriaRevision`, `ErroresRegistrosRevision` | La ficha se registra con la ayuda nueva `PacientesDePrueba.Adulto(...)`; el contacto rastreable pasa a ser el correo. `ConsultaAuditoriaRevision` y `ErroresRegistrosRevision` comparan `Correo` en lugar de `Contacto`. |
| `RevisionDatosTests` | Registro y dos ediciones con la ayuda nueva; las fichas de las ediciones se leen con sus ámbitos porque `Actualizar` ahora los compara. El conteo de migraciones aplicadas pasa de 3 a 4. |
| `RecuperacionTests` | El conteo de migraciones aplicadas pasa de 3 a 4 en la base restaurada. |
| `PermisosUsuariosTests` | La lista de permisos explícitos de cada rol incluye los tres nuevos. |
| `CoordinadorDeBajaDePacienteTests`, `MatrizDePermisosTests` (unitarias) | Firma nueva de registrar; filas y casos de los permisos nuevos. |
| `RepositorioPacientesFalso` (doble) | Implementa el contrato ampliado, con contadores de lecturas. |

Nuevo: `tests/TherapEase.IntegrationTests/PacientesDePrueba.cs`, que entrega datos válidos de una persona adulta con nacimiento fijo.

## 6. Límites de esta evidencia

- Solo datos ficticios, sin nube ni gastos. No se probaron páginas, formularios ni navegador.
- El resultado de la CI, la revisión de otra persona y la aprobación de Lucía son pendientes de esta entrega.
- Las confirmaciones de producto de Dulce (propuesta 3.8) siguen pendientes; los valores son provisionales para datos de prueba.
- Los casos de este documento no se suman a la evidencia histórica de B00/B01.
