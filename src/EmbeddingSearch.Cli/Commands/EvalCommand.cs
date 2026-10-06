using System.Text.Json;
using EmbeddingSearch.Core.Evaluation;
using EmbeddingSearch.Core.Search;

namespace EmbeddingSearch.Cli.Commands;

/// <summary>
/// Etiketli sorgu setiyle (data/degerlendirme.json) üç arama modunun kalitesini ölçer.
/// Bir şeyi değiştirdiğinizde (tokenizer, parça boyutu, sağlayıcı...) iyileşip iyileşmediğini buradan görün.
/// </summary>
public static class EvalCommand
{
    private sealed record EvalQuery(string Query, string[] Relevant);

    public static async Task<int> RunAsync(CliArgs args)
    {
        var provider = ProviderFactory.Create(args);
        var folder = Paths.Resolve(args.Get("data", "data/ornek"));
        var evalFile = Paths.Resolve(args.Get("queries", "data/degerlendirme.json"));
        int k = args.GetInt("k", 3);

        var queries = JsonSerializer.Deserialize<List<EvalQuery>>(
            await File.ReadAllTextAsync(evalFile),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

        var engine = await IndexBuilder.BuildAsync(folder, provider);

        Console.WriteLine($"Sağlayıcı: {provider.Fingerprint} | {queries.Count} sorgu | k = {k}\n");
        Console.WriteLine($"{"Mod",-10} {"MRR",8} {$"Recall@{k}",10}");
        Console.WriteLine(new string('-', 30));

        foreach (var mode in Enum.GetValues<SearchMode>())
        {
            var runs = new List<(IReadOnlyCollection<string>, IReadOnlyList<string>)>();
            foreach (var q in queries)
            {
                var hits = await engine.SearchAsync(q.Query, k: 10, mode);
                // Aynı belgenin birden çok parçası gelebilir; belge düzeyinde değerlendirmek için tekilleştir.
                var sources = hits.Select(h => h.Chunk.Source).Distinct().ToList();
                runs.Add((q.Relevant, sources));
            }

            double mrr = Metrics.MeanReciprocalRank(runs);
            double recall = runs.Average(r => Metrics.RecallAtK(r.Item1, r.Item2, k));
            Console.WriteLine($"{mode,-10} {mrr,8:F3} {recall,10:F3}");
        }

        return 0;
    }
}
