using EmbeddingSearch.Core.Text;

namespace EmbeddingSearch.Tests;

public class TextTests
{
    [Fact]
    public void Turkish_tokenizer_lowercases_dotted_capital_I_correctly()
    {
        Assert.Equal(new[] { "istanbul", "ılık" }, Tokenizer.Turkish.Tokenize("İSTANBUL ILIK"));
    }

    [Fact]
    public void Tokenizer_splits_on_punctuation()
    {
        Assert.Equal(new[] { "merhaba", "dünya", "c", "2024" }, Tokenizer.Turkish.Tokenize("Merhaba, dünya! C# 2024."));
    }

    [Fact]
    public void Short_text_is_a_single_chunk()
    {
        Assert.Single(TextChunker.Chunk("bir iki üç", maxWords: 10, overlapWords: 2));
    }

    [Fact]
    public void Empty_text_has_no_chunks()
    {
        Assert.Empty(TextChunker.Chunk("   ", maxWords: 10, overlapWords: 2));
    }

    [Fact]
    public void Chunks_overlap_and_cover_all_words()
    {
        var text = string.Join(' ', Enumerable.Range(1, 25));

        var chunks = TextChunker.Chunk(text, maxWords: 10, overlapWords: 3);

        Assert.Equal("1 2 3 4 5 6 7 8 9 10", chunks[0]);
        Assert.StartsWith("8 9 10", chunks[1]);   // Önceki parçanın son 3 kelimesi.
        Assert.EndsWith("25", chunks[^1]);
        Assert.All(chunks, c => Assert.True(c.Split(' ').Length <= 10));
    }

    [Fact]
    public void Overlap_must_be_smaller_than_chunk_size()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => TextChunker.Chunk("a b c", maxWords: 5, overlapWords: 5));
    }
}
