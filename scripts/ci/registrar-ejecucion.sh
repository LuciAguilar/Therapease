#!/usr/bin/env bash
# A-06: deja en el resumen del trabajo lo que el plan pide registrar de cada ejecución (runner, visibilidad, duración, commit y enlace).
# La duración exacta y los minutos facturables los muestra GitHub; aquí queda lo identificable. Los datos llegan por variables de entorno del flujo.
# Uso: bash scripts/ci/registrar-ejecucion.sh <trabajo> <inicio en segundos epoch> <resultado>
set -euo pipefail

trabajo="${1:?Falta el nombre del trabajo}"
inicio="${2:?Falta la hora de inicio}"
resultado="${3:-desconocido}"
destino="${GITHUB_STEP_SUMMARY:-/dev/stdout}"

duracion=$(($(date +%s) - inicio))

{
  echo "### Registro de la ejecución: $trabajo"
  echo ""
  echo "| Dato | Valor |"
  echo "| --- | --- |"
  echo "| Resultado | $resultado |"
  echo "| Duración medida dentro del trabajo | $((duracion / 60)) min $((duracion % 60)) s |"
  echo "| Runner | ${RUNNER_OS:-?} ${RUNNER_ARCH:-?}, imagen ${ImageOS:-?} ${ImageVersion:-?} |"
  echo "| Visibilidad del repositorio | ${VISIBILIDAD_REPOSITORIO:-desconocida} |"
  echo "| Commit de la rama del cambio | ${SHA_DEL_CAMBIO:-${GITHUB_SHA:-?}} |"
  echo "| Commit que ejecutó GitHub (en un PR, la combinación con la rama base) | ${GITHUB_SHA:-?} |"
  echo "| Evento | ${GITHUB_EVENT_NAME:-?} |"
  echo "| Ejecución | ${GITHUB_SERVER_URL:-}/${GITHUB_REPOSITORY:-}/actions/runs/${GITHUB_RUN_ID:-} (intento ${GITHUB_RUN_ATTEMPT:-?}) |"
  echo ""
} >> "$destino"
