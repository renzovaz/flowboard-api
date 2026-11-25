#!/usr/bin/env bash
# =============================================================================
# Deploy do FlowBoard API por DIGEST (ADR-021, ADR-023)
#
# Uso:
#   scripts/deploy.sh <versão> [--digest sha256:...] [--type deploy|rollback] [--force]
#
#   <versão>   versão SemVer publicada (ex.: 1.1.0). O digest é resolvido pelo
#              release-manifest.json do GitHub Release vX.Y.Z (ou informado em --digest).
#   --force    permite executar com o marcador de estado crítico (DEGRADED) presente.
#
# Fluxo:
#   pré-validação → resolve digest → pull (antes de mexer no que está rodando)
#   → backup obrigatório (instalação existente) → sobe a nova versão → smoke test
#   → sucesso: atualiza a versão ativa  |  falha: rollback automático para a versão ativa
#
# Estado em deploy/state/ (fora do Git):
#   current           versão e digest ATIVOS e verificados (só muda após smoke test aprovado)
#   image.env         IMAGE_REF ativo (para comandos manuais com --env-file)
#   deployments.log   implantações bem-sucedidas (initial, deploy, rollback, auto-rollback)
#   attempts.log      todas as tentativas e etapas
#   DEGRADED          marcador de estado crítico (rollback automático também falhou)
#
# Códigos de saída:
#   0 sucesso (ou versão já ativa)          1 falhou, versão anterior restaurada
#   2 falhou na primeira implantação        3 CRÍTICO: rollback automático falhou
#   4 abortado antes de alterar o ambiente (pré-validação, pull ou backup)
# =============================================================================
set -Eeuo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
DEPLOY_DIR="$ROOT/deploy"
STATE_DIR="$DEPLOY_DIR/state"
BACKUP_DIR="$DEPLOY_DIR/backups"
REPO="${FLOWBOARD_REPO:-renzovaz/flowboard-api}"
IMAGE="ghcr.io/$REPO"
VOLUME="flowboard_pgdata"

# ---------- argumentos ----------
VERSION=""; DIGEST=""; TYPE="deploy"; FORCE=false
while [[ $# -gt 0 ]]; do
  case "$1" in
    --digest) DIGEST="$2"; shift 2 ;;
    --type) TYPE="$2"; shift 2 ;;
    --force) FORCE=true; shift ;;
    -h|--help) sed -n '2,30p' "$0"; exit 0 ;;
    *) VERSION="$1"; shift ;;
  esac
done

OP_ID="$(date -u +%Y%m%dT%H%M%SZ)-$$"
log() { echo "[deploy] $*"; }
attempt() { printf '%s\t%s\t%s\t%s\t%s\n' "$(date -u +%Y-%m-%dT%H:%M:%SZ)" "$OP_ID" "$VERSION" "$1" "${2:-}" >> "$STATE_DIR/attempts.log"; }
abort() { log "ABORTADO: $1"; attempt "ABORTED" "$1"; exit 4; }

compose() { docker compose --project-directory "$DEPLOY_DIR" -f "$DEPLOY_DIR/compose.yaml" --env-file "$DEPLOY_DIR/.env" "$@"; }

# ---------- 1. pré-validação ----------
mkdir -p "$STATE_DIR" "$BACKUP_DIR"
[[ "$VERSION" =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]] || { echo "Uso: deploy.sh <versão X.Y.Z> [--digest sha256:...]" >&2; exit 4; }
attempt "STARTED" "type=$TYPE"

exec 9>"$STATE_DIR/.lock"
flock -n 9 || abort "outro deploy está em andamento"

for tool in docker curl jq flock; do command -v "$tool" >/dev/null || abort "ferramenta ausente: $tool"; done
[[ -f "$DEPLOY_DIR/.env" ]] || abort "deploy/.env não existe (cp deploy/.env.example deploy/.env)"
grep -qE '^POSTGRES_PASSWORD=.+' "$DEPLOY_DIR/.env" || abort "POSTGRES_PASSWORD vazio em deploy/.env"
if [[ -f "$STATE_DIR/DEGRADED" && "$FORCE" != true ]]; then
  abort "estado CRÍTICO registrado em deploy/state/DEGRADED; investigue e use --force"
fi

# ---------- 2. resolve o digest (referência imutável) ----------
if [[ -z "$DIGEST" ]]; then
  manifest_url="https://github.com/$REPO/releases/download/v$VERSION/release-manifest.json"
  log "resolvendo digest em $manifest_url"
  manifest="$(curl -fsSL "$manifest_url")" || abort "release-manifest.json da v$VERSION não encontrado"
  [[ "$(jq -r .version <<<"$manifest")" == "$VERSION" ]] || abort "manifesto não corresponde à versão $VERSION"
  DIGEST="$(jq -r .digest <<<"$manifest")"
fi
[[ "$DIGEST" =~ ^sha256:[0-9a-f]{64}$ ]] || abort "digest inválido: $DIGEST"
NEW_REF="$IMAGE@$DIGEST"
attempt "RESOLVED" "$DIGEST"

