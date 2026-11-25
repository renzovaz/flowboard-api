using FlowBoard.Api.Infrastructure;
using FlowBoard.Api.Infrastructure.Database;
using FlowBoard.Api.Infrastructure.HealthChecks;
using FlowBoard.Api.Infrastructure.Observability;

var builder = WebApplication.CreateBuilder(args);

builder.AddFlowBoardLogging();
builder.Services.AddFlowBoard();

var app = builder.Build();

app.UseExceptionHandler();      // exceções → Problem Details (GlobalExceptionHandler)
app.UseStatusCodePages();       // 404/405 sem corpo → Problem Details
app.UseHttpLogging();           // uma linha de log por requisição (método, caminho, status, duração)
app.UseFlowBoardSwaggerIfEnabled();
app.MapFlowBoardHealthChecks();
app.MapControllers();

await app.ApplyMigrationsIfEnabledAsync();

await app.RunAsync();

/// <summary>
/// Torna a classe Program acessível aos testes de integração (WebApplicationFactory).
/// </summary>
public partial class Program;
