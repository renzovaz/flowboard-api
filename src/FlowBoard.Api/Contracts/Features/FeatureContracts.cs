using System.ComponentModel.DataAnnotations;
using FlowBoard.Api.Domain.Entities;
using FlowBoard.Api.Domain.Rules;

namespace FlowBoard.Api.Contracts.Features;

/// <summary>Corpo de POST /api/program-increments/{piId}/features e PUT /api/features/{id}.</summary>
public sealed record FeatureRequest
{
    [StringLength(DomainLimits.TitleMaxLength, MinimumLength = 1)]
    public required string Title { get; init; }

    [StringLength(DomainLimits.DescriptionMaxLength)]
    public string? Description { get; init; }

    /// <summary>Escala Fibonacci modificada: 1, 2, 3, 5, 8, 13, 20.</summary>
    public required int BusinessValue { get; init; }

    public required int TimeCriticality { get; init; }

    public required int RiskReduction { get; init; }

    public required int JobSize { get; init; }
}

/// <summary>Feature com Cost of Delay e WSJF calculados (R7). Rank é preenchido na listagem ordenada.</summary>
public sealed record FeatureResponse(
    int Id, int ProgramIncrementId, string Title, string? Description,
    int BusinessValue, int TimeCriticality, int RiskReduction, int JobSize,
    int CostOfDelay, decimal Wsjf, int? Rank, DateTime CreatedAt)
{
    public static FeatureResponse From(Feature f, int? rank = null) =>
        new(f.Id, f.ProgramIncrementId, f.Title, f.Description,
            f.BusinessValue, f.TimeCriticality, f.RiskReduction, f.JobSize,
            f.CostOfDelay, WsjfCalculator.Round(f.Wsjf), rank, f.CreatedAt);
}
