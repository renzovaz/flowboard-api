using System.Text.Json.Serialization;
using FlowBoard.Api.Infrastructure.Database;
using FlowBoard.Api.Infrastructure.ErrorHandling;
using FlowBoard.Api.Infrastructure.HealthChecks;
using FlowBoard.Api.Services;

namespace FlowBoard.Api.Infrastructure;

/// <summary>Registro de dependências (injeção de dependência nativa do ASP.NET Core).</summary>
public static partial class ServiceRegistration
{
    public static IServiceCollection AddFlowBoard(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddFlowBoardDatabase();
        services.AddFlowBoardProblemDetails();
        services.AddFlowBoardHealthChecks();

        services.AddScoped<TeamService>();
        services.AddScoped<ProgramIncrementService>();
        services.AddScoped<FeatureService>();
        services.AddScoped<StoryService>();

        services.AddControllers()
            // Enums como texto no JSON ("InProgress" em vez de 2).
            .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        services.AddOpenApi();
        return services;
    }

    /// <summary>
    /// Swagger UI e documento OpenAPI: desligados por padrão (ADR-018).
    /// Ativados por Swagger:Enabled=true (Development, ou explicitamente na demonstração).
    /// </summary>
    public static void UseFlowBoardSwaggerIfEnabled(this WebApplication app)
    {
        if (!app.Configuration.GetValue<bool>("Swagger:Enabled"))
        {
            return;
        }

        if (app.Environment.IsProduction())
        {
            LogSwaggerInProduction(app.Logger);
        }

        app.MapOpenApi();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/openapi/v1.json", "FlowBoard API v1");
            options.DocumentTitle = "FlowBoard API";
        });
    }

    [LoggerMessage(EventId = 3001, Level = LogLevel.Warning,
        Message = "SwaggerEnabledInProduction: documentação interativa habilitada em Production (uso restrito à demonstração)")]
    private static partial void LogSwaggerInProduction(ILogger logger);
}
