using FlowBoard.Api.Domain.Exceptions;

namespace FlowBoard.Api.Domain.Rules;

/// <summary>
/// R7 — WSJF (Weighted Shortest Job First), fórmula do SAFe:
///   Cost of Delay = Business Value + Time Criticality + Risk Reduction
///   WSJF          = Cost of Delay ÷ Job Size
/// Valores na escala Fibonacci modificada {1, 2, 3, 5, 8, 13, 20}.
/// </summary>
public static class WsjfCalculator
{
    public static readonly IReadOnlySet<int> AllowedValues = new HashSet<int> { 1, 2, 3, 5, 8, 13, 20 };

    public static void ValidateScale(int value, string field)
    {
        if (!AllowedValues.Contains(value))
        {
            throw new BusinessRuleException(
                ErrorCodes.InvalidWsjfValue,
                "Valor de WSJF inválido",
                $"O campo '{field}' deve estar na escala 1, 2, 3, 5, 8, 13, 20 (recebido: {value}).");
        }
    }

    public static int CostOfDelay(int businessValue, int timeCriticality, int riskReduction) =>
        businessValue + timeCriticality + riskReduction;

    /// <summary>WSJF exato. JobSize ≥ 1 é garantido pela validação de escala (sem divisão por zero).</summary>
    public static decimal Calculate(int businessValue, int timeCriticality, int riskReduction, int jobSize) =>
        (decimal)CostOfDelay(businessValue, timeCriticality, riskReduction) / jobSize;

    /// <summary>Valor exibido na API: 2 casas decimais.</summary>
    public static decimal Round(decimal wsjf) => Math.Round(wsjf, 2, MidpointRounding.AwayFromZero);

    /// <summary>
    /// Ordenação determinística (ADR-001): WSJF desc → Cost of Delay desc → CreatedAt asc → Id asc.
    /// </summary>
    public static IReadOnlyList<T> Order<T>(IEnumerable<T> items) where T : IWsjfItem =>
        items
            .OrderByDescending(i => i.Wsjf)
            .ThenByDescending(i => i.CostOfDelay)
            .ThenBy(i => i.CreatedAt)
            .ThenBy(i => i.Id)
            .ToList();
}

/// <summary>Dados necessários para ordenar um item por WSJF.</summary>
public interface IWsjfItem
{
    int Id { get; }

    int CostOfDelay { get; }

    decimal Wsjf { get; }

    DateTime CreatedAt { get; }
}
