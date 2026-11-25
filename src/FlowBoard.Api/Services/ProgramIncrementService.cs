using FlowBoard.Api.Contracts.ProgramIncrements;
using FlowBoard.Api.Data;
using FlowBoard.Api.Domain.Entities;
using FlowBoard.Api.Domain.Exceptions;
using FlowBoard.Api.Domain.Rules;
using Microsoft.EntityFrameworkCore;

namespace FlowBoard.Api.Services;

/// <summary>Casos de uso de Program Increments, incluindo o progresso (R8).</summary>
public sealed class ProgramIncrementService(FlowBoardDbContext db, TimeProvider clock)
{
    public async Task<ProgramIncrementResponse> CreateAsync(CreateProgramIncrementRequest request, CancellationToken ct)
    {
        var name = request.Name.Trim();
        if (await db.ProgramIncrements.AnyAsync(p => p.Name == name, ct))
        {
            throw new ConflictException(ErrorCodes.DuplicateName, "Nome já utilizado", $"Já existe um PI chamado '{name}'.");
        }

        var pi = new ProgramIncrement(name, request.StartDate, request.IterationCount, request.IterationLengthWeeks,
            clock.GetUtcNow().UtcDateTime);
        db.ProgramIncrements.Add(pi);
        await db.SaveChangesAsync(ct);
        return ProgramIncrementResponse.From(pi);
    }

    public async Task<IReadOnlyList<ProgramIncrementResponse>> ListAsync(CancellationToken ct)
    {
        var pis = await db.ProgramIncrements.AsNoTracking().OrderBy(p => p.Id).ToListAsync(ct);
        return pis.Select(ProgramIncrementResponse.From).ToList();
    }

    public async Task<ProgramIncrementResponse> GetAsync(int piId, CancellationToken ct) =>
        ProgramIncrementResponse.From(await FindAsync(piId, ct));

    /// <summary>R8 — progresso por pontos: geral, por Feature e por equipe.</summary>
    public async Task<ProgressResponse> GetProgressAsync(int piId, CancellationToken ct)
    {
        await FindAsync(piId, ct);

        var features = await db.Features.AsNoTracking()
            .Where(f => f.ProgramIncrementId == piId)
            .OrderBy(f => f.Id)
            .Select(f => new { f.Id, f.Title })
            .ToListAsync(ct);

        var stories = await (
                from s in db.Stories
                join f in db.Features on s.FeatureId equals f.Id
                where f.ProgramIncrementId == piId
                select new { s.FeatureId, s.TeamId, s.StoryPoints, IsDone = s.Status == StoryStatus.Done })
            .ToListAsync(ct);

        var teamIds = stories.Where(s => s.TeamId != null).Select(s => s.TeamId!.Value).Distinct().ToList();
        var teamNames = await db.Teams.AsNoTracking()
            .Where(t => teamIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, t => t.Name, ct);

        var overall = ProgressCalculator.Calculate(stories.Select(s => new ProgressItem(s.StoryPoints, s.IsDone)));

        var byFeature = features.Select(f =>
        {
            var r = ProgressCalculator.Calculate(stories.Where(s => s.FeatureId == f.Id).Select(s => new ProgressItem(s.StoryPoints, s.IsDone)));
            return new FeatureProgress(f.Id, f.Title, r.DonePoints, r.TotalPoints, r.Percent, r.UnestimatedStories);
        }).ToList();

        // Stories sem equipe entram no geral e nas Features, mas não nas equipes.
        var byTeam = teamIds.Order().Select(teamId =>
        {
            var r = ProgressCalculator.Calculate(stories.Where(s => s.TeamId == teamId).Select(s => new ProgressItem(s.StoryPoints, s.IsDone)));
            return new TeamProgress(teamId, teamNames[teamId], r.DonePoints, r.TotalPoints, r.Percent, r.UnestimatedStories);
        }).ToList();

        return new ProgressResponse(piId, ProgressSummary.From(overall), byFeature, byTeam);
    }

    private async Task<ProgramIncrement> FindAsync(int piId, CancellationToken ct) =>
        await db.ProgramIncrements.AsNoTracking().SingleOrDefaultAsync(p => p.Id == piId, ct)
        ?? throw new NotFoundException("Program Increment", piId);
}
