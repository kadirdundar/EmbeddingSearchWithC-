using EmbeddingSearch.Core.Search;

namespace EmbeddingSearch.Cli.Commands;

public static class SearchCommand
{
    public static async Task<int> RunAsync(CliArgs args)
    {
        if (args.Positional.Count < 1)
        {
            Console.Error.WriteLine("Kullanım: search \"sorgu\" [--index index.embs] [--mode vector|keyword|hybrid] [--k 5]");
            return 1;
        }

        var query = string.Join(' ', args.Positional);
        var mode = Enum.Parse<SearchMode>(args.Get("mode", "hybrid"), ignoreCase: true);
        var engine = SearchEngine.Load(args.Get("index", "index.embs"), ProviderFactory.Create(args));

        Printer.PrintHits(await engine.SearchAsync(query, args.GetInt("k", 5), mode));
        return 0;
    }
}
