var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.Run();

/// <summary>
/// Torna a classe Program acessível aos testes de integração (WebApplicationFactory).
/// </summary>
public partial class Program;
