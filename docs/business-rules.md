# Regras de negócio

O FlowBoard implementa **oito regras de negócio (R1–R8)**, aprovadas no planejamento, e **duas restrições complementares (RC1, RC2)**. As restrições complementares **não fazem parte das regras originais**: existem apenas para impedir que as regras aprovadas sejam contornadas.

Cada regra é uma classe pura em `src/FlowBoard.Api/Domain/Rules` com testes próprios em `tests/FlowBoard.UnitTests/Rules`.

| Regra | Classe | Erro |
|---|---|---|
| R1 Fluxo Kanban | `KanbanWorkflow` | 409 `INVALID_TRANSITION` |
| R2 Limite de WIP | `WipPolicy` | 409 `WIP_LIMIT_EXCEEDED` |
| R3 Definition of Ready | `DefinitionOfReady` | 422 `DEFINITION_OF_READY_NOT_MET` |
| R4 Story Points | `StoryPointsPolicy` | 422 `INVALID_STORY_POINTS` / `STORY_TOO_LARGE` |
| R5 Capacidade | `CapacityPolicy` | 409 `CAPACITY_EXCEEDED` |
| R6 Iteração IP | `IterationPolicy` | 422 `IP_ITERATION_NOT_PLANNABLE` / `INVALID_ITERATION` |
| R7 WSJF | `WsjfCalculator` | 422 `INVALID_WSJF_VALUE` |
| R8 Progresso | `ProgressCalculator` | — |
| RC1 | `Story.UpdatePlanning` | 422 `DEFINITION_OF_READY_VIOLATED` |
| RC2 | `Story.UpdatePlanning` | 409 `TEAM_CHANGE_NOT_ALLOWED` |

## R1 — Fluxo Kanban

```
Backlog ──► Ready ──► InProgress ──► Review ──► Done
                          ▲             │
                          └─────────────┘  (retrabalho)
```

Qualquer outra transição (pular etapa, repetir o mesmo status, sair de `Done`) retorna **409**. *Adaptação:* no Kanban real cada equipe define suas políticas. Aqui elas são fixas.

## R2 — Limite de WIP

Uma Story só entra em `InProgress` ou `Review` se a quantidade de Stories **da mesma equipe** na coluna for **menor** que o limite (`ocupação >= limite` → 409). Quem sai da coluna libera a vaga, e o retrabalho também respeita o limite. A verificação é feita com **lock da equipe** (`SELECT ... FOR UPDATE`), então requisições simultâneas nunca ultrapassam o limite (ADR-002).

## R3 — Definition of Ready

Para passar de `Backlog` para `Ready`: pontos, critérios de aceite (≥ 10 caracteres não brancos), equipe e iteração. A resposta 422 lista **todos** os critérios pendentes em `missingCriteria`. *Observação:* a DoR é uma prática ágil comum, não um artefato formal do SAFe.

## R4 — Story Points

Opcionais no Backlog. Quando informados, pertencem a {1, 2, 3, 5, 8, 13}. Acima de 13 a Story deve ser dividida. Depois do Backlog, os pontos não podem voltar a nulo. *Adaptação:* a escala do SAFe inclui 20, 40 e 100. Aqui eles são proibidos para Stories, que devem caber em uma iteração.

## R5 — Capacidade por iteração

A soma dos pontos da equipe no mesmo PI e iteração não ultrapassa `capacityPoints` (o limite exato é permitido). Entram Stories em qualquer status. Na atualização, a própria Story não é contada duas vezes. *Adaptação:* a capacidade é fixa por equipe.

## R6 — Iteração IP

O PI tem N iterações (padrão 5), e a última é a **IP** (Innovation and Planning). Stories só podem ser planejadas de 1 a N−1. Data de início e número de iterações são imutáveis.

## R7 — WSJF

`Cost of Delay = Business Value + Time Criticality + Risk Reduction` e `WSJF = Cost of Delay ÷ Job Size`, na escala {1, 2, 3, 5, 8, 13, 20}, exatamente como no SAFe. O valor é calculado a cada leitura e nunca armazenado. A ordenação é determinística: WSJF ↓, Cost of Delay ↓, `CreatedAt` ↑, `Id` ↑.

## R8 — Progresso

`Progresso = pontos em Done ÷ pontos estimados × 100` (1 casa decimal), por Feature, por equipe e por PI. Stories sem pontos ficam fora do cálculo e aparecem em `unestimatedStories`. Total zero resulta em 0%.

## Restrições complementares

- **RC1:** depois do Backlog, `teamId`, `iteration`, `acceptanceCriteria` (e `storyPoints`, já previsto na R4) não podem ser apagados. Sem isso, uma Story em andamento sem equipe deixaria de contar no WIP (R2), e sem iteração deixaria de contar na capacidade (R5).
- **RC2:** a equipe só pode mudar em `Backlog` ou `Ready`. Sem isso, uma Story em `InProgress` poderia "pular" para uma equipe com WIP cheio.

## Conceitos de Kanban e SAFe utilizados

| Conceito | Original | No FlowBoard |
|---|---|---|
| Quadro Kanban | colunas e políticas definidas pela equipe | 5 colunas fixas |
| Limite de WIP | por etapa | por equipe e coluna (`InProgress`, `Review`) |
| Agile Team | equipe multifuncional (Scrum ou Team Kanban) | `Team` com capacidade e WIP. Pessoas não são modeladas |
| Program Increment | 8–12 semanas, 4 iterações + 1 IP | `ProgramIncrement` com N iterações, e a última é a IP |
| Feature e WSJF | priorização econômica no ART Backlog | fórmula idêntica, valores absolutos por Feature |
| User Story | cabe em uma iteração | `Story` com pontos ≤ 13 |
| Flow Metrics (SAFe 6) | Load, Velocity, Time, Distribution, Efficiency, Predictability | Load, Velocity e Time em `/metrics` (v1.1.0) |
| ART, Épicos, Portfólio, PI Objectives, ROAM | — | fora do escopo |
