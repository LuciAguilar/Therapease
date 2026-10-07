# TherapEase — B00 · Evidencia de Identity en servidor

**Fecha inicial:** 06-oct-2026. **Actualización:** 07-oct-2026, comentarios y nombres según revisión. **Estado:** revisión humana y aprobación pendientes. Esta entrega aún no está publicada. B00/LCA y R04 siguen abiertos.

## 1. Alcance e identificación

- Base de código: main `8baf8fbc34890d4710c2b78e920f10b8d3e92cb3`, que incorpora los PR #1 y #3. Rama de trabajo existente: `revision/etapa09`.
- Se incorporaron las 11 comprobaciones anteriores y se añadieron 26 casos de Identity: **37 pasan, 0 fallan, 0 omitidos**. Compilación Release del proyecto de integración y sus cuatro referencias sin advertencias en la ejecución final.
- Solo se modifican pruebas y documentación. Backend, PageModel, migraciones, configuración de ejecución y reglas compartidas no se modifican.
- Entorno: Windows, SDK .NET 10.0.401, Docker 29.8.1 con motor Linux, PostgreSQL 17 desechable, Testcontainers.PostgreSql 4.15.0 y xUnit 2.9.3. Una base aislada por caso, usuarios migrador/aplicación separados, datos exclusivamente ficticios y eliminación de contenedores al terminar.
- Publicación y PR de pruebas pendientes. El manifiesto local identifica los archivos y, después del guardado, el commit local de la entrega. La evidencia conserva TRX, huellas SHA-256, consultas y scripts de revisión.

## 2. Resultados

| Comprobación | Resultado y alcance | Referencia |
| --- | --- | --- |
| Contraseña de 12 caracteres sin composición | 12 letras minúsculas aceptadas, 11 rechazadas; nombre duplicado sin distinguir mayúsculas rechazado. Hash verificado por el hasher real de Identity, contraseña incorrecta rechazada. | Q19, S-08, CU11 |
| Acceso rechazado | Inexistente, incorrecta, inactivo y bloqueado reciben el mismo mensaje y ninguna cookie de sesión. No se midió igualdad temporal de respuestas. | Q01, S-02 |
| Bloqueo | Cinco fallos, también concurrentes, dejan bloqueo; contraseña correcta no entra durante él. Fecha de vencimiento acotada a 15 minutos; después de colocarla en el pasado vuelve a evaluarse la contraseña. | S-01, S-03 |
| Temporal | Políticas reales permiten cambio propio y niegan acceso operativo/administración; el cambio retira la condición temporal y renueva la sesión. | CU02, CU11, S-07 parcial |
| Cambio propio | Revoca dos cookies anteriores y admite la nueva; clave anterior deja de funcionar. Actual incorrecta o nueva corta no cambia clave, sello ni auditoría. | Q02, CU11 |
| Salida | Revoca ambas cookies; emite retirada de cookie; repetición sin sesión no falla. | Q02, S-05 en componente |
| Restablecimiento | Conserva el vencimiento del bloqueo, revoca cookie anterior y entrega temporal. La temporal no permite entrar mientras sigue bloqueado; después del vencimiento se exige cambiarla. | CU02, Q02 |
| Roles y desactivación | El cambio de rol cierra toda la sesión anterior, tanto al aumentar como al retirar permisos; se necesita iniciar sesión de nuevo y se usa el rol actual. Desactivación invalida cookie e impide nuevo acceso. | CU02, Q01–Q02 |
| Permiso en servicios | Usuaria y superusuario con temporal no pueden crear, restablecer, desactivar ni cambiar roles; sin escrituras de esos intentos. | S-12, S-07 parcial |
| Último superusuario | Servicio rechaza desactivación y retirada de rol cuando queda uno. Base conserva uno en pruebas de concurrencia de dos bajas, dos retiradas de rol y combinación baja/rol. | CU02, Q19 |
| Cookies y caducidad | Emisor real genera HttpOnly/Secure/SameSite=Lax en contexto HTTPS de componente. Cookie original válido a 29 minutos e inválido a 31 con reloj controlado; configuración de renovación deslizante comprobada. Ausente/manipulado no autentica. | S-04 parcial, S-06, Q19 parcial |
| Claves de sesión | Se guardan en PostgreSQL y otro proveedor de servicios lee la cookie. No se reinició el contenedor de aplicación en este caso. | S-31 parcial |
| Operador local | Comando real crea primer superusuario una vez, temporal solo en salida de consola capturada; segundo alta rechazada. Comando de restablecimiento cambia temporal; claves ausentes de error y auditoría. | Procedimiento aprobado CU02 |
| Atomicidad | Fallo inducido al insertar auditoría revierte alta, restablecimiento, cambio de rol, desactivación y cambio propio. Sesión anterior sigue válida tras revertir; no se confirma el cambio. | Q03, Q08 parcial |
| Datos rastreables | Contraseñas, temporal, hash, cookie y conexión de los casos observados no aparecen en auditoría ni registros capturados. No cubre todavía todos los flujos ni los contactos clínicos. | Q04, S-21/S-24 parciales |
| Persistencia previa | Migraciones reales, auditoría solo inserción, cruce SQL/concurrencia, versión EF y baja/recuperación de paciente sin citas activas pasan de nuevo. | A-07, Q03/Q06/Q07/Q20 parciales |
| Servidor real | Endpoint técnico de auditoría por HTTP: 401 sin sesión/revocada, 403 sin permiso/con temporal, 200 superusuario y 400 instante sin zona. | Q03, Q09 parcial |

