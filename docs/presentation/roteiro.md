# Roteiro da apresentação (cerca de 15 minutos)

## Preparação (antes da aula)

```bash
cd flowboard-api
docker compose down                        # o ambiente de desenvolvimento usa a mesma porta
# em deploy/.env: SWAGGER_ENABLED=true (somente para a demonstração)
./scripts/deploy.sh 1.0.0                  # deixa a 1.0.0 ativa
docker pull ghcr.io/renzovaz/flowboard-api@sha256:7e27a1fe76cc910633dedff0f41523a5da4e02ffb878489653c76cdfabf12f17   # cache da 1.1.0
```

Abas abertas no navegador (plano B se a rede falhar): Actions (execuções vermelha e verde), PR #29, Releases, pacote GHCR, Security › Code scanning e `/swagger`.

> Para recomeçar com um banco vazio: `docker compose -f deploy/compose.yaml --env-file deploy/.env --env-file deploy/state/image.env down -v` **apaga os dados de demonstração**.

## Sequência

| # | Tempo | O que mostrar | Comando / tela | Requisito |
|---|---|---|---|---|
| 1 | 1 min | Problema, solução e tecnologias | README | 1 |
| 2 | 1 min | Aplicação no ar | `docker compose ... ps` (healthy) · `curl -s localhost:8080/health \| jq` | 5, 7 |
| 3 | 1,5 min | Swagger e cadastros | `./scripts/seed-demo.sh` e criar uma equipe ao vivo no `/swagger` | 1 |
| 4 | 1 min | Priorização WSJF | `GET /api/program-increments/1/features`: (13+8+2)/3 = 7,67 | 1 (R7) |
| 5 | 1 min | Fluxo Kanban | `GET /api/teams/1/board` e mover a Story 6 para InProgress (3/3) | 1 (R1) |
| 6 | 1 min | Violação de WIP | mover a Story 7 → **409 `WIP_LIMIT_EXCEEDED`** | 1 (R2) |
| 7 | 1 min | DoR e capacidade | Story sem pontos → Ready = **422 `missingCriteria`** · Story de 8 pontos acima da capacidade = **409** | 1 (R3, R5) |
| 8 | 1 min | Testes | `dotnet test`: 75 unitários + 32 de integração (PostgreSQL real) | 4 |
| 9 | 1 min | Pipeline verde | Actions › CI na `main`: jobs, resumo de cobertura, artefato | 3 |
| 10 | 1,5 min | Falha controlada | PR #29: execução vermelha (U04 "No exception was thrown") → correção → verde. Fechado sem merge | 3, 4 |
| 11 | 1 min | Artefato no GHCR | release v1.1.0: tabela versão, commit e digest; pacote; aba Security (Trivy) | 5, 8 |
| 12 | 1 min | Atualização | `./scripts/deploy.sh 1.1.0`: backup, smoke test, `/metrics` 200, dados preservados | 6 |
| 13 | 1 min | Rollback | `./scripts/rollback.sh`: mesmo digest da 1.0.0, `/metrics` 404, `deploy/state/deployments.log` | 6 |
| 14 | 1 min | Detectar falha após o deploy | `docker compose ... stop db` → API `unhealthy`, `/health/ready` 503, erro 503 com `traceId`, log 2001 com o mesmo `traceId` → `start db` | 9 |

Atalho para os comandos do Compose implantado:

```bash
C="docker compose -f deploy/compose.yaml --env-file deploy/.env --env-file deploy/state/image.env"
$C ps
$C stop db ; curl -s localhost:8080/api/teams | jq ; $C logs api --no-log-prefix | grep DatabaseUnavailable | tail -1 ; $C start db
```

## Perguntas prováveis

| Pergunta | Resposta curta |
|---|---|
| Por que o lock `FOR UPDATE`? | Sem ele, requisições simultâneas leem a mesma contagem de WIP. O teste I14 mostrou de 9 a 10 aceitas com limite 3. Com o lock, entram exatamente 3. |
| Por que deploy por digest? | A tag de imagem é um ponteiro mutável. O digest garante o mesmo artefato byte a byte, no deploy e no rollback. |
| Por que o rollback não reverte o banco? | As migrations são aditivas (ADR-008): a versão anterior funciona com o esquema novo. Mudanças destrutivas seguem *Expand/Contract*. |
| Push Protection × Gitleaks? | O Push Protection **impede** o push de segredos conhecidos. O Gitleaks **detecta** no CI o que escapou, inclusive no histórico. |
| Por que não há deploy automático pelo Actions? | Não há servidor. Um *self-hosted runner* em repositório público permitiria que *forks* executassem código na máquina. |
| A falha do PR #29 é "de verdade"? | Sim: foi uma regressão introduzida de propósito, num PR isolado, e o pipeline real a detectou e bloqueou. Também houve uma falha real não planejada na `main` (run 45668328481), corrigida no #28. |
