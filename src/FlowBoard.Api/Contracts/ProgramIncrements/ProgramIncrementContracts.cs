using System.ComponentModel.DataAnnotations;
using FlowBoard.Api.Domain.Entities;
using FlowBoard.Api.Domain.Rules;

namespace FlowBoard.Api.Contracts.ProgramIncrements;

/// <summary>Corpo de POST /api/program-increments. Iterações e duração são opcionais (padrão 5 e 2).</summary>
public sealed record CreateProgramIncrementRequest
{
    [StringLength(DomainLimits.ProgramIncrementNameMaxLength, MinimumLength = 1)]
    public required string Name { get; init; }

    public required DateOnly StartDate { get; init; }

    public int IterationCount { get; init; } = DomainLimits.IterationCountDefault;

    public int IterationLengthWeeks { get; init; } = DomainLimits.IterationLengthWeeksDefault;
}

public sealed record ProgramIncrementResponse(
    int Id, string Name, DateOnly StartDate, DateOnly EndDate, int IterationCount, int IterationLengthWeeks,
    int IpIteration, IReadOnlyList<IterationInfo> Iterations, DateTime CreatedAt)
{
    public static ProgramIncrementResponse From(ProgramIncrement pi) =>
        new(pi.Id, pi.Name, pi.StartDate, pi.EndDate, pi.IterationCount, pi.IterationLengthWeeks,
            pi.IpIteration, pi.GetIterations(), pi.CreatedAt);
}

/// <summary>Progresso do PI por pontos (R8): geral, por Feature e por equipe.</summary>
public sealed record ProgressResponse(
    int ProgramIncrementId, ProgressSummary Overall, IReadOnlyList<FeatureProgress> Features, IReadOnlyList<TeamProgress> Teams);

public sealed record ProgressSummary(int DonePoints, int TotalPoints, decimal Percent, int UnestimatedStories)
{
    public static ProgressSummary From(ProgressResult r) => new(r.DonePoints, r.TotalPoints, r.Percent, r.UnestimatedStories);
}

public sealed record FeatureProgress(int FeatureId, string Title, int DonePoints, int TotalPoints, decimal Percent, int UnestimatedStories);

public sealed record TeamProgress(int TeamId, string TeamName, int DonePoints, int TotalPoints, decimal Percent, int UnestimatedStories);