Las pruebas de concurrencia de cruces y conservación de superusuario mantienen la primera transacción abierta hasta observar que PostgreSQL bloquea la segunda; luego confirman y contrastan el resultado. Prueban las restricciones de almacenamiento, sin afirmar que cubran todos los intercalados ni el flujo completo de las páginas.

## 3. Secretos y configuración

- Búsqueda acotada de claves privadas, patrones de tokens conocidos y contraseñas literales de conexión: **172 blobs de historial alcanzable, 150 archivos actuales y 8 capas/4,064 archivos de imagen**. No se detectaron credenciales reales de TherapEase en ese alcance.
- Tres coincidencias clasificadas: diez claves privadas de ejemplo que GnuTLS publica en su código, iguales por SHA-256 del contenido DER a sus [vectores oficiales de GnuTLS 3.8.3](https://github.com/gnutls/gnutls/blob/3.8.3/lib/crypto-selftests-pk.c), y dos ejemplos comentados de OpenSSL. No se ignoraron coincidencias sin contrastarlas.
- Imagen histórica `therapease-pr1-e418010:revision`, digest `sha256:b243a14702090d50d8ca1e634f5232f70ae0444c892d849ff79ae7bde8549c1b`, usuario 1654. Las entradas de compilación de aplicación no cambiaron respecto a main; no se reconstruyó ni publicó la imagen. Sus archivos de configuración no contienen conexión ni contraseña.
- `.env.example` contiene credenciales explícitamente ficticias. Secretos y respaldos locales están excluidos de Git; la imagen solo copia proyectos y fuentes de `src`. No se conectó con Neon ni se probó su TLS. La protección contra secretos de GitHub sigue sin verificarse.
- NuGet directo/transitivo del proyecto de integración: consulta del 06-oct sin vulnerabilidades reportadas. No equivale a análisis de todas las vulnerabilidades de la imagen ni auditoría completa de dependencias.

La búsqueda no garantiza ausencia universal de secretos: cubre patrones y referencias locales alcanzables, sin objetos eliminados, cachés o forks. Scripts, JSON y contraste completo están conservados en la evidencia local; no contienen valores encontrados ni credenciales reales.

## 4. Reproducción y revisión

**Pruebas comentadas según el skill del proyecto.** Se conservan las explicaciones revisadas por Claude, con secciones, descripciones de pruebas y espaciado. `ConcurrenciaSqlRevision` y la prueba de datos sensibles sustituyen los nombres con jerga; no cambian las reglas comprobadas.

Reejecución final del 07-oct-2026 con Docker 29.8.1 disponible: **37 pasan, 0 fallan y 0 omitidas**, duración informada por xUnit 14 s. Compilación Release sin advertencias. Resultado local `identidad-comentada-07oct.trx`. Las correcciones menores solicitadas por Claude están aplicadas; la revisión humana y la aprobación final siguen pendientes.

Con SDK .NET 10 y Docker Desktop con contenedores Linux disponible, desde la raíz del repo:

```powershell
dotnet test tests/TherapEase.IntegrationTests/TherapEase.IntegrationTests.csproj -c Release --logger "trx;LogFileName=identidad-b00.trx" --results-directory TestResults/B00
```

No requiere `.env`, PostgreSQL instalado ni conexión externa de base. Testcontainers usa puertos locales asignados y sus credenciales ficticias; puede descargar imágenes si no están disponibles. El servidor HTTP de prueba arranca la aplicación compilada desde la solución del clon, sin rutas nuevas ni referencias a carpetas históricas.

Archivos principales: [casos Identity](../../tests/TherapEase.IntegrationTests/IdentidadServidorTests.cs), [casos complementarios](../../tests/TherapEase.IntegrationTests/IdentidadComplementariaTests.cs), [11 casos incorporados](../../tests/TherapEase.IntegrationTests/RevisionDatosTests.cs), [coordinación de pruebas de concurrencia](../../tests/TherapEase.IntegrationTests/ConcurrenciaSqlRevision.cs).

La revisión humana de Miguel o Dulce y la aprobación final de Lucía siguen pendientes. Este resultado no acredita CI: no se configuró un flujo nuevo. La concreción de la dependencia de pruebas está registrada como nota posterior en ADR-05, sin cambiar la decisión arquitectónica original.

## 5. Pendientes que se conservan

- **Dulce:** PageModel y unitarias; después probar formulario real, envío antifalsificación, cookies/navegador, renovación por actividad, salida y cambio/restablecimiento desde las páginas, redirecciones y caché. Si falla la auditoría, el servicio lanza una excepción y revierte; la página debe mostrar «no se guardó», sin confirmar éxito.
- **Q19:** HTTPS local real, transporte y cabeceras según decisiones pendientes. Una cabecera Secure emitida en componente o una cookie enviado manualmente por HTTP no prueba comunicaciones cifradas.
- **S-31:** reinicio real del contenedor de aplicación con claves en PostgreSQL; la recreación del proveedor solo es evidencia parcial.
- **Q08:** pérdidas de respuesta e interrupciones antes/después del commit y resultado incierto; los fallos de auditoría solo cubren reversión antes del commit.
- **Q10–Q11:** recuperación pendiente, siguiente bloque después de revisar esta entrega.
- **B00:** límites A-01/CU04, restantes casos de datos/horarios, producto, reproducción por otra persona y decisión LCA. CI desde B01; recorrido Q16 completo desde B04. Sin datos reales, nube ni cierre de LCA por este informe.
