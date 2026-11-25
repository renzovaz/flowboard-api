# Observabilidade

## Health Checks

| Endpoint | Verifica | Uso |
|---|---|---|
| `GET /health/live` | só o processo (nenhuma dependência) | *liveness* (orquestradores) |
| `GET /health/ready` | consulta ao PostgreSQL (*timeout* de 3 s) | `HEALTHCHECK` do Docker e smoke test do deploy |
| `GET /health` | todas as verificações, mais **versão** e duração (JSON) | demonstração de deploy e rollback |

`Unhealthy` retorna **503**. Os health checks não entram no log de requisições (evita ruído do `HEALTHCHECK` a cada 30 s).

```json
{"status":"Healthy","version":"1.1.0","totalDurationMs":52.7,
 "checks":[{"name":"postgresql","status":"Healthy","durationMs":50.1,"error":null}]}
```

## Logs estruturados

- Logging nativo do .NET com **JSON no console** (`AddJsonConsole`, horário UTC). O Docker coleta o stdout com rotação (10 MB × 3 arquivos).
- **Escopos** em cada linha: `TraceId` e `RequestPath`. O mesmo `traceId` aparece no Problem Details devolvido ao cliente.
- **Uma linha por requisição** (`HttpLogging`): método, caminho, status final e duração. **Sem cabeçalhos e sem corpo**, para não registrar dados sensíveis.
- SQL **não** é registrado, e `EnableSensitiveDataLogging` nunca é ativado.

### Eventos

| EventId | Evento | Nível |
|---|---|---|
| 1001 | `StoryMoved` (de, para, equipe) | Information |
| 1002 | `WipLimitRejected` (equipe, coluna, ocupação/limite) | Warning |
| 1003 | `CapacityExceeded` | Warning |
| 2001 | `DatabaseUnavailable` (com a causa raiz, ex.: `PostgresException 57P01`) | Error |
| 2002 | `IntegrityViolationNotCaughtByDomain` | Error |
| 2003 | `UnhandledException` (*stack trace* somente no log) | Error |
| 2004 | `WaitingForDatabase` (tentativa N/20 de aplicar migrations enquanto o banco inicia) | Warning |
| 3001 | `SwaggerEnabledInProduction` | Warning |

Consultas úteis:

```bash
C="docker compose -f deploy/compose.yaml --env-file deploy/.env --env-file deploy/state/image.env"
$C logs api --no-log-prefix | jq -c 'select(.EventId==1002)'          # rejeições de WIP
$C logs api --no-log-prefix | grep <traceId> | jq -c '{EventId,Message}' # correlação com uma resposta
```

## Como identificar uma falha depois do deploy

`docker compose stop db` produz cinco sinais independentes:

1. `docker compose ps` mostra a API como **`unhealthy`** (o `HEALTHCHECK` usa `/health/ready`).
2. `/health/ready` responde **503**, e `/health` mostra `postgresql: Unhealthy`.
3. Requisições de negócio recebem **503 `DATABASE_UNAVAILABLE`** com `traceId` e `Retry-After: 5`.
4. O log mostra o evento **2001** com o mesmo `traceId` e a causa raiz.
5. Em `/metrics` (v1.1.0), a taxa de respostas 503 aumenta.

`docker compose start db` restabelece `healthy` **sem reiniciar a API**.

## Métricas — `/metrics` (v1.1.0)

Formato Prometheus, via `prometheus-net`.

| Métrica | Tipo | Conceito |
|---|---|---|
| `http_requests_received_total`, `http_request_duration_seconds` | contador / histograma | saúde técnica por rota e status |
| `flowboard_story_moves_total{from,to}` | contador | fluxo entre colunas |
| `flowboard_wip_rejections_total{team}` | contador | política de WIP sendo aplicada (R2) |
| `flowboard_stories_completed_total{team}` | contador | **Flow Velocity** (throughput) |
| `flowboard_story_cycle_time_seconds` | histograma | **Flow Time** |
| `flowboard_wip_current{team,column}` | gauge | **Flow Load**, calculado no momento da coleta com uma consulta agrupada |

Os rótulos são limitados a equipe e coluna, nunca o Id da Story (cardinalidade controlada). Se o banco estiver fora, `flowboard_wip_current` é omitida, e o `/metrics` continua respondendo.

Prometheus e Grafana (*dashboard*) são **opcionais** no planejamento e não fazem parte da entrega obrigatória.
