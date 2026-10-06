# 2. Benzerlik ölçüleri ve SIMD

İki vektör ne kadar "yakın"? Üç yaygın ölçü vardır.

## İç çarpım (dot product)

```
a · b = a₁b₁ + a₂b₂ + ... + aₙbₙ
```

Hem yöne hem uzunluğa duyarlıdır. Hesaplaması en ucuz olanıdır.

## Kosinüs benzerliği

```
cos(a, b) = (a · b) / (‖a‖ · ‖b‖)
```

İki vektör arasındaki açının kosinüsüdür; yalnızca **yöne** bakar, uzunluğu yok sayar.
Sonuç `[-1, 1]` aralığındadır: 1 = aynı yön, 0 = ilgisiz (dik), -1 = zıt yön.

Metin aramasında genellikle kosinüs tercih edilir, çünkü uzun bir metnin vektörü sırf uzun
olduğu için "daha benzer" görünmemelidir.

## Öklid uzaklığı

```
d(a, b) = √( Σ (aᵢ − bᵢ)² )
```

Küçük olan daha iyidir.

## Kilit fikir: önce normalize et, sonra iç çarpım yap

Bir vektörü normuna bölerseniz uzunluğu 1 olur (**birim vektör**). İki birim vektör için:

```
cos(a, b) = a · b                    (paydalar 1 olur)
‖a − b‖²  = 2 − 2 · cos(a, b)        (yani üç ölçü de aynı sıralamayı verir!)
```

Bu yüzden projede **tüm vektörler indekslenirken bir kez normalize edilir** ve aramada yalnızca
ucuz iç çarpım kullanılır. Her sorguda milyonlarca kez karekök ve bölme işleminden kurtulmuş oluruz.
`HashingEmbeddingProvider` ve `OpenAiCompatibleEmbeddingProvider` her ikisi de normalize vektör döndürür.

## SIMD: aynı anda birden çok sayı

Arama sırasında işlemcinin neredeyse tüm zamanı `Dot` fonksiyonunda geçer. Modern işlemciler
tek bir komutla 4, 8 veya 16 float'ı birlikte çarpabilir (SSE/AVX/AVX-512, ARM'de NEON).
Buna **SIMD** (Single Instruction, Multiple Data) denir.

.NET'te `System.Numerics.Vector<float>` bunu donanımdan bağımsız şekilde sunar:

```csharp
var va = MemoryMarshal.Cast<float, Vector<float>>(a);   // float[]'i 8'li gruplar olarak gör
var vb = MemoryMarshal.Cast<float, Vector<float>>(b);
for (int j = 0; j < va.Length; j++)
    sum += va[j] * vb[j];                               // tek komutta 8 çarpma + 8 toplama
```

Kendiniz ölçün:

```bash
dotnet run -c Release --project src/EmbeddingSearch.Cli -- bench
```

Örnek çıktı (AVX2'li bir makinede, 8 float genişlik):

```
İç çarpım (100000 adet, 256 boyut): skaler 30.3 ms, SIMD 10.3 ms → 3.0x hızlanma
```

> Benchmark'ları her zaman `-c Release` ile çalıştırın; Debug derlemesi optimizasyonları kapatır.
> Ciddi ölçümler için [BenchmarkDotNet](https://benchmarkdotnet.org) kullanın (yol haritasında alıştırma var).

## Bellek düzeni de önemli

`BruteForceIndex`, vektörleri `List<float[]>` yerine tek bir uzun `float[]` içinde ardışık tutar:

```
_data: [v0₀ v0₁ ... v0ₙ | v1₀ v1₁ ... v1ₙ | v2₀ ...]
```

Ardışık bellek, işlemcinin önbellek (cache) satırlarını ve önceden getirme (prefetch) mekanizmasını
verimli kullanır; ayrıca .NET çöp toplayıcısı (GC) için milyonlarca küçük nesne yerine tek bir
büyük nesne vardır.

## Kodda nereye bakmalı?

- `src/EmbeddingSearch.Core/Math/VectorMath.cs`
- `tests/EmbeddingSearch.Tests/VectorMathTests.cs` — SIMD ve skaler sonucun aynı olduğunu
  farklı uzunluklarda (1, 7, 8, 17, 1537) doğrular. Neden bu uzunluklar seçildi, düşünün.

**Sonraki:** [3. Vektör indeksleri](03-vektor-indeksleri.md)
