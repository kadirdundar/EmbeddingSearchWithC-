namespace EmbeddingSearch.Core.Embeddings;

/// <summary>
/// Metni sabit uzunlukta bir sayı dizisine (vektöre) dönüştüren bileşen.
/// </summary>
public interface IEmbeddingProvider
{
    /// <summary>
    /// Sağlayıcıyı ve ayarlarını tanımlayan kimlik, örn. "hashing:512" veya "openai:text-embedding-3-small".
    /// Kaydedilmiş bir indeks farklı bir sağlayıcıyla sorgulanırsa sonuçlar anlamsız olur;
    /// bu kimlik o hatayı yakalamak için indeks dosyasına yazılır.
    /// </summary>
    string Fingerprint { get; }

    Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<float[]>> EmbedBatchAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken = default);
}
