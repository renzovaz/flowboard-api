using FlowBoard.Api.Contracts.Teams;
using FlowBoard.Api.Data;
using FlowBoard.Api.Domain.Entities;
using FlowBoard.Api.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace FlowBoard.Api.Services;

/// <summary>Casos de uso de equipes: cadastro, quadro Kanban e carga por iteração.</summary>
public sealed class TeamService(FlowBoardDbContext db, TimeProvider clock)
{
    public async Task<TeamResponse> CreateAsync(TeamRequest request, CancellationToken ct)
    {
        await EnsureNameIsFreeAsync(request.Name, null, ct);
        var team = new Team(request.Name, request.CapacityPoints, request.WipLimitInProgress,
            request.WipLimitReview, clock.GetUtcNow().UtcDateTime);
        db.Teams.Add(team);
        await db.SaveChangesAsync(ct);
        return TeamResponse.From(team);
    }

    public async Task<IReadOnlyList<TeamResponse>> ListAsync(CancellationToken ct) =>
        await db.Teams.AsNoTracking().OrderBy(t => t.Id).Select(t => TeamResponse.From(t)).ToListAsync(ct);

    public async Task<TeamResponse> GetAsync(int teamId, CancellationToken ct) =>
        TeamResponse.From(await FindAsync(teamId, ct));

    public async Task<TeamResponse> UpdateAsync(int teamId, TeamRequest request, CancellationToken ct)
    {
        var team = await FindAsync(teamId, ct);
        await EnsureNameIsFreeAsync(request.Name, teamId, ct);
        team.Update(request.Name, request.CapacityPoints, request.WipLimitInProgress, request.WipLimitReview);
        await db.SaveChangesAsync(ct);
        return TeamResponse.From(team);
    }

    /// <summary>Quadro Kanban: Stories da equipe por coluna, com ocupação de WIP (R2).</summary>
    public async Task<BoardResponse> GetBoardAsync(int teamId, CancellationToken ct)
    {
        var team = await FindAsync(teamId, ct);
        var stories = await db.Stories.AsNoTracking()
            .Where(s => s.TeamId == teamId)
            .OrderBy(s => s.Id)
            .ToListAsync(ct);

        var columns = Enum.GetValues<StoryStatus>().Select(status =>
        {
            var cards = stories.Where(s => s.Status == status)
                .Select(s => new BoardCard(s.Id, s.Title, s.StoryPoints, s.FeatureId, s.Iteration))
                .ToList();
            var limit = team.GetWipLimit(status);
            return new BoardColumn(status, limit, cards.Count, limit is null ? null : cards.Count >= limit, cards);
        }).ToList();

        return new BoardResponse(team.Id, team.Name, columns);
    }

    /// <summary>Carga planejada × capacidade da equipe em cada iteração de um PI (R5, R6).</summary>
    public async Task<TeamCapacityResponse> GetCapacityAsync(int teamId, int programIncrementId, CancellationToken ct)
    {
        var team = await FindAsync(teamId, ct);
        var pi = await db.ProgramIncrements.AsNoTracking().SingleOrDefaultAsync(p => p.Id == programIncrementId, ct)
            ?? throw new NotFoundException("Program Increment", programIncrementId);

        var loads = await (
                from s in db.Stories
                join f in db.Features on s.FeatureId equals f.Id
                where s.TeamId == teamId && f.ProgramIncrementId == programIncrementId && s.Iteration != null
                group s by s.Iteration into g
                select new { Iteration = g.Key!.Value, Points = g.Sum(x => x.StoryPoints ?? 0) })
            .ToDictionaryAsync(x => x.Iteration, x => x.Points, ct);

        var iterations = pi.GetIterations().Select(i =>
        {
            var planned = loads.GetValueOrDefault(i.Number);
            var available = i.IsIp ? 0 : Math.Max(team.CapacityPoints - planned, 0);
            return new IterationLoad(i.Number, planned, available, i.IsIp);
        }).ToList();

        return new TeamCapacityResponse(team.Id, pi.Id, team.CapacityPoints, iterations);
    }

    private async Task<Team> FindAsync(int teamId, CancellationToken ct) =>
        await db.Teams.SingleOrDefaultAsync(t => t.Id == teamId, ct) ?? throw new NotFoundException("Equipe", teamId);

    private async Task EnsureNameIsFreeAsync(string name, int? ignoreTeamId, CancellationToken ct)
    {
        var trimmed = name.Trim();
        if (await db.Teams.AnyAsync(t => t.Name == trimmed && t.Id != ignoreTeamId, ct))
        {
            throw new ConflictException(ErrorCodes.DuplicateName, "Nome já utilizado", $"Já existe uma equipe chamada '{trimmed}'.");
        }
    }
}
