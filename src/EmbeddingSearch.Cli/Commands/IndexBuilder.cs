using EmbeddingSearch.Core.Embeddings;
using EmbeddingSearch.Core.Indexing;
using EmbeddingSearch.Core.Search;

namespace EmbeddingSearch.Cli.Commands;

public static class IndexBuilder
{
    private static readonly string[] Extensions = { ".txt", ".md" };

    /// <summary>Bir klasördeki .txt/.md dosyalarını indeksler. Kaynak adı, klasöre göre göreli yoldur.</summary>
    public static async Task<SearchEngine> BuildAsync(string folder, IEmbeddingProvider provider, IVectorIndex? index = null, bool verbose = false)
    {
        var engine = new SearchEngine(provider, index);
        var files = Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories)
            .Where(f => Extensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
            .OrderBy(f => f, StringComparer.Ordinal);

        foreach (var file in files)
        {
            var source = Path.GetRelativePath(folder, file).Replace('\\', '/');
            int chunks = await engine.AddDocumentAsync(source, await File.ReadAllTextAsync(file));
            if (verbose)
                Console.WriteLine($"  + {source} ({chunks} parça)");
        }

        return engine;
    }
}
