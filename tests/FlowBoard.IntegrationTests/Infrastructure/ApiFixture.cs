using FlowBoard.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

[assembly: AssemblyFixture(typeof(FlowBoard.IntegrationTests.Infrastructure.ApiFixture))]

namespace FlowBoard.IntegrationTests.Infrastructure;

/// <summary>
/// Sobe UM contêiner PostgreSQL 17 (Testcontainers) e UMA instância da API em memória
/// (WebApplicationFactory), compartilhados por todos os testes de integração.
/// Cada teste cria seus próprios dados com nomes únicos, então não há limpeza entre testes.
/// </summary>
public sealed class ApiFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine").Build();

    public WebApplicationFactory<Program> Factory { get; private set; } = null!;

    public HttpClient Client { get; private set; } = null!;

    public string ConnectionString => _postgres.GetConnectionString();

    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync();
        Factory = CreateFactory(ConnectionString);
        Client = Factory.CreateClient();
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await Factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    /// <summary>Cria uma instância da API apontando para o banco informado (migrations aplicadas na subida).</summary>
    public static WebApplicationFactory<Program> CreateFactory(string connectionString) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:Default", connectionString);
            builder.UseSetting("Database:ApplyMigrationsOnStartup", "true");
        });

    /// <summary>Cria um DbContext novo (escopo próprio) para preparar ou verificar dados.</summary>
    public FlowBoardDbContext CreateDbContext() =>
        Factory.Services.CreateScope().ServiceProvider.GetRequiredService<FlowBoardDbContext>();

    /// <summary>Sufixo único para nomes de equipes e PIs (isolamento entre testes).</summary>
    public static string Unique(string prefix) => $"{prefix}-{Guid.NewGuid():N}"[..Math.Min(prefix.Length + 9, 50)];
}
