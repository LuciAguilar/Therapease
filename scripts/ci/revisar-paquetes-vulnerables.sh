#!/usr/bin/env bash
# S-33: falla si algún paquete NuGet, directo o transitivo, tiene una vulnerabilidad alta o crítica.
# Las de severidad menor se muestran como aviso, con el nombre del paquete, para que alguien las revise; no detienen el cambio.
# Uso: bash scripts/ci/revisar-paquetes-vulnerables.sh [solucion]   (requiere un restore previo)
# INFORME_DE_PRUEBA: solo para probar este script; apunta a un JSON de `dotnet list package` y evita ejecutar dotnet.
set -euo pipefail
cd "$(dirname "$0")/../.."

solucion="${1:-TherapEase.sln}"
informe="${INFORME_DE_PRUEBA:-}"

if [ -z "$informe" ]; then
  informe="$(mktemp)"
  trap 'rm -f "$informe"' EXIT
  # Si dotnet falla (sin restore, sin acceso a la fuente de avisos), el script falla: callar no es aprobar.
  dotnet list "$solucion" package --vulnerable --include-transitive --format json > "$informe"
fi

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

# Una línea por vulnerabilidad: severidad, paquete y aviso. En el JSON, el id del paquete precede a sus vulnerabilidades.
hallazgos="$(awk '
  function valor(linea, clave,   s) {
    if (match(linea, "\"" clave "\": *\"[^\"]*\"")) {
      s = substr(linea, RSTART, RLENGTH)
      sub("\"" clave "\": *\"", "", s)
      sub("\"$", "", s)
      return s
    }
    return ""
  }
  /"id":/ { paquete = valor($0, "id") }
  /"severity":/ { severidad = valor($0, "severity") }
  /"advisoryurl":/ { print severidad "\t" paquete "\t" valor($0, "advisoryurl") }
' "$informe")"

graves=0
menores=0
while IFS=$'\t' read -r severidad paquete aviso; do
  [ -z "$severidad" ] && continue
  case "${severidad,,}" in
    high | critical)
      graves=$((graves + 1))
      echo "::error::$paquete: vulnerabilidad $severidad. $aviso"
      ;;
    *)
      menores=$((menores + 1))
      echo "::warning::$paquete: vulnerabilidad $severidad. $aviso"
      ;;
  esac
done <<< "$hallazgos"

if [ "$graves" -gt 0 ]; then
  echo "Hay $graves vulnerabilidad(es) de severidad alta o crítica en paquetes NuGet."
  exit 1
fi

echo "Sin vulnerabilidades altas o críticas en paquetes NuGet (bajas o moderadas: $menores)."
