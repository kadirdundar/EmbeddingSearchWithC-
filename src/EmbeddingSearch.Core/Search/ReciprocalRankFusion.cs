using EmbeddingSearch.Core.Indexing;

namespace EmbeddingSearch.Core.Search;

/// <summary>
/// Birden çok sıralı sonuç listesini tek listede birleştirir (RRF).
/// </summary>
/// <remarks>
/// BM25 skoru 0..∞, kosinüs skoru -1..1 aralığındadır; doğrudan toplanamazlar.
/// RRF skorlara değil SIRALARA bakar: skor(d) = Σ 1 / (k + sıra(d)).
/// Basit, ayar gerektirmeyen ve pratikte çok iyi çalışan bir yöntemdir. k = 60 yaygın varsayılandır.
/// </remarks>
public static class ReciprocalRankFusion
{
    public static IReadOnlyList<SearchResult> Fuse(IEnumerable<IReadOnlyList<SearchResult>> rankings, int topK, int k = 60)
    {
        var fused = new Dictionary<string, float>();
        foreach (var ranking in rankings)
        {
            for (int rank = 0; rank < ranking.Count; rank++)
            {
                var id = ranking[rank].Id;
                fused[id] = fused.GetValueOrDefault(id) + 1f / (k + rank + 1);
            }
        }

        return fused
            .OrderByDescending(x => x.Value)
            .ThenBy(x => x.Key, StringComparer.Ordinal)
            .Take(topK)
            .Select(x => new SearchResult(x.Key, x.Value))
            .ToList();
    }
}
