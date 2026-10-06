# 1. Embedding nedir?

Bilgisayar "kahve" kelimesinin ne demek olduğunu bilmez; yalnızca sayılarla çalışabilir.
**Embedding**, bir metni (kelime, cümle, paragraf) sabit uzunlukta bir sayı dizisine,
yani bir **vektöre** dönüştürme işlemidir:

```
"Türk kahvesi cezvede pişirilir"  →  [0.12, -0.03, 0.88, ..., 0.41]   (ör. 768 sayı)
```

Asıl önemli olan şu özelliktir: **anlamca yakın metinlerin vektörleri de birbirine yakın olur.**
Böylece "anlam" kavramını "uzaklık" kavramına çevirmiş oluruz ve aramayı geometriyle yapabiliriz.

## Sezgisel bir örnek: 2 boyut

Gerçek embedding'ler yüzlerce boyutludur ama fikir 2 boyutta da görülebilir.
Diyelim ki boyut 1 "yiyecek-içecekle ilgili mi?", boyut 2 "teknolojiyle ilgili mi?" olsun:

```
teknoloji ▲
          │   • "C# programlama"
          │       • "yapay zeka"
          │
          │                 • "kahve makinesi"
          │
          │                          • "Türk kahvesi"   • "mercimek çorbası"
          └──────────────────────────────────────────────► yiyecek
```

"Türk kahvesi" sorgusunun en yakın komşusu "mercimek çorbası" ve "kahve makinesi"dir;
"C# programlama" uzaktadır. Gerçek modeller bu boyutları kendileri öğrenir ve
boyutların tek tek insan tarafından okunabilir bir anlamı genellikle yoktur.

## Embedding'ler nasıl üretilir?

| Yöntem | Ne yakalar? | Bu projede |
|---|---|---|
| **Hashing trick** (kelime + karakter n-gram sayımı) | Sözcüksel benzerlik: aynı/benzer kelimeler | `HashingEmbeddingProvider` |
| TF-IDF + boyut indirgeme (LSA) | Birlikte geçen kelimeler | — (alıştırma) |
| Word2Vec / GloVe | Kelime düzeyinde anlam | — |
| **Transformer modelleri** (BERT, E5, nomic-embed, OpenAI text-embedding-3) | Cümle düzeyinde anlam, eş anlamlılar, bağlam | `OpenAiCompatibleEmbeddingProvider` |

### Bu projedeki iki sağlayıcı

**`HashingEmbeddingProvider`** internet ve model gerektirmez, bu yüzden projeyi hemen
çalıştırabilirsiniz. Her kelimeyi ve kelimenin karakter üçlülerini (`<ka`, `kah`, `ahv`, `hve`, `ve>`)
bir hash fonksiyonuyla vektörün bir hücresine eşler. Karakter üçlüleri sayesinde Türkçe'deki
ekler kısmen tolere edilir: "kahve" ile "kahvelerin" ortak üçlüleri paylaşır.

> ⚠️ Sınırı: "araba" ile "otomobil" ortak hiçbir harf grubu paylaşmaz, bu yüzden bu sağlayıcıya
> göre alakasızdırlar. Gerçek **anlamsal** arama için bir dil modeli gerekir.

**`OpenAiCompatibleEmbeddingProvider`** OpenAI'nin `/v1/embeddings` API biçimini konuşan her
servisle çalışır. En kolay ücretsiz yol [Ollama](https://ollama.com) ile yerel model kullanmaktır:

```bash
ollama pull nomic-embed-text            # veya: bge-m3 (çok dilli, Türkçe'de daha iyi)

export EMBEDDING_BASE_URL=http://localhost:11434/v1/
export EMBEDDING_MODEL=nomic-embed-text

dotnet run --project src/EmbeddingSearch.Cli -- demo --provider openai
dotnet run --project src/EmbeddingSearch.Cli -- eval --provider openai
```

`eval` sonuçlarını hashing sağlayıcısıyla karşılaştırın. Özellikle "otomobil" gibi belgede
geçmeyen ama anlamca ilgili sorgularda farkı göreceksiniz.

## Bilmeniz gereken incelikler

1. **Aynı model, aynı uzay.** Farklı modellerin vektörleri karşılaştırılamaz; hatta aynı modelin
   farklı boyut ayarı bile. Bu yüzden indeks dosyasına sağlayıcının kimliği (`Fingerprint`) yazılır
   ve `SearchEngine.Load` uyuşmazlıkta hata verir.
2. **Deterministik hash.** .NET'te `string.GetHashCode()` her uygulama başlangıcında farklı
   değer üretir (hash-flooding saldırılarına karşı). Diske kaydedilen bir şey için asla kullanmayın;
   projede FNV-1a kullanılmasının nedeni budur.
3. **Bazı modeller önek ister.** E5 ailesi `"query: ..."` ve `"passage: ..."` öneklerini bekler;
   nomic-embed `"search_query: "` / `"search_document: "` kullanır. Model kartını okuyun.
4. **Boyut ≠ kalite.** 3072 boyutlu bir vektör 384 boyutlu bir vektörden 8 kat fazla bellek tutar
   ve 8 kat yavaş karşılaştırılır; her zaman 8 kat daha iyi sonuç vermez. Kendi verinizle ölçün
   (bkz. [6. Değerlendirme](06-degerlendirme.md)).

## Kodda nereye bakmalı?

- `src/EmbeddingSearch.Core/Embeddings/IEmbeddingProvider.cs`
- `src/EmbeddingSearch.Core/Embeddings/HashingEmbeddingProvider.cs`
- `src/EmbeddingSearch.Core/Embeddings/OpenAiCompatibleEmbeddingProvider.cs`

**Sonraki:** [2. Benzerlik ölçüleri ve SIMD](02-benzerlik-olculeri.md)
