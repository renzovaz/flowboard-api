#!/usr/bin/env bash
# Rollback do FlowBoard API para o MESMO artefato publicado anteriormente (por digest).
#
# Uso:
#   scripts/rollback.sh            → volta para a implantação bem-sucedida anterior à atual
#   scripts/rollback.sh <versão>   → volta para a versão indicada
#
# O digest vem do histórico (deploy/state/deployments.log), não da tag do registro:
# garante exatamente a imagem que já rodou. O rollback usa o mesmo núcleo do deploy
# (backup obrigatório, smoke test e retorno automático se o alvo falhar).
# O banco NÃO é revertido (migrations são aditivas — ADR-008).
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
STATE_DIR="$ROOT/deploy/state"
HISTORY="$STATE_DIR/deployments.log"

[[ -f "$STATE_DIR/current" && -f "$HISTORY" ]] || { echo "[rollback] não há implantação registrada." >&2; exit 4; }
read -r CURRENT_VERSION CURRENT_DIGEST < "$STATE_DIR/current"

TARGET_VERSION="${1:-}"; TARGET_DIGEST=""
if [[ -z "$TARGET_VERSION" ]]; then
  # Última implantação bem-sucedida com digest diferente do atual (lendo do fim para o início).
  line="$(tac "$HISTORY" | awk -F'\t' -v cur="$CURRENT_DIGEST" '$4 != cur {print; exit}')"
  [[ -n "$line" ]] || { echo "[rollback] não há versão anterior à $CURRENT_VERSION no histórico." >&2; exit 4; }
  TARGET_VERSION="$(cut -f3 <<<"$line")"; TARGET_DIGEST="$(cut -f4 <<<"$line")"
else
  TARGET_DIGEST="$(tac "$HISTORY" | awk -F'\t' -v v="$TARGET_VERSION" '$3 == v {print $4; exit}')"
fi

echo "[rollback] $CURRENT_VERSION → $TARGET_VERSION ${TARGET_DIGEST:-(digest via release-manifest.json)}"
args=("$TARGET_VERSION" --type rollback)
[[ -n "$TARGET_DIGEST" ]] && args+=(--digest "$TARGET_DIGEST")
exec "$ROOT/scripts/deploy.sh" "${args[@]}"
