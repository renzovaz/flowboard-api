using FlowBoard.Api.Contracts.Stories;
using FlowBoard.Api.Data;
using FlowBoard.Api.Domain.Entities;
using FlowBoard.Api.Domain.Exceptions;
using FlowBoard.Api.Domain.Rules;
using FlowBoard.Api.Infrastructure.Observability;
using Microsoft.EntityFrameworkCore;

namespace FlowBoard.Api.Services;

/// <summary>
/// Casos de uso de Stories: criação, planejamento e movimentação no quadro.
/// Regras que dependem das OUTRAS Stories da equipe (WIP — R2, capacidade — R5) são verificadas
/// dentro de uma transação, depois de travar a linha da equipe (SELECT ... FOR UPDATE, ADR-002).
/// </summary>
public sealed partial class StoryService(
    FlowBoardDbContext db, TimeProvider clock, FlowMetrics metrics, ILogger<StoryService> logger)
{
    public async Task<StoryResponse> CreateAsync(int featureId, StoryRequest request, CancellationToken ct)
    {
        var (feature, pi) = await FindFeatureAsync(featureId, ct);
        var team = request.TeamId is null ? null : await FindTeamAsync(request.TeamId.Value, ct);

        // Regras locais (R4, R6) são validadas na construção da Story.
        var story = new Story(feature.Id, pi, request.Title, request.AcceptanceCriteria,
            request.StoryPoints, request.TeamId, request.Iteration, clock.GetUtcNow().UtcDateTime);

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        if (team is not null)
        {
            await db.LockTeamsAsync([team.Id], ct);
            await EnsureCapacityAsync(story, team, pi.Id, ct);
        }

        db.Stories.Add(story);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return StoryResponse.From(story, pi.Id);
    }

    public async Task<StoryResponse> GetAsync(int storyId, CancellationToken ct)
    {
        var story = await FindStoryAsync(storyId, ct);
        var piId = await db.Features.Where(f => f.Id == story.FeatureId).Select(f => f.ProgramIncrementId).SingleAsync(ct);
        return StoryResponse.From(story, piId);
    }

    /// <summary>Atualiza o planejamento (R4, R5, R6, RC1, RC2).</summary>
    public async Task<StoryResponse> UpdateAsync(int storyId, StoryRequest request, CancellationToken ct)
    {
        var story = await FindStoryAsync(storyId, ct);
        var (_, pi) = await FindFeatureAsync(story.FeatureId, ct);
        var team = request.TeamId is null ? null : await FindTeamAsync(request.TeamId.Value, ct);

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        if (team is not null)
        {
            await db.LockTeamsAsync([team.Id], ct);
        }

        story.UpdatePlanning(pi, request.Title, request.AcceptanceCriteria, request.StoryPoints, request.TeamId, request.Iteration);

        if (team is not null)
        {
            await EnsureCapacityAsync(story, team, pi.Id, ct);
        }

        await db.SaveChangesAsync(ct); // xmin: edição simultânea da mesma Story → 409
        await transaction.CommitAsync(ct);
        return StoryResponse.From(story, pi.Id);
    }

    /// <summary>
    /// Move a Story no quadro. Ordem das verificações (o erro mais fundamental vem primeiro):
    /// existência (404) → transição R1 (409) → Definition of Ready R3 (422) → lock + WIP R2 (409).
    /// </summary>
    public async Task<StoryResponse> ChangeStatusAsync(int storyId, StoryStatus target, CancellationToken ct)
    {
        var story = await FindStoryAsync(storyId, ct);
        var from = story.Status;

        KanbanWorkflow.EnsureCanMove(from, target);
        if (target == StoryStatus.Ready)
        {
            DefinitionOfReady.Ensure(story);
        }

        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        Team? team = null;
        var occupancy = 0;
        if (story.TeamId is not null)
        {
            await db.LockTeamsAsync([story.TeamId.Value], ct);
            team = await db.Teams.SingleAsync(t => t.Id == story.TeamId.Value, ct);
            var limit = team.GetWipLimit(target);
            if (limit is not null)
            {
                // Contagem feita DEPOIS do lock: reflete o que outras requisições já confirmaram.
                occupancy = await db.Stories.CountAsync(s => s.TeamId == team.Id && s.Status == target, ct);
                try
                {
                    WipPolicy.EnsureCanEnter(target, occupancy, limit, team.Name);
                }
                catch (ConflictException)
                {
                    LogWipRejected(logger, story.Id, team.Id, target, occupancy, limit.Value);
                    metrics.WipRejected(team.Id);
                    throw;
                }
            }
        }

        story.MoveTo(target, clock.GetUtcNow().UtcDateTime);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        LogStoryMoved(logger, story.Id, from, target, story.TeamId);
        metrics.StoryMoved(story, from);
        var piId = await db.Features.Where(f => f.Id == story.FeatureId).Select(f => f.ProgramIncrementId).SingleAsync(ct);
        return StoryResponse.From(story, piId);
    }

    /// <summary>R5 — soma a carga da equipe no mesmo PI e iteração (sem contar a própria Story).</summary>
    private async Task EnsureCapacityAsync(Story story, Team team, int piId, CancellationToken ct)
    {
        if (story.Iteration is null || story.StoryPoints is null)
        {
            return;
        }

        var iteration = story.Iteration.Value;
        var planned = await (
                from s in db.Stories
                join f in db.Features on s.FeatureId equals f.Id
                where s.TeamId == team.Id && s.Iteration == iteration && f.ProgramIncrementId == piId
                select new PlannedStory(s.Id, s.StoryPoints))
            .ToListAsync(ct);

        try
        {
            CapacityPolicy.EnsureWithinCapacity(planned, story.Id == 0 ? null : story.Id,
                story.StoryPoints.Value, team.CapacityPoints, iteration);
        }
        catch (ConflictException)
        {
            LogCapacityExceeded(logger, team.Id, iteration, team.CapacityPoints);
            throw;
        }
    }

    private async Task<Story> FindStoryAsync(int storyId, CancellationToken ct) =>
        await db.Stories.SingleOrDefaultAsync(s => s.Id == storyId, ct) ?? throw new NotFoundException("Story", storyId);

    private async Task<Team> FindTeamAsync(int teamId, CancellationToken ct) =>
        await db.Teams.AsNoTracking().SingleOrDefaultAsync(t => t.Id == teamId, ct) ?? throw new NotFoundException("Equipe", teamId);

    private async Task<(Feature Feature, ProgramIncrement Pi)> FindFeatureAsync(int featureId, CancellationToken ct)
    {
        var feature = await db.Features.AsNoTracking().SingleOrDefaultAsync(f => f.Id == featureId, ct)
            ?? throw new NotFoundException("Feature", featureId);
        var pi = await db.ProgramIncrements.AsNoTracking().SingleAsync(p => p.Id == feature.ProgramIncrementId, ct);
        return (feature, pi);
    }

    [LoggerMessage(EventId = 1001, Level = LogLevel.Information,
        Message = "StoryMoved story={StoryId} {From}->{To} team={TeamId}")]
    private static partial void LogStoryMoved(ILogger logger, int storyId, StoryStatus from, StoryStatus to, int? teamId);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Warning,
        Message = "WipLimitRejected story={StoryId} team={TeamId} column={Column} occupancy={Occupancy}/{Limit}")]
    private static partial void LogWipRejected(ILogger logger, int storyId, int teamId, StoryStatus column, int occupancy, int limit);

    [LoggerMessage(EventId = 1003, Level = LogLevel.Warning,
        Message = "CapacityExceeded team={TeamId} iteration={Iteration} capacity={Capacity}")]
    private static partial void LogCapacityExceeded(ILogger logger, int teamId, int iteration, int capacity);
}
