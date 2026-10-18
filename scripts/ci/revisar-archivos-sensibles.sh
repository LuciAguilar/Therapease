#!/usr/bin/env bash
# 05E: secretos fuera de Git. Falla si el repositorio versiona archivos de entorno reales, credenciales, certificados o claves privadas.
# Solo se imprimen nombres de archivo, nunca el contenido que coincidió. GitHub Secret Protection es otra capa; esta comprobación no la sustituye.
# Uso: bash scripts/ci/revisar-archivos-sensibles.sh
set -euo pipefail
cd "$(dirname "$0")/../.."

fallos=0

# Nombres: .env* (salvo el ejemplo ficticio), credentials*.json, certificados/claves y carpetas secrets.
while IFS= read -r archivo; do
  [ "$archivo" = ".env.example" ] && continue
  echo "::error file=$archivo::Archivo sensible versionado; debe salir de Git y rotarse si contenía datos reales."
  fallos=$((fallos + 1))
done < <(git ls-files | grep -i -E '(^|/)\.env($|\.)|(^|/)credentials[^/]*\.json$|\.(pem|pfx|p12|key)$|(^|/)secrets?/' || true)

# Contenido: bloques de clave privada y claves secretas de age (ADR-05).
while IFS= read -r archivo; do
  echo "::error file=$archivo::Contiene una clave privada; debe salir de Git y rotarse."
  fallos=$((fallos + 1))
done < <(git grep -l -I -E 'BEGIN ([A-Z]+ )?PRIVATE KEY|AGE-SECRET-KEY-1' -- . ':!scripts/ci' || true)

if [ "$fallos" -gt 0 ]; then
  echo "Se encontraron $fallos archivo(s) sensibles."
  exit 1
fi

echo "Sin archivos de entorno reales, credenciales ni claves privadas versionados."
