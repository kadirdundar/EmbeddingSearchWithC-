using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using EmbeddingSearch.Core.Math;

namespace EmbeddingSearch.Core.Embeddings;

/// <summary>
/// OpenAI'nin <c>/v1/embeddings</c> uç noktasıyla uyumlu herhangi bir servisi kullanır:
/// OpenAI, Azure OpenAI (uyumlu uç nokta), Ollama (<c>http://localhost:11434/v1</c>), LM Studio vb.
/// </summary>
/// <example>
/// Ollama ile yerel ve ücretsiz:
/// <code>
/// ollama pull nomic-embed-text
/// var provider = new OpenAiCompatibleEmbeddingProvider(new HttpClient(),
///     new Uri("http://localhost:11434/v1/"), model: "nomic-embed-text");
/// </code>
/// </example>
public sealed class OpenAiCompatibleEmbeddingProvider : IEmbeddingProvider
{
    private readonly HttpClient _http;
    private readonly Uri _baseUrl;
    private readonly string _model;
    private readonly string? _apiKey;

    public OpenAiCompatibleEmbeddingProvider(HttpClient http, Uri baseUrl, string model, string? apiKey = null)
    {
        _http = http;
        // Göreli "embeddings" yolunun doğru birleşmesi için taban adres "/" ile bitmelidir.
        _baseUrl = baseUrl.AbsoluteUri.EndsWith('/') ? baseUrl : new Uri(baseUrl.AbsoluteUri + "/");
        _model = model;
        _apiKey = apiKey;
    }

    public string Fingerprint => $"openai:{_baseUrl.Host}:{_model}";

    public async Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken = default)
        => (await EmbedBatchAsync(new[] { text }, cancellationToken))[0];

    public async Task<IReadOnlyList<float[]>> EmbedBatchAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(_baseUrl, "embeddings"))
        {
            Content = JsonContent.Create(new EmbeddingRequest(_model, texts)),
        };
        if (!string.IsNullOrEmpty(_apiKey))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        using var response = await _http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"Embedding isteği başarısız ({(int)response.StatusCode}): {body}");
        }

        var payload = await response.Content.ReadFromJsonAsync<EmbeddingResponse>(cancellationToken)
            ?? throw new InvalidOperationException("Boş embedding yanıtı.");

        // Yanıttaki sıra garanti değildir; "index" alanına göre sıralanır.
        return payload.Data
            .OrderBy(d => d.Index)
            .Select(d => VectorMath.Normalized(d.Embedding))
            .ToArray();
    }

    private sealed record EmbeddingRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("input")] IReadOnlyList<string> Input);

    private sealed record EmbeddingResponse(
        [property: JsonPropertyName("data")] List<EmbeddingData> Data);

    private sealed record EmbeddingData(
        [property: JsonPropertyName("index")] int Index,
        [property: JsonPropertyName("embedding")] float[] Embedding);
}
