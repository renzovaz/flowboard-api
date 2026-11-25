using FlowBoard.Api.Domain.Exceptions;
using FlowBoard.Api.Domain.Rules;

namespace FlowBoard.UnitTests.Rules;

/// <summary>R5 — Capacidade da equipe por iteração.</summary>
public class CapacityPolicyTests
{
    private static readonly PlannedStory[] Load15 = [new(1, 8), new(2, 5), new(3, 2)];

    // U12
    [Fact]
    public void EnsureWithinCapacity_WhenLoadEqualsCapacity_Allows()
    {
        // 15 + 5 = 20/20: o limite exato é permitido.
        Assert.Null(Record.Exception(() => CapacityPolicy.EnsureWithinCapacity(Load15, null, 5, 20, 1)));
    }

    // U13
    [Fact]
    public void EnsureWithinCapacity_WhenExceeds_ThrowsCapacityExceeded()
    {
        var ex = Assert.Throws<ConflictException>(() => CapacityPolicy.EnsureWithinCapacity(Load15, null, 8, 20, 1));

        Assert.Equal(ErrorCodes.CapacityExceeded, ex.ErrorCode);
        Assert.Contains("23/20", ex.Message, StringComparison.Ordinal);
    }

    // U14
    [Fact]
    public void EnsureWithinCapacity_OnUpdate_DoesNotCountStoryTwice()
    {
        // Carga 20/20; a Story 2 muda de 5 para 3 pontos → 18/20, permitido.
        PlannedStory[] load20 = [new(1, 8), new(2, 5), new(3, 7)];

        Assert.Equal(15, CapacityPolicy.CalculateLoad(load20, excludeStoryId: 2));
        Assert.Null(Record.Exception(() => CapacityPolicy.EnsureWithinCapacity(load20, 2, 3, 20, 1)));
    }

    [Fact]
    public void CalculateLoad_UnestimatedStoriesCountAsZero()
    {
        PlannedStory[] stories = [new(1, 5), new(2, null)];

        Assert.Equal(5, CapacityPolicy.CalculateLoad(stories, null));
    }
}
