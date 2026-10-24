# TherapEase — B01 · Pruebas de permisos

**Estado al 20-oct-2026:** entrega aprobada por Lucía, revisada por Miguel y fusionada en PR #8 (`7a73e0a`); CI correcta. Las pruebas finales siguen fechadas el 19-oct; no se ejecutaron de nuevo por actualizar el informe.

Se comprueba quién puede leer o modificar usuarios con servicios reales y PostgreSQL desechable. Son **34 casos nuevos**; se repitieron 16 de integración y 44 unitarias existentes. Solo datos ficticios, sin páginas nuevas ni cambios de producción.

## 1. Qué se comprueba

| Casos nuevos | Qué deben demostrar |
| --- | --- |
| 28 rechazos | Listar, consultar ficha, crear, editar datos, cambiar rol, desactivar y restablecer. Cada operación se deniega sin sesión, con rol Usuaria, con Usuaria temporal y con Superusuario temporal. Respuesta sin datos, mensaje de falta de permiso y usuarios/roles/auditoría sin cambios. |
| 4 combinaciones de rol y temporal | Sin temporal: Superusuario tiene los tres permisos explícitos; Usuaria solo `DarDeBajaPacientes`. Con temporal: ninguno de esos tres. Las reglas web permiten el cambio propio y rechazan las funciones operativas con temporal. Permisos numéricos desconocidos también se deniegan. |
| 1 ficha autorizada | Superusuario con sesión válida puede leer el correo en la ficha; la lista no lo expone. Las consultas no escriben. |
| 1 consulta propia sin sesión | Respuesta sin permiso y sin datos; no escribe cambios. |

Las 16 existentes cubren cambio de rol, desactivación, temporal, administración denegada, datos propios y siete solicitudes HTTPS de Q03: seis rechazos y una consulta autorizada. Las unitarias de Miguel comprueban la tabla explícita, la autorización y su conexión con las reglas web. Se reutilizan; no son nuevas por repetirse.

Referencias: HU01–HU03/HU15; CU01, CU02 y CU11; Q01/Q02/Q19; L-CU02-03/04/06 y S-11/S-12, solo su parte de servidor. La decisión del permiso de baja se consulta sin ejecutar bajas de pacientes.

## 2. Versión y resultados

Repetición final del 19-oct: base local `bbf7aed`, refactorización aprobada, con ajustes autorizados de Contraseñas. Main `922a4e3`, verificado con fetch y merge, ya actualizado. Sin reset ni push.

| Ejecución final | Correctas | Fallidas | Omitidas | Duración informada |
| --- | --- | --- | --- | --- |
| Integración | **50** | 0 | 0 | 21 s |
| Unitarias existentes | **44** | 0 | 0 | 79 ms |

Release, .NET SDK 10.0.402, PostgreSQL 17/Testcontainers e Identity reales; mismas dependencias. La preparación HTTP usa HTTPS local real. Las solicitudes de administración usan `SolicitudIdentidad`, con autenticación real dentro de la prueba; no son solicitudes a formularios ni recorridos de navegador.

Evidencia: `permisos-integracion.trx`, `permisos-unitarias.trx` y `manifest-final.json`. Identifican la versión, fuentes, filtros, horarios y huellas. Pruebas comentadas según la skill del proyecto.

## 3. Cómo repetirlo

Desde la raíz del repositorio, con Docker funcionando. Ejecutar un proyecto después del otro para que no compitan por los mismos archivos de compilación.

```powershell
dotnet test tests/TherapEase.IntegrationTests/TherapEase.IntegrationTests.csproj -c Release --filter 'FullyQualifiedName~PermisosUsuariosTests|FullyQualifiedName~Cambio_Rol_Revoca_Cookie_Anterior_Y_Nueva_Sesion_Usa_Permiso_Actual|FullyQualifiedName~Desactivacion_Revoca_Cookie_Y_No_Admite_Acceso|FullyQualifiedName~Temporal_Solo_Permite_Cambio_Propio_Y_No_Operaciones|FullyQualifiedName~Sin_Permiso_No_Crea_Restablece_Desactiva_Ni_Cambia_Rol|FullyQualifiedName~Datos_Propios_No_Autorizan_Administrar_Otros|FullyQualifiedName~Sesion_Y_Permiso_Actual_Se_Exigen|FullyQualifiedName~Periodo_Incluye_Inicio_Excluye_Fin_Y_Ordena' --logger 'trx;LogFileName=permisos-integracion.trx' --results-directory TestResults/permisos

dotnet test tests/TherapEase.UnitTests/TherapEase.UnitTests.csproj -c Release --filter 'FullyQualifiedName~TherapEase.UnitTests.Application.Identidad.AutorizacionTests|FullyQualifiedName~TherapEase.UnitTests.Domain.Identidad.MatrizDePermisosTests|FullyQualifiedName~TherapEase.UnitTests.Web.ManejadorDeRequisitoDePermisoTests' --logger 'trx;LogFileName=permisos-unitarias.trx' --results-directory TestResults/permisos
```

La ejecución registrada usó además `--no-restore` y una ubicación separada para la evidencia de Q03. No se añadieron dependencias.

## 4. Qué falta

- ✅ Auditoría y aprobación de Lucía recibidas. Se ajustaron tres encabezados de 95 a 94 caracteres; solo comentarios. Las 50 de integración y 44 unitarias se repitieron el 19-oct junto con las otras entregas. Commit local autorizado.
- ✅ Miguel aprobó `95c9425`; [PR #8](https://github.com/LuciAguilar/Therapease/pull/8) fusionado y [CI](https://github.com/LuciAguilar/Therapease/actions/runs/39512430706) correcta.
- Formularios, todas las rutas, campos adicionales, navegación y navegador: esperan PageModel/páginas de Dulce. No se completan S-11/S-12 por estas pruebas de servicios.
- Las cuatro entregas tienen aprobación de Lucía, revisión humana de Miguel y CI correcta. Las pruebas de páginas/navegador siguen pendientes. B01/B00/LCA/R04 siguen abiertos; sin datos reales, nube ni gastos.

La publicación autorizada ya se realizó en PR #8. Su excepción de push forzado está consumida; no autoriza otro push.

✅ Publicación y revisión: [PR #8](https://github.com/LuciAguilar/Therapease/pull/8), fusión `7a73e0a`; [CI correcta](https://github.com/LuciAguilar/Therapease/actions/runs/39512430706).
