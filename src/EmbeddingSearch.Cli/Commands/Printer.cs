using EmbeddingSearch.Core.Search;

namespace EmbeddingSearch.Cli.Commands;

public static class Printer
{
    public static void PrintHits(IReadOnlyList<SearchHit> hits)
    {
        if (hits.Count == 0)
        {
            Console.WriteLine("  (sonuç yok)");
            return;
        }

        for (int i = 0; i < hits.Count; i++)
        {
            var hit = hits[i];
            Console.WriteLine($"  {i + 1}. [{hit.Score:F4}] {hit.Chunk.Id}");
            Console.WriteLine($"     {Snippet(hit.Chunk.Text, 110)}");
        }
    }

    private static string Snippet(string text, int max)
        => text.Length <= max ? text : text[..max].TrimEnd() + "…";
}
