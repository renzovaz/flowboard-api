using System.Diagnostics;
using FlowBoard.Api.Domain.Exceptions;

namespace FlowBoard.Api.Infrastructure.ErrorHandling;

public static class ProblemDetailsSetup
{
    /// <summary>
    /// Padroniza TODAS as respostas de erro: adiciona traceId (correlação com os logs)
    /// e errorCode VALIDATION_ERROR nas respostas 400 geradas pela validação automática.
    /// </summary>
    public static IServiceCollection AddFlowBoardProblemDetails(this IServiceCollection services)
    {
        services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
        {
            var problem = context.ProblemDetails;
            problem.Instance ??= context.HttpContext.Request.Path;
            problem.Extensions["traceId"] = Activity.Current?.TraceId.ToString() ?? context.HttpContext.TraceIdentifier;

            if (problem.Status == StatusCodes.Status400BadRequest && !problem.Extensions.ContainsKey("errorCode"))
            {
                problem.Extensions["errorCode"] = ErrorCodes.ValidationError;
                problem.Type = ErrorDocs.TypeFor(ErrorCodes.ValidationError);
            }
        });
        services.AddExceptionHandler<GlobalExceptionHandler>();
        return services;
    }
}
