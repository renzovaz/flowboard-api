namespace FlowBoard.Api.Domain.Rules;

/// <summary>
/// R8 — Progresso (%) = pontos das Stories em Done ÷ pontos de todas as Stories estimadas × 100.
/// Stories sem pontos ficam fora do cálculo e são informadas em UnestimatedStories.
/// </summary>
public static class ProgressCalculator
{
    public static ProgressResult Calculate(IEnumerable<ProgressItem> stories)
    {
        var done = 0;
        var total = 0;
        var unestimated = 0;

        foreach (var story in stories)
        {
            if (story.StoryPoints is null)
            {
                unestimated++;
                continue;
            }

            total += story.StoryPoints.Value;
            if (story.IsDone)
            {
                done += story.StoryPoints.Value;
            }
        }

        // Sem pontos estimados → 0% (evita divisão por zero).
        var percent = total == 0 ? 0m : Math.Round(done * 100m / total, 1, MidpointRounding.AwayFromZero);
        return new ProgressResult(done, total, percent, unestimated);
    }
}

public sealed record ProgressItem(int? StoryPoints, bool IsDone);

public sealed record ProgressResult(int DonePoints, int TotalPoints, decimal Percent, int UnestimatedStories);
