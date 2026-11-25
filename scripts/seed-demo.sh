#!/usr/bin/env bash
# Popula dados de demonstração CHAMANDO A PRÓPRIA API (as regras de negócio são aplicadas).
# Uso: ./scripts/seed-demo.sh [BASE_URL]   (padrão: http://localhost:8080)
# Requisitos: curl e jq. Execute em um banco recém-criado (nomes são únicos).
set -euo pipefail

BASE_URL="${1:-${BASE_URL:-http://localhost:8080}}"

api() { # api METHOD PATH [JSON] -> corpo; aborta em HTTP >= 400
  local method="$1" path="$2" body="${3:-}" out code
  if [[ -n "$body" ]]; then
    out=$(curl -sS -X "$method" -H "Content-Type: application/json" -d "$body" -w $'\n%{http_code}' "$BASE_URL$path")
  else
    out=$(curl -sS -X "$method" -w $'\n%{http_code}' "$BASE_URL$path")
  fi
  code="${out##*$'\n'}"; out="${out%$'\n'*}"
  if (( code >= 400 )); then
    echo "Falha: $method $path -> HTTP $code" >&2; echo "$out" >&2; exit 1
  fi
  printf '%s' "$out"
}
id() { jq -r '.id'; }

story() { # story FEATURE TEAM POINTS ITERATION TITLE
  api POST "/api/features/$1/stories" "$(jq -nc --arg t "$5" --argjson p "$3" --argjson team "$2" --argjson it "$4" \
    '{title:$t, acceptanceCriteria:"Dado o cenário descrito, quando a ação ocorrer, então o resultado esperado é exibido", storyPoints:$p, teamId:$team, iteration:$it}')" | id
}
move() { # move STORY STATUS...
  local s="$1"; shift
  for status in "$@"; do api PATCH "/api/stories/$s/status" "{\"status\":\"$status\"}" >/dev/null; done
}

echo "→ Verificando a API em $BASE_URL"
curl -sf "$BASE_URL/health/ready" >/dev/null 2>&1 || curl -sf "$BASE_URL/api/teams" >/dev/null || { echo "API indisponível em $BASE_URL" >&2; exit 1; }

echo "→ Equipes"
ALPHA=$(api POST /api/teams '{"name":"Alpha","capacityPoints":20,"wipLimitInProgress":3,"wipLimitReview":2}' | id)
BETA=$(api POST /api/teams '{"name":"Beta","capacityPoints":20,"wipLimitInProgress":3,"wipLimitReview":2}' | id)

echo "→ Program Increment"
PI=$(api POST /api/program-increments '{"name":"PI 2026.1","startDate":"2026-11-23","iterationCount":5,"iterationLengthWeeks":2}' | id)

echo "→ Features (WSJF)"
LOGIN=$(api POST "/api/program-increments/$PI/features" '{"title":"Login social","description":"Entrar com conta Google","businessValue":13,"timeCriticality":8,"riskReduction":2,"jobSize":3}' | id)
NOTIF=$(api POST "/api/program-increments/$PI/features" '{"title":"Notificações","description":"Avisos por e-mail","businessValue":8,"timeCriticality":5,"riskReduction":3,"jobSize":5}' | id)
REPORT=$(api POST "/api/program-increments/$PI/features" '{"title":"Relatórios","description":"Exportação em PDF","businessValue":5,"timeCriticality":2,"riskReduction":1,"jobSize":8}' | id)

echo "→ Stories da equipe Alpha"
A1=$(story "$LOGIN" "$ALPHA" 5 1 "Botão Entrar com Google");      move "$A1" Ready InProgress Review Done
A2=$(story "$LOGIN" "$ALPHA" 3 1 "Callback OAuth");               move "$A2" Ready InProgress Review Done
A3=$(story "$LOGIN" "$ALPHA" 3 1 "Vincular conta existente");     move "$A3" Ready InProgress Review
A4=$(story "$LOGIN" "$ALPHA" 2 1 "Tratar erro de autorização");   move "$A4" Ready InProgress
A5=$(story "$NOTIF" "$ALPHA" 3 2 "Template de e-mail");           move "$A5" Ready InProgress
A6=$(story "$NOTIF" "$ALPHA" 2 2 "Preferências de notificação");  move "$A6" Ready
A7=$(story "$NOTIF" "$ALPHA" 1 2 "Link de descadastro");          move "$A7" Ready

echo "→ Stories da equipe Beta"
B1=$(story "$REPORT" "$BETA" 5 1 "Gerar PDF do quadro");          move "$B1" Ready InProgress
B2=$(story "$REPORT" "$BETA" 8 2 "Agendar envio de relatório")
api POST "/api/features/$LOGIN/stories" '{"title":"Login com Microsoft (a estimar)"}' >/dev/null

cat <<MSG

✔ Dados de demonstração criados.
  Equipes: Alpha=$ALPHA  Beta=$BETA   PI=$PI
  Features (por WSJF): Login social=$LOGIN  Notificações=$NOTIF  Relatórios=$REPORT
  Alpha: InProgress 2/3 (Stories $A4, $A5) · Ready: $A6, $A7 · Review: $A3 · Done: $A1, $A2

  Sugestões para a demonstração:
    curl -s $BASE_URL/api/program-increments/$PI/features | jq           # priorização WSJF
    curl -s $BASE_URL/api/teams/$ALPHA/board | jq                         # quadro Kanban
    curl -s -X PATCH $BASE_URL/api/stories/$A6/status -H 'Content-Type: application/json' -d '{"status":"InProgress"}'   # 3/3
    curl -s -X PATCH $BASE_URL/api/stories/$A7/status -H 'Content-Type: application/json' -d '{"status":"InProgress"}'   # 409 WIP
    curl -s $BASE_URL/api/program-increments/$PI/progress | jq            # progresso (R8)
MSG
