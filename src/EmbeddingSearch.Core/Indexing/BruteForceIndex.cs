using EmbeddingSearch.Core.Math;

namespace EmbeddingSearch.Core.Indexing;

/// <summary>
/// Kesin (exact) arama: sorguyu indeksteki HER vektörle karşılaştırır.
/// </summary>
/// <remarks>
/// Karmaşıklık O(n · d). Birkaç yüz bin vektöre kadar şaşırtıcı derecede hızlıdır ve
/// her zaman doğru cevabı verir. Yaklaşık indekslerin (LSH, HNSW) doğruluğu bu indekse
/// göre ölçülür ("recall").
///
/// Vektörler tek bir düz (flat) dizide ardışık tutulur: bellekte bitişik veri, işlemci
/// önbelleğini (cache) iyi kullanır ve float[][]'e göre belirgin şekilde hızlıdır.
/// </remarks>
public sealed class BruteForceIndex : IVectorIndex
{
    private readonly List<string> _ids = new();
    private float[] _data = Array.Empty<float>();
    private int _dimensions;

    public int Count => _ids.Count;

    public void Add(string id, float[] vector)
    {
        if (_ids.Count == 0)
            _dimensions = vector.Length;
        else if (vector.Length != _dimensions)
            throw new ArgumentException($"Beklenen boyut {_dimensions}, gelen {vector.Length}.");

        int needed = (_ids.Count + 1) * _dimensions;
        if (_data.Length < needed)
            Array.Resize(ref _data, System.Math.Max(needed, _data.Length * 2));

        vector.CopyTo(_data, _ids.Count * _dimensions);
        _ids.Add(id);
    }

    public IReadOnlyList<SearchResult> Search(float[] query, int k)
    {
        if (_ids.Count == 0)
            return Array.Empty<SearchResult>();
        if (query.Length != _dimensions)
            throw new ArgumentException($"Sorgu boyutu {query.Length}, indeks boyutu {_dimensions}.");

        var top = new TopK(k);
        var data = _data.AsSpan();
        for (int i = 0; i < _ids.Count; i++)
        {
            var vector = data.Slice(i * _dimensions, _dimensions);
            top.Add(_ids[i], VectorMath.Dot(query, vector));
        }
        return top.ToSortedList();
    }
}
