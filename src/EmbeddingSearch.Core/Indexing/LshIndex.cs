using EmbeddingSearch.Core.Math;

namespace EmbeddingSearch.Core.Indexing;

/// <summary>
/// Yaklaşık en yakın komşu (ANN) araması: Rastgele Hiperdüzlem LSH (Locality Sensitive Hashing).
/// </summary>
/// <remarks>
/// Fikir: Uzayı rastgele düzlemlerle böl. Bir vektörün her düzlemin hangi tarafında kaldığı
/// bir bit verir; <c>bitsPerTable</c> bit yan yana bir "kova" (bucket) numarası oluşturur.
/// Aralarındaki açı küçük olan iki vektörün aynı tarafa düşme olasılığı yüksektir, yani
/// benzer vektörler büyük ihtimalle aynı kovaya düşer.
///
/// Arama sırasında yalnızca sorgunun kovasındaki adaylar kesin olarak puanlanır.
/// - Daha çok bit  → daha küçük kovalar → daha hızlı ama daha düşük recall.
/// - Daha çok tablo → kaçırılan komşuyu başka bir tabloda yakalama şansı → daha yüksek recall.
/// - Multi-probe   → komşu kovalara da (tek bit farklı) bakarak az tabloyla yüksek recall.
/// </remarks>
public sealed class LshIndex : IVectorIndex
{
    private readonly int _numTables;
    private readonly int _bitsPerTable;
    private readonly bool _multiProbe;
    private readonly int _seed;

    private readonly List<string> _ids = new();
    private readonly List<float[]> _vectors = new();
    private Dictionary<int, List<int>>[]? _tables;
    private float[][][]? _hyperplanes; // [tablo][bit][boyut]
    private int _dimensions;

    public LshIndex(int numTables = 8, int bitsPerTable = 12, bool multiProbe = true, int seed = 42)
    {
        if (bitsPerTable is <= 0 or > 30)
            throw new ArgumentOutOfRangeException(nameof(bitsPerTable), "1 ile 30 arasında olmalı.");
        if (numTables <= 0)
            throw new ArgumentOutOfRangeException(nameof(numTables));

        _numTables = numTables;
        _bitsPerTable = bitsPerTable;
        _multiProbe = multiProbe;
        _seed = seed;
    }

    public int Count => _ids.Count;

    /// <summary>Son aramada kesin olarak puanlanan aday sayısı (ne kadar iş tasarruf edildiğini görmek için).</summary>
    public int LastCandidateCount { get; private set; }

    public void Add(string id, float[] vector)
    {
        if (_hyperplanes is null)
            Initialize(vector.Length);
        else if (vector.Length != _dimensions)
            throw new ArgumentException($"Beklenen boyut {_dimensions}, gelen {vector.Length}.");

        int position = _ids.Count;
        _ids.Add(id);
        _vectors.Add(vector);

        for (int t = 0; t < _numTables; t++)
        {
            int bucket = Hash(t, vector);
            if (!_tables![t].TryGetValue(bucket, out var list))
                _tables[t][bucket] = list = new List<int>();
            list.Add(position);
        }
    }

    public IReadOnlyList<SearchResult> Search(float[] query, int k)
    {
        if (_ids.Count == 0)
        {
            LastCandidateCount = 0;
            return Array.Empty<SearchResult>();
        }

        var candidates = new HashSet<int>();
        for (int t = 0; t < _numTables; t++)
        {
            int bucket = Hash(t, query);
            Collect(t, bucket, candidates);

            if (_multiProbe)
            {
                for (int bit = 0; bit < _bitsPerTable; bit++)
                    Collect(t, bucket ^ (1 << bit), candidates);
            }
        }

        LastCandidateCount = candidates.Count;

        var top = new TopK(k);
        foreach (int i in candidates)
            top.Add(_ids[i], VectorMath.Dot(query, _vectors[i]));
        return top.ToSortedList();
    }

    private void Collect(int table, int bucket, HashSet<int> candidates)
    {
        if (_tables![table].TryGetValue(bucket, out var list))
            candidates.UnionWith(list);
    }

    private int Hash(int table, float[] vector)
    {
        int bucket = 0;
        var planes = _hyperplanes![table];
        for (int bit = 0; bit < _bitsPerTable; bit++)
        {
            if (VectorMath.Dot(planes[bit], vector) >= 0f)
                bucket |= 1 << bit;
        }
        return bucket;
    }

    private void Initialize(int dimensions)
    {
        _dimensions = dimensions;
        var random = new Random(_seed);

        _hyperplanes = new float[_numTables][][];
        _tables = new Dictionary<int, List<int>>[_numTables];
        for (int t = 0; t < _numTables; t++)
        {
            _tables[t] = new Dictionary<int, List<int>>();
            _hyperplanes[t] = new float[_bitsPerTable][];
            for (int b = 0; b < _bitsPerTable; b++)
            {
                var plane = new float[dimensions];
                for (int d = 0; d < dimensions; d++)
                    plane[d] = NextGaussian(random);
                _hyperplanes[t][b] = plane;
            }
        }
    }

    /// <summary>
    /// Standart normal dağılımdan örnek (Box-Muller dönüşümü). Hiperdüzlem normallerinin
    /// Gauss dağılımından seçilmesi, tüm yönlerin eşit olasılıkla seçilmesini sağlar.
    /// </summary>
    private static float NextGaussian(Random random)
    {
        double u1 = 1.0 - random.NextDouble();
        double u2 = random.NextDouble();
        return (float)(System.Math.Sqrt(-2.0 * System.Math.Log(u1)) * System.Math.Cos(2.0 * System.Math.PI * u2));
    }
}
