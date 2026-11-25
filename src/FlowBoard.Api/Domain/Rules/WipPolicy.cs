using FlowBoard.Api.Domain.Entities;
using FlowBoard.Api.Domain.Exceptions;

namespace FlowBoard.Api.Domain.Rules;

/// <summary>
/// R2 — Limite de WIP: uma Story só entra em uma coluna limitada (InProgress, Review)
/// se a ocupação atual da MESMA equipe nessa coluna for MENOR que o limite.
/// </summary>
public static class WipPolicy
{
    /// <param name="column">Coluna de destino.</param>
    /// <param name="currentCount">Quantidade de Stories da equipe que já estão na coluna.</param>
    /// <param name="limit">Limite da coluna (null = coluna sem limite).</param>
    /// <param name="teamName">Nome da equipe, usado na mensagem.</param>
    public static void EnsureCanEnter(StoryStatus column, int currentCount, int? limit, string teamName)
    {
        if (limit is null)
        {
            return;
        }

        // Comparação inclusiva: com limite 3 e 3 Stories na coluna, a 4ª é recusada.
        if (currentCount >= limit.Value)
        {
            throw new ConflictException(
                ErrorCodes.WipLimitExceeded,
                "Limite de WIP atingido",
                $"A coluna {column} da equipe {teamName} está no limite ({currentCount}/{limit.Value}).");
        }
    }
}
