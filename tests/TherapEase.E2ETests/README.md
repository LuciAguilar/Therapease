# Pruebas de navegador de Lucía

El proyecto contiene 165 casos: 54 de acceso y cuentas (B01), 23 de pacientes (B02), 32 de agenda/citas (B03), 10 regresiones de pago de Dulce y 46 casos propios B04 de Lucía (28 HTTP, 7 de interfaz, 9 de avisos y 2 Q16). Los archivos forman la clase parcial AccesoSesionesYPermisosTests para compartir la preparación y los filtros de xUnit. Los 46 propios se ejecutaron juntos sobre main 7578449; CI y revisión humana de esta entrega pendientes.

| Estado | Categoría | Archivo |
| --- | --- | --- |
| ✅ integrada | Acceso | [AccesoTests.cs](Acceso/AccesoTests.cs) |
| ✅ integrada | Permisos | [PermisosTests.cs](Permisos/PermisosTests.cs) |
| ✅ integrada | Sesiones | [SesionesTests.cs](Sesiones/SesionesTests.cs) |
| ✅ integrada | Contraseñas | [ContrasenasTests.cs](Contrasenas/ContrasenasTests.cs) |
| ✅ integrada | Formularios, cuentas y resultados inciertos | [FormulariosTests.cs](Formularios/FormulariosTests.cs) |
| ✅ integrada | Recursos, cabeceras y error seguro | [RecursosYErroresTests.cs](Seguridad/RecursosYErroresTests.cs) |
| ✅ integrada | Pacientes | [PacientesPaginasTests.cs](Pacientes/PacientesPaginasTests.cs) |
| ✅ integrada y revisada | Agenda y citas | [AgendaPaginasTests.cs](Citas/AgendaPaginasTests.cs) |
| ✅ integrada | Regresiones de pago de Dulce (10) | [PagoDeCitaPaginasTests.cs](Citas/PagoDeCitaPaginasTests.cs) |
| ✅ ejecutada / ⏳ revisión humana | HTTP y ayudas de pago (28) | [PagoHttpRevisionTests.cs](Citas/PagoHttpRevisionTests.cs) y [PagoRevision.cs](Citas/PagoRevision.cs) |
| ✅ ejecutada / ⏳ revisión humana | Interfaz de pago (7) | [PagoInterfazRevisionTests.cs](Citas/PagoInterfazRevisionTests.cs) |
| ✅ ejecutada / ⏳ revisión humana | Avisos (9) y Q16 (2) | [AvisosDePagoRevisionTests.cs](Citas/AvisosDePagoRevisionTests.cs) |
| ✅ compartida | Preparación | [NavegadorRevision.cs](Apoyo/NavegadorRevision.cs) y [ayudas](Apoyo/AccesoSesionesYPermisosTests.cs) |

Ejecutar desde la raíz:

```powershell
pwsh scripts/qa/probar-paginas.ps1 -Canal chrome
```

El script compila y publica Web en Production, usa HTTPS y PostgreSQL ficticio desechable con Docker. En GitHub se usa Chromium; localmente puede usarse Chrome o Edge (-Canal msedge). No se graban trazas de identidad hasta la decisión de B04; los TRX y los diagnósticos omiten formularios, cookies y contraseñas.

Los casos de citas retienen y cortan peticiones concretas: el doble clic no depende de hacerlo en el instante adecuado. Comprobar una reserva abre su detalle sin escribir; reenviar conserva los datos y el identificador. Cada variante conserva su evidencia propia.

[QA B01](../../docs/testing/TherapEase-09-B01-QA-paginas.md) · [QA B03](../../docs/testing/TherapEase-09-B03-QA-paginas.md). B01 y B02 ya tienen revisión humana; el QA nuevo B03 conserva Claude APROBADO v2, 18-nov, y fue revisado por Miguel y fusionado por Lucía en #24, main 9d0f3a0. CI main correcta y B03 cerrado por Lucía el 20-nov.

[QA B04](../../docs/testing/TherapEase-09-B04-QA.md): 46 casos propios de este proyecto y 25 de servidor en integración. Claude aprobó el QA; los dos añadidos autorizados se comprobaron sin otra auditoría requerida. Pendientes revisión humana, CI y aprobación final de Lucía. No se habilitan trazas.
