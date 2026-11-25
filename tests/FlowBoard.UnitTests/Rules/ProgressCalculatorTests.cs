using FlowBoard.Api.Domain.Rules;

namespace FlowBoard.UnitTests.Rules;

/// <summary>R8 — Progresso por pontos.</summary>
public class ProgressCalculatorTests
{
    // U21
    [Fact]
    public void Calculate_ExcludesUnestimatedStories()
    {
        // A(5, Done) + B(3, Done) + C(8) + D(sem pontos) → 8/16 = 50,0%
        ProgressItem[] stories = [new(5, true), new(3, true), new(8, false), new(null, false)];

        var result = ProgressCalculator.Calculate(stories);

        Assert.Equal(new ProgressResult(DonePoints: 8, TotalPoints: 16, Percent: 50.0m, UnestimatedStories: 1), result);
    }

    // U22
    [Fact]
    public void Calculate_WhenNoPoints_ReturnsZero()
    {
        var result = ProgressCalculator.Calculate([new(null, false)]);

        Assert.Equal(0m, result.Percent);
        Assert.Equal(1, result.UnestimatedStories);
    }

    [Fact]
    public void Calculate_RoundsToOneDecimal()
    {
        // 8 / 13 = 61,538...% → 61,5%
        var result = ProgressCalculator.Calculate([new(5, true), new(3, true), new(5, false)]);

        Assert.Equal(61.5m, result.Percent);
    }
}
