using EmbeddingSearch.Core.Text;

namespace EmbeddingSearch.Core.Indexing;

/// <summary>
/// Klasik anahtar kelime araması: Okapi BM25. Elasticsearch/Lucene'in varsayılan sıralama fonksiyonu.
/// </summary>
/// <remarks>
/// skor(D, Q) = Σ_{q ∈ Q} IDF(q) · tf(q, D) · (k1 + 1) / (tf(q, D) + k1 · (1 − b + b · |D| / ortalama|D|))
///
/// - IDF: nadir kelimeler daha değerlidir ("ve" kelimesi her belgede geçer, "kuantum" geçmez).
/// - k1 : bir kelimenin tekrarının skoru ne kadar artırabileceğini sınırlar (doyum).
/// - b  : uzun belgelerin sırf uzun oldukları için avantajlı olmasını engeller.
///
/// Embedding aramasının kaçırdığı durumlarda güçlüdür: ürün kodları, özel isimler, nadir terimler.
/// </remarks>
public sealed class Bm25Index
{
    private readonly Tokenizer _tokenizer;
    private readonly double _k1;
    private readonly double _b;

    private readonly List<string> _ids = new();
    private readonly List<int> _lengths = new();
    private readonly Dictionary<string, List<(int Doc, int Tf)>> _postings = new();
    private long _totalLength;

    public Bm25Index(Tokenizer? tokenizer = null, double k1 = 1.2, double b = 0.75)
    {
        _tokenizer = tokenizer ?? Tokenizer.Turkish;
        _k1 = k1;
        _b = b;
    }

    public int Count => _ids.Count;

    public void Add(string id, string text)
    {
        int doc = _ids.Count;
        _ids.Add(id);

        var tokens = _tokenizer.Tokenize(text);
        _lengths.Add(tokens.Count);
        _totalLength += tokens.Count;

        // "Ters indeks" (inverted index): kelime → (belge, kelime sıklığı) listesi.
        foreach (var group in tokens.GroupBy(t => t))
        {
            if (!_postings.TryGetValue(group.Key, out var list))
                _postings[group.Key] = list = new List<(int, int)>();
            list.Add((doc, group.Count()));
        }
    }

    public IReadOnlyList<SearchResult> Search(string query, int k)
    {
        if (_ids.Count == 0)
            return Array.Empty<SearchResult>();

        double avgLength = (double)_totalLength / _ids.Count;
        var scores = new Dictionary<int, double>();

        foreach (var term in _tokenizer.Tokenize(query).Distinct())
        {
            if (!_postings.TryGetValue(term, out var postings))
                continue;

            double idf = Idf(postings.Count);
            foreach (var (doc, tf) in postings)
            {
                double norm = tf + _k1 * (1 - _b + _b * _lengths[doc] / avgLength);
                double score = idf * tf * (_k1 + 1) / norm;
                scores[doc] = scores.GetValueOrDefault(doc) + score;
            }
        }

        var top = new TopK(k);
        foreach (var (doc, score) in scores)
            top.Add(_ids[doc], (float)score);
        return top.ToSortedList();
    }

    /// <summary>Lucene'deki "+1"li IDF; çok yaygın kelimelerde negatif skoru engeller.</summary>
    private double Idf(int docFrequency)
        => System.Math.Log(1 + (_ids.Count - docFrequency + 0.5) / (docFrequency + 0.5));
}
