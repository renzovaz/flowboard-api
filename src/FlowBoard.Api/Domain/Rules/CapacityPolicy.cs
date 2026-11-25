using FlowBoard.Api.Domain.Exceptions;

namespace FlowBoard.Api.Domain.Rules;

/// <summary>
/// R5 — Capacidade: a soma dos pontos das Stories de uma equipe, no mesmo PI e iteração,
/// não pode ultrapassar a capacidade da equipe. O limite exato é permitido.
/// </summary>
public static class CapacityPolicy
{
    /// <summary>Soma os pontos planejados, ignorando a Story que está sendo (re)planejada.</summary>
    public static int CalculateLoad(IEnumerable<PlannedStory> plannedStories, int? excludeStoryId) =>
        plannedStories
            .Where(s => excludeStoryId is null || s.StoryId != excludeStoryId.Value)
            .Sum(s => s.StoryPoints ?? 0);

    /// <param name="plannedStories">Stories da equipe no mesmo PI e iteração (qualquer status).</param>
    /// <param name="storyId">Story sendo planejada (null na criação). Evita contar a mesma Story duas vezes.</param>
    /// <param name="newPoints">Pontos que a Story terá após a operação.</param>
    public static void EnsureWithinCapacity(IEnumerable<PlannedStory> plannedStories, int? storyId,
        int newPoints, int capacityPoints, int iteration)
    {
        var total = CalculateLoad(plannedStories, storyId) + newPoints;
        if (total > capacityPoints)
        {
            throw new ConflictException(
                ErrorCodes.CapacityExceeded,
                "Capacidade excedida",
                $"Capacidade excedida: {total}/{capacityPoints} pontos na iteração {iteration}.");
        }
    }
}

/// <summary>Dados mínimos de uma Story para o cálculo de carga.</summary>
public sealed record PlannedStory(int StoryId, int? StoryPoints);
