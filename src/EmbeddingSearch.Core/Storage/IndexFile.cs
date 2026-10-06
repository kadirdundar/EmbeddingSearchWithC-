using System.Text;
using EmbeddingSearch.Core.Search;

namespace EmbeddingSearch.Core.Storage;

public sealed record IndexRecord(Chunk Chunk, float[] Vector);

/// <summary>
/// İndeksin basit ikili (binary) dosya biçimi.
/// </summary>
/// <remarks>
/// Biçim:
/// <code>
/// "EMBS" (4 bayt) | sürüm (int32) | sağlayıcı kimliği (string) | kayıt sayısı (int32)
/// her kayıt: id | kaynak | parça no (int32) | metin | boyut (int32) | boyut × float32
/// </code>
/// JSON yerine ikili biçim seçildi: 1536 boyutlu bir vektör JSON'da ~20 KB, ikili olarak 6 KB tutar
/// ve okunması çok daha hızlıdır. Başlıktaki "sihirli sayı" ve sürüm, yanlış/eski dosyaları erken yakalar.
/// </remarks>
public static class IndexFile
{
    private static readonly byte[] Magic = "EMBS"u8.ToArray();
    private const int Version = 1;

    public static void Write(string path, string fingerprint, IEnumerable<IndexRecord> records)
    {
        var list = records.ToList();
        using var stream = File.Create(path);
        using var writer = new BinaryWriter(stream, Encoding.UTF8);

        writer.Write(Magic);
        writer.Write(Version);
        writer.Write(fingerprint);
        writer.Write(list.Count);

        foreach (var (chunk, vector) in list)
        {
            writer.Write(chunk.Id);
            writer.Write(chunk.Source);
            writer.Write(chunk.ChunkIndex);
            writer.Write(chunk.Text);
            writer.Write(vector.Length);
            foreach (float value in vector)
                writer.Write(value);
        }
    }

    public static (string Fingerprint, IReadOnlyList<IndexRecord> Records) Read(string path)
    {
        using var stream = File.OpenRead(path);
        using var reader = new BinaryReader(stream, Encoding.UTF8);

        if (!reader.ReadBytes(Magic.Length).AsSpan().SequenceEqual(Magic))
            throw new InvalidDataException($"'{path}' bir EmbeddingSearch indeks dosyası değil.");

        int version = reader.ReadInt32();
        if (version != Version)
            throw new InvalidDataException($"Desteklenmeyen indeks sürümü: {version}.");

        string fingerprint = reader.ReadString();
        int count = reader.ReadInt32();
        var records = new List<IndexRecord>(count);

        for (int i = 0; i < count; i++)
        {
            var chunk = new Chunk(
                Id: reader.ReadString(),
                Source: reader.ReadString(),
                ChunkIndex: reader.ReadInt32(),
                Text: reader.ReadString());

            var vector = new float[reader.ReadInt32()];
            for (int d = 0; d < vector.Length; d++)
                vector[d] = reader.ReadSingle();

            records.Add(new IndexRecord(chunk, vector));
        }

        return (fingerprint, records);
    }
}
