# 5. Parçalama (chunking) ve kalıcılık

## Neden belgeleri parçalara bölüyoruz?

1. **Model sınırı:** Embedding modellerinin bir girdi sınırı vardır (ör. 512 veya 8192 token).
   Fazlası kesilir ve sessizce kaybolur.
2. **Anlam seyrelmesi:** 50 sayfalık bir belgenin tek vektörü, içindeki tüm konuların bulanık
   bir ortalamasıdır. "İade süresi kaç gün?" sorusu, 37. sayfadaki tek paragrafa çok benzer,
   ama tüm belgenin ortalamasına pek benzemez.
3. **Sonuç gösterimi / RAG:** Kullanıcıya veya bir dil modeline 50 sayfa yerine ilgili paragrafı vermek istersiniz.

## Örtüşme (overlap)

```
maxWords = 10, overlapWords = 3

Parça 0: [1 2 3 4 5 6 7 8 9 10]
Parça 1:               [8 9 10 11 12 13 14 15 16 17]
Parça 2:                              [15 16 17 18 ...]
```

Bir cümle iki parçanın sınırına denk gelirse, örtüşme sayesinde en az bir parçada bütün kalır.

## Parçalama stratejileri

| Strateji | Artı | Eksi | Bu projede |
|---|---|---|---|
| Sabit kelime/token sayısı + örtüşme | Basit, öngörülebilir | Cümleyi ortadan bölebilir | ✅ `TextChunker` |
| Cümle / paragraf sınırları | Anlam bütünlüğü | Parça boyları dengesiz | Alıştırma |
| Özyinelemeli (başlık → paragraf → cümle) | Yapılı belgelerde (Markdown) çok iyi | Daha karmaşık | Alıştırma |
| Anlamsal (komşu cümlelerin embedding'i ayrışınca böl) | Konu değişimlerini yakalar | Pahalı | İleri alıştırma |

Parça boyutunun etkisini ölçün: `SearchEngine` kurucusundaki `maxWords` değerini değiştirip
`eval` komutunu çalıştırın. (Örnek belgeler kısa olduğu için önce `data/ornek` klasörüne daha uzun
metinler eklemeniz gerekebilir.)

## Kalıcılık: indeksi diske kaydetmek

Embedding hesaplamak pahalıdır (uzak API'de para, yerel modelde GPU/CPU zamanı).
Bu yüzden vektörler bir kez hesaplanıp diske yazılır:

```bash
dotnet run --project src/EmbeddingSearch.Cli -- index data/ornek --out index.embs
dotnet run --project src/EmbeddingSearch.Cli -- search "mercimek çorbası" --index index.embs
```

`Storage/IndexFile.cs` basit bir ikili biçim kullanır:

```
"EMBS" | sürüm | sağlayıcı kimliği | kayıt sayısı | [id | kaynak | parça no | metin | boyut | float × boyut] ...
```

Tasarım kararları:

- **Sihirli sayı (`EMBS`) ve sürüm:** Yanlış veya eski biçimdeki dosyalar anlaşılır bir hatayla reddedilir.
  Biçimi değiştirirseniz sürümü artırın.
- **Sağlayıcı kimliği:** Farklı bir modelle oluşturulmuş indeksi sorgulamak hatasız çalışır ama
  anlamsız sonuç verir; en sinsi hata türüdür. Bu yüzden yüklemede kontrol edilir.
- **İndeks yapısı kaydedilmez, yeniden kurulur:** Yalnızca ham vektörler saklanır; LSH tabloları ve
  BM25 ters indeksi yüklemede yeniden oluşturulur. Basitlik ↔ yükleme süresi takası.

### Kalıcılık için alıştırmalar

- Vektörleri `float16` (`System.Half`) olarak saklayın → dosya yarıya iner. Recall değişiyor mu?
- Dosyayı `MemoryMappedFile` ile açıp vektörleri kopyalamadan kullanın (çok büyük indeksler için).
- Yalnızca değişen dosyaları yeniden indeksleyen artımlı (incremental) indeksleme: dosya içeriğinin
  SHA-256 özetini saklayın.
- SQLite + [sqlite-vec](https://github.com/asg017/sqlite-vec) veya PostgreSQL + [pgvector](https://github.com/pgvector/pgvector)
  ile depolama.

**Sonraki:** [6. Değerlendirme](06-degerlendirme.md)
