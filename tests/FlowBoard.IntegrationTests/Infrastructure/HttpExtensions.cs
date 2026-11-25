using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FlowBoard.IntegrationTests.Infrastructure;

/// <summary>Atalhos para chamar a API e ler JSON/Problem Details nos testes.</summary>
internal static class HttpExtensions
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public static Task<HttpResponseMessage> PostJsonAsync(this HttpClient client, string url, object body) =>
        client.PostAsJsonAsync(url, body, Json, TestContext.Current.CancellationToken);

    public static Task<HttpResponseMessage> PutJsonAsync(this HttpClient client, string url, object body) =>
        client.PutAsJsonAsync(url, body, Json, TestContext.Current.CancellationToken);

    public static Task<HttpResponseMessage> PatchJsonAsync(this HttpClient client, string url, object body) =>
        client.PatchAsJsonAsync(url, body, Json, TestContext.Current.CancellationToken);

    public static async Task<T> ReadAsync<T>(this HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<T>(Json, TestContext.Current.CancellationToken))!;

    public static async Task<JsonElement> ReadJsonAsync(this HttpResponseMessage response) =>
        await response.ReadAsync<JsonElement>();

    /// <summary>Lê o campo "errorCode" do Problem Details.</summary>
    public static async Task<string?> ReadErrorCodeAsync(this HttpResponseMessage response) =>
        (await response.ReadJsonAsync()).TryGetProperty("errorCode", out var code) ? code.GetString() : null;
}
