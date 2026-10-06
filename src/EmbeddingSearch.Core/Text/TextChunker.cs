namespace EmbeddingSearch.Core.Text;

/// <summary>
/// Uzun metinleri örtüşen (overlapping) parçalara böler.
/// </summary>
/// <remarks>
/// Neden parçalama? Embedding modellerinin bir girdi sınırı vardır ve uzun bir metnin tek
/// vektörü, içindeki pek çok konunun "ortalaması" olur; bu da aramada isabeti düşürür.
/// Örtüşme, bir cümlenin iki parçanın sınırında bölünüp anlamını kaybetmesini engeller.
/// </remarks>
public static class TextChunker
{
    public static IReadOnlyList<string> Chunk(string text, int maxWords = 120, int overlapWords = 20)
    {
        if (maxWords <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxWords));
        if (overlapWords < 0 || overlapWords >= maxWords)
            throw new ArgumentOutOfRangeException(nameof(overlapWords), "Örtüşme 0 ile maxWords arasında olmalı.");

        var words = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
            return Array.Empty<string>();
        if (words.Length <= maxWords)
            return new[] { string.Join(' ', words) };

        var chunks = new List<string>();
        int step = maxWords - overlapWords;
        for (int start = 0; start < words.Length; start += step)
        {
            int length = System.Math.Min(maxWords, words.Length - start);
            chunks.Add(string.Join(' ', words, start, length));
            if (start + length >= words.Length)
                break;
        }
        return chunks;
    }
}
