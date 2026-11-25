using System.Net;
using FlowBoard.Api.Domain.Entities;
using FlowBoard.IntegrationTests.Infrastructure;

namespace FlowBoard.IntegrationTests.Api;

/// <summary>Endpoints de Program Increments (E07–E10), incluindo progresso (R8).</summary>
public class ProgramIncrementsTests(ApiFixture api)
{
    private static readonly DateTime Now = new(2026, 11, 20, 13, 0, 0, DateTimeKind.Utc);
    private readonly Scenario _scenario = new(api.Client);

    [Fact]
    public async Task PostPi_ReturnsIterationsWithIpAsLast()
    {
        var response = await api.Client.PostJsonAsync("/api/program-increments",
            new { name = ApiFixture.Unique("PI"), startDate = "2026-11-23" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var pi = await response.ReadJsonAsync();
        Assert.Equal(5, pi.GetProperty("iterationCount").GetInt32());
        Assert.Equal(5, pi.GetProperty("ipIteration").GetInt32());
        Assert.Equal("2027-01-31", pi.GetProperty("endDate").GetString());
        var iterations = pi.GetProperty("iterations").EnumerateArray().ToList();
        Assert.True(iterations[4].GetProperty("isIp").GetBoolean());
    }

    [Fact]
    public async Task PostPi_IterationCountOutOfRange_Returns422()
    {
        var response = await api.Client.PostJsonAsync("/api/program-increments",
            new { name = ApiFixture.Unique("PI"), startDate = "2026-11-23", iterationCount = 7 });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("INVALID_RANGE", await response.ReadErrorCodeAsync());
    }

    // R8 — progresso calculado por pontos (dados preparados diretamente no domínio).
    [Fact]
    public async Task GetProgress_ComputesOverallByFeatureAndByTeam()
    {
        var ct = TestContext.Current.CancellationToken;
        var teamId = await _scenario.CreateTeamAsync();
        var piId = await _scenario.CreatePiAsync();

        await using (var db = api.CreateDbContext())
        {
            var pi = await db.ProgramIncrements.FindAsync([piId], ct);
            var feature = new Feature(piId, "Login social", null, 13, 8, 2, 3, Now);
            db.Features.Add(feature);
            await db.SaveChangesAsync(ct);

            // A(5, Done) + B(3, Done) + C(8, InProgress) + D(sem pontos) → 8/16 = 50%
            var a = NewStory(feature.Id, pi!, 5, teamId);
            var b = NewStory(feature.Id, pi!, 3, teamId);
            var c = NewStory(feature.Id, pi!, 8, teamId);
            var d = new Story(feature.Id, pi!, "D", null, null, null, null, Now);
            MoveTo(a, StoryStatus.Done);
            MoveTo(b, StoryStatus.Done);
            MoveTo(c, StoryStatus.InProgress);
            db.Stories.AddRange(a, b, c, d);
            await db.SaveChangesAsync(ct);
        }

        var progress = await _scenario.GetJsonAsync($"/api/program-increments/{piId}/progress");

        var overall = progress.GetProperty("overall");
        Assert.Equal(8, overall.GetProperty("donePoints").GetInt32());
        Assert.Equal(16, overall.GetProperty("totalPoints").GetInt32());
        Assert.Equal(50.0m, overall.GetProperty("percent").GetDecimal());
        Assert.Equal(1, overall.GetProperty("unestimatedStories").GetInt32());
        Assert.Equal(50.0m, progress.GetProperty("features")[0].GetProperty("percent").GetDecimal());
        var team = progress.GetProperty("teams")[0];
        Assert.Equal(teamId, team.GetProperty("teamId").GetInt32());
        Assert.Equal(16, team.GetProperty("totalPoints").GetInt32());
    }

    [Fact]
    public async Task GetProgress_UnknownPi_Returns404()
    {
        var response = await api.Client.GetAsync("/api/program-increments/999999/progress", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static Story NewStory(int featureId, ProgramIncrement pi, int points, int teamId) =>
        new(featureId, pi, $"Story {points}", "Critérios de aceite completos", points, teamId, 1, Now);

    private static void MoveTo(Story story, StoryStatus target)
    {
        foreach (var step in new[] { StoryStatus.Ready, StoryStatus.InProgress, StoryStatus.Review, StoryStatus.Done }.TakeWhile(s => s <= target))
        {
            story.MoveTo(step, Now);
        }
    }
}
