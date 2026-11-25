# Evidência: release, implantação, atualização e rollback

Execuções reais em 25/11/2026, num ambiente local (WSL + Docker), usando as imagens publicadas no GHCR.

## Artefatos publicados

| Versão | Commit | Digest | Release | Workflow |
|---|---|---|---|---|
| 1.0.0 | `5f57a14` | `sha256:268a1cb28e771fa05e1c9a0fee6397770dc5b6676a4a0c696071146a7fb1970c` | [v1.0.0](https://github.com/renzovaz/flowboard-api/releases/tag/v1.0.0) | [45669035765](https://github.com/renzovaz/flowboard-api/actions/runs/45669035765) |
| 1.1.0 | `b10094e` | `sha256:7e27a1fe76cc910633dedff0f41523a5da4e02ffb878489653c76cdfabf12f17` | [v1.1.0](https://github.com/renzovaz/flowboard-api/releases/tag/v1.1.0) | [45670463565](https://github.com/renzovaz/flowboard-api/actions/runs/45670463565) |

Nos dois releases, todos os jobs (`guard`, `verify / build-test`, `image`, `github-release`) terminaram com **success**. No modo release, `verify / secret-scan` e `verify / docker-build` são **pulados** por desenho (ADR-022). O pacote [`ghcr.io/renzovaz/flowboard-api`](https://github.com/renzovaz/flowboard-api/pkgs/container/flowboard-api) é público: o pull anônimo do manifesto pelo digest retornou HTTP 200.

## 1. Primeira implantação — `./scripts/deploy.sh 1.0.0`

```
[deploy] resolvendo digest em https://github.com/renzovaz/flowboard-api/releases/download/v1.0.0/release-manifest.json
[deploy] baixando ghcr.io/renzovaz/flowboard-api@sha256:268a1cb2…
[deploy] primeira implantação (sem versão anterior e sem volume de dados): backup não necessário
[smoke] versão em execução: 1.0.0 ✔
[smoke] digest em execução: sha256:268a1cb2… ✔
[deploy] SUCESSO: versão ativa = 1.0.0
```

Depois do `seed-demo.sh`: `/health` → `{"status":"Healthy","version":"1.0.0"}` e `/metrics` → **404** (o endpoint não existe na 1.0.0).

## 2. Atualização — `./scripts/deploy.sh 1.1.0`

```
[deploy] backup obrigatório do banco → deploy/backups/20261125T181020Z-antes-de-1.1.0.dump
[deploy] implantando 1.1.0 (sha256:7e27a1fe…)
[smoke] versão em execução: 1.1.0 ✔
[smoke] digest em execução: sha256:7e27a1fe… ✔
[deploy] SUCESSO: versão ativa = 1.1.0
```

- `/health` → `1.1.0`, e `/metrics` → **200** (`flowboard_wip_current{team="1",column="InProgress"} 2`, entre outras séries).
- Quadro da equipe Alpha **idêntico** antes e depois (Ready 2, InProgress 2, Review 1, Done 2): os dados foram preservados.

## 3. Rollback — `./scripts/rollback.sh`

```
[rollback] 1.1.0 → 1.0.0 sha256:268a1cb2…   ← digest lido do histórico, não da tag
[deploy] backup obrigatório do banco → deploy/backups/20261125T181035Z-antes-de-1.0.0.dump
[smoke] versão em execução: 1.0.0 ✔
[smoke] digest em execução: sha256:268a1cb2… ✔
[deploy] SUCESSO: versão ativa = 1.0.0
```

`/metrics` voltou a responder **404** e os dados ficaram intactos. O banco **não** foi revertido (ADR-008): a 1.1.0 não tem migrations.

## 4. Falha no deploy com rollback automático

Para provar o caminho de falha, foi pedido ao script um deploy da versão `1.0.0` com o **digest da 1.1.0** (simulando um artefato errado):

```
$ ./scripts/deploy.sh 1.0.0 --digest sha256:7e27a1fe…
[smoke] FALHA: versão em execução '1.1.0' ≠ esperada '1.0.0'
[deploy] FALHA no smoke test da versão 1.0.0
[deploy] rollback automático para 1.0.0 (sha256:268a1cb2…)
[smoke] versão em execução: 1.0.0 ✔
[smoke] digest em execução: sha256:268a1cb2… ✔
[deploy] versão 1.0.0 restaurada; o deploy de 1.0.0 FALHOU
exit=1
```

## Registros gerados (`deploy/state/`)

`deployments.log` (somente implantações bem-sucedidas):

```
2026-11-25T17:55:36Z  initial        1.0.0  sha256:268a1cb2…
2026-11-25T18:10:28Z  deploy         1.1.0  sha256:7e27a1fe…
2026-11-25T18:10:44Z  rollback       1.0.0  sha256:268a1cb2…
2026-11-25T18:11:10Z  auto-rollback  1.0.0  sha256:268a1cb2…
```

`attempts.log` (todas as etapas da tentativa que falhou):

```
18:10:53Z STARTED type=deploy → RESOLVED → PULLED → BACKUP_OK → SMOKE_FAILED → AUTO_ROLLBACK_OK restaurada=1.0.0
```

`current` permaneceu `1.0.0 sha256:268a1cb2…` durante toda a tentativa que falhou: a versão ativa só muda depois de um smoke test aprovado.
