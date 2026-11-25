using System.Net;
using FlowBoard.IntegrationTests.Infrastructure;

namespace FlowBoard.IntegrationTests.Api;

/// <summary>Endpoints de equipes (E01–E06) via HTTP com PostgreSQL real.</summary>
public class TeamsTests(ApiFixture api)
{
    private readonly Scenario _scenario = new(api.Client);

    // I01
    [Fact]
    public async Task PostTeam_ReturnsCreatedWithLocation()
    {
        var name = ApiFixture.Unique("Alpha");

        var response = await api.Client.PostJsonAsync("/api/teams",
            new { name, capacityPoints = 20, wipLimitInProgress = 3, wipLimitReview = 2 });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.ReadJsonAsync();
        var id = body.GetProperty("id").GetInt32();
        Assert.EndsWith($"/api/teams/{id}", response.Headers.Location!.ToString(), StringComparison.Ordinal);
        Assert.Equal(name, body.GetProperty("name").GetString());
        Assert.Equal(3, body.GetProperty("wipLimitInProgress").GetInt32());
    }

    // I02
    [Fact]
    public async Task PostTeam_DuplicateName_Returns409()
    {
        var team = new { name = ApiFixture.Unique("Dup"), capacityPoints = 20, wipLimitInProgress = 3, wipLimitReview = 2 };
        await api.Client.PostJsonAsync("/api/teams", team);

        var response = await api.Client.PostJsonAsync("/api/teams", team);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("DUPLICATE_NAME", await response.ReadErrorCodeAsync());
    }

    [Fact]
    public async Task PostTeam_WipLimitOutOfRange_Returns422InvalidRange()
    {
        var response = await api.Client.PostJsonAsync("/api/teams",
            new { name = ApiFixture.Unique("Range"), capacityPoints = 20, wipLimitInProgress = 21, wipLimitReview = 2 });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("INVALID_RANGE", await response.ReadErrorCodeAsync());
    }

    [Fact]
    public async Task PostTeam_MissingRequiredField_Returns400ValidationError()
    {
        var response = await api.Client.PostJsonAsync("/api/teams", new { name = ApiFixture.Unique("Missing"), capacityPoints = 20 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.ReadJsonAsync();
        Assert.Equal("VALIDATION_ERROR", body.GetProperty("errorCode").GetString());
        Assert.True(body.TryGetProperty("traceId", out _));
    }

    [Fact]
    public async Task GetTeam_Unknown_Returns404()
    {
        var response = await api.Client.GetAsync("/api/teams/999999", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("RESOURCE_NOT_FOUND", await response.ReadErrorCodeAsync());
    }

    [Fact]
    public async Task GetBoard_NewTeam_ReturnsFiveEmptyColumnsWithWipLimits()
    {
        var teamId = await _scenario.CreateTeamAsync(wipInProgress: 3, wipReview: 2);

        var board = await _scenario.GetJsonAsync($"/api/teams/{teamId}/board");

        var columns = board.GetProperty("columns").EnumerateArray().ToList();
        Assert.Equal(["Backlog", "Ready", "InProgress", "Review", "Done"], columns.Select(c => c.GetProperty("status").GetString()));
        Assert.Equal(3, columns[2].GetProperty("wipLimit").GetInt32());
        Assert.False(columns[2].GetProperty("isFull").GetBoolean());
    }
}
