using Microsoft.AspNetCore.HttpLogging;

namespace FlowBoard.Api.Infrastructure.Observability;

/// <summary>
/// Logs estruturados em JSON no console (o Docker coleta o stdout) com escopos
/// (TraceId, RequestPath), e uma linha por requisição HTTP — sem cabeçalhos nem corpo.
/// </summary>
public static class LoggingSetup
{
    public static WebApplicationBuilder AddFlowBoardLogging(this WebApplicationBuilder builder)
    {
        builder.Logging.ClearProviders();
        builder.Logging.AddJsonConsole(options =>
        {
            options.IncludeScopes = true;
            options.UseUtcTimestamp = true;
            options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
        });

        builder.Services.AddHttpLogging(options =>
        {
            options.LoggingFields = HttpLoggingFields.RequestMethod | HttpLoggingFields.RequestPath
                | HttpLoggingFields.ResponseStatusCode | HttpLoggingFields.Duration;
            options.CombineLogs = true;
        });

        return builder;
    }
}
