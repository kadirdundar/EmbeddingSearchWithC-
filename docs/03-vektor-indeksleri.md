# 3. Vektör indeksleri: kesin ve yaklaşık arama

Elimizde n tane vektör ve bir sorgu vektörü var. Sorguya en benzer **k** tanesini istiyoruz
(**k-NN**, k-nearest neighbours).

## Kesin arama: Brute force

En basit yol: sorguyu her vektörle karşılaştır, en iyi k'yı tut.

- Maliyet: **O(n · d)** (n = vektör sayısı, d = boyut)
- Her zaman doğru cevabı verir.
- 100 bin × 768 boyut ≈ 77 milyon çarpma; SIMD ile birkaç on milisaniye.

Küçümsemeyin: **birkaç yüz bin vektöre kadar çoğu zaman brute force yeterlidir** ve
yaklaşık yöntemlerin getirdiği karmaşıklığa ve hataya değmez.

### En iyi k'yı tutmak: min-heap

Tüm skorları sıralamak O(n log n)'dir. Bunun yerine k boyutlu bir **min-heap** tutarız: tepesinde
şimdiye kadarki en kötü "iyi" sonuç durur. Yeni skor ondan iyiyse yer değiştirir. Maliyet O(n log k).
Bkz. `Indexing/TopK.cs` (.NET'in `PriorityQueue<TElement, TPriority>` sınıfı kullanılır).

## Yaklaşık arama (ANN)

Milyonlarca vektörde her sorguda tüm veriyi taramak pahalıdır. **ANN** (Approximate Nearest
Neighbour) indeksleri, aramayı verinin küçük bir alt kümesine daraltır. Bedeli: bazen gerçek
en yakın komşuyu kaçırırlar. Bu kaybı **recall** ile ölçeriz:

```
Recall@k = (ANN'in bulduğu ilk k içinde gerçek ilk k'dan kaç tane var) / k
```

### LSH: Rastgele hiperdüzlemler (bu projede)

1. Uzaya rastgele bir düzlem yerleştir. Her vektör ya düzlemin bir tarafında (1) ya da diğerinde (0)dır.
2. Bunu b kez yap → her vektör için b bitlik bir **imza** (ör. `101100101101`) elde edilir.
3. Aynı imzaya sahip vektörler aynı **kovaya** konur.

İki vektör arasındaki açı θ ise, rastgele bir düzlemin onları ayırma olasılığı θ/π'dir.
Yani **benzer vektörler büyük olasılıkla aynı kovaya düşer.** Arama sırasında yalnızca sorgunun
kovasındaki adaylar kesin olarak puanlanır.

Ayar düğmeleri (`LshIndex` kurucusu):

| Parametre | Artırınca | Azaltınca |
|---|---|---|
| `bitsPerTable` | Kovalar küçülür → hızlı, recall düşer | Kovalar büyür → yavaş, recall artar |
| `numTables` | Kaçırılanı başka tabloda yakalama şansı → recall artar, bellek artar | Tersi |
| `multiProbe` | Komşu kovalara da (1 bit farklı) bak → az tabloyla yüksek recall | — |

Deneyin ve tabloyu kendiniz doldurun:

```bash
dotnet run -c Release --project src/EmbeddingSearch.Cli -- bench --bits 8
dotnet run -c Release --project src/EmbeddingSearch.Cli -- bench --bits 16
dotnet run -c Release --project src/EmbeddingSearch.Cli -- bench --tables 16 --no-multiprobe
dotnet run -c Release --project src/EmbeddingSearch.Cli -- bench --n 100000 --dim 384
```

Örnek çıktı (varsayılan ayarlar):

```
İndeks          Kurulum   Sorgu başı  Recall@10     Aday
--------------------------------------------------------
BruteForce        212ms      1.188ms      1.000    20000
LSH               259ms      0.278ms      0.918      713
```

LSH, 20.000 vektör yerine ortalama 713 aday puanlayarak ~4 kat hızlanmış, bedeli %8 recall kaybı. Hız farkı n büyüdükçe açılır: `--n 100000` ile deneyin.

> Not: `bench` tamamen rastgele değil **kümeli** sentetik veri üretir. Gerçek embedding'ler konulara
> göre kümelenir; düzgün dağılmış rastgele yüksek boyutlu veri ANN için gerçekçi olmayan
> derecede zordur ("boyutun laneti").

### Endüstride kullanılan diğer yöntemler

| Yöntem | Fikir | Nerede? |
|---|---|---|
| **IVF** (Inverted File) | k-means ile veriyi kümelere böl; sorguya en yakın birkaç kümede ara | FAISS, pgvector `ivfflat` |
| **HNSW** | Çok katmanlı "küçük dünya" grafı; üst katmanlarda büyük adımlarla, alt katmanlarda ince arama | Qdrant, Weaviate, pgvector `hnsw`, Lucene, Redis |
| **PQ** (Product Quantization) | Vektörü parçalara böl, her parçayı 1 baytlık kod ile sıkıştır | FAISS `IVFPQ`; milyarlarca vektör |
| **Skaler/İkili nicemleme** | float32 → int8 (4x küçük) veya 1 bit (32x küçük) | Çoğu vektör veritabanı |

HNSW bugün fiilî standarttır. Kendiniz yazmak harika bir alıştırmadır; bkz. [yol haritası](yol-haritasi.md).

## Kodda nereye bakmalı?

- `Indexing/IVectorIndex.cs` — tüm indekslerin ortak sözleşmesi
- `Indexing/BruteForceIndex.cs`, `Indexing/LshIndex.cs`, `Indexing/TopK.cs`
- `src/EmbeddingSearch.Cli/Commands/BenchCommand.cs`

**Sonraki:** [4. Hibrit arama](04-hibrit-arama.md)
