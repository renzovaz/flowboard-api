using FlowBoard.Api.Infrastructure;
using FlowBoard.Api.Infrastructure.Database;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddFlowBoard();

var app = builder.Build();

app.UseExceptionHandler();      // exceções → Problem Details (GlobalExceptionHandler)
app.UseStatusCodePages();       // 404/405 sem corpo → Problem Details
app.UseFlowBoardSwaggerIfEnabled();
app.MapControllers();

await app.ApplyMigrationsIfEnabledAsync();

await app.RunAsync();

/// <summary>
/// Torna a classe Program acessível aos testes de integração (WebApplicationFactory).
/// </summary>
public partial class Program;
