# Yol haritası ve alıştırmalar

Alıştırmalar zorluk sırasına göre dizilmiştir. Her birinde **önce bir test yazın**, sonra kodu.
Değişikliğin kaliteyi etkilediği yerlerde `eval`, hızı etkilediği yerlerde `bench` ile ölçün.

Seviye işaretleri: 🟢 başlangıç · 🟡 orta · 🔴 ileri

---

## Seviye 1 — Kodu tanıyın 🟢

- [ ] **1.1 Kendi verinizi indeksleyin.** Bir klasöre birkaç `.md` notunuzu koyun, `index` ve
      `search` komutlarıyla arayın. Hangi sorgular iyi, hangileri kötü çalışıyor? Not alın.
- [ ] **1.2 Stop word listesi.** `Tokenizer`'a isteğe bağlı bir etkisiz kelime listesi ekleyin
      ("ve", "bir", "bu", "da", "de", "ile", "için", "gibi"...). "gezegenler ve galaksiler" sorgusunun
      BM25 sonucunu önce/sonra karşılaştırın.
- [ ] **1.3 ASCII katlama.** `ç→c, ğ→g, ı→i, ö→o, ş→s, ü→u` dönüşümünü yapan bir seçenek ekleyin.
      Değerlendirme setine Türkçe karaktersiz sorgular ekleyip (`"cezvede kahve nasil pisirilir"`) ölçün.
- [ ] **1.4 Sonuçta eşleşen kelimeleri vurgulayın.** `Printer` sınıfında sorgu kelimelerini
      `[köşeli parantez]` veya ANSI renkleriyle işaretleyin.
- [ ] **1.5 Etkileşimli mod.** `repl` komutu: indeksi bir kez yükleyip ardı ardına sorgu alsın.
- [ ] **1.6 Skor eşiği.** `--min-score` seçeneği ile alakasız sonuçları eleyin. "otomobil" sorgusunda ne olur?

## Seviye 2 — Algoritmaları geliştirin 🟡

- [ ] **2.1 Basit kök bulma.** Kelimelerin ilk 5 karakterini kök kabul eden bir tokenizer seçeneği.
      (Türkçe bilgi erişimi literatüründe "F5 stemming" olarak bilinir.) `eval` ile ölçün.
- [ ] **2.2 Cümle tabanlı parçalayıcı.** `TextChunker`'a cümle sınırlarına (`.`, `!`, `?`) saygı
      gösteren bir strateji ekleyin. Kısaltmalara dikkat: "Dr.", "vb.", "ör.".
- [ ] **2.3 Markdown farkındalıklı parçalama.** Başlıklara (`#`, `##`) göre bölün ve her parçanın
      başına ait olduğu başlığı ekleyin. Bu basit hile genellikle isabeti ciddi artırır.
- [ ] **2.4 nDCG@k metriği.** `Metrics` sınıfına ekleyin ve testini yazın.
- [ ] **2.5 Ağırlıklı hibrit.** RRF'e her liste için ağırlık parametresi ekleyin
      (ör. vektör 0.7, BM25 0.3). En iyi ağırlığı `eval` ile bulun; sonra aşırı uyum riskini düşünün.
- [ ] **2.6 Silme ve güncelleme.** `IVectorIndex`'e `Remove(id)` ekleyin. `BruteForceIndex`'te
      düz dizide silme nasıl yapılır? ("mezar taşı" / tombstone + periyodik sıkıştırma)
- [ ] **2.7 Metadata filtreleme.** `SearchAsync`'e `Func<Chunk, bool>? filter` ekleyin
      (ör. yalnızca belirli bir klasördeki belgeler). Filtre aramadan önce mi sonra mı uygulanmalı?
      ANN indekslerinde neden zor bir problemdir?
- [ ] **2.8 Paralel arama.** `BruteForceIndex.Search`'ü `Parallel.For` ile bölümlere ayırın; her
      iş parçacığı kendi `TopK`'sını tutsun, sonunda birleştirin. Hangi n değerinden sonra kazanç başlıyor?

## Seviye 3 — Performans ve ölçek 🟡/🔴

