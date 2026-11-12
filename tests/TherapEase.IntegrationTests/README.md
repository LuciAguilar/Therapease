# Pruebas de integración

Se clasificaron los archivos de Lucía y, por autorización expresa del 11-nov, los tres archivos de pacientes de Miguel. Cada archivo conserva su autoría. Se conservan nombres de clases, métodos y namespaces. Apoyo contiene las ayudas existentes, incluidas las que reutilizan otros proyectos.

| Estado | Carpeta | Archivos |
| --- | --- | --- |
| ✅ organizada | [Apoyo/Arquitectura](Apoyo/Arquitectura/) | ArquitecturaRevision.cs |
| ✅ organizada | [Apoyo/Auditoria](Apoyo/Auditoria/) | ConsultaAuditoriaRevision.cs |
| ✅ organizada | [Apoyo/BaseDeDatos](Apoyo/BaseDeDatos/) | ConcurrenciaSqlRevision.cs, PostgreSqlRevision.cs |
| ✅ organizada | [Apoyo/ErroresYRegistros](Apoyo/ErroresYRegistros/) | ErroresRegistrosRevision.cs |
| ✅ organizada | [Apoyo/HorasYDocker](Apoyo/HorasYDocker/) | HorasDockerRevision.cs |
| ✅ organizada | [Apoyo/Https](Apoyo/Https/) | HttpsLocalRevision.cs |
| ✅ organizada | [Apoyo/Identidad](Apoyo/Identidad/) | ServidorIdentidadRevision.cs, SolicitudIdentidad.cs |
| ✅ organizada | [Apoyo/Interrupciones](Apoyo/Interrupciones/) | InterrupcionesIdentidadRevision.cs |
| ✅ organizada | [Apoyo/Pacientes](Apoyo/Pacientes/) | PacientesRevision.cs, PacientesDePrueba.cs |
| ✅ organizada | [Apoyo/Recuperacion](Apoyo/Recuperacion/) | RecuperacionRevision.cs |
| ✅ organizada | [Apoyo/Servidor](Apoyo/Servidor/) | ServidorRevision.cs |
| ✅ organizada | [Apoyo/Sesiones](Apoyo/Sesiones/) | SesionesHttpsRevision.cs |
| ✅ organizada | [Arquitectura](Arquitectura/) | ArquitecturaTests.cs |
| ✅ organizada | [Auditoria](Auditoria/) | AuditoriaServidorTests.cs, ConsultaAuditoriaApiTests.cs, EventosAccesoServidorTests.cs |
| ✅ organizada | [Datos/Integridad](Datos/Integridad/) | RevisionDatosTests.cs |
| ✅ organizada | [Datos/Recuperacion](Datos/Recuperacion/) | RecuperacionTests.cs |
| ✅ organizada | [Entorno/HorasYDocker](Entorno/HorasYDocker/) | HorasDockerTests.cs |
| ✅ organizada | [Http/Formularios](Http/Formularios/) | FallosDeFormularioTests.cs |
| ✅ organizada | [Http/Https](Http/Https/) | HttpsLocalTests.cs |
| ✅ organizada | [Identidad/Acceso](Identidad/Acceso/) | IdentidadComplementariaTests.cs, IdentidadServidorTests.cs |
| ✅ organizada | [Identidad/Concurrencia](Identidad/Concurrencia/) | ConcurrenciaServidorTests.cs |
| ✅ organizada | [Identidad/Contrasenas](Identidad/Contrasenas/) | ContrasenasServidorTests.cs |
| ✅ organizada | [Identidad/Interrupciones](Identidad/Interrupciones/) | InterrupcionesIdentidadTests.cs |
| ✅ organizada | [Identidad/Permisos](Identidad/Permisos/) | PermisosUsuariosTests.cs |
| ✅ organizada | [Identidad/Sesiones](Identidad/Sesiones/) | SesionesHttpsTests.cs, SesionesServidorTests.cs |
| ✅ organizada | [Identidad/Usuarios](Identidad/Usuarios/) | ConsultaYEdicionUsuariosTests.cs, UsuariosServidorTests.cs |
| ✅ organizada | [Pacientes/Baja](Pacientes/Baja/) | BajaPacienteTests.cs |
| ✅ organizada | [Pacientes/Fichas](Pacientes/Fichas/) | PacientesFlujoTests.cs, PacientesServidorTests.cs |
| ✅ organizada | [Pacientes/Guardado](Pacientes/Guardado/) | PacientesGuardadoTests.cs |
| ✅ organizada | [Pacientes/Migracion](Pacientes/Migracion/) | PacientesMigracionTests.cs, MigracionDeDatosDePacienteTests.cs |
| ✅ organizada | [Pacientes/Proteccion](Pacientes/Proteccion/) | PacientesProteccionTests.cs |
| ✅ organizada | [Seguridad/Cabeceras](Seguridad/Cabeceras/) | CabecerasSeguridadTests.cs |
| ✅ organizada | [Seguridad/ErroresYRegistros](Seguridad/ErroresYRegistros/) | ErroresRegistrosTests.cs |

## Autoría y alcance

Los tres archivos de pacientes de Miguel cambian únicamente de ubicación, conservando contenido y namespaces. No quedan archivos .cs en la raíz de este proyecto. Las unitarias de Miguel y Dulce no se reorganizan.

Compilar o ejecutar con el mismo proyecto TherapEase.IntegrationTests.csproj. Los filtros por clase siguen funcionando. El [QA B01](../../docs/testing/TherapEase-09-B01-QA-paginas.md) contiene la preparación HTTP y de navegador.
