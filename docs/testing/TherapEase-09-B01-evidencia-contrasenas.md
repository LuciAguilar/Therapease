# TherapEase — B01 · Pruebas de contraseñas

**Estado al 20-oct-2026:** entrega aprobada por Lucía, revisada por Miguel y fusionada en PR #8 (`7a73e0a`); CI correcta. Las pruebas finales siguen fechadas el 19-oct; no se ejecutaron de nuevo por actualizar el informe.

Se comprueba el cambio propio y el restablecimiento con servicios reales y PostgreSQL desechable. Solo datos ficticios. No se modificó producción, paquetes, tablas o reglas de acceso.

## 1. Casos nuevos

| Cantidad | Qué comprueban |
| --- | --- |
| 12 | CU11 acepta doce letras minúsculas, mayúsculas o números, sin exigir mezcla. Dos roles, con y sin temporal. Cambia únicamente el usuario de la sesión, revoca sus cookies anteriores, conserva la nueva y la de otro usuario, y registra el cambio. |
| 6 | Contraseña nueva ausente, vacía o de once caracteres: conserva la contraseña protegida, la marca de seguridad y la temporal pendiente; no emite una cookie nueva ni registra un cambio de contraseña. |
| 4 | Cambio ordinario en ambos roles: omitir la contraseña actual no modifica usuarios, roles o auditoría. |
| 4 | Sesión ausente, alterada, revocada o de usuario inactivo: CU11 rechaza sin cambios. Conserva las cabeceras que ya haya emitido la validación para borrar cookies rechazadas. |
| 1 | Cinco contraseñas actuales incorrectas en CU11 bloquean quince minutos. Acertar después no cambia la contraseña ni abre sesión mientras siga el bloqueo. |
| 1 | Un nuevo restablecimiento invalida la primera temporal y la sesión abierta con ella; solo sirve la segunda. Hay dos eventos de restablecimiento confirmados, sin sus valores. |

Se repitieron **34 casos existentes de integración**: doce de Identity, dieciséis de interrupciones y seis de auditoría. Cubren también contraseñas protegidas, restricción con temporal, revocación, bloqueo, fallos de guardado y respuesta perdida. Se conservan sus autores; no son desarrollo nuevo.

Referencias: HU03/HU15, CU02/CU11, Q02/Q03/Q04/Q08/Q19 y decisiones de AGENTS §2.2–§2.3. La prueba del bloqueo desde CU11 no acredita los eventos de acceso o bloqueo pendientes del backend.

## 2. Resultado final y versión

| Grupo | Correctas | Fallidas | Omitidas |
| --- | --- | --- | --- |
| Integración —28 nuevas y 34 existentes— | **62** | **0** | **0** |
| Unitarias existentes —cambio propio, generador de temporales y ocultación en ToString— | **16** | **0** | **0** |

Release. Duración informada: integración 25 s; unitarias 66 ms. Base local `bbf7aed5b1aaef13a7d1029bef3ae29e3f9785b1`, refactorización aprobada, sobre main `922a4e3`. Fetch y merge realizados antes de empezar; ya actualizado. Sin reset ni push.

Evidencia final: `contrasenas-integracion.trx`, `contrasenas-unitarias.trx` y `manifest-final.json`, con filtros, horarios y huellas. Los casos nuevos usan etiquetas como parámetros, conservando las temporales generadas, cookies y comparaciones de credenciales en memoria. Pruebas comentadas según la skill del proyecto; encabezados de 94 caracteres.

## 3. Código y repetición

Fuente nueva: [ContrasenasServidorTests.cs](../../tests/TherapEase.IntegrationTests/ContrasenasServidorTests.cs). Usa ayudas simples para verificar contraseñas y leer eventos, intentos y fin del bloqueo. La solicitud y su autenticación siguen visibles en cada prueba.

Con Docker disponible, este comando repite las 28 nuevas:

```powershell
dotnet test tests/TherapEase.IntegrationTests/TherapEase.IntegrationTests.csproj -c Release --filter 'FullyQualifiedName~ContrasenasServidorTests' --logger 'trx;LogFileName=contrasenas-nuevas.trx' --results-directory TestResults/contrasenas
```

Para repetir los 62 y las 16 unitarias, usar los filtros del manifest. La ejecución registrada usó `--no-restore`; `THERAPEASE_EVIDENCIA_Q08` dirige los resúmenes de interrupciones a su carpeta de evidencia.

## 4. Pendientes

Auditoría resuelta y aprobación de Lucía recibida. Se hizo explícita la comprobación de la sesión ya abierta: el bloqueo impide volver a entrar, pero no la cierra. Las cuatro entregas se repitieron con los mismos casos; revisión humana de Miguel y CI del PR #8 completas, con fusión el 20-oct.

Páginas, PageModel y navegador siguen con su trabajo pendiente. No se acredita mostrar la temporal una sola vez, impedir su recuperación con recargar/atrás o la cabecera no-store en formularios. La verificación de identidad y el canal de entrega siguen siendo el procedimiento aprobado. La caducidad adicional de 24 horas se exige antes de datos reales; no se implementa ni se prueba aquí. B01/B00/LCA/R04 siguen abiertos; sin nube ni gastos.

✅ Publicación y revisión: [PR #8](https://github.com/LuciAguilar/Therapease/pull/8), fusión `7a73e0a`; [CI correcta](https://github.com/LuciAguilar/Therapease/actions/runs/39512430706).
