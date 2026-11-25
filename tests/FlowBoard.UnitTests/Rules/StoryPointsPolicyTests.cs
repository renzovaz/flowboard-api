using FlowBoard.Api.Domain.Exceptions;
using FlowBoard.Api.Domain.Rules;

namespace FlowBoard.UnitTests.Rules;

/// <summary>R4 — Story Points na sequência Fibonacci aprovada.</summary>
public class StoryPointsPolicyTests
{
    // U09
    [Theory]
    [InlineData(null)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(8)]
    [InlineData(13)]
    public void Validate_FibonacciValuesOrNull_Accepts(int? points)
    {
        Assert.Null(Record.Exception(() => StoryPointsPolicy.Validate(points)));
    }

    // U10
    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(7)]
    [InlineData(-1)]
    public void Validate_NonFibonacci_ThrowsInvalidStoryPoints(int points)
    {
        var ex = Assert.Throws<BusinessRuleException>(() => StoryPointsPolicy.Validate(points));

        Assert.Equal(ErrorCodes.InvalidStoryPoints, ex.ErrorCode);
        Assert.Equal(422, ex.StatusCode);
    }

    // U11
    [Theory]
    [InlineData(20)]
    [InlineData(21)]
    public void Validate_Above13_ThrowsStoryTooLarge(int points)
    {
        var ex = Assert.Throws<BusinessRuleException>(() => StoryPointsPolicy.Validate(points));

        Assert.Equal(ErrorCodes.StoryTooLarge, ex.ErrorCode);
    }
}
