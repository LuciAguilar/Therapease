#!/usr/bin/env bash
# Resume un archivo TRX en el resumen del trabajo y falla si hay pruebas fallidas u omitidas, o si no se ejecutó ninguna.
# Una prueba omitida no cuenta como correcta (AGENTS 2.7): si hay una, el cambio no queda aprobado en silencio.
# Uso: bash scripts/ci/resumir-resultados.sh <archivo.trx> <titulo>
set -euo pipefail

trx="${1:?Falta el archivo TRX}"
titulo="${2:?Falta el título}"
destino="${GITHUB_STEP_SUMMARY:-/dev/stdout}"

if [ ! -f "$trx" ]; then
  echo "::error::No existe $trx; las pruebas no produjeron resultados."
  exit 1
fi

contadores="$(grep -o -m1 '<Counters [^>]*>' "$trx" || true)"
if [ -z "$contadores" ]; then
  echo "::error::$trx no tiene contadores; no se puede asegurar qué se ejecutó."
  exit 1
fi

valor() {
  local numero
  numero="$(grep -o -m1 " $1=\"[0-9]*\"" <<<"$contadores" | grep -o '[0-9]*' || true)"
  echo "${numero:-0}"
}

total="$(valor total)"
ejecutadas="$(valor executed)"
correctas="$(valor passed)"
fallidas="$(valor failed)"
no_ejecutadas="$(valor notExecuted)"

# xUnit cuenta una prueba omitida en el total pero no entre las ejecutadas (executed); notExecuted cubre otros casos.
omitidas=$((total - ejecutadas))
if [ "$no_ejecutadas" -gt "$omitidas" ]; then
  omitidas="$no_ejecutadas"
fi
no_ejecutadas="$omitidas"

{
  echo "### Pruebas: $titulo"
  echo ""
  echo "| Total | Correctas | Fallidas | Omitidas o no ejecutadas |"
  echo "| ---: | ---: | ---: | ---: |"
  echo "| $total | $correctas | $fallidas | $no_ejecutadas |"
  echo ""
} >> "$destino"

if [ "$total" -eq 0 ]; then
  echo "::error::$titulo: no se ejecutó ninguna prueba."
  exit 1
fi

if [ "$fallidas" -gt 0 ] || [ "$no_ejecutadas" -gt 0 ] || [ "$correctas" -ne "$total" ]; then
  echo "::error::$titulo: $correctas correctas de $total ($fallidas fallidas, $no_ejecutadas omitidas o no ejecutadas)."
  exit 1
fi

echo "$titulo: $correctas correctas de $total, 0 fallidas, 0 omitidas."