PREV_VERSION=""; PREV_DIGEST=""
if [[ -f "$STATE_DIR/current" ]]; then read -r PREV_VERSION PREV_DIGEST < "$STATE_DIR/current"; fi

if [[ "$DIGEST" == "$PREV_DIGEST" ]]; then
  log "a versão $VERSION ($DIGEST) já está ativa; nada a fazer"
  attempt "NOOP"
  exit 0
fi

# ---------- 3. pull ANTES de alterar o ambiente ----------
log "baixando $NEW_REF"
export IMAGE_REF="$NEW_REF"
compose pull --quiet || abort "falha ao baixar as imagens (rede/registro)"
attempt "PULLED"

# ---------- 4. classificação e backup obrigatório ----------
FIRST_DEPLOY=false
if [[ -z "$PREV_DIGEST" ]] && ! docker volume inspect "$VOLUME" >/dev/null 2>&1; then
  FIRST_DEPLOY=true
  log "primeira implantação (sem versão anterior e sem volume de dados): backup não necessário"
  attempt "FIRST_DEPLOY"
else
  backup_file="$BACKUP_DIR/$(date -u +%Y%m%dT%H%M%SZ)-antes-de-$VERSION.dump"
  log "backup obrigatório do banco → ${backup_file#"$ROOT"/}"
  compose up -d --wait db >/dev/null 2>&1 || abort "não foi possível iniciar o banco para o backup"
  if ! compose exec -T db pg_dump -U flowboard -d flowboard -Fc > "$backup_file" \
     || [[ ! -s "$backup_file" ]] \
     || ! compose exec -T db pg_restore --list < "$backup_file" >/dev/null; then
    rm -f "$backup_file"
    attempt "BACKUP_FAILED"
    abort "backup falhou; nenhuma alteração foi feita"
  fi
  attempt "BACKUP_OK" "${backup_file#"$ROOT"/}"
fi

# ---------- 5. sobe a nova versão e verifica ----------
start_and_verify() { # <ref> <versão> <digest>
  IMAGE_REF="$1" compose up -d --wait --wait-timeout 90 api >/dev/null 2>&1 || true
  "$ROOT/scripts/smoke-test.sh" "$2" "$3"
}

log "implantando $VERSION ($DIGEST)"
if start_and_verify "$NEW_REF" "$VERSION" "$DIGEST"; then
  echo "$VERSION $DIGEST" > "$STATE_DIR/current"
  echo "IMAGE_REF=$NEW_REF" > "$STATE_DIR/image.env"
  kind="$TYPE"; $FIRST_DEPLOY && kind="initial"
  printf '%s\t%s\t%s\t%s\n' "$(date -u +%Y-%m-%dT%H:%M:%SZ)" "$kind" "$VERSION" "$DIGEST" >> "$STATE_DIR/deployments.log"
  rm -f "$STATE_DIR/DEGRADED"
  attempt "SUCCESS" "$kind"
  log "SUCESSO: versão ativa = $VERSION"
  exit 0
fi

attempt "SMOKE_FAILED"
log "FALHA no smoke test da versão $VERSION"
compose logs --tail 50 api || true

# ---------- 6a. falha na primeira implantação: não há para onde voltar ----------
if [[ -z "$PREV_DIGEST" ]]; then
  compose stop api >/dev/null 2>&1 || true
  attempt "FIRST_DEPLOY_FAILED"
  log "primeira implantação falhou; API parada. Verifique os logs acima."
  exit 2
fi

# ---------- 6b. rollback automático para a versão ativa ----------
log "rollback automático para $PREV_VERSION ($PREV_DIGEST)"
if start_and_verify "$IMAGE@$PREV_DIGEST" "$PREV_VERSION" "$PREV_DIGEST"; then
  printf '%s\t%s\t%s\t%s\n' "$(date -u +%Y-%m-%dT%H:%M:%SZ)" "auto-rollback" "$PREV_VERSION" "$PREV_DIGEST" >> "$STATE_DIR/deployments.log"
  attempt "AUTO_ROLLBACK_OK" "restaurada=$PREV_VERSION"
  log "versão $PREV_VERSION restaurada; o deploy de $VERSION FALHOU"
  exit 1
fi

# ---------- 7. estado crítico ----------
touch "$STATE_DIR/DEGRADED"
attempt "AUTO_ROLLBACK_FAILED"
cat >&2 <<MSG
[deploy] CRÍTICO: o rollback automático para $PREV_VERSION também falhou.
  1. Diagnóstico:  docker compose -f deploy/compose.yaml --env-file deploy/.env --env-file deploy/state/image.env logs api db
                   curl -s http://127.0.0.1:8080/health
  2. Se o banco foi afetado, restaure o backup feito antes deste deploy (decisão manual):
                   ls -t deploy/backups/ | head -1
                   docker compose ... exec -T db pg_restore -U flowboard -d flowboard --clean --if-exists < deploy/backups/<arquivo>
  3. Após corrigir, execute novamente com --force (o marcador deploy/state/DEGRADED bloqueia novos deploys).
MSG
exit 3
