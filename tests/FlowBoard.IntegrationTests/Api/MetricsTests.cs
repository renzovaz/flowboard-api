using System.Net;
using FlowBoard.IntegrationTests.Infrastructure;

namespace FlowBoard.IntegrationTests.Api;

/// <summary>Endpoint /metrics (v1.1.0): métricas HTTP e de fluxo Kanban no formato Prometheus.</summary>
public class MetricsTests(ApiFixture api)
{
    private readonly Scenario _s = new(api.Client);

    [Fact]
    public async Task Metrics_ExposesHttpAndFlowMetrics()
    {
        var ct = TestContext.Current.CancellationToken;
        var teamId = await _s.CreateTeamAsync(wipInProgress: 1);
        var piId = await _s.CreatePiAsync();
        var featureId = await _s.CreateFeatureAsync(piId);
        await _s.CreateStoryInAsync(featureId, teamId, "Done");
        await _s.CreateStoryInAsync(featureId, teamId, "InProgress");
        var blocked = await _s.CreateStoryInAsync(featureId, teamId, "Ready");
        Assert.Equal(HttpStatusCode.Conflict, (await _s.MoveAsync(blocked, "InProgress")).StatusCode);

        var response = await api.Client.GetAsync("/metrics", ct);
        var text = await response.Content.ReadAsStringAsync(ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("http_request_duration_seconds", text, StringComparison.Ordinal);
        Assert.Contains("flowboard_story_moves_total{from=\"Review\",to=\"Done\"}", text, StringComparison.Ordinal);
        Assert.Contains($"flowboard_wip_rejections_total{{team=\"{teamId}\"}} 1", text, StringComparison.Ordinal);
        Assert.Contains($"flowboard_stories_completed_total{{team=\"{teamId}\"}} 1", text, StringComparison.Ordinal);
        Assert.Contains($"flowboard_wip_current{{team=\"{teamId}\",column=\"InProgress\"}} 1", text, StringComparison.Ordinal);
        Assert.Contains("flowboard_story_cycle_time_seconds_count", text, StringComparison.Ordinal);
    }
}
