# TherapEase — B01 · Duración y minutos de CI

**25-oct-2026 · APROBADO v2 por Claude; publicación autorizada por Lucía.** Corte de la medición: **09:12 de Hermosillo del 25-oct**. Incluye nueve ejecuciones de PR y tres push a main, desde el 17-oct. No se lanzaron pruebas ni se cambió el flujo para medirlas. Este registro y A-06 ✅ del [plan de pruebas](TherapEase-09-plan-de-pruebas.md) se publican juntos.

## 1. Qué significan los tiempos

La ventana del flujo va desde su inicio registrado hasta su última actualización al terminar. El tiempo sumado cuenta la duración de cada trabajo ejecutado; puede superar la ventana porque varias máquinas trabajan a la vez. Una espera entre publicaciones no se suma.

**Repositorio público; máquinas estándar de GitHub, Linux `ubuntu-24.04`: 0 minutos facturables aplicables.** No es una lectura de la factura ni incluye almacenamiento. [Regla de GitHub](https://docs.github.com/en/billing/concepts/product-billing/github-actions).

## 2. Ejecuciones consultadas

✅ significa **dato consultado**, también si el resultado fue fallido. El resultado se indica en su propia columna. Horas de Hermosillo (UTC−7).

| Estado | PR / ejecución | SHA | Inicio en Hermosillo | Ventana del flujo | Tiempo sumado de trabajos | Resultado |
| --- | --- | --- | --- | --- | --- | --- |
| ✅ | [#7 · 39076736098](https://github.com/LuciAguilar/Therapease/actions/runs/39076736098) | `ffd2b5c` | 2026-10-17 15:51:20 | 2 min 55 s | 3 min 59 s | Fallida |
| ✅ | [#7 · 39151476673](https://github.com/LuciAguilar/Therapease/actions/runs/39151476673) | `b1aed8a` | 2026-10-18 10:51:14 | 2 min 51 s | 3 min 55 s | Correcta |
| ✅ | [#7 · 39152597181](https://github.com/LuciAguilar/Therapease/actions/runs/39152597181) | `a2047ec` | 2026-10-18 11:05:58 | 2 min 48 s | 3 min 49 s | Correcta |
| ✅ | [#8 · 39367792481](https://github.com/LuciAguilar/Therapease/actions/runs/39367792481) | `95c9425` | 2026-10-19 21:06:26 | 3 min 30 s | 4 min 22 s | Correcta |
| ✅ | [#8 · 39506871820](https://github.com/LuciAguilar/Therapease/actions/runs/39506871820) | `bdca370` | 2026-10-20 15:16:21 | 0 min 41 s | 1 min 37 s | Fallida |
| ✅ | [#8 · 39512430706](https://github.com/LuciAguilar/Therapease/actions/runs/39512430706) | `95c9425` | 2026-10-20 16:03:28 | 3 min 15 s | 4 min 23 s | Correcta |
| ✅ | [#9 · 39905673597](https://github.com/LuciAguilar/Therapease/actions/runs/39905673597) | `cb7da59` | 2026-10-22 22:25:28 | 2 min 46 s | 3 min 51 s | Correcta |
| ✅ | [#10 · 40138449133](https://github.com/LuciAguilar/Therapease/actions/runs/40138449133) | `202d70a` | 2026-10-24 15:24:52 | 3 min 25 s | 4 min 37 s | Correcta |
| ✅ | [#10 · 40160408560](https://github.com/LuciAguilar/Therapease/actions/runs/40160408560) | `350b593` | 2026-10-24 21:02:16 | 3 min 31 s | 4 min 38 s | Correcta |

La integración de **39506871820** quedó omitida: no cuenta como prueba ejecutada. Las versiones finales consultadas de los cuatro PR terminaron correctamente.

Los tres push a main también consumen tiempo y se cuentan aparte para evitar mezclarlos con los PR:

| Estado | Ejecución en main | SHA | Inicio en Hermosillo | Ventana del flujo | Tiempo sumado de trabajos | Resultado |
| --- | --- | --- | --- | --- | --- | --- |
| ✅ | [39170353696](https://github.com/LuciAguilar/Therapease/actions/runs/39170353696) | `922a4e3` | 2026-10-18 15:00:55 | 2 min 39 s | 3 min 56 s | Correcta |
| ✅ | [39521624805](https://github.com/LuciAguilar/Therapease/actions/runs/39521624805) | `7a73e0a` | 2026-10-20 17:29:27 | 3 min 14 s | 4 min 18 s | Correcta |
| ✅ | [40160154004](https://github.com/LuciAguilar/Therapease/actions/runs/40160154004) | `05fc3c7` | 2026-10-24 20:58:12 | 3 min 01 s | 4 min 07 s | Correcta |

## 3. Duración de cada trabajo

| Ejecución | Compilación y unitarias | Paquetes y controles | Imagen | Integración |
| --- | --- | --- | --- | --- |
| 39076736098 | 0 min 40 s | 0 min 39 s | 0 min 30 s | 2 min 10 s · fallida |
| 39151476673 | 0 min 36 s | 0 min 36 s | 0 min 34 s | 2 min 09 s |
| 39152597181 | 0 min 34 s | 0 min 37 s | 0 min 30 s | 2 min 08 s |
| 39367792481 | 0 min 38 s | 0 min 33 s | 0 min 25 s | 2 min 46 s |
| 39506871820 | 0 min 29 s · fallida | 0 min 38 s | 0 min 30 s | No ejecutada |
| 39512430706 | 0 min 30 s | 0 min 40 s | 0 min 34 s | 2 min 39 s |
| 39905673597 | 0 min 42 s | 0 min 39 s | 0 min 31 s | 1 min 59 s |
| 40138449133 | 0 min 30 s | 0 min 45 s | 0 min 32 s | 2 min 50 s |
| 40160408560 | 0 min 42 s | 0 min 44 s | 0 min 29 s | 2 min 43 s |
| 39170353696 | 0 min 43 s | 0 min 44 s | 0 min 37 s | 1 min 52 s |
| 39521624805 | 0 min 29 s | 0 min 40 s | 0 min 31 s | 2 min 38 s |
| 40160154004 | 0 min 41 s | 0 min 33 s | 0 min 39 s | 2 min 14 s |

## 4. Minutos usados

Incluye intentos fallidos; no suma el trabajo omitido. Los minutos se calculan desde los segundos originales, antes de redondear cada fila. [Duración y minutos facturables](https://docs.github.com/en/actions/how-tos/monitor-workflows/view-job-execution-time).

| Origen | Ejecuciones | Tiempo sumado de trabajos | Minutos de ejecución | Facturables aplicables |
| --- | --- | --- | --- | --- |
| #7 | 3 | 11 min 43 s | 11,72 | 0 |
| #8 | 3 | 10 min 22 s | 10,37 | 0 |
| #9 | 1 | 3 min 51 s | 3,85 | 0 |
| #10 | 2 | 9 min 15 s | 9,25 | 0 |
| Total PR | 9 | 35 min 11 s | 35,18 | 0 |
| Push a main | 3 | 12 min 21 s | 12,35 | 0 |
| **Total registrado** | **12** | **47 min 32 s** | **47,53** | **0** |

Son **2.852 segundos**: 48 trabajos registrados, 47 ejecutados y uno omitido. El corte anterior tenía ocho ejecuciones de PR y 1.833 segundos (30,55 minutos); la ampliación añade 278 segundos del PR #10 y 741 de main. No es un total mensual de toda la cuenta. Si cambia la visibilidad o la máquina utilizada, se revisa de nuevo la regla de cobro.

## 5. Incidente de rama y orden para seguir

En **39506871820**, `bdca370` es una fusión hecha por `GalloNav` de `e918a3d` (versión retirada) con `95c9425`. Se publicó en `revision/etapa09` el **20-oct a las 15:16**. A las **16:03**, `GalloNav` devolvió la rama a `95c9425` mediante un push no lineal, seguido de **39512430706**. [Fusión intermedia](https://github.com/LuciAguilar/Therapease/commit/bdca3706310144f56376d3c9b2be8b5040837638) · [Historial del PR #8](https://github.com/LuciAguilar/Therapease/pull/8).

El PR #8 entró con `95c9425`; este incidente no cambió lo fusionado. Se distingue de la excepción autorizada del **19-oct**. Lucía confirmó el 25-oct que había aprobado la intervención histórica y pidió impedir que se repita.

El orden vigente es:

1. Miguel termina sus ajustes y Lucía fusiona **#9 con squash**.
2. En `revision/etapa09`: `git fetch` y `git merge origin/main`, sin reset ni pérdida de cambios. Antes de resolver diferencias en README, plan B01 y plan de pruebas, proponer a Lucía una combinación que conserve decisiones y resultados aprobados de ambas versiones.
3. Añadir los diez casos al **mismo #10** y repetirlos.
4. Miguel o Dulce revisa las pruebas; Lucía da la aprobación final.
5. Fusionar **#10 con Create a merge commit**.

Hoy #10 solo contiene documentos y sigue como borrador: Miguel no necesita revisarlos. Sus diez casos aún no están publicados y esperan el backend del #9. AGENTS ya llegó a main en `05fc3c7`; #10 no debe fusionarse primero. Las páginas y el navegador siguen pendientes de Dulce. B01/B00/LCA/R04 permanecen abiertos; B02 se rige por la autorización de Lucía del 25-oct en AGENTS §3.
