using FlowBoard.Api.Domain.Entities;

namespace FlowBoard.UnitTests;

/// <summary>Objetos de domínio prontos para os testes (padrão "Test Data Builder" simplificado).</summary>
internal static class TestData
{
    public static readonly DateTime Now = new(2026, 11, 20, 13, 0, 0, DateTimeKind.Utc);

    /// <summary>PI com 5 iterações de 2 semanas; a iteração 5 é a IP.</summary>
    public static ProgramIncrement Pi() => new("PI 2026.1", new DateOnly(2026, 11, 23), 5, 2, Now);

    /// <summary>Story que atende à Definition of Ready (5 pontos, equipe 1, iteração 2).</summary>
    public static Story ReadyCandidate() =>
        new(1, Pi(), "Login com Google", "Dado um usuário cadastrado, quando...", 5, 1, 2, Now);

    /// <summary>Cria uma Story e a move pelas colunas informadas.</summary>
    public static Story StoryIn(StoryStatus status)
    {
        var story = ReadyCandidate();
        var path = new[] { StoryStatus.Ready, StoryStatus.InProgress, StoryStatus.Review, StoryStatus.Done };
        foreach (var step in path.TakeWhile(s => s <= status))
        {
            story.MoveTo(step, Now);
        }

        return story;
    }
}
