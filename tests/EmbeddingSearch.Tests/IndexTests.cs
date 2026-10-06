using EmbeddingSearch.Core.Evaluation;
using EmbeddingSearch.Core.Indexing;
using EmbeddingSearch.Core.Math;

namespace EmbeddingSearch.Tests;

public class IndexTests
{
    [Fact]
    public void TopK_keeps_highest_scores_in_descending_order()
    {
        var top = new TopK(3);
        foreach (var (id, score) in new[] { ("a", 0.1f), ("b", 0.9f), ("c", 0.5f), ("d", 0.7f), ("e", 0.2f) })
            top.Add(id, score);

        Assert.Equal(new[] { "b", "d", "c" }, top.ToSortedList().Select(r => r.Id));
    }

    [Fact]
    public void BruteForce_returns_exact_nearest_neighbours()
    {
        var index = new BruteForceIndex();
        index.Add("x", VectorMath.Normalized(new float[] { 1, 0 }));
        index.Add("y", VectorMath.Normalized(new float[] { 0, 1 }));
        index.Add("xy", VectorMath.Normalized(new float[] { 1, 1 }));

        var results = index.Search(VectorMath.Normalized(new float[] { 1, 0.1f }), k: 2);

        Assert.Equal(new[] { "x", "xy" }, results.Select(r => r.Id));
        Assert.True(results[0].Score >= results[1].Score);
    }

    [Fact]
    public void BruteForce_rejects_wrong_dimensions()
    {
        var index = new BruteForceIndex();
        index.Add("a", new float[] { 1, 0 });
        Assert.Throws<ArgumentException>(() => index.Add("b", new float[] { 1, 0, 0 }));
    }

    [Fact]
    public void Empty_indexes_return_no_results()
    {
        Assert.Empty(new BruteForceIndex().Search(new float[] { 1 }, 5));
        Assert.Empty(new LshIndex().Search(new float[] { 1 }, 5));
        Assert.Empty(new Bm25Index().Search("kahve", 5));
    }

    [Fact]
    public void Lsh_has_high_recall_against_brute_force_on_clustered_data()
    {
        var random = new Random(7);
        const int dim = 64;
        var centers = Enumerable.Range(0, 20).Select(_ => RandomUnit(random, dim)).ToList();
        var data = Enumerable.Range(0, 3000).Select(_ => Perturb(random, centers[random.Next(centers.Count)], 0.5f)).ToList();

        var exact = new BruteForceIndex();
        var lsh = new LshIndex(numTables: 8, bitsPerTable: 10);
        for (int i = 0; i < data.Count; i++)
        {
            exact.Add(i.ToString(), data[i]);
            lsh.Add(i.ToString(), data[i]);
        }

        double recall = Enumerable.Range(0, 50).Average(_ =>
        {
            var query = Perturb(random, data[random.Next(data.Count)], 0.3f);
            var truth = exact.Search(query, 10).Select(r => r.Id).ToList();
            var approx = lsh.Search(query, 10).Select(r => r.Id).ToList();
            return Metrics.RecallAtK(truth, approx, 10);
        });

        Assert.True(recall > 0.8, $"Recall@10 = {recall:F3}");
        Assert.True(lsh.LastCandidateCount < data.Count, "LSH tüm veriyi taramamalı.");
    }

    [Fact]
    public void Bm25_prefers_documents_with_rare_query_terms()
    {
        var index = new Bm25Index();
        index.Add("ortak", "bu bir belge ve bu bir metin");
        index.Add("nadir", "bu belge kuantum fiziği hakkında");
        index.Add("diger", "bu da başka bir belge");

        var results = index.Search("kuantum belge", 3);

        Assert.Equal("nadir", results[0].Id);
    }

    [Fact]
    public void Bm25_ignores_documents_without_query_terms()
    {
        var index = new Bm25Index();
        index.Add("a", "kahve");
        index.Add("b", "çay");

        Assert.Equal(new[] { "a" }, index.Search("kahve", 5).Select(r => r.Id));
    }

    private static float[] RandomUnit(Random random, int dim)
        => VectorMath.Normalized(Enumerable.Range(0, dim).Select(_ => Gaussian(random)).ToArray());

    private static float[] Perturb(Random random, float[] v, float noise)
        => VectorMath.Normalized(v.Select(x => x + noise / MathF.Sqrt(v.Length) * Gaussian(random)).ToArray());

    private static float Gaussian(Random random)
        => (float)(Math.Sqrt(-2.0 * Math.Log(1.0 - random.NextDouble())) * Math.Cos(2.0 * Math.PI * random.NextDouble()));
}
