using FlowBoard.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace FlowBoard.Api.Infrastructure.Database;

/// <summary>Registro do DbContext e aplicação das migrations na inicialização.</summary>
public static class DatabaseSetup
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

        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<FlowBoardDbContext>();
        await db.Database.MigrateAsync();
    }
}
