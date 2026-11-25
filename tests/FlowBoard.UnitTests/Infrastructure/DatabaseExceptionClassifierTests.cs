using System.Net.Sockets;
using FlowBoard.Api.Infrastructure.ErrorHandling;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FlowBoard.UnitTests.Infrastructure;

/// <summary>U24 — classificação de falhas de banco (ADR-017): 503 × 409 × 500.</summary>
public class DatabaseExceptionClassifierTests
{
    [Theory]
    [InlineData("08006", DatabaseFailure.Unavailable)]        // connection_failure
    [InlineData("57P01", DatabaseFailure.Unavailable)]        // admin_shutdown
    [InlineData("57P03", DatabaseFailure.Unavailable)]        // cannot_connect_now
    [InlineData("53300", DatabaseFailure.Unavailable)]        // too_many_connections
    [InlineData("23505", DatabaseFailure.UniqueViolation)]
    [InlineData("40001", DatabaseFailure.ConcurrencyConflict)] // serialization_failure
    [InlineData("40P01", DatabaseFailure.ConcurrencyConflict)] // deadlock_detected
    [InlineData("23514", DatabaseFailure.IntegrityViolation)]  // check_violation
    [InlineData("23503", DatabaseFailure.IntegrityViolation)]  // foreign_key_violation
    [InlineData("42601", DatabaseFailure.None)]                // syntax_error → erro inesperado (500)
    public void Classify_PostgresSqlState_MapsToExpectedFailure(string sqlState, DatabaseFailure expected)
    {
        var exception = new PostgresException("erro", "ERROR", "ERROR", sqlState);

        Assert.Equal(expected, DatabaseExceptionClassifier.Classify(exception));
    }

    [Fact]
    public void Classify_WrappedByEfCore_FindsInnerPostgresException()
    {
        var wrapped = new DbUpdateException("falha ao salvar", new PostgresException("dup", "ERROR", "ERROR", "23505"));

        Assert.Equal(DatabaseFailure.UniqueViolation, DatabaseExceptionClassifier.Classify(wrapped));
    }

    [Fact]
    public void Classify_ClientSideConnectionFailure_IsUnavailable()
    {
        var refused = new NpgsqlException("Failed to connect", new SocketException((int)SocketError.ConnectionRefused));

        Assert.Equal(DatabaseFailure.Unavailable, DatabaseExceptionClassifier.Classify(refused));
        Assert.Equal(DatabaseFailure.Unavailable, DatabaseExceptionClassifier.Classify(new TimeoutException()));
    }

    [Fact]
    public void Classify_OptimisticConcurrency_IsConcurrencyConflict()
    {
        Assert.Equal(DatabaseFailure.ConcurrencyConflict,
            DatabaseExceptionClassifier.Classify(new DbUpdateConcurrencyException("xmin mudou")));
    }

    [Fact]
    public void Classify_NonDatabaseException_IsNone()
    {
        Assert.Equal(DatabaseFailure.None, DatabaseExceptionClassifier.Classify(new InvalidOperationException()));
    }
}
