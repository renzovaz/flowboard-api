using FlowBoard.Api.Domain.Entities;
using FlowBoard.Api.Domain.Exceptions;
using FlowBoard.Api.Domain.Rules;

namespace FlowBoard.UnitTests.Rules;

/// <summary>R2 — Limite de WIP por equipe e coluna.</summary>
public class WipPolicyTests
{
    // U03
    [Fact]
    public void EnsureCanEnter_WhenBelowLimit_Allows()
    {
        // 2 Stories em InProgress, limite 3: a terceira pode entrar.
        var exception = Record.Exception(() => WipPolicy.EnsureCanEnter(StoryStatus.InProgress, 2, 3, "Alpha"));

        Assert.Null(exception);
    }

    // U04 — borda do limite (alvo da demonstração de falha controlada no CI)
    [Fact]
    public void EnsureCanEnter_WhenColumnIsAtLimit_ThrowsWipLimitExceeded()
    {
        // 3 Stories em InProgress, limite 3: a quarta deve ser recusada.
        var ex = Assert.Throws<ConflictException>(() => WipPolicy.EnsureCanEnter(StoryStatus.InProgress, 3, 3, "Alpha"));

        Assert.Equal(ErrorCodes.WipLimitExceeded, ex.ErrorCode);
        Assert.Contains("(3/3)", ex.Message, StringComparison.Ordinal);
    }

    // U05
    [Fact]
    public void GetWipLimit_ReviewColumn_UsesReviewLimit()
    {
        var team = new Team("Alpha", 20, wipLimitInProgress: 3, wipLimitReview: 2, TestData.Now);

        Assert.Equal(3, team.GetWipLimit(StoryStatus.InProgress));
        Assert.Equal(2, team.GetWipLimit(StoryStatus.Review));
        Assert.Null(team.GetWipLimit(StoryStatus.Backlog));
        Assert.Throws<ConflictException>(() =>
            WipPolicy.EnsureCanEnter(StoryStatus.Review, 2, team.GetWipLimit(StoryStatus.Review), team.Name));
    }

    [Fact]
    public void EnsureCanEnter_ColumnWithoutLimit_AlwaysAllows()
    {
        var exception = Record.Exception(() => WipPolicy.EnsureCanEnter(StoryStatus.Done, 500, null, "Alpha"));

        Assert.Null(exception);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(21)]
    public void Team_WipLimitOutOfRange_ThrowsInvalidRange(int limit)
    {
        var ex = Assert.Throws<BusinessRuleException>(() => new Team("Alpha", 20, limit, 2, TestData.Now));

        Assert.Equal(ErrorCodes.InvalidRange, ex.ErrorCode);
    }
}
