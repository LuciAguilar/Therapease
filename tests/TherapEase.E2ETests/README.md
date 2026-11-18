# Pruebas de navegador de Lucía

Los 109 casos se reparten en 54 de acceso y cuentas (B01), 23 de pacientes (B02) y 32 de agenda/citas (B03). Los archivos forman la clase parcial AccesoSesionesYPermisosTests para compartir la preparación y conservar los filtros de xUnit.

| Estado | Categoría | Archivo |
| --- | --- | --- |
| ✅ integrada | Acceso | [AccesoTests.cs](Acceso/AccesoTests.cs) |
| ✅ integrada | Permisos | [PermisosTests.cs](Permisos/PermisosTests.cs) |
| ✅ integrada | Sesiones | [SesionesTests.cs](Sesiones/SesionesTests.cs) |
| ✅ integrada | Contraseñas | [ContrasenasTests.cs](Contrasenas/ContrasenasTests.cs) |
| ✅ integrada | Formularios, cuentas y resultados inciertos | [FormulariosTests.cs](Formularios/FormulariosTests.cs) |
| ✅ integrada | Recursos, cabeceras y error seguro | [RecursosYErroresTests.cs](Seguridad/RecursosYErroresTests.cs) |
| ✅ integrada | Pacientes | [PacientesPaginasTests.cs](Pacientes/PacientesPaginasTests.cs) |
| ✅ ejecutada / ⏳ revisión humana | Agenda y citas | [AgendaPaginasTests.cs](Citas/AgendaPaginasTests.cs) |
| ✅ compartida | Preparación | [NavegadorRevision.cs](Apoyo/NavegadorRevision.cs) y [ayudas](Apoyo/AccesoSesionesYPermisosTests.cs) |

Ejecutar desde la raíz:

```powershell
pwsh scripts/qa/probar-paginas.ps1 -Canal chrome
```

El script compila y publica Web en Production, usa HTTPS y PostgreSQL ficticio desechable con Docker. En GitHub se usa Chromium; localmente puede usarse Chrome. No se graban trazas de identidad hasta la decisión de B04; los TRX y los diagnósticos omiten formularios, cookies y contraseñas.

Los casos de citas retienen y cortan peticiones concretas: el doble clic no depende de hacerlo en el instante adecuado. Comprobar una reserva abre su detalle sin escribir; reenviar conserva los datos y el identificador. Cada variante conserva su evidencia propia.

[QA B01](../../docs/testing/TherapEase-09-B01-QA-paginas.md) · [QA B03](../../docs/testing/TherapEase-09-B03-QA-paginas.md). B01 y B02 ya tienen revisión humana; el QA nuevo B03 conserva Claude APROBADO v2, 18-nov, y espera revisión humana distinta de Lucía y su decisión de fusión.
