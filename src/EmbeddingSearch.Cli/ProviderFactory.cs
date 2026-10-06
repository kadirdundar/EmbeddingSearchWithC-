using EmbeddingSearch.Core.Embeddings;

namespace EmbeddingSearch.Cli;

/// <summary>
/// --provider seçeneğine göre embedding sağlayıcısı oluşturur.
/// </summary>
/// <remarks>
/// openai sağlayıcısı ortam değişkenleriyle yapılandırılır (API anahtarını koda yazmayın!):
///   EMBEDDING_BASE_URL  (varsayılan: https://api.openai.com/v1/)
///   EMBEDDING_MODEL     (varsayılan: text-embedding-3-small)
///   EMBEDDING_API_KEY   (Ollama için gerekmez)
/// </remarks>
public static class ProviderFactory
{
    public static IEmbeddingProvider Create(CliArgs args)
    {
        string name = args.Get("provider", "hashing");
        switch (name)
        {
            case "hashing":
                return new HashingEmbeddingProvider(dimensions: args.GetInt("dim", 512));

            case "openai":
                var baseUrl = Environment.GetEnvironmentVariable("EMBEDDING_BASE_URL") ?? "https://api.openai.com/v1/";
                var model = Environment.GetEnvironmentVariable("EMBEDDING_MODEL") ?? "text-embedding-3-small";
                var apiKey = Environment.GetEnvironmentVariable("EMBEDDING_API_KEY");
                return new OpenAiCompatibleEmbeddingProvider(new HttpClient(), new Uri(baseUrl), model, apiKey);

            default:
                throw new ArgumentException($"Bilinmeyen sağlayıcı '{name}'. Seçenekler: hashing, openai");
        }
    }
}
