# Registro de decisões arquiteturais (ADRs)

Cada decisão registra o **contexto**, a **escolha** e o **motivo**. As alternativas descartadas aparecem quando ajudam a entender a escolha.

| ADR | Decisão | Motivo |
|---|---|---|
| **001** | IDs `int` (identity) e `CreatedAt` em todas as entidades. WSJF desempata por Cost of Delay ↓, `CreatedAt` ↑ e `Id` ↑ | Legível na demonstração (`/api/stories/7`). O desempate é determinístico e usa a data real de criação. O `Id` é só o último critério técnico. Risco de enumeração aceito: não há autenticação nem dados sensíveis. |
| **002** | Lock pessimista `SELECT ... FOR UPDATE` na linha da equipe para R2 (WIP) e R5 (capacidade). Concorrência otimista (`xmin`) na Story | Determinístico e sem *retry*: a segunda requisição da mesma equipe espera e conta o valor já confirmado. Equipes diferentes não se bloqueiam. Duas equipes são travadas em ordem crescente de Id, o que evita *deadlock*. Comprovado pelo teste I14: sem o lock, 9 a 10 de 10 requisições simultâneas foram aceitas com limite 3. |
| **003** | .NET 10 LTS e PostgreSQL 17 | Suporte longo (.NET 10 até 11/2028). O .NET 11 é STS e recente. A imagem do PostgreSQL 18 mudou o caminho de dados. |
| **004** | Restrição complementar RC2: a equipe de uma Story só muda em `Backlog`/`Ready` (409) | Impede que a troca de equipe burle a R2. **Não faz parte das 8 regras originais.** |
| **005** | Reduzir capacidade ou WIP abaixo da ocupação atual é permitido | Mesmo comportamento do Kanban: os itens atuais continuam, e só novas entradas são bloqueadas. |
| **006** | Nomes de equipe e PI únicos (409). Faixas de validação: capacidade 1–200, WIP 1–20, iterações 2–6, duração 1–4 semanas | Integridade e clareza. As faixas também existem como `CHECK` no banco (defesa em profundidade). |
| **007** | `StartedAt` guarda a primeira entrada em `InProgress` | O *cycle time* fica correto mesmo com retrabalho (Review → InProgress). |
| **008** | Migrations **aditivas** e compatíveis com a versão anterior. Rollback nunca reverte o banco. Mudanças destrutivas via *Expand/Contract* | Permite rollback só trocando a imagem. Há uma guarda automática no CI (`scripts/ci/check-migrations.sh`). Limitações em `docs/deployment.md`. |
| **009** | `packages.lock.json` versionados e `dotnet restore --locked-mode` no CI e no Dockerfile | Builds reproduzíveis: o mesmo commit sempre usa os mesmos pacotes, e qualquer mudança aparece no diff do PR. |
| **010** | Dockerfile multi-stage | A imagem final não contém SDK nem código-fonte. |
| **011** | Runtime `aspnet:10.0-alpine` (fixado por digest) | Enxuta e com o `wget` necessário ao `HEALTHCHECK`. A *chiseled* foi descartada porque não tem *shell* para o health check sem código extra na aplicação. |
| **012** | Usuário não-root (`app`, UID 1654) | Se comprometida, a aplicação não tem privilégios de administrador no contêiner. |
| **013** | Porta 8080 | Padrão das imagens .NET. Não exige root. |
| **014** | Dois Compose: `compose.yaml` (dev, **compila**) e `deploy/compose.yaml` (implantação, **baixa** do GHCR) | Separa construção de implantação. |
| **015** | Um único usuário de banco (dono do esquema) | Necessário para aplicar migrations na inicialização. Separar usuário de DDL e de DML fica como melhoria futura (risco aceito). |
| **016** | Sem retry automático do Npgsql (`EnableRetryOnFailure`) | O retry não pode ser usado com transações iniciadas pelo código (lock `FOR UPDATE`) sem envolver toda a unidade de trabalho em `strategy.ExecuteAsync(...)`, que refaria lock, contagem e gravação. Até isso ser implementado, falhas transitórias retornam 503 com `Retry-After`. |
| **017** | Classificação explícita de falhas de banco no handler global: indisponível → 503, `UNIQUE` → 409, concorrência → 409, `CHECK`/`FK` → 500 | Não presumir que o framework retorna 503. Integridade violada no banco indica erro de programação (o domínio deveria ter barrado). |
| **018** | Swagger/OpenAPI desligados por padrão em Production e habilitados explicitamente (`SWAGGER_ENABLED=true`) para a demonstração, com log de aviso 3001 | Menor superfície exposta por padrão. |
| **019** | Senha do PostgreSQL obrigatória e sem valor de exemplo (`POSTGRES_PASSWORD=` vazio + `${VAR:?}`) | O Compose se recusa a subir sem senha. Não existe senha padrão que possa ser usada por engano. |
| **020** | Versionamento SemVer por tag anotada `vX.Y.Z` na `main`. Tags Git `v*` imutáveis por ruleset | Release rastreável e irreversível. |
| **021** | **Digest** como referência definitiva de deploy e rollback. O release recusa republicar uma versão existente e gera `release-manifest.json` (versão ↔ commit ↔ digest) anexado ao GitHub Release | As tags de imagem do registro são ponteiros mutáveis. O digest garante o mesmo artefato byte a byte. |
| **022** | `ci.yml` reutilizável (`workflow_call` com `is-release`). No release, só `build-test` roda no commit da tag | Sem duplicar build, testes e auditorias. O chamador concede as permissões máximas usadas pelos jobs do CI. |
| **023** | Deploy com pull antes de alterar o ambiente, backup obrigatório e validado, versão ativa atualizada só após smoke test, rollback automático, registro separado de tentativas e sucessos, e marcador `DEGRADED` | Integridade do processo de implantação (`docs/deployment.md`). |
| **024** | Implantação por *pull* (Continuous Delivery), sem *self-hosted runner* | Em repositório público, *forks* poderiam executar código na máquina do runner. |
| **025** | Uma única aplicação com camadas por pastas (`Controllers`, `Contracts`, `Services`, `Domain`, `Data`, `Infrastructure`). Sem Repository Pattern, AutoMapper ou FluentValidation | O `DbContext` já é repositório e unidade de trabalho. As regras são classes puras testáveis sem banco. Menos indireção para quem está aprendendo. |
| **026** | Métricas com `prometheus-net` (v1.1.0) | Estável e simples. O exportador Prometheus do OpenTelemetry para ASP.NET Core ainda está em pré-lançamento. |
| **027** | Na inicialização, a aplicação **aguarda o banco** (até ~60 s, 20 × 3 s) antes de aplicar as migrations, somente para falhas de indisponibilidade | Corrige uma falha real do CI na `main` (smoke test com a API subindo antes do PostgreSQL, PR #28). Não altera o ADR-016: as transações das requisições continuam sem retry automático. |
