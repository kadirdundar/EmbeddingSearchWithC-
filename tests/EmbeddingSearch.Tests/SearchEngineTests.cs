using EmbeddingSearch.Core.Embeddings;
using EmbeddingSearch.Core.Evaluation;
using EmbeddingSearch.Core.Indexing;
using EmbeddingSearch.Core.Search;

namespace EmbeddingSearch.Tests;

public class SearchEngineTests
{
    private static async Task<SearchEngine> CreateEngineAsync(IEmbeddingProvider? provider = null)
    {
        var engine = new SearchEngine(provider ?? new HashingEmbeddingProvider(256));
        await engine.AddDocumentAsync("kahve.txt", "Türk kahvesi cezvede pişirilir ve köpüğü önemlidir.");
        await engine.AddDocumentAsync("uzay.txt", "Mars kızıl gezegen olarak bilinir, Jüpiter en büyük gezegendir.");
        await engine.AddDocumentAsync("futbol.txt", "Ofsayt kuralı futbolun en tartışmalı kuralıdır.");
        return engine;
    }

    [Theory]
    [InlineData(SearchMode.Vector)]
    [InlineData(SearchMode.Keyword)]
    [InlineData(SearchMode.Hybrid)]
    public async Task Every_mode_finds_the_obvious_document(SearchMode mode)
    {
        var engine = await CreateEngineAsync();

        var hits = await engine.SearchAsync("kızıl gezegen Mars", k: 2, mode);

        Assert.Equal("uzay.txt", hits[0].Chunk.Source);
    }

    [Fact]
    public async Task Save_and_load_round_trip_preserves_results()
    {
        var engine = await CreateEngineAsync();
        var path = Path.GetTempFileName();
        try
        {
            engine.Save(path);
            var loaded = SearchEngine.Load(path, new HashingEmbeddingProvider(256));

            var before = await engine.SearchAsync("kahve köpüğü", 3, SearchMode.Hybrid);
            var after = await loaded.SearchAsync("kahve köpüğü", 3, SearchMode.Hybrid);

            Assert.Equal(engine.Chunks.Count, loaded.Chunks.Count);
            Assert.Equal(before.Select(h => (h.Chunk.Id, h.Score)), after.Select(h => (h.Chunk.Id, h.Score)));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task Loading_with_a_different_provider_fails()
    {
        var engine = await CreateEngineAsync();
        var path = Path.GetTempFileName();
        try
        {
            engine.Save(path);
            Assert.Throws<InvalidOperationException>(() => SearchEngine.Load(path, new HashingEmbeddingProvider(128)));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task Engine_works_with_lsh_index()
    {
        var engine = new SearchEngine(new HashingEmbeddingProvider(256), new LshIndex(numTables: 4, bitsPerTable: 4));
        await engine.AddDocumentAsync("kahve.txt", "Türk kahvesi cezvede pişirilir.");

        var hits = await engine.SearchAsync("Türk kahvesi", 1, SearchMode.Vector);

        Assert.Equal("kahve.txt", hits[0].Chunk.Source);
    }

    [Fact]
    public async Task Long_documents_are_split_into_chunks()
    {
        var engine = new SearchEngine(new HashingEmbeddingProvider(64), maxWords: 10, overlapWords: 2);
        int count = await engine.AddDocumentAsync("uzun.txt", string.Join(' ', Enumerable.Repeat("kelime", 50)));

        Assert.True(count > 1);
        Assert.Equal(count, engine.Chunks.Count);
    }

    [Fact]
    public void Rrf_rewards_items_ranked_high_in_multiple_lists()
    {
        var a = new[] { new SearchResult("x", 9), new SearchResult("y", 8) };
        var b = new[] { new SearchResult("y", 0.9f), new SearchResult("z", 0.8f) };

        var fused = ReciprocalRankFusion.Fuse(new[] { a, b }, topK: 3);

        Assert.Equal("y", fused[0].Id);
    }

    [Fact]
    public void Metrics_compute_expected_values()
    {
        var relevant = new[] { "a", "b" };
        var retrieved = new[] { "c", "a", "d", "b" };

        Assert.Equal(0.5, Metrics.ReciprocalRank(relevant, retrieved));
        Assert.Equal(0.5, Metrics.RecallAtK(relevant, retrieved, 2));
        Assert.Equal(1.0, Metrics.RecallAtK(relevant, retrieved, 4));
        Assert.Equal(0.0, Metrics.ReciprocalRank(relevant, new[] { "x" }));
    }
}
