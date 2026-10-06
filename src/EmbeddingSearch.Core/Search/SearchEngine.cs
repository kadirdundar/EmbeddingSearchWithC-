using EmbeddingSearch.Core.Embeddings;
using EmbeddingSearch.Core.Indexing;
using EmbeddingSearch.Core.Storage;
using EmbeddingSearch.Core.Text;

namespace EmbeddingSearch.Core.Search;

/// <summary>
/// Belgeleri parçalar, embedding'lerini çıkarır, hem vektör hem BM25 indeksine ekler
/// ve üç modda (vektör / anahtar kelime / hibrit) arama yapar.
/// </summary>
public sealed class SearchEngine
{
    private readonly IEmbeddingProvider _embeddings;
    private readonly IVectorIndex _vectorIndex;
    private readonly Bm25Index _keywordIndex = new();
    private readonly Dictionary<string, Chunk> _chunks = new();
    private readonly Dictionary<string, float[]> _vectors = new();
    private readonly int _maxWords;
    private readonly int _overlapWords;

    public SearchEngine(IEmbeddingProvider embeddings, IVectorIndex? vectorIndex = null, int maxWords = 120, int overlapWords = 20)
    {
        _embeddings = embeddings;
        _vectorIndex = vectorIndex ?? new BruteForceIndex();
        _maxWords = maxWords;
        _overlapWords = overlapWords;
    }

    public IEmbeddingProvider Embeddings => _embeddings;

    public IReadOnlyCollection<Chunk> Chunks => _chunks.Values;

    /// <summary>Bir belgeyi parçalara bölüp indeksler. Eklenen parça sayısını döndürür.</summary>
    public async Task<int> AddDocumentAsync(string source, string text, CancellationToken cancellationToken = default)
    {
        var pieces = TextChunker.Chunk(text, _maxWords, _overlapWords);
        if (pieces.Count == 0)
            return 0;

        // Toplu (batch) istek: uzak API'lerde her parça için ayrı istek atmaktan çok daha hızlıdır.
        var vectors = await _embeddings.EmbedBatchAsync(pieces, cancellationToken);

        for (int i = 0; i < pieces.Count; i++)
            AddChunk(new Chunk($"{source}#{i}", source, i, pieces[i]), vectors[i]);

        return pieces.Count;
    }

    public async Task<IReadOnlyList<SearchHit>> SearchAsync(
        string query, int k = 5, SearchMode mode = SearchMode.Hybrid, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<SearchResult> results;
        switch (mode)
        {
            case SearchMode.Vector:
                results = _vectorIndex.Search(await _embeddings.EmbedAsync(query, cancellationToken), k);
                break;
            case SearchMode.Keyword:
                results = _keywordIndex.Search(query, k);
                break;
            case SearchMode.Hybrid:
                // Birleştirmeden önce her iki listeden de fazladan aday alınır; böylece bir listede
                // 7. sırada olup diğerinde 1. olan bir sonuç kaçırılmaz.
                int candidates = System.Math.Max(k * 4, 20);
                var vectorResults = _vectorIndex.Search(await _embeddings.EmbedAsync(query, cancellationToken), candidates);
                var keywordResults = _keywordIndex.Search(query, candidates);
                results = ReciprocalRankFusion.Fuse(new[] { vectorResults, keywordResults }, k);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(mode));
        }

        return results.Select(r => new SearchHit(_chunks[r.Id], r.Score)).ToList();
    }

    /// <summary>Tüm parçaları ve vektörlerini diske yazar; tekrar embedding hesaplamaya gerek kalmaz.</summary>
    public void Save(string path)
    {
        var records = _chunks.Values.Select(c => new IndexRecord(c, _vectors[c.Id]));
        IndexFile.Write(path, _embeddings.Fingerprint, records);
    }

    /// <summary>
    /// Kaydedilmiş bir indeksi yükler. Dosyayı oluşturan sağlayıcı ile verilen sağlayıcı farklıysa
    /// hata verir: farklı modellerin vektörleri farklı "uzaylarda"dır ve karşılaştırılamaz.
    /// </summary>
    public static SearchEngine Load(string path, IEmbeddingProvider embeddings, IVectorIndex? vectorIndex = null)
    {
        var (fingerprint, records) = IndexFile.Read(path);
        if (fingerprint != embeddings.Fingerprint)
        {
            throw new InvalidOperationException(
                $"İndeks '{fingerprint}' ile oluşturulmuş, ancak sorgu sağlayıcısı '{embeddings.Fingerprint}'. " +
                "İndeksi aynı sağlayıcıyla yeniden oluşturun.");
        }

        var engine = new SearchEngine(embeddings, vectorIndex);
        foreach (var record in records)
            engine.AddChunk(record.Chunk, record.Vector);
        return engine;
    }

    private void AddChunk(Chunk chunk, float[] vector)
    {
        if (!_chunks.TryAdd(chunk.Id, chunk))
            throw new InvalidOperationException($"'{chunk.Id}' zaten indekslenmiş.");

        _vectors[chunk.Id] = vector;
        _vectorIndex.Add(chunk.Id, vector);
        _keywordIndex.Add(chunk.Id, chunk.Text);
    }
}
