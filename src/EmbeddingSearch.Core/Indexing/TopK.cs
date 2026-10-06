namespace EmbeddingSearch.Core.Indexing;

/// <summary>
/// Bir akıştan en yüksek skorlu k öğeyi tutar.
/// </summary>
/// <remarks>
/// Tüm skorları sıralamak O(n log n) iken, k boyutlu bir min-heap ile O(n log k) olur.
/// Heap'in tepesinde şu ana kadarki en KÖTÜ k'lık sonuç durur; yeni aday ondan iyiyse yer değiştirir.
/// </remarks>
public sealed class TopK
{
    private readonly int _k;
    private readonly PriorityQueue<string, float> _heap;

    public TopK(int k)
    {
        if (k <= 0) throw new ArgumentOutOfRangeException(nameof(k));
        _k = k;
        _heap = new PriorityQueue<string, float>(k + 1);
    }

    public void Add(string id, float score)
    {
        if (_heap.Count < _k)
        {
            _heap.Enqueue(id, score);
        }
        else if (_heap.TryPeek(out _, out var worst) && score > worst)
        {
            _heap.EnqueueDequeue(id, score);
        }
    }

    /// <summary>Sonuçları en iyiden en kötüye sıralı döndürür.</summary>
    public IReadOnlyList<SearchResult> ToSortedList()
    {
        var results = _heap.UnorderedItems
            .Select(x => new SearchResult(x.Element, x.Priority))
            .ToList();
        results.Sort((a, b) => b.Score.CompareTo(a.Score));
        return results;
    }
}
