namespace FlowBoard.Api.Domain.Exceptions;

/// <summary>
/// Classe base das exceções de negócio. Cada exceção carrega:
/// - ErrorCode: código estável (ex.: WIP_LIMIT_EXCEEDED);
/// - Title: título curto em português;
/// - Message: detalhe da situação;
/// - Extensions: dados extras opcionais (ex.: lista de critérios pendentes da DoR).
/// O handler global converte estas exceções em respostas Problem Details.
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string errorCode, string title, string message,
        IReadOnlyDictionary<string, object?>? extensions = null)
        : base(message)
    {
        ErrorCode = errorCode;
        Title = title;
        Extensions = extensions ?? new Dictionary<string, object?>();
    }

    public string ErrorCode { get; }

    public string Title { get; }

    public IReadOnlyDictionary<string, object?> Extensions { get; }

    /// <summary>Código HTTP correspondente (404, 409 ou 422).</summary>
    public abstract int StatusCode { get; }
}

/// <summary>Recurso inexistente → HTTP 404.</summary>
public sealed class NotFoundException(string resource, int id)
    : DomainException(ErrorCodes.ResourceNotFound, "Recurso não encontrado", $"{resource} {id} não foi encontrado(a).")
{
    public override int StatusCode => 404;
}

/// <summary>Operação válida, mas em conflito com o estado atual → HTTP 409.</summary>
public sealed class ConflictException(string errorCode, string title, string message,
    IReadOnlyDictionary<string, object?>? extensions = null)
    : DomainException(errorCode, title, message, extensions)
{
    public override int StatusCode => 409;
}

/// <summary>Dados bem formados que violam uma regra de negócio → HTTP 422.</summary>
public sealed class BusinessRuleException(string errorCode, string title, string message,
    IReadOnlyDictionary<string, object?>? extensions = null)
    : DomainException(errorCode, title, message, extensions)
{
    public override int StatusCode => 422;
}
