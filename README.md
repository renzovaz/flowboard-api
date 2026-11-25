# FlowBoard API

[![CI](https://github.com/renzovaz/flowboard-api/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/renzovaz/flowboard-api/actions/workflows/ci.yml)
[![Release](https://github.com/renzovaz/flowboard-api/actions/workflows/release.yml/badge.svg)](https://github.com/renzovaz/flowboard-api/actions/workflows/release.yml)

API REST para **gestão ágil do fluxo de trabalho**, com **Kanban** como núcleo funcional e **conceitos selecionados do SAFe** (Program Increment, iterações, iteração IP, Features priorizadas por WSJF e capacidade das equipes).

Projeto da disciplina **Integração DevOps** (Engenharia de Software). O objetivo principal é demonstrar, na prática, o ciclo completo de **DevOps e DevSecOps**: versionamento com GitHub Flow, testes automatizados, integração contínua, artefato Docker publicado no GHCR, implantação e rollback por digest, segurança da cadeia de entrega e observabilidade.

## Sumário

1. [Integrantes](#integrantes)
2. [A aplicação](#a-aplicação)
3. [Tecnologias](#tecnologias)
4. [Executar a aplicação](#executar-a-aplicação)
5. [Testar a API](#testar-a-api)
6. [Endpoints](#endpoints)
7. [Testes automatizados](#testes-automatizados)
8. [Versionamento e branches](#versionamento-e-branches)
9. [Pipeline de CI](#pipeline-de-ci)
10. [Artefato: imagem Docker no GHCR](#artefato-imagem-docker-no-ghcr)
11. [Entrega, implantação e rollback (CD)](#entrega-implantação-e-rollback-cd)
12. [Segurança (DevSecOps)](#segurança-devsecops)
13. [Observabilidade](#observabilidade)
14. [Evidências](#evidências)
15. [Estrutura do repositório](#estrutura-do-repositório)
16. [Créditos e transparência](#créditos-e-transparência)

## Integrantes

| Nome | GitHub |
|---|---|
| Renzo Machado | [@renzovaz](https://github.com/renzovaz) |

> O desenvolvimento foi **individual**. O repositório está preparado para um segundo integrante (`CONTRIBUTING.md`, `CODEOWNERS`, template de PR), sem simular colaborações que não existiram.

## A aplicação

**Problema:** equipes ágeis começam muito trabalho e terminam pouco, planejam acima da capacidade, priorizam por opinião e têm pouca visibilidade do progresso.

**Solução:** o FlowBoard aplica regras automáticas a esses pontos:

| Regra | Descrição |
|---|---|
| R1 | Fluxo Kanban `Backlog → Ready → InProgress → Review → Done` (retrabalho `Review → InProgress`) |
| R2 | **Limite de WIP** por equipe e coluna, seguro sob requisições simultâneas |
| R3 | **Definition of Ready** para entrar em `Ready` |
| R4 | Story Points em Fibonacci (1–13). Acima disso, a Story deve ser dividida |
| R5 | **Capacidade** da equipe por iteração |
| R6 | A **iteração IP** (última do PI) não aceita Stories |
| R7 | Priorização de Features por **WSJF** (fórmula do SAFe) |
| R8 | **Progresso** por pontos, por Feature, equipe e PI |

Detalhes, exemplos e as restrições complementares RC1/RC2 estão em [`docs/business-rules.md`](docs/business-rules.md).

## Tecnologias

| Área | Ferramentas |
|---|---|
| Aplicação | C# 14, **ASP.NET Core 10** (LTS), Entity Framework Core 10, Npgsql |
| Banco | **PostgreSQL 17** |
| Testes | **xUnit v3**, WebApplicationFactory, **Testcontainers**, coverlet, ReportGenerator |
| Documentação da API | OpenAPI + Swagger UI |
| Versionamento | Git, GitHub (GitHub Flow, rulesets, issues, PRs) |
| CI/CD | **GitHub Actions** (`ci.yml`, `release.yml`) |
| Contêineres | **Docker** (multi-stage, não-root), **Docker Compose** |
| Registro | **GitHub Container Registry (GHCR)** |
| Segurança | .NET Analyzers, **NuGet Audit**, **Gitleaks**, **Trivy**, Push Protection, Dependabot |
| Observabilidade | logs JSON, Health Checks, Prometheus (`/metrics`, via prometheus-net) |

## Executar a aplicação

**Pré-requisitos:** Docker com Docker Compose v2. Para os scripts: `bash`, `curl` e `jq` (no Windows, use o WSL). Para desenvolver ou rodar os testes: .NET SDK 10.0.404 (`global.json`).

### Opção A — imagem publicada (implantação)

```bash
git clone https://github.com/renzovaz/flowboard-api.git && cd flowboard-api
cp deploy/.env.example deploy/.env
# edite deploy/.env: POSTGRES_PASSWORD=<senha forte>  (openssl rand -base64 24)
#                    SWAGGER_ENABLED=true              (opcional, para explorar pelo Swagger)
./scripts/deploy.sh 1.1.0
```

### Opção B — a partir do código (desenvolvimento)

```bash
cp .env.example .env            # defina POSTGRES_PASSWORD
docker compose up -d --build
docker compose ps               # api e db devem ficar "healthy"
```

Nas duas opções, a API fica em **http://localhost:8080** e o Swagger em **http://localhost:8080/swagger** (quando habilitado).

### Variáveis de ambiente

| Variável | Onde | Descrição |
|---|---|---|
| `POSTGRES_PASSWORD` | `.env` / `deploy/.env` | **Obrigatória.** Sem valor, o Compose não sobe |
| `ConnectionStrings__Default` | Compose | conexão com o PostgreSQL, montada com a senha |
| `ASPNETCORE_ENVIRONMENT` | Compose | `Development` ou `Production` |
| `Database__ApplyMigrationsOnStartup` | Compose | aplica as migrations na inicialização |
| `Swagger__Enabled` / `SWAGGER_ENABLED` | Compose / `deploy/.env` | Swagger desligado por padrão em Production |
| `IMAGE_REF` | `deploy/state/image.env` | imagem **por digest**, definida pelos scripts de deploy |

## Testar a API

```bash
./scripts/seed-demo.sh                                   # cria equipes, PI, Features e Stories pela API
curl -s localhost:8080/api/program-increments/1/features | jq   # priorização WSJF
curl -s localhost:8080/api/teams/1/board | jq                   # quadro Kanban (Alpha: InProgress 2/3)
curl -s -X PATCH localhost:8080/api/stories/6/status -H 'Content-Type: application/json' -d '{"status":"InProgress"}'  # 3/3
curl -s -X PATCH localhost:8080/api/stories/7/status -H 'Content-Type: application/json' -d '{"status":"InProgress"}'  # 409 WIP
curl -s localhost:8080/api/program-increments/1/progress | jq   # progresso (R8)
curl -s localhost:8080/health | jq                              # saúde e versão
```

Também é possível usar o Swagger ou o arquivo [`src/FlowBoard.Api/FlowBoard.Api.http`](src/FlowBoard.Api/FlowBoard.Api.http).

## Endpoints

| Método | Rota | Descrição | Regras |
|---|---|---|---|
| POST | `/api/teams` | cria equipe | faixas, nome único |
| GET | `/api/teams` · `/api/teams/{id}` | lista / consulta | — |
| PUT | `/api/teams/{id}` | atualiza capacidade e WIP | faixas |
| GET | `/api/teams/{id}/board` | quadro Kanban com ocupação de WIP | R2 |
| GET | `/api/teams/{id}/capacity?programIncrementId=` | carga × capacidade por iteração | R5, R6 |
| POST | `/api/program-increments` | cria PI (a última iteração é a IP) | R6 |
| GET | `/api/program-increments` · `/{id}` | lista / consulta com as datas das iterações | — |
| GET | `/api/program-increments/{id}/progress` | progresso por Feature, equipe e PI | R8 |
| POST | `/api/program-increments/{id}/features` | cria Feature | R7 |
| GET | `/api/program-increments/{id}/features` | backlog ordenado por WSJF | R7 |
| GET · PUT | `/api/features/{id}` | consulta / atualiza | R7 |
| POST | `/api/features/{id}/stories` | cria Story (Backlog) | R4, R5, R6 |
| GET · PUT | `/api/stories/{id}` | consulta / atualiza planejamento | R4, R5, R6, RC1, RC2 |
| PATCH | `/api/stories/{id}/status` | move no quadro | R1, R2, R3 |
| GET | `/health` · `/health/live` · `/health/ready` | saúde (versão, liveness, readiness) | — |
| GET | `/metrics` | métricas Prometheus (**v1.1.0**) | — |

Erros seguem o padrão **Problem Details** com `errorCode` e `traceId`. Catálogo completo em [`docs/errors.md`](docs/errors.md).

## Testes automatizados

| Projeto | Tipo | O que cobre |
|---|---|---|
| `tests/FlowBoard.UnitTests` | unitários (milissegundos, sem Docker) | as 8 regras, RC1/RC2 e a classificação de falhas de banco |
| `tests/FlowBoard.IntegrationTests` | integração (API real + **PostgreSQL 17 real** via Testcontainers) | endpoints, códigos HTTP, constraints, `xmin`, **concorrência do WIP**, health checks, **banco indisponível → 503**, `/metrics` |

```bash
dotnet test tests/FlowBoard.UnitTests        # 75 casos
dotnet test tests/FlowBoard.IntegrationTests # 32 testes (requer Docker)
```

- **Gate de cobertura:** mínimo de **80% de linhas** no namespace `Domain`. Abaixo disso, o pipeline falha.
- **Teste de concorrência (I14):** 10 requisições simultâneas com WIP 3 resultam em exatamente 3 aceitas e 7 recusadas. Removendo o lock, o teste **falha** (9 a 10 aceitas), o que comprova que ele realmente protege a regra.
- Um teste falhando derruba o job `build-test`, que é **check obrigatório** do ruleset da `main`. O merge fica bloqueado.

## Versionamento e branches

- **GitHub Flow:** a `main` está sempre implantável, e toda mudança entra por **Pull Request** a partir de `feature/*`, `fix/*`, `ci/*`, `docs/*` ou `chore/*`. Branches `demo/*` são demonstrações e **não** são mescladas.
- **Conventional Commits** (`feat(api): ...`, `fix(domain): ...`), com descrição em português.
- **Merge commit:** preserva os commits individuais e o "Merge pull request #N" no histórico.
- **Ruleset da `main`:** PR obrigatório, checks `build-test`, `secret-scan` e `docker-build` obrigatórios, branch atualizada antes do merge, sem *force push*, sem exclusão e **sem bypass** (nem para o administrador).
- **Tags `v*` imutáveis** (ruleset) e branches `demo/**` protegidas contra exclusão (preservação de evidências).
- Issues e milestones (`v1.0.0`, `v1.1.0`) ligadas aos PRs (`Closes #N`).

Guia completo em [`CONTRIBUTING.md`](CONTRIBUTING.md).

## Pipeline de CI

Arquivo [`.github/workflows/ci.yml`](.github/workflows/ci.yml). Dispara em PR e push para a `main`, toda segunda-feira (novas CVEs), manualmente e via `workflow_call` (reutilizado pelo release).

| Job | Etapas |
|---|---|
| **build-test** | checkout → setup .NET (`global.json`, cache) → `restore --locked-mode` (**NuGet Audit**) → relatório de vulneráveis → guarda de migrations (PR) → `dotnet format --verify-no-changes` → build Release (**analyzers, avisos = erro**) → testes unitários + **gate de cobertura** → testes de integração (Testcontainers) → relatório de cobertura → upload dos resultados → `dotnet publish` → **artefato `flowboard-api-<sha>`** |
| **secret-scan** | checkout com histórico completo → **Gitleaks** (commits do PR/push, e histórico inteiro no agendamento) |
| **docker-build** | build da imagem → **smoke test** (API + PostgreSQL, `/health` e usuário não-root) → **Trivy** (SARIF para a aba Security e gate CRITICAL/HIGH) |

Boas práticas: `permissions: contents: read` (menor privilégio), actions fixadas por **SHA**, `concurrency` cancelando execuções antigas de PR e `timeout-minutes` em todos os jobs.

## Artefato: imagem Docker no GHCR

- **Imagem:** [`ghcr.io/renzovaz/flowboard-api`](https://github.com/renzovaz/flowboard-api/pkgs/container/flowboard-api), com as tags `X.Y.Z`, `sha-<7>` e `latest`.
- **Dockerfile multi-stage:** SDK só no build. Runtime `aspnet:10.0-alpine` fixado por digest, usuário **não-root** (UID 1654), porta 8080, `HEALTHCHECK` em `/health/ready` e labels OCI.
- **Rastreabilidade:** cada [GitHub Release](https://github.com/renzovaz/flowboard-api/releases) traz **versão ↔ commit ↔ digest** e o `release-manifest.json`. A imagem inclui **SBOM** e **provenance**.

```bash
docker pull ghcr.io/renzovaz/flowboard-api@<digest do release>
```

## Entrega, implantação e rollback (CD)

| Etapa | Onde | Gatilho |
|---|---|---|
| Build e teste | `ci.yml` | PR / push |
| **Entrega** | `release.yml`: `guard` (tag SemVer, commit pertence à `main`) → `verify` (CI reutilizado) → `image` (Trivy **antes** do push, GHCR, digest) → `github-release` | tag `vX.Y.Z` |
| **Implantação** | `scripts/deploy.sh <versão>` (pull, backup, smoke test, rollback automático) | comando |

```bash
./scripts/deploy.sh 1.0.0      # primeira implantação
./scripts/deploy.sh 1.1.0      # atualização (backup obrigatório antes)
./scripts/rollback.sh          # volta para o MESMO digest da versão anterior
```

- O deploy usa sempre o **digest** (as tags do registro são mutáveis).
- A versão ativa só muda **depois do smoke test**: `/health/ready` em 200, versão correta e digest correto.
- Migrations são **aditivas** e o rollback **não** reverte o banco (ADR-008). Limitações e procedimentos em [`docs/deployment.md`](docs/deployment.md).

## Segurança (DevSecOps)

| Prática | Ferramenta | O que verifica |
|---|---|---|
| Segredos fora do código | `.env` no `.gitignore`, `.env.example` com senha **vazia**, `${VAR:?}` | o Compose não sobe sem senha, e não existe senha padrão |
| Prevenção de vazamento | **GitHub Push Protection** | **bloqueia o push** de segredos conhecidos |
| Detecção de vazamento | **Gitleaks** (CI) | segredos nos commits e em todo o histórico (agendado) |
| Dependências vulneráveis | **NuGet Audit** (restore) + Dependabot | CVEs em pacotes diretos e transitivos. Moderada ou acima quebra o build |
| Imagem vulnerável | **Trivy** (CI e release) | CVEs do sistema e do .NET na imagem. CRITICAL/HIGH com correção bloqueia a publicação |
| Qualidade e segurança do código | **.NET Analyzers** (avisos = erro) | regras recomendadas de confiabilidade e segurança |
| *Supply chain* | actions por SHA, imagens base por digest, `nuget.config` só com nuget.org, lock files | dependências imutáveis e de origem conhecida |
| Contêiner | não-root, `read_only`, `cap_drop: ALL`, `no-new-privileges`, banco sem porta exposta | superfície mínima em execução |
| Pipeline | `GITHUB_TOKEN` com menor privilégio por job, sem token pessoal | credenciais mínimas e temporárias |
| Erros | Problem Details sem detalhes internos | nada de *stack trace* na resposta |

## Observabilidade

- **Health Checks:** `/health/live`, `/health/ready` (PostgreSQL) e `/health` (relatório com **versão**).
- **Logs JSON** com `traceId` (o mesmo do erro devolvido ao cliente) e eventos de negócio (`StoryMoved`, `WipLimitRejected`) e de falha (`DatabaseUnavailable`).
- **Banco indisponível → 503 `DATABASE_UNAVAILABLE`**, com a API `unhealthy` no Docker.
- **`/metrics` (v1.1.0):** métricas HTTP e de fluxo (Flow Load, Velocity e Time).

Detalhes e roteiro de diagnóstico em [`docs/observability.md`](docs/observability.md).

## Evidências

| Evidência | Link |
|---|---|
| Execução **com falha** (falha controlada, PR #29) | [run 45669612342](https://github.com/renzovaz/flowboard-api/actions/runs/45669612342): U04 e U05 falham, merge bloqueado |
| Execução **com sucesso** após a correção | [run 45669995935](https://github.com/renzovaz/flowboard-api/actions/runs/45669995935): 75/75 unitários, 31/31 integração |
| Falha **real não planejada** na `main` e sua correção | [run 45668328481](https://github.com/renzovaz/flowboard-api/actions/runs/45668328481) → [PR #28](https://github.com/renzovaz/flowboard-api/pull/28) |
| Histórico do CI | [Actions › CI](https://github.com/renzovaz/flowboard-api/actions/workflows/ci.yml) |
| Releases (versão ↔ commit ↔ digest) | [v1.0.0](https://github.com/renzovaz/flowboard-api/releases/tag/v1.0.0) · [v1.1.0](https://github.com/renzovaz/flowboard-api/releases/tag/v1.1.0) |
| Imagem no GHCR | [ghcr.io/renzovaz/flowboard-api](https://github.com/renzovaz/flowboard-api/pkgs/container/flowboard-api) |
| Varreduras de segurança (Trivy) | [Security › Code scanning](https://github.com/renzovaz/flowboard-api/security/code-scanning) |
| Pull Requests | [PRs mesclados](https://github.com/renzovaz/flowboard-api/pulls?q=is%3Apr+is%3Amerged) |

Relatos detalhados, com saídas reais:
- [`docs/evidence/ci-failure.md`](docs/evidence/ci-failure.md): falha controlada (motivo, identificação, correção) e falha real na `main`;
- [`docs/evidence/deploy-rollback.md`](docs/evidence/deploy-rollback.md): deploy da 1.0.0, atualização para a 1.1.0, rollback e rollback automático.

## Estrutura do repositório

```
.github/workflows/   ci.yml, release.yml          · .github/  dependabot, template de PR, CODEOWNERS
src/FlowBoard.Api/   Controllers · Contracts (DTOs) · Services · Domain (Entities, Rules, Exceptions)
                     Data (DbContext, Configurations, Migrations) · Infrastructure (erros, health, logs, métricas)
tests/               FlowBoard.UnitTests · FlowBoard.IntegrationTests
deploy/              compose.yaml (implantação por digest) · .env.example
scripts/             deploy.sh · rollback.sh · smoke-test.sh · seed-demo.sh · ci/check-migrations.sh
docs/                business-rules · errors · deployment · observability · decisions (ADRs) · AI-ASSISTANCE · evidence
Dockerfile · compose.yaml (dev) · global.json · Directory.Build.props · Directory.Packages.props · nuget.config
```

Decisões arquiteturais justificadas em [`docs/decisions.md`](docs/decisions.md).

## Créditos e transparência

Este projeto foi desenvolvido com apoio de Inteligência Artificial. O uso está registrado em [`docs/AI-ASSISTANCE.md`](docs/AI-ASSISTANCE.md).

Licença [MIT](LICENSE).
