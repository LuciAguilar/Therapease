#!/usr/bin/env bash
# S-33: falla si algún paquete NuGet, directo o transitivo, tiene una vulnerabilidad alta o crítica.
# Las de severidad menor se muestran como aviso para que alguien las revise; no detienen el cambio.
# Uso: bash scripts/ci/revisar-paquetes-vulnerables.sh [solucion]   (requiere un restore previo)
set -euo pipefail
cd "$(dirname "$0")/../.."

solucion="${1:-TherapEase.sln}"
informe="$(mktemp)"
trap 'rm -f "$informe"' EXIT

# Si dotnet falla (sin restore, sin acceso a la fuente de avisos), el script falla: callar no es aprobar.
dotnet list "$solucion" package --vulnerable --include-transitive --format json > "$informe"

# "problems" aparece cuando alguna fuente no pudo consultarse; en ese caso no se sabe si hay vulnerabilidades.
if grep -q '"problems"' "$informe"; then
  echo "::error::La auditoría de paquetes reportó problemas y no se puede asegurar que no haya vulnerabilidades."
  cat "$informe"
  exit 1
fi

if ! grep -q '"projects"' "$informe"; then
  echo "::error::La auditoría de paquetes no devolvió proyectos; revisa la solución indicada."
  cat "$informe"
  exit 1
fi

graves="$(grep -c -i -E '"severity": *"(high|critical)"' "$informe" || true)"
menores="$(grep -c -i -E '"severity": *"(low|moderate)"' "$informe" || true)"

if [ "$menores" -gt 0 ]; then
  echo "::warning::Hay $menores vulnerabilidad(es) de severidad baja o moderada en paquetes; revísalas en el informe."
fi

if [ "$graves" -gt 0 ]; then
  echo "::error::Hay $graves vulnerabilidad(es) de severidad alta o crítica en paquetes NuGet."
  cat "$informe"
  exit 1
fi

echo "Sin vulnerabilidades altas o críticas en paquetes NuGet (bajas o moderadas: $menores)."
