using FlowBoard.Api.Domain.Entities;
using FlowBoard.Api.Domain.Exceptions;
using FlowBoard.Api.Domain.Rules;

namespace FlowBoard.UnitTests.Rules;

/// <summary>R1 — Fluxo Kanban com transições válidas.</summary>
public class KanbanWorkflowTests
{
    // U01
    [Theory]
    [InlineData(StoryStatus.Backlog, StoryStatus.Ready)]
    [InlineData(StoryStatus.Ready, StoryStatus.InProgress)]
    [InlineData(StoryStatus.InProgress, StoryStatus.Review)]
    [InlineData(StoryStatus.Review, StoryStatus.Done)]
    [InlineData(StoryStatus.Review, StoryStatus.InProgress)] // retrabalho
    public void CanMove_ValidTransitions_ReturnsTrue(StoryStatus from, StoryStatus to)
    {
        Assert.True(KanbanWorkflow.CanMove(from, to));
    }

    // U02
    [Theory]
    [InlineData(StoryStatus.Backlog, StoryStatus.Done)]        // pular etapas
    [InlineData(StoryStatus.Backlog, StoryStatus.InProgress)]
    [InlineData(StoryStatus.Ready, StoryStatus.Review)]
    [InlineData(StoryStatus.InProgress, StoryStatus.InProgress)] // mesmo status
    [InlineData(StoryStatus.Done, StoryStatus.Review)]        // Done é final
    [InlineData(StoryStatus.Ready, StoryStatus.Backlog)]
    public void EnsureCanMove_InvalidTransitions_ThrowsInvalidTransition(StoryStatus from, StoryStatus to)
    {
        var ex = Assert.Throws<ConflictException>(() => KanbanWorkflow.EnsureCanMove(from, to));

        Assert.Equal(ErrorCodes.InvalidTransition, ex.ErrorCode);
        Assert.Equal(409, ex.StatusCode);
    }
}
