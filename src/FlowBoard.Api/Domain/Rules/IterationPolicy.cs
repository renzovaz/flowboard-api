using FlowBoard.Api.Domain.Entities;
using FlowBoard.Api.Domain.Exceptions;

namespace FlowBoard.Api.Domain.Rules;

/// <summary>
/// R6 — Iteração IP: Stories só podem ser planejadas nas iterações 1 até (IterationCount − 1).
/// A última iteração do PI é a de Inovação e Planejamento.
/// </summary>
public static class IterationPolicy
{
    public static void Validate(int? iteration, ProgramIncrement programIncrement)
    {
        if (iteration is null)
        {
            return;
        }

        if (iteration.Value < 1 || iteration.Value > programIncrement.IterationCount)
        {
            throw new BusinessRuleException(
                ErrorCodes.InvalidIteration,
                "Iteração inválida",
                $"Iteração fora do intervalo do PI (1 a {programIncrement.IterationCount}).");
        }

        if (iteration.Value == programIncrement.IpIteration)
        {
            throw new BusinessRuleException(
                ErrorCodes.IpIterationNotPlannable,
                "Iteração IP não aceita Stories",
                $"A iteração {iteration.Value} é a iteração de Inovação e Planejamento (IP) e não aceita Stories planejadas.");
        }
    }
}
