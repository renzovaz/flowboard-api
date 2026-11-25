using System.Net;
using FlowBoard.IntegrationTests.Infrastructure;

namespace FlowBoard.IntegrationTests.Api;

/// <summary>Stories e quadro Kanban via HTTP (E15–E18): R1–R6, RC1 e RC2.</summary>
public class StoriesWorkflowTests(ApiFixture api)
{
    private readonly Scenario _s = new(api.Client);

    private async Task<(int TeamId, int PiId, int FeatureId)> SetupAsync(int capacity = 20, int wipInProgress = 3)
    {
        var teamId = await _s.CreateTeamAsync(capacity, wipInProgress);
        var piId = await _s.CreatePiAsync();
        var featureId = await _s.CreateFeatureAsync(piId);
        return (teamId, piId, featureId);
    }

    // I04 — R1 + R8 de ponta a ponta
    [Fact]
    public async Task Story_FullFlowToDone_UpdatesPiProgress()
    {
        var (teamId, piId, featureId) = await SetupAsync();
        await _s.CreateStoryInAsync(featureId, teamId, "Done", points: 5);
        await _s.CreateStoryAsync(featureId, 5, teamId);

        var progress = await _s.GetJsonAsync($"/api/program-increments/{piId}/progress");

        Assert.Equal(50.0m, progress.GetProperty("overall").GetProperty("percent").GetDecimal());
        var board = await _s.GetJsonAsync($"/api/teams/{teamId}/board");
        Assert.Equal(1, board.GetProperty("columns")[4].GetProperty("count").GetInt32()); // Done
    }

    // I05 — R2 via HTTP (borda do limite; alvo da falha controlada)
    [Fact]
    public async Task MoveStory_WhenWipFull_Returns409WipLimitExceeded()
    {
        var (teamId, _, featureId) = await SetupAsync(wipInProgress: 3);
        for (var i = 0; i < 3; i++)
        {
            await _s.CreateStoryInAsync(featureId, teamId, "InProgress");
        }

        var fourth = await _s.CreateStoryInAsync(featureId, teamId, "Ready");

        var response = await _s.MoveAsync(fourth, "InProgress");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("WIP_LIMIT_EXCEEDED", await response.ReadErrorCodeAsync());
        var board = await _s.GetJsonAsync($"/api/teams/{teamId}/board");
        Assert.True(board.GetProperty("columns")[2].GetProperty("isFull").GetBoolean());
    }

    // I06
    [Fact]
    public async Task MoveStory_WipLimitIsPerTeam()
    {
        var (alpha, _, featureId) = await SetupAsync(wipInProgress: 1);
        var beta = await _s.CreateTeamAsync(wipInProgress: 1);
        await _s.CreateStoryInAsync(featureId, alpha, "InProgress"); // Alpha: 1/1 (cheia)
        var betaStory = await _s.CreateStoryInAsync(featureId, beta, "Ready");

        var response = await _s.MoveAsync(betaStory, "InProgress");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task MoveStory_SkippingColumns_Returns409InvalidTransition()
    {
        var (teamId, _, featureId) = await SetupAsync();
        var story = await _s.CreateStoryAsync(featureId, 3, teamId);

        var response = await _s.MoveAsync(story, "Done");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("INVALID_TRANSITION", await response.ReadErrorCodeAsync());
    }

    // I07 — R3
    [Fact]
    public async Task MoveToReady_WithoutDoR_Returns422WithMissingCriteria()
    {
        var (_, _, featureId) = await SetupAsync();
        var story = await _s.CreateStoryAsync(featureId, points: null, teamId: null, iteration: null);

        var response = await _s.MoveAsync(story, "Ready");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var body = await response.ReadJsonAsync();
        Assert.Equal("DEFINITION_OF_READY_NOT_MET", body.GetProperty("errorCode").GetString());
        Assert.Equal(["storyPoints", "teamId", "iteration"],
            body.GetProperty("missingCriteria").EnumerateArray().Select(e => e.GetString()));
    }

    // I08 — R5
    [Fact]
    public async Task CreateStory_OverCapacity_Returns409()
    {
        var (teamId, _, featureId) = await SetupAsync(capacity: 20);
        await _s.CreateStoryAsync(featureId, 13, teamId, iteration: 1);
        await _s.CreateStoryAsync(featureId, 2, teamId, iteration: 1); // 15/20

        var response = await api.Client.PostJsonAsync($"/api/features/{featureId}/stories",
            new { title = "Grande", storyPoints = 8, teamId, iteration = 1 });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("CAPACITY_EXCEEDED", await response.ReadErrorCodeAsync());
        var capacity = await _s.GetJsonAsync($"/api/teams/{teamId}/capacity?programIncrementId={(await _s.GetJsonAsync($"/api/features/{featureId}")).GetProperty("programIncrementId").GetInt32()}");
        Assert.Equal(15, capacity.GetProperty("iterations")[0].GetProperty("plannedPoints").GetInt32());
    }

    // I09 — R6
    [Fact]
    public async Task CreateStory_InIpIteration_Returns422()
    {
        var (teamId, _, featureId) = await SetupAsync();

        var response = await api.Client.PostJsonAsync($"/api/features/{featureId}/stories",
            new { title = "Na IP", storyPoints = 3, teamId, iteration = 5 });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("IP_ITERATION_NOT_PLANNABLE", await response.ReadErrorCodeAsync());
    }

    [Fact]
    public async Task CreateStory_NonFibonacciPoints_Returns422()
    {
        var (_, _, featureId) = await SetupAsync();

        var response = await api.Client.PostJsonAsync($"/api/features/{featureId}/stories", new { title = "X", storyPoints = 4 });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("INVALID_STORY_POINTS", await response.ReadErrorCodeAsync());
    }

    // I10 — RC2
    [Fact]
    public async Task UpdateStory_ChangeTeamWhileInProgress_Returns409()
    {
        var (alpha, _, featureId) = await SetupAsync();
        var beta = await _s.CreateTeamAsync();
        var story = await _s.CreateStoryInAsync(featureId, alpha, "InProgress");

        var response = await api.Client.PutJsonAsync($"/api/stories/{story}",
            new { title = "Troca", acceptanceCriteria = "Critérios de aceite completos", storyPoints = 1, teamId = beta, iteration = 1 });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("TEAM_CHANGE_NOT_ALLOWED", await response.ReadErrorCodeAsync());
    }

    // I11 — RC1
    [Fact]
    public async Task UpdateStory_ClearTeamAfterBacklog_Returns422()
    {
        var (teamId, _, featureId) = await SetupAsync();
        var story = await _s.CreateStoryInAsync(featureId, teamId, "Ready");

        var response = await api.Client.PutJsonAsync($"/api/stories/{story}",
            new { title = "Sem equipe", acceptanceCriteria = "Critérios de aceite completos", storyPoints = 1, iteration = 1 });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("DEFINITION_OF_READY_VIOLATED", await response.ReadErrorCodeAsync());
    }

    // I12
    [Fact]
    public async Task MoveStory_UnknownStatus_Returns400()
    {
        var (teamId, _, featureId) = await SetupAsync();
        var story = await _s.CreateStoryAsync(featureId, 3, teamId);

        var response = await _s.MoveAsync(story, "Testing");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("VALIDATION_ERROR", await response.ReadErrorCodeAsync());
    }

    [Fact]
    public async Task MoveStory_Unknown_Returns404()
    {
        var response = await _s.MoveAsync(999999, "Ready");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
