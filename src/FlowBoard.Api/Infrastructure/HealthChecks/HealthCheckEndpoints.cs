using System.Text.Json;
using FlowBoard.Api.Data;
using FlowBoard.Api.Infrastructure.Observability;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FlowBoard.Api.Infrastructure.HealthChecks;

/// <summary>
/// /health/live  → o processo responde (não consulta dependências);
/// /health/ready → consegue consultar o PostgreSQL? (usado pelo HEALTHCHECK do Docker e pelo smoke test);
/// /health       → relatório completo com versão e duração.
/// Unhealthy → HTTP 503.
/// </summary>
public static class HealthCheckEndpoints
{
    private const string ReadyTag = "ready";

    public static IServiceCollection AddFlowBoardHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddDbContextCheck<FlowBoardDbContext>("postgresql", HealthStatus.Unhealthy, [ReadyTag]);

        // Nenhuma verificação pode travar o endpoint por mais de 3 segundos.
        services.Configure<HealthCheckServiceOptions>(options =>
        {
            foreach (var registration in options.Registrations)
            {
                registration.Timeout = TimeSpan.FromSeconds(3);
            }
        });
        return services;
    }

    public static void MapFlowBoardHealthChecks(this WebApplication app)
    {
        app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false })
            .WithHttpLogging(HttpLoggingFields.None);
        app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = c => c.Tags.Contains(ReadyTag) })
            .WithHttpLogging(HttpLoggingFields.None);
        app.MapHealthChecks("/health", new HealthCheckOptions { ResponseWriter = WriteReportAsync })
            .WithHttpLogging(HttpLoggingFields.None);
    }

    private static Task WriteReportAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";
        var body = new
        {
            status = report.Status.ToString(),
            version = AppInfo.Version,
            totalDurationMs = Math.Round(report.TotalDuration.TotalMilliseconds, 1),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                durationMs = Math.Round(e.Value.Duration.TotalMilliseconds, 1),
                error = e.Value.Exception is null ? null : "Falha ao consultar a dependência.",
            }),
        };
        return context.Response.WriteAsync(JsonSerializer.Serialize(body, JsonSerializerOptions.Web));
    }
}
