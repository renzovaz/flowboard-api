# Entrega, implantação e rollback

Este documento descreve como o artefato produzido pelo pipeline chega a um ambiente de execução, como atualizar a versão e como voltar a uma versão anterior.

## 1. Estratégia: Continuous Delivery com implantação por *pull*

| Etapa | Onde | Gatilho | Resultado |
|---|---|---|---|
| **Build** | `ci.yml` | PR, push na `main` | código compilado e binários publicados (artefato do Actions) |
| **Teste** | `ci.yml` | PR, push na `main` | testes unitários e de integração, cobertura, Gitleaks, NuGet Audit, Trivy |
| **Entrega** | `release.yml` | tag `vX.Y.Z` | imagem escaneada e publicada no GHCR, GitHub Release com versão, commit e digest |
| **Implantação** | `scripts/deploy.sh` | comando com a versão | contêiner da versão escolhida em execução e verificado |

Toda versão aprovada fica **pronta** para produção, e a decisão de implantar é humana. Não usamos *self-hosted runner* no computador local porque, em repositório público, código vindo de *forks* poderia executar comandos na máquina. Por isso a máquina de destino **puxa** a versão desejada.

## 2. Requisitos de infraestrutura

- Docker Engine com Docker Compose v2 (testado com Docker 29 e Compose 5).
- `bash`, `curl`, `jq` e `flock` (no Windows, usar o WSL).
- 2 GB de RAM livres e a porta **8080** livre (a porta é publicada apenas em `127.0.0.1`).
- Acesso a `ghcr.io` e `github.com` para baixar a imagem e o `release-manifest.json`.

## 3. Versionamento e identificação do artefato

- Versões **SemVer**. Uma release é uma tag anotada `vX.Y.Z` sobre um commit da `main`, e o job `guard` verifica isso.
- As tags Git `v*` são protegidas por ruleset: não podem ser alteradas nem apagadas.
- A imagem `ghcr.io/renzovaz/flowboard-api` recebe as tags `X.Y.Z`, `sha-<7>` e `latest`.
- **As tags de imagem são ponteiros mutáveis.** Por isso:
  - o release **recusa** publicar uma versão que já existe no GHCR;
  - o **digest** (`sha256:...`) é a referência definitiva. Ele é registrado no `release-manifest.json`, anexado ao GitHub Release, e usado pelo Compose (`IMAGE_REF=ghcr.io/...@sha256:...`).
- A imagem inclui **SBOM** e **provenance** (atestado de origem) gerados no build.

## 4. Primeira implantação

```bash
cp deploy/.env.example deploy/.env
# edite deploy/.env: POSTGRES_PASSWORD=<senha forte>   (openssl rand -base64 24)
docker compose down            # se o ambiente de desenvolvimento estiver rodando (mesma porta)
./scripts/deploy.sh 1.0.0
```

O script resolve o digest da versão, baixa a imagem, sobe banco e API e executa o smoke test: `/health/ready` deve responder 200, a versão em `/health` deve ser a pedida, e o contêiner precisa rodar o digest esperado.

## 5. Atualização de versão

```bash
./scripts/deploy.sh 1.1.0
```

1. Pré-validação (ferramentas, senha, lock contra deploys simultâneos, marcador `DEGRADED`).
2. Resolve o digest pelo `release-manifest.json` e faz o **pull antes** de mexer no que está rodando.
3. **Backup obrigatório** (`pg_dump -Fc`) em `deploy/backups/`, validado com `pg_restore --list`. **Se o backup falhar, o deploy é abortado** sem alterar nada.
4. Sobe a nova versão e roda o smoke test.
5. **Sucesso:** só então atualiza a versão ativa (`deploy/state/current`) e registra em `deployments.log`.
6. **Falha:** rollback **automático** para a versão ativa (mesmo digest), com novo smoke test.

## 6. Rollback

```bash
./scripts/rollback.sh          # volta para a implantação bem-sucedida anterior
./scripts/rollback.sh 1.0.0    # volta para uma versão específica
```

O rollback usa o **digest gravado no histórico** do deploy original, não a tag do registro. Isso garante o **mesmo artefato byte a byte**. Ele passa pelas mesmas garantias do deploy: backup, smoke test e retorno automático se o alvo falhar.

## 7. Registros do processo (`deploy/state/`, fora do Git)

