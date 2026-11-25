using System.Net;
using FlowBoard.IntegrationTests.Infrastructure;

namespace FlowBoard.IntegrationTests.Concurrency;

/// <summary>
/// I14 — ADR-002: requisições SIMULTÂNEAS nunca ultrapassam o limite de WIP.
/// Sem o lock FOR UPDATE, várias requisições leriam a mesma contagem e o limite seria violado.
/// </summary>
public class WipLimitConcurrencyTests(ApiFixture api)
{
    private readonly Scenario _s = new(api.Client);

    [Fact]
    public async Task ConcurrentMoves_NeverExceedWipLimit()
    {
        // Arrange: equipe com WIP 3 em InProgress e 10 Stories prontas (Ready)
        var teamId = await _s.CreateTeamAsync(capacity: 200, wipInProgress: 3);
        var piId = await _s.CreatePiAsync();
        var featureId = await _s.CreateFeatureAsync(piId);
        var stories = new List<int>();
        for (var i = 0; i < 10; i++)
        {
            stories.Add(await _s.CreateStoryInAsync(featureId, teamId, "Ready"));
        }

        // Act: 10 requisições disparadas ao mesmo tempo
        var responses = await Task.WhenAll(stories.Select(id => _s.MoveAsync(id, "InProgress")));

        // Assert: exatamente 3 entram, 7 são recusadas, e o banco confirma 3 em InProgress
        Assert.Equal(3, responses.Count(r => r.StatusCode == HttpStatusCode.OK));
        Assert.Equal(7, responses.Count(r => r.StatusCode == HttpStatusCode.Conflict));
        var board = await _s.GetJsonAsync($"/api/teams/{teamId}/board");
        Assert.Equal(3, board.GetProperty("columns")[2].GetProperty("count").GetInt32());
    }
}
