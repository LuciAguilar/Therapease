# TherapEase

## 1. Qué es y estado de hoy

TherapEase es una aplicación web para la gestión administrativa de un consultorio psicológico.

**Estado de hoy — 27-oct-2026:** main `a7c0c52` (PR #10 fusionado; [CI de esa fusión correcta](https://github.com/LuciAguilar/Therapease/actions/runs/40395259207)). Miguel implementa B02 en la rama `b02/fichas-de-pacientes`, todavía sin PR.

- B01 espera las páginas, formularios y navegador; B00/LCA/R04 siguen abiertos.
- B02: propuesta y diez decisiones aprobadas por Lucía el 26-oct; servidor en implementación y [contrato para páginas](docs/planning/TherapEase-etapa09-B02-contrato-para-paginas.md) preparado para Dulce.
- Solo datos ficticios, sin nube ni gastos; montos, «solo particular» y cancelación automática fuera.

## 2. Quién hace qué ahora

| Persona | Trabajo actual |
| --- | --- |
| Lucía | Decide las propuestas y dirige QA; otra persona revisa sus pruebas. |
| Miguel | Implementa el servidor de B02 (campos/tutor, permisos, búsqueda/homónimos, reenvío y ámbitos) en su rama desde main y avisa a Lucía de cualquier problema técnico. |
| Dulce | Confirma producto con la usuaria y prepara páginas, PageModel y sus unitarias. |

## 3. Documentos clave

| Documento | Para qué sirve |
| --- | --- |
| [AGENTS](AGENTS.md) | Reglas obligatorias, alcance y límites vigentes. |
| [Plan B01](docs/planning/TherapEase-etapa09-B01.md) | Acceso, identidad y páginas pendientes. |
| [Plan B02](docs/planning/TherapEase-etapa09-B02.md) | Plan aprobado; [propuesta ajustada](docs/planning/TherapEase-etapa09-B02-propuesta.md) aprobada el 26-oct. |
| [Contrato para páginas, B01](docs/planning/TherapEase-etapa09-B01-contrato-para-paginas.md) | Entradas, resultados y errores de acceso y usuarios para los PageModel de Dulce. |
| [Contrato para páginas, B02](docs/planning/TherapEase-etapa09-B02-contrato-para-paginas.md) | Entradas, resultados y errores de pacientes para los PageModel de Dulce. |
| [Plan de pruebas](docs/testing/TherapEase-09-plan-de-pruebas.md) | Casos, responsables y evidencia; enlaza B00. |
| [08B](docs/requirements/TherapEase-etapa08-casos-historias.md) | Historias, criterios y casos de uso. |
| [08D](docs/planning/TherapEase-etapa08-backlog-trazabilidad.md) | Backlog y trazabilidad; enlaza modelos y requisitos. |
| [Entrevista](docs/requirements/TherapEase-09-entrevista-requisitos.md) | Respuestas recibidas y confirmaciones pendientes. |
| [ADR](docs/architecture/adr/) | Decisiones de arquitectura y sus límites. |

## 4. Instalar

| Herramienta | Comando de comprobación | Resultado esperado |
| --- | --- | --- |
| SDK .NET 10 | `dotnet --list-sdks` | Una entrada `10.0.xxx`; instalar el SDK, no solo el runtime. |
| Docker Desktop | `docker version` | Cliente y servidor disponibles, con contenedores Linux. |
| Git | `git --version` | Versión instalada. |
| VS Code (editor comprobado) | `code --version` | Versión del editor. Si se elige otro, comprobar su versión con su comando. |
| StarUML | `winget list --id MKLabs.StarUML --exact --source winget` | Instalación detectada; abrir el `.mdj` con StarUML. |

## 5. Construir, probar y ejecutar

SDK de `global.json`; Visual Studio 18 o superior o VS Code/terminal. Desde la raíz, en PowerShell; sustituir `<clave>` por contraseñas locales y mantener las variables en la misma terminal. La aplicación no carga `.env`.

Arranque predeterminado: `dotnet run --project src/Web`. Imagen: `docker build -t therapease:local .` construye; `docker run --rm -p 8080:8080 therapease:local` ejecuta.
```powershell
dotnet build TherapEase.sln   # compilar
dotnet test TherapEase.sln    # probar; Docker debe estar disponible
docker run -d --name therapease-pg -e POSTGRES_PASSWORD=<clave> -e POSTGRES_DB=therapease_ficticia -p 55432:5432 postgres:17 # base local ficticia
Get-Content scripts/db/crear-roles.sql -Raw | docker exec -i therapease-pg psql -U postgres -v ON_ERROR_STOP=1 -v base=therapease_ficticia -v clave_migrador=<clave> -v clave_app=<clave> -f - # crear los roles
$env:ConnectionStrings__Migraciones = "Host=localhost;Port=55432;Database=therapease_ficticia;Username=therapease_migrador;Password=<clave>" # conexión de migración
dotnet tool restore # restaurar herramientas
dotnet ef database update --project src/Infrastructure --startup-project src/Web # aplicar migraciones
$env:ConnectionStrings__TherapEase = "Host=localhost;Port=55432;Database=therapease_ficticia;Username=therapease_app;Password=<clave>" # conexión de la aplicación
dotnet run --project src/Web -- --crear-superusuario <nombre-de-usuario> # una vez; temporal solo en esta consola
dotnet run --project src/Web -- --restablecer-superusuario <nombre-de-usuario> # recuperación por operador, solo si hace falta
dotnet dev-certs https --trust # confiar en el certificado local
dotnet run --project src/Web --launch-profile https # https://localhost:7028; usar HTTPS para formularios
```
En Git Bash, después de `dotnet restore`:
```bash
bash scripts/ci/revisar-paquetes-vulnerables.sh # revisar paquetes
bash scripts/ci/revisar-fijaciones.sh # revisar fijaciones y permisos
bash scripts/ci/revisar-archivos-sensibles.sh # revisar archivos sensibles
```

- Modelo/migraciones: `dotnet ef migrations has-pending-model-changes --project src/Infrastructure --startup-project src/Web`, con `ConnectionStrings__Migraciones` ficticia; no conecta a una base.
- Nueva migración: `dotnet ef migrations add <Nombre> --project src/Infrastructure --startup-project src/Web --output-dir Migraciones`; revisar SQL con `dotnet ef migrations script`. Al terminar: `Remove-Item Env:ConnectionStrings__TherapEase, Env:ConnectionStrings__Migraciones`.
- Auditoría: superusuario activo sin temporal, en la misma ventana autenticada: `GET /api/auditoria/eventos?desde=<instante-con-zona>&hasta=<instante-con-zona>`; opcionales `tipoRegistro` (Paciente/Cita/Usuario, nombres completos), `idRegistro`, `limite` (1–500; 100 por defecto). Sin zona, números/listas en tipoRegistro o filtros inválidos: 400; sin sesión: 401; sin permiso: 403. El recorrido espera páginas.
- Reglas y `/salud`: identidad, cabeceras, citas y auditoría en [AGENTS §2](AGENTS.md#2-reglas-que-deben-acompañar-cada-cambio-05e-y-07a07c). `/salud` mide el proceso, no PostgreSQL; el detalle operativo está en [B00](docs/planning/TherapEase-etapa09-B00.md) y [B01](docs/planning/TherapEase-etapa09-B01.md).

## 6. Cómo está organizado el código

| Proyecto | Puede referenciar |
| --- | --- |
| `TherapEase.Domain` | nada |
| `TherapEase.Application` | Domain |
| `TherapEase.Infrastructure` | Application, Domain |
| `TherapEase.Web` | Application; Domain solo para enumeraciones y contenedores de constantes sin lógica; Infrastructure solo para componer dependencias en el arranque |

| Capa | Carpeta dentro del módulo | Qué contiene |
| --- | --- | --- |
| Domain | `Entidades` y `Entidades/Enums` | Solo datos: entidades y enumeraciones. |
| Domain | `Reglas` | Las operaciones sobre las entidades (`ReglasDePaciente`, `ReglasDeCita`, `HoraHermosillo`). |
| Domain | `Excepciones`, `Constantes` | Excepciones de regla de negocio y constantes. |
| Application | `Interfaces/Repositorios` | Puertos de persistencia (uno por módulo) y `IUnidadDeTrabajo`. |
| Application | `Interfaces/Servicios` | Contratos de los servicios y de lo que implementa Infrastructure (sesión, usuario actual). |
| Application | `Servicios` | Implementaciones de los servicios. |
| Application | `Modelos`, `Constantes` | DTO y resultados (`RespuestaServicio<T>`), mensajes. |
| Infrastructure | `Entidades`, `Configuraciones`, `Repositorios`, `Servicios`, `Constantes` | Entidades de Identity, mapeo de EF, implementación de puertos y adaptadores. |
| Infrastructure | `Data`, `Migraciones`, `Configuracion` | Contexto de EF, migraciones y registro de dependencias (no pertenecen a un módulo). |
| Web | `Pages`, `Endpoints`, `Seguridad`, `Comandos`, `Configuracion`, `Salud` | Pantallas, rutas técnicas, cookie y políticas, comandos de operador, composición y sonda de salud. |

## 7. Antes de abrir un PR

- Indicar B/HU/CU/Q, criterios cubiertos, cambios y pendientes.
- Incluir las unitarias propias y evidencia de las comprobaciones aplicables.
- Mantener CI correcta; no acreditar pruebas omitidas o no ejecutadas.
- Revisar SQL, impacto y reversión si hay migraciones; no incluir secretos ni datos reales.
- Obtener revisión de otra persona y aprobación final de Lucía; seguir [AGENTS](AGENTS.md).
