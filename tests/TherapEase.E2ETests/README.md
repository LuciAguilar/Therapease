# Pruebas de navegador de Lucía

Las 54 pruebas de acceso y cuentas se organizan por tema. Los archivos de casos forman una misma clase parcial AccesoSesionesYPermisosTests para conservar la preparación compartida, los nombres y los filtros de xUnit.

| Estado | Categoría | Archivo |
| --- | --- | --- |
| ✅ organizada | Acceso | [AccesoTests.cs](Acceso/AccesoTests.cs) |
| ✅ organizada | Permisos | [PermisosTests.cs](Permisos/PermisosTests.cs) |
| ✅ organizada | Sesiones | [SesionesTests.cs](Sesiones/SesionesTests.cs) |
| ✅ organizada | Contraseñas | [ContrasenasTests.cs](Contrasenas/ContrasenasTests.cs) |
| ✅ organizada | Formularios, cuentas y resultados inciertos | [FormulariosTests.cs](Formularios/FormulariosTests.cs) |
| ✅ organizada | Recursos, cabeceras y error seguro | [RecursosYErroresTests.cs](Seguridad/RecursosYErroresTests.cs) |
| ✅ organizada | Preparación común | [NavegadorRevision.cs](Apoyo/NavegadorRevision.cs) y [ayudas del recorrido](Apoyo/AccesoSesionesYPermisosTests.cs) |

Ejecutar desde la raíz con pwsh scripts/qa/probar-paginas.ps1 -Canal chrome. El [QA B01](../../docs/testing/TherapEase-09-B01-QA-paginas.md) identifica alcance y resultados. Autoría de Lucía, auditoría y revisión humana de esta entrega pendientes.
