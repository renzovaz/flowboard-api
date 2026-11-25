using FlowBoard.Api.Domain.Entities;
using FlowBoard.Api.Domain.Exceptions;
using FlowBoard.Api.Domain.Rules;

namespace FlowBoard.UnitTests.Rules;

/// <summary>R3 — Definition of Ready para entrada em Ready.</summary>
public class DefinitionOfReadyTests
{
    // U06
    [Fact]
    public void GetMissingCriteria_WhenAllCriteriaMet_ReturnsEmpty()
    {
        var missing = DefinitionOfReady.GetMissingCriteria(TestData.ReadyCandidate());

        Assert.Empty(missing);
    }

    // U07
    [Fact]
    public void Ensure_WhenPointsAndIterationMissing_ListsBoth()
    {
        var story = new Story(1, TestData.Pi(), "Sem estimativa", "Critérios de aceite completos", null, 1, null, TestData.Now);

        var ex = Assert.Throws<BusinessRuleException>(() => story.MoveTo(StoryStatus.Ready, TestData.Now));

        Assert.Equal(ErrorCodes.DefinitionOfReadyNotMet, ex.ErrorCode);
        var missing = Assert.IsAssignableFrom<IReadOnlyList<string>>(ex.Extensions["missingCriteria"]);
        Assert.Equal(["storyPoints", "iteration"], missing);
        Assert.Equal(StoryStatus.Backlog, story.Status); // nada mudou
    }

    // U08
    [Theory]
    [InlineData(null)]
    [InlineData("          ")]
    [InlineData("ok ok ok")]       // 6 caracteres não brancos
    [InlineData("a b c d e f g h i")] // 9 caracteres não brancos
    public void GetMissingCriteria_WhenCriteriaIsMissingWhitespaceOrShort_ReportsAcceptanceCriteria(string? criteria)
    {
        var missing = DefinitionOfReady.GetMissingCriteria(5, criteria, 1, 2);

        Assert.Equal(["acceptanceCriteria"], missing);
    }
}
