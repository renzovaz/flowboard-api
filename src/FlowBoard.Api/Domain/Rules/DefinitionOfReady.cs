using FlowBoard.Api.Domain.Entities;
using FlowBoard.Api.Domain.Exceptions;

namespace FlowBoard.Api.Domain.Rules;

/// <summary>
/// R3 — Definition of Ready: para entrar em Ready, a Story precisa de pontos, critérios de aceite
/// (mínimo de 10 caracteres não brancos), equipe e iteração.
/// </summary>
public static class DefinitionOfReady
{
    /// <summary>Retorna a lista de critérios NÃO atendidos (vazia quando a DoR está atendida).</summary>
    public static IReadOnlyList<string> GetMissingCriteria(int? storyPoints, string? acceptanceCriteria, int? teamId, int? iteration)
    {
        var missing = new List<string>();

        if (storyPoints is null)
        {
            missing.Add("storyPoints");
        }

        if (CountNonWhitespace(acceptanceCriteria) < DomainLimits.AcceptanceCriteriaMinChars)
        {
            missing.Add("acceptanceCriteria");
        }

        if (teamId is null)
        {
            missing.Add("teamId");
        }

        if (iteration is null)
        {
            missing.Add("iteration");
        }

        return missing;
    }

    public static IReadOnlyList<string> GetMissingCriteria(Story story) =>
        GetMissingCriteria(story.StoryPoints, story.AcceptanceCriteria, story.TeamId, story.Iteration);

    /// <summary>Lança HTTP 422 listando TODOS os critérios pendentes de uma vez.</summary>
    public static void Ensure(Story story)
    {
        var missing = GetMissingCriteria(story);
        if (missing.Count > 0)
        {
            throw new BusinessRuleException(
                ErrorCodes.DefinitionOfReadyNotMet,
                "Definition of Ready não atendida",
                $"Critérios pendentes: {string.Join(", ", missing)}.",
                new Dictionary<string, object?> { ["missingCriteria"] = missing });
        }
    }

    private static int CountNonWhitespace(string? text) =>
        text is null ? 0 : text.Count(c => !char.IsWhiteSpace(c));
}
