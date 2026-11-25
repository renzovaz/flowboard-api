using System.Net;
using System.Text.Json;

namespace FlowBoard.IntegrationTests.Infrastructure;

/// <summary>
/// Cria dados de teste PELA API (mesmas regras de produção), com nomes únicos por teste.
/// </summary>
internal sealed class Scenario(HttpClient client)
{
    public HttpClient Client { get; } = client;

    public async Task<int> CreateTeamAsync(int capacity = 20, int wipInProgress = 3, int wipReview = 2)
    {
        var response = await Client.PostJsonAsync("/api/teams", new
        {
            name = ApiFixture.Unique("Team"),
            capacityPoints = capacity,
            wipLimitInProgress = wipInProgress,
            wipLimitReview = wipReview,
        });
        return await IdOf(response);
    }

    public async Task<int> CreatePiAsync(int iterationCount = 5)
    {
        var response = await Client.PostJsonAsync("/api/program-increments", new
        {
            name = ApiFixture.Unique("PI"),
            startDate = "2026-11-23",
            iterationCount,
        });
        return await IdOf(response);
    }

    public async Task<int> CreateFeatureAsync(int piId, int bv = 8, int tc = 5, int rr = 3, int jobSize = 5, string title = "Feature")
    {
        var response = await Client.PostJsonAsync($"/api/program-increments/{piId}/features", new
        {
            title,
            businessValue = bv,
            timeCriticality = tc,
            riskReduction = rr,
            jobSize,
        });
        return await IdOf(response);
    }

    public static async Task<int> IdOf(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.ReadJsonAsync()).GetProperty("id").GetInt32();
    }

    public async Task<JsonElement> GetJsonAsync(string url)
    {
        var response = await Client.GetAsync(url, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.ReadJsonAsync();
    }
}
