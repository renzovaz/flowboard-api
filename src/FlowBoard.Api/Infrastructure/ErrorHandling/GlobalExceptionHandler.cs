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
            _ => Unexpected(exception),
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
        var problem = new ProblemDetails
        {
            Status = exception.StatusCode,
            Title = exception.Title,
            Detail = exception.Message,
            Type = ErrorDocs.TypeFor(exception.ErrorCode),
        };
        problem.Extensions["errorCode"] = exception.ErrorCode;
        foreach (var (key, value) in exception.Extensions)
        {
            problem.Extensions[key] = value;
        }

        return problem;
    }

    private ProblemDetails Unexpected(Exception exception)
    {
        // Detalhes completos SOMENTE no log; a resposta nunca expõe stack trace ou mensagem interna.
        LogUnhandled(logger, exception);
        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Erro interno",
            Detail = "Ocorreu um erro inesperado. Informe o traceId ao suporte.",
            Type = ErrorDocs.TypeFor(ErrorCodes.InternalError),
        };
        problem.Extensions["errorCode"] = ErrorCodes.InternalError;
        return problem;
    }

    [LoggerMessage(EventId = 2003, Level = LogLevel.Error, Message = "UnhandledException")]
    private static partial void LogUnhandled(ILogger logger, Exception exception);
}
