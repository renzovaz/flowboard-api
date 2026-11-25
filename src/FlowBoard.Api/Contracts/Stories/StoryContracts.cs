using System.ComponentModel.DataAnnotations;
using FlowBoard.Api.Domain.Entities;
using FlowBoard.Api.Domain.Rules;

namespace FlowBoard.Api.Contracts.Stories;

/// <summary>
/// Corpo de POST /api/features/{featureId}/stories e PUT /api/stories/{id}.
/// Apenas o título é obrigatório; pontos, equipe e iteração podem ser definidos depois (Backlog).
/// </summary>
public sealed record StoryRequest
{
    [StringLength(DomainLimits.TitleMaxLength, MinimumLength = 1)]
    public required string Title { get; init; }

    [StringLength(DomainLimits.AcceptanceCriteriaMaxLength)]
    public string? AcceptanceCriteria { get; init; }

    /// <summary>Fibonacci: 1, 2, 3, 5, 8, 13 (R4).</summary>
    public int? StoryPoints { get; init; }

    public int? TeamId { get; init; }

    /// <summary>Iteração do PI (1 até IterationCount − 1; a última é a IP — R6).</summary>
    public int? Iteration { get; init; }
}

/// <summary>Corpo de PATCH /api/stories/{id}/status. Valor fora do enum → HTTP 400.</summary>
public sealed record ChangeStoryStatusRequest
{
    public required StoryStatus Status { get; init; }
}

public sealed record StoryResponse(
    int Id, int FeatureId, int ProgramIncrementId, int? TeamId, string Title, string? AcceptanceCriteria,
    int? StoryPoints, int? Iteration, StoryStatus Status, DateTime CreatedAt, DateTime? StartedAt, DateTime? CompletedAt)
{
    public static StoryResponse From(Story s, int programIncrementId) =>
        new(s.Id, s.FeatureId, programIncrementId, s.TeamId, s.Title, s.AcceptanceCriteria,
            s.StoryPoints, s.Iteration, s.Status, s.CreatedAt, s.StartedAt, s.CompletedAt);
}
