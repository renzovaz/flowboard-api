using FlowBoard.Api.Domain.Rules;

namespace FlowBoard.Api.Domain.Entities;

/// <summary>
/// Agile Team (SAFe): equipe com capacidade por iteração e limites de WIP por coluna.
/// </summary>
public class Team
{
    // Construtor sem parâmetros exigido pelo Entity Framework Core.
    private Team()
    {
    }

    public Team(string name, int capacityPoints, int wipLimitInProgress, int wipLimitReview, DateTime createdAt)
    {
        Update(name, capacityPoints, wipLimitInProgress, wipLimitReview);
        CreatedAt = createdAt;
    }

    public int Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public int CapacityPoints { get; private set; }

    public int WipLimitInProgress { get; private set; }

    public int WipLimitReview { get; private set; }

    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Atualiza os dados da equipe. Reduzir capacidade ou WIP abaixo da ocupação atual é permitido
    /// (ADR-005): somente novas entradas passam a ser bloqueadas.
    /// </summary>
    public void Update(string name, int capacityPoints, int wipLimitInProgress, int wipLimitReview)
    {
        DomainLimits.EnsureInRange(capacityPoints, DomainLimits.CapacityMin, DomainLimits.CapacityMax, "capacityPoints");
        DomainLimits.EnsureInRange(wipLimitInProgress, DomainLimits.WipLimitMin, DomainLimits.WipLimitMax, "wipLimitInProgress");
        DomainLimits.EnsureInRange(wipLimitReview, DomainLimits.WipLimitMin, DomainLimits.WipLimitMax, "wipLimitReview");

        Name = name.Trim();
        CapacityPoints = capacityPoints;
        WipLimitInProgress = wipLimitInProgress;
        WipLimitReview = wipLimitReview;
    }

    /// <summary>Limite de WIP da coluna (R2). Colunas sem limite retornam null.</summary>
    public int? GetWipLimit(StoryStatus column) => column switch
    {
        StoryStatus.InProgress => WipLimitInProgress,
        StoryStatus.Review => WipLimitReview,
        _ => null,
    };
}
