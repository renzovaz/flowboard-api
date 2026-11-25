using FlowBoard.Api.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace FlowBoard.Api.Infrastructure.ErrorHandling;

/// <summary>
/// Ponto ÚNICO de conversão de exceções em respostas HTTP no formato Problem Details (RFC 9457).
/// Controllers e Services apenas lançam exceções; nenhum deles monta respostas de erro.
/// </summary>
public sealed partial class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var problem = exception switch
        {
            DomainException domain => FromDomain(domain),
            _ => FromInfrastructure(httpContext, exception),
        };

        httpContext.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception,
        });
    }

    private static ProblemDetails FromDomain(DomainException exception)
    {
        var problem = Create(exception.StatusCode, exception.ErrorCode, exception.Title, exception.Message);
        foreach (var (key, value) in exception.Extensions)
        {
            problem.Extensions[key] = value;
        }

        return problem;
    }

    /// <summary>Falhas que não são regras de negócio: banco de dados (ADR-017) ou erro inesperado.</summary>
    private ProblemDetails FromInfrastructure(HttpContext httpContext, Exception exception)
    {
        switch (DatabaseExceptionClassifier.Classify(exception))
        {
            case DatabaseFailure.Unavailable:
                LogDatabaseUnavailable(logger, exception.GetType().Name, exception.Message);
                httpContext.Response.Headers.RetryAfter = "5";
                return Create(StatusCodes.Status503ServiceUnavailable, ErrorCodes.DatabaseUnavailable,
                    "Banco de dados indisponível",
                    "O banco de dados não está respondendo. Tente novamente em alguns segundos.");

            case DatabaseFailure.UniqueViolation:
                return Create(StatusCodes.Status409Conflict, ErrorCodes.DuplicateName,
                    "Nome já utilizado", "Já existe um registro com este nome.");

            case DatabaseFailure.ConcurrencyConflict:
                return Create(StatusCodes.Status409Conflict, ErrorCodes.ConcurrencyConflict,
                    "Conflito de concorrência", "O registro foi alterado por outra requisição. Tente novamente.");

            case DatabaseFailure.IntegrityViolation:
                // O domínio deveria ter barrado antes: é um erro de programação, não do cliente.
                LogIntegrityViolation(logger, exception);
                return InternalError();

            default:
                // Detalhes completos SOMENTE no log; a resposta nunca expõe stack trace ou mensagem interna.
                LogUnhandled(logger, exception);
                return InternalError();
        }
    }

    private static ProblemDetails InternalError() =>
        Create(StatusCodes.Status500InternalServerError, ErrorCodes.InternalError,
            "Erro interno", "Ocorreu um erro inesperado. Informe o traceId ao suporte.");

    private static ProblemDetails Create(int status, string errorCode, string title, string detail)
    {
        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Type = ErrorDocs.TypeFor(errorCode),
        };
        problem.Extensions["errorCode"] = errorCode;
        return problem;
    }

    [LoggerMessage(EventId = 2001, Level = LogLevel.Error, Message = "DatabaseUnavailable {ExceptionType}: {Reason}")]
    private static partial void LogDatabaseUnavailable(ILogger logger, string exceptionType, string reason);

    [LoggerMessage(EventId = 2002, Level = LogLevel.Error, Message = "IntegrityViolationNotCaughtByDomain")]
    private static partial void LogIntegrityViolation(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 2003, Level = LogLevel.Error, Message = "UnhandledException")]
    private static partial void LogUnhandled(ILogger logger, Exception exception);
}
