namespace EmbeddingSearch.Core.Evaluation;

/// <summary>
/// Arama kalitesini sayıyla ölçmek için metrikler. "Hissen daha iyi" yerine "MRR 0.62'den 0.71'e çıktı".
/// </summary>
public static class Metrics
{
    /// <summary>
    /// Recall@k: Doğru (ilgili) sonuçların ne kadarı ilk k sonuç içinde bulundu?
    /// Yaklaşık indeksleri (LSH) kesin indekse (brute force) karşı ölçmek için de kullanılır.
    /// </summary>
    public static double RecallAtK(IReadOnlyCollection<string> relevant, IReadOnlyList<string> retrieved, int k)
    {
        if (relevant.Count == 0) return 1.0;
        var relevantSet = relevant as ISet<string> ?? relevant.ToHashSet();
        int hits = retrieved.Take(k).Count(relevantSet.Contains);
        return (double)hits / System.Math.Min(relevant.Count, k);
    }

    /// <summary>
    /// Reciprocal Rank: ilk doğru sonucun sırası r ise 1/r. Hiç yoksa 0.
    /// Birçok sorgu üzerinden ortalaması MRR'dir (Mean Reciprocal Rank).
    /// </summary>
    public static double ReciprocalRank(IReadOnlyCollection<string> relevant, IReadOnlyList<string> retrieved)
    {
        for (int i = 0; i < retrieved.Count; i++)
        {
            if (relevant.Contains(retrieved[i]))
                return 1.0 / (i + 1);
        }
        return 0.0;
    }

    public static double MeanReciprocalRank(IEnumerable<(IReadOnlyCollection<string> Relevant, IReadOnlyList<string> Retrieved)> queries)
    {
        var list = queries.ToList();
        return list.Count == 0 ? 0.0 : list.Average(q => ReciprocalRank(q.Relevant, q.Retrieved));
    }
}
