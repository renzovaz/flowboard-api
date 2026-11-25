# Como contribuir

O projeto segue o **GitHub Flow**: a branch `main` está sempre implantável e toda mudança entra por Pull Request.

## Branches

Formato `tipo/descricao-curta`, com os tipos `feature`, `fix`, `refactor`, `test`, `ci`, `docs`, `chore` e `demo` (demonstrações que **não** são mescladas).

Exemplo: `feature/story-board-moves`.

## Commits

[Conventional Commits](https://www.conventionalcommits.org/pt-br/), com tipo e escopo em inglês e descrição em português, no imperativo:

```
feat(stories): adiciona movimentação no quadro com limite de WIP
fix(domain): restaura comparação inclusiva no limite de WIP
```

Cada commit representa uma mudança lógica que compila e passa nos testes.

## Pull Requests

1. Abra o PR para a `main` usando o template (regras afetadas, como testar e checklist).
2. Os checks obrigatórios do CI precisam passar. O ruleset bloqueia o merge caso contrário.
3. O merge usa **merge commit**, preservando os commits individuais no histórico.
4. Referencie a issue com `Closes #N`.

## Dependências

Ao alterar `Directory.Packages.props`, rode `dotnet restore` localmente e commite os `packages.lock.json` atualizados **no mesmo commit**. O CI usa `--locked-mode` e falha se os arquivos estiverem desatualizados.

## Migrations

Somente alterações **aditivas** (ADR-008). Remoções e renomeações seguem *Expand/Contract* em releases separadas.

## Checks obrigatórios

Os nomes dos jobs do CI (`build-test`, `secret-scan`, `docker-build`) são referenciados pelo ruleset da `main`. Renomear um job exige atualizar o ruleset.

## Colaboradores

Atualmente o projeto é desenvolvido individualmente. Quando um segundo integrante entrar, ele será adicionado como colaborador e ao `CODEOWNERS`, e o ruleset passará a exigir 1 aprovação.
