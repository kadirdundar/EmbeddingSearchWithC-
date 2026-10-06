using System.Diagnostics;

namespace EmbeddingSearch.Cli.Commands;

public static class IndexCommand
{
    public static async Task<int> RunAsync(CliArgs args)
    {
        if (args.Positional.Count < 1)
        {
            Console.Error.WriteLine("Kullanım: index <klasör> [--out index.embs] [--provider hashing|openai]");
            return 1;
        }

        var folder = args.Positional[0];
        var output = args.Get("out", "index.embs");
        var provider = ProviderFactory.Create(args);

        var stopwatch = Stopwatch.StartNew();
        var engine = await IndexBuilder.BuildAsync(folder, provider, verbose: true);
        engine.Save(output);

        Console.WriteLine($"{engine.Chunks.Count} parça {stopwatch.ElapsedMilliseconds} ms'de indekslendi → {output}");
        return 0;
    }
}
