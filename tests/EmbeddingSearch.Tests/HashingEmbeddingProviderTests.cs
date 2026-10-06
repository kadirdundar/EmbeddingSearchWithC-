using EmbeddingSearch.Core.Embeddings;
using EmbeddingSearch.Core.Math;

namespace EmbeddingSearch.Tests;

public class HashingEmbeddingProviderTests
{
    private readonly HashingEmbeddingProvider _provider = new(dimensions: 256);

    [Fact]
    public void Embedding_has_requested_dimensions_and_unit_length()
    {
        var v = _provider.Embed("Türk kahvesi");
        Assert.Equal(256, v.Length);
        Assert.Equal(1f, VectorMath.Norm(v), precision: 4);
    }

    [Fact]
    public void Embedding_is_deterministic_across_instances()
    {
        // string.GetHashCode kullanılsaydı bu test farklı process'lerde başarısız olurdu.
        var other = new HashingEmbeddingProvider(dimensions: 256);
        Assert.Equal(_provider.Embed("aynı metin"), other.Embed("aynı metin"));
    }

    [Fact]
    public void Inflected_words_are_closer_than_unrelated_words()
    {
        var kahve = _provider.Embed("kahve");
        var kahveler = _provider.Embed("kahveler");
        var gezegen = _provider.Embed("gezegen");

        Assert.True(VectorMath.Dot(kahve, kahveler) > VectorMath.Dot(kahve, gezegen));
    }

    [Fact]
    public async Task Batch_matches_single_embeddings()
    {
        var batch = await _provider.EmbedBatchAsync(new[] { "bir", "iki" });
        Assert.Equal(_provider.Embed("bir"), batch[0]);
        Assert.Equal(_provider.Embed("iki"), batch[1]);
    }
}
