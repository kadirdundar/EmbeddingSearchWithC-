using System.Diagnostics;
using EmbeddingSearch.Core.Evaluation;
using EmbeddingSearch.Core.Indexing;
using EmbeddingSearch.Core.Math;

namespace EmbeddingSearch.Cli.Commands;

/// <summary>
/// Sentetik veriyle hız/doğruluk ölçümü: SIMD vs. skaler iç çarpım ve brute force vs. LSH.
/// </summary>
public static class BenchCommand
{
    public static Task<int> RunAsync(CliArgs args)
    {
        int n = args.GetInt("n", 20_000);
        int dim = args.GetInt("dim", 256);
        int queryCount = args.GetInt("queries", 200);
        int k = args.GetInt("k", 10);

        Console.WriteLine($"Veri: {n} vektör × {dim} boyut, {queryCount} sorgu, k = {k}");
        Console.WriteLine($"SIMD donanım desteği: {System.Numerics.Vector.IsHardwareAccelerated} " +
                          $"(aynı anda {System.Numerics.Vector<float>.Count} float)\n");

        var random = new Random(123);
        var data = SyntheticData.Clustered(random, n, dim, clusters: 100);
        var queries = Enumerable.Range(0, queryCount)
            .Select(_ => SyntheticData.Perturb(random, data[random.Next(n)], noise: 0.3f))
            .ToList();

        BenchDot(data, dim);

        var exact = new BruteForceIndex();
        var lsh = new LshIndex(
            numTables: args.GetInt("tables", 8),
            bitsPerTable: args.GetInt("bits", 12),
            multiProbe: !args.Has("no-multiprobe"));

        var buildExact = Time(() => { for (int i = 0; i < n; i++) exact.Add(i.ToString(), data[i]); });
        var buildLsh = Time(() => { for (int i = 0; i < n; i++) lsh.Add(i.ToString(), data[i]); });

        var truth = new List<IReadOnlyList<SearchResult>>();
        var exactTime = Time(() => { foreach (var q in queries) truth.Add(exact.Search(q, k)); });

        double recallSum = 0;
        long candidateSum = 0;
        var lshTime = Time(() =>
        {
            for (int i = 0; i < queries.Count; i++)
            {
                var approx = lsh.Search(queries[i], k);
                candidateSum += lsh.LastCandidateCount;
                recallSum += Metrics.RecallAtK(
                    truth[i].Select(r => r.Id).ToList(),
                    approx.Select(r => r.Id).ToList(), k);
            }
        });

        Console.WriteLine($"{"İndeks",-12} {"Kurulum",10} {"Sorgu başı",12} {$"Recall@{k}",10} {"Aday",8}");
        Console.WriteLine(new string('-', 56));
        Console.WriteLine($"{"BruteForce",-12} {buildExact,8:F0}ms {exactTime / queryCount,10:F3}ms {1.0,10:F3} {n,8}");
        Console.WriteLine($"{"LSH",-12} {buildLsh,8:F0}ms {lshTime / queryCount,10:F3}ms {recallSum / queryCount,10:F3} {candidateSum / queryCount,8}");
        Console.WriteLine("\nDeneyin: --bits 8 / 16, --tables 4 / 16, --no-multiprobe");
        return Task.FromResult(0);
    }

    private static void BenchDot(List<float[]> data, int dim)
    {
        const int repeats = 5;
        float sink = 0;
        var query = data[0];
        // Isınma (JIT derlemesi ölçüme karışmasın).
        foreach (var v in data.Take(1000)) sink += VectorMath.Dot(query, v) + VectorMath.DotScalar(query, v);

        double simd = Time(() => { for (int r = 0; r < repeats; r++) foreach (var v in data) sink += VectorMath.Dot(query, v); });
        double scalar = Time(() => { for (int r = 0; r < repeats; r++) foreach (var v in data) sink += VectorMath.DotScalar(query, v); });

        Console.WriteLine($"İç çarpım ({data.Count * repeats} adet, {dim} boyut): skaler {scalar:F1} ms, SIMD {simd:F1} ms " +
                          $"→ {scalar / simd:F1}x hızlanma  (sink={sink:F0})\n");
    }

    private static double Time(Action action)
    {
        var stopwatch = Stopwatch.StartNew();
        action();
        return stopwatch.Elapsed.TotalMilliseconds;
    }
}

/// <summary>
/// Gerçek embedding'ler uzayda düzgün dağılmaz, konulara göre kümelenir. Tamamen rastgele veri
/// ANN indeksleri için gerçekçi olmayan derecede zor bir durumdur; bu yüzden kümeli veri üretilir.
/// </summary>
public static class SyntheticData
{
    public static List<float[]> Clustered(Random random, int n, int dim, int clusters)
    {
        var centers = Enumerable.Range(0, clusters).Select(_ => RandomUnit(random, dim)).ToList();
        return Enumerable.Range(0, n)
            .Select(_ => Perturb(random, centers[random.Next(clusters)], noise: 0.5f))
            .ToList();
    }

    public static float[] Perturb(Random random, float[] v, float noise)
    {
        var result = new float[v.Length];
        float scale = noise / MathF.Sqrt(v.Length);
        for (int i = 0; i < v.Length; i++)
            result[i] = v[i] + scale * Gaussian(random);
        VectorMath.NormalizeInPlace(result);
        return result;
    }

    private static float[] RandomUnit(Random random, int dim)
    {
        var v = new float[dim];
        for (int i = 0; i < dim; i++) v[i] = Gaussian(random);
        VectorMath.NormalizeInPlace(v);
        return v;
    }

    private static float Gaussian(Random random)
    {
        double u1 = 1.0 - random.NextDouble(), u2 = random.NextDouble();
        return (float)(Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2));
    }
}
