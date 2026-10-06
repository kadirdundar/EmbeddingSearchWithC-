namespace EmbeddingSearch.Core.Indexing;

/// <summary>Bir arama sonucu: kayıt kimliği ve skoru (büyük olan daha iyi).</summary>
public readonly record struct SearchResult(string Id, float Score);
