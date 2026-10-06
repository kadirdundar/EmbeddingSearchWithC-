using System.Text;
using EmbeddingSearch.Cli;
using EmbeddingSearch.Cli.Commands;

Console.OutputEncoding = Encoding.UTF8;

if (args.Length == 0 || args[0] is "-h" or "--help" or "help")
{
    PrintHelp();
    return 0;
}

var rest = new CliArgs(args.Skip(1));
try
{
    return args[0] switch
    {
        "demo" => await DemoCommand.RunAsync(rest),
        "index" => await IndexCommand.RunAsync(rest),
        "search" => await SearchCommand.RunAsync(rest),
        "eval" => await EvalCommand.RunAsync(rest),
        "bench" => await BenchCommand.RunAsync(rest),
        _ => Unknown(args[0]),
    };
}
catch (Exception ex) when (ex is IOException or InvalidOperationException or ArgumentException or HttpRequestException)
{
    Console.Error.WriteLine($"Hata: {ex.Message}");
    return 1;
}

static int Unknown(string command)
{
    Console.Error.WriteLine($"Bilinmeyen komut: {command}\n");
    PrintHelp();
    return 1;
}

static void PrintHelp()
{
    Console.WriteLine("""
        EmbeddingSearch — C# ile embedding tabanlı arama (öğrenme projesi)

        Komutlar:
          demo                         Örnek verilerle üç arama modunu karşılaştırır
          index <klasör> [--out f]     .txt/.md dosyalarını indeksleyip diske kaydeder
          search "sorgu" [--index f]   Kaydedilmiş indekste arar
                 [--mode vector|keyword|hybrid] [--k 5]
          eval [--k 3]                 Etiketli sorgularla MRR / Recall ölçer
          bench [--n 20000] [--dim 256] [--bits 12] [--tables 8] [--no-multiprobe]
                                       SIMD ve BruteForce/LSH hız-doğruluk ölçümü

        Ortak seçenekler:
          --provider hashing|openai    Embedding sağlayıcısı (varsayılan: hashing, internet gerekmez)
          --dim 512                    hashing sağlayıcısının vektör boyutu

        openai sağlayıcısı için ortam değişkenleri:
          EMBEDDING_BASE_URL  EMBEDDING_MODEL  EMBEDDING_API_KEY
          (Ollama: EMBEDDING_BASE_URL=http://localhost:11434/v1/ EMBEDDING_MODEL=nomic-embed-text)
        """);
}
