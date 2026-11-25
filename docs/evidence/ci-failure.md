# Evidência: execução com falha e com sucesso no GitHub Actions

Requisito 3 da disciplina: pelo menos **uma execução bem-sucedida e uma com falha**, com o motivo da falha, como ela foi identificada e a correção aplicada.

Este repositório tem **duas** falhas reais documentadas:

1. **Falha controlada** (planejada): uma regressão no limite de WIP introduzida de propósito num PR isolado.
2. **Falha não planejada** na `main`: uma condição de corrida no smoke test da imagem Docker, investigada e corrigida.

---

## 1. Falha controlada: regressão no limite de WIP (PR #29)

- **PR:** https://github.com/renzovaz/flowboard-api/pull/29 (rótulo `ci-demo`, **fechado sem merge**)
- **Branch preservada:** `demo/ci-failure-wip-boundary` (ruleset impede a exclusão)

### Linha do tempo

| Passo | Commit | Execução | Resultado |
|---|---|---|---|
| Regressão: `currentCount >= limit` → `currentCount > limit` em `WipPolicy` | `04947aa` | [45669612342](https://github.com/renzovaz/flowboard-api/actions/runs/45669612342) | ❌ `build-test` **failure** |
| Correção: comparação inclusiva restaurada | `57cfaa3` | [45669995935](https://github.com/renzovaz/flowboard-api/actions/runs/45669995935) | ✅ todos os checks **success** |
| PR fechado **sem merge**: a `main` nunca recebeu o código defeituoso | — | — | — |

### Motivo da falha

Erro de borda clássico: com `>`, uma coluna com limite 3 e 3 Stories (3/3) **aceitaria a 4ª**, violando a regra R2 (limite de WIP).

### Como foi identificada

A etapa **"Testes unitários + gate de cobertura do Domain"** falhou (`Failed: 2, Passed: 73, Total: 75`). O log da execução mostra:

```
FlowBoard.UnitTests.Rules.WipPolicyTests.EnsureCanEnter_WhenColumnIsAtLimit_ThrowsWipLimitExceeded [FAIL]
FlowBoard.UnitTests.Rules.WipPolicyTests.GetWipLimit_ReviewColumn_UsesReviewLimit [FAIL]
Assert.Throws() Failure: No exception was thrown
Expected: typeof(FlowBoard.Api.Domain.Exceptions.ConflictException)
```

O nome do teste já identifica a regra (limite de WIP) e o cenário (coluna **no** limite), e a mensagem mostra que a recusa (409) não aconteceu. O logger do GitHub Actions também gerou **anotações** no PR, apontando o arquivo e a linha.

- **Bloqueio:** `build-test` é **check obrigatório** do ruleset da `main`, então o merge ficou bloqueado.
- **Etapas seguintes:** testes de integração e publicação foram **puladas**, porque o pipeline para na primeira etapa que falha. Executados **localmente** contra o mesmo código defeituoso, os testes de integração da mesma regra também falham:

```
StoriesWorkflowTests.MoveStory_WhenWipFull_Returns409WipLimitExceeded [FAIL]   Expected: Conflict  Actual: OK
WipLimitConcurrencyTests.ConcurrentMoves_NeverExceedWipLimit [FAIL]            Expected: 3         Actual: 4
```

### Correção

Commit `fix(demo): restaura comparação inclusiva no limite de WIP`. Na execução seguinte: unitários `75/75` e integração `31/31` aprovados, e `docker-build` e `secret-scan` verdes.

---

## 2. Falha não planejada na `main`: condição de corrida no smoke test (corrigida no PR #28)

| | Execução | Resultado |
|---|---|---|
| CI na `main` após o merge do #24 | [45668328481](https://github.com/renzovaz/flowboard-api/actions/runs/45668328481) | ❌ `docker-build` **failure** |
| PR de correção #28 | https://github.com/renzovaz/flowboard-api/pull/28 | ✅ |

**Sintoma:** a etapa *Smoke test da imagem* falhou (a API não respondeu `/health/ready` em 60 s, e o `jq` terminou com código 4 por falta de entrada). O envio do SARIF falhou em seguida porque o Trivy não chegou a rodar. No PR #24, o mesmo job tinha passado.

**Causa (reproduzida localmente):** no smoke test, a API era iniciada com `docker run` logo após o PostgreSQL. Se o banco ainda não aceitava conexões, a migration da inicialização lançava uma exceção e o **processo encerrava** (o contêiner saiu com código 139). No Compose isso não acontece, por causa do `depends_on: service_healthy`.

**Correção:**
1. A API aguarda o banco antes de aplicar as migrations (até ~60 s, só para indisponibilidade), registrando o evento `WaitingForDatabase` (2004). Ver ADR-027.
2. O smoke test aguarda o `pg_isready`, espera até 90 s, exibe os logs da API em caso de falha, e o SARIF só é enviado quando foi gerado.

**Validação:** a API iniciada 7 s **antes** do PostgreSQL registrou `WaitingForDatabase tentativa 1/20: Name does not resolve` e `tentativa 2/20: Connection refused`, e depois ficou `healthy`.

---

## Execuções bem-sucedidas de referência

- CI na `main`: https://github.com/renzovaz/flowboard-api/actions/workflows/ci.yml?query=branch%3Amain
- Release `v1.0.0`: [45669035765](https://github.com/renzovaz/flowboard-api/actions/runs/45669035765) (guard, verify, image e github-release: success)
