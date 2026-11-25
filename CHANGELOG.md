# Changelog

Todas as mudanças relevantes do FlowBoard API. Formato baseado em [Keep a Changelog](https://keepachangelog.com/pt-BR/1.1.0/) e versionamento [SemVer](https://semver.org/lang/pt-BR/).

Cada versão publicada gera uma imagem no GHCR identificada por **versão, commit e digest** (veja o GitHub Release correspondente). O deploy e o rollback usam sempre o **digest**.

## [1.0.0] - 2026-11-25

MVP completo.

### Adicionado
- Entidades `Team`, `ProgramIncrement`, `Feature` e `Story`, persistidas em PostgreSQL 17 com EF Core 10.
- Regras de negócio R1–R8: fluxo Kanban, limite de WIP por equipe e coluna, Definition of Ready, Story Points em Fibonacci, capacidade por iteração, iteração IP, priorização WSJF e progresso por pontos.
- Restrições complementares RC1 (campos da DoR não podem ser apagados depois do Backlog) e RC2 (troca de equipe só em Backlog/Ready).
- 18 endpoints REST, incluindo quadro Kanban, capacidade por iteração e progresso do PI.
- Controle de concorrência: lock `FOR UPDATE` por equipe (WIP e capacidade) e concorrência otimista (`xmin`) por Story.
- Erros no padrão Problem Details com `errorCode` e `traceId`. Banco indisponível retorna 503.
- Health checks `/health/live`, `/health/ready` e `/health` (com versão), e logs estruturados em JSON.
- Imagem Docker multi-stage, não-root, com `HEALTHCHECK`.
- Pipelines de CI (build, testes, cobertura, Gitleaks, NuGet Audit, Trivy) e de release (GHCR, digest, GitHub Release).
- Scripts de deploy, rollback e smoke test por digest, e script de dados de demonstração.
