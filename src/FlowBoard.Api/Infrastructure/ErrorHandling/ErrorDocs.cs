namespace FlowBoard.Api.Infrastructure.ErrorHandling;

/// <summary>Link de documentação do código de erro (campo "type" do Problem Details).</summary>
public static class ErrorDocs
{
    private const string BaseUrl = "https://github.com/renzovaz/flowboard-api/blob/main/docs/errors.md#";

    public static string TypeFor(string errorCode) => BaseUrl + errorCode.ToLowerInvariant();
}
