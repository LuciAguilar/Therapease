# B03 — QA de páginas de agenda y citas

**Preparación del 18-nov-2026.** Autoría de Lucía con Codex. Usa las páginas de Dulce de #23, ya integradas en main `7d1d1ee`, y el servidor B03 existente. Claude APROBADO v2 sobre los 32 casos y la organización de integración. La revisión humana de este QA corresponde a Miguel o Dulce; Lucía decide la fusión.

## 1. Alcance y trazabilidad

Reutiliza los casos CU05/CU06 del [plan de pruebas](TherapEase-09-plan-de-pruebas.md) y el [plan B03](../planning/TherapEase-etapa09-B03.md). No duplica sus tablas.

| Alcance | Qué se comprueba |
| --- | --- |
| Consulta y acceso | Agenda, vacío, periodos límite, detalle, sesión, temporal y horas de Hermosillo con dos zonas del navegador. |
| Captura y reserva | Ambos roles, selección de paciente/ámbito, horario, captura conservada y una cita con un evento. |
| Protección del formulario | Antifalsificación ausente/ajena y nombre mostrado como texto. |
| Respuesta incierta | Cortes antes/después de guardar, consulta sin escribir y reenvío manual del mismo número. |
| Envíos repetidos | Doble POST y doble clic con petición retenida; una cita y un evento. |
| Estado de pantalla | Acciones ocultas durante el envío, selectores, Escape y anchuras de teléfono/escritorio. |
| Paciente desactualizado | Baja ficticia después de abrir la captura, antes de enviarla: no se agenda. La concurrencia dentro del servicio conserva los 37 casos ya integrados en #19. |

Son 32 casos en [AgendaPaginasTests.cs](../../tests/TherapEase.E2ETests/Citas/AgendaPaginasTests.cs). Los cortes y pausas son preparados; no dependen de coincidir por suerte. Comprobar conserva el formulario y no crea registros; reenviar conserva actor, datos y número de reserva.

## 2. Herramientas y resultado local

| Comprobación | Con qué se ejecutó | Resultado |
| --- | --- | --- |
| Compilación | .NET 10, Release, advertencias como error | 0 errores y 0 advertencias. |
| Unitarias | xUnit | 965/965. |
| Integración | xUnit, Docker/Testcontainers y PostgreSQL desechable | 574/574. |
| Navegador | Playwright .NET y Chrome sin ventana, HTTPS, Web en Production | 109/109: 54 B01, 23 B02 y 32 B03. |
| Organización | SHA-256 y detección R100 de Git | Cuatro archivos sin cambios de contenido; cuatro movimientos y su README en `784c38d`. |

Los resultados corresponden a la preparación tras incorporar main `7d1d1ee`. No sumar repeticiones. CI y revisión humana se comprueban en este PR; el navegador de CI es Chromium. Los TRX de CI identifican la ejecución real.

## 3. Cómo repetirlo

Con Docker y el navegador de Playwright preparados, desde la raíz:

```powershell
dotnet build TherapEase.sln -c Release -warnaserror
dotnet test tests/TherapEase.UnitTests -c Release --no-build
dotnet test tests/TherapEase.IntegrationTests -c Release --no-build
pwsh scripts/qa/probar-paginas.ps1 -Canal chrome
```

Para Chromium, usar `-Canal chromium`. El script vuelve a publicar Web y escribe los resultados en TestResults/paginas. No basta editar un CSS ya publicado: MapStaticAssets también sirve comprimidos `.br`/`.gz` generados al publicar.

Cada variante escribe `corte-{despuesDeGuardar}-comprobar-{comprobar}.json`, con hechos técnicos sin formularios, contraseñas ni cookies. Las trazas de identidad siguen deshabilitadas hasta la decisión de B04.

## 4. Revisión y límites

Claude APROBADO v2, 18-nov, incluye los dos casos de Comprobar y los cuatro movimientos R100. Se aplicó su mejora opcional para separar los nombres de evidencia; la lógica permanece igual y se repitió el conjunto completo.

La integración queda organizada en Entorno, Citas y Pacientes; se conservan clases, métodos, namespaces y autoría. Ver el [índice de integración](../../tests/TherapEase.IntegrationTests/README.md) y el [índice E2E](../../tests/TherapEase.E2ETests/README.md).

La revisión humana del QA y la decisión de fusión son independientes de las de #23. Este PR no cierra B03 ni acredita recorrido humano/producto de agenda. Q16 completo y trazas: B04; rendimiento: B08; nuevas bajas/recuperaciones: B06. Solo datos ficticios.
