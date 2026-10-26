# TherapEase — B01 · Pruebas de sesiones

**Estado al 20-oct-2026:** entrega aprobada por Lucía, revisada por Miguel y fusionada en PR #8 (`7a73e0a`); CI correcta. Las pruebas finales siguen fechadas el 19-oct; no se ejecutaron de nuevo por actualizar el informe.

Se comprueba cuándo vence una sesión, cómo se renueva y cuándo debe dejar de servir. Solo datos ficticios; Identity y PostgreSQL reales, con HTTPS local en los casos indicados. No se cambió producción ni se añadieron paquetes.

## 1. Casos nuevos

| Cantidad | Qué comprueban |
| --- | --- |
| 2 | Sin recibir otra cookie: válida a los 29 min 59 s y vencida a los 30 min 1 s. No se afirma el resultado en el instante exacto de treinta minutos. |
| 1 | Salir revoca las dos sesiones propias y conserva la de otra persona. |
| 1 | Otra instancia de TherapEase acepta la cookie; una configuración con otro nombre de protección la rechaza, aun usando la misma base ficticia. |
| 1 | HTTPS emite una cookie renovada con treinta minutos, misma identidad y sin conservación persistente. Con reloj de prueba, la renovada sigue válida después de vencer la original. |
| 3 | Tras renovar por HTTPS, salir, restablecer o cambiar la contraseña propia invalida original y renovada: Q03 responde 401 sin datos. El cambio propio conserva solo la cookie nueva. La sesión de otra persona sigue válida. |

Se repitieron **16 existentes**: trece de Identity —vencimiento, cookies alteradas, claves compartidas, revocación y cuatro fallos de auditoría— y tres de HTTPS —atributos de la cookie, selección para HTTP y consulta sin sesión—. No se vuelven a contar como desarrollo nuevo.

Referencias: HU01–HU03/HU15; CU01/CU02/CU11; Q02/Q19; L-CU01-02/03 y L-CU02-03/04/06, solo el alcance indicado. Los casos de cambio o restablecimiento comprueban revocación; no completan la entrega de Contraseñas.

## 2. Resultado final y versión

| Correctas | Fallidas | Omitidas | Duración informada |
| --- | --- | --- | --- |
| **24** —8 nuevas y 16 repetidas— | **0** | **0** | **12 s** |

Release; .NET SDK 10.0.402, PostgreSQL 17/Testcontainers e Identity reales. Base local `bbf7aed`, refactorización aprobada, con ajustes autorizados de Contraseñas. Fetch y merge de main realizados antes de empezar; main `922a4e3`, ya actualizado. Sin reset ni push.

Evidencia final: `sesiones.trx` y `manifest-final.json`, con horarios, huellas, filtros y fuentes. Las cookies permanecen en memoria; los mensajes de los casos nuevos no muestran sus valores. Pruebas comentadas según la skill del proyecto, encabezados de 94 caracteres.

## 3. Código y repetición

Archivos de `tests/TherapEase.IntegrationTests/`: `SesionesServidorTests.cs`, `SesionesHttpsTests.cs` y `SesionesHttpsRevision.cs`. La ayuda HTTPS reutiliza el servidor de QA y le asigna una carpeta propia; no crea rutas de prueba en la aplicación ni altera la confianza de Windows.

Con Docker disponible, este comando repite las ocho nuevas:

```powershell
dotnet test tests/TherapEase.IntegrationTests/TherapEase.IntegrationTests.csproj -c Release --filter 'FullyQualifiedName~SesionesServidorTests|FullyQualifiedName~SesionesHttpsTests' --logger 'trx;LogFileName=sesiones-nuevas.trx' --results-directory TestResults/sesiones
```

Para repetir las 24, usar la propiedad `filtro` del manifest de la ejecución en `--filter`. La ejecución registrada usó `--no-restore` y guardó la evidencia HTTPS en una carpeta separada.

## 4. Pendientes

Auditoría de Sesiones resuelta y aprobación de Lucía recibida el 19-oct. Las 24 se repitieron el 19-oct con los mismos casos y las ayudas finales. Miguel aprobó PR #8, su CI terminó correctamente y Lucía lo fusionó. El navegador y las páginas/PageModel de Dulce siguen pendientes: HTTP y clientes de prueba no demuestran su recorrido real, cerrar el navegador o volver atrás. B01/B00/LCA/R04 continúan abiertos; sin nube ni gastos. Contraseñas conserva su entrega.

✅ Publicación y revisión: [PR #8](https://github.com/LuciAguilar/Therapease/pull/8), fusión `7a73e0a`; [CI correcta](https://github.com/LuciAguilar/Therapease/actions/runs/39512430706).
