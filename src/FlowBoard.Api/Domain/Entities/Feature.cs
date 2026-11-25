using FlowBoard.Api.Domain.Rules;

namespace FlowBoard.Api.Domain.Entities;

/// <summary>
/// Feature (SAFe): funcionalidade planejada para um PI e priorizada por WSJF (R7).
/// O WSJF é sempre calculado, nunca armazenado.
/// </summary>
public class Feature : IWsjfItem
{
    private Feature()
    {
    }

    public Feature(int programIncrementId, string title, string? description,
        int businessValue, int timeCriticality, int riskReduction, int jobSize, DateTime createdAt)
    {
        ProgramIncrementId = programIncrementId;
        CreatedAt = createdAt;
        Update(title, description, businessValue, timeCriticality, riskReduction, jobSize);
    }

    public int Id { get; private set; }

    public int ProgramIncrementId { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public int BusinessValue { get; private set; }

    public int TimeCriticality { get; private set; }

    public int RiskReduction { get; private set; }

    public int JobSize { get; private set; }

    public DateTime CreatedAt { get; private set; }

    /// <summary>Custo do Atraso = Valor de Negócio + Criticidade de Tempo + Redução de Risco.</summary>
    public int CostOfDelay => WsjfCalculator.CostOfDelay(BusinessValue, TimeCriticality, RiskReduction);

    /// <summary>WSJF exato (sem arredondamento), usado na ordenação.</summary>
    public decimal Wsjf => WsjfCalculator.Calculate(BusinessValue, TimeCriticality, RiskReduction, JobSize);

    public void Update(string title, string? description,
        int businessValue, int timeCriticality, int riskReduction, int jobSize)
    {
        WsjfCalculator.ValidateScale(businessValue, "businessValue");
        WsjfCalculator.ValidateScale(timeCriticality, "timeCriticality");
        WsjfCalculator.ValidateScale(riskReduction, "riskReduction");
        WsjfCalculator.ValidateScale(jobSize, "jobSize");

        Title = title.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        BusinessValue = businessValue;
        TimeCriticality = timeCriticality;
        RiskReduction = riskReduction;
        JobSize = jobSize;
    }
}
