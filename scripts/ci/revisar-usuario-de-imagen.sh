#!/usr/bin/env bash
# 05E: el contenedor no debe correr como administrador.
# El usuario configurado puede venir como «nombre», «uid», «nombre:grupo» o «uid:gid»; solo importa la parte anterior a «:».
# Se rechaza vacío (Docker usa root), «root» y cualquier uid 0 («0», «00», «0:0», «root:root»). Si la imagen trae el comando id,
# se comprueba además el identificador efectivo del proceso, porque un nombre cualquiera podría resolverse al uid 0.
# Uso: bash scripts/ci/revisar-usuario-de-imagen.sh <imagen>
set -euo pipefail

imagen="${1:?Falta el nombre de la imagen}"

es_administrador() {
  local nombre="${1%%:*}"
  [[ -z "$nombre" || "$nombre" == "root" || "$nombre" =~ ^0+$ ]]
}

usuario="$(docker image inspect --format '{{.Config.User}}' "$imagen")"
echo "Usuario configurado en la imagen: ${usuario:-<vacío>}"

if es_administrador "$usuario"; then
  echo "::error::La imagen corre como administrador (usuario «${usuario:-<vacío>}»)."
  exit 1
fi

if efectivo="$(docker run --rm --entrypoint id "$imagen" -u 2>/dev/null)"; then
  echo "Identificador efectivo del proceso: $efectivo"
  if [[ "$efectivo" =~ ^0+$ ]]; then
    echo "::error::El proceso de la imagen se ejecuta con el identificador 0 (administrador)."
    exit 1
  fi
else
  echo "::warning::La imagen no trae el comando id: solo se comprobó el usuario configurado."
fi

echo "La imagen no corre como administrador."
