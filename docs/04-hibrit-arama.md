# 4. Hibrit arama: BM25 + embedding

Embedding araması anlamı yakalar ama **kesin eşleşmelerde** zayıf kalabilir:
ürün kodları (`TR-4512`), özel isimler, kısaltmalar, nadir teknik terimler.
Klasik anahtar kelime araması ise tam tersi: kelime birebir geçmiyorsa bulamaz.
İkisini birleştirmek (hibrit arama) çoğu üretim sisteminde en iyi sonucu verir.

## Ters indeks (inverted index)

Anahtar kelime aramasının temel veri yapısı: her kelime için o kelimeyi içeren belgelerin listesi.

```
"kahve"   → [(kahve.txt, 3 kez)]
"gezegen" → [(uzay.txt, 1 kez)]
"ve"      → [(kahve.txt, 4), (uzay.txt, 2), (futbol.txt, 3), ...]
```

Sorgu geldiğinde yalnızca sorgudaki kelimelerin listelerine bakılır; tüm belgeleri taramaya gerek yoktur.

## BM25

Bir belgenin bir sorgu için skoru:

```
skor(D, Q) = Σ  IDF(q) · tf(q,D) · (k1 + 1) / ( tf(q,D) + k1 · (1 − b + b · |D| / ort|D|) )
            q∈Q
```

Formül korkutucu görünse de üç sezgiden oluşur:

1. **IDF** (ters belge sıklığı): Nadir kelime değerlidir. "ve" her belgede geçer → ~0 puan;
   "murabaha" tek belgede geçer → yüksek puan.
2. **Doyum (k1)**: Bir kelimenin 2. tekrarı puanı epey artırır, 50. tekrarı neredeyse hiç.
   Anahtar kelime doldurmayı (keyword stuffing) cezalandırır.
3. **Uzunluk normalizasyonu (b)**: Uzun belgeler sırf uzun oldukları için daha çok kelime içerir;
   b bunu dengeler.

Bkz. `Indexing/Bm25Index.cs`.

## İki listeyi birleştirmek: Reciprocal Rank Fusion (RRF)

BM25 skorları 0..∞, kosinüs skorları -1..1 aralığındadır; doğrudan toplanamaz.
RRF skorları değil **sıraları** kullanır:

```
RRF(d) = Σ  1 / (60 + sıra_i(d))
         i
```

| Belge | Vektör sırası | BM25 sırası | RRF |
|---|---|---|---|
| A | 1 | — | 1/61 = 0.0164 |
| B | 3 | 1 | 1/63 + 1/61 = 0.0323 |
| C | 2 | 5 | 1/62 + 1/65 = 0.0315 |

İki listede de üst sıralarda olan B kazanır. Ayar gerektirmez ve şaşırtıcı derecede sağlamdır
(Cormack, Clarke & Büttcher, SIGIR 2009). Bkz. `Search/ReciprocalRankFusion.cs`.

## Türkçe'ye özgü zorluklar

`demo` komutunu çalıştırın ve şu sorgulara dikkat edin:

- **"ISTANBUL fethi"** — Türkçe kültürde `I` küçültülünce `ı` olur, yani "ıstanbul". BM25 hiçbir
  şey bulamaz; vektör tarafı karakter üçlüleri sayesinde doğru belgeyi yine de üste getirir.
  Kullanıcılar sıklıkla Türkçe karakter kullanmadan yazar ("ozel", "cay", "sogan").
  → **Alıştırma:** Hem belgeleri hem sorguları ASCII'ye katlayan (`ç→c, ğ→g, ı→i, ö→o, ş→s, ü→u`)
  bir tokenizer seçeneği ekleyin ve `eval` sonucunu karşılaştırın.
- **"gezegenler ve galaksiler"** — Belgede "gezegenden" ve "galaksilerini" geçiyor. Türkçe eklemeli
  bir dil olduğundan BM25 birebir eşleşme bulamaz; üstelik tek eşleşen kelime "ve" olduğu için
  alakasız belgeler gelir.
  → **Alıştırma 1:** Etkisiz kelime (stop word) listesi ekleyin ("ve", "bir", "bu", "da", "de"...).
  → **Alıştırma 2:** Basit bir kök bulucu (stemmer) ekleyin; ör. kelimenin ilk 5 harfini almak
  (Türkçe IR'de şaşırtıcı derecede iyi çalışan bilinen bir sezgisel yöntemdir) veya
  [Zemberek](https://github.com/ahmetaa/zemberek-nlp) gibi bir morfolojik analizci.

## Kodda nereye bakmalı?

- `Indexing/Bm25Index.cs`, `Search/ReciprocalRankFusion.cs`
- `Search/SearchEngine.cs` → `SearchAsync` içindeki `SearchMode.Hybrid` dalı. Neden her listeden
  k yerine `k * 4` aday alındığını düşünün.
- `Text/Tokenizer.cs`

**Sonraki:** [5. Parçalama ve kalıcılık](05-parcalama-ve-kalicilik.md)
