using System.Net;
using FlowBoard.IntegrationTests.Infrastructure;

namespace FlowBoard.IntegrationTests.Api;

/// <summary>Features e WSJF (E11–E14, R7).</summary>
public class FeaturesTests(ApiFixture api)
{
    private readonly Scenario _scenario = new(api.Client);

    // I03
    [Fact]
    public async Task GetFeatures_ReturnsOrderedByWsjfWithRank()
    {
        var piId = await _scenario.CreatePiAsync();
        await _scenario.CreateFeatureAsync(piId, 8, 5, 3, 5, "Notificações"); // 16/5 = 3,20
        await _scenario.CreateFeatureAsync(piId, 13, 8, 2, 3, "Login social"); // 23/3 = 7,67
        await _scenario.CreateFeatureAsync(piId, 5, 2, 1, 8, "Relatórios");    // 8/8 = 1,00

        var features = (await _scenario.GetJsonAsync($"/api/program-increments/{piId}/features")).EnumerateArray().ToList();

        Assert.Equal(["Login social", "Notificações", "Relatórios"], features.Select(f => f.GetProperty("title").GetString()));
        Assert.Equal([1, 2, 3], features.Select(f => f.GetProperty("rank").GetInt32()));
        Assert.Equal(7.67m, features[0].GetProperty("wsjf").GetDecimal());
        Assert.Equal(23, features[0].GetProperty("costOfDelay").GetInt32());
    }

    [Fact]
    public async Task PostFeature_ValueOutsideScale_Returns422InvalidWsjfValue()
    {
        var piId = await _scenario.CreatePiAsync();

        var response = await api.Client.PostJsonAsync($"/api/program-increments/{piId}/features",
            new { title = "Inválida", businessValue = 8, timeCriticality = 5, riskReduction = 3, jobSize = 4 });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("INVALID_WSJF_VALUE", await response.ReadErrorCodeAsync());
    }

    [Fact]
    public async Task PutFeature_ChangesWsjfAndRanking()
    {
        var piId = await _scenario.CreatePiAsync();
        var first = await _scenario.CreateFeatureAsync(piId, 13, 8, 2, 3, "A");
        var second = await _scenario.CreateFeatureAsync(piId, 5, 2, 1, 8, "B");

        // B passa a ter WSJF (20+20+20)/1 = 60
        var response = await api.Client.PutJsonAsync($"/api/features/{second}",
            new { title = "B", businessValue = 20, timeCriticality = 20, riskReduction = 20, jobSize = 1 });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var features = (await _scenario.GetJsonAsync($"/api/program-increments/{piId}/features")).EnumerateArray().ToList();
        Assert.Equal([second, first], features.Select(f => f.GetProperty("id").GetInt32()));
    }

    [Fact]
    public async Task PostFeature_UnknownPi_Returns404()
    {
        var response = await api.Client.PostJsonAsync("/api/program-increments/999999/features",
            new { title = "X", businessValue = 1, timeCriticality = 1, riskReduction = 1, jobSize = 1 });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
