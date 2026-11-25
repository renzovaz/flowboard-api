using FlowBoard.Api.Data;
using FlowBoard.Api.Infrastructure.ErrorHandling;
using Microsoft.EntityFrameworkCore;

namespace FlowBoard.Api.Infrastructure.Database;

/// <summary>Registro do DbContext e aplicação das migrations na inicialização.</summary>
public static partial class DatabaseSetup
{
    public static IServiceCollection AddFlowBoardDatabase(this IServiceCollection services)
    {
        // A connection string é lida de forma "preguiçosa" (quando o DbContext é criado),
        // vinda de variável de ambiente (ConnectionStrings__Default). Nunca há valor padrão.
        services.AddDbContext<FlowBoardDbContext>((sp, options) =>
        {
            var connectionString = sp.GetRequiredService<IConfiguration>().GetConnectionString("Default");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "A connection string 'Default' não foi configurada (variável ConnectionStrings__Default).");
            }

            // Sem EnableRetryOnFailure (ADR-016): incompatível com as transações explícitas do lock FOR UPDATE.
            options.UseNpgsql(connectionString);
        });

        return services;
    }

    /// <summary>Aplica as migrations pendentes quando Database:ApplyMigrationsOnStartup = true.</summary>
    public static async Task ApplyMigrationsIfEnabledAsync(this WebApplication app)
    {
        if (!app.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
        {
            return;
        }

        // O banco pode ainda estar iniciando (ex.: contêineres subindo juntos sem depends_on).
        // Aguarda até ~60 s SOMENTE para falhas de indisponibilidade; outros erros falham na hora.
        const int maxAttempts = 20;
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await using var scope = app.Services.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<FlowBoardDbContext>();
                await db.Database.MigrateAsync();
                return;
            }
            catch (Exception ex) when (attempt < maxAttempts
                && DatabaseExceptionClassifier.Classify(ex) == DatabaseFailure.Unavailable)
            {
                LogWaitingForDatabase(app.Logger, attempt, maxAttempts, ex.GetBaseException().Message);
                await Task.Delay(TimeSpan.FromSeconds(3));
            }
        }
    }

    [LoggerMessage(EventId = 2004, Level = LogLevel.Warning,
        Message = "WaitingForDatabase tentativa {Attempt}/{MaxAttempts}: {Reason}")]
    private static partial void LogWaitingForDatabase(ILogger logger, int attempt, int maxAttempts, string reason);
}
