# Prepara Web local y ejecuta las pruebas de navegador de acceso, sesiones y permisos.
# Usa PostgreSQL desechable y guarda resultados sin formularios, cookies ni contraseñas.

param(
    [ValidateSet('chromium','chrome','msedge')][string]$Canal = 'chromium',
    [string]$Resultados
)
$ErrorActionPreference = 'Stop'
$raizQa = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
if (-not $Resultados) { $Resultados = Join-Path $raizQa 'TestResults/paginas' }
$Resultados = [IO.Path]::GetFullPath($Resultados)
New-Item -ItemType Directory -Path $Resultados -Force | Out-Null
$variablesQa = @('THERAPEASE_WEB_QA_DLL','THERAPEASE_CANAL_NAVEGADOR','THERAPEASE_EVIDENCIA_QA','ASPNETCORE_CONTENTROOT')
$anterioresQa = @{}
foreach ($nombreQa in $variablesQa) { $anterioresQa[$nombreQa] = [Environment]::GetEnvironmentVariable($nombreQa) }
Push-Location $raizQa
try {
    dotnet build tests/TherapEase.E2ETests -c Release -warnaserror
    if ($LASTEXITCODE -ne 0) { throw 'No se compilaron las pruebas.' }
    dotnet publish src/Web -c Release --no-restore -o TestResults/web
    if ($LASTEXITCODE -ne 0) { throw 'No se preparo Web local.' }
    $env:THERAPEASE_WEB_QA_DLL = Join-Path $raizQa 'TestResults/web/TherapEase.Web.dll'
    $env:ASPNETCORE_CONTENTROOT = Join-Path $raizQa 'TestResults/web'
    $env:THERAPEASE_EVIDENCIA_QA = $Resultados
    $env:THERAPEASE_CANAL_NAVEGADOR = if ($Canal -eq 'chromium') { $null } else { $Canal }
    dotnet test tests/TherapEase.E2ETests -c Release --no-build --logger 'trx;LogFileName=navegador.trx' --results-directory $Resultados
    if ($LASTEXITCODE -ne 0) { throw 'Hay pruebas de navegador fallidas. Revisa el TRX.' }
} finally {
    foreach ($nombreQa in $variablesQa) { [Environment]::SetEnvironmentVariable($nombreQa, $anterioresQa[$nombreQa]) }
    Pop-Location
}
