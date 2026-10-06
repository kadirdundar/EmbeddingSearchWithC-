using EmbeddingSearch.Core.Search;

namespace EmbeddingSearch.Cli.Commands;

/// <summary>Örnek verilerle üç arama modunu yan yana gösterir.</summary>
public static class DemoCommand
{
    public static async Task<int> RunAsync(CliArgs args)
    {
        var provider = ProviderFactory.Create(args);
        var folder = Paths.Resolve(args.Get("data", "data/ornek"));

        Console.WriteLine($"Sağlayıcı: {provider.Fingerprint}");
        Console.WriteLine($"İndeksleniyor: {folder}");
        var engine = await IndexBuilder.BuildAsync(folder, provider, verbose: true);
        Console.WriteLine($"Toplam {engine.Chunks.Count} parça indekslendi.\n");

        string[] queries =
        {
            "kahve nasıl hazırlanır",
            "gezegenler ve galaksiler",
            "ISTANBUL fethi",          // ASCII "I" Türkçede "ı" olur → BM25 kaçırır, trigramlar kısmen kurtarır.
            "kahvelerin köpüğü",       // Ekli kelime: karakter trigramları yakalar, BM25 kaçırır.
            "otomobil",                // Hiçbir belgede yok: sözcüksel sağlayıcının sınırı.
        };

        foreach (var query in queries)
        {
            Console.WriteLine($"══ Sorgu: \"{query}\"");
            foreach (var mode in Enum.GetValues<SearchMode>())
            {
                Console.WriteLine($"── {mode}");
                Printer.PrintHits(await engine.SearchAsync(query, k: 3, mode));
            }
            Console.WriteLine();
        }

        return 0;
    }
}
