# FlowBoard API — Documento de Evidências

**Disciplina:** Integração DevOps — Engenharia de Software  
**Integrante:** Renzo Machado ([@renzovaz](https://github.com/renzovaz)) — desenvolvimento individual  
**Repositório:** https://github.com/renzovaz/flowboard-api  
**Artefato:** `ghcr.io/renzovaz/flowboard-api` — versões `1.0.0` e `1.1.0`

## 1. Descrição do projeto

O **FlowBoard API** é uma API REST de gestão ágil do fluxo de trabalho. O **Kanban** é o núcleo funcional, e **conceitos selecionados do SAFe** (Program Increment, iteração IP, Features priorizadas por WSJF e capacidade das equipes) servem de referência conceitual. A API aplica oito regras de negócio:

1. fluxo Kanban com transições válidas;
2. limite de WIP por equipe e coluna, seguro sob concorrência;
3. Definition of Ready;
4. Story Points em Fibonacci;
5. capacidade por iteração;
6. iteração IP sem Stories;
7. priorização WSJF;
8. progresso por pontos.

O foco do trabalho é o **ciclo DevOps/DevSecOps** construído em torno da aplicação.

## 2. Tecnologias

C# 14 / **ASP.NET Core 10 (LTS)**, Entity Framework Core 10, **PostgreSQL 17**, **xUnit v3** + Testcontainers, OpenAPI/Swagger, **Git/GitHub**, **GitHub Actions**, **Docker** e **Docker Compose**, **GHCR**, .NET Analyzers, **NuGet Audit**, **Gitleaks**, **Trivy**, GitHub Push Protection, Dependabot, logs JSON, Health Checks e Prometheus (`/metrics`).

## 3. Estratégia de versionamento

- **GitHub Flow:** `main` sempre implantável, branches `feature/*`, `fix/*`, `ci/*`, `docs/*` e `chore/*`, e integração exclusivamente por **Pull Request** com **merge commit**.
- **Conventional Commits** (`feat(api): …`, `fix(data): …`), com issues e milestones ligadas aos PRs.
- **Ruleset da `main`:** PR obrigatório, três checks obrigatórios (`build-test`, `secret-scan`, `docker-build`), branch atualizada antes do merge, sem *force push* e **sem bypass**.
- **Releases:** tags anotadas **SemVer** (`v1.0.0`, `v1.1.0`), imutáveis por ruleset.
- **Números (25/11/2026):** 59 commits na `main` (antes deste documento), **14 PRs mesclados** (#15–#25, #28, #30, #31), 1 PR de demonstração fechado sem merge (#29) e 2 PRs do Dependabot fechados com justificativa (#26, #27).

## 4. Pipeline

**CI — `.github/workflows/ci.yml`** (PR, push na `main`, semanal, manual e reutilizável):

| Job | Etapas |
|---|---|
| `build-test` | checkout → setup .NET (`global.json`) → restore `--locked-mode` + **NuGet Audit** → guarda de migrations → `dotnet format` → build com analyzers (avisos = erro) → **75 casos unitários** + gate de **80% de cobertura** no Domain → **32 testes de integração** com PostgreSQL real → relatório de cobertura → `dotnet publish` → **artefato** |
| `secret-scan` | **Gitleaks** com histórico completo (`fetch-depth: 0`) |
| `docker-build` | build da imagem → **smoke test** (API + PostgreSQL, não-root) → **Trivy** (SARIF + gate CRITICAL/HIGH) |

**CD — `.github/workflows/release.yml`** (tag `vX.Y.Z`):

```
guard (tag SemVer + commit pertence à main) → verify (ci.yml reutilizado) → image (Trivy ANTES do push, GHCR, SBOM, provenance, digest) → github-release (versão ↔ commit ↔ digest)
```

## 5. Estratégia de deploy

*Continuous Delivery* com implantação por *pull*: o pipeline entrega automaticamente uma imagem versionada e escaneada, e o ambiente puxa a versão desejada **pelo digest**.

- `./scripts/deploy.sh <versão>`:
    1. resolve o digest pelo `release-manifest.json`;
    2. faz o pull **antes** de alterar o ambiente;
    3. faz **backup obrigatório** validado;
    4. roda o **smoke test** (saúde, versão e digest);
    5. só então atualiza a versão ativa;
    6. se algo falhar, faz **rollback automático**.
- `./scripts/rollback.sh`: volta para o **mesmo digest** registrado no histórico.
- Migrations **aditivas** (ADR-008): o rollback troca só a imagem e não reverte o banco.

## 6. Evidências do pipeline

### 6.1 Execução com falha (falha controlada)

| | |
|---|---|
| PR | [#29](https://github.com/renzovaz/flowboard-api/pull/29) — fechado **sem merge** |
| Execução | [45669612342](https://github.com/renzovaz/flowboard-api/actions/runs/45669612342) — `build-test`: **failure** |
| Causa | regressão introduzida de propósito em `WipPolicy`: `currentCount >= limit` → `currentCount > limit`. Com limite 3, a coluna aceitaria a 4ª Story (viola a R2) |
| Identificação | etapa "Testes unitários" com `Failed: 2, Passed: 73`. Os testes **U04** `EnsureCanEnter_WhenColumnIsAtLimit_ThrowsWipLimitExceeded` e **U05** falharam com `Assert.Throws() Failure: No exception was thrown`. O merge ficou **bloqueado** pelo ruleset |
| Correção | commit `fix(demo): restaura comparação inclusiva no limite de WIP` |

### 6.2 Execução bem-sucedida (após a correção)

| | |
|---|---|
| Execução | [45669995935](https://github.com/renzovaz/flowboard-api/actions/runs/45669995935): `build-test`, `secret-scan` e `docker-build` com **success** |
| Resultado | 75/75 unitários e 31/31 de integração aprovados |

### 6.3 Falha real não planejada (e corrigida)

| | |
|---|---|
| Execução | [45668328481](https://github.com/renzovaz/flowboard-api/actions/runs/45668328481) — `docker-build` com **failure** na `main` |
| Causa | no smoke test, a API subia antes de o PostgreSQL aceitar conexões, e a migration da inicialização encerrava o processo |
| Correção | [PR #28](https://github.com/renzovaz/flowboard-api/pull/28): a API aguarda o banco (ADR-027), e o smoke test aguarda o `pg_isready` e exibe os logs em caso de falha |

## 7. Artefato gerado

| Versão | Commit | Digest | Release |
|---|---|---|---|
| 1.0.0 | `5f57a14` | `sha256:268a1cb28e771fa05e1c9a0fee6397770dc5b6676a4a0c696071146a7fb1970c` | https://github.com/renzovaz/flowboard-api/releases/tag/v1.0.0 |
| 1.1.0 | `b10094e` | `sha256:7e27a1fe76cc910633dedff0f41523a5da4e02ffb878489653c76cdfabf12f17` | https://github.com/renzovaz/flowboard-api/releases/tag/v1.1.0 |

Pacote: https://github.com/renzovaz/flowboard-api/pkgs/container/flowboard-api (público). Os releases v1.0.0 e v1.1.0 terminaram com **success** em todos os jobs.

## 8. Deploy, atualização e rollback (execuções reais)

| Ação | Resultado |
|---|---|
| `deploy.sh 1.0.0` | primeira implantação, smoke test aprovado (versão 1.0.0 e digest conferidos). `/metrics` → 404 |
| `deploy.sh 1.1.0` | backup validado, smoke test aprovado, `/metrics` → 200, **dados preservados** |
| `rollback.sh` | volta ao **mesmo digest** da 1.0.0, `/metrics` → 404, dados intactos |
| deploy com artefato errado | o smoke test detecta a versão divergente e faz **rollback automático** para a 1.0.0 (código de saída 1) |

Saídas completas em `docs/evidence/deploy-rollback.md`.

## 9. Segurança e observabilidade

- **Segurança:** Push Protection; Gitleaks sem vazamentos; NuGet Audit sem pacotes vulneráveis; Trivy com 0 vulnerabilidades CRITICAL/HIGH; contêiner não-root com sistema de arquivos somente leitura e `cap_drop: ALL`; senha obrigatória sem valor padrão; actions fixadas por SHA; menor privilégio no `GITHUB_TOKEN`.
- **Observabilidade:**
  - `/health/live`, `/health/ready` e `/health` (com versão);
  - logs JSON com `traceId`;
  - banco indisponível → **503 `DATABASE_UNAVAILABLE`** e contêiner `unhealthy`;
  - `/metrics` com métricas HTTP e de fluxo Kanban.

## 10. Transparência

O desenvolvimento contou com apoio de IA (Claude, Anthropic), registrado em `docs/AI-ASSISTANCE.md`. Todas as execuções, releases e implantações citadas são reais e verificáveis pelos links.
