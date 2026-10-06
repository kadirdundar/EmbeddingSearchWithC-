# 6. Arama kalitesini ölçmek

"Bu değişiklik aramayı iyileştirdi mi?" sorusuna hisle değil sayıyla cevap vermek gerekir.
Bunun için bir **değerlendirme seti** gerekir: sorgular ve her sorgu için doğru (ilgili) belgeler.

Projede `data/degerlendirme.json`:

```json
[
  { "query": "ofsayt kuralı nedir", "relevant": ["futbol.txt"] },
  { "query": "katılım bankacılığı murabaha", "relevant": ["finans.txt"] }
]
```

```bash
dotnet run --project src/EmbeddingSearch.Cli -- eval
```

```
Sağlayıcı: hashing:512:3 | 20 sorgu | k = 3

Mod             MRR   Recall@3
------------------------------
Vector        0.938      0.950
Keyword       0.900      0.900
Hybrid        0.938      0.950
```

## Metrikler

**Recall@k** — İlgili belgelerin ne kadarı ilk k sonuçta?
Tek ilgili belge varsa: ilk k'da ise 1, değilse 0.

**MRR** (Mean Reciprocal Rank) — İlk doğru sonuç kaçıncı sırada? 1. ise 1, 2. ise 0.5, 3. ise 0.33...
Tüm sorguların ortalaması. Kullanıcının ilk doğru sonuca ne kadar çabuk ulaştığını ölçer.

**nDCG@k** (bu projede yok, alıştırma) — Birden çok ilgili belge ve dereceli alaka
("çok ilgili = 3, biraz ilgili = 1") olduğunda kullanılır. Üst sıralardaki doğru sonuçlara daha çok puan verir.

Bkz. `src/EmbeddingSearch.Core/Evaluation/Metrics.cs`.

## İyi bir değerlendirme seti nasıl hazırlanır?

- **Gerçek sorgular kullanın.** Belgeden kopyalanmış cümleler kolay sorgulardır ve yanıltıcı
  derecede yüksek skor verir. Kullanıcıların gerçekte nasıl yazdığını düşünün: kısa, yazım hatalı,
  Türkçe karaktersiz ("cay nasil demlenir").
- **Zor örnekler ekleyin:** eş anlamlılar ("otomobil" ↔ "araba"), belgede hiç geçmeyen ama
  anlamca ilgili sorgular, ürün kodları.
- **Yeterli sayıda olsun:** 20 sorguda tek bir sorgu MRR'ı 0.05 oynatır. Gürültüyle gerçek
  iyileşmeyi ayırt etmek için en az birkaç yüz sorgu hedefleyin.
- **Ezberlemeyin:** Ayarları sürekli aynı sette optimize ederseniz o sete aşırı uyum (overfitting)
  sağlarsınız. Bir kısmını ayrı bir test seti olarak saklayın.

## Deney önerileri

Her birini değiştirip `eval` çalıştırın, sonucu bir tabloya not edin:

1. `HashingEmbeddingProvider` boyutu: `--dim 64`, `--dim 2048`. Neden küçük boyut kötüleşir? (çakışmalar)
2. N-gram boyutu 2, 3, 4.
3. Kelime ağırlığı (`weight: 2f`) ile karakter ağırlığı arasındaki oran.
4. BM25'in `k1` ve `b` parametreleri.
5. RRF'teki `k = 60` sabiti.
6. Ollama ile gerçek bir model (`--provider openai`). Sonra eş anlamlı sorgular ekleyip tekrar.
7. Kendi sorgularınızı ekleyin: özellikle Türkçe karaktersiz yazılmış olanları.

**Sonraki:** [Yol haritası ve alıştırmalar](yol-haritasi.md)