| Arquivo | Conteúdo | Quando é escrito |
|---|---|---|
| `current` | versão e digest ativos e verificados | somente após smoke test aprovado |
| `image.env` | `IMAGE_REF` ativo, para comandos manuais | junto com `current` |
| `deployments.log` | implantações bem-sucedidas (`initial`, `deploy`, `rollback`, `auto-rollback`) | a cada sucesso |
| `attempts.log` | todas as tentativas e etapas (`STARTED`, `BACKUP_OK`, `SMOKE_FAILED`...) | durante o processo |
| `DEGRADED` | o rollback automático também falhou | estado crítico. Bloqueia novos deploys sem `--force` |

Códigos de saída do `deploy.sh`:

| Código | Significado |
|---|---|
| 0 | sucesso (ou versão já ativa) |
| 1 | falhou, versão anterior restaurada |
| 2 | falhou na primeira implantação |
| 3 | crítico: o rollback automático falhou |
| 4 | abortado antes de alterar o ambiente |

Comandos manuais sobre o ambiente implantado:

```bash
docker compose -f deploy/compose.yaml --env-file deploy/.env --env-file deploy/state/image.env ps
docker compose -f deploy/compose.yaml --env-file deploy/.env --env-file deploy/state/image.env logs -f api
```

## 8. Política de migrations e compatibilidade (ADR-008)

**Princípio:** a versão **N−1** precisa funcionar com o esquema criado pela versão **N**. O rollback troca **apenas a imagem**. Migrations **nunca** são revertidas automaticamente, e a imagem de runtime nem contém as ferramentas do EF para isso.

Isso funciona porque o EF Core só lê e grava as colunas que conhece. Tabelas e colunas novas (anuláveis ou com `DEFAULT`) são ignoradas pela versão antiga. Na inicialização, a versão antiga aplica apenas as próprias migrations pendentes (nenhuma).

| Permitido em uma release | Proibido junto com o código que depende da mudança |
|---|---|
| criar tabela | remover ou renomear tabela ou coluna |
| adicionar coluna anulável ou com `DEFAULT` | mudar o tipo de uma coluna |
| criar índice | `NOT NULL` sem `DEFAULT` em coluna nova |
| afrouxar uma restrição | endurecer `CHECK`/`UNIQUE` de forma que a versão antiga possa violar |

Mudanças destrutivas seguem ***Expand/Contract*** em releases separadas:
1. **Expand:** adiciona o novo e grava nos dois formatos.
2. **Migrate:** lê só o novo.
3. **Contract:** remove o antigo, depois de encerrada a janela de rollback.

O CI aplica uma **guarda automática** (`scripts/ci/check-migrations.sh`): uma migration nova com `Drop*`/`Rename*`/`AlterColumn` no `Up()` faz o pipeline falhar, a menos que o PR tenha o rótulo `breaking-migration`.

**Limitações desta estratégia:**
- Dados gravados pela versão N em colunas novas ficam invisíveis para N−1 durante o rollback.
- O rollback não corrige dados que já foram corrompidos por um bug da versão N.
- Mudanças de **comportamento** sem mudança de esquema não são protegidas por esta política.
- Para uma mudança destrutiva já aplicada, o único caminho é restaurar o backup, perdendo o que foi gravado depois dele.

## 9. Backup e restauração

Os backups são feitos automaticamente pelo `deploy.sh` antes de cada atualização. Restauração manual (decisão humana):

```bash
C="docker compose -f deploy/compose.yaml --env-file deploy/.env --env-file deploy/state/image.env"
$C stop api
$C exec -T db pg_restore -U flowboard -d flowboard --clean --if-exists < deploy/backups/<arquivo>.dump
$C start api && ./scripts/smoke-test.sh "$(cut -d' ' -f1 deploy/state/current)"
```

> A senha do PostgreSQL só é lida na **primeira criação do volume**. Para trocá-la depois, use `ALTER USER` ou recrie o volume.

## 10. Checklist de release

1. `CHANGELOG.md` com a seção da nova versão.
2. Migrations aditivas (guarda do CI verde).
3. CI verde na `main`.
4. Criar e enviar a tag: `git tag -a vX.Y.Z -m "..." && git push origin vX.Y.Z`.
5. Workflow **Release** verde (guard, verify, image, github-release).
6. Pacote do GHCR com visibilidade **pública** (só na primeira publicação).
7. `./scripts/deploy.sh X.Y.Z` com smoke test aprovado.
