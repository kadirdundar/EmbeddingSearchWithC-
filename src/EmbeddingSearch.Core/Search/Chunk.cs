namespace EmbeddingSearch.Core.Search;

/// <summary>İndekslenen en küçük metin birimi: bir belgenin bir parçası.</summary>
public sealed record Chunk(string Id, string Source, int ChunkIndex, string Text);

/// <summary>Arama motorunun döndürdüğü sonuç.</summary>
public sealed record SearchHit(Chunk Chunk, float Score);

public enum SearchMode
{
    /// <summary>Yalnızca embedding benzerliği.</summary>
    Vector,

    /// <summary>Yalnızca BM25 anahtar kelime araması.</summary>
    Keyword,

    /// <summary>İkisinin RRF ile birleşimi.</summary>
    Hybrid,
}
