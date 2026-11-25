#!/usr/bin/env bash
# Smoke test pós-deploy: confirma que a versão implantada está saudável e é EXATAMENTE a esperada.
#   1. /health/ready responde 200 (até 60 s);
#   2. a versão reportada em /health é a versão pedida;
#   3. (opcional) o contêiner da API roda a imagem com o digest esperado.
# Uso: scripts/smoke-test.sh <versão> [<digest sha256:...>]
# Variáveis: BASE_URL (padrão http://127.0.0.1:8080), SMOKE_TIMEOUT (padrão 60)
set -euo pipefail

expected_version="${1:?Uso: smoke-test.sh <versão> [digest]}"
expected_digest="${2:-}"
base_url="${BASE_URL:-http://127.0.0.1:8080}"
timeout="${SMOKE_TIMEOUT:-60}"

echo "[smoke] aguardando $base_url/health/ready (até ${timeout}s)..."
deadline=$((SECONDS + timeout))
until curl -sf -o /dev/null "$base_url/health/ready"; do
  if (( SECONDS >= deadline )); then
    echo "[smoke] FALHA: /health/ready não respondeu 200 em ${timeout}s" >&2
    curl -s "$base_url/health" >&2 || true
    exit 1
  fi
  sleep 2
done

version="$(curl -sf "$base_url/health" | jq -r '.version')"
if [[ "$version" != "$expected_version" ]]; then
  echo "[smoke] FALHA: versão em execução '$version' ≠ esperada '$expected_version'" >&2
  exit 1
fi
echo "[smoke] versão em execução: $version ✔"

if [[ -n "$expected_digest" ]]; then
  container="$(docker ps -q --filter label=com.docker.compose.project=flowboard \
                            --filter label=com.docker.compose.service=api | head -1)"
  image_id="$(docker inspect --format '{{.Image}}' "$container")"
  repo_digests="$(docker image inspect --format '{{join .RepoDigests " "}}' "$image_id")"
  if [[ "$repo_digests" != *"$expected_digest"* ]]; then
    echo "[smoke] FALHA: a imagem em execução não corresponde ao digest $expected_digest" >&2
    echo "[smoke] RepoDigests: $repo_digests" >&2
    exit 1
  fi
  echo "[smoke] digest em execução: $expected_digest ✔"
fi

echo "[smoke] OK"
