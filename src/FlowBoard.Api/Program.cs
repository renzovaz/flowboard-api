using FlowBoard.Api.Infrastructure.Database;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddFlowBoardDatabase();

var app = builder.Build();

await app.ApplyMigrationsIfEnabledAsync();

await app.RunAsync();

/// <summary>
/// Torna a classe Program acessível aos testes de integração (WebApplicationFactory).
/// </summary>
public partial class Program;
