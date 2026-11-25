using FlowBoard.Api.Domain.Entities;
using FlowBoard.Api.Domain.Exceptions;

namespace FlowBoard.UnitTests.Entities;

/// <summary>Comportamentos da entidade Story (datas do fluxo, RC1 e RC2).</summary>
public class StoryTests
{
    // U23
    [Fact]
    public void MoveTo_OnRework_KeepsFirstStartedAtAndSetsCompletedAt()
    {
        var story = TestData.StoryIn(StoryStatus.Review);
        var firstStart = story.StartedAt;

        story.MoveTo(StoryStatus.InProgress, TestData.Now.AddHours(3)); // retrabalho
        story.MoveTo(StoryStatus.Review, TestData.Now.AddHours(4));
        story.MoveTo(StoryStatus.Done, TestData.Now.AddHours(5));

        Assert.Equal(TestData.Now, firstStart);
        Assert.Equal(firstStart, story.StartedAt);
        Assert.Equal(TestData.Now.AddHours(5), story.CompletedAt);
    }

    [Fact]
    public void NewStory_StartsInBacklog()
    {
        Assert.Equal(StoryStatus.Backlog, TestData.ReadyCandidate().Status);
    }

    // RC1
    [Fact]
    public void UpdatePlanning_ClearingDorFieldsAfterBacklog_ThrowsDefinitionOfReadyViolated()
    {
        var story = TestData.StoryIn(StoryStatus.Ready);

        var ex = Assert.Throws<BusinessRuleException>(() =>
            story.UpdatePlanning(TestData.Pi(), story.Title, story.AcceptanceCriteria, storyPoints: null, story.TeamId, iteration: null));

        Assert.Equal(ErrorCodes.DefinitionOfReadyViolated, ex.ErrorCode);
    }

    // RC2
    [Fact]
    public void UpdatePlanning_ChangeTeamWhileInProgress_ThrowsTeamChangeNotAllowed()
    {
        var story = TestData.StoryIn(StoryStatus.InProgress);

        var ex = Assert.Throws<ConflictException>(() =>
            story.UpdatePlanning(TestData.Pi(), story.Title, story.AcceptanceCriteria, story.StoryPoints, teamId: 2, story.Iteration));

        Assert.Equal(ErrorCodes.TeamChangeNotAllowed, ex.ErrorCode);
    }

    [Fact]
    public void UpdatePlanning_ChangeTeamWhileReady_IsAllowed()
    {
        var story = TestData.StoryIn(StoryStatus.Ready);

        story.UpdatePlanning(TestData.Pi(), story.Title, story.AcceptanceCriteria, story.StoryPoints, teamId: 2, story.Iteration);

        Assert.Equal(2, story.TeamId);
    }
}
