#!/usr/bin/env bash
# Guarda de migrations (ADR-008): falha se uma migration NOVA contém operações destrutivas
# no método Up(). Mudanças destrutivas exigem Expand/Contract e o rótulo "breaking-migration".
# Uso: scripts/ci/check-migrations.sh <commit-base> [<commit-head>]
set -euo pipefail

base="$1"; head="${2:-HEAD}"
dir="src/FlowBoard.Api/Data/Migrations"
pattern='\.(DropTable|DropColumn|RenameColumn|RenameTable|AlterColumn)\('

mapfile -t added < <(git diff --name-only --diff-filter=A "$base" "$head" -- "$dir/*.cs" \
  | grep -vE '\.Designer\.cs$|ModelSnapshot\.cs$' || true)

if [[ ${#added[@]} -eq 0 ]]; then
  echo "Nenhuma migration nova."
  exit 0
fi

status=0
for file in "${added[@]}"; do
  # Apenas o corpo de Up(): Down() naturalmente contém DropTable ao desfazer um CreateTable.
  up_body=$(awk '/override void Up\(/{flag=1} /override void Down\(/{flag=0} flag' "$file")
  if grep -nE "$pattern" <<<"$up_body"; then
    echo "::error file=$file::Migration com operação destrutiva em Up(). Use Expand/Contract (docs/deployment.md, ADR-008)."
    status=1
  else
    echo "OK (aditiva): $file"
  fi
done
exit $status
