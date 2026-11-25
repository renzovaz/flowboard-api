using System.Net.Sockets;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FlowBoard.Api.Infrastructure.ErrorHandling;

/// <summary>Categoria de uma falha de banco, usada para escolher o código HTTP (ADR-017).</summary>
public enum DatabaseFailure
{
    None,
    Unavailable,        // 503 DATABASE_UNAVAILABLE
    UniqueViolation,    // 409 DUPLICATE_NAME
    ConcurrencyConflict, // 409 CONCURRENCY_CONFLICT
    IntegrityViolation, // 500 — o domínio deveria ter barrado antes (erro de programação)
}

/// <summary>
/// Identifica falhas de banco percorrendo a cadeia de InnerException
/// (o EF Core "embrulha" os erros do Npgsql, ex.: DbUpdateException → PostgresException).
/// Diferencia indisponibilidade do banco de erros de integridade e de concorrência.
/// </summary>
public static class DatabaseExceptionClassifier
{
    private static readonly HashSet<string> UnavailableStates =
    [
        "57P01", // admin_shutdown
        "57P02", // crash_shutdown
        "57P03", // cannot_connect_now (banco iniciando)
        "53300", // too_many_connections
    ];

    public static DatabaseFailure Classify(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            switch (current)
            {
                case DbUpdateConcurrencyException:
                    return DatabaseFailure.ConcurrencyConflict;
                case PostgresException pg:
                    return ClassifySqlState(pg.SqlState);
                case NpgsqlException:          // erro do cliente: conexão recusada, socket, timeout
                case TimeoutException:
                case SocketException:
                    return DatabaseFailure.Unavailable;
            }
        }

        return DatabaseFailure.None;
    }

    private static DatabaseFailure ClassifySqlState(string sqlState) => sqlState switch
    {
        PostgresErrorCodes.UniqueViolation => DatabaseFailure.UniqueViolation,
        PostgresErrorCodes.SerializationFailure or PostgresErrorCodes.DeadlockDetected => DatabaseFailure.ConcurrencyConflict,
        PostgresErrorCodes.CheckViolation or PostgresErrorCodes.ForeignKeyViolation or PostgresErrorCodes.NotNullViolation
            => DatabaseFailure.IntegrityViolation,
        _ when sqlState.StartsWith("08", StringComparison.Ordinal) || UnavailableStates.Contains(sqlState)
            => DatabaseFailure.Unavailable,
        _ => DatabaseFailure.None,
    };
}
