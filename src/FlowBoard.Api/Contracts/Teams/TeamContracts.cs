using System.ComponentModel.DataAnnotations;
using FlowBoard.Api.Domain.Entities;
using FlowBoard.Api.Domain.Rules;

namespace FlowBoard.Api.Contracts.Teams;

/// <summary>
/// Corpo de POST /api/teams e PUT /api/teams/{id}.
/// "required" = campo obrigatório no JSON (ausente → HTTP 400).
/// As faixas (capacidade 1–200, WIP 1–20) são regras de negócio validadas no domínio (HTTP 422).
/// </summary>
public sealed record TeamRequest
{
    [StringLength(DomainLimits.TeamNameMaxLength, MinimumLength = 1)]
    public required string Name { get; init; }

    public required int CapacityPoints { get; init; }

    public required int WipLimitInProgress { get; init; }

    public required int WipLimitReview { get; init; }
}

public sealed record TeamResponse(
    int Id, string Name, int CapacityPoints, int WipLimitInProgress, int WipLimitReview, DateTime CreatedAt)
{
    public static TeamResponse From(Team team) =>
        new(team.Id, team.Name, team.CapacityPoints, team.WipLimitInProgress, team.WipLimitReview, team.CreatedAt);
}

/// <summary>Quadro Kanban da equipe (GET /api/teams/{id}/board).</summary>
public sealed record BoardResponse(int TeamId, string TeamName, IReadOnlyList<BoardColumn> Columns);

public sealed record BoardColumn(StoryStatus Status, int? WipLimit, int Count, bool? IsFull, IReadOnlyList<BoardCard> Stories);

public sealed record BoardCard(int Id, string Title, int? StoryPoints, int FeatureId, int? Iteration);

/// <summary>Carga planejada × capacidade por iteração (GET /api/teams/{id}/capacity).</summary>
public sealed record TeamCapacityResponse(int TeamId, int ProgramIncrementId, int CapacityPoints, IReadOnlyList<IterationLoad> Iterations);

public sealed record IterationLoad(int Number, int PlannedPoints, int AvailablePoints, bool IsIp);
