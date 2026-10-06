using System.Globalization;
using System.Text;

namespace EmbeddingSearch.Core.Text;

/// <summary>
/// Metni küçük harfe çevirip harf/rakam dışındaki karakterlerden böler.
/// </summary>
/// <remarks>
/// Türkçe için kritik nokta: "I".ToLowerInvariant() → "i" olur ama Türkçede doğrusu "ı"dır,
/// "İ".ToLowerInvariant() ise "i̇" (i + birleşik nokta) üretir. Bu yüzden tr-TR kültürü kullanılır.
/// Aksi halde "İSTANBUL" ile "istanbul" farklı kelimeler olarak görülürdü.
/// </remarks>
public sealed class Tokenizer
{
    private readonly CultureInfo _culture;

    public Tokenizer(CultureInfo? culture = null)
    {
        _culture = culture ?? CultureInfo.GetCultureInfo("tr-TR");
    }

    public static Tokenizer Turkish { get; } = new();

    public IReadOnlyList<string> Tokenize(string text)
    {
        var tokens = new List<string>();
        var current = new StringBuilder();

        foreach (char c in text.ToLower(_culture))
        {
            if (char.IsLetterOrDigit(c))
            {
                current.Append(c);
            }
            else if (current.Length > 0)
            {
                tokens.Add(current.ToString());
                current.Clear();
            }
        }

        if (current.Length > 0)
            tokens.Add(current.ToString());

        return tokens;
    }
}
