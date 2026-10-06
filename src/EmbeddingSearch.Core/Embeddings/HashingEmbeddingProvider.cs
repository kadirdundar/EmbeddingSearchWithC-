using EmbeddingSearch.Core.Math;
using EmbeddingSearch.Core.Text;

namespace EmbeddingSearch.Core.Embeddings;

/// <summary>
/// İnternet ve model gerektirmeyen, öğrenme amaçlı yerel embedding üreticisi ("hashing trick").
/// </summary>
/// <remarks>
/// Her kelime ve her kelimenin karakter üçlüleri (trigram) bir hash fonksiyonuyla vektörün
/// bir boyutuna eşlenir ve o boyuttaki değer artırılır. Karakter üçlüleri sayesinde
/// "kahve" ile "kahveler" gibi ekli kelimeler de birbirine yakın düşer.
///
/// ÖNEMLİ: Bu yöntem SÖZCÜKSEL (lexical) benzerlik yakalar, ANLAMSAL (semantic) değil.
/// "araba" ile "otomobil" bu sağlayıcıya göre alakasızdır. Gerçek anlamsal arama için
/// <see cref="OpenAiCompatibleEmbeddingProvider"/> ile bir dil modeli kullanın.
/// </remarks>
public sealed class HashingEmbeddingProvider : IEmbeddingProvider
{
    private readonly int _dimensions;
    private readonly int _ngramSize;
    private readonly Tokenizer _tokenizer;

    public HashingEmbeddingProvider(int dimensions = 512, int ngramSize = 3, Tokenizer? tokenizer = null)
    {
        if (dimensions <= 0) throw new ArgumentOutOfRangeException(nameof(dimensions));
        if (ngramSize <= 0) throw new ArgumentOutOfRangeException(nameof(ngramSize));
        _dimensions = dimensions;
        _ngramSize = ngramSize;
        _tokenizer = tokenizer ?? Tokenizer.Turkish;
    }

    public string Fingerprint => $"hashing:{_dimensions}:{_ngramSize}";

    public float[] Embed(string text)
    {
        var vector = new float[_dimensions];

        foreach (var token in _tokenizer.Tokenize(text))
        {
            // Kelimenin kendisi, karakter n-gramlarından daha güçlü bir sinyaldir.
            AddFeature(vector, "w:" + token, weight: 2f);

            // "<" ve ">" kelime sınırlarını işaretler: "<ka", "kah", "ahv", "hve", "ve>".
            var padded = "<" + token + ">";
            for (int i = 0; i + _ngramSize <= padded.Length; i++)
                AddFeature(vector, "c:" + padded.Substring(i, _ngramSize), weight: 1f);
        }

        VectorMath.NormalizeInPlace(vector);
        return vector;
    }

    public Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken = default)
        => Task.FromResult(Embed(text));

    public Task<IReadOnlyList<float[]>> EmbedBatchAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<float[]>>(texts.Select(Embed).ToArray());

    private void AddFeature(float[] vector, string feature, float weight)
    {
        uint hash = Fnv1a(feature);
        int index = (int)(hash % (uint)_dimensions);
        // İkinci bir bit ile işaret seçmek, çakışan özelliklerin birbirini sistematik olarak
        // şişirmesini engeller (çakışmalar ortalamada birbirini götürür).
        float sign = (hash & 0x8000_0000) == 0 ? 1f : -1f;
        vector[index] += sign * weight;
    }

    /// <summary>
    /// FNV-1a hash. string.GetHashCode() KULLANILMAZ, çünkü .NET'te her işlem (process)
    /// başlangıcında rastgele tohumlanır: kaydedilen indeks bir sonraki çalıştırmada bozulurdu.
    /// </summary>
    private static uint Fnv1a(string s)
    {
        uint hash = 2166136261;
        foreach (char c in s)
        {
            hash ^= c;
            hash *= 16777619;
        }
        return hash;
    }
}
