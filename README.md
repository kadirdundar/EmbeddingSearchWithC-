# EmbeddingSearch — C# ile embedding tabanlı arama

Sıfırdan, **dış bağımlılık olmadan** yazılmış, öğrenme amaçlı bir anlamsal arama motoru.
Amaç bir kütüphane kullanmak değil, vektör aramasının **içinde ne olduğunu** anlamaktır:
embedding, kosinüs benzerliği, SIMD, kesin ve yaklaşık (LSH) indeksler, BM25, hibrit arama
ve arama kalitesinin ölçülmesi.

```
                    ┌──────────────────────┐
  belge ──► parçala ─► embedding sağlayıcısı ├──► vektör indeksi ─┐
              │      └──────────────────────┘    (BruteForce/LSH) │
              │                                                    ├──► RRF ──► sonuçlar
              └─────────────────────────────────► BM25 indeksi  ───┘
```

## Hızlı başlangıç

Gereksinim: [.NET 8 SDK](https://dotnet.microsoft.com/download) veya üstü.

```bash
dotnet test                                                     # 39 test
dotnet run --project src/EmbeddingSearch.Cli -- demo            # örnek verilerle 3 arama modu
dotnet run --project src/EmbeddingSearch.Cli -- eval            # MRR / Recall ölçümü
dotnet run -c Release --project src/EmbeddingSearch.Cli -- bench  # SIMD ve LSH hız/doğruluk
```

Kendi belgelerinizle:

```bash
dotnet run --project src/EmbeddingSearch.Cli -- index ~/notlarim --out notlar.embs
dotnet run --project src/EmbeddingSearch.Cli -- search "aradığım konu" --index notlar.embs --mode hybrid
```

Varsayılan sağlayıcı (`hashing`) internet gerektirmez ama yalnızca **sözcüksel** benzerlik yakalar.
Gerçek **anlamsal** arama için [Ollama](https://ollama.com) ile yerel bir model kullanın:

```bash
ollama pull nomic-embed-text
export EMBEDDING_BASE_URL=http://localhost:11434/v1/ EMBEDDING_MODEL=nomic-embed-text
dotnet run --project src/EmbeddingSearch.Cli -- eval --provider openai
```

(OpenAI için `EMBEDDING_API_KEY` tanımlayın; `EMBEDDING_BASE_URL` varsayılanı OpenAI'dir.)

## Öğrenme rehberi

Belgeleri sırayla okuyun; her biri ilgili kod dosyalarına ve denenecek komutlara yönlendirir.

| # | Konu | Kod |
|---|---|---|
| 1 | [Embedding nedir?](docs/01-embedding-nedir.md) | `Embeddings/` |
| 2 | [Benzerlik ölçüleri ve SIMD](docs/02-benzerlik-olculeri.md) | `Math/VectorMath.cs` |
| 3 | [Vektör indeksleri: Brute force, LSH, HNSW](docs/03-vektor-indeksleri.md) | `Indexing/` |
| 4 | [Hibrit arama: BM25 + RRF](docs/04-hibrit-arama.md) | `Indexing/Bm25Index.cs`, `Search/` |
| 5 | [Parçalama ve kalıcılık](docs/05-parcalama-ve-kalicilik.md) | `Text/TextChunker.cs`, `Storage/` |
| 6 | [Arama kalitesini ölçmek](docs/06-degerlendirme.md) | `Evaluation/` |
| ★ | [**Yol haritası: 5 seviyede alıştırmalar + okuma listesi**](docs/yol-haritasi.md) | |

## Proje yapısı

```
src/EmbeddingSearch.Core/        Kütüphane (sıfır NuGet bağımlılığı)
  Embeddings/                    IEmbeddingProvider, Hashing, OpenAI-uyumlu sağlayıcılar
  Math/VectorMath.cs             Dot (SIMD), kosinüs, normalize
  Text/                          Türkçe farkındalıklı tokenizer, örtüşmeli parçalayıcı
  Indexing/                      BruteForceIndex, LshIndex, Bm25Index, TopK (min-heap)
  Search/                        SearchEngine, ReciprocalRankFusion
  Storage/IndexFile.cs           İkili indeks dosyası biçimi
  Evaluation/Metrics.cs          Recall@k, MRR
src/EmbeddingSearch.Cli/         demo / index / search / eval / bench komutları
tests/EmbeddingSearch.Tests/     xUnit testleri
data/ornek/                      10 kısa Türkçe örnek belge
data/degerlendirme.json          20 etiketli sorgu
docs/                            Öğrenme belgeleri
```

## Nereden başlamalı?

1. `demo` çıktısını okuyun ve her sorguda üç modun neden farklı sonuç verdiğini açıklamaya çalışın.
2. `docs/01`'den başlayarak belgeleri okuyun.
3. [Yol haritasındaki](docs/yol-haritasi.md) 🟢 alıştırmalardan birini seçin. Önce testini yazın,
   sonra `eval` ile etkisini ölçün.
