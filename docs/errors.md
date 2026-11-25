# Catálogo de erros

Todas as respostas de erro seguem o padrão **Problem Details (RFC 9457)**, com dois campos extras:

- `errorCode`: código estável, usado pelos testes e por clientes da API;
- `traceId`: identificador da requisição, o mesmo registrado nos logs. Permite localizar no servidor o detalhe de um erro recebido pelo cliente.

```json
{
  "type": "https://github.com/renzovaz/flowboard-api/blob/main/docs/errors.md#wip_limit_exceeded",
  "title": "Limite de WIP atingido",
  "status": 409,
  "detail": "A coluna InProgress da equipe Alpha está no limite (3/3).",
  "instance": "/api/stories/7/status",
  "errorCode": "WIP_LIMIT_EXCEEDED",
  "traceId": "41294ffc2b4a06abeb32a81867a541be"
}
```

## Convenção de códigos HTTP

| Código | Uso |
|---|---|
| **400** | requisição malformada: JSON inválido, campo obrigatório ausente, tipo errado, valor de enum inexistente |
| **404** | recurso referenciado não existe |
| **409** | dados válidos, mas a operação **conflita com o estado atual** (poderia funcionar em outro momento) |
| **422** | dados bem formados que **violam uma regra de negócio** (inválidos qualquer que seja o momento) |
| **503** | dependência indisponível (banco de dados) |
| **500** | erro inesperado. Nunca expõe *stack trace* ou mensagem interna, só o `traceId` |

## Códigos

### validation_error
**400.** Falha na validação estrutural da requisição. O campo `errors` lista os problemas por campo.

### resource_not_found
**404.** Equipe, PI, Feature ou Story inexistente.

### duplicate_name
**409.** Já existe uma equipe ou um PI com o mesmo nome. Também é retornado quando duas criações simultâneas disputam o mesmo nome (`UNIQUE` do banco, SqlState 23505).

### invalid_transition
**409 (R1).** Transição de coluna não permitida: pular etapas, repetir o mesmo status ou sair de `Done`.

### wip_limit_exceeded
**409 (R2).** A coluna de destino da equipe atingiu o limite de WIP.

### capacity_exceeded
**409 (R5).** A soma dos pontos da equipe na iteração ultrapassaria a capacidade.

### team_change_not_allowed
**409 (RC2).** A equipe de uma Story só pode mudar em `Backlog` ou `Ready`.

### concurrency_conflict
**409.** A mesma Story foi alterada por outra requisição ao mesmo tempo (`xmin`), ou o PostgreSQL detectou conflito de serialização ou *deadlock*. Basta repetir a operação.

### definition_of_ready_not_met
**422 (R3).** A Story não atende à Definition of Ready para entrar em `Ready`. O campo `missingCriteria` lista **todos** os critérios pendentes (`storyPoints`, `acceptanceCriteria`, `teamId`, `iteration`).

### definition_of_ready_violated
**422 (R4 / RC1).** Depois de sair do Backlog, a Story não pode ter pontos, critérios, equipe ou iteração apagados. Também traz `missingCriteria`.

### invalid_story_points
**422 (R4).** Story Points fora da sequência 1, 2, 3, 5, 8, 13.

### story_too_large
**422 (R4).** Story com mais de 13 pontos: deve ser dividida.

### invalid_iteration
**422 (R6).** Iteração fora do intervalo do PI.

### ip_iteration_not_plannable
**422 (R6).** A última iteração do PI (IP) não aceita Stories planejadas.

### invalid_wsjf_value
**422 (R7).** Campo do WSJF fora da escala 1, 2, 3, 5, 8, 13, 20.

### invalid_range
**422.** Capacidade (1–200), WIP (1–20), número de iterações (2–6) ou duração da iteração (1–4 semanas) fora da faixa.

### database_unavailable
**503.** O PostgreSQL não está respondendo (conexão recusada, *timeout*, banco desligando ou iniciando). Inclui o cabeçalho `Retry-After: 5`. O log registra o evento 2001 com a causa raiz.

### internal_error
**500.** Erro inesperado. O detalhe completo (com *stack trace*) fica **somente** no log (eventos 2002/2003), localizável pelo `traceId`.
