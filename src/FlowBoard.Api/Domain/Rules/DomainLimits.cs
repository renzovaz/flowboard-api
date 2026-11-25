using FlowBoard.Api.Domain.Exceptions;

namespace FlowBoard.Api.Domain.Rules;

/// <summary>
/// Todas as faixas de validação do domínio em um único lugar (aprovadas na Parte 1 do planejamento).
/// </summary>
public static class DomainLimits
{
    public const int CapacityMin = 1;
    public const int CapacityMax = 200;
    public const int WipLimitMin = 1;
    public const int WipLimitMax = 20;
    public const int IterationCountMin = 2;
    public const int IterationCountMax = 6;
    public const int IterationCountDefault = 5;
    public const int IterationLengthWeeksMin = 1;
    public const int IterationLengthWeeksMax = 4;
    public const int IterationLengthWeeksDefault = 2;
    public const int AcceptanceCriteriaMinChars = 10;

    public const int TeamNameMaxLength = 100;
    public const int ProgramIncrementNameMaxLength = 50;
    public const int TitleMaxLength = 150;
    public const int DescriptionMaxLength = 1000;
    public const int AcceptanceCriteriaMaxLength = 2000;

    /// <summary>Garante que o valor esteja na faixa [min, max]; caso contrário, HTTP 422 INVALID_RANGE.</summary>
    public static void EnsureInRange(int value, int min, int max, string field)
    {
        if (value < min || value > max)
        {
            throw new BusinessRuleException(
                ErrorCodes.InvalidRange,
                "Valor fora da faixa permitida",
                $"O campo '{field}' deve estar entre {min} e {max} (recebido: {value}).");
        }
    }
}
