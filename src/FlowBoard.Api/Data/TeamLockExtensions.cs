using Microsoft.EntityFrameworkCore;

namespace FlowBoard.Api.Data;

/// <summary>
/// Lock pessimista por equipe (ADR-002). Único SQL escrito manualmente no projeto.
/// </summary>
public static class TeamLockExtensions
{
    /// <summary>
    /// Trava as linhas das equipes com SELECT ... FOR UPDATE dentro da transação atual.
    /// Uma segunda requisição para a MESMA equipe espera até a primeira confirmar (commit),
    /// e só então conta o WIP/capacidade — já com o valor atualizado.
    /// As equipes são travadas em ordem crescente de Id para evitar deadlock.
    /// </summary>
    public static async Task LockTeamsAsync(this FlowBoardDbContext db, IEnumerable<int> teamIds, CancellationToken ct)
    {
        if (db.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException("O lock de equipe exige uma transação aberta.");
        }

        foreach (var teamId in teamIds.Distinct().Order())
        {
            // ExecuteSqlAsync com interpolação gera parâmetro SQL ($1), sem risco de SQL injection.
            await db.Database.ExecuteSqlAsync($"SELECT id FROM teams WHERE id = {teamId} FOR UPDATE", ct);
        }
    }
}
