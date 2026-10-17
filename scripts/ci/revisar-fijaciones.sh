#!/usr/bin/env bash
# S-34 y 05E: acciones de GitHub fijadas por SHA completo e imágenes base fijadas por digest.
# Además, cada flujo declara permisos mínimos y ninguno usa pull_request_target (ejecutaría código ajeno con permisos del repositorio).
# Uso: bash scripts/ci/revisar-fijaciones.sh
set -euo pipefail
cd "$(dirname "$0")/../.."

fallos=0
falla() {
  echo "::error file=$1,line=$2::$3"
  fallos=$((fallos + 1))
}

# ---- Flujos de GitHub Actions ----
flujos=()
for archivo in .github/workflows/*.yml .github/workflows/*.yaml; do
  [ -e "$archivo" ] && flujos+=("$archivo")
done

if [ "${#flujos[@]}" -eq 0 ]; then
  echo "::error::No hay ningún flujo en .github/workflows."
  exit 1
fi

for archivo in "${flujos[@]}"; do
  grep -q -E '^permissions:' "$archivo" || falla "$archivo" 1 "El flujo debe declarar permissions al inicio (permisos mínimos)."

  numero=0
  while IFS= read -r linea; do
    numero=$((numero + 1))
    linea="${linea%$'\r'}"
    sin_comentario="${linea%%#*}"

    if [[ "$sin_comentario" =~ pull_request_target ]]; then
      falla "$archivo" "$numero" "pull_request_target no está permitido."
    fi

    if [[ "$sin_comentario" =~ ^[[:space:]]*-?[[:space:]]*uses:[[:space:]]*([^[:space:]]+) ]]; then
      uso="${BASH_REMATCH[1]}"
      uso="${uso//\"/}"
      uso="${uso//\'/}"
      case "$uso" in
        ./*) ;;
        docker://*)
          [[ "$uso" =~ @sha256:[0-9a-f]{64}$ ]] || falla "$archivo" "$numero" "La imagen $uso debe fijarse por digest sha256."
          ;;
        *@*)
          [[ "${uso##*@}" =~ ^[0-9a-f]{40}$ ]] || falla "$archivo" "$numero" "La acción $uso debe fijarse por SHA completo de 40 caracteres."
          ;;
        *)
          falla "$archivo" "$numero" "La acción $uso no indica versión; debe fijarse por SHA completo."
          ;;
      esac
    fi

    if [[ "$sin_comentario" =~ ^[[:space:]]*image:[[:space:]]*([^[:space:]]+) ]]; then
      [[ "${BASH_REMATCH[1]}" =~ @sha256:[0-9a-f]{64} ]] || falla "$archivo" "$numero" "La imagen ${BASH_REMATCH[1]} debe fijarse por digest sha256."
    fi
  done < "$archivo"
done

# ---- Dockerfile ----
while IFS= read -r dockerfile; do
  etapas=()
  numero=0
  while IFS= read -r linea; do
    numero=$((numero + 1))
    linea="${linea%$'\r'}"
    sin_comentario="${linea%%#*}"
    [[ "$sin_comentario" =~ ^[[:space:]]*[Ff][Rr][Oo][Mm][[:space:]]+(.*)$ ]] || continue

    # shellcheck disable=SC2206
    palabras=(${BASH_REMATCH[1]})
    imagen=""
    alias=""
    indice=0
    while [ "$indice" -lt "${#palabras[@]}" ]; do
      palabra="${palabras[$indice]}"
      if [[ "$palabra" == --* ]]; then
        :
      elif [ -z "$imagen" ]; then
        imagen="$palabra"
      elif [[ "${palabra,,}" == "as" ]]; then
        alias="${palabras[$((indice + 1))]:-}"
        break
      fi
      indice=$((indice + 1))
    done

    es_etapa=0
    for etapa in "${etapas[@]:-}"; do
      [ -n "$etapa" ] && [ "${etapa,,}" == "${imagen,,}" ] && es_etapa=1
    done

    if [ "$es_etapa" -eq 0 ] && [ "${imagen,,}" != "scratch" ]; then
      [[ "$imagen" =~ @sha256:[0-9a-f]{64}$ ]] || falla "$dockerfile" "$numero" "La imagen base $imagen debe fijarse por digest sha256."
    fi

    [ -n "$alias" ] && etapas+=("$alias")
  done < "$dockerfile"
done < <(git ls-files -- 'Dockerfile' '*/Dockerfile' '*.Dockerfile')

if [ "$fallos" -gt 0 ]; then
  echo "Se encontraron $fallos problema(s) de fijación."
  exit 1
fi

echo "Acciones, imágenes base, permisos y disparadores de los flujos comprobados."
