using System.Net;
using FlowBoard.IntegrationTests.Infrastructure;
using Testcontainers.PostgreSql;

namespace FlowBoard.IntegrationTests.Api;

/// <summary>Health Checks e comportamento com o banco indisponível (I13, I15, I16).</summary>
public class HealthTests(ApiFixture api)
{
    // I13
    [Fact]
    public async Task HealthReady_WithDatabaseUp_Returns200AndReportsVersion()
    {
        var ct = TestContext.Current.CancellationToken;

        var live = await api.Client.GetAsync("/health/live", ct);
        var ready = await api.Client.GetAsync("/health/ready", ct);
        var report = await (await api.Client.GetAsync("/health", ct)).ReadJsonAsync();

        Assert.Equal(HttpStatusCode.OK, live.StatusCode);
        Assert.Equal(HttpStatusCode.OK, ready.StatusCode);
        Assert.Equal("Healthy", report.GetProperty("status").GetString());
        Assert.False(string.IsNullOrEmpty(report.GetProperty("version").GetString()));
        Assert.Equal("postgresql", report.GetProperty("checks")[0].GetProperty("name").GetString());
    }

    // I15 e I16 — contêiner EXCLUSIVO, parado durante o teste
    [Fact]
    public async Task Requests_WhenDatabaseStopped_Return503DatabaseUnavailable()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var postgres = new PostgreSqlBuilder("postgres:17-alpine").Build();
        await postgres.StartAsync(ct);
        await using var factory = ApiFixture.CreateFactory(postgres.GetConnectionString());
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/teams", ct)).StatusCode);

        await postgres.StopAsync(ct);

        // I15 — requisição de negócio
        var response = await client.GetAsync("/api/teams", ct);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("DATABASE_UNAVAILABLE", await response.ReadErrorCodeAsync());
        Assert.True(response.Headers.Contains("Retry-After"));

        // I16 — readiness
        var ready = await client.GetAsync("/health/ready", ct);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, ready.StatusCode);

        // liveness continua 200: o processo está vivo, a dependência é que caiu
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/live", ct)).StatusCode);
    }
}
