namespace EmbeddingSearch.Core.Indexing;

/// <summary>
/// Vektörleri saklayan ve bir sorgu vektörüne en benzer k kaydı bulan yapı.
/// Tüm vektörlerin normalize edilmiş olduğu varsayılır; skor = kosinüs benzerliği.
/// </summary>
public interface IVectorIndex
{
    int Count { get; }

    void Add(string id, float[] vector);

    IReadOnlyList<SearchResult> Search(float[] query, int k);
}