- [ ] **3.1 BenchmarkDotNet.** `benchmarks/` altında yeni bir proje açıp `VectorMath.Dot`'u
      [BenchmarkDotNet](https://benchmarkdotnet.org) ile ölçün. `Vector<float>`, `Vector256<float>`
      ve `TensorPrimitives.Dot` (System.Numerics.Tensors paketi) sürümlerini karşılaştırın.
- [ ] **3.2 int8 nicemleme.** Vektörleri `sbyte[]` olarak saklayan bir `QuantizedBruteForceIndex`.
      Bellek 4 kat azalır. Recall ne kadar düşüyor? İpucu: önce nicemlenmiş vektörlerle 100 aday seç,
      sonra orijinal float vektörlerle yeniden puanla (rescoring).
- [ ] **3.3 İkili (binary) nicemleme.** Her boyutu tek bit'e indirin, benzerliği Hamming uzaklığıyla
      (`BitOperations.PopCount`) hesaplayın. 32 kat sıkıştırma!
- [ ] **3.4 IVF indeksi.** k-means ile vektörleri `nlist` kümeye ayırın; aramada sorguya en yakın
      `nprobe` kümeye bakın. LSH ile `bench` üzerinde karşılaştırın.
- [ ] **3.5 HNSW indeksi.** 🔴 Projenin "final sınavı". Malkov & Yashunin'in
      [makalesini](https://arxiv.org/abs/1603.09320) okuyun ve `IVectorIndex`'i uygulayan bir
      `HnswIndex` yazın. Parametreler: `M`, `efConstruction`, `efSearch`. Önce tek katmanlı
      (NSW) sürümle başlamak işi kolaylaştırır. Testiniz: brute force'a karşı recall > 0.95.
- [ ] **3.6 Bellek eşlemeli indeks.** `MemoryMappedFile` ile RAM'e sığmayan indeksler.

## Seviye 4 — Gerçek bir uygulamaya dönüştürün 🟡

- [ ] **4.1 Web API.** ASP.NET Core Minimal API ile `POST /documents` ve `GET /search?q=...&mode=hybrid`
      uç noktaları. `SearchEngine`'i singleton olarak DI'a kaydedin. Eşzamanlı ekleme/arama için
      iş parçacığı güvenliğini (`ReaderWriterLockSlim`) düşünün.
- [ ] **4.2 Basit arayüz.** Blazor veya tek bir HTML sayfası ile arama kutusu.
- [ ] **4.3 Microsoft.Extensions.AI.** `IEmbeddingProvider` yerine (veya ona adaptör olarak)
      .NET'in standart `IEmbeddingGenerator<string, Embedding<float>>` arayüzünü kullanın.
      Böylece OpenAI, Azure OpenAI, Ollama vb. için hazır istemciler takılabilir.
      Bkz. [.NET AI belgeleri](https://learn.microsoft.com/dotnet/ai/).
- [ ] **4.4 Embedding önbelleği.** Aynı metin için tekrar API çağrısı yapmayan, metnin SHA-256
      özetine göre çalışan bir `CachingEmbeddingProvider` dekoratörü.
- [ ] **4.5 Dayanıklılık.** `OpenAiCompatibleEmbeddingProvider`'a hız sınırı (HTTP 429) ve geçici
      hatalar için üstel geri çekilmeli yeniden deneme ekleyin
      (`Microsoft.Extensions.Http.Resilience` veya Polly). Büyük girdileri 100'lük gruplara bölün.
- [ ] **4.6 Yerel ONNX modeli.** 🔴 `Microsoft.ML.OnnxRuntime` ile bir sentence-transformer modelini
      (ör. çok dilli `paraphrase-multilingual-MiniLM-L12-v2`) internet olmadan çalıştıran bir
      `OnnxEmbeddingProvider`. Tokenizer için `Microsoft.ML.Tokenizers`. Mean pooling'i kendiniz yazın.
- [ ] **4.7 Vektör veritabanı.** Aynı arayüzü Qdrant, PostgreSQL + pgvector veya SQLite + sqlite-vec
      ile uygulayın. Kendi yazdığınız indeksle hız ve recall'u karşılaştırın.

## Seviye 5 — Arama kalitesinin ötesi 🔴

- [ ] **5.1 Yeniden sıralama (reranking).** İlk 50 sonucu bir cross-encoder modeli
      (ör. `bge-reranker`) veya bir LLM ile yeniden sıralayın. Neden bi-encoder'dan daha isabetli
      ama daha yavaştır?
- [ ] **5.2 RAG (Retrieval-Augmented Generation).** Arama sonuçlarını bağlam olarak bir dil modeline
      verip soruya cevap üreten `ask` komutu. Cevapta kaynak belgeyi (`Chunk.Id`) gösterin.
- [ ] **5.3 Sorgu genişletme.** Bir LLM'e sorgunun 3 farklı ifadesini ürettirip her biriyle arayın,
      sonuçları RRF ile birleştirin (multi-query retrieval).
- [ ] **5.4 MMR (Maximal Marginal Relevance).** Birbirinin kopyası olan sonuçlar yerine çeşitli
      sonuçlar döndürün. Örtüşen parçalar nedeniyle bu projede gerçekten işe yarar.

---

## Okuma listesi

**Temel kavramlar**
- Manning, Raghavan & Schütze — *Introduction to Information Retrieval* (ücretsiz çevrimiçi kitap;
  özellikle 1., 6. ve 8. bölümler: ters indeks, TF-IDF/vektör uzayı modeli, değerlendirme)
- Robertson & Zaragoza — *The Probabilistic Relevance Framework: BM25 and Beyond* (2009)
- Cormack, Clarke & Büttcher — *Reciprocal Rank Fusion outperforms Condorcet and individual Rank Learning Methods* (SIGIR 2009)

**Embedding'ler**
- Reimers & Gurevych — *Sentence-BERT* (2019) ve [sbert.net](https://www.sbert.net) belgeleri
- [MTEB Leaderboard](https://huggingface.co/spaces/mteb/leaderboard) — embedding modellerini
  karşılaştırmak için; Türkçe görevlere göre filtreleyin

**Vektör indeksleri**
- Charikar — *Similarity Estimation Techniques from Rounding Algorithms* (2002) — rastgele hiperdüzlem LSH
- Malkov & Yashunin — *Efficient and robust approximate nearest neighbor search using HNSW graphs* ([arXiv:1603.09320](https://arxiv.org/abs/1603.09320))
- Jégou, Douze & Schmid — *Product Quantization for Nearest Neighbor Search* (2011)
- [FAISS](https://github.com/facebookresearch/faiss) wiki'si — indeks türlerinin pratik karşılaştırması

**.NET performansı**
- [SIMD-accelerated types in .NET](https://learn.microsoft.com/dotnet/standard/simd)
- Stephen Toub'un her .NET sürümü için yazdığı "Performance Improvements in .NET" blog yazıları
