using FlowBoard.Api.Domain.Exceptions;

namespace FlowBoard.Api.Domain.Rules;

/// <summary>
/// R4 — Story Points na sequência Fibonacci {1, 2, 3, 5, 8, 13}.
/// Acima de 13 a Story deve ser dividida (orientação do SAFe: uma Story cabe em uma iteração).
/// </summary>
public static class StoryPointsPolicy
{
    public static readonly IReadOnlySet<int> AllowedValues = new HashSet<int> { 1, 2, 3, 5, 8, 13 };

    public const int MaxPoints = 13;

    /// <summary>Valida os pontos; null é permitido (Story ainda não estimada, no Backlog).</summary>
    public static void Validate(int? storyPoints)
    {
        if (storyPoints is null)
        {
            return;
        }

        if (storyPoints.Value > MaxPoints)
        {
            throw new BusinessRuleException(
                ErrorCodes.StoryTooLarge,
                "Story grande demais",
                $"Stories maiores que {MaxPoints} pontos devem ser divididas em Stories menores (recebido: {storyPoints.Value}).");
        }

        if (!AllowedValues.Contains(storyPoints.Value))
        {
            throw new BusinessRuleException(
                ErrorCodes.InvalidStoryPoints,
                "Story Points inválidos",
                $"Story Points devem seguir a sequência 1, 2, 3, 5, 8, 13 (recebido: {storyPoints.Value}).");
        }
    }
}
