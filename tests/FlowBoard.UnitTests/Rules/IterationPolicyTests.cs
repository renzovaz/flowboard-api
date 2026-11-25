using FlowBoard.Api.Domain.Exceptions;
using FlowBoard.Api.Domain.Rules;

namespace FlowBoard.UnitTests.Rules;

/// <summary>R6 — Restrição de planejamento na iteração IP.</summary>
public class IterationPolicyTests
{
    // U15
    [Fact]
    public void Validate_IpIteration_ThrowsIpIterationNotPlannable()
    {
        var ex = Assert.Throws<BusinessRuleException>(() => IterationPolicy.Validate(5, TestData.Pi()));

        Assert.Equal(ErrorCodes.IpIterationNotPlannable, ex.ErrorCode);
    }

    // U16
    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(-1)]
    public void Validate_OutOfRange_ThrowsInvalidIteration(int iteration)
    {
        var ex = Assert.Throws<BusinessRuleException>(() => IterationPolicy.Validate(iteration, TestData.Pi()));

        Assert.Equal(ErrorCodes.InvalidIteration, ex.ErrorCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(1)]
    [InlineData(4)]
    public void Validate_DevelopmentIterationsOrNull_Accepts(int? iteration)
    {
        Assert.Null(Record.Exception(() => IterationPolicy.Validate(iteration, TestData.Pi())));
    }

    // U17
    [Fact]
    public void GetIterations_AreCalculatedFromStartDate()
    {
        var pi = TestData.Pi(); // início 2026-11-23, 5 iterações de 2 semanas

        var iterations = pi.GetIterations();

        Assert.Equal(5, iterations.Count);
        Assert.Equal(new DateOnly(2026, 11, 23), iterations[0].StartDate);
        Assert.Equal(new DateOnly(2026, 12, 6), iterations[0].EndDate);
        Assert.Equal(new DateOnly(2027, 1, 18), iterations[4].StartDate);
        Assert.True(iterations[4].IsIp);
        Assert.False(iterations[3].IsIp);
        Assert.Equal(new DateOnly(2027, 1, 31), pi.EndDate);
    }
}
