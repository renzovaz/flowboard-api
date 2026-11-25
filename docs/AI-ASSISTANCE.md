# Uso de Inteligência Artificial no desenvolvimento

Este documento registra, de forma transparente, como a Inteligência Artificial foi usada no FlowBoard API.

## Ferramenta

- **Assistente:** Claude (Anthropic), via Claude Code.
- **Período:** 18 a 30 de novembro de 2026.

## Onde houve apoio da IA

| Atividade | Apoio |
|---|---|
| Análise dos requisitos da disciplina e escolha do tema | proposta e comparação de alternativas |
| Planejamento técnico e arquitetura | elaboração das especificações e dos ADRs (`docs/decisions.md`) |
| Implementação | geração do código C#, testes, workflows do GitHub Actions, Dockerfile, Compose e scripts |
| Documentação | README e documentos em `docs/` |
| Operação do repositório | branches, commits, Pull Requests, configuração de rulesets e releases, executados pelo assistente com autorização do responsável pelo projeto |

## Responsabilidade humana

- Definição do tema, do escopo e das diretrizes, e aprovação do planejamento.
- Revisão do resultado, execução e demonstração do projeto na apresentação.
- Os commits são registrados com a identidade Git do responsável pelo projeto. Em 25/11/2026 a identidade do repositório foi padronizada (Renzo Machado, conta `renzovaz`). Os commits anteriores mantêm a identidade global então configurada na máquina, **sem reescrita de histórico**.

## Verificação e integridade

- **Nenhuma evidência foi simulada.** Execuções do GitHub Actions (inclusive a falha controlada), imagens no GHCR, releases, deploys e rollbacks são reais e verificáveis pelos links do repositório.
- Todo código passou pelos mesmos controles automáticos: build com analyzers, testes unitários e de integração, cobertura, Gitleaks, NuGet Audit e Trivy.
- Afirmações sobre testes e pipelines foram feitas somente após a execução e a verificação dos resultados.

## Conformidade acadêmica

O uso de IA deve respeitar as regras da disciplina e da instituição. **Cabe ao responsável pelo projeto confirmar com o professor** se é exigida alguma forma específica de declaração além deste documento.

## Aprendizado

Para apresentar e explicar o projeto, o responsável estudou os conceitos aplicados: C# e ASP.NET Core (camadas, injeção de dependência, async/await), Entity Framework Core e migrations, testes com xUnit e Testcontainers, GitHub Actions, Docker, GHCR e as práticas de DevSecOps e observabilidade adotadas.
